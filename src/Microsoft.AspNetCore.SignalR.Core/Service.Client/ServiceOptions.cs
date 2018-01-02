// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceOptions
    {
        public static readonly TimeSpan DefaultJwtBearerLifetime = TimeSpan.FromSeconds(30);
        public static readonly int DefaultConnectionNumber = 3;
        
        public TimeSpan JwtBearerLifetime { get; set; } = DefaultJwtBearerLifetime;

        public int ConnectionNumber { get; set; } = DefaultConnectionNumber;

        public Func<HttpContext, IEnumerable<Claim>> ClaimProvider { get; set; } = context => context.User.Claims;
    }
}
