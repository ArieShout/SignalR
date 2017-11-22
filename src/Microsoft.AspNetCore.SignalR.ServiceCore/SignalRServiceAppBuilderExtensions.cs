// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.AspNetCore.Builder
{
    public static class SignalRServiceAppBuilderExtensions
    {
        public static IApplicationBuilder UseSignalRService(this IApplicationBuilder app, String connStr, Action<NewHubRouteBuilder> configure)
        {
            app.UseSockets(routes =>
            {
                configure(new NewHubRouteBuilder(routes));
            });

            return app;
        }
    }
}
