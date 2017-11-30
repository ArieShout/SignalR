// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR.ServiceCore.API
{
    public class ServiceHubConnectionContext
    {
        private readonly ServiceConnectionContext _connectionContext;
        private readonly HubConnection _hubConnection;
        public ServiceHubConnectionContext(ServiceConnectionContext connectionContext, HubConnection hubConnection)
        {
            _connectionContext = connectionContext;
            _hubConnection = hubConnection;
        }
        public virtual string ConnectionId => _connectionContext.ConnectionId;
        public async Task InvokeAsync(string methodName, object[] args)
        {
            await _hubConnection.InvokeAsync(ConnectionId, methodName, args);
        }
    }
}
