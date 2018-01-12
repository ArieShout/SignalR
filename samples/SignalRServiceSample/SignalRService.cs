using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SignalRServiceSample
{
    [Authorize]
    public class SignalRService : Hub
    {
        // Channel name should meet below constraints:
        // - starts with alphanumeric characters
        // - consists of alphanumeric characters, '+', '-', '_', or '/'
        // - has less than 64 characters
        private static readonly Regex ChannelNameRegex = new Regex("^[0-9A-Za-z][0-9A-Za-z+-_/]{0,63}$");

        public override async Task OnConnectedAsync()
        {
            var channels = Context.User.FindAll("scope").Select(c => Channel.FromString(c.Value));
            Context.Connection.Metadata.Add("channels", channels);
            await Task.CompletedTask;
        }

        public async Task<MessageResult> Publish(string channel, string eventName, object data)
        {
            var ret = ValidateChannels(new[] {channel}, Channel.AccessPermission.Publish);
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
            var ret = ValidateChannels(channels, Channel.AccessPermission.Subscribe);
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
            var ret = ValidateChannelName(channels);
            if (ret != null)
            {
                return ret;
            }

            var tasks = channels.Select(x => Groups.RemoveAsync(Context.ConnectionId, x));
            await Task.WhenAll(tasks);
            return MessageResult.Success();
        }

        #region Private Methods

        private MessageResult ValidateChannelName(IEnumerable<string> channels)
        {
            var invalidChannels = channels.Where(IsChannelNameInvalid).ToArray();
            return invalidChannels.Any()
                ? MessageResult.Error(null, $"Invalid channel names: {string.Join(';', invalidChannels)}")
                : null;
        }

        private MessageResult ValidatePermission(IEnumerable<string> channels,
            Channel.AccessPermission requiredPermission)
        {
            if (!Context.Connection.Metadata.ContainsKey("channels"))
            {
                return MessageResult.Error(null, "No permission data found.");
            }

            var permissions = (IEnumerable<Channel>)Context.Connection.Metadata["channels"];
            var unauthorizedChannels = channels.Where(c => IsUnauthorized(c, requiredPermission, permissions)).ToArray();
            if (unauthorizedChannels.Any())
            {
                return MessageResult.Error(null, $"Unauthorized channels: {string.Join(';', unauthorizedChannels)}");
            }

            return null;
        }

        private MessageResult ValidateChannels(IEnumerable<string> channels, Channel.AccessPermission requiredPermission)
        {
            return ValidateChannelName(channels) ?? ValidatePermission(channels, requiredPermission);
        }

        private static bool IsChannelNameInvalid(string channel) =>
            string.IsNullOrEmpty(channel) || !ChannelNameRegex.IsMatch(channel);

        private static bool IsUnauthorized(string channel, Channel.AccessPermission requiredPermission,
            IEnumerable<Channel> channels)
        {
            return !channels.Any(c =>
                (c.Name == "*" || c.Name.Equals(channel, StringComparison.InvariantCultureIgnoreCase)) &&
                c.Permission.HasFlag(requiredPermission));
        }

        #endregion
    }
}
