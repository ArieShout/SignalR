using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public interface IHubStatManager<THub>
    {
        Stats Stat();
        void Tick(DateTimeOffset now);

        Task GetHubStat(HttpContext context);
    }
}
