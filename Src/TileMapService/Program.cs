using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TileMapService
{
    static class Program
    {
        public static async Task Main()
        {
            var host = Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
                    .Build();

            // TODO: check and log configuration errors
            // https://stackoverflow.com/questions/56077346/asp-net-core-call-async-init-on-singleton-service

            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole());

            var logger = loggerFactory.CreateLogger(typeof(Program));
            logger.LogInformation($"System info: {Environment.NewLine}{String.Join(Environment.NewLine, GetEnvironmentInfo())}");

            if (host.Services.GetService(typeof(ITileSourceFabric)) is not ITileSourceFabric service)
            {
                throw new InvalidOperationException();
            }

            await service.InitAsync();
            await host.RunAsync();
        }

        private static string[] GetEnvironmentInfo() =>
            new[]
            {
                $"MachineName='{Environment.MachineName}'  Domain='{Environment.UserDomainName}'  User='{Environment.UserName}'",
                $"CPU={Environment.ProcessorCount}  OS='{Environment.OSVersion}' ('{RuntimeInformation.OSDescription.Trim()}')",
                $"OS x64={Environment.Is64BitOperatingSystem}  Process x64={Environment.Is64BitProcess}  .NET='{Environment.Version}'  Culture='{CultureInfo.CurrentCulture.DisplayName}' ({CultureInfo.CurrentCulture.Name})",
                $"UtcOffset={TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)}  TZ='{TimeZoneInfo.Local.StandardName}'",
                $"UtcNow={DateTime.UtcNow}  Uptime={TimeSpan.FromMilliseconds(Environment.TickCount64)}",
                $"Process [PID={Environment.ProcessId}]='{Process.GetCurrentProcess()?.MainModule?.FileName}'",
                $"Assembly='{typeof(Program).Assembly.Location}'",
                $"CurrentDirectory='{Environment.CurrentDirectory}'",
            };
    }
}
