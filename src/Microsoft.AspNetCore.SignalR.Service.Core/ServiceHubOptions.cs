// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class ServiceHubOptions
    {
        public LogLevel ConsoleLogLevel { get; set; } = LogLevel.Information;

        public int ServiceConnectionNo { get; set; } = 2;

        // TODO: selectively pass claims to SignalR service
        public Func<HttpContext, IEnumerable<Claim>> ClaimProvider { get; set; } =
            context => context.User.Claims;
    }
}
