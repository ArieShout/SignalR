namespace Microsoft.AspNetCore.SignalR.ServiceServer
{
    public static class HubConnectionContextExtension
    {
        public static string GetTargetConnectionId(this HubConnectionContext connection)
        {
            return connection.Metadata.TryGetValue("TargetConnId", out var targetConnId) ? (string)targetConnId : null;
        }

        public static void AddTargetConnectionId(this HubConnectionContext connection, string targetConnId)
        {
            connection.Metadata.Add("TargetConnId", targetConnId);
        }
    }
}
