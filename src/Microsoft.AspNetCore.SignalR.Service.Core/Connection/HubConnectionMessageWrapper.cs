using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public class HubConnectionMessageWrapper
    {
        int _hubConnectionIndex;
        HubMethodInvocationMessage _hubInvocationMessage;
        public HubConnectionMessageWrapper(int hubConnectionIndex, HubMethodInvocationMessage hubInvocationMessage)
        {
            _hubConnectionIndex = hubConnectionIndex;
            _hubInvocationMessage = hubInvocationMessage;
        }

        public int HubConnectionIndex => _hubConnectionIndex;
        public HubMethodInvocationMessage HubMethodInvocationMessage => _hubInvocationMessage;
    }
}