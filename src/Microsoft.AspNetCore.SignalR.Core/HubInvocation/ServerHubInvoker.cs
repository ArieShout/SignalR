// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServerHubInvoker : IHubInvoker<ServerHub>
    {
        private readonly IHubMessageBroker _hubMessageBroker;
        private readonly IHubStatusManager _hubStatusManager;

        public ServerHubInvoker(IHubMessageBroker hubMessageBroker, IHubStatusManager hubStatusManager)
        {
            _hubMessageBroker = hubMessageBroker;
            _hubStatusManager = hubStatusManager;
        }

        public Type GetReturnType(string invocationId)
        {
            return typeof(object);
        }

        public Type[] GetParameterTypes(string methodName)
        {
            return null;
        }

        public async Task OnConnectedAsync(HubConnectionContext connection)
        {
            var hubName = GetHubName(connection);
            await _hubMessageBroker.OnServerConnectedAsync(hubName, connection);
            _ = _hubStatusManager.AddServerConnection(hubName);
        }

        public async Task OnDisconnectedAsync(HubConnectionContext connection, Exception exception)
        {
            var hubName = GetHubName(connection);
            await _hubMessageBroker.OnServerDisconnectedAsync(hubName, connection);
            _ = _hubStatusManager.RemoveServerConnection(hubName);
        }

        public async Task OnInvocationAsync(HubConnectionContext connection, HubMethodInvocationMessage message, bool isStreamedInvocation)
        {
            var hubName = GetHubName(connection);
            await _hubMessageBroker.PassThruServerMessage(hubName, connection, message);
            _ = _hubStatusManager.AddServerMessage(hubName);
        }

        public async Task OnCompletionAsync(HubConnectionContext connection, CompletionMessage message)
        {
            var hubName = GetHubName(connection);
            await _hubMessageBroker.PassThruServerMessage(hubName, connection, message);
            _ = _hubStatusManager.AddServerMessage(hubName);
        }

        private string GetHubName(HubConnectionContext connection) => connection.GetHubName() ??
                                                                      throw new Exception(
                                                                          $"No specified hub binded to connection: {connection.ConnectionId}");
    }
}
