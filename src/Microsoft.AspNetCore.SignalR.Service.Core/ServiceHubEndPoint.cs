// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
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
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.SignalR.Core.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;

namespace Microsoft.AspNetCore.SignalR.ServiceCore
{
    public class ServiceHubEndPoint<THub> : IInvocationBinder where THub : Hub
    {
        private static readonly string OnClientConnectedMethod = "OnConnectedAsync";
        private static readonly string OnDisconnectedAsyncMethod = "OnDisconnectedAsync";

        private readonly ILogger<ServiceHubEndPoint<THub>> _logger;
        private readonly HubLifetimeManager<THub> _lifetimeMgr;
        private HubConnection _hubConnection;
        // This HubConnectionList is duplicate with HubLifetimeManager
        private readonly HubConnectionList _connections = new HubConnectionList();
        private readonly IHubContext<THub> _hubContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly Dictionary<string, HubMethodDescriptor> _methods = new Dictionary<string, HubMethodDescriptor>(StringComparer.OrdinalIgnoreCase);

        public ServiceHubEndPoint(HubLifetimeManager<THub> lifetimeMgr,
            ILogger<ServiceHubEndPoint<THub>> logger,
            IServiceScopeFactory serviceScopeFactory,
            IHubContext<THub> hubContext)
        {
            _lifetimeMgr = lifetimeMgr;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _hubContext = hubContext;
            DiscoverHubMethods();
        }

        public void UseHub(string path, LogLevel logLevel = LogLevel.Information)
        {
            var encodedCredential = Convert.ToBase64String(Encoding.UTF8.GetBytes("Username:Password"));
            _hubConnection = new HubConnectionBuilder()
                                .WithHubBinder(this)
                                .WithConsoleLogger(logLevel) // Debug purpose
                                .WithUrl(path)
                                .WithHeader("Authorization", "Basic " + encodedCredential)
                                .Build();
            var output = Channel.CreateUnbounded<HubMessage>();
            async Task WriteToTransport()
            {
                try
                {
                    while (await output.Reader.WaitToReadAsync())
                    {
                        while (output.Reader.TryRead(out var hubMessage))
                        {
                            await _hubConnection.SendHubMessage(hubMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString();
                    //connectionContext.Abort(ex);
                }
            }

            var writingOutputTask = WriteToTransport();

            _hubConnection.On<HubMethodInvocationMessage>(OnClientConnectedMethod, async invocationMessage =>
            {
                await HandleOnClientConnectedAsync(invocationMessage, output);
            });

            _hubConnection.On<HubMethodInvocationMessage>(OnDisconnectedAsyncMethod, async invocationMessage =>
            {
                await HandleOnDisconnectedAsync(invocationMessage);
            });

            foreach (var hubMethod in _methods.Keys)
            {
                _hubConnection.On<HubMethodInvocationMessage>(hubMethod, async invocationMessage =>
                {
                    await HandleHubCallAsync(invocationMessage);
                });
            }
        }

        public async Task StartAsync()
        {
            try
            {
                await _hubConnection.StartAsync();
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
                    throw new NotSupportedException($"Duplicate definitions of '{methodName}'. Overloading is not supported.");
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

        private async Task HandleOnClientConnectedAsync(HubMethodInvocationMessage hubMethodInvocationMessage, Channel<HubMessage> output)
        {
            var connectionId = hubMethodInvocationMessage.GetConnectionId();
            ServiceConnectionContext serviceConnCtx = new ServiceConnectionContext(connectionId);

            ServiceHubConnectionContext serviceHubConnCtx = new ServiceHubConnectionContext(serviceConnCtx, output, _hubConnection);
            _connections.Add(serviceHubConnCtx);
            await _lifetimeMgr.OnConnectedAsync(serviceHubConnCtx);
            await HubOnConnectedAsync(serviceHubConnCtx);
            await SendMessageAsync(serviceHubConnCtx, CompletionMessage.WithResult(hubMethodInvocationMessage.InvocationId, ""));
        }

        private async Task HandleOnDisconnectedAsync(HubMethodInvocationMessage hubMethodInvocationMessage)
        {
            var connectionId = hubMethodInvocationMessage.GetConnectionId();
            HubConnectionContext serviceHubConnCtx = _connections[connectionId];
            await _lifetimeMgr.OnDisconnectedAsync(serviceHubConnCtx);
            await HubOnDisconnectedAsync(serviceHubConnCtx);
            await SendMessageAsync(serviceHubConnCtx, CompletionMessage.WithResult(hubMethodInvocationMessage.InvocationId, ""));
        }

        private async Task HandleHubCallAsync(HubMethodInvocationMessage hubMethodInvocationMessage)
        {
            try
            {
                var connectionId = hubMethodInvocationMessage.GetConnectionId();
                HubConnectionContext serviceHubConnCtx = _connections[connectionId];
                if (!_methods.TryGetValue(hubMethodInvocationMessage.Target, out var descriptor))
                {
                    // Send an error to the client. Then let the normal completion process occur
                    _logger.UnknownHubMethod(hubMethodInvocationMessage.Target);
                    await SendMessageAsync(serviceHubConnCtx, CompletionMessage.WithError(
                        hubMethodInvocationMessage.InvocationId, $"Unknown hub method '{hubMethodInvocationMessage.Target}'"));
                }
                else
                {
                    // TODO. support StreamItem 
                    await Invoke(descriptor, serviceHubConnCtx, hubMethodInvocationMessage, false);
                }
            }
            catch (Exception e)
            {
                e.ToString();
                // Abort the entire connection if the invocation fails in an unexpected way
                await _hubConnection.DisposeAsync();
                //connection.Abort(ex);
            }
        }

        private async Task SendMessageAsync(HubConnectionContext connection, HubMessage hubMessage)
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

        private async Task SendInvocationError(HubMethodInvocationMessage hubMethodInvocationMessage,
            HubConnectionContext connection, string errorMessage)
        {
            if (hubMethodInvocationMessage.NonBlocking)
            {
                return;
            }

            await SendMessageAsync(connection, CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId, errorMessage));
        }

        private void InitializeHub(THub hub, HubConnectionContext hubConnection)
        {
            hub.Clients = _hubContext.Clients;
            hub.Context = new HubCallerContext(hubConnection);
            hub.Groups = _hubContext.Groups;
        }

        private async Task<bool> IsHubMethodAuthorized(IServiceProvider provider, ClaimsPrincipal principal, IList<IAuthorizeData> policies)
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
                    await SendMessageAsync(connection, CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId,
                        $"The client attempted to invoke the streaming '{hubMethodInvocationMessage.Target}' method in a non-streaming fashion."));
                }

                return false;
            }

            if (!isStreamedResult && isStreamedInvocation)
            {
                _logger.NonStreamingMethodCalledWithStream(hubMethodInvocationMessage);
                await SendMessageAsync(connection, CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId,
                    $"The client attempted to invoke the non-streaming '{hubMethodInvocationMessage.Target}' method in a streaming fashion."));

                return false;
            }

