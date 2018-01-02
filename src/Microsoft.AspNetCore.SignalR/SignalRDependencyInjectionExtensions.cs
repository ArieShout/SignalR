// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRDependencyInjectionExtensions
    {
        public static ISignalRBuilder AddSignalR(this IServiceCollection services)
        {
            return AddSignalR(services, _ => { });
        }

        public static ISignalRBuilder AddSignalR(this IServiceCollection services, Action<HubOptions> configure)
        {
            services.Configure(configure);
            services.AddSockets();
            return services.AddSignalRCore()
                .AddJsonProtocol();
        }

        public static ISignalRServiceBuilder AddSignalRService(this IServiceCollection services)
        {
            return AddSignalRService(services, _ => { });
        }

        public static ISignalRServiceBuilder AddSignalRService(this IServiceCollection services,
            Action<ServiceOptions> configure)
        {
            services.Configure(configure);
            services.AddRouting();

            return services.AddSignalRServiceCore();
        }

        public static ISignalRBuilder AddSignalRServer(this IServiceCollection services)
        {
            return AddSignalRServer(services, _ => { });
        }

        public static ISignalRBuilder AddSignalRServer(this IServiceCollection services,
            Action<ServerOptions> configure)
        {
            services.Configure(configure);
            services.AddRouting();

            return services.AddSignalRServerCore();
        }
    }
}
