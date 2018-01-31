// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.SignalR
{
    public static class AppBuilderExtensions
    {
        public static IApplicationBuilder UseSignalRServer(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            return app.UseSignalR(routes =>
            {
                routes.MapHub<ClientHub>("/client");
                routes.MapHub<ServerHub>("/server");
            });
        }
    }
}