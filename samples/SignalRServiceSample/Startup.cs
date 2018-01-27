// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using static System.Console;

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
            // To use Redis scaleout uncomment .AddRedis2
            var server = services.AddSignalRServiceServer(options =>
                {
                    options.AudienceProvider = () => new[]
                    {
                        $"{Configuration["Auth:JWT:Audience"]}/client/",
                        $"{Configuration["Auth:JWT:Audience"]}/server/"
                    };
                    options.SigningKeyProvider = () => new[]
                    {
                        Configuration["Auth:JWT:IssuerSigningKey"],
                        Configuration["Auth:JWT:IssuerSigningKey2"]
                    };
                    options.EnableStickySession = bool.TryParse(Configuration["EnableStickySession"], out var value) && value;
                },
                options =>
                {
                    options.MarkTimestampInCritialPhase = Configuration["Diagnostic:EnableMetrics"] != null ?
                        bool.TryParse(Configuration["Diagnostic:EnableMetrics"], out var value) && value : false;
                    options.EchoAll4TroubleShooting = Configuration["Diagnostic:EchoAll4TroubleShooting"] != null ?
                        bool.TryParse(Configuration["Diagnostic:EchoAll4TroubleShooting"], out var v) && v : false;
                    options.DisableDupOutputChannel = Configuration["Diagnostic:DisableDupOutputChannel"] != null ?
                        bool.TryParse(Configuration["Diagnostic:DisableDupOutputChannel"], out var d) && d : false;
                });

            var redisConnStr = $"{Configuration["Redis:ConnectionString"]}";
            if (!string.IsNullOrEmpty(redisConnStr))
            {
                WriteLine("Redis: on");
                server.AddRedis2(options =>
                {
                    options.Options = ConfigurationOptions.Parse(redisConnStr);
                });
            }
            else
            {
                WriteLine("Redis: off");
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSignalRServiceServer();
        }
    }
}
