using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.ServiceServer
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
    }
}
