using System.Threading.Channels;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR
{
    public class ServiceConnectionContext : DefaultConnectionContext
    {
        public ServiceConnectionContext(string connectionId, Channel<byte[]> transport, Channel<byte[]> application) :
            base(connectionId, transport, application)
        {
        }

        public override Channel<byte[]> Transport { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}
