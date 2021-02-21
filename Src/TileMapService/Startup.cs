using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TileMapService
{
    class Startup
    {
        // TODO: DI/services for global data

        public IConfiguration Configuration { get; set; }

        public static Dictionary<string, ITileSource> TileSources;

        public Startup()
        {
            this.Configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
               .Build();

            // TODO: check and log configuration errors

            Startup.TileSources = Utils.GetTileSetConfigurations(this.Configuration)
                .ToDictionary(c => c.Name, c => TileSourceFabric.CreateTileSource(c));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton(this.Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<ErrorLoggingMiddleware>();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
