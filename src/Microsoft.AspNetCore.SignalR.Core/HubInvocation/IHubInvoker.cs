// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    // Bundle hub method discovery and invocation together
    public interface IHubInvoker<THub> : IInvocationBinder where THub : Hub
    {
        Task OnConnectedAsync(HubConnectionContext connection);

        Task OnDisconnectedAsync(HubConnectionContext connection, Exception exception);

        Task OnInvocationAsync(HubConnectionContext connection, HubMethodInvocationMessage message,
            bool isStreamedInvocation);

        Task OnCompletionAsync(HubConnectionContext connection, CompletionMessage message);
    }
}
