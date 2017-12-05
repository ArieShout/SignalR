using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Internal;
namespace Microsoft.AspNetCore.SignalR.ServiceCore.Connection
{
    public interface IServiceHubReceiver
    {
        void GenHubProtocolReaderWriter(HubProtocolReaderWriter protocolReaderWriter);
    }
}
