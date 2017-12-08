using Microsoft.AspNetCore.Authentication.JwtBearer;


namespace Microsoft.AspNetCore.SignalR.Service.Server
{
    public static class MessageReceivedContextExtensions
    {
        public static bool IsTokenFromQueryString(this MessageReceivedContext context)
        {
            var headers = context.Request.Headers;
            return (headers["Connection"] == "Upgrade" && headers["Upgrade"] == "websocket") ||
                   headers["Accept"] == "text/event-stream";
        }
    }
}
