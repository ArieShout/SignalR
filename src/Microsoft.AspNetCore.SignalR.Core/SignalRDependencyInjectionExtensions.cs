// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Core;
using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRDependencyInjectionExtensions
    {
        public static ISignalRBuilder AddSignalRCore(this IServiceCollection services)
        {
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultHubLifetimeManager<>));
            services.AddSingleton(typeof(IHubProtocolResolver), typeof(DefaultHubProtocolResolver));
            services.AddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
            services.AddSingleton(typeof(IHubContext<,>), typeof(HubContext<,>));
            services.AddSingleton(typeof(HubEndPoint<>), typeof(HubEndPoint<>));
            services.AddSingleton(typeof(IUserIdProvider), typeof(DefaultUserIdProvider));
            services.AddScoped(typeof(IHubActivator<>), typeof(DefaultHubActivator<>));

            services.AddSingleton(typeof(IHubInvoker<>), typeof(HubInvoker<>));

            services.AddAuthorization();

            return new SignalRBuilder(services);
        }

        public static ISignalRServiceBuilder AddSignalRServiceCore(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IHubProtocolResolver), typeof(DefaultHubProtocolResolver));
            services.AddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
            services.AddSingleton(typeof(IUserIdProvider), typeof(DefaultUserIdProvider));
            services.AddScoped(typeof(IHubActivator<>), typeof(DefaultHubActivator<>));

            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultServiceHubLifetimeManager<>));
            services.AddSingleton(typeof(ServiceConnection<>), typeof(ServiceConnection<>));
            services.AddSingleton(typeof(ServiceAuthHelper));
            services.AddSingleton(typeof(IHubInvoker<>), typeof(HubInvoker<>));

            services.AddAuthorization();

            return new SignalRServiceBuilder(services);
        }
    }
}
