// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Features;

namespace Microsoft.AspNetCore.SignalR.Service.Core
{
    public class ServiceConnectionContext : ConnectionContext,
        IConnectionUserFeature
    {
        public ServiceConnectionContext(string connectionId)
        {
            ConnectionId = connectionId;
        }
        public override string ConnectionId { get; set; }

        public override IFeatureCollection Features => throw new System.NotImplementedException();

        public override IDictionary<object, object> Metadata { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public override Channel<byte[]> Transport { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public ClaimsPrincipal User { get; set; }
    }
}
