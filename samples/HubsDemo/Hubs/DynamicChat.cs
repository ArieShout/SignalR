// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;

namespace SocketsSample.Hubs
{
    public class DynamicChat : DynamicHub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.Client(Context.ConnectionId).onConnected(Context.ConnectionId);
            await Clients.All.Send($"{Context.ConnectionId} joined");
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            await Clients.All.Send($"{Context.ConnectionId} left");
        }

        public Task AllExceptOne(string connectionId, string message)
        {
            return Clients.AllExcept(new List<string>(new string[] { connectionId })).Send($"broadcast all except for {connectionId}: {message}");
        }

        public Task Send(string message)
        {
            return Clients.All.Send($"{Context.ConnectionId}: {message}");
        }

        public Task SendToGroup(string groupName, string message)
        {
            return Clients.Group(groupName).Send($"{Context.ConnectionId}@{groupName}: {message}");
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddAsync(Context.ConnectionId, groupName);

            await Clients.Group(groupName).Send($"{Context.ConnectionId} joined {groupName}");
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveAsync(Context.ConnectionId, groupName);

            await Clients.Group(groupName).Send($"{Context.ConnectionId} left {groupName}");
        }

        public Task Echo(string message)
        {
            return Clients.Client(Context.ConnectionId).Send($"{Context.ConnectionId}: {message}");
        }
    }
}
