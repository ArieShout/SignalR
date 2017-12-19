// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR.Core;
using Microsoft.AspNetCore.SignalR.Core.Internal;
using Microsoft.AspNetCore.SignalR.Features;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public abstract class BaseHubEndPoint<THub> : HubEndPoint<THub> where THub : Hub
    {
        private static readonly Base64Encoder Base64Encoder = new Base64Encoder();
        private static readonly PassThroughEncoder PassThroughEncoder = new PassThroughEncoder();

        private readonly ILogger<BaseHubEndPoint<THub>> _logger;
        private readonly IHubProtocolResolver _protocolResolver;
        private readonly HubOptions _hubOptions;
        private readonly IUserIdProvider _userIdProvider;

        protected BaseHubEndPoint(HubLifetimeManager<THub> lifetimeManager,
                           IHubProtocolResolver protocolResolver,
                           IHubContext<THub> hubContext,
                           IOptions<HubOptions> hubOptions,
                           ILogger<BaseHubEndPoint<THub>> logger,
                           IServiceScopeFactory serviceScopeFactory,
                           IUserIdProvider userIdProvider) : base(lifetimeManager, protocolResolver, hubContext, hubOptions, logger, serviceScopeFactory, userIdProvider)
        {
            _protocolResolver = protocolResolver;
            _hubOptions = hubOptions.Value;
            _logger = logger;
            _userIdProvider = userIdProvider;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var output = Channel.CreateUnbounded<HubMessage>();

            // Set the hub feature before doing anything else. This stores
            // all the relevant state for a SignalR Hub connection.
            connection.Features.Set<IHubFeature>(new HubFeature());

            var connectionContext = new ServiceHubConnectionContext(output, connection);

            if (!await ProcessNegotiate(connectionContext))
            {
                return;
            }

            connectionContext.UserIdentifier = _userIdProvider.GetUserId(connectionContext);

            // Hubs support multiple producers so we set up this loop to copy
            // data written to the HubConnectionContext's channel to the transport channel
            var protocolReaderWriter = connectionContext.ProtocolReaderWriter;
            async Task WriteToTransport()
            {
                try
                {
                    while (await output.Reader.WaitToReadAsync())
                    {
                        while (output.Reader.TryRead(out var hubMessage))
                        {
                            var buffer = protocolReaderWriter.WriteMessage(hubMessage);
                            while (await connection.Transport.Writer.WaitToWriteAsync())
                            {
                                if (connection.Transport.Writer.TryWrite(buffer))
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    connectionContext.Abort(ex);
                }
            }

            var writingOutputTask = WriteToTransport();

            try
            {
                var hubName = GetHubName(connectionContext) ??
                              throw new Exception($"No specified hub binded to connection: {connection.ConnectionId}");

                SetConnectionMetadata(connectionContext);

                await RunHubAsync(hubName, connectionContext);
            }
            finally
            {
                // Nothing should be writing to the HubConnectionContext
                output.Writer.TryComplete();

                // This should unwind once we complete the output
                await writingOutputTask;
            }
        }

        #region Private Methods

        private static string GetHubName(HubConnectionContext connection) =>
            connection.GetHttpContext()?.GetRouteValue("hubName")?.ToString();

        private static void SetConnectionMetadata(HubConnectionContext connection)
        {
            var context = connection.GetHttpContext();
            if (context == null) return;

            if (context.Request.Query.TryGetValue("uid", out var uid) &&
                !string.IsNullOrEmpty(uid))
            {
                connection.AddUid(uid);
            }
        }

        private async Task<bool> ProcessNegotiate(ServiceHubConnectionContext connection)
        {
            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(_hubOptions.NegotiateTimeout);
                    while (await connection.Input.WaitToReadAsync(cts.Token))
                    {
                        while (connection.Input.TryRead(out var buffer))
                        {
                            if (NegotiationProtocol.TryParseMessage(buffer, out var negotiationMessage))
                            {
                                var protocol = _protocolResolver.GetProtocol(negotiationMessage.Protocol, connection);

                                var transportCapabilities = connection.Features.Get<IConnectionTransportFeature>()?.TransportCapabilities
                                    ?? throw new InvalidOperationException("Unable to read transport capabilities.");

                                var dataEncoder = (protocol.Type == ProtocolType.Binary && (transportCapabilities & TransferMode.Binary) == 0)
                                    ? (IDataEncoder)Base64Encoder
                                    : PassThroughEncoder;

                                var transferModeFeature = connection.Features.Get<ITransferModeFeature>() ??
                                    throw new InvalidOperationException("Unable to read transfer mode.");

                                transferModeFeature.TransferMode =
                                    (protocol.Type == ProtocolType.Binary && (transportCapabilities & TransferMode.Binary) != 0)
                                        ? TransferMode.Binary
                                        : TransferMode.Text;

                                connection.ProtocolReaderWriter = new HubProtocolReaderWriter(protocol, dataEncoder);

                                _logger.UsingHubProtocol(protocol.Name);

                                return true;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.NegotiateCanceled();
            }

            return false;
        }

        private async Task RunHubAsync(string hubName, ServiceHubConnectionContext connection)
        {
            await OnHubConnectedAsync(hubName, connection);

            try
            {
                await DispatchMessagesAsync(hubName, connection);
            }
            catch (Exception ex)
            {
                _logger.ErrorProcessingRequest(ex);
                await OnHubDisconnectedAsync(hubName, connection, ex);
                throw;
            }

            await OnHubDisconnectedAsync(hubName, connection, null);
        }

        private async Task DispatchMessagesAsync(string hubName, ServiceHubConnectionContext connection)
        {
            // Since we dispatch multiple hub invocations in parallel, we need a way to communicate failure back to the main processing loop.
            // This is done by aborting the connection.

            try
            {
                while (await connection.Input.WaitToReadAsync(connection.ConnectionAbortedToken))
                {
                    while (connection.Input.TryRead(out var buffer))
                    {
                        if (!connection.ProtocolReaderWriter.ReadMessages(buffer, this, out var hubMessages)) continue;
                        foreach (var hubMessage in hubMessages)
                        {
                            switch (hubMessage)
                            {
                                case InvocationMessage invocationMessage:
                                    _logger.ReceivedHubInvocation(invocationMessage);

                                    // Don't wait on the result of execution, continue processing other
                                    // incoming messages on this connection.
                                    _ = ProcessInvocation(hubName, connection, invocationMessage, isStreamedInvocation: false);
                                    break;

                                case StreamInvocationMessage streamInvocationMessage:
                                    _logger.ReceivedStreamHubInvocation(streamInvocationMessage);

                                    // Don't wait on the result of execution, continue processing other
                                    // incoming messages on this connection.
                                    _ = ProcessInvocation(hubName, connection, streamInvocationMessage, isStreamedInvocation: true);
                                    break;

                                case CompletionMessage completionMessage:
                                    _logger.ReceivedCompletion(completionMessage);

                                    _ = ProcessCompletion(hubName, connection, completionMessage);
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
            }
            catch (OperationCanceledException)
            {
                // If there's an exception, bubble it to the caller
                connection.AbortException?.Throw();
            }
        }

        private async Task ProcessInvocation(string hubName, ServiceHubConnectionContext connection,
            HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamedInvocation)
        {
            try
            {

                await OnHubInvocationAsync(hubName, connection, hubMethodInvocationMessage);
            }
            catch (Exception ex)
            {
                // Abort the entire connection if the invocation fails in an unexpected way
                connection.Abort(ex);
            }
        }

        private async Task ProcessCompletion(String hubName, ServiceHubConnectionContext connection, CompletionMessage completionMessage)
        {
            try
            {
                await OnHubCompletionAsync(hubName, connection, completionMessage);
            }
            catch (Exception ex)
            {
                // Abort the entire connection if the invocation fails in an unexpected way
                connection.Abort(ex);
            }
        }

        #endregion

        #region Abstract Methods

        protected abstract Task OnHubConnectedAsync(string hubName, HubConnectionContext connection);

        protected abstract Task OnHubDisconnectedAsync(string hubName, HubConnectionContext connection, Exception exception);

        protected abstract Task OnHubInvocationAsync(string hubName, HubConnectionContext connection, HubMethodInvocationMessage message);

        protected abstract Task OnHubCompletionAsync(string hubName, HubConnectionContext connection, CompletionMessage message);

        #endregion
    }
}
