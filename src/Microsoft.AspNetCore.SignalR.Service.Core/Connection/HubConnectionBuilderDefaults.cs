﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderDefaults
    {
        public static readonly string LoggerFactoryKey = "LoggerFactory";
        public static readonly string HubProtocolKey = "HubProtocol";
        public static readonly string HubBinderKey = "HubBinder";
        public static readonly string RequestQueueKey = "RequestQueue";
        public static readonly string StatKey = "Stat";
    }
}