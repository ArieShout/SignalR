// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class ServiceHubRouteBuilder
    {
        private readonly RouteBuilder _routes;
        private readonly ServiceHubBuilder _hubBuilder;
        private readonly SignalRServiceConfiguration _config;

        public ServiceHubRouteBuilder(RouteBuilder routes, ServiceHubBuilder hubBuilder, SignalRServiceConfiguration config)
        {
            _routes = routes;
            _hubBuilder = hubBuilder;
            _config = config;
        }

        public void MapHub<THub>(string path) where THub : Hub
        {
            var authHelper = _routes.ServiceProvider.GetRequiredService<SignalRServiceAuthHelper>();
            _routes.MapRoute(path, c => authHelper.GetServiceEndpoint<THub>(c, _config));
            _hubBuilder.BuildServiceHub<THub>(_config);
        }
    }
}
