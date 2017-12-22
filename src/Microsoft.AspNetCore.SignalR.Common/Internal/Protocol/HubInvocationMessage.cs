// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public abstract class HubInvocationMessage : HubMessage
    {
        public string InvocationId { get; }

        public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();

        protected HubInvocationMessage(string invocationId)
        {
            InvocationId = invocationId;
        }
    }
}
