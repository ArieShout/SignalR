// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.SignalR.Service
{
    public static class AppBuilderExtensions
    {
        public static IApplicationBuilder UseSignalRService(this IApplicationBuilder app, string connectionString, Action<ServiceClientBuilder> configure)
        {
            // Assign only once
            SignalR.ServiceProvider = app.ApplicationServices;

            var signalr = SignalR.Parse(connectionString);
            var builder = new ServiceClientBuilder(app.ApplicationServices, signalr);
            configure(builder);

            return app;
        }
    }
}
