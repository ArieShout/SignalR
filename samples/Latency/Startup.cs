using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            // disable or enable the Output Channel used in HubConnectionContext, only for performance experiment
            var noOutputChan = Configuration["SignalRService:NoOutputChannel"];
            bool noOutputChannel = noOutputChan != null ? bool.TryParse(noOutputChan, out var v) && v : false;
            services.AddSignalR(option => { option.NoOutputChannel = noOutputChannel; });

            var latencyOption = new LatencyOption();
            latencyOption.ConcurrentClientCount = Configuration.GetValue<int>("SignalRService:ConcurrentClientCount");
            services.AddSingleton(typeof(LatencyOption), latencyOption);
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
            app.UseSignalR(
                routes => { routes.MapHub<Chat>("/chat");
            });
        }
    }
}
