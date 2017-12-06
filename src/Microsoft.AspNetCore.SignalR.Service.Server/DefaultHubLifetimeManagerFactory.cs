// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
