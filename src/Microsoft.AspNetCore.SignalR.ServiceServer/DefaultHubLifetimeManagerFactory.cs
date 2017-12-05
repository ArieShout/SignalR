using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.ServiceServer
{
    public class DefaultHubLifetimeManagerFactory : IHubLifetimeManagerFactory
    {
        public HubLifetimeManager<THub> Create<THub>(string hubName) where THub : Hub
        {
            return new DefaultHubLifetimeManager<THub>();
        }
    }
}
