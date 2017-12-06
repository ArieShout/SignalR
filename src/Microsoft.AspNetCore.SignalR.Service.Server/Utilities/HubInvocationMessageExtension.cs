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
    }
}
