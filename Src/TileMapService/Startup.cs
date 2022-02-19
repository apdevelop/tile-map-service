using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
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
            // TODO: custom exception
            ////app.UseExceptionHandler(appError =>
            ////{
            ////    appError.Run(async context =>
            ////    {
            ////        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ////        context.Response.ContentType = "application/json";

            ////        var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
            ////        if (contextFeature != null)
            ////        {
            ////            await context.Response.WriteAsync();
            ////        }
            ////    });
            ////});
            app.UseCors(builder => builder.AllowAnyOrigin());
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
