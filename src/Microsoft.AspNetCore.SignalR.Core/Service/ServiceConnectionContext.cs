using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Features;

namespace Microsoft.AspNetCore.SignalR
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
