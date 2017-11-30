using System.Collections.Concurrent;
using System.Linq;

namespace Microsoft.AspNetCore.SignalR.ServiceServer
{
    public class HubConnectionRouter : IHubConnectionRouter
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _connectionStatus = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>(); 

        // TODO: inject dependency of config provider, so that we can change routing algorithm without restarting service
        public HubConnectionRouter()
        {
        }

        // TODO: Using least connection routing right now. Should support multiple routing method in the future.
        public void OnClientConnected(string hubName, HubConnectionContext connection)
        {
            if (connection.Metadata.ContainsKey("TargetConnId")) return;
            if (!_connectionStatus.TryGetValue(hubName, out var hubConnectionStatus)) return;
            var targetConnection = hubConnectionStatus.Aggregate((l, r) => l.Value < r.Value ? l : r);
            connection.Metadata.Add("TargetConnId", targetConnection.Key);
            // TODO: It is possible that below update will fail because of high volume of concurrent connections.
            //       Need a more robust way to update connection count.
            hubConnectionStatus.TryUpdate(targetConnection.Key, targetConnection.Value + 1, targetConnection.Value);
        }

        public void OnClientDisconnected(string hubName, HubConnectionContext connection)
        {
            if (!connection.Metadata.TryGetValue("TargetConnId", out var targetConnId)) return;
            if (!_connectionStatus.TryGetValue(hubName, out var hubConnectionStatus)) return;
            if (hubConnectionStatus.TryGetValue((string) targetConnId, out var connCnt))
            {
                // TODO: It is possible that below update will fail because of high volume of concurrent connections.
                //       Need a more robust way to update connection count.
                hubConnectionStatus.TryUpdate((string) targetConnId, connCnt > 0 ? connCnt - 1 : 0, connCnt);
            }
        }

        public void OnServerConnected(string hubName, HubConnectionContext connection)
        {
            var hubConnectionStatus = _connectionStatus.GetOrAdd(hubName, new ConcurrentDictionary<string, int>());
            hubConnectionStatus.TryAdd(connection.ConnectionId, 0);
        }

        public void OnServerDisconnected(string hubName, HubConnectionContext connection)
        {
            if (_connectionStatus.TryGetValue(hubName, out var hubConnectionStatus))
            {
                hubConnectionStatus.TryRemove(connection.ConnectionId, out var value);
            }
        }
    }
}
