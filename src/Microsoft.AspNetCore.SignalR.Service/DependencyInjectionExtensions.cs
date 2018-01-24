// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Core;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRServiceDependencyInjectionExtensions
    {
        public static IServiceCollection AddSignalRService(this IServiceCollection services, Action<HubOptions> configureHub = null)
        {
            if (configureHub != null) services.Configure(configureHub);
            
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(ServiceHubLifetimeManager<>));
            services.AddSingleton(typeof(IHubProtocolResolver), typeof(DefaultHubProtocolResolver));
            services.AddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
            services.AddSingleton(typeof(IUserIdProvider), typeof(DefaultUserIdProvider));
            services.AddTransient(typeof(IHubActivator<>), typeof(DefaultHubActivator<>));

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, JsonHubProtocol>());
            //services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, MessagePackHubProtocol>());
            services.AddSingleton(typeof(ServiceClient<>));
            services.AddSingleton(typeof(IHubInvoker<>), typeof(ServiceHubInvoker<>));

            services.AddAuthorization();

            return services;
        }
    }
}
