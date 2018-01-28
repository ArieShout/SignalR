// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Owin;

namespace Microsoft.AspNetCore.SignalR.Owin
{
    public static class AppBuilderExtensions
    {
        public static IAppBuilder UseSignalRService(this IAppBuilder app, string connectionString,
            Action<ServiceClientBuilder> configure)
        {
            var signalr = SignalR.Parse(connectionString);
            var builder = new ServiceClientBuilder(SignalR.ServiceProvider, signalr);
            configure(builder);

            return app;
        }
    }
}
