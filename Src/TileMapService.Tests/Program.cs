using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using TileMapService.MBTiles;

namespace TileMapService.Tests
{
    class Program
    {
        #region Test environment configuration (port number, path to temporary files)

        private const int TestPort = 5002;

        private static string BaseUrl => $"http://localhost:{TestPort}";

        private static string TestDataPath => Path.Join(Path.GetTempPath(), "TileMapServiceTestData");

        private static string SettingsFilePath => Path.Join(TestDataPath, "testsettings.json");

        private static string Mbtiles1FilePath => Path.Join(TestDataPath, "test1.mbtiles");

        private static string Mbtiles2FilePath => Path.Join(TestDataPath, "test2.mbtiles");

        private static string LocalFilesPath => Path.Join(TestDataPath, ""); // TODO: files source

        #endregion

        public static async Task Main()
        {
            Cleanup();
            PrepareTestData();
            await CreateAndRunServiceHostAsync();
            await PerformTestsAsync();
            Cleanup();

            // TODO: ? nunit tests runner
            Console.ReadKey();
        }

        private static void PrepareTestData()
        {
            if (!Directory.Exists(TestDataPath))
            {
                Directory.CreateDirectory(TestDataPath);
            }

            // Create config file
            var tileSources = new[]
            {
                new TileSourceConfiguration
                {
                    Type = TileSourceConfiguration.TypeMBTiles,
                    Id = "source1",
                    Location = Mbtiles1FilePath,
                    MinZoom = 0,
                    MaxZoom = 18,
                },
                new TileSourceConfiguration
                {
                    Type = TileSourceConfiguration.TypeMBTiles,
                    Id = "source2",
                    Title = "Tile Source 2",
                    Location = Mbtiles2FilePath,
                },
                new TileSourceConfiguration
                {
                    Type = TileSourceConfiguration.TypeLocalFiles, // TODO: create files structure
                    Id = "source3",
                    Title = "Tile Source 3",
                    Location = LocalFilesPath,
                    MaxZoom = 3,
                    Format = "png",
                    Tms = false,
                },
            };

            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(new { TileSources = tileSources }));

            // Create and fill MBTiles databases
            var db1 = MBTiles.Repository.CreateEmptyDatabase(Mbtiles1FilePath);
            db1.AddMetadataItem(new MetadataItem(MetadataItem.KeyName, "World Countries"));
            db1.AddMetadataItem(new MetadataItem(MetadataItem.KeyFormat, "png"));
            // TODO: add tiles

            var db2 = MBTiles.Repository.CreateEmptyDatabase(Mbtiles2FilePath);
            db2.AddMetadataItem(new MetadataItem(MetadataItem.KeyName, "Satellite Imagery"));
            db2.AddMetadataItem(new MetadataItem(MetadataItem.KeyFormat, "jpg"));
            db2.AddMetadataItem(new MetadataItem(MetadataItem.KeyMinZoom, "0"));
            db2.AddMetadataItem(new MetadataItem(MetadataItem.KeyMaxZoom, "5"));
            // TODO: add tiles
        }

        private static async Task CreateAndRunServiceHostAsync()
        {
            // https://docs.microsoft.com/ru-ru/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0
            var host = Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration(configurationBuilder =>
                    {
                        var configuration = new ConfigurationBuilder()
                            .AddJsonFile(SettingsFilePath)
                            .Build();

                        // https://stackoverflow.com/a/58594026/1182448
                        configurationBuilder.Sources.Clear();
                        configurationBuilder.AddConfiguration(configuration);
                    })
                    .ConfigureWebHostDefaults(webHostBuilder =>
                    {
                        webHostBuilder.UseStartup<Startup>();
                        webHostBuilder.UseKestrel(options =>
                        {
                            options.Listen(IPAddress.Loopback, TestPort);
                        });
                    })
                    .Build();

            await (host.Services.GetService(typeof(ITileSourceFabric)) as ITileSourceFabric).InitAsync();

            var _ = host.RunAsync();
        }

        private static async Task PerformTestsAsync()
        {
            var testRunner = new TestRunner(BaseUrl);
            await testRunner.GetTmsServicesAsync();
            await testRunner.GetTmsTileMapServiceAsync();
            await testRunner.GetTmsTileMap1Async();
            await testRunner.GetTmsTileMap2Async();
            await testRunner.GetTmsTileMap3Async();
            await testRunner.GetWmtsCapabilitiesAsync();
            // TODO: get tiles

            await Task.Delay(1000);
            Console.WriteLine("All tests passed");
        }

        private static void Cleanup()
        {
            if (Directory.Exists(TestDataPath))
            {
                var dir = new DirectoryInfo(TestDataPath);
                dir.Delete(true);
            }
        }
    }
}
