// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public static class HubInvocationMessageExtension
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public static TMessage AddMetadata<TMessage>(this TMessage message, IDictionary<string, string> metadata)
            where TMessage : HubInvocationMessage
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

        public static bool TryGetMetadata<TMessage>(this TMessage message, string metadataName, out string metadataValue)
            where TMessage : HubInvocationMessage
        {
            if (message.Metadata != null &&
                message.Metadata.TryGetValue(metadataName, out metadataValue))
            {
                return true;
            }
            metadataValue = null;
            return false;
        }

        public static TMessage AddConnectionId<TMessage>(this TMessage message, string connectionId)
            where TMessage : HubInvocationMessage
        {
            return message.AddMetadata(HubInvocationMessage.ConnectionIdKeyName, connectionId);
        }

        public static string GetConnectionId<TMessage>(this TMessage message) where TMessage : HubInvocationMessage
        {
            message.Metadata.TryGetValue(HubInvocationMessage.ConnectionIdKeyName, out var connectionId);
            return connectionId;
        }

        public static bool TryGetConnectionId<TMessage>(this TMessage message, out string connectionId)
            where TMessage : HubInvocationMessage
        {
            return message.TryGetMetadata(HubInvocationMessage.ConnectionIdKeyName, out connectionId);
        }

        public static TMessage AddGroupName<TMessage>(this TMessage message, string groupName)
            where TMessage : HubInvocationMessage
        {
            return message.AddMetadata(HubInvocationMessage.GroupNameKeyName, groupName);
        }

        public static bool TryGetGroupName<TMessage>(this TMessage message, out string groupName)
            where TMessage : HubInvocationMessage
        {
            return message.TryGetMetadata(HubInvocationMessage.GroupNameKeyName, out groupName);
        }

        public static TMessage AddExcludedIds<TMessage>(this TMessage message, IReadOnlyList<string> excludedIds)
            where TMessage : HubInvocationMessage
        {
            return message.AddMetadata(HubInvocationMessage.ExcludedIdsKeyName, string.Join(",", excludedIds));
        }

        public static bool TryGetExcludedIds<TMessage>(this TMessage message, out IReadOnlyList<string> excludedIdList)
            where TMessage : HubInvocationMessage
        {
            excludedIdList = message.TryGetMetadata(HubInvocationMessage.ExcludedIdsKeyName, out var value)
                ? new List<string>(value.Split(','))
                : null;

            return excludedIdList != null;
        }

        public static TMessage AddAction<TMessage>(this TMessage message, string actionName)
            where TMessage : HubInvocationMessage
        {
            return message.AddMetadata(HubInvocationMessage.ActionKeyName, actionName);
        }

        public static bool TryGetAction<TMessage>(this TMessage message, out string actionName)
            where TMessage : HubInvocationMessage
        {
            return message.TryGetMetadata(HubInvocationMessage.ActionKeyName, out actionName);
        }

        public static TMessage AddClaims<TMessage>(this TMessage message, IEnumerable<Claim> claims)
            where TMessage : HubInvocationMessage
        {
            return message.AddMetadata(HubInvocationMessage.ClaimsKeyName, JsonConvert.SerializeObject(claims, JsonSerializerSettings));
        }

        public static bool TryGetClaims<TMessage>(this TMessage message, out IEnumerable<Claim> claims)
            where TMessage : HubInvocationMessage
        {
            claims = message.TryGetMetadata(HubInvocationMessage.ClaimsKeyName, out var serializedClaims)
                ? JsonConvert.DeserializeObject<IEnumerable<Claim>>(serializedClaims)
                : null;

            return claims != null;
        }
    }
}
