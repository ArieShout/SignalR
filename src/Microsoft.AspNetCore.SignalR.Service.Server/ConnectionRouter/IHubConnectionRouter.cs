using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public interface IHubConnectionRouter
    {
        Task OnClientConnected(string hubName, HubConnectionContext connection);
        Task OnClientDisconnected(string hubName, HubConnectionContext connection);
        void OnServerConnected(string hubName, HubConnectionContext connection);
        void OnServerDisconnected(string hubName, HubConnectionContext connection);
    }
}
