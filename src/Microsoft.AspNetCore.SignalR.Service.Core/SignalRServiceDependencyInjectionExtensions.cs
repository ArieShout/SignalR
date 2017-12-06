// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.ServiceCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRServiceDependencyInjectionExtensions
    {
        public static ISignalRServiceBuilder AddSignalRService(this IServiceCollection services)
        {
            return services.AddSignalRServiceCore();
        }

        public static ISignalRServiceBuilder AddSignalRServiceCore(this IServiceCollection services)
        {
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultServiceHubLifetimeManager<>));
            services.AddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
            services.AddSingleton(typeof(ServiceHubEndPoint<>), typeof(ServiceHubEndPoint<>));
            services.AddScoped(typeof(IHubActivator<>), typeof(DefaultHubActivator<>));
            services.AddAuthorization();

            return new SignalRServiceBuilder(services);
        }
    }
}
