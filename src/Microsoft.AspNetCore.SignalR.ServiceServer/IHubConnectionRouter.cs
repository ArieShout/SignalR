namespace Microsoft.AspNetCore.SignalR.ServiceServer
{
    public interface IHubConnectionRouter
    {
        void OnClientConnected(string hubName, HubConnectionContext connection);
        void OnClientDisconnected(string hubName, HubConnectionContext connection);
        void OnServerConnected(string hubName, HubConnectionContext connection);
        void OnServerDisconnected(string hubName, HubConnectionContext connection);
    }
}
