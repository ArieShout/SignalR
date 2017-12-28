using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceHubConnectionContext : HubConnectionContext
    {
        private readonly ConnectionContext _connectionContext;
        private readonly TaskCompletionSource<object> _abortCompletedTcs = new TaskCompletionSource<object>();

        public ServiceHubConnectionContext(ConnectionContext connectionContext, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory) : base(connectionContext, keepAliveInterval, loggerFactory)
        {
            _connectionContext = connectionContext;
        }
    }
}
