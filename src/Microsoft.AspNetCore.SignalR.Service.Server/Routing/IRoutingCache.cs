using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public interface IRoutingCache
    {
        bool TryGetTarget(HubConnectionContext connection, out RouteTarget targetConnId);

        Task SetTargetAsync(HubConnectionContext connection, RouteTarget targetConnId);

        Task RemoveTargetAsync(HubConnectionContext connection);

        Task DelayRemoveTargetAsync(HubConnectionContext connection, RouteTarget targetConnId);
    }
}
