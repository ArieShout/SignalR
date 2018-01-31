// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace MyChat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                //.ConfigureLogging((context, factory) =>
                //{
                //    factory.AddConfiguration(context.Configuration.GetSection("Logging"));
                //    factory.AddConsole();
                //    //factory.AddDebug();
                //})
                .UseKestrel()
                .UseUrls("http://*:5050")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}