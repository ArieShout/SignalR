using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SignalRServiceSample
{
    //[Authorize]
    public class SignalRService : Hub
    {
        public async Task<MessageResult> Publish(string channelName, string eventName, byte[] data)
        {
            if (!IsValidGroupName(channelName))
            {
                return MessageResult.Error(null, "Illegal channel name: ${channelName}");
            }

            if (!IsAuthorized(channelName, Context.User))
            {
                return MessageResult.Error(null, "Permission denied. Not authorized to subsribe channel: ${channelName}");
            }

            await Clients.Group(channelName).InvokeAsync(eventName, data);
            return MessageResult.Success();
        }

        public async Task<MessageResult> Subscribe(string channelName)
        {
            if (!IsValidGroupName(channelName))
            {
                return MessageResult.Error(null, "Illegal channel name: ${channelName}");
            }

            if (!IsAuthorized(channelName, Context.User))
            {
                return MessageResult.Error(null, "Permission denied. Not authorized to subsribe channel: ${channelName}");
            }

            await Groups.AddAsync(Context.ConnectionId, channelName);
            return MessageResult.Success();
        }

        public async Task<MessageResult> SubscribeMultiple(IEnumerable<string> channelName)
        {
            await Task.CompletedTask;
            return MessageResult.Success();
        }

        public async Task<MessageResult> Unsubscribe(string channelName)
        {
            if (!IsValidGroupName(channelName))
            {
                return MessageResult.Error(null, "Illegal channel name: ${channelName}");
            }

            await Groups.RemoveAsync(Context.ConnectionId, channelName);
            return MessageResult.Success();
        }

        public async Task<MessageResult> UnsubscribeMultiple(IEnumerable<string> channelName)
        {
            await Task.CompletedTask;
            return MessageResult.Success();
        }

        #region Private Methods

        private static bool IsValidGroupName(string channelName) => !string.IsNullOrEmpty(channelName);

        private static bool IsAuthorized(string channelName, ClaimsPrincipal user)
        {
            return true;
        }

        #endregion
    }
}
