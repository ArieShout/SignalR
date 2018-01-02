// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceConnectionBuilder
    {
        public IServiceProvider ApplicationServices { get; }

        public ServiceConnectionBuilder(IServiceProvider applicationServices)
        {
            ApplicationServices = applicationServices;
        }

        public async void BuildServiceHub<THub>(ServiceCredential credential) where THub : Hub
        {
            var serviceConnection = ApplicationServices.GetRequiredService<ServiceConnection<THub>>();
            serviceConnection.UseHub(credential);
            await serviceConnection.StartAsync();
        }
    }
}
