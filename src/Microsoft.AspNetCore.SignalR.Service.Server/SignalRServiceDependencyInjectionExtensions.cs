// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Core;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Service.Server;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRServiceDependencyInjectionExtensions
    {
        public static ISignalRBuilder AddSignalRServiceServer(this IServiceCollection services)
        {
            return AddSignalRServiceServer(services, _ => { });
        }

        public static ISignalRBuilder AddSignalRServiceServer(this IServiceCollection services, Action<HubOptions> configure)
        {
            services.Configure(configure);
            services.AddSockets2();
            return services.AddSignalRCore2();
        }

        public static IServiceCollection AddSockets2(this IServiceCollection services)
        {
            services.AddRouting();
            services.AddAuthentication();
            services.AddAuthorizationPolicyEvaluator();
            services.TryAddSingleton<HttpConnectionDispatcher>();
            return services.AddSocketsCore();
        }

        public static ISignalRBuilder AddSignalRCore2(this IServiceCollection services)
        {
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultHubLifetimeManager<>));
            services.AddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
            services.AddSingleton(typeof(IHubContext<,>), typeof(HubContext<,>));
            services.AddSingleton(typeof(IUserIdProvider), typeof(DefaultUserIdProvider));
            services.AddScoped(typeof(IHubActivator<>), typeof(DefaultHubActivator<>));

            services.AddSingleton(typeof(IHubProtocolResolver), typeof(ServiceHubProtocolResolver));
            services.AddSingleton(typeof(HubEndPoint<ClientHub>), typeof(ClientHubEndPoint<ClientHub>));
            services.AddSingleton(typeof(HubEndPoint<ServerHub>), typeof(ServerHubEndPoint<ServerHub>));
            services.AddSingleton(typeof(IHubLifetimeManagerFactory), typeof(DefaultHubLifetimeManagerFactory));
            services.AddSingleton(typeof(IHubMessageBroker), typeof(HubMessageBroker));
            services.AddSingleton(typeof(IHubConnectionRouter), typeof(HubConnectionRouter));

            services.AddAuthorization();

            return new SignalRBuilder(services);
        }

        public static ISignalRBuilder AddRedis2(this ISignalRBuilder builder)
        {
            return AddRedis2(builder, o => { });
        }

        public static ISignalRBuilder AddRedis2(this ISignalRBuilder builder, Action<RedisOptions2> configure)
        {
            builder.Services.Configure(configure);
            builder.Services.AddSingleton(typeof(IHubConnectionRouter), typeof(RedisHubConnectionRouter));
            builder.Services.AddSingleton(typeof(IHubLifetimeManagerFactory), typeof(RedisHubLifetimeManagerFactory));
            return builder;
        }
    }
}
