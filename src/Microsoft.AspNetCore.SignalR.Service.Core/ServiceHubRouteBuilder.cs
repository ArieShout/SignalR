// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
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
            var authorizeAttributes = typeof(THub).GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            var authorizeData = authorizeAttributes as IList<IAuthorizeData> ?? authorizeAttributes.ToList<IAuthorizeData>();
            var authHelper = _routes.ServiceProvider.GetRequiredService<SignalRServiceAuthHelper>();
            var statManager = _routes.ServiceProvider.GetRequiredService<IHubStatManager<THub>>();
            _routes.MapRoute(path, c => authHelper.GetServiceEndpoint<THub>(c, authorizeData, _config));
            _hubBuilder.BuildServiceHub<THub>(_config);
            _routes.MapRoute(path + "/stat", c => statManager.GetHubStat(c));
        }
    }
}
