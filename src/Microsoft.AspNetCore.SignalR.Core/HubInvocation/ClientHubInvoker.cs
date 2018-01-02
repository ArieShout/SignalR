// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    public class ClientHubInvoker : IHubInvoker<ClientHub>
    {
        private readonly IHubMessageBroker _hubMessageBroker;
        private readonly IHubStatusManager _hubStatusManager;

        public ClientHubInvoker(IHubMessageBroker hubMessageBroker, IHubStatusManager hubStatusManager)
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
            throw new NotImplementedException();
        }

        public async Task OnConnectedAsync(HubConnectionContext connection)
        {
            var hubName = GetHubName(connection);
            await _hubMessageBroker.OnClientConnectedAsync(hubName, connection);
            _ = _hubStatusManager.AddClientConnection(hubName);
        }

        public async Task OnDisconnectedAsync(HubConnectionContext connection, Exception exception)
        {
            var hubName = GetHubName(connection);
            await _hubMessageBroker.OnClientDisconnectedAsync(hubName, connection);
            _ = _hubStatusManager.RemoveClientConnection(hubName);
        }

        public async Task OnInvocationAsync(HubConnectionContext connection, HubMethodInvocationMessage message, bool isStreamedInvocation)
        {
            var hubName = GetHubName(connection);
            await _hubMessageBroker.PassThruClientMessage(hubName, connection, message);
            _ = _hubStatusManager.AddClientMessage(hubName);
        }

        public async Task OnCompletionAsync(HubConnectionContext connection, CompletionMessage message)
        {
            await Task.CompletedTask;
        }

        private string GetHubName(HubConnectionContext connection) => connection.GetHubName() ??
                                                                      throw new Exception(
                                                                          $"No specified hub binded to connection: {connection.ConnectionId}");
    }
}
