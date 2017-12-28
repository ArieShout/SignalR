// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Core.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceConnection<THub> : IInvocationBinder where THub : Hub
    {
        private const string OnClientConnectedMethod = "OnConnectedAsync";
        private const string OnDisconnectedAsyncMethod = "OnDisconnectedAsync";

        private readonly List<HubConnection> _hubConnections = new List<HubConnection>();
        private readonly HubLifetimeManager<THub> _lifetimeMgr;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ServiceConnection<THub>> _logger;
        private readonly ServiceOptions _serviceOptions;

        private readonly ConcurrentDictionary<string, List<InvocationHandler>> _handlers =
            new ConcurrentDictionary<string, List<InvocationHandler>>();

        // This HubConnectionList is duplicate with HubLifetimeManager
        private readonly HubConnectionList _connections = new HubConnectionList();

        private readonly ServiceAuthHelper _authHelper;

        private readonly IHubInvoker<THub> _hubInvoker;

        private readonly List<string> _methods = new List<string>();

        public ServiceConnection(HubLifetimeManager<THub> lifetimeMgr,
            IOptions<ServiceOptions> serviceOptions,
            ServiceAuthHelper authHelper,
            ILoggerFactory loggerFactory, IHubInvoker<THub> hubInvoker)
        {
            _lifetimeMgr = lifetimeMgr;
            _serviceOptions = serviceOptions.Value;
            _authHelper = authHelper;
            _hubInvoker = hubInvoker;

            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ServiceConnection<THub>>();

            DiscoverHubMethods();
        }

        public void UseHub(ServiceCredential config)
        {
            var requestHandlingQ = Channel.CreateUnbounded<HubMessageWrapper>();

            for (var i = 0; i < _serviceOptions.ConnectionNumber; i++)
            {
                var hubConnection = new HubConnectionBuilder()
                    //.WithHubBinder(this)
                    .WithConsoleLogger() // Debug purpose
                    .WithUrl(_authHelper.GetServerUrl<THub>(config))
                    .WithJwtBearer(() => _authHelper.GetServerToken<THub>(config))
                    //.WithMessageQueue(requestHandlingQ)
                    .Build();
                _hubConnections.Add(hubConnection);
            }

            On<HubMessageWrapper>(OnClientConnectedMethod,
                async invocationMessage => { await OnConnectedAsync(invocationMessage); });
            On<HubMessageWrapper>(OnDisconnectedAsyncMethod,
                async invocationMessage => { await OnDisconnectedAsync(invocationMessage); });
            foreach (var hubMethod in _methods)
            {
                On<HubMessageWrapper>(hubMethod,
                    async invocationMessage => { await OnInvocationAsync(invocationMessage); });
            }
        }

        public async Task StartAsync()
        {
            try
            {
                var tasks = _hubConnections.Select(c => c.StartAsync());
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
                //_logger.ServiceConnectionCanceled(e);
            }
        }

        private void DiscoverHubMethods()
        {
            var hubType = typeof(THub);

            foreach (var methodInfo in HubReflectionHelper.GetHubMethods(hubType))
            {
                var methodName = methodInfo.Name;

                if (_methods.Contains(methodName))
                {
                    throw new NotSupportedException(
                        $"Duplicate definitions of '{methodName}'. Overloading is not supported.");
                }

                _methods.Add(methodName);
                _logger.HubMethodBound(methodName);
            }
        }

        private async Task OnConnectedAsync(HubMessageWrapper messageWrapper)
        {
            var message = messageWrapper.HubMethodInvocationMessage;
            var connContext = CreateConnectionContext(message);
            var hubConnContext = new HubConnectionContext(connContext, TimeSpan.FromSeconds(30), _loggerFactory);

            _connections.Add(hubConnContext);

            await _lifetimeMgr.OnConnectedAsync(hubConnContext);

            await _hubInvoker.OnConnectedAsync(hubConnContext);

            await SendMessageAsync(hubConnContext, CompletionMessage.WithResult(message.InvocationId, ""));
        }

        private ServiceConnectionContext CreateConnectionContext(HubInvocationMessage message)
        {
            var connectionId = message.GetConnectionId();
            var connectionContext = new ServiceConnectionContext(connectionId);
            if (message.TryGetClaims(out var claims))
            {
                connectionContext.User = new ClaimsPrincipal();
                connectionContext.User.AddIdentity(new ClaimsIdentity(claims));
            }
            return connectionContext;
        }

        private async Task OnDisconnectedAsync(HubMessageWrapper messageWrapper)
        {
            var message = messageWrapper.HubMethodInvocationMessage;
            var connectionId = message.GetConnectionId();
            var hubConnContext = _connections[connectionId];

            await _hubInvoker.OnDisconnectedAsync(hubConnContext, null);

            await _lifetimeMgr.OnDisconnectedAsync(hubConnContext);

            await SendMessageAsync(hubConnContext, CompletionMessage.WithResult(message.InvocationId, ""));
        }

        private async Task OnInvocationAsync(HubMessageWrapper messageWrapper)
        {
            var message = messageWrapper.HubMethodInvocationMessage;
            var connectionId = message.GetConnectionId();
            var hubConnContext = _connections[connectionId];

            await _hubInvoker.OnInvocationAsync(hubConnContext, message, false);
        }

        private async Task SendMessageAsync(HubConnectionContext connection, HubMessage hubMessage)
        {
            while (await connection.Output.Writer.WaitToWriteAsync())
            {
                if (connection.Output.Writer.TryWrite(hubMessage))
                {
                    return;
                }
            }

            // Output is closed. Cancel this invocation completely
            _logger.OutboundChannelClosed();
            throw new OperationCanceledException("Outbound channel was closed while trying to write hub message");
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

        Type IInvocationBinder.GetReturnType(string invocationId)
        {
            return _hubInvoker.GetReturnType(invocationId);
        }

        Type[] IInvocationBinder.GetParameterTypes(string methodName)
        {
            return _hubInvoker.GetParameterTypes(methodName);
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
