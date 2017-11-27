using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubMessageBroker : IHubMessageBroker
    {
        private const string ConnectionIdKeyName = "connId";
        private const string GroupNameKeyName = "groupName";
        private const string ExcludedIdsKeyName = "excluded";
        private readonly ConcurrentDictionary<string, HubLifetimeManager<ClientHub>> _clientHubManagerDict = new ConcurrentDictionary<string, HubLifetimeManager<ClientHub>>();
        private readonly ConcurrentDictionary<string, HubLifetimeManager<ServerHub>> _serverHubManagerDict = new ConcurrentDictionary<string, HubLifetimeManager<ServerHub>>();
        private readonly ConcurrentDictionary<string, string> _pendingClientCalls = new ConcurrentDictionary<string, string>();

        #region Client connections

        public async Task OnClientConnectedAsync(string hubName, HubConnectionContext2 context)
        {
            var clientHubManager = _clientHubManagerDict.GetOrAdd(hubName, new DefaultHubLifetimeManager<ClientHub>());
            await clientHubManager.OnConnectedAsync(context);
        }

        public async Task OnClientDisconnectedAsync(string hubName, HubConnectionContext2 context)
        {
            var clientHubManager = _clientHubManagerDict[hubName];
            if (clientHubManager != null)
            {
                await clientHubManager.OnDisconnectedAsync(context);
            }
        }

        public async Task PassThruClientMessage(string hubName, HubConnectionContext2 context, HubMethodInvocationMessage message)
        {
            var serverHubManager = _serverHubManagerDict[hubName];
            if (serverHubManager != null)
            {
                _pendingClientCalls.TryAdd(message.InvocationId, context.ConnectionId);
                // Add original connection Id to message metadata
                message.Metadata.Add(ConnectionIdKeyName, context.ConnectionId);

                // TODO: Assign one server for this client connection
                await serverHubManager.InvokeAllAsync(message.Target, message.Arguments);
            }
        }

        #endregion

        #region Server connections

        public async Task OnServerConnectedAsync(string hubName, HubConnectionContext2 context)
        {
            var serverHubManager = _serverHubManagerDict.GetOrAdd(hubName, new DefaultHubLifetimeManager<ServerHub>());
            await serverHubManager.OnConnectedAsync(context);
        }

        public async Task OnServerDisconnectedAsync(string hubName, HubConnectionContext2 context)
        {
            var serverHubManager = _serverHubManagerDict[hubName];
            if (serverHubManager != null)
            {
                await serverHubManager.OnDisconnectedAsync(context);
            }
        }

        public async Task PassThruServerMessage(string hubName, HubConnectionContext2 context, HubMethodInvocationMessage message)
        {
            var clientHubManager = _clientHubManagerDict[hubName];
            if (clientHubManager != null)
            {
                // Invoke a single connection
                if (message.Metadata.TryGetValue(ConnectionIdKeyName, out var connectionId))
                {
                    await clientHubManager.InvokeConnectionAsync(connectionId, message.Target,
                        message.Arguments);
                }
                // Invoke a group
                else if (message.Metadata.TryGetValue(GroupNameKeyName, out var groupName))
                {
                    await clientHubManager.InvokeGroupAsync(groupName, message.Target,
                        message.Arguments);
                }
                // Invoke all except
                else if (message.Metadata.TryGetValue(ExcludedIdsKeyName, out var excludedIds))
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

        public async Task PassThruServerMessage(string hubName, HubConnectionContext2 context, CompletionMessage message)
        {
            var clientHubManager = _clientHubManagerDict[hubName];
            if (clientHubManager != null)
            {
                if (_pendingClientCalls.TryRemove(message.InvocationId, out var connectionId))
                {
                    await ((DefaultHubLifetimeManager<ClientHub>)clientHubManager).SendMessageAsync(connectionId, message);
                }
            }
        }

        #endregion
    }
}
