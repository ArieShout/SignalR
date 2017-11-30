// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public static class HubInvocationMessageExtension
    {
        public static TMessage AddMetadata<TMessage>(this TMessage message, IDictionary<string, string> metadata) where TMessage : HubInvocationMessage
        {
            if (message == null || metadata == null) return message;
            foreach (var kvp in metadata)
            {
                message.Metadata.Add(kvp.Key, kvp.Value);
            }
            return message;
        }

        public static TMessage AddConnectionId<TMessage>(this TMessage message, string connectionId) where TMessage : HubInvocationMessage
        {
            message.Metadata.Add(HubInvocationMessage.ConnectionIdKeyName, connectionId);
            return message;
        }

        public static string GetConnectionId<TMessage>(this TMessage message) where TMessage : HubInvocationMessage
        {
            message.Metadata.TryGetValue(HubInvocationMessage.ConnectionIdKeyName, out var connectionId);
            return connectionId;
        }
    }
}
