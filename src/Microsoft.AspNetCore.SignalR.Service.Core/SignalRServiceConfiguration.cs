using System;
using System.Linq;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class SignalRServiceConfiguration
    {
        public string HostName { get; private set; }

        public string Key { get; private set; }

        public static bool TryParse(string connectionString, out SignalRServiceConfiguration config)
        {
            config = null;
            if (string.IsNullOrEmpty(connectionString)) return false;

            var dict = connectionString.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(new[] {'='}, 2))
                .ToDictionary(t => t[0].Trim().ToLower(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
            if (!dict.ContainsKey("hostname") || !dict.ContainsKey("key")) return false;

            config = new SignalRServiceConfiguration
            {
                HostName = dict["hostname"],
                Key = dict["key"]
            };
            return true;
        }
    }
}
