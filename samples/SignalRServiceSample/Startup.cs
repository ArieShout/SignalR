// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
//using StackExchange.Redis;

namespace SignalRServiceSample
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //    .AddJwtBearer(ConfigureJwtBearerOptions);

            //var redisConnStr = $"{Configuration["Redis:ConnectionString"]}";
            //if (!string.IsNullOrEmpty(redisConnStr))
            //{
            //    WriteLine("Redis: on");
            //    server.AddRedis2(options =>
            //    {
            //        options.Options = ConfigurationOptions.Parse(redisConnStr);
            //    });
            //}
            //else
            //{
            //    WriteLine("Redis: off");
            //}
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSignalR(routes =>
            {
                routes.MapHub<SignalRService>("/signalr");
            });
        }

        private void ConfigureJwtBearerOptions(JwtBearerOptions options)
        {
            options.TokenValidationParameters.LifetimeValidator =
                (before, expires, token, parameters) => expires > DateTime.UtcNow;

            options.TokenValidationParameters.ValidAudiences = new[]
            {
                $"{Configuration["Auth:JWT:Audience"]}"
            };

            options.TokenValidationParameters.IssuerSigningKeys = new[]
            {
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Auth:JWT:IssuerSigningKey"])),
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Auth:JWT:IssuerSigningKey2"]))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var signalRTokenHeader = context.Request.Query["signalRTokenHeader"];

                    if (!string.IsNullOrEmpty(signalRTokenHeader) &&
                        (context.HttpContext.WebSockets.IsWebSocketRequest || context.Request.Headers["Accept"] == "text/event-stream"))
                    {
                        context.Token = context.Request.Query["signalRTokenHeader"];
                    }
                    return Task.CompletedTask;
                }
            };
        }
    }
}
