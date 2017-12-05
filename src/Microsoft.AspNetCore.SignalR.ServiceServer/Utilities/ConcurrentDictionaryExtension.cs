using System;
using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public static class ConcurrentDictionaryExtension
    {
        // TODO: It is possible that below update will fail because of high volume of concurrent connections.
        //       Need a more robust way to update connection count.
        public static bool TryUpdate(this ConcurrentDictionary<string, int> dict, string key,
            Func<int, int> updateFactory)
        {
            return dict.TryGetValue(key, out var currentValue) && dict.TryUpdate(key, updateFactory(currentValue), currentValue);
        }

        // TODO: It is possible that below update will fail because of high volume of concurrent connections.
        //       Need a more robust way to update connection count.
        public static bool TryUpdate(this ConcurrentDictionary<string, int> dict, string key, int newValue)
        {
            return dict.TryGetValue(key, out var currentValue) && dict.TryUpdate(key, newValue, currentValue);
        }
    }
}
