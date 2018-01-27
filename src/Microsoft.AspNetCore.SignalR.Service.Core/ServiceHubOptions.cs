// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class ServiceHubOptions
    {
        public LogLevel ConsoleLogLevel { get; set; } = LogLevel.Information;

        public int ConnectionNumber { get; set; } = 2;

        public string ServerId { get; set; } = Guid.NewGuid().ToString();

        public ProtocolType ProtocolType { get; set; } = ProtocolType.Binary;

        public int ReceiveBufferSize { get; set; } = 4096;
        public int SendBufferSize { get; set; } = 4096;
        public bool EnableMetrics { get; set; } = false;

        public bool EchoAll4TroubleShooting { get; set; } = false;
        public bool MarkTimestampInCritialPhase { get; set; } = false;

        public bool DontSendComplete { get; set; } = false; // For performance debug purpose
        public MessagePassingType MessagePassingType { get; set; } = MessagePassingType.AsyncCall;
        // TODO: selectively pass claims to SignalR service
        public Func<HttpContext, IEnumerable<Claim>> ClaimProvider { get; set; } =
            context => context.User.Claims;
    }
    public enum MessagePassingType
    {
        Channel,
        AsyncCall
    }

}
