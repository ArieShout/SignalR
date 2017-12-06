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
        public static TMessage AddMetadata<TMessage>(this TMessage message, string key, string value)
            where TMessage : HubInvocationMessage
        {
            if (message != null && !string.IsNullOrEmpty(key))
            {
                message.Metadata.Add(key, value);
            }
            return message;
        }

        public static bool TryGetProperty<TMessage>(this TMessage message, string propertyName, out string propertyValue) where TMessage : HubInvocationMessage
        {
            if (message.Metadata == null)
            {
                propertyValue = null;
                return false;
            }
            return message.Metadata.TryGetValue(propertyName, out propertyValue);
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

        public static bool TryGetConnectionId<TMessage>(this TMessage message, out string propertyValue) where TMessage : HubInvocationMessage
        {
            if (message.Metadata == null)
            {
                propertyValue = null;
                return false;
            }
            return message.Metadata.TryGetValue(HubInvocationMessage.ConnectionIdKeyName, out propertyValue);
        }

        public static TMessage AddGroupName<TMessage>(this TMessage message, string groupName) where TMessage : HubInvocationMessage
        {
            message.Metadata.Add(HubInvocationMessage.GroupNameKeyName, groupName);
            return message;
        }

        public static bool TryGetGroupName<TMessage>(this TMessage message, out string propertyValue) where TMessage : HubInvocationMessage
        {
            if (message.Metadata == null)
            {
                propertyValue = null;
                return false;
            }
            return message.Metadata.TryGetValue(HubInvocationMessage.GroupNameKeyName, out propertyValue);
        }

        public static TMessage AddExcludedIds<TMessage>(this TMessage message, IReadOnlyList<string> excludedIds) where TMessage : HubInvocationMessage
        {
            message.Metadata.Add(HubInvocationMessage.ExcludedIdsKeyName, System.String.Join(",", excludedIds));
            return message;
        }

        public static bool TryGetExcludedIds<TMessage>(this TMessage message, out IReadOnlyList<string> propertyValue) where TMessage : HubInvocationMessage
        {
            if (message.Metadata == null)
            {
                propertyValue = null;
                return false;
            }
            string value;
            if (message.Metadata.TryGetValue(HubInvocationMessage.ExcludedIdsKeyName, out value))
            {
                propertyValue = new List<string>(value.Split(','));
                return true;
            }
            propertyValue = null;
            return false;
        }
    }
}
