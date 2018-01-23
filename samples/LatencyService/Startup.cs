using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Latency
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
            var consoleLogLevel = Configuration["SignalRService:ConsoleLogLevel"];
            if (!Enum.TryParse<LogLevel>(consoleLogLevel, true, out var logLevel))
            {
                logLevel = LogLevel.Information;
            }
            var protocolType = Configuration["SignalRService:ProtocolType"];
            if (!Enum.TryParse<ProtocolType>(protocolType, true, out var protoType))
            {
                protoType = ProtocolType.Text;
            }
            services.AddSignalRService(hubOption => {
                hubOption.ConsoleLogLevel = logLevel;
                hubOption.ConnectionNumber = Configuration.GetValue<int>("SignalRService:ServiceConnectionNo");
                hubOption.ProtocolType = protoType;
                hubOption.ReceiveBufferSize = Configuration.GetValue<int>("SignalRService:ReceiveBufferSize");
                hubOption.SendBufferSize = Configuration.GetValue<int>("SignalRService:SendBufferSize");
                hubOption.EnableMetrics = Configuration["SignalRService:EnableMetrics"] != null ?
                    bool.TryParse(Configuration["SignalRService:EnableMetrics"], out var value) && value : false;
                hubOption.EchoAll4TroubleShooting = Configuration["SignalRService:EchoAll4TroubleShooting"] != null ?
                    bool.TryParse(Configuration["SignalRService:EchoAll4TroubleShooting"], out var v) && v : false;
            });
            //services.AddSignalRService(hubOption => { hubOption.ConsoleLogLevel = logLevel; });
	    services.AddSingleton(typeof(IUserTracker<>), typeof(InMemoryUserTracker<>));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseFileServer();
            app.UseSignalRService(Configuration["SignalRService:ConnectionString"],
                routes => { routes.MapHub<Chat>("chat"); });
        }
    }
}
