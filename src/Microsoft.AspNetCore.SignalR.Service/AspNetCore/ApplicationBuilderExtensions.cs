// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using static Microsoft.AspNetCore.SignalR.SignalR;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSignalRService(this IApplicationBuilder app, string connectionString, Action<ServiceClientBuilder> configure)
        {
            // Assign only once
            ServiceProvider = app.ApplicationServices;

            var signalr = Parse(connectionString);
            var builder = new ServiceClientBuilder(app.ApplicationServices, signalr);
            configure(builder);

            return app;
        }
    }
}
