// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceCredential
    {
        public string HostName { get; private set; }

        public string AccessKey { get; private set; }

        public static bool TryParse(string connectionString, out ServiceCredential credential)
        {
            credential = null;
            if (string.IsNullOrEmpty(connectionString)) return false;

            var dict = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(new[] { '=' }, 2))
                .ToDictionary(t => t[0].Trim().ToLower(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
            if (!dict.ContainsKey("hostname") || !dict.ContainsKey("accesskey")) return false;

            credential = new ServiceCredential
            {
                HostName = dict["hostname"],
                AccessKey = dict["accesskey"]
            };
            return true;
        }
    }
}
