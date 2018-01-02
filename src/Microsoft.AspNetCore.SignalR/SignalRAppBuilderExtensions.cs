// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class SignalRAppBuilderExtensions
    {
        public static IApplicationBuilder UseSignalR(this IApplicationBuilder app, Action<HubRouteBuilder> configure)
        {
            app.UseSockets(routes =>
            {
                configure(new HubRouteBuilder(routes));
            });

            return app;
        }

        public static IApplicationBuilder UseSignalRService(this IApplicationBuilder app, string connectionString,
            Action<ServiceRouteBuilder> config)
        {
            var routeBuilder = new RouteBuilder(app);
            var connectionBuilder = new ServiceClientBuilder(app.ApplicationServices);

            if (ServiceCredential.TryParse(connectionString, out var credential))
            {
                var hubRouteBuilder = new ServiceRouteBuilder(routeBuilder, connectionBuilder, credential);
                config(hubRouteBuilder);
            }
            else
            {
                throw new Exception($"Invalid SignalR Service connection string: {connectionString}");
            }

            app.UseRouter(routeBuilder.Build());
            return app;
        }

        public static IApplicationBuilder UseSignalRServer(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseSockets(routes =>
            {
                var hubRouteBuilder = new HubRouteBuilder(routes);
                hubRouteBuilder.MapHub<ClientHub>("client/{hubName}");
                hubRouteBuilder.MapHub<ServerHub>("server/{hubName}");
            });

            var routeBuilder = new RouteBuilder(app);
            var healthDataProvider = app.ApplicationServices.GetRequiredService<IHubStatusManager>();
            routeBuilder.MapRoute("health", healthDataProvider.GetHubStatus);
            app.UseRouter(routeBuilder.Build());
            return app;
        }
    }
}
