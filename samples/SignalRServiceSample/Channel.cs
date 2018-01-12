using System;

namespace SignalRServiceSample
{
    public class Channel
    {
        [Flags]
        public enum AccessPermission
        {
            Subscribe = 1,
            Publish = 1 << 1
        }

        public string Name { get; set; }

        public AccessPermission Permission { get; set; } = AccessPermission.Subscribe | AccessPermission.Publish;

        public override string ToString()
        {
            return $"{Name}:{(int) Permission}";
        }

        public static Channel FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            var items = value.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
            var ret = new Channel
            {
                Name = items[0]
            };

            if (items.Length == 2 && Enum.TryParse<AccessPermission>(items[1], out var permission))
            {
                ret.Permission = permission;
                return ret;
            }
            return ret;
        }
    }
}
