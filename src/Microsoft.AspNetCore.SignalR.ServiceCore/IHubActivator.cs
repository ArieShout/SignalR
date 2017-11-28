// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR.ServiceCore.API;

namespace Microsoft.AspNetCore.SignalR.ServiceCore
{
    public interface IHubActivator<THub> where THub : ServiceHub
    {
        THub Create();
        void Release(THub hub);
    }
}
