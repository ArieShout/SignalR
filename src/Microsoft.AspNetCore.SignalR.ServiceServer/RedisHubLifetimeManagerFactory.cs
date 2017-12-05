using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.ServiceServer
{
    public class RedisHubLifetimeManagerFactory : IHubLifetimeManagerFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptionsFactory<RedisOptions2> _redisOptionsFactory;

        public RedisHubLifetimeManagerFactory(ILoggerFactory loggerFactory, IOptionsFactory<RedisOptions2> redisOptionsFactory)
        {
            _loggerFactory = loggerFactory;
            _redisOptionsFactory = redisOptionsFactory;
        }

        public HubLifetimeManager<THub> Create<THub>(string hubName) where THub : Hub
        {
            return new RedisHubLifetimeManager2<THub>(
                _loggerFactory.CreateLogger<RedisHubLifetimeManager2<THub>>(),
                _redisOptionsFactory.Create(string.Empty),
                hubName);
        }
    }
}
