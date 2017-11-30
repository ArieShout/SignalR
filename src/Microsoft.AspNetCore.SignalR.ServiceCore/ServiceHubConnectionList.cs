using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR.ServiceCore.API;
using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.SignalR.ServiceCore
{
    public class ServiceHubConnectionList : IReadOnlyCollection<ServiceHubConnectionContext>
    {
        private readonly ConcurrentDictionary<string, ServiceHubConnectionContext> _connections = new ConcurrentDictionary<string, ServiceHubConnectionContext>();

        public int Count => _connections.Count;

        public ServiceHubConnectionContext this[string connectionId]
        {
            get
            {
                _connections.TryGetValue(connectionId, out var connection);
                return connection;
            }
        }

        public void Add(ServiceHubConnectionContext connection)
        {
            _connections.TryAdd(connection.ConnectionId, connection);
        }

        public void Remove(ServiceHubConnectionContext connection)
        {
            _connections.TryRemove(connection.ConnectionId, out _);
        }

        public IEnumerator<ServiceHubConnectionContext> GetEnumerator()
        {
            return _connections.Values.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
