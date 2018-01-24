using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    internal static class ServiceProxyExtensions
    {
        public static ServiceClientProxy CreateAllClientsProxy(this SignalR signalr, string hubName)
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{hubName}",
                () => signalr.GenerateServerToken(hubName));
        }

        public static ServiceClientProxy CreateAllClientsExceptProxy(this SignalR signalr, string hubName, IReadOnlyList<string> excludedIds)
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{hubName}",
                () => signalr.GenerateServerToken(hubName), excludedIds);
        }

        public static ServiceClientProxy CreateSingleClientProxy(this SignalR signalr, string hubName, string connectionId)
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{hubName}/connection/{connectionId}",
                () => signalr.GenerateServerToken(hubName));
        }

        public static ServiceClientProxy CreateMultipleClientProxy(this SignalR signalr, string hubName, IReadOnlyList<string> connectionIds)
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{hubName}/connections/{string.Join(",", connectionIds)}",
                () => signalr.GenerateServerToken(hubName));
        }

        public static ServiceClientProxy CreateSingleUserProxy(this SignalR signalr, string hubName, string userId)
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{hubName}/user/{userId}",
                () => signalr.GenerateServerToken(hubName));
        }

        public static ServiceClientProxy CreateMultipleUserProxy(this SignalR signalr, string hubName, IReadOnlyList<string> userIds)
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{hubName}/users/{string.Join(",", userIds)}",
                () => signalr.GenerateServerToken(hubName));
        }

        public static ServiceClientProxy CreateSingleGroupProxy(this SignalR signalr, string hubName, string groupName)
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{hubName}/group/{groupName}",
                () => signalr.GenerateServerToken(hubName));
        }

        public static ServiceClientProxy CreateMultipleGroupProxy(this SignalR signalr, string hubName, IReadOnlyList<string> groupNames)
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{hubName}/groups/{string.Join(",", groupNames)}",
                () => signalr.GenerateServerToken(hubName));
        }

        public static ServiceClientProxy CreateSingleGroupExceptProxy(this SignalR signalr, string hubName, string groupName,
            IReadOnlyList<string> excludedIds)
        {
            return new ServiceClientProxy(
                $"http://{signalr.HostName}/{signalr.ApiVersion}/hub/{hubName}/group/{groupName}",
                () => signalr.GenerateServerToken(hubName), excludedIds);
        }
    }
}