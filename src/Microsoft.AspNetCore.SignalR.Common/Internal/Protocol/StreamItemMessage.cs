// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public class StreamItemMessage : HubInvocationMessage
    {
        public object Item { get; }

        public StreamItemMessage(string invocationId, object item, IDictionary<string, string> metadata = null)
            : base(invocationId, metadata)
        {
            Item = item;
        }

        public override string ToString()
        {
            return $"StreamItem {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(Item)}: {Item ?? "<<null>>"} }}";
        }
    }
}
