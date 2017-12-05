namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public interface IHubLifetimeManagerFactory
    {
        HubLifetimeManager<THub> Create<THub>(string hubName) where THub : Hub;
    }
}
