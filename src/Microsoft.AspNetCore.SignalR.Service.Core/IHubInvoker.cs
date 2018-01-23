using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public interface IHubInvoker
    {
        Task OnInvocationAsync(HubConnectionMessageWrapper hubMessage, bool isStreamedInvocation = false);
    }
}
