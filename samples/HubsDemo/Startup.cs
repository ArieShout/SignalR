// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
using SocketsSample.Hubs;

namespace SocketsSample
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
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddSockets();

            services.AddSignalRService();
            services.AddCors(o =>
            {
                o.AddPolicy("Everything", p =>
                {
                    p.AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowAnyOrigin();
                });
            });
            //services.AddEndPoint<MessagesEndPoint>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseFileServer();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("Everything");

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
                configHub.BuildServiceHub<Chat>(signalrServicePath + "/server/default", logLevel);
                configHub.BuildServiceHub<DynamicChat>(signalrServicePath + "/server/dynamic", logLevel);
                configHub.BuildServiceHub<Streaming>(signalrServicePath + "/server/streaming", logLevel);
                configHub.BuildServiceHub<HubTChat>(signalrServicePath + "/server/hubT", logLevel);
            });
        }
    }
}
