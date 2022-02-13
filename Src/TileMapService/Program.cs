using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TileMapService
{
    class Program
    {
        public static async Task Main()
        {
            var host = CreateHostBuilder().Build();

            // TODO: check and log configuration errors
            // https://stackoverflow.com/questions/56077346/asp-net-core-call-async-init-on-singleton-service

            if (host.Services.GetService(typeof(ITileSourceFabric)) is not ITileSourceFabric service)
            {
                throw new InvalidOperationException();
            }

            await service.InitAsync();

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder()
        {
            // Using configuration from appsettings.json ("Kestrel" section)
            return Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    });
        }
    }
}
