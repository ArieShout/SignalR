using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
namespace Microsoft.AspNetCore.SignalR.ServiceCore.Connection
{
    public interface IServiceHubConnection
    {
        Task OnDataReceivedAsync(InvocationMessage invocation);
    }
}
