using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public static class HubInvocationMessageExtension
    {
        public static TMessage AddMetadata<TMessage>(this TMessage message, IDictionary<string, string> metadata)
            where TMessage : HubInvocationMessage
        {
            if (message != null && metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    message.Metadata.Add(kvp.Key, kvp.Value);
                }
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

        public static bool TryGetProperty(this HubInvocationMessage message, string propertyName, out string propertyValue)
        {
            if (message.Metadata == null)
            {
                propertyValue = null;
                return false;
            }
            return message.Metadata.TryGetValue(propertyName, out propertyValue);
        }
    }
}
