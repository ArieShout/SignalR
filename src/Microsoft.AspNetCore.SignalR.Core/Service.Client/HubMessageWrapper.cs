using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubMessageWrapper
    {
        public HubMessageWrapper(HubConnection hubConnection, HubMethodInvocationMessage hubInvocationMessage)
        {
            HubConnection = hubConnection;
            HubMethodInvocationMessage = hubInvocationMessage;
        }

        public HubConnection HubConnection { get; }

        public HubMethodInvocationMessage HubMethodInvocationMessage { get; }
    }
}
