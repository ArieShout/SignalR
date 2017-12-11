// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using System.Security.Claims;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class ServiceHubConnectionContext : HubConnectionContext
    {
        private readonly ServiceConnectionContext _connectionContext;
        private readonly HubConnection _hubConnection;
        private ClaimsPrincipal _user;
        public ServiceHubConnectionContext(ServiceConnectionContext connectionContext,
            ChannelWriter<HubMessage> output, HubConnection hubConnection)
            : base(output, null)
        {
            _connectionContext = connectionContext;
            _hubConnection = hubConnection;
            // Temporarily use connection Id as user
            var claims = new List<Claim>() {
                new Claim(ClaimTypes.Name, _connectionContext.ConnectionId)
            };
            var id = new ClaimsIdentity(claims);
            _user = new ClaimsPrincipal(id);
        }

        public override string ConnectionId => _connectionContext.ConnectionId;
        public override ClaimsPrincipal User => _user;
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
