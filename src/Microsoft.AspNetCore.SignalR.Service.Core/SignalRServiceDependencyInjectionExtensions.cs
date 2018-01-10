// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Service.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRServiceDependencyInjectionExtensions
    {
        public static ISignalRServiceBuilder AddSignalRService(this IServiceCollection services)
        {
            return services.AddSignalRService(_ => { });
        }

        public static ISignalRServiceBuilder AddSignalRService(this IServiceCollection services, Action<ServiceHubOptions> configure)
        {
            services.Configure(configure);
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultServiceHubLifetimeManager<>));
            services.AddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
            services.AddSingleton(typeof(ServiceHubEndPoint<>), typeof(ServiceHubEndPoint<>));
            services.AddSingleton(typeof(IHubStatManager<>), typeof(ServiceHubStatManager<>));
            services.AddScoped(typeof(IHubActivator<>), typeof(DefaultHubActivator<>));
            services.AddSingleton(typeof(SignalRServiceAuthHelper));

            services.AddAuthorization();
            services.AddRouting();

            return new SignalRServiceBuilder(services);
        }
    }
}
