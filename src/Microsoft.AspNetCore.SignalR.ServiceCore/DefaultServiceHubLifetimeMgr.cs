// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.ServiceCore.API;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using System.Linq;

namespace Microsoft.AspNetCore.SignalR.ServiceCore
{
    public class DefaultServiceHubLifetimeMgr<THub> : ServiceHubLifetimeMgr<THub>
    {
        private readonly ServiceHubConnectionList _connections = new ServiceHubConnectionList();

        public override ServiceHubConnectionList Connections => _connections;

        public override Task AddGroupAsync(string connectionId, string groupName)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override Task InvokeUserAsync(string userId, string methodName, object[] args)
        {
            throw new NotImplementedException();
        }

        public override Task OnConnectedAsync(ServiceHubConnectionContext connection)
        {
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(ServiceHubConnectionContext connection)
        {
            _connections.Remove(connection);
            return Task.CompletedTask;
        }

        public override Task RemoveGroupAsync(string connectionId, string groupName)
        {
            throw new NotImplementedException();
        }

        private Task InvokeAllWhere(string methodName, object[] args, Func<ServiceHubConnectionContext, bool> include)
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

        private async Task WriteAsync(ServiceHubConnectionContext connection, string methodName, object[] args)
        {
            await connection.InvokeAsync(methodName, args);
        }
    }
}
