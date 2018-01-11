using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatDemo
{
    public class Broadcast : IDisposable
    {
        private readonly string _connectionString;
        private readonly Timer _timer;
        private readonly ServiceClient _client;

        public Broadcast(string connectionString)
        {
            _connectionString = connectionString;
            _client = InitServiceClient(connectionString).Result;
            _timer = new Timer(Run, this, 5 * 1000, 30 * 1000);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async Task<ServiceClient> InitServiceClient(string connectionString)
        {
            var client = new ServiceClient(connectionString);
            await client.StartAsync();
            await client.SubscribeAsync("group-chat");
            return client;
        }

        private static void Run(object state)
        {
            _ = ((Broadcast) state).BroadcastMessage();
        }

        private async Task BroadcastMessage()
        {
            Console.WriteLine("[Broadcast] start...");
            await _client.PublishAsync("group-chat", "broadcast", new {
                name = "[SYSTEM]",
                message = $"Current time is {DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}"
            });
            Console.WriteLine("[Broadcast] done.");
        }
    }
}
