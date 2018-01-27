// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Internal;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Core.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR.Core;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class ServiceHubEndPoint<THub> : IInvocationBinder, IHubInvoker, IHeartbeatHandler where THub : Hub
    {
        private const string OnClientConnectedMethod = "OnConnectedAsync";
        private const string OnDisconnectedAsyncMethod = "OnDisconnectedAsync";

        private readonly List<HubConnection> _hubConnections = new List<HubConnection>();
        private readonly HubLifetimeManager<THub> _lifetimeMgr;
        private readonly ILogger<ServiceHubEndPoint<THub>> _logger;
        private readonly ServiceHubOptions _options;

        private readonly ConcurrentDictionary<string, List<InvocationHandler>> _handlers =
            new ConcurrentDictionary<string, List<InvocationHandler>>();

        // This HubConnectionList is duplicate with HubLifetimeManager
        private readonly HubConnectionList _connections = new HubConnectionList();

        private readonly IHubContext<THub> _hubContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly SignalRServiceAuthHelper _authHelper;

        private readonly Dictionary<string, HubMethodDescriptor> _methods =
            new Dictionary<string, HubMethodDescriptor>(StringComparer.OrdinalIgnoreCase);
        private readonly IHubStatManager<THub> _statManager;
        private Heartbeat _heartbeat;
        public ServiceHubEndPoint(HubLifetimeManager<THub> lifetimeMgr,
            IHubStatManager<THub> statManager,
            IOptions<ServiceHubOptions> hubOptions,
            IServiceScopeFactory serviceScopeFactory,
            IHubContext<THub> hubContext,
            SignalRServiceAuthHelper authHelper,
            ILoggerFactory loggerFactory)
        {
            _lifetimeMgr = lifetimeMgr;
            _statManager = statManager;
            _options = hubOptions.Value;
            _serviceScopeFactory = serviceScopeFactory;
            _authHelper = authHelper;

            loggerFactory.AddConsole(_options.ConsoleLogLevel);
            _logger = loggerFactory.CreateLogger<ServiceHubEndPoint<THub>>();

            _hubContext = hubContext;
            _heartbeat = new Heartbeat(new IHeartbeatHandler[] { this }, _logger);
            DiscoverHubMethods();
        }

        public void UseHub(SignalRServiceConfiguration config)
        {
            var requestHandlingQ = Channel.CreateUnbounded<HubConnectionMessageWrapper>();

            async Task WriteToTransport()
            {
                try
                {
                    while (await requestHandlingQ.Reader.WaitToReadAsync())
                    {
                        while (requestHandlingQ.Reader.TryRead(out var messageWrapper))
                        {
                            await DispatchInvocationAsync(messageWrapper);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.MessageQueueError(ex);
                }
            }

            var writingOutputTask = WriteToTransport();

            for (int i = 0; i < _options.ConnectionNumber; i++)
            {
                var builder = new HubConnectionBuilder();
                builder.WithHubBinder(this)
                    .WithConsoleLogger(_options.ConsoleLogLevel) // Debug purpose
                    // Add uid to Service URL
                    .WithUrl($"{_authHelper.GetServerUrl<THub>(config)}?uid={_options.ServerId}", _options.ReceiveBufferSize)
                    .WithTransport(TransportType.WebSockets)
                    .WithJwtBearer(() => _authHelper.GetServerToken(config))
                    .WithStat(_statManager.Stat())
                    .WithEnableMetrics(_options.EnableMetrics)
                    .WithIndex(i)
                    .WithHubProtocol(_options.ProtocolType == ProtocolType.Binary ?
                        new MessagePackHubProtocol(true) : (IHubProtocol)new JsonHubProtocol());
                if (_options.MessagePassingType == MessagePassingType.Channel)
                {
                    builder.WithMessageQueue(requestHandlingQ);
                }
                else
                {
                    builder.WithHubInvoker(this);
                }
                var hubConnection = builder.Build();
                _hubConnections.Add(hubConnection);
            }

            On<HubConnectionMessageWrapper>(OnClientConnectedMethod,
                async invocationMessage => { await HandleOnClientConnectedAsync(invocationMessage); });
            On<HubConnectionMessageWrapper>(OnDisconnectedAsyncMethod,
                async invocationMessage => { await HandleOnDisconnectedAsync(invocationMessage); });
            foreach (var hubMethod in _methods.Keys)
            {
                On<HubConnectionMessageWrapper>(hubMethod,
                    async invocationMessage => { await HandleHubCallAsync(invocationMessage, 
                        (HubInvocationMessage)invocationMessage.HubMethodInvocationMessage is StreamItemMessage); });
            }
        }

        public async Task StartAsync()
        {
            try
            {
                _heartbeat.Start();
                foreach (var hubConnection in _hubConnections)
                {
                    await hubConnection.StartAsync();
                }
            }
            catch (Exception e)
            {
                _logger.ServiceConnectionCanceled(e);
            }
        }

        private void DiscoverHubMethods()
        {
            var hubType = typeof(THub);
            var hubTypeInfo = hubType.GetTypeInfo();

            foreach (var methodInfo in HubReflectionHelper.GetHubMethods(hubType))
            {
                var methodName = methodInfo.Name;

                if (_methods.ContainsKey(methodName))
                {
                    throw new NotSupportedException(
                        $"Duplicate definitions of '{methodName}'. Overloading is not supported.");
                }

                var executor = ObjectMethodExecutor.Create(methodInfo, hubTypeInfo);
                var authorizeAttributes = methodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
                _methods[methodName] = new HubMethodDescriptor(executor, authorizeAttributes);

                _logger.HubMethodBound(methodName);
            }
        }

        private async Task HubOnConnectedAsync(ServiceHubConnectionContext connection)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                    var hub = hubActivator.Create();
                    try
                    {
                        InitializeHub(hub, connection);
                        await hub.OnConnectedAsync();
                    }
                    finally
                    {
                        hubActivator.Release(hub);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorInvokingHubMethod("OnConnectedAsync", ex);
                throw;
            }
        }

        private async Task HubOnDisconnectedAsync(HubConnectionContext connection)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                    var hub = hubActivator.Create();
                    try
                    {
                        InitializeHub(hub, connection);
                        await hub.OnDisconnectedAsync(null);
                    }
                    finally
                    {
                        hubActivator.Release(hub);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorInvokingHubMethod("OnDisconnectedAsync", ex);
                throw;
            }
        }

        private async Task SendCompletionMessage(HubMethodInvocationMessage origRequest,
            HubConnectionContext serviceConnection, object result)
        {
            var completeMessage = CompletionMessage.WithResult(origRequest.InvocationId, result);
            completeMessage.AddMetadata(origRequest.Metadata);
            if (_options.EnableMetrics)
            {
                ServiceMetrics.MarkSendMsgToServiceStage(completeMessage.Metadata);
            }
            await SendMessageAsync(serviceConnection, completeMessage);
        }

        private async Task HandleOnClientConnectedAsync(HubConnectionMessageWrapper messageWrapper)
        {
            var message = messageWrapper.HubMethodInvocationMessage;
            var connectionId = message.GetConnectionId();
            var connContext = new ServiceConnectionContext(connectionId);
            if (message.TryGetClaims(out var claims))
            {
                connContext.User = new ClaimsPrincipal();
                connContext.User.AddIdentity(new ClaimsIdentity(claims));
            }
            var hubConnection = _hubConnections[messageWrapper.HubConnectionIndex];
            var hubConnContext = new ServiceHubConnectionContext(connContext, hubConnection.Output, hubConnection, _options);

            _connections.Add(hubConnContext);
            await _lifetimeMgr.OnConnectedAsync(hubConnContext);
            await HubOnConnectedAsync(hubConnContext);
            if (!_options.DontSendComplete)
            {
                await SendCompletionMessage(message, hubConnContext, null);
            }
        }

        private async Task HandleOnDisconnectedAsync(HubConnectionMessageWrapper messageWrapper)
        {
            var message = messageWrapper.HubMethodInvocationMessage;
            var connectionId = message.GetConnectionId();
            var hubConnContext = _connections[connectionId];
            await HubOnDisconnectedAsync(hubConnContext);
            await _lifetimeMgr.OnDisconnectedAsync(hubConnContext);
            if (!_options.DontSendComplete)
            {
                await SendCompletionMessage(message, hubConnContext, null);
            }
            _connections.Remove(hubConnContext);
        }

        private async Task HandleHubCallAsync(HubConnectionMessageWrapper messageWrapper, bool isStreamedInvocation)
        {
            if (_options.EchoAll4TroubleShooting)
            {
                messageWrapper.HubMethodInvocationMessage.AddAction("InvokeConnectionAsync");
                await _hubConnections[messageWrapper.HubConnectionIndex].SendHubMessage(messageWrapper.HubMethodInvocationMessage);
                if (!_options.DontSendComplete)
                {
                    await SendCompletionMessage(messageWrapper.HubMethodInvocationMessage,
                    _connections[messageWrapper.HubMethodInvocationMessage.GetConnectionId()], null);
                }
                return;
            }
            var message = messageWrapper.HubMethodInvocationMessage;
            try
            {
                var connectionId = message.GetConnectionId();
                var hubConnContext = _connections[connectionId];
                if (_methods.TryGetValue(message.Target, out var descriptor))
                {
                    // TODO. support StreamItem 
                    await Invoke(descriptor, hubConnContext, message, isStreamedInvocation);
                }
                else
                {
                    // Send an error to the client. Then let the normal completion process occur
                    _logger.UnknownHubMethod(message.Target);
                    await SendMessageAsync(hubConnContext, CompletionMessage.WithError(
                        message.InvocationId,
                        $"Unknown hub method '{message.Target}'").AddConnectionId(connectionId));
                }
            }
            catch (Exception e)
            {
                _logger.FailToCallHub(message.Target, e);
                // Abort the entire connection if the invocation fails in an unexpected way
                await _hubConnections[messageWrapper.HubConnectionIndex].DisposeAsync();
                //connection.Abort(ex);
            }
        }

        private async Task SendMessageAsync(HubConnectionContext connection, HubMessage hubMessage)
        {
            if (_options.MessagePassingType == MessagePassingType.Channel)
            {
                while (await connection.Output.WaitToWriteAsync())
                {
                    if (connection.Output.TryWrite(hubMessage))
                    {
                        return;
                    }
                }

                // Output is closed. Cancel this invocation completely
                _logger.OutboundChannelClosed();
                throw new OperationCanceledException("Outbound channel was closed while trying to write hub message");
            }
            else
            {
                ServiceHubConnectionContext serviceConnection = (ServiceHubConnectionContext)connection;
                _ = serviceConnection.HubConnection.SendHubMessage(hubMessage);
            }
        }

        private async Task SendInvocationError(HubMethodInvocationMessage hubMethodInvocationMessage,
            HubConnectionContext connection, string errorMessage)
        {
            if (hubMethodInvocationMessage.NonBlocking)
            {
                return;
            }

            await SendMessageAsync(connection,
                CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId, errorMessage).AddConnectionId(hubMethodInvocationMessage.GetConnectionId()));
        }

        private void InitializeHub(THub hub, HubConnectionContext hubConnection)
        {
            hub.Clients = _hubContext.Clients;
            hub.Context = new HubCallerContext(hubConnection);
            hub.Groups = _hubContext.Groups;
        }

        private async Task<bool> IsHubMethodAuthorized(IServiceProvider provider, ClaimsPrincipal principal,
            IList<IAuthorizeData> policies)
        {
            // If there are no policies we don't need to run auth
            if (!policies.Any())
            {
                return true;
            }

            var authService = provider.GetRequiredService<IAuthorizationService>();
            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            var authorizePolicy = await AuthorizationPolicy.CombineAsync(policyProvider, policies);
            // AuthorizationPolicy.CombineAsync only returns null if there are no policies and we check that above
            Debug.Assert(authorizePolicy != null);

            var authorizationResult = await authService.AuthorizeAsync(principal, authorizePolicy);
            // Only check authorization success, challenge or forbid wouldn't make sense from a hub method invocation
            return authorizationResult.Succeeded;
        }

        private async Task<bool> ValidateInvocationMode(Type resultType, bool isStreamedInvocation,
            HubMethodInvocationMessage hubMethodInvocationMessage, HubConnectionContext connection)
        {
            var isStreamedResult = IsStreamed(resultType);
            if (isStreamedResult && !isStreamedInvocation)
            {
                if (!hubMethodInvocationMessage.NonBlocking)
                {
                    _logger.StreamingMethodCalledWithInvoke(hubMethodInvocationMessage);
                    await SendMessageAsync(connection, CompletionMessage.WithError(
                        hubMethodInvocationMessage.InvocationId,
                        $"The client attempted to invoke the streaming '{hubMethodInvocationMessage.Target}' method in a non-streaming fashion.")
                        .AddConnectionId(hubMethodInvocationMessage.GetConnectionId()));
                }

                return false;
            }

            if (!isStreamedResult && isStreamedInvocation)
            {
                _logger.NonStreamingMethodCalledWithStream(hubMethodInvocationMessage);
                await SendMessageAsync(connection, CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId,
                    $"The client attempted to invoke the non-streaming '{hubMethodInvocationMessage.Target}' method in a streaming fashion.")
                    .AddConnectionId(hubMethodInvocationMessage.GetConnectionId()));

                return false;
            }

            return true;
        }

        private static bool IsChannel(Type type, out Type payloadType)
        {
            var channelType = type.AllBaseTypes().FirstOrDefault(t =>
                t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ChannelReader<>));
            if (channelType == null)
            {
                payloadType = null;
                return false;
            }
            else
            {
                payloadType = channelType.GetGenericArguments()[0];
                return true;
            }
        }

        private static bool IsIObservable(Type iface)
        {
            return iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IObservable<>);
        }

        private static bool IsStreamed(Type resultType)
        {
            var observableInterface = IsIObservable(resultType)
                ? resultType
                : resultType.GetInterfaces().FirstOrDefault(IsIObservable);

            if (observableInterface != null)
            {
                return true;
            }

            if (IsChannel(resultType, out _))
            {
                return true;
            }

            return false;
        }

        private async Task Invoke(HubMethodDescriptor descriptor, HubConnectionContext connection,
            HubMethodInvocationMessage message, bool isStreamedInvocation)
        {
            var methodExecutor = descriptor.MethodExecutor;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                if (!await IsHubMethodAuthorized(scope.ServiceProvider, connection.User, descriptor.Policies))
                {
                    _logger.HubMethodNotAuthorized(message.Target);
                    await SendInvocationError(message, connection,
                        $"Failed to invoke '{message.Target}' because user is unauthorized");
                    return;
                }

                if (!await ValidateInvocationMode(methodExecutor.MethodReturnType, isStreamedInvocation,
                    message, connection))
                {
                    return;
                }

                var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                var hub = hubActivator.Create();

                try
                {
                    InitializeHub(hub, connection);

                    var result = await ExecuteHubMethod(methodExecutor, hub, message.Arguments);

                    if (isStreamedInvocation)
                    {
                        // TODO. Streamed Invocation support
                    }
                    else if (!message.NonBlocking)
                    {
                        _logger.SendingResult(message.InvocationId,
                            methodExecutor.MethodReturnType.FullName);
                        if (!_options.DontSendComplete)
                        {
                            await SendCompletionMessage(message, connection, result);
                        }
                    }
                }
                catch (TargetInvocationException ex)
                {
                    _logger.FailedInvokingHubMethod(message.Target, ex);
                    await SendInvocationError(message, connection, ex.InnerException.Message);
                }
                catch (Exception ex)
                {
                    _logger.FailedInvokingHubMethod(message.Target, ex);
                    await SendInvocationError(message, connection, ex.Message);
                }
                finally
                {
                    hubActivator.Release(hub);
                }
            }
        }

        private static async Task<object> ExecuteHubMethod(ObjectMethodExecutor methodExecutor, THub hub,
            object[] arguments)
        {
            // ReadableChannel is awaitable but we don't want to await it.
            if (methodExecutor.IsMethodAsync && !IsChannel(methodExecutor.MethodReturnType, out _))
            {
                if (methodExecutor.MethodReturnType == typeof(Task))
                {
                    await (Task) methodExecutor.Execute(hub, arguments);
                }
                else
                {
                    return await methodExecutor.ExecuteAsync(hub, arguments);
                }
            }
            else
            {
                return methodExecutor.Execute(hub, arguments);
            }

            return null;
        }

        private IDisposable On<T1>(string methodName, Action<T1> handler)
        {
            return On(methodName,
                new[] {typeof(T1)},
                args => handler((T1) args[0]));
        }

        private IDisposable On(string methodName, Type[] parameterTypes, Action<object[]> handler)
        {
            return On(methodName, parameterTypes, (parameters, state) =>
            {
                var currentHandler = (Action<object[]>) state;
                currentHandler(parameters);
                return Task.CompletedTask;
            }, handler);
        }

        private IDisposable On(string methodName, Type[] parameterTypes, Func<object[], object, Task> handler,
            object state)
        {
            var invocationHandler = new InvocationHandler(parameterTypes, handler, state);
            var invocationList = _handlers.AddOrUpdate(methodName, _ => new List<InvocationHandler> {invocationHandler},
                (_, invocations) =>
                {
                    lock (invocations)
                    {
                        invocations.Add(invocationHandler);
                    }
                    return invocations;
                });

            return new Subscription(invocationHandler, invocationList);
        }

        private async Task DispatchInvocationAsync(HubConnectionMessageWrapper messageWrapper)
        {
            HubMethodInvocationMessage invocation = messageWrapper.HubMethodInvocationMessage;
            // Find the handler
            if (!_handlers.TryGetValue(invocation.Target, out var handlers))
            {
                _logger.MissingHandler(invocation.Target);
                return;
            }

            //TODO: Optimize this!
            // Copying the callbacks to avoid concurrency issues
            InvocationHandler[] copiedHandlers;
            lock (handlers)
            {
                copiedHandlers = new InvocationHandler[handlers.Count];
                handlers.CopyTo(copiedHandlers);
            }

            foreach (var handler in copiedHandlers)
            {
                try
                {
                    await handler.InvokeAsync(new object[] {messageWrapper});
                }
                catch (Exception ex)
                {
                    _logger.ErrorInvokingClientSideMethod(invocation.Target, ex);
                }
            }
        }

        Type IInvocationBinder.GetReturnType(string invocationId)
        {
            return typeof(object);
        }

        Type[] IInvocationBinder.GetParameterTypes(string methodName)
        {
            HubMethodDescriptor descriptor;
            if (!_methods.TryGetValue(methodName, out descriptor))
            {
                return Type.EmptyTypes;
            }
            return descriptor.ParameterTypes;
        }

        public void OnHeartbeat(DateTimeOffset now)
        {
            _statManager.Tick(now);
        }

        public async Task OnInvocationAsync(HubConnectionMessageWrapper hubMessage, bool isStreamedInvocation = false)
        {
            switch (hubMessage.HubMethodInvocationMessage.Target)
            {
                case OnClientConnectedMethod:
                    await HandleOnClientConnectedAsync(hubMessage);
                    break;
                case OnDisconnectedAsyncMethod:
                    await HandleOnDisconnectedAsync(hubMessage);
                    break;
                default:
                    await HandleHubCallAsync(hubMessage, isStreamedInvocation);
                    break;
            }
        }

        private class HubMethodDescriptor
        {
            public HubMethodDescriptor(ObjectMethodExecutor methodExecutor, IEnumerable<IAuthorizeData> policies)
            {
                MethodExecutor = methodExecutor;
                ParameterTypes = methodExecutor.MethodParameters.Select(p => p.ParameterType).ToArray();
                Policies = policies.ToArray();
            }

            public ObjectMethodExecutor MethodExecutor { get; }

            public Type[] ParameterTypes { get; }

            public IList<IAuthorizeData> Policies { get; }
        }

        private class Subscription : IDisposable
        {
            private readonly InvocationHandler _handler;
            private readonly List<InvocationHandler> _handlerList;

            public Subscription(InvocationHandler handler, List<InvocationHandler> handlerList)
            {
                _handler = handler;
                _handlerList = handlerList;
            }

            public void Dispose()
            {
                lock (_handlerList)
                {
                    _handlerList.Remove(_handler);
                }
            }
        }

        private struct InvocationHandler
        {
            public Type[] ParameterTypes { get; }
            private readonly Func<object[], object, Task> _callback;
            private readonly object _state;

            public InvocationHandler(Type[] parameterTypes, Func<object[], object, Task> callback, object state)
            {
                _callback = callback;
                ParameterTypes = parameterTypes;
                _state = state;
            }

            public Task InvokeAsync(object[] parameters)
            {
                return _callback(parameters, _state);
            }
        }
    }
}