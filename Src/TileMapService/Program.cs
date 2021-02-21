using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TileMapService
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder()
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder()
        {
            // Using config from appsettings.json ("Kestrel" section)
            return Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    });
        }
    }
}
