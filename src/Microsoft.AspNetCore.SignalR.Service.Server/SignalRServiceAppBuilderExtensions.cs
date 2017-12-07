// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Service.Server;

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

            return app;
        }
    }
}
