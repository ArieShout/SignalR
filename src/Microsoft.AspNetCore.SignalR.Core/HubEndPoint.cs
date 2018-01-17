// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Core;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubEndPoint<THub> where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<HubEndPoint<THub>> _logger;
        private readonly IHubProtocolResolver _protocolResolver;
        private readonly HubOptions _hubOptions;
        private readonly IUserIdProvider _userIdProvider;
        private readonly IHubInvoker<THub> _hubInvoker;

        public HubEndPoint(HubLifetimeManager<THub> lifetimeManager,
                           IHubProtocolResolver protocolResolver,
                           IOptions<HubOptions> hubOptions,
                           ILoggerFactory loggerFactory,
                           IUserIdProvider userIdProvider,
                           IHubInvoker<THub> hubInvoker)
        {
            _protocolResolver = protocolResolver;
            _lifetimeManager = lifetimeManager;
            _loggerFactory = loggerFactory;
            _hubOptions = hubOptions.Value;
            _logger = loggerFactory.CreateLogger<HubEndPoint<THub>>();
            _userIdProvider = userIdProvider;
            _hubInvoker = hubInvoker;
        }

        public async Task OnConnectedAsync(ConnectionContext connection)
        {
            var connectionContext = new HubConnectionContext(connection, _hubOptions.KeepAliveInterval, _loggerFactory);

            if (!await connectionContext.NegotiateAsync(_hubOptions.NegotiateTimeout, _protocolResolver, _userIdProvider))
            {
                return;
            }

            // We don't need to hold this task, it's also held internally and awaited by DisposeAsync.
            _ = connectionContext.StartAsync();

            try
            {
                await _lifetimeManager.OnConnectedAsync(connectionContext);
                await RunHubAsync(connectionContext);
            }
            finally
            {
                await _lifetimeManager.OnDisconnectedAsync(connectionContext);

                await connectionContext.DisposeAsync();
            }
        }

        private async Task RunHubAsync(HubConnectionContext connection)
        {
            await _hubInvoker.OnConnectedAsync(connection);

            try
            {
                await DispatchMessagesAsync(connection);
            }
            catch (Exception ex)
            {
                _logger.ErrorProcessingRequest(ex);
                await _hubInvoker.OnDisconnectedAsync(connection, ex);
                throw;
            }

            await _hubInvoker.OnDisconnectedAsync(connection, null);
        }

        private async Task DispatchMessagesAsync(HubConnectionContext connection)
        {
            // Since we dispatch multiple hub invocations in parallel, we need a way to communicate failure back to the main processing loop.
            // This is done by aborting the connection.

            try
            {
                while (await connection.Input.WaitToReadAsync(connection.ConnectionAbortedToken))
                {
                    while (connection.Input.TryRead(out var buffer))
                    {
                        if (connection.ProtocolReaderWriter.ReadMessages(buffer, _hubInvoker, out var hubMessages))
                        {
                            foreach (var hubMessage in hubMessages)
                            {
                                ProcessHubMessage(connection, hubMessage);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // If there's an exception, bubble it to the caller
                connection.AbortException?.Throw();
            }
        }

        private void ProcessHubMessage(HubConnectionContext connection, HubMessage hubMessage)
        {
            switch (hubMessage)
            {
                case InvocationMessage invocationMessage:
                    _logger.ReceivedHubInvocation(invocationMessage);

                    // Don't wait on the result of execution, continue processing other
                    // incoming messages on this connection.
                    _ = _hubInvoker.OnInvocationAsync(connection, invocationMessage, isStreamedInvocation: false);
                    break;

                case StreamInvocationMessage streamInvocationMessage:
                    _logger.ReceivedStreamHubInvocation(streamInvocationMessage);

                    // Don't wait on the result of execution, continue processing other
                    // incoming messages on this connection.
                    _ = _hubInvoker.OnInvocationAsync(connection, streamInvocationMessage, isStreamedInvocation: true);
                    break;

                case CancelInvocationMessage cancelInvocationMessage:
                    // Check if there is an associated active stream and cancel it if it exists.
                    // The cts will be removed when the streaming method completes executing
                    if (connection.ActiveRequestCancellationSources.TryGetValue(cancelInvocationMessage.InvocationId, out var cts))
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
                    _logger.UnsupportedMessageReceived(hubMessage.GetType().FullName);
                    throw new NotSupportedException($"Received unsupported message: {hubMessage}");
            }
        }
    }
}
