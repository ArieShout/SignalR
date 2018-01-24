// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceClientBuilder
    {
        private readonly SignalR _signalr;
        private readonly IServiceProvider _serviceProvider;

        public ServiceClientBuilder(IServiceProvider serviceProvider, SignalR signalr)
        {
            _serviceProvider = serviceProvider;
            _signalr = signalr;
        }

        public ServiceClient<THub> UseHub<THub>() where THub: Hub
        {
            var serviceClient = _serviceProvider.GetRequiredService<ServiceClient<THub>>();
            serviceClient.UseService(_signalr);
            return serviceClient;
        }
    }
}
