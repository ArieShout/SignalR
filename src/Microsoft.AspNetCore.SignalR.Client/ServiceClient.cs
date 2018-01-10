using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public class ServiceClient
    {
        public enum ProtocolType
        {
            Text = 0,
            Binary = 1
        }

        private readonly ServiceCredential _credential;
        private readonly TransportType _transport;
        private readonly HubConnection _connection;

        public ServiceClient(string connectionString, TransportType transport = TransportType.WebSockets,
            ProtocolType protocol = ProtocolType.Text)
        {
            if (!ServiceCredential.TryParse(connectionString, out _credential))
            {
                throw new ArgumentException($"Invalid connection string: {connectionString}");
            }

            _transport = transport;

            _connection = new HubConnectionBuilder()
                .WithUrl(_credential.ServiceUrl)
                .WithTransport(_transport)
                .WithHubProtocol(protocol == ProtocolType.Text
                    ? new JsonHubProtocol()
                    : (IHubProtocol) new MessagePackHubProtocol())
                .WithJwtBearer(() => _credential.GenerateJwtBearer())
                .Build();
        }

        public async Task StartAsync()
        {
            await _connection.StartAsync();
        }

        public async Task StopAsync()
        {
            await _connection.StopAsync();
        }

        public async Task SubscribeAsync(string channelName)
        {
            await _connection.SendAsync("subscribe", channelName);
        }

        public async Task UnsubscribeAsync(string channelName)
        {
            await _connection.SendAsync("unsubscribe", channelName);
        }

        public async Task PublishAsync(string channelName, string eventName, object data)
        {
            await _connection.SendAsync("publish", $"{channelName}:{eventName}", data);
        }

        public void On<T>(string channel, string eventName, Action<T> handler)
        {
            _connection.On($"{channel}:{eventName}", handler);
        }
    }
}
