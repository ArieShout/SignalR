using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ServiceChatSample
{
    public class TimeService
    {
        private readonly SignalR _signalr;
        private readonly IHubClients<IServiceClientProxy> _hubClientProxy;
        private readonly Timer _timer;

        public TimeService(SignalR signalr)
        {
            _signalr = signalr;
            _hubClientProxy = _signalr.CreateHubClientsProxy<Chat>();
            _timer = new Timer(Run, this, 100, 60 * 1000);
        }

        private static void Run(object state)
        {
            _ = ((TimeService) state).Broadcast();
        }

        private async Task Broadcast()
        {
            await _hubClientProxy.All.InvokeAsync("broadcastMessage",
                new object[]
                {
                    "[Current UTC Time]",
                    DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                });
        }
    }
}
