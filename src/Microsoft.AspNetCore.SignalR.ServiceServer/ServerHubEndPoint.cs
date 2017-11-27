// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR.Core;
using Microsoft.AspNetCore.SignalR.Core.Internal;
using Microsoft.AspNetCore.SignalR.Features;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServerHubEndPoint<THub> : HubEndPoint<THub>, IInvocationBinder where THub : Hub
    {
        private static readonly Base64Encoder Base64Encoder = new Base64Encoder();
        private static readonly PassThroughEncoder PassThroughEncoder = new PassThroughEncoder();

        private readonly Dictionary<string, HubMethodDescriptor> _methods = new Dictionary<string, HubMethodDescriptor>(StringComparer.OrdinalIgnoreCase);

        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IHubContext<THub> _hubContext;
        private readonly ILogger<ServerHubEndPoint<THub>> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHubProtocolResolver _protocolResolver;
        private readonly HubOptions _hubOptions;
        private readonly IUserIdProvider _userIdProvider;
        private readonly HubMessageBroker _hubMessageBroker;

        public ServerHubEndPoint(HubLifetimeManager<THub> lifetimeManager,
                           IHubProtocolResolver protocolResolver,
                           IHubContext<THub> hubContext,
                           IOptions<HubOptions> hubOptions,
                           ILogger<ServerHubEndPoint<THub>> logger,
                           IServiceScopeFactory serviceScopeFactory,
                           IUserIdProvider userIdProvider,
                           HubMessageBroker hubMessageBroker) : base(lifetimeManager, protocolResolver, hubContext, hubOptions, logger, serviceScopeFactory, userIdProvider)
        {
            _protocolResolver = protocolResolver;
            _lifetimeManager = lifetimeManager;
            _hubContext = hubContext;
            _hubOptions = hubOptions.Value;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _userIdProvider = userIdProvider;
            _hubMessageBroker = hubMessageBroker;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var output = Channel.CreateUnbounded<HubMessage>();

            // Set the hub feature before doing anything else. This stores
            // all the relevant state for a SignalR Hub connection.
            connection.Features.Set<IHubFeature>(new HubFeature());

            var connectionContext = new HubConnectionContext2(output, connection);

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
                var hubName = GetHubName(connectionContext);
                //await _lifetimeManager.OnConnectedAsync(connectionContext);
                await RunHubAsync(hubName, connectionContext);
            }
            finally
            {
                //await _lifetimeManager.OnDisconnectedAsync(connectionContext);

                // Nothing should be writing to the HubConnectionContext
                output.Writer.TryComplete();

                // This should unwind once we complete the output
                await writingOutputTask;
            }
        }

        private string GetHubName(HubConnectionContext2 connection)
        {
            if (connection.Metadata.ContainsKey("HubName"))
            {
                return connection.Metadata["HubName"].ToString();
            }
            throw new Exception($"No specified hub binded to connection: {connection.ConnectionId}");
        }

        private async Task<bool> ProcessNegotiate(HubConnectionContext2 connection)
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

        private async Task RunHubAsync(String hubName, HubConnectionContext2 connection)
        {
            await HubOnConnectedAsync(hubName, connection);

            try
            {
                await DispatchMessagesAsync(hubName, connection);
            }
            catch (Exception ex)
            {
                _logger.ErrorProcessingRequest(ex);
                await HubOnDisconnectedAsync(hubName, connection, ex);
                throw;
            }

            await HubOnDisconnectedAsync(hubName, connection, null);
        }

        private async Task HubOnConnectedAsync(String hubName, HubConnectionContext2 connection)
        {
            await _hubMessageBroker.OnServerConnectedAsync(hubName, connection);
        }

        private async Task HubOnDisconnectedAsync(String hubName, HubConnectionContext2 connection, Exception exception)
        {
            await _hubMessageBroker.OnServerDisconnectedAsync(hubName, connection);
        }

        private async Task DispatchMessagesAsync(String hubName, HubConnectionContext2 connection)
        {
            // Since we dispatch multiple hub invocations in parallel, we need a way to communicate failure back to the main processing loop.
            // This is done by aborting the connection.

            try
            {
                while (await connection.Input.WaitToReadAsync(connection.ConnectionAbortedToken))
                {
                    while (connection.Input.TryRead(out var buffer))
                    {
                        if (connection.ProtocolReaderWriter.ReadMessages(buffer, this, out var hubMessages))
                        {
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
            }
            catch (OperationCanceledException)
            {
                // If there's an exception, bubble it to the caller
                connection.AbortException?.Throw();
            }
        }

        private async Task ProcessInvocation(String hubName, HubConnectionContext2 connection,
            HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamedInvocation)
        {
            try
            {
                await _hubMessageBroker.PassThruServerMessage(hubName, connection, hubMethodInvocationMessage);
            }
            catch (Exception ex)
            {
                // Abort the entire connection if the invocation fails in an unexpected way
                connection.Abort(ex);
            }
        }

        private async Task ProcessCompletion(String hubName, HubConnectionContext2 connection, CompletionMessage completionMessage)
        {
            try
            {
                await _hubMessageBroker.PassThruServerMessage(hubName, connection, completionMessage);
            }
            catch (Exception ex)
            {
                // Abort the entire connection if the invocation fails in an unexpected way
                connection.Abort(ex);
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

        // REVIEW: We can decide to move this out of here if we want pluggable hub discovery
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
}
