using Microsoft.AspNetCore.SignalR.ServiceCore.API;

namespace MyChat
{
    public class ChatHub : ServiceHub
    {
        public void Send(string message)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.InvokeAsync("Send", message);
        }

        public void broadcastMessage(string name, string message)
        {
            Clients.All.InvokeAsync("broadcastMessage", name, message);
        }

        public void echo(string name, string message)
        {
            Clients.Client(Context.ConnectionId).InvokeAsync("echo", name, message + " (echo from server)");
        }
    }
}
