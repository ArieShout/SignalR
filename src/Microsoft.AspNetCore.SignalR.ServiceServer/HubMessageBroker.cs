using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubMessageBroker
    {
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

        public async Task PassThruClientMessage(string hubName, HubConnectionContext2 context, HubMethodInvocationMessage invocationMessage)
        {
            var serverHubManager = _serverHubManagerDict[hubName];
            if (serverHubManager != null)
            {
                _pendingClientCalls.TryAdd(invocationMessage.InvocationId, context.ConnectionId);
                // TODO: Assign one server for this client connection
                await serverHubManager.InvokeAllAsync(invocationMessage.Target, invocationMessage.Arguments);
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

        public async Task PassThruServerMessage(string hubName, HubConnectionContext2 context, HubMethodInvocationMessage invocationMessage)
        {
            var clientHubManager = _clientHubManagerDict[hubName];
            if (clientHubManager != null)
            {
                // TODO: check the target of invocation, whether a connection, a group or all.
                await clientHubManager.InvokeAllAsync(invocationMessage.Target, invocationMessage.Arguments);
            }
        }

        public async Task PassThruServerMessage(string hubName, HubConnectionContext2 context, CompletionMessage completionMessage)
        {
            var clientHubManager = _clientHubManagerDict[hubName];
            if (clientHubManager != null)
            {
                if (_pendingClientCalls.TryRemove(completionMessage.InvocationId, out var connectionId))
                {
                    await ((DefaultHubLifetimeManager<ClientHub>)clientHubManager).SendMessageAsync(connectionId, completionMessage);
                }
            }
        }

        #endregion
    }
}
