// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.ServiceCore.API
{
    public class ServiceHubContext<THub> : IServiceHubContext<THub>, IServiceHubClients where THub : ServiceHub
    {
        private readonly ServiceHubLifetimeMgr<THub> _lifetimeMgr;
        public ServiceHubContext(ServiceHubLifetimeMgr<THub> lifetimeMgr)
        {
            _lifetimeMgr = lifetimeMgr;
            All = new AllServiceClientProxy<THub>(lifetimeMgr);
            Groups = new GroupManager<THub>(lifetimeMgr);
        }
        IServiceHubClients IServiceHubContext<THub>.Clients => this;

        public virtual IServiceClientProxy All { get; }

        public IServiceGroupManager Groups { get; }

        public virtual IServiceClientProxy AllExcept(IReadOnlyList<string> excludedIds)
        {
            return new AllServiceClientsExceptProxy<THub>(_lifetimeMgr, excludedIds);
        }

        public virtual IServiceClientProxy Client(string connectionId)
        {
            return new SingleServiceClientProxy<THub>(_lifetimeMgr, connectionId);
        }

        public virtual IServiceClientProxy Group(string groupName)
        {
            return new ServiceGroupProxy<THub>(_lifetimeMgr, groupName);
        }

        public virtual IServiceClientProxy User(string userId)
        {
            throw new NotImplementedException();
        }
    }
}
