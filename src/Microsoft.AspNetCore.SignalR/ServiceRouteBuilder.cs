// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceRouteBuilder
    {
        private readonly RouteBuilder _routes;
        private readonly ServiceConnectionBuilder _builder;
        private readonly ServiceCredential _credential;

        public ServiceRouteBuilder(RouteBuilder routes, ServiceConnectionBuilder builder, ServiceCredential credential)
        {
            _routes = routes;
            _builder = builder;
            _credential = credential;
        }

        public void MapHub<THub>(string path) where THub : Hub
        {
            var authorizeAttributes = typeof(THub).GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            var authorizeData = authorizeAttributes as IList<IAuthorizeData> ?? authorizeAttributes.ToList<IAuthorizeData>();
            var authHelper = _routes.ServiceProvider.GetRequiredService<ServiceAuthHelper>();

            _routes.MapRoute(path, c => authHelper.GetServiceEndpoint<THub>(c, authorizeData, _credential));
            _builder.BuildServiceHub<THub>(_credential);
        }
    }
}
