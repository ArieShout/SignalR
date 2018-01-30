// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Latency
{
    public class HubWithPresence : Hub
    {
        private IUserTracker<HubWithPresence> _userTracker;

        public HubWithPresence(IUserTracker<HubWithPresence> userTracker)
        {
            _userTracker = userTracker;
        }

        public long GetUsersOnline()
        {
            return _userTracker.Users();
        }

        public Task OnUsersJoined()
        {
            _userTracker.AddUser();
            return Task.CompletedTask;
        }

        public Task OnUsersLeft()
        {
            _userTracker.RemoveUser();
            return Task.CompletedTask;
        }
    }
}
