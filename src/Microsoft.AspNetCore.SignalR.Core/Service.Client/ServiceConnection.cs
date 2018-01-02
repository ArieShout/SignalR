using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Core.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Service.Client
{
    public class ServiceConnection<THub> : BaseHubConnection where THub : Hub
    {
        private const string OnConnectedAsyncMethod = "onconnectedasync";
        private const string OnDisconnectedAsyncMethod = "ondisconnectedasync";

        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IHubInvoker<THub> _hubInvoker;
        private readonly HubConnectionList _connections = new HubConnectionList();

        private readonly Channel<HubMessage> _output;

        private Task _writeTask;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public ServiceConnection(IConnection connection,
            IHubProtocol protocol,
            HubLifetimeManager<THub> lifetimeManager,
            IHubInvoker<THub> hubInvoker,
            ILoggerFactory loggerFactory) :
            base(connection, protocol, loggerFactory)
        {
            _hubInvoker = hubInvoker;
            _lifetimeManager = lifetimeManager;
            _output = Channel.CreateUnbounded<HubMessage>();

            connection.OnReceived((data, state) => ((ServiceConnection<THub>)state).OnDataReceivedAsync(data), this);
        }

        public override async Task StartAsync()
        {
            await base.StartAsync();

            _writeTask = WriteToTransport();
        }

        public override async Task StopAsync()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            await base.StopAsync();
        }

        #region Private Methods

        private async Task WriteToTransport()
        {
            while (await _output.Reader.WaitToReadAsync())
            {
                while (_output.Reader.TryRead(out var message))
                {
                    await SendMessageAsync(message);
                }
            }
        }

        private async Task OnDataReceivedAsync(byte[] data)
        {
            if (ProtocolReaderWriter.ReadMessages(data, _hubInvoker, out var messages))
            {
                foreach (var message in messages)
                {
                    switch (message)
                    {
                        case InvocationMessage invocationMessage:
                            Logger.ReceivedHubInvocation(invocationMessage);

                            // Don't wait on the result of execution, continue processing other
                            // incoming messages on this connection.
                            _ = OnInvocationAsync(invocationMessage);
                            break;

                        case StreamInvocationMessage streamInvocationMessage:
                            Logger.ReceivedStreamHubInvocation(streamInvocationMessage);

                            // Don't wait on the result of execution, continue processing other
                            // incoming messages on this connection.
                            _ = OnInvocationAsync(streamInvocationMessage, isStreamedInvocation: true);
                            break;

                        case CancelInvocationMessage cancelInvocationMessage:
                            _ = OnCancelInvocationAsync(cancelInvocationMessage);
                            break;

                        case CompletionMessage completionMessage:
                            Logger.ReceivedCompletion(completionMessage);

                            _ = OnCompletionAsync(completionMessage);
                            break;

                        case PingMessage _:
                            // We don't care about pings
                            break;

                        // Other kind of message we weren't expecting
                        default:
                            Logger.UnsupportedMessageReceived(message.GetType().FullName);
                            throw new NotSupportedException($"Received unsupported message: {message}");
                    }
                }

                await Task.CompletedTask;
            }
        }

        private async Task OnInvocationAsync(HubMethodInvocationMessage message, bool isStreamedInvocation = false)
        {
            switch (message.Target.ToLower())
            {
                case OnConnectedAsyncMethod:
                    await OnConnectedAsync(message);
                    break;

                case OnDisconnectedAsyncMethod:
                    await OnDisconnectedAsync(message);
                    break;

                default:
                    await OnMethodInvocationAsync(message, isStreamedInvocation);
                    break;
            }
        }

        private async Task OnConnectedAsync(HubInvocationMessage message)
        {
            var connection = CreateHubConnectionContext(message);
            _connections.Add(connection);

            await _lifetimeManager.OnConnectedAsync(connection);

            await _hubInvoker.OnConnectedAsync(connection);

            await SendMessageAsync(CompletionMessage.WithResult(message.InvocationId, ""));
        }

        private async Task OnDisconnectedAsync(HubInvocationMessage message)
        {
            var connection = GetHubConnectionContext(message);
            if (connection == null)
            {
                await SendMessageAsync(CompletionMessage.WithError(message.InvocationId, "No connection found."));
                return;
            }

            await _hubInvoker.OnDisconnectedAsync(connection, null);

            await _lifetimeManager.OnDisconnectedAsync(connection);

            await SendMessageAsync(CompletionMessage.WithResult(message.InvocationId, ""));

            _connections.Remove(connection);
        }

        private async Task OnMethodInvocationAsync(HubMethodInvocationMessage message, bool isStreamedInvocation)
        {
            var connection = GetHubConnectionContext(message);
            if (connection == null)
            {
                await SendMessageAsync(CompletionMessage.WithError(message.InvocationId, "No connection found."));
                return;
            }

            await _hubInvoker.OnInvocationAsync(connection, message, isStreamedInvocation);
        }

        private async Task OnCancelInvocationAsync(CancelInvocationMessage message)
        {
            var connection = GetHubConnectionContext(message);
            if (connection == null)
            {
                await SendMessageAsync(CompletionMessage.WithError(message.InvocationId, "No connection found."));
                return;
            }

            // Check if there is an associated active stream and cancel it if it exists.
            // The cts will be removed when the streaming method completes executing
            if (connection.ActiveRequestCancellationSources.TryGetValue(message.InvocationId, out var cts))
            {
                Logger.CancelStream(message.InvocationId);
                cts.Cancel();
            }
            else
            {
                // Stream can be canceled on the server while client is canceling stream.
                Logger.UnexpectedCancel();
            }
        }

        private async Task OnCompletionAsync(CompletionMessage message)
        {
            var connection = GetHubConnectionContext(message);
            if (connection == null)
            {
                await SendMessageAsync(CompletionMessage.WithError(message.InvocationId, "No connection found."));
                return;
            }

            await _hubInvoker.OnCompletionAsync(connection, message);
        }

        private HubConnectionContext CreateHubConnectionContext(HubInvocationMessage message)
        {
            var context = CreateConnectionContext(message);
            // TODO: configurable KeepAliveInterval
            return new HubConnectionContext(context, TimeSpan.FromSeconds(30), LoggerFactory)
                {
                    Output = _output,
                    ProtocolReaderWriter = ProtocolReaderWriter
                };
        }

        private DefaultConnectionContext CreateConnectionContext(HubInvocationMessage message)
        {
            var connectionId = message.GetConnectionId();
            // TODO:
            // No channels for logical ConnectionContext. These channels won't be used in current context.
            // So no exception or error will be thrown.
            // We should have a cleaner approach to reuse DefaultConnectionContext for SignalR Service.
            var connectionContext = new DefaultConnectionContext(connectionId, null, null);
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

        private async Task SendMessageAsync(HubMessage hubMessage)
        {
            var payload = ProtocolReaderWriter.WriteMessage(hubMessage);
            // TODO:
            await Connection.SendAsync(payload, CancellationToken.None);
        }

        #endregion
    }
}
