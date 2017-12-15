using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace MyChat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .ConfigureLogging((context, factory) =>
                {
                    factory.AddConfiguration(context.Configuration.GetSection("Logging"));
                    factory.AddConsole();
                    factory.AddDebug();
                })
                .UseUrls("http://*:5050")
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}