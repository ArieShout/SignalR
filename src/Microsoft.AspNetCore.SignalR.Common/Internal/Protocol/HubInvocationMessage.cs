// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public abstract class HubInvocationMessage : HubMessage
    {
        internal const string ActionKeyName = "action";
        internal const string ConnectionIdKeyName = "connId";
        internal const string GroupNameKeyName = "group";
        internal const string ExcludedIdsKeyName = "excluded";
        internal const string ClaimsKeyName = "claims";

        public string InvocationId { get; }

        public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();

        protected HubInvocationMessage(string invocationId)
        {
            InvocationId = invocationId;
        }
    }
}
