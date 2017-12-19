using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public class DefaultRoutingCache : IRoutingCache
    {
        // TODO: enable user to configure retension period
        private static readonly DistributedCacheEntryOptions ExpireIn30Min = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = new TimeSpan(0, 30, 0)
        };

        private readonly IDistributedCache _cache;

        private readonly SignalRServiceOptions _options;

        public DefaultRoutingCache(IDistributedCache cache, IOptions<SignalRServiceOptions> options)
        {
            _cache = cache;
            _options = options.Value;
        }

        public bool TryGetTarget(HubConnectionContext connection, out RouteTarget target)
        {
            var targetValue = _options.EnableStickySession && connection.TryGetUid(out var uid)
                ? _cache.GetString(uid)
                : null;
            target = RouteTarget.FromString(targetValue);
            return target != null;
        }

        public async Task SetTargetAsync(HubConnectionContext connection, RouteTarget target)
        {
            if (_options.EnableStickySession && connection.TryGetUid(out var uid))
            {
                await _cache.SetStringAsync(uid, target.ToString());
            }
        }

        public async Task RemoveTargetAsync(HubConnectionContext connection)
        {
            if (_options.EnableStickySession && connection.TryGetUid(out var uid))
            {
                await _cache.RemoveAsync(uid);
            }
        }

        public async Task DelayRemoveTargetAsync(HubConnectionContext connection, RouteTarget target)
        {
            if (_options.EnableStickySession && connection.TryGetUid(out var uid))
            {
                await _cache.SetStringAsync(uid, target.ToString(), ExpireIn30Min);
            }
        }
    }
}
