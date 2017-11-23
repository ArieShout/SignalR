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
        public static ISignalRBuilder AddSignalRServiceServer(this IServiceCollection services)
        {
            return AddSignalRServiceServer(services, _ => { });
        }

        public static ISignalRBuilder AddSignalRServiceServer(this IServiceCollection services, Action<HubOptions> configure)
        {
            services.Configure(configure);
            services.AddSockets();
            return services.AddSignalRCore();
        }
    }
}
