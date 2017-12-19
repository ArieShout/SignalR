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
                    options.SigningKeyProvider = () => new[] {Configuration["Auth:JWT:IssuerSigningKey"]};
                    options.EnableStickySession = bool.TryParse(Configuration["EnableStickySession"], out var value) && value;
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
