using System.Web.Http;
using System.Web.Mvc;

namespace ServiceChatNetFxSample.Areas.ChatPage
{
    public class ChatPageAreaRegistration : AreaRegistration
    {
        public override string AreaName => "ChatPage";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "ChatPage_Default",
                "chat",
                new { controller = "Chat", action = "Index" });

            ChatPageConfig.Register(GlobalConfiguration.Configuration);
        }
    }
}