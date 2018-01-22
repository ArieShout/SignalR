using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Latency
{
    public class Chat : HubWithPresence
    {
        public Chat(IUserTracker<Chat> userTracker)
            : base(userTracker)
        {
        }

        public override Task OnConnectedAsync()
        {
            Task t = OnUsersJoined();
	    //Console.WriteLine("+++connections: " + GetUsersOnline());
	    if (GetUsersOnline() == 4000)
	    {
		    Clients.All.InvokeAsync("start", "start");//GetUsersOnline());
	    }
            return t;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return OnUsersLeft();
        }

        public void broadcastMessage(string name, string message)
        {
            Clients.All.InvokeAsync("broadcastMessage", name, message);
        }

        public void echo(string name, string message)
        {
	    /*
            long DatetimeMinTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;
            long timestamp = (DateTime.Now.ToUniversalTime().Ticks - DatetimeMinTimeTicks) / 10000;
	    */
            Clients.Client(Context.ConnectionId).InvokeAsync("echo", name, message);
        }
    }
}
