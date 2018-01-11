using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SignalRServiceSample
{
    //[Authorize]
    public class SignalRService : Hub
    {
        private static readonly Regex ChannelNameRegex = new Regex("^[0-9A-Za-z][0-9A-Za-z-_+/]{0,63}$");

        public async Task<MessageResult> Publish(string channel, string eventName, object data)
        {
            var ret = ValidateChannels(new[] {channel});
            if (ret != null)
            {
                return ret;
            }

            // TODO: persist event before any action
            await Clients.Group(channel).InvokeAsync($"{channel}:{eventName}", data);
            return MessageResult.Success();
        }

        public async Task<MessageResult> Subscribe(IEnumerable<string> channels)
        {
            var ret = ValidateChannels(channels);
            if (ret != null)
            {
                return ret;
            }

            var tasks = channels.Select(x => Groups.AddAsync(Context.ConnectionId, x));
            await Task.WhenAll(tasks);
            return MessageResult.Success();
        }

        public async Task<MessageResult> Unsubscribe(IEnumerable<string> channels)
        {
            var ret = ValidateChannels(channels);
            if (ret != null)
            {
                return ret;
            }

            var tasks = channels.Select(x => Groups.RemoveAsync(Context.ConnectionId, x));
            await Task.WhenAll(tasks);
            return MessageResult.Success();
        }

        #region Private Methods

        private MessageResult ValidateChannels(IEnumerable<string> channels)
        {
            var invalidChannels = channels.Where(IsChannelNameInvalid).ToArray();
            if (invalidChannels.Any())
            {
                return MessageResult.Error(null, $"Invalid channel names: {string.Join(';', invalidChannels)}");
            }

            var unauthorizedChannels = channels.Where(c => IsUnauthorized(c, Context.User)).ToArray();
            if (unauthorizedChannels.Any())
            {
                return MessageResult.Error(null, $"Unauthorized channels: {string.Join(';', unauthorizedChannels)}");
            }

            return null;
        }

        private static bool IsChannelNameInvalid(string channel) =>
            string.IsNullOrEmpty(channel) || !ChannelNameRegex.IsMatch(channel);

        private static bool IsUnauthorized(string channelName, ClaimsPrincipal user)
        {
            return false;
        }

        #endregion
    }
}
