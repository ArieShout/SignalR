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

        public HubMessageBroker(IHubConnectionRouter router)
        {
            _router = router;
        }

        #region Client connections

        public async Task OnClientConnectedAsync(string hubName, HubConnectionContext context)
        {
            var clientHubManager = _clientHubManagerDict.GetOrAdd(hubName, new DefaultHubLifetimeManager<ClientHub>());
            await clientHubManager.OnConnectedAsync(context);

            // Assign client connection to a server connection
            _router.OnClientConnected(hubName, context);

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

            _router.OnClientDisconnected(hubName, context);

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

            var serverHubManager = _serverHubManagerDict[hubName];
            if (serverHubManager != null)
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
            var serverHubManager = _serverHubManagerDict.GetOrAdd(hubName, new DefaultHubLifetimeManager<ServerHub>());
            await serverHubManager.OnConnectedAsync(context);
            _router.OnServerConnected(hubName, context);
        }

        public async Task OnServerDisconnectedAsync(string hubName, HubConnectionContext context)
        {
            var serverHubManager = _serverHubManagerDict[hubName];
            if (serverHubManager != null)
            {
                await serverHubManager.OnDisconnectedAsync(context);
            }
            _router.OnServerDisconnected(hubName, context);
            // TODO: Disconnect all client connections routing to this server
        }

        public async Task PassThruServerMessage(string hubName, HubConnectionContext context, HubMethodInvocationMessage message)
        {
            var clientHubManager = _clientHubManagerDict[hubName];
            if (clientHubManager != null)
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
            var clientHubManager = _clientHubManagerDict[hubName];
            if (clientHubManager != null)
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

        #endregion
    }
}
