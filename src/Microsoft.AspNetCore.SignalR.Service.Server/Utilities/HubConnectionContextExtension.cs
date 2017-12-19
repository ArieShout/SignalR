namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public static class HubConnectionContextExtension
    {
        public static bool TryGetRouteTarget(this HubConnectionContext connection, out RouteTarget target)
        {
            target = null;
            if (connection.Metadata.TryGetValue("Target", out var value) )
            {
                target = (RouteTarget)value;
            }
            return target != null;
        }

        public static void AddRouteTarget(this HubConnectionContext connection, RouteTarget target)
        {
            connection.Metadata.Add("Target", target);
        }

        public static bool TryGetUid(this HubConnectionContext connection, out string uid)
        {
            if (connection.Metadata.TryGetValue("UID", out var value))
            {
                uid = (string) value;
            }
            else
            {
                uid = null;
            }
            return !string.IsNullOrEmpty(uid);
        }

        public static void AddUid(this HubConnectionContext connection, string uid)
        {
            connection.Metadata.Add("UID", uid);
        }
    }
}
