// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Core;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public class ClientHubEndPoint<THub> : BaseHubEndPoint<THub> where THub : Hub
    {
        private readonly IHubMessageBroker _hubMessageBroker;

        public ClientHubEndPoint(HubLifetimeManager<THub> lifetimeManager,
                           IHubProtocolResolver protocolResolver,
                           IHubContext<THub> hubContext,
                           IOptions<HubOptions> hubOptions,
                           ILogger<ClientHubEndPoint<THub>> logger,
                           IServiceScopeFactory serviceScopeFactory,
                           IUserIdProvider userIdProvider,
                           IHubMessageBroker hubMessageBroker) : base(lifetimeManager, protocolResolver, hubContext, hubOptions, logger, serviceScopeFactory, userIdProvider)
        {
            _hubMessageBroker = hubMessageBroker;
        }

        protected override async Task OnHubConnectedAsync(string hubName, HubConnectionContext connection)
        {
            await _hubMessageBroker.OnClientConnectedAsync(hubName, connection);
        }

        protected override async Task OnHubDisconnectedAsync(string hubName, HubConnectionContext connection, Exception exception)
        {
            await _hubMessageBroker.OnClientDisconnectedAsync(hubName, connection);
        }

        protected override async Task OnHubInvocationAsync(string hubName, HubConnectionContext connection, HubMethodInvocationMessage message)
        {
            await _hubMessageBroker.PassThruClientMessage(hubName, connection, message);
        }

        protected override async Task OnHubCompletionAsync(string hubName, HubConnectionContext connection, CompletionMessage message)
        {
            await Task.CompletedTask;
        }
    }
}
