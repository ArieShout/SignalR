// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using System.Security.Claims;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class ServiceHubConnectionContext : HubConnectionContext
    {
        private readonly ServiceConnectionContext _connectionContext;
        private readonly HubConnection _hubConnection;
        private readonly ServiceHubOptions _hubOptions;
        public ServiceHubConnectionContext(ServiceConnectionContext connectionContext,
            ChannelWriter<HubMessage> output, HubConnection hubConnection, ServiceHubOptions hubOptions)
            : base(output, null)
        {
            _connectionContext = connectionContext;
            _hubConnection = hubConnection;
            _hubOptions = hubOptions;
        }

        public HubConnection HubConnection => _hubConnection;
        public override string ConnectionId => _connectionContext.ConnectionId;

        public override ClaimsPrincipal User => _connectionContext.User;

        public async Task InvokeAsync(string methodName, object[] args)
        {
            await _hubConnection.InvokeAsync(ConnectionId, methodName, args);
        }

        public async Task InvokeAsync(InvocationMessage message)
        {
            await _hubConnection.InvokeAsync(message);
        }

        public async Task SendAsync(HubMessage hubMessage)
        {
            if (_hubOptions.MessagePassingType == MessagePassingType.AsyncCall)
            {
                await _hubConnection.SendHubMessage(hubMessage);
            }
            else
            {
                while (await Output.WaitToWriteAsync())
                {
                    if (Output.TryWrite(hubMessage))
                    {
                        break;
                    }
                }
            }
        }

        private string GetNextInvocationId()
        {
            return _hubConnection.GetNextId();
        }
    }
}
