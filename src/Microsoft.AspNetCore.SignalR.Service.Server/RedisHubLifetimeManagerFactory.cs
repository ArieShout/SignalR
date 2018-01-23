// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public class RedisHubLifetimeManagerFactory : IHubLifetimeManagerFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptionsFactory<RedisOptions2> _redisOptionsFactory;
        private readonly SignalRServiceOptions _options;

        public RedisHubLifetimeManagerFactory(ILoggerFactory loggerFactory, IOptionsFactory<RedisOptions2> redisOptionsFactory, IOptions<SignalRServiceOptions> serviceOptions)
        {
            _loggerFactory = loggerFactory;
            _redisOptionsFactory = redisOptionsFactory;
            _options = serviceOptions.Value;
        }

        public HubLifetimeManager<THub> Create<THub>(string hubName) where THub : Hub
        {
            return new RedisHubLifetimeManager2<THub>(
                _loggerFactory.CreateLogger<RedisHubLifetimeManager2<THub>>(),
                _redisOptionsFactory.Create(string.Empty),
                _options.ServiceId, hubName);
        }
    }
}
