using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace TileMapService
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddControllers();
            services.AddSingleton<ITileSourceFabric, TileSourceFabric>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<ErrorLoggingMiddleware>();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors(builder => builder.AllowAnyOrigin());
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
