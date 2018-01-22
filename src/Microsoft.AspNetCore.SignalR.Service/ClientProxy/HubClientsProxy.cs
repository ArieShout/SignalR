using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Service
{
    public class HubClientsProxy<THub> : IHubClients<IServiceClientProxy> where THub : Hub
    {
        private readonly SignalR _signalr;

        public HubClientsProxy(SignalR signalr)
        {
            _signalr = signalr;
            All = _signalr.CreateAllClientsProxy<THub>();
        }

        public IServiceClientProxy All { get; }

        public IServiceClientProxy AllExcept(IReadOnlyList<string> excludedIds)
        {
            return _signalr.CreateAllClientsExceptProxy<THub>(excludedIds);
        }

        public IServiceClientProxy Client(string connectionId)
        {
            return _signalr.CreateSingleClientProxy<THub>(connectionId);
        }

        public IServiceClientProxy Clients(IReadOnlyList<string> connectionIds)
        {
            return _signalr.CreateMultipleClientProxy<THub>(connectionIds);
        }

        public IServiceClientProxy Group(string groupName)
        {
            return _signalr.CreateSingleGroupProxy<THub>(groupName);
        }

        public IServiceClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            return _signalr.CreateMultipleGroupProxy<THub>(groupNames);
        }

        public IServiceClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludeIds)
        {
            return _signalr.CreateSingleGroupExceptProxy<THub>(groupName, excludeIds);
        }

        public IServiceClientProxy User(string userId)
        {
            return _signalr.CreateSingleUserProxy<THub>(userId);
        }

        public IServiceClientProxy Users(IReadOnlyList<string> userIds)
        {
            return _signalr.CreateMultipleUserProxy<THub>(userIds);
        }
    }
}
