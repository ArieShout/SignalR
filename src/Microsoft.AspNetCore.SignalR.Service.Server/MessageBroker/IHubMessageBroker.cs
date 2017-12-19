using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubMessageBroker
    {
        Task OnClientConnectedAsync(string hubName, HubConnectionContext context);
        Task OnClientDisconnectedAsync(string hubName, HubConnectionContext context);
        Task PassThruClientMessage(string hubName, HubConnectionContext connection, HubMethodInvocationMessage message);
        Task OnServerConnectedAsync(string hubName, HubConnectionContext context);
        Task OnServerDisconnectedAsync(string hubName, HubConnectionContext context);
        Task PassThruServerMessage(string hubName, HubConnectionContext context, HubMethodInvocationMessage message);
        Task PassThruServerMessage(string hubName, HubConnectionContext context, CompletionMessage message);
    }
}