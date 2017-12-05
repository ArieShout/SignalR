using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Redis;
using Microsoft.AspNetCore.SignalR.Redis.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.SignalR.ServiceServer
{
    public class RedisHubConnectionRouter : IHubConnectionRouter
    {
        private readonly IConnectionMultiplexer _redisServerConnection;
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _connectionStatus = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>(); 

        // TODO: inject dependency of config provider, so that we can change routing algorithm without restarting service
        public RedisHubConnectionRouter(ILogger<RedisHubConnectionRouter> logger, IOptions<RedisOptions2> options)
        {
            _logger = logger;
            _redisServerConnection = InitRedisConnection(logger, options.Value);
        }

        private IConnectionMultiplexer InitRedisConnection(ILogger<RedisHubConnectionRouter> logger, RedisOptions2 options)
        {
            var writer = new LoggerTextWriter(logger);
            var redisConnection = options.Connect(writer);
            redisConnection.ConnectionRestored += (_, e) =>
            {
                // We use the subscription connection type
                // Ignore messages from the subscription connection (avoids duplicates)
                if (e.ConnectionType == ConnectionType.Subscription)
                {
                    return;
                }

                _logger.ConnectionRestored();
            };

            redisConnection.ConnectionFailed += (_, e) =>
            {
                // We use the subscription connection type
                // Ignore messages from the subscription connection (avoids duplicates)
                if (e.ConnectionType == ConnectionType.Subscription)
                {
                    return;
                }

                _logger.ConnectionFailed(e.Exception);
            };

            if (redisConnection.IsConnected)
            {
                _logger.Connected();
            }
            else
            {
                _logger.NotConnected();
            }
            return redisConnection;
        }

        #region Client Events

        public async Task OnClientConnected(string hubName, HubConnectionContext connection)
        {
            if (connection.GetTargetConnectionId() != null) return;

            var targetConnId = AssignClientToServer(hubName);

            connection.AddTargetConnectionId(targetConnId);

            // Update entry score in the Sorted Set in Redis
            await _redisServerConnection.GetDatabase().SortedSetIncrementAsync(hubName, connection.ConnectionId, 1);
            // Sync score from Redis
            if (_connectionStatus.TryGetValue(hubName, out var hubConnectionStatus))
            {
                var latest = await _redisServerConnection.GetDatabase().SortedSetScoreAsync(hubName, targetConnId);
                hubConnectionStatus.TryUpdate(targetConnId, c => latest.HasValue ? (int)latest.Value : c + 1);
            }
        }

        private string AssignClientToServer(string hubName)
        {
            string targetConnId = null;
            // Try routing to local connections first
            if (_connectionStatus.TryGetValue(hubName, out var hubConnectionStatus))
            {
                targetConnId = hubConnectionStatus.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            }
            else // Find available connections from Redis
            {
                var availableConnArray = _redisServerConnection.GetDatabase().SortedSetRangeByRank(hubName, start: 0, stop: 0);
                if (availableConnArray != null && availableConnArray.Length > 0)
                {
                    targetConnId = availableConnArray[0];
                }
            }

            return targetConnId;
        }

        public async Task OnClientDisconnected(string hubName, HubConnectionContext connection)
        {
            var targetConnId = connection.GetTargetConnectionId();
            if (targetConnId == null) return;

            // Update entry score in the Sorted Set in Redis
            await _redisServerConnection.GetDatabase().SortedSetDecrementAsync(hubName, connection.ConnectionId, 1);
            // Sync score from Redis
            if (_connectionStatus.TryGetValue(hubName, out var hubConnectionStatus))
            {
                var latest = await _redisServerConnection.GetDatabase().SortedSetScoreAsync(hubName, targetConnId);
                hubConnectionStatus.TryUpdate(targetConnId, c => latest.HasValue ? (int)latest.Value : c - 1);
            }
        }

        #endregion

        #region Server Events

        public void OnServerConnected(string hubName, HubConnectionContext connection)
        {
            var hubConnectionStatus = _connectionStatus.GetOrAdd(hubName, new ConcurrentDictionary<string, int>());
            hubConnectionStatus.TryAdd(connection.ConnectionId, 0);
            // Add new entry to a Sorted Set in Redis
            _redisServerConnection.GetDatabase().SortedSetAdd(hubName, connection.ConnectionId, 0);
        }

        public void OnServerDisconnected(string hubName, HubConnectionContext connection)
        {
            if (_connectionStatus.TryGetValue(hubName, out var hubConnectionStatus))
            {
                hubConnectionStatus.TryRemove(connection.ConnectionId, out _);
            }
            // Remove entry from the Sorted Set in Redis
            _redisServerConnection.GetDatabase().SortedSetRemove(hubName, connection.ConnectionId);
        }

        #endregion

        #region Misc

        private class LoggerTextWriter : TextWriter
        {
            private readonly ILogger _logger;

            public LoggerTextWriter(ILogger logger)
            {
                _logger = logger;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {

            }

            public override void WriteLine(string value)
            {
                _logger.LogDebug(value);
            }
        }

        #endregion
    }
}