            return true;
        }

        private static bool IsChannel(Type type, out Type payloadType)
        {
            var channelType = type.AllBaseTypes().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ChannelReader<>));
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
            var observableInterface = IsIObservable(resultType) ?
                resultType :
                resultType.GetInterfaces().FirstOrDefault(IsIObservable);

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
            HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamedInvocation)
        {
            var methodExecutor = descriptor.MethodExecutor;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                /* TODO: Authorization support
                */
                if (!await ValidateInvocationMode(methodExecutor.MethodReturnType, isStreamedInvocation, hubMethodInvocationMessage, connection))
                {
                    return;
                }

                var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                var hub = hubActivator.Create();

                try
                {
                    InitializeHub(hub, connection);

                    var result = await ExecuteHubMethod(methodExecutor, hub, hubMethodInvocationMessage.Arguments);

                    if (isStreamedInvocation)
                    {
                        // TODO. Streamed Invocation support
                    }
                    else if (!hubMethodInvocationMessage.NonBlocking)
                    {
                        _logger.SendingResult(hubMethodInvocationMessage.InvocationId, methodExecutor.MethodReturnType.FullName);
                        await SendMessageAsync(connection, CompletionMessage.WithResult(hubMethodInvocationMessage.InvocationId, result));
                    }
                }
                catch (TargetInvocationException ex)
                {
                    _logger.FailedInvokingHubMethod(hubMethodInvocationMessage.Target, ex);
                    await SendInvocationError(hubMethodInvocationMessage, connection, ex.InnerException.Message);
                }
                catch (Exception ex)
                {
                    _logger.FailedInvokingHubMethod(hubMethodInvocationMessage.Target, ex);
                    await SendInvocationError(hubMethodInvocationMessage, connection, ex.Message);
                }
                finally
                {
                    hubActivator.Release(hub);
                }
            }
        }

        private static async Task<object> ExecuteHubMethod(ObjectMethodExecutor methodExecutor, THub hub, object[] arguments)
        {
            // ReadableChannel is awaitable but we don't want to await it.
            if (methodExecutor.IsMethodAsync && !IsChannel(methodExecutor.MethodReturnType, out _))
            {
                if (methodExecutor.MethodReturnType == typeof(Task))
                {
                    await (Task)methodExecutor.Execute(hub, arguments);
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
    }

    public static class HubReflectionHelper
    {
        private static readonly Type[] _excludeInterfaces = new[] { typeof(IDisposable) };

        public static IEnumerable<MethodInfo> GetHubMethods(Type hubType)
        {
            var methods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var allInterfaceMethods = _excludeInterfaces.SelectMany(i => GetInterfaceMethods(hubType, i));

            return methods.Except(allInterfaceMethods).Where(m => IsHubMethod(m));
        }

        private static IEnumerable<MethodInfo> GetInterfaceMethods(Type type, Type iface)
        {
            if (!iface.IsAssignableFrom(type))
            {
                return Enumerable.Empty<MethodInfo>();
            }

            return type.GetInterfaceMap(iface).TargetMethods;
        }

        private static bool IsHubMethod(MethodInfo methodInfo)
        {
            var baseDefinition = methodInfo.GetBaseDefinition().DeclaringType;
            if (typeof(object) == baseDefinition || methodInfo.IsSpecialName)
            {
                return false;
            }

            var baseType = baseDefinition.GetTypeInfo().IsGenericType ? baseDefinition.GetGenericTypeDefinition() : baseDefinition;
            return typeof(Hub) != baseType;
        }
    }
}
