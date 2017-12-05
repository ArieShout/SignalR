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
    public class DefaultServiceHubLifetimeMgr<THub> : HubLifetimeManager<THub>
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
            return InvokeAllWhere(methodName, args, c => true);
        }

        public override Task InvokeAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedIds)
        {
            return InvokeAllWhere(methodName, args, connection =>
            {
                return !excludedIds.Contains(connection.ConnectionId);
            });
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
            return WriteAsync(connection, methodName, args);
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
                var tasks = group.Values.Select(c => WriteAsync(c, methodName, args));
                return Task.WhenAll(tasks);
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

        private Task InvokeAllWhere(string methodName, object[] args, Func<HubConnectionContext, bool> include)
        {
            var tasks = new List<Task>(_connections.Count);
            foreach (var connection in _connections)
            {
                if (!include(connection))
                {
                    continue;
                }
                tasks.Add(WriteAsync(connection, methodName, args));
            }

            return Task.WhenAll(tasks);
        }

        private async Task WriteAsync(HubConnectionContext connection, string methodName, object[] args)
        {
            await ((ServiceHubConnectionContext)connection).InvokeAsync(methodName, args);
        }
    }
}
