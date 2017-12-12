using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public class HubMessageBroker : IHubMessageBroker
    {
        private const string OnConnectedMethodName = "OnConnectedAsync";
        private const string OnDisconnectedMethodName = "OnDisconnectedAsync";

        // TODO: cleanup unused HubLifetimeManager to avoid memory leak
        private readonly ConcurrentDictionary<string, HubLifetimeManager<ClientHub>> _clientHubManagerDict =
            new ConcurrentDictionary<string, HubLifetimeManager<ClientHub>>();

        private readonly ConcurrentDictionary<string, HubLifetimeManager<ServerHub>> _serverHubManagerDict =
            new ConcurrentDictionary<string, HubLifetimeManager<ServerHub>>();

        private readonly Dictionary<string, Func<HubLifetimeManager<ClientHub>, HubMethodInvocationMessage, Task>>
            _clientHubActions =
                new Dictionary<string, Func<HubLifetimeManager<ClientHub>, HubMethodInvocationMessage, Task>>
                {
                    {"invokeconnectionasync", InvokeConnectionAsync},
                    {"invokeallasync", InvokeAllAsync},
                    {"invokeallexceptasync", InvokeAllExceptAsync},
                    {"invokegroupasync", InvokeGroupAsync},
                    {"addgroupasync", AddGroupAsync},
                    {"removegroupasync", RemoveGroupAsync}
                };

        private readonly IHubConnectionRouter _router;
        private readonly IHubLifetimeManagerFactory _hubLifetimeManagerFactory;

        public HubMessageBroker(IHubConnectionRouter router, IHubLifetimeManagerFactory hubLifetimeManagerFactory)
        {
            _router = router;
            _hubLifetimeManagerFactory = hubLifetimeManagerFactory;
        }

        #region Client Connections

        public async Task OnClientConnectedAsync(string hubName, HubConnectionContext context)
        {
            var clientHubManager = GetOrAddClientHubManager(hubName);
            await clientHubManager.OnConnectedAsync(context);

            // Assign client connection to a server connection
            // Don't wait for its completion
            _ = _router.OnClientConnected(hubName, context);

            // When scaling out with Redis, it is possible that no server connection is connected to current instance.
            // So we need to create a ServerHubLifetimeManager to send the message to Redis.
            GetOrAddServerHubManager(hubName);

            // Invoke OnConnectedAsync on server
            var message = new InvocationMessage(Guid.NewGuid().ToString(), true, OnConnectedMethodName, null,
                new object[0]);
            message.AddClaims(context.User.Claims);
            await PassThruClientMessage(hubName, context, message);
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

        public async Task PassThruClientMessage(string hubName, HubConnectionContext context,
            HubMethodInvocationMessage message)
        {
            var targetConnId = context.GetTargetConnectionId();

            if (!IsServerConnectionAlive(targetConnId))
            {
                throw new Exception("No assigned server.");
            }

            if (_serverHubManagerDict.TryGetValue(hubName, out var serverHubManager))
            {
                // Add original connection Id to message metadata
                message.AddConnectionId(context.ConnectionId);
                await ((DefaultHubLifetimeManager<ServerHub>) serverHubManager).SendMessageAsync(targetConnId, message);
            }
        }

        #endregion

        #region Server Connections

        public async Task OnServerConnectedAsync(string hubName, HubConnectionContext context)
        {
            var serverHubManager = GetOrAddServerHubManager(hubName);
            await serverHubManager.OnConnectedAsync(context);
            _router.OnServerConnected(hubName, context);

            // When scaling out with Redis, it is possible that no client connection is connected to current instance.
            // So we need to create a HubLifetimeManager to send messages to Redis.
            GetOrAddClientHubManager(hubName);
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

        public async Task PassThruServerMessage(string hubName, HubConnectionContext context,
            HubMethodInvocationMessage message)
        {
            if (TryGetAction(message, out var action) &&
                _clientHubManagerDict.TryGetValue(hubName, out var clientHubManager))
            {
                await action.Invoke(clientHubManager, message);
            }
        }

        public async Task PassThruServerMessage(string hubName, HubConnectionContext context, CompletionMessage message)
        {
            if (message.TryGetConnectionId(out var connectionId) &&
                _clientHubManagerDict.TryGetValue(hubName, out var clientHubManager))
            {
                await ((DefaultHubLifetimeManager<ClientHub>) clientHubManager).SendMessageAsync(connectionId,
                    message);
            }
        }

        #endregion

        #region Utilities

        // TODO: implement check liveness of server connection
        private bool IsServerConnectionAlive(string connectionId)
        {
            return !string.IsNullOrEmpty(connectionId);
        }

        private HubLifetimeManager<ServerHub> GetOrAddServerHubManager(string hubName)
        {
            // ConcurrentDictionary.TryGetValue is lock free
            return _serverHubManagerDict.GetOrAdd(hubName,
                _ => _hubLifetimeManagerFactory.Create<ServerHub>($"server.{hubName}"));
        }

        private HubLifetimeManager<ClientHub> GetOrAddClientHubManager(string hubName)
        {
            return _clientHubManagerDict.GetOrAdd(hubName,
                _ => _hubLifetimeManagerFactory.Create<ClientHub>($"client.{hubName}"));
        }

        private bool TryGetAction(HubMethodInvocationMessage message,
            out Func<HubLifetimeManager<ClientHub>, HubMethodInvocationMessage, Task> action)
        {
            if (message.TryGetAction(out var actionName) && !string.IsNullOrEmpty(actionName) &&
                _clientHubActions.TryGetValue(actionName.ToLower(), out action))
            {
                return true;
            }

            action = null;
            return false;
        }

        #endregion

        #region Static Methods

        private static async Task InvokeConnectionAsync(HubLifetimeManager<ClientHub> clientHubManager,
            HubMethodInvocationMessage message)
        {
            if (message.TryGetConnectionId(out var connectionId))
            {
                await clientHubManager.InvokeConnectionAsync(connectionId, message.Target, message.Arguments);
            }
        }

        private static async Task InvokeAllAsync(HubLifetimeManager<ClientHub> clientHubManager,
            HubMethodInvocationMessage message)
        {
            await clientHubManager.InvokeAllAsync(message.Target, message.Arguments);
        }

        private static async Task InvokeAllExceptAsync(HubLifetimeManager<ClientHub> clientHubManager,
            HubMethodInvocationMessage message)
        {
            if (message.TryGetExcludedIds(out var excludedIds))
            {
                await clientHubManager.InvokeAllExceptAsync(message.Target, message.Arguments,
                    excludedIds);
            }
        }

        private static async Task InvokeGroupAsync(HubLifetimeManager<ClientHub> clientHubManager,
            HubMethodInvocationMessage message)
        {
            if (message.TryGetGroupName(out var groupName))
            {
                await clientHubManager.InvokeGroupAsync(groupName, message.Target,
                    message.Arguments);
            }
        }

        private static async Task AddGroupAsync(HubLifetimeManager<ClientHub> clientHubManager,
            HubMethodInvocationMessage message)
        {
            if (message.TryGetGroupName(out var groupName) &&
                message.TryGetConnectionId(out var connectionId))
            {
                await clientHubManager.AddGroupAsync(connectionId, groupName);
            }
        }

        private static async Task RemoveGroupAsync(HubLifetimeManager<ClientHub> clientHubManager,
            HubMethodInvocationMessage message)
        {
            if (message.TryGetGroupName(out var groupName) &&
                message.TryGetConnectionId(out var connectionId))
            {
                await clientHubManager.RemoveGroupAsync(connectionId, groupName);
            }
        }

        #endregion
    }
}