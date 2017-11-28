using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubMessageBroker
    {
        Task OnClientConnectedAsync(string hubName, HubConnectionContext2 context);
        Task OnClientDisconnectedAsync(string hubName, HubConnectionContext2 context);
        Task PassThruClientMessage(string hubName, HubConnectionContext2 context, HubMethodInvocationMessage message);
        Task OnServerConnectedAsync(string hubName, HubConnectionContext2 context);
        Task OnServerDisconnectedAsync(string hubName, HubConnectionContext2 context);
        Task PassThruServerMessage(string hubName, HubConnectionContext2 context, HubMethodInvocationMessage message);
        Task PassThruServerMessage(string hubName, HubConnectionContext2 context, CompletionMessage message);
    }
}