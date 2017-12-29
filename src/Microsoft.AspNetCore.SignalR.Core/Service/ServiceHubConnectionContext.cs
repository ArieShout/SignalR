using System;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceHubConnectionContext : HubConnectionContext
    {
        private readonly HttpConnection _httpConnection;
        private readonly ConnectionContext _connectionContext;
        private string _connectionId = string.Empty;

        public ServiceHubConnectionContext(HttpConnection httpConnection, ConnectionContext connectionContext, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory) : base(connectionContext, keepAliveInterval, loggerFactory)
        {
            _httpConnection = httpConnection;
            _connectionContext = connectionContext;
            _connectionId = connectionContext.ConnectionId;
        }

        public override string ConnectionId => _connectionId;

        public void SetConnectionId(string connectionId)
        {
            _connectionId = connectionId;
        }
    }
}
