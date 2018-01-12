using System;
using System.Collections.Generic;
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

        private ServiceCredential _credential;
        private TransportType _transport;
        private HubConnection _connection;

        public ServiceClient(string connectionString, TransportType transport = TransportType.WebSockets,
            ProtocolType protocol = ProtocolType.Text)
        {
            if (!ServiceCredential.TryParse(connectionString, out var credential))
            {
                throw new ArgumentException($"Invalid connection string: {connectionString}");
            }
            Init(credential, transport, protocol);
        }

        public ServiceClient(ServiceCredential credential, TransportType transport = TransportType.WebSockets,
            ProtocolType protocol = ProtocolType.Text)
        {
            Init(credential, transport, protocol);
        }

        private void Init(ServiceCredential credential, TransportType transport, ProtocolType protocol)
        {
            _credential = credential;
            _transport = transport;
            _connection = new HubConnectionBuilder()
                .WithUrl(_credential.ServiceUrl)
                .WithTransport(_transport)
                .WithHubProtocol(protocol == ProtocolType.Text
                    ? new JsonHubProtocol()
                    : (IHubProtocol) new MessagePackHubProtocol())
                .WithJwtBearer(() => _credential.GenerateJwtBearer("*"))
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

        public async Task<MessageResult> SubscribeAsync(string channel)
        {
            return await SubscribeAsync(new[] {channel});
        }

        public async Task<MessageResult> SubscribeAsync(IEnumerable<string> channels)
        {
            return await _connection.InvokeAsync<MessageResult>("subscribe", channels);
        }

        public async Task<MessageResult> UnsubscribeAsync(string channel)
        {
            return await UnsubscribeAsync(new[] {channel});
        }

        public async Task<MessageResult> UnsubscribeAsync(IEnumerable<string> channels)
        {
            return await _connection.InvokeAsync<MessageResult>("unsubscribe", channels);
        }

        public async Task<MessageResult> PublishAsync(string channelName, string eventName, object data)
        {
            return await _connection.InvokeAsync<MessageResult>("publish", channelName, eventName, data);
        }

        public void On<T>(string channel, string eventName, Action<T> handler)
        {
            _connection.On($"{channel}:{eventName}", handler);
        }
    }
}
