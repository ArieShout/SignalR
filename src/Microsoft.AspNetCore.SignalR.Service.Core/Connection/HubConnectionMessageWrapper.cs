using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public class HubConnectionMessageWrapper
    {
        HubConnection _hubConnection;
        HubMethodInvocationMessage _hubInvocationMessage;
        public HubConnectionMessageWrapper(HubConnection hubConnection, HubMethodInvocationMessage hubInvocationMessage)
        {
            _hubConnection = hubConnection;
            _hubInvocationMessage = hubInvocationMessage;
        }

        public HubConnection HubConnection => _hubConnection;
        public HubMethodInvocationMessage HubMethodInvocationMessage => _hubInvocationMessage;
    }
}
