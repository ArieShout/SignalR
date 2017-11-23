// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Core;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRServiceDependencyInjectionExtensions
    {
        public static ISignalRBuilder AddSignalRService(this IServiceCollection services)
        {
            return AddSignalRService(services, _ => { });
        }

        public static ISignalRBuilder AddSignalRService(this IServiceCollection services, Action<HubOptions> configure)
        {
            services.Configure(configure);
            services.AddNewSockets();
            return services.AddSignalRServiceCore();
        }

        public static IServiceCollection AddNewSockets(this IServiceCollection services)
        {
            services.AddRouting();
            services.AddAuthorizationPolicyEvaluator();
            services.TryAddSingleton<NewHttpConnectionDispatcher>();
            return services.AddNewSocketsCore();
        }

        public static IServiceCollection AddNewSocketsCore(this IServiceCollection services)
        {
            services.TryAddSingleton<NewConnectionManager>();
            return services;
        }

        public static ISignalRBuilder AddSignalRServiceCore(this IServiceCollection services)
        {
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultHubLifetimeManager<>));
            services.AddSingleton(typeof(IHubProtocolResolver), typeof(DefaultHubProtocolResolver));
            services.AddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
            services.AddSingleton(typeof(IHubContext<,>), typeof(HubContext<,>));
            services.AddSingleton(typeof(NewHubEndPoint<>), typeof(NewHubEndPoint<>));
            services.AddSingleton(typeof(IUserIdProvider), typeof(DefaultUserIdProvider));
            services.AddScoped(typeof(IHubActivator<>), typeof(DefaultHubActivator<>));

            services.AddAuthorization();

            return new SignalRBuilder(services);
        }
    }
}
