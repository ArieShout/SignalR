// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.ServiceCore.API;

namespace Microsoft.AspNetCore.SignalR.ServiceCore
{
    public class DefaultServiceHubLifetimeMgr<THub> : ServiceHubLifetimeMgr<THub>
    {
        public override Task AddGroupAsync(string connectionId, string groupName)
        {
            throw new NotImplementedException();
        }

        public override Task InvokeAllAsync(string methodName, object[] args)
        {
            throw new NotImplementedException();
        }

        public override Task InvokeAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedIds)
        {
            throw new NotImplementedException();
        }

        public override Task InvokeConnectionAsync(string connectionId, string methodName, object[] args)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override Task OnDisconnectedAsync(ServiceHubConnectionContext connection)
        {
            throw new NotImplementedException();
        }

        public override Task RemoveGroupAsync(string connectionId, string groupName)
        {
            throw new NotImplementedException();
        }
    }
}
