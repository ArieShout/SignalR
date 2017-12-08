// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR.Service.Core;

namespace Microsoft.AspNetCore.Builder
{
    public static class SignalRServiceAppBuilderExtensions
    {
        // TODO: support connecting to multiple SignalR Services
        public static IApplicationBuilder UseSignalRService(this IApplicationBuilder app, string connectionString, Action<ServiceHubRouteBuilder> config)
        {
            var routeBuilder = new RouteBuilder(app);
            var hubBuilder = new ServiceHubBuilder(app.ApplicationServices);

            if (SignalRServiceConfiguration.TryParse(connectionString, out var serviceConfig))
            {
                var hubRouteBuilder = new ServiceHubRouteBuilder(routeBuilder, hubBuilder, serviceConfig);
                config(hubRouteBuilder);
            }
            else
            {
                throw new Exception($"Invalid SignalR Service connection string: {connectionString}");
            }

            app.UseRouter(routeBuilder.Build());
            return app;
        }
    }
}
