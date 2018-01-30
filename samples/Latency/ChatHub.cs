using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Latency
{
    public class Chat : HubWithPresence
    {
        private readonly LatencyOption _latencyOption;
        public Chat(IUserTracker<Chat> userTracker, LatencyOption latencyOption)
            : base(userTracker)
        {
            _latencyOption = latencyOption;
        }

        public override Task OnConnectedAsync()
        {
            Task t = OnUsersJoined();
	        if (GetUsersOnline() == _latencyOption.ConcurrentClientCount)
	        {
		        Clients.All.SendAsync("start", "start");
	        }
            return t;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return OnUsersLeft();
        }

        public void broadcastMessage(string name, string message)
        {
            Clients.All.SendAsync("broadcastMessage", name, message);
        }

        public void echo(string name, string message)
        {
            Clients.Client(Context.ConnectionId).SendAsync("echo", name, message);
        }
    }
}
