using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    internal static class ServiceProxyExtensions
    {
        public static ServiceClientProxy CreateAllClientsProxy<THub>(this SignalR signalr) where THub : Hub
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{typeof(THub).Name.ToLower()}",
                signalr.GenerateServerToken<THub>);
        }

        public static ServiceClientProxy CreateAllClientsExceptProxy<THub>(this SignalR signalr, IReadOnlyList<string> excludedIds)
            where THub : Hub
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{typeof(THub).Name.ToLower()}",
                signalr.GenerateServerToken<THub>, excludedIds);
        }

        public static ServiceClientProxy CreateSingleClientProxy<THub>(this SignalR signalr, string connectionId) where THub : Hub
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{typeof(THub).Name.ToLower()}/connection/{connectionId}",
                signalr.GenerateServerToken<THub>);
        }

        public static ServiceClientProxy CreateMultipleClientProxy<THub>(this SignalR signalr, IReadOnlyList<string> connectionIds) where THub : Hub
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{typeof(THub).Name.ToLower()}/connections/{string.Join(",", connectionIds)}",
                signalr.GenerateServerToken<THub>);
        }

        public static ServiceClientProxy CreateSingleUserProxy<THub>(this SignalR signalr, string userId) where THub : Hub
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{typeof(THub).Name.ToLower()}/user/{userId}",
                signalr.GenerateServerToken<THub>);
        }

        public static ServiceClientProxy CreateMultipleUserProxy<THub>(this SignalR signalr, IReadOnlyList<string> userIds)
            where THub : Hub
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{typeof(THub).Name.ToLower()}/users/{string.Join(",", userIds)}",
                signalr.GenerateServerToken<THub>);
        }

        public static ServiceClientProxy CreateSingleGroupProxy<THub>(this SignalR signalr, string groupName) where THub : Hub
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{typeof(THub).Name.ToLower()}/group/{groupName}",
                signalr.GenerateServerToken<THub>);
        }

        public static ServiceClientProxy CreateMultipleGroupProxy<THub>(this SignalR signalr, IReadOnlyList<string> groupNames)
            where THub : Hub
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{typeof(THub).Name.ToLower()}/groups/{string.Join(",", groupNames)}",
                signalr.GenerateServerToken<THub>);
        }

        public static ServiceClientProxy CreateSingleGroupExceptProxy<THub>(this SignalR signalr, string groupName,
            IReadOnlyList<string> excludedIds) where THub : Hub
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{typeof(THub).Name.ToLower()}/group/{groupName}",
                signalr.GenerateServerToken<THub>, excludedIds);
        }
    }
}