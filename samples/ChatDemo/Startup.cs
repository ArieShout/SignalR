using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.ServiceCore;
namespace MyChat
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalRService();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseFileServer();
            app.UseSignalRService(configHub =>
            {
                string signalrServicePath = Configuration.GetSection("SignalRServerService").GetValue<string>("RootPath");
                string consoleLogLevel = Configuration.GetSection("SignalRServerService").GetValue<string>("ConsoleLogLevel");
                LogLevel logLevel = LogLevel.Information;
                if ("Debug".Equals(consoleLogLevel))
                {
                    logLevel = LogLevel.Debug;
                }
                else if ("Information".Equals(consoleLogLevel))
                {
                    logLevel = LogLevel.Information;
                }
                else if ("Trace".Equals(consoleLogLevel))
                {
                    logLevel = LogLevel.Trace;
                }
                configHub.BuildServiceHub<ChatHub>(signalrServicePath +"/server/chat", logLevel);
            });
        }
    }
}
