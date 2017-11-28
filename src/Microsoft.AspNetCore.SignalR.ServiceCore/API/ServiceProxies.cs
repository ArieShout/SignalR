// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.ServiceCore.API
{
    public class AllServiceClientProxy<THub> : IServiceClientProxy
    {
        private readonly ServiceHubLifetimeMgr<THub> _lifetimeMgr;
        public AllServiceClientProxy(ServiceHubLifetimeMgr<THub> lifetimeMgr)
        {
            _lifetimeMgr = lifetimeMgr;
        }

        public Task InvokeAsync(string method, object[] args)
        {
            return _lifetimeMgr.InvokeAllAsync(method, args);
        }
    }

    public class SingleServiceClientProxy<THub> : IServiceClientProxy
    {
        private readonly string _connectionId;
        private readonly ServiceHubLifetimeMgr<THub> _lifetimeMgr;
        public SingleServiceClientProxy(ServiceHubLifetimeMgr<THub> lifetimeMgr, string connetionId)
        {
            _lifetimeMgr = lifetimeMgr;
            _connectionId = connetionId;
        }
        public Task InvokeAsync(string method, params object[] args)
        {
            return _lifetimeMgr.InvokeConnectionAsync(_connectionId, method, args);
        }
    }

    public class AllServiceClientsExceptProxy<THub> : IServiceClientProxy
    {
        private readonly ServiceHubLifetimeMgr<THub> _lifetimeMgr;
        private IReadOnlyList<string> _excludedIds;
        public AllServiceClientsExceptProxy(ServiceHubLifetimeMgr<THub> lifetimeMgr, IReadOnlyList<string> excludedIds)
        {
            _lifetimeMgr = lifetimeMgr;
            _excludedIds = excludedIds;
        }

        public Task InvokeAsync(string method, params object[] args)
        {
            return _lifetimeMgr.InvokeAllExceptAsync(method, args, _excludedIds);
        }
    }
}
