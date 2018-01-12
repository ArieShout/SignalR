using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatDemo
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        private ServiceCredential _credential;

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
            if (!ServiceCredential.TryParse(Configuration["SignalRService:ConnectionString"], out _credential))
            {
                throw new ArgumentException(
                    $"Invalid SignalR Service connection string: {Configuration["SignalRService: ConnectionString"]}");
            }
            var broadcast = new Broadcast(_credential);
            services.AddSingleton(broadcast);

            services.AddRouting();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseFileServer();

            var router = BuildSignalRAuthRouter(app);
            app.UseRouter(router);
        }

        private IRouter BuildSignalRAuthRouter(IApplicationBuilder app)
        {
            var routeBuilder = new RouteBuilder(app);
            routeBuilder.MapRoute("signalr/{name}", context =>
            {
                var name = context.GetRouteValue("name");
                var jwt = _credential.GenerateJwtBearer(new[]
                {
                    new Channel {Name = "group-chat"},
                    new Channel {Name = name.ToString()}
                });
                Console.WriteLine($"JWT = {jwt}");
                return context.Response.WriteAsync(jwt);
            });
            return routeBuilder.Build();
        }
    }
}
