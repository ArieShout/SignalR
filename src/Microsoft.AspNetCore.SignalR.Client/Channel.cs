using System;

namespace Microsoft.AspNetCore.SignalR.Client
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
            return $"{Name}:{(int)Permission}";
        }
    }
}
