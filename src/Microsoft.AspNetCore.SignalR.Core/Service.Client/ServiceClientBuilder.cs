// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceClientBuilder
    {
        public IServiceProvider ApplicationServices { get; }

        public ServiceClientBuilder(IServiceProvider applicationServices)
        {
            ApplicationServices = applicationServices;
        }

        public async void BuildServiceClient<THub>(ServiceCredential credential) where THub : Hub
        {
            var serviceConnection = ApplicationServices.GetRequiredService<ServiceClient<THub>>();
            serviceConnection.UseService(credential);
            await serviceConnection.StartAsync();
        }
    }
}
