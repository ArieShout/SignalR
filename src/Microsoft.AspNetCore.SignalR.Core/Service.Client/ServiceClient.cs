// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Service.Client;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceClient<THub> where THub : Hub
    {
        private readonly List<ServiceConnection<THub>> _serviceConnections = new List<ServiceConnection<THub>>();

        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IHubInvoker<THub> _hubInvoker;
        private readonly ServiceAuthHelper _authHelper;
        private readonly ServiceOptions _serviceOptions;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ServiceClient<THub>> _logger;

        public ServiceClient(HubLifetimeManager<THub> lifetimeManager,
            IHubInvoker<THub> hubInvoker,
            ServiceAuthHelper authHelper,
            IOptions<ServiceOptions> serviceOptions,
            ILoggerFactory loggerFactory)
        {
            _lifetimeManager = lifetimeManager;
            _hubInvoker = hubInvoker;
            _authHelper = authHelper;
            _serviceOptions = serviceOptions.Value;

            _loggerFactory = loggerFactory;
            //_loggerFactory.AddConsole(LogLevel.Trace);
            _logger = loggerFactory.CreateLogger<ServiceClient<THub>>();
        }

        public void UseService(ServiceCredential credential)
        {
            var serviceUrl = GetServiceUrl(credential);
            var httpOptions = new HttpOptions
            {
                JwtBearerTokenFactory = () => _authHelper.GetServerToken<THub>(credential)
            };

            for (var i = 0; i < _serviceOptions.ConnectionNumber; i++)
            {
                var serviceConnection = CreateServiceConnection(serviceUrl, httpOptions);
                _serviceConnections.Add(serviceConnection);
            }
        }

        public async Task StartAsync()
        {
            try
            {
                var tasks = _serviceConnections.Select(c => c.StartAsync());
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
            }
        }

        public async Task StopAsync()
        {
            try
            {
                var tasks = _serviceConnections.Select(c => c.StopAsync());
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
            }
        }

        #region Private Methods

        private Uri GetServiceUrl(ServiceCredential credential)
        {
            return new Uri(_authHelper.GetServerUrl<THub>(credential));
        }

        private ServiceConnection<THub> CreateServiceConnection(Uri serviceUrl, HttpOptions httpOptions)
        {
            var httpConnection =
                new HttpConnection(serviceUrl, TransportType.WebSockets, _loggerFactory, httpOptions);
            return new ServiceConnection<THub>(httpConnection, new JsonHubProtocol(), _lifetimeManager, _hubInvoker, _loggerFactory);
        }

        #endregion
    }
}
