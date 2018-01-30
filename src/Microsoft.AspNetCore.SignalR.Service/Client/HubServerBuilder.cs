// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubServerBuilder
    {
        private readonly SignalR _signalr;
        private readonly IServiceProvider _serviceProvider;

        public HubServerBuilder(IServiceProvider serviceProvider, SignalR signalr)
        {
            _serviceProvider = serviceProvider;
            _signalr = signalr;
        }

        public HubServer<THub> UseHub<THub>() where THub: Hub
        {
            var hubServer = _serviceProvider.GetRequiredService<HubServer<THub>>();
            hubServer.UseService(_signalr);
            // Automatically start hub server
            _ = hubServer.StartAsync();
            return hubServer;
        }
    }
}
