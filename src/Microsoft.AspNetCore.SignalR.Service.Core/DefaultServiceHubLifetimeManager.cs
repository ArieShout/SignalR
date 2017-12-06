// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using System.Linq;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.AspNetCore.SignalR.ServiceCore
{
    public class DefaultServiceHubLifetimeManager<THub> : HubLifetimeManager<THub>
    {
        private readonly HubConnectionList _connections = new HubConnectionList();
        private readonly HubGroupList _groups = new HubGroupList();
        public override HubConnectionList Connections => _connections;
        public override Task AddGroupAsync(string connectionId, string groupName)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var connection = _connections[connectionId];
            if (connection == null)
            {
                return Task.CompletedTask;
            }

            _groups.Add(connection, groupName);

            return Task.CompletedTask;
        }

        public override Task InvokeAllAsync(string methodName, object[] args)
        {
            // Send the message to SignalR Service through any existing connection.
            // Here, we choose the first connection.
            HubConnectionContext connection = _connections.ElementAt(0);
            InvocationMessage message = ((ServiceHubConnectionContext)connection).CreateInvocationMessage(methodName, args);
            return ((ServiceHubConnectionContext)connection).InvokeAsync(message);
        }

        public override Task InvokeAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedIds)
        {
            HubConnectionContext connection = _connections.ElementAt(0);
            InvocationMessage message = CreateInvocationMessageWithExcludedIds(connection, excludedIds, methodName, args);
            return ((ServiceHubConnectionContext)connection).InvokeAsync(message);
        }

        public override Task InvokeConnectionAsync(string connectionId, string methodName, object[] args)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            var connection = _connections[connectionId];

            if (connection == null)
            {
                return Task.CompletedTask;
            }
            InvocationMessage message = CreateInvocationMessageWithConnectionId(connection, methodName, args);
            return ((ServiceHubConnectionContext)connection).InvokeAsync(message);
        }

        public override Task InvokeGroupAsync(string groupName, string methodName, object[] args)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var group = _groups[groupName];
            if (group != null)
            {
                HubConnectionContext connection = _connections.ElementAt(0);
                InvocationMessage message = CreateInvocationMessageWithGroupName(connection, groupName, methodName, args);
                return ((ServiceHubConnectionContext)connection).InvokeAsync(message);
            }

            return Task.CompletedTask;
        }

        public override Task InvokeUserAsync(string userId, string methodName, object[] args)
        {
            throw new NotImplementedException();
        }

        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            _connections.Remove(connection);
            return Task.CompletedTask;
        }

        public override Task RemoveGroupAsync(string connectionId, string groupName)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var connection = _connections[connectionId];
            if (connection == null)
            {
                return Task.CompletedTask;
            }

            _groups.Remove(connectionId, groupName);

            return Task.CompletedTask;
        }

        private InvocationMessage CreateInvocationMessageWithConnectionId(HubConnectionContext connection, string methodName, object[] args)
        {
            var invocationMessage = ((ServiceHubConnectionContext)connection).CreateInvocationMessage(methodName, args);
            invocationMessage.AddConnectionId(connection.ConnectionId);
            return invocationMessage;
        }

        private InvocationMessage CreateInvocationMessageWithGroupName(HubConnectionContext connection, string groupName, string methodName, object[] args)
        {
            var invocationMessage = ((ServiceHubConnectionContext)connection).CreateInvocationMessage(methodName, args);
            invocationMessage.AddGroupName(groupName);
            return invocationMessage;
        }

        private InvocationMessage CreateInvocationMessageWithExcludedIds(HubConnectionContext connection, IReadOnlyList<string> excludedIds,
            string methodName, object[] args)
        {
            var invocationMessage = ((ServiceHubConnectionContext)connection).CreateInvocationMessage(methodName, args);
            invocationMessage.AddExcludedIds(excludedIds);
            return invocationMessage;
        }
    }
}
