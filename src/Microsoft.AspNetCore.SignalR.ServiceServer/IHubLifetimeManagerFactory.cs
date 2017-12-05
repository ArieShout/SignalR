using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.ServiceServer
{
    public interface IHubLifetimeManagerFactory
    {
        HubLifetimeManager<THub> Create<THub>(string hubName) where THub : Hub;
    }
}
