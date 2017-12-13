// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public interface IHubStatusManager
    {
        Task AddClientConnection(string hubName);

        Task AddServerConnection(string hubName);

        Task RemoveClientConnection(string hubName);

        Task RemoveServerConnection(string hubName);

        Task AddClientMessage(string hubName);

        Task AddServerMessage(string hubName);

        Task GetHubStatus(HttpContext context);
    }
}
