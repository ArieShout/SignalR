﻿using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Core.Internal;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class Heartbeat : IDisposable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

        private readonly IHeartbeatHandler[] _callbacks;

        private readonly ILogger _logger;

        private Timer _timer;

        private int _executingOnHeartbeat;

        public Heartbeat(IHeartbeatHandler[] callbacks, ILogger logger)
        {
            _callbacks = callbacks;
            _logger = logger;
        }

        public void Start()
        {
            _timer = new Timer(OnHeartbeat, state: this, dueTime: Interval, period: Interval);
        }

        private static void OnHeartbeat(object state)
        {
            ((Heartbeat)state).OnHeartbeat();
        }

        // Called by the Timer (background) thread
        internal void OnHeartbeat()
        {
            var now = DateTimeOffset.UtcNow;

            if (Interlocked.Exchange(ref _executingOnHeartbeat, 1) == 0)
            {
                try
                {
                    foreach (var callback in _callbacks)
                    {
                        callback.OnHeartbeat(now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(0, ex, $"{nameof(Heartbeat)}.{nameof(OnHeartbeat)}");
                }
                finally
                {
                    Interlocked.Exchange(ref _executingOnHeartbeat, 0);
                }
            }
            else
            {
                _logger.HeartbeatSlow(Interval, now);
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
