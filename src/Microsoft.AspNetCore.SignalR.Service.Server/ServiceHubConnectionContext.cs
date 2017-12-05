// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public class ServiceHubConnectionContext : HubConnectionContext
    {
        private readonly ConnectionContext _connectionContext;
        private readonly TaskCompletionSource<object> _abortCompletedTcs = new TaskCompletionSource<object>();

        public ServiceHubConnectionContext(ChannelWriter<HubMessage> output, ConnectionContext connectionContext) : base(output, connectionContext)
        {
            _connectionContext = connectionContext;
        }

        // Used by the HubEndPoint only
        internal ChannelReader<byte[]> Input => _connectionContext.Transport;

        internal ExceptionDispatchInfo AbortException { get; private set; }

        // Currently used only for streaming methods
        internal ConcurrentDictionary<string, CancellationTokenSource> ActiveRequestCancellationSources { get; } = new ConcurrentDictionary<string, CancellationTokenSource>();

        public new string UserIdentifier { get; internal set; }

        internal void Abort(Exception exception)
        {
            AbortException = ExceptionDispatchInfo.Capture(exception);
            Abort();
        }

        // Used by the HubEndPoint only
        internal Task AbortAsync()
        {
            Abort();
            return _abortCompletedTcs.Task;
        }
    }
}
