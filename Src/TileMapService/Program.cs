using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TileMapService
{
    static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors();
            builder.Services.AddControllers();
            builder.Services.AddSingleton<ITileSourceFabric, TileSourceFabric>();

            var app = builder.Build();
            app.UseMiddleware<ErrorLoggingMiddleware>();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCors(b => b.AllowAnyOrigin());
            app.MapControllers();

            // TODO: check and log configuration errors

            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole());

            var logger = loggerFactory.CreateLogger(typeof(Program));
            logger.LogInformation($"System info: {Environment.NewLine}{String.Join(Environment.NewLine, GetEnvironmentInfo())}");

            if (app.Services.GetService(typeof(ITileSourceFabric)) is ITileSourceFabric tileSourceFabric)
            {
                await tileSourceFabric.InitAsync();
            }

            await app.RunAsync();
        }

        private static string[] GetEnvironmentInfo() =>
            [
                $"MachineName='{Environment.MachineName}'  Domain='{Environment.UserDomainName}'  User='{Environment.UserName}'",
                $"CPU={Environment.ProcessorCount}  OS='{Environment.OSVersion}' ('{RuntimeInformation.OSDescription.Trim()}')",
                $"OS x64={Environment.Is64BitOperatingSystem}  Process x64={Environment.Is64BitProcess}  .NET='{Environment.Version}'  Culture='{CultureInfo.CurrentCulture.DisplayName}' ({CultureInfo.CurrentCulture.Name})",
                $"Process [PID={Environment.ProcessId}]='{Process.GetCurrentProcess()?.MainModule?.FileName}'  Assembly='{typeof(Program).Assembly.Location}'",
            ];
    }
}
