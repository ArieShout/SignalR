// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.ServiceCore.API;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.ServiceCore
{
    public class ServiceHubBuilder
    {
        public IServiceProvider ApplicationServices { get; }
        public ServiceHubBuilder(IServiceProvider applicationServices)
        {
            ApplicationServices = applicationServices;
        }
        public async void BuildServiceHub<THub>(string path) where THub : ServiceHub
        {
            ServiceHubEndPoint<THub> endPoint = this.ApplicationServices.GetRequiredService<ServiceHubEndPoint<THub>>();
            endPoint.UseHub(path);
            await endPoint.StartAsync();
        }
    }
}
