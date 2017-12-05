namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public class DefaultHubLifetimeManagerFactory : IHubLifetimeManagerFactory
    {
        public HubLifetimeManager<THub> Create<THub>(string hubName) where THub : Hub
        {
            return new DefaultHubLifetimeManager<THub>();
        }
    }
}
