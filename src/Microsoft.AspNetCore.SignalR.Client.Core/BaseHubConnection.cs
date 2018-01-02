// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Features;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public abstract class BaseHubConnection
    {
        protected readonly IConnection Connection;
        protected bool StartCalled = false;
        protected IHubProtocol Protocol;
        protected bool NeedKeepAlive;
        protected HubProtocolReaderWriter ProtocolReaderWriter;
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly ILogger Logger;
        protected CancellationTokenSource ConnectionActive;

        protected BaseHubConnection(IConnection connection, IHubProtocol protocol, ILoggerFactory loggerFactory)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            Logger = LoggerFactory.CreateLogger<BaseHubConnection>();
        }

        public virtual async Task StartAsync()
        {
            try
            {
                await StartAsyncCore().ForceAsync();
            }
            finally
            {
                StartCalled = true;
            }
        }

        protected async Task StartAsyncCore()
        {
            var transferModeFeature = Connection.Features.Get<ITransferModeFeature>();
            if (transferModeFeature == null)
            {
                transferModeFeature = new TransferModeFeature();
                Connection.Features.Set(transferModeFeature);
            }

            var requestedTransferMode =
                Protocol.Type == ProtocolType.Binary
                    ? TransferMode.Binary
                    : TransferMode.Text;

            transferModeFeature.TransferMode = requestedTransferMode;
            await Connection.StartAsync();
            NeedKeepAlive = Connection.Features.Get<IConnectionInherentKeepAliveFeature>() == null;

            var actualTransferMode = transferModeFeature.TransferMode;

            ProtocolReaderWriter = new HubProtocolReaderWriter(Protocol, GetDataEncoder(requestedTransferMode, actualTransferMode));

            Logger.HubProtocol(Protocol.Name);

            ConnectionActive = new CancellationTokenSource();
            using (var memoryStream = new MemoryStream())
            {
                NegotiationProtocol.WriteMessage(new NegotiationMessage(Protocol.Name), memoryStream);
                await Connection.SendAsync(memoryStream.ToArray(), ConnectionActive.Token);
            }
        }

        public virtual async Task StopAsync() => await StopAsyncCore().ForceAsync();

        protected Task StopAsyncCore() => Connection.StopAsync();

        public virtual async Task DisposeAsync() => await DisposeAsyncCore().ForceAsync();

        protected async Task DisposeAsyncCore()
        {
            await Connection.DisposeAsync();
        }

        private IDataEncoder GetDataEncoder(TransferMode requestedTransferMode, TransferMode actualTransferMode)
        {
            if (requestedTransferMode == TransferMode.Binary && actualTransferMode == TransferMode.Text)
            {
                // This is for instance for SSE which is a Text protocol and the user wants to use a binary
                // protocol so we need to encode messages.
                return new Base64Encoder();
            }

            Debug.Assert(requestedTransferMode == actualTransferMode, "All transports besides SSE are expected to support binary mode.");

            return new PassThroughEncoder();
        }

        protected class TransferModeFeature : ITransferModeFeature
        {
            public TransferMode TransferMode { get; set; }
        }
    }
}
