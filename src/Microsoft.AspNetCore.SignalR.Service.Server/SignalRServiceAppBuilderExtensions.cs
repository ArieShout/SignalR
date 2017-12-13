// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Service.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class SignalRServiceAppBuilderExtensions
    {
        public static IApplicationBuilder UseSignalRServiceServer(this IApplicationBuilder app)
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
