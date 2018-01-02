// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Core.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using System.Security.Claims;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceConnection<THub> : IInvocationBinder where THub : Hub
    {
        private const string OnConnectedAsyncMethod = "OnConnectedAsync";
        private const string OnDisconnectedAsyncMethod = "OnDisconnectedAsync";

        private readonly List<HttpConnection> _httpConnections = new List<HttpConnection>();
        private readonly HubLifetimeManager<THub> _lifetimeMgr;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ServiceConnection<THub>> _logger;
        private readonly ServiceOptions _serviceOptions;

        //private readonly ConcurrentDictionary<string, List<InvocationHandler>> _handlers =
        //    new ConcurrentDictionary<string, List<InvocationHandler>>();

        // This HubConnectionList is duplicate with HubLifetimeManager
        private readonly HubConnectionList _connections = new HubConnectionList();

        private readonly ServiceAuthHelper _authHelper;

        private readonly IHubInvoker<THub> _hubInvoker;

        //private readonly List<string> _methods = new List<string>();

        public ServiceConnection(HubLifetimeManager<THub> lifetimeMgr,
            IOptions<ServiceOptions> serviceOptions,
            ServiceAuthHelper authHelper,
            ILoggerFactory loggerFactory,
            IHubInvoker<THub> hubInvoker)
        {
            _lifetimeMgr = lifetimeMgr;
            _serviceOptions = serviceOptions.Value;
            _authHelper = authHelper;
            _hubInvoker = hubInvoker;

            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ServiceConnection<THub>>();
        }

        public void UseHub(ServiceCredential credential)
        {
            var serviceUrl = GetServiceUrl(credential);
            var httpOptions = new HttpOptions
            {
                JwtBearerTokenFactory = () => _authHelper.GetServerToken<THub>(credential)
            };

            for (var i = 0; i < _serviceOptions.ConnectionNumber; i++)
            {
                var httpConnection = CreateHttpConnection(serviceUrl, httpOptions);
                _httpConnections.Add(httpConnection);
            }
        }

        public async Task StartAsync()
        {
            try
            {
                var tasks = _httpConnections.Select(c => c.StartAsync());
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
                //_logger.ServiceConnectionCanceled(e);
            }
        }

        #region Private Methods

        private Uri GetServiceUrl(ServiceCredential credential)
        {
            return new Uri(_authHelper.GetServerUrl<THub>(credential));
        }

        private HttpConnection CreateHttpConnection(Uri serviceUrl, HttpOptions httpOptions)
        {
            var httpConnection =
                new HttpConnection(serviceUrl, TransportType.WebSockets, _loggerFactory, httpOptions);
            var connectionContext = new DefaultConnectionContext(string.Empty, httpConnection.Transport,
                httpConnection.Application);
            var hubConnection =
                new HubConnectionContext(connectionContext, TimeSpan.FromSeconds(30), _loggerFactory);

            httpConnection.OnReceived((data, state) => ((ServiceConnection<THub>)state).OnDataReceivedAsync(httpConnection, hubConnection, data), this);
            httpConnection.Closed += Shutdown;

            return httpConnection;
        }

        private async Task OnDataReceivedAsync(HttpConnection httpConnection, HubConnectionContext connection, byte[] data)
        {
            if (!connection.ProtocolReaderWriter.ReadMessages(data, _hubInvoker, out var messages)) return;

            foreach (var message in messages)
            {
                switch (message)
                {
                    case InvocationMessage invocationMessage:
                        _logger.ReceivedHubInvocation(invocationMessage);

                        // Don't wait on the result of execution, continue processing other
                        // incoming messages on this connection.
                        _ = OnInvocationAsync(httpConnection, invocationMessage);
                        break;

                    case StreamInvocationMessage streamInvocationMessage:
                        _logger.ReceivedStreamHubInvocation(streamInvocationMessage);

                        // Don't wait on the result of execution, continue processing other
                        // incoming messages on this connection.
                        _ = _hubInvoker.OnInvocationAsync(connection, streamInvocationMessage,
                            isStreamedInvocation: true);
                        break;

                    case CancelInvocationMessage cancelInvocationMessage:
                        // Check if there is an associated active stream and cancel it if it exists.
                        // The cts will be removed when the streaming method completes executing
                        if (connection.ActiveRequestCancellationSources.TryGetValue(
                            cancelInvocationMessage.InvocationId, out var cts))
                        {
                            _logger.CancelStream(cancelInvocationMessage.InvocationId);
                            cts.Cancel();
                        }
                        else
                        {
                            // Stream can be canceled on the server while client is canceling stream.
                            _logger.UnexpectedCancel();
                        }
                        break;

                    case CompletionMessage completionMessage:
                        _logger.ReceivedCompletion(completionMessage);

                        _ = _hubInvoker.OnCompletionAsync(connection, completionMessage);
                        break;

                    case PingMessage _:
                        // We don't care about pings
                        break;

                    // Other kind of message we weren't expecting
                    default:
                        _logger.UnsupportedMessageReceived(message.GetType().FullName);
                        throw new NotSupportedException($"Received unsupported message: {message}");
                }
            }

            await Task.CompletedTask;
        }

        private void Shutdown(Exception exception = null)
        {
        }

        private async Task OnInvocationAsync(HttpConnection httpConnection, InvocationMessage message)
        {
            switch (message.Target.ToLower())
            {
                case OnConnectedAsyncMethod:
                    await OnConnectedAsync(httpConnection, message);
                    break;

                case OnDisconnectedAsyncMethod:
                    await OnDisconnectedAsync(httpConnection, message);
                    break;

                default:
                    await OnInvocationAsync(message);
                    break;
            }
        }

        private async Task OnConnectedAsync(HttpConnection httpConnection, HubInvocationMessage message)
        {
            var connection = CreateHubConnectionContext(httpConnection, message);

            await _lifetimeMgr.OnConnectedAsync(connection);

            await _hubInvoker.OnConnectedAsync(connection);

            await SendMessageAsync(httpConnection, connection, CompletionMessage.WithResult(message.InvocationId, ""));
        }

        private async Task OnDisconnectedAsync(HttpConnection httpConnection, HubInvocationMessage message)
        {
            var connection = GetHubConnectionContext(message);

            await _hubInvoker.OnDisconnectedAsync(connection, null);

            await _lifetimeMgr.OnDisconnectedAsync(connection);

            await SendMessageAsync(httpConnection, connection, CompletionMessage.WithResult(message.InvocationId, ""));
        }

        private async Task OnInvocationAsync(HubMethodInvocationMessage message)
        {
            var connection = GetHubConnectionContext(message);
            await _hubInvoker.OnInvocationAsync(connection, message, false);
        }

        private HubConnectionContext CreateHubConnectionContext(HttpConnection httpConnection,
            HubInvocationMessage message)
        {
            var context = CreateConnectionContext(httpConnection, message);
            return new HubConnectionContext(context, TimeSpan.FromSeconds(30), _loggerFactory);
        }

        private DefaultConnectionContext CreateConnectionContext(HttpConnection httpConnection, HubInvocationMessage message)
        {
            var connectionId = message.GetConnectionId();
            var connectionContext = new DefaultConnectionContext(connectionId, httpConnection.Transport, httpConnection.Application);
            if (message.TryGetClaims(out var claims))
            {
                connectionContext.User = new ClaimsPrincipal();
                connectionContext.User.AddIdentity(new ClaimsIdentity(claims));
            }
            return connectionContext;
        }

        private HubConnectionContext GetHubConnectionContext(HubInvocationMessage message)
        {
            return message.TryGetConnectionId(out var connectionId) ? _connections[connectionId] : null;
        }

        private static async Task SendMessageAsync(HttpConnection httpConnection, HubConnectionContext hubConnection,
            HubMessage hubMessage)
        {
            var payload = hubConnection.ProtocolReaderWriter.WriteMessage(hubMessage);
            await httpConnection.SendAsync(payload);
        }

        Type IInvocationBinder.GetReturnType(string invocationId)
        {
            return _hubInvoker.GetReturnType(invocationId);
        }

        Type[] IInvocationBinder.GetParameterTypes(string methodName)
        {
            return _hubInvoker.GetParameterTypes(methodName);
        }

        #endregion
    }
}
