using System;
using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public static class ConcurrentDictionaryExtension
    {
        // TODO: It is possible that below update will fail because of high volume of concurrent connections.
        //       Need a more robust way to update connection count.
        public static bool TryUpdate<T>(this ConcurrentDictionary<T, int> dict, T key,
            Func<int, int> updateFactory)
        {
            var retry = 3;
            while (retry > 0)
            {
                if (!dict.TryGetValue(key, out var currentValue)) break;
                if (dict.TryUpdate(key, updateFactory(currentValue), currentValue))
                {
                    return true;
                }
                retry--;
            }
            return false;
        }
    }
}
