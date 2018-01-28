using System.Configuration;
using Microsoft.AspNetCore.SignalR.Owin;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(ServiceChatNetFxSample.Startup))]

namespace ServiceChatNetFxSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
            app.UseSignalRService(ConfigurationManager.AppSettings["SignalRService:ConnectionString"],
                builder => { _ = builder.UseHub<Chat>().StartAsync(); });
        }
    }
}
