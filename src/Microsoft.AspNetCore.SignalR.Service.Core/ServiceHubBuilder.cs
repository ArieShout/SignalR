// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class ServiceHubBuilder
    {
        public IServiceProvider ApplicationServices { get; }

        public ServiceHubBuilder(IServiceProvider applicationServices)
        {
            ApplicationServices = applicationServices;
        }

        public async void BuildServiceHub<THub>(SignalRServiceConfiguration config) where THub : Hub
        {
            var endPoint = ApplicationServices.GetRequiredService<ServiceHubEndpoint<THub>>();
            endPoint.UseHub(config);
            await endPoint.StartAsync();
        }
    }
}
