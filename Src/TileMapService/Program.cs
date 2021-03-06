using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace TileMapService
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder().Build();

            // TODO: check and log configuration errors
            // https://stackoverflow.com/questions/56077346/asp-net-core-call-async-init-on-singleton-service
            await (host.Services.GetService(typeof(ITileSourceFabric)) as ITileSourceFabric).InitAsync();

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
