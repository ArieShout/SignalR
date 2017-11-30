namespace Microsoft.AspNetCore.SignalR.ServiceServer
{
    public static class HubConnectionContextExtension
    {
        public static string GetTargetConnectionId(this HubConnectionContext connection)
        {
            return connection.Metadata.TryGetValue("TargetConnId", out var targetConnId) ? (string)targetConnId : null;
        }
    }
}
