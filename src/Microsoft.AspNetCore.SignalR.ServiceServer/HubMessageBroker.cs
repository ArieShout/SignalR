using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.ServiceServer;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubMessageBroker : IHubMessageBroker
    {
        private const string ConnectionIdKeyName = "connId";
        private const string GroupNameKeyName = "groupName";
        private const string ExcludedIdsKeyName = "excluded";

        private const string OnConnectedMethodName = "OnConnectedAsync";
        private const string OnDisconnectedMethodName = "OnDisconnectedAsync";

        // TODO: cleanup unused HubLifetimeManager to avoid memory leak
        private readonly ConcurrentDictionary<string, HubLifetimeManager<ClientHub>> _clientHubManagerDict = new ConcurrentDictionary<string, HubLifetimeManager<ClientHub>>();
        private readonly ConcurrentDictionary<string, HubLifetimeManager<ServerHub>> _serverHubManagerDict = new ConcurrentDictionary<string, HubLifetimeManager<ServerHub>>();

        private readonly IHubConnectionRouter _router;
        private readonly IHubLifetimeManagerFactory _hubLifetimeManagerFactory;

        public HubMessageBroker(IHubConnectionRouter router, IHubLifetimeManagerFactory hubLifetimeManagerFactory)
        {
            _router = router;
            _hubLifetimeManagerFactory = hubLifetimeManagerFactory;
        }

        #region Client connections

        public async Task OnClientConnectedAsync(string hubName, HubConnectionContext context)
        {
            var clientHubManager = _clientHubManagerDict.GetOrAdd(hubName, _hubLifetimeManagerFactory.Create<ClientHub>(hubName));
            await clientHubManager.OnConnectedAsync(context);

            // Assign client connection to a server connection
            // Don't wait for its completion
            _ = _router.OnClientConnected(hubName, context);

            // When scaling out with Redis, it is possible that no server connection is connected to current instance.
            // So we need to create a ServerHubLifetimeManager to send the message to Redis.
            AssureServerHubLifetimeManager(hubName);

            // Invoke OnConnectedAsync on server
            await PassThruClientMessage(hubName, context,
                new InvocationMessage(Guid.NewGuid().ToString(), true, OnConnectedMethodName, null, new object[0]));
        }

        public async Task OnClientDisconnectedAsync(string hubName, HubConnectionContext context)
        {
            var clientHubManager = _clientHubManagerDict[hubName];
            if (clientHubManager != null)
            {
                await clientHubManager.OnDisconnectedAsync(context);
            }

            // Don't wait for its completion
            _ = _router.OnClientDisconnected(hubName, context);

            // Invoke OnDiconnectedAsync on server
            await PassThruClientMessage(hubName, context,
                new InvocationMessage(Guid.NewGuid().ToString(), true, OnDisconnectedMethodName, null, new object[0]));
        }

        public async Task PassThruClientMessage(string hubName, HubConnectionContext context, HubMethodInvocationMessage message)
        {
            var targetConnId = context.GetTargetConnectionId();

            if (!IsServerConnectionAlive(targetConnId))
            {
                throw new Exception("No assigned server.");
            }

            if (_serverHubManagerDict.TryGetValue(hubName, out var serverHubManager))
            {
                // Add original connection Id to message metadata
                message.Metadata.Add(ConnectionIdKeyName, context.ConnectionId);

                await ((DefaultHubLifetimeManager<ServerHub>)serverHubManager).SendMessageAsync(targetConnId, message);
            }
        }

        #endregion

        #region Server connections

        public async Task OnServerConnectedAsync(string hubName, HubConnectionContext context)
        {
            var serverHubManager = _serverHubManagerDict.GetOrAdd(hubName, _hubLifetimeManagerFactory.Create<ServerHub>(hubName));
            await serverHubManager.OnConnectedAsync(context);
            _router.OnServerConnected(hubName, context);

            // When scaling out with Redis, it is possible that no client connection is connected to current instance.
            // So we need to create a HubLifetimeManager to send messages to Redis.
            AssureClientHubLifetimeManager(hubName);
        }

        public async Task OnServerDisconnectedAsync(string hubName, HubConnectionContext context)
        {
            if (_serverHubManagerDict.TryGetValue(hubName, out var serverHubManager))
            {
                await serverHubManager.OnDisconnectedAsync(context);
            }
            _router.OnServerDisconnected(hubName, context);
            // TODO: Disconnect all client connections routing to this server
        }

        public async Task PassThruServerMessage(string hubName, HubConnectionContext context, HubMethodInvocationMessage message)
        {
            if (_clientHubManagerDict.TryGetValue(hubName, out var clientHubManager))
            {
                // Invoke a single connection
                if (message.TryGetProperty(ConnectionIdKeyName, out var connectionId))
                {
                    await clientHubManager.InvokeConnectionAsync(connectionId, message.Target,
                        message.Arguments);
                }
                // Invoke a group
                else if (message.TryGetProperty(GroupNameKeyName, out var groupName))
                {
                    await clientHubManager.InvokeGroupAsync(groupName, message.Target,
                        message.Arguments);
                }
                // Invoke all except
                else if (message.TryGetProperty(ExcludedIdsKeyName, out var excludedIds))
                {
                    await clientHubManager.InvokeAllExceptAsync(message.Target, message.Arguments,
                        excludedIds.Split(',').ToList());
                }
                // Invoke all
                else
                {
                    await clientHubManager.InvokeAllAsync(message.Target, message.Arguments);
                }                
            }
        }

        public async Task PassThruServerMessage(string hubName, HubConnectionContext context, CompletionMessage message)
        {
            if (_clientHubManagerDict.TryGetValue(hubName, out var clientHubManager))
            {
                if (message.TryGetProperty(ConnectionIdKeyName, out var connectionId))
                {
                    await ((DefaultHubLifetimeManager<ClientHub>)clientHubManager).SendMessageAsync(connectionId, message);
                }
            }
        }

        #endregion

        #region Utilities

        // TODO: implement check liveness of server connection
        private bool IsServerConnectionAlive(string connectionId)
        {
            return !string.IsNullOrEmpty(connectionId);
        }

        private void AssureServerHubLifetimeManager(string hubName)
        {
            // ConcurrentDictionary.TryGetValue is lock free
            if (_serverHubManagerDict.TryGetValue(hubName, out _)) return;
            // TODO: Possible error handling
            _serverHubManagerDict.TryAdd(hubName, _hubLifetimeManagerFactory.Create<ServerHub>(hubName));
        }

        private void AssureClientHubLifetimeManager(string hubName)
        {
            // ConcurrentDictionary.TryGetValue is lock free
            if (_clientHubManagerDict.TryGetValue(hubName, out _)) return;
            // TODO: Possible error handling
            _clientHubManagerDict.TryAdd(hubName, _hubLifetimeManagerFactory.Create<ClientHub>(hubName));
        }

        #endregion
    }
}
