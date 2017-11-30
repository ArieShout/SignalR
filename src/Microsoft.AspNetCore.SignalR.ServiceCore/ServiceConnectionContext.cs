// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR.ServiceCore
{
    public class ServiceConnectionContext : ConnectionContext
    {
        public ServiceConnectionContext(string connectionId)
        {
            ConnectionId = connectionId;
        }
        public override string ConnectionId { get; set; }

        public override IFeatureCollection Features => throw new System.NotImplementedException();

        public override IDictionary<object, object> Metadata { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public override Channel<byte[]> Transport { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}
