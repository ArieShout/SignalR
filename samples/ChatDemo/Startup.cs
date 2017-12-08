using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            LogLevel logLevel = LogLevel.Information;
            string consoleLogLevel = Configuration.GetSection("SignalRServerService").GetValue<string>("ConsoleLogLevel");
            switch (consoleLogLevel)
            {
                case "Debug":
                    logLevel = LogLevel.Debug;
                    break;
                case "Information":
                    logLevel = LogLevel.Information;
                    break;
                case "Trace":
                    logLevel = LogLevel.Trace;
                    break;
                default:
                    logLevel = LogLevel.Information;
                    break;
            }
            services.AddSignalRService(hubOption => { hubOption.ConsoleLogLevel = logLevel; });
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
                configHub.BuildServiceHub<ChatHub>(signalrServicePath + "/server/chat");
            });
        }
    }
}
