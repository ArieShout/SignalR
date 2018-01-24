using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubClientsProxy : IHubClients<IServiceClientProxy>
    {
        private readonly SignalR _signalr;
        private readonly string _hubName;

        public HubClientsProxy(SignalR signalr, string hubName)
        {
            _signalr = signalr;
            _hubName = hubName;
            All = _signalr.CreateAllClientsProxy(_hubName);
        }

        public IServiceClientProxy All { get; }

        public IServiceClientProxy AllExcept(IReadOnlyList<string> excludedIds)
        {
            return _signalr.CreateAllClientsExceptProxy(_hubName, excludedIds);
        }

        public IServiceClientProxy Client(string connectionId)
        {
            return _signalr.CreateSingleClientProxy(_hubName, connectionId);
        }

        public IServiceClientProxy Clients(IReadOnlyList<string> connectionIds)
        {
            return _signalr.CreateMultipleClientProxy(_hubName, connectionIds);
        }

        public IServiceClientProxy Group(string groupName)
        {
            return _signalr.CreateSingleGroupProxy(_hubName, groupName);
        }

        public IServiceClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            return _signalr.CreateMultipleGroupProxy(_hubName, groupNames);
        }

        public IServiceClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludeIds)
        {
            return _signalr.CreateSingleGroupExceptProxy(_hubName, groupName, excludeIds);
        }

        public IServiceClientProxy User(string userId)
        {
            return _signalr.CreateSingleUserProxy(_hubName, userId);
        }

        public IServiceClientProxy Users(IReadOnlyList<string> userIds)
        {
            return _signalr.CreateMultipleUserProxy(_hubName, userIds);
        }
    }
}
