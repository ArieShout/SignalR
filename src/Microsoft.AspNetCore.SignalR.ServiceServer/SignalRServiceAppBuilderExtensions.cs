// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class SignalRServiceAppBuilderExtensions
    {
        public static IApplicationBuilder UseSignalRServiceServer(this IApplicationBuilder app)
        {
            app.UseSockets2(routes =>
            {
                var hubRouteBuilder = new HubRouteBuilder(routes);
                hubRouteBuilder.MapHub<ClientHub>("client");
                hubRouteBuilder.MapHub<ServerHub>("server");
            });

            return app;
        }

        public static IApplicationBuilder UseSockets2(this IApplicationBuilder app, Action<SocketRouteBuilder2> callback)
        {
            var dispatcher = app.ApplicationServices.GetRequiredService<HttpConnectionDispatcher>();

            var routes = new RouteBuilder(app);

            callback(new SocketRouteBuilder2(routes, dispatcher));

            app.UseWebSockets();
            app.UseRouter(routes.Build());
            return app;
        }
    }
}
