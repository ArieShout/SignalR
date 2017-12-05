using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task OnClientConnected(string hubName, HubConnectionContext connection)
        {
            if (connection.GetTargetConnectionId() != null) return;
            if (!_connectionStatus.TryGetValue(hubName, out var hubConnectionStatus)) return;
            var targetConnId = hubConnectionStatus.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            connection.AddTargetConnectionId(targetConnId);
            hubConnectionStatus.TryUpdate(targetConnId, c => c + 1);
            await Task.CompletedTask;
        }

        public async Task OnClientDisconnected(string hubName, HubConnectionContext connection)
        {
            var targetConnId = connection.GetTargetConnectionId();
            if (targetConnId == null) return;
            if (!_connectionStatus.TryGetValue(hubName, out var hubConnectionStatus)) return;
            hubConnectionStatus.TryUpdate(targetConnId, c => c > 0 ? c - 1 : 0);
            await Task.CompletedTask;
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
                hubConnectionStatus.TryRemove(connection.ConnectionId, out _);
            }
        }
    }
}
