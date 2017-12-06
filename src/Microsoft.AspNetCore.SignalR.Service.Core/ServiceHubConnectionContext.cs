// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.ServiceCore
{
    public class ServiceHubConnectionContext : HubConnectionContext
    {
        private readonly ServiceConnectionContext _connectionContext;
        private readonly HubConnection _hubConnection;
        public ServiceHubConnectionContext(ServiceConnectionContext connectionContext, HubConnection hubConnection)
            : base(null, null)
        {
            _connectionContext = connectionContext;
            _hubConnection = hubConnection;
        }
        public override string ConnectionId => _connectionContext.ConnectionId;

        public InvocationMessage CreateInvocationMessage(string methodName, object[] args)
        {
            var invocationMessage = new InvocationMessage(GetNextInvocationId(),
                nonBlocking: false, target: methodName,
                argumentBindingException: null, arguments: args);
            return invocationMessage;
        }

        public async Task InvokeAsync(string methodName, object[] args)
        {
            await _hubConnection.InvokeAsync(ConnectionId, methodName, args);
        }

        public async Task InvokeAsync(InvocationMessage message)
        {
            await _hubConnection.InvokeAsync(message);
        }
        
        private string GetNextInvocationId()
        {
            return _hubConnection.GetNextId();
        }
    }
}
