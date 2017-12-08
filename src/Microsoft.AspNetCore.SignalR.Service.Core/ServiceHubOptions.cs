// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class ServiceHubOptions
    {
        public LogLevel ConsoleLogLevel { get; set; } = LogLevel.Information;
    }
}
