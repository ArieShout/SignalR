// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Sockets
{
    public class SocketRouteBuilder2 : SocketRouteBuilder
    {
        private readonly HttpConnectionDispatcher _dispatcher;
        private readonly RouteBuilder _routes;

        public SocketRouteBuilder2(RouteBuilder routes, HttpConnectionDispatcher dispatcher) : base(routes, dispatcher)
        {
            _routes = routes;
            _dispatcher = dispatcher;
        }

        public new void MapSocket(string path, HttpSocketOptions options, Action<ISocketBuilder> socketConfig)
        {
            var socketBuilder = new SocketBuilder(_routes.ServiceProvider);
            socketConfig(socketBuilder);
            var socket = socketBuilder.Build();
            _routes.MapRoute(path + "/{hubName}", c => _dispatcher.ExecuteAsync(c, options, socket));
            _routes.MapRoute(path + "/{hubName}/negotiate", c => _dispatcher.ExecuteNegotiateAsync(c, options));
        }
    }
}
