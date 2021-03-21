using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
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

        private static string LocalFilesPath => Path.Join(TestDataPath, "tiles3");

        #endregion

        public static async Task Main()
        {
            Cleanup();
            PrepareTestData();
            await CreateAndRunServiceHostAsync();
            await PerformTestsAsync();
            Cleanup();

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
                    Type = TileSourceConfiguration.TypeLocalFiles,
                    Id = "source3",
                    Title = "Tile Source 3",
                    Location = LocalFilesPath + "\\{z}\\{x}\\{y}.png",
                    MinZoom = 0,
                    MaxZoom = 2,
                    Format = "png",
                    Tms = false,
                },
            };

            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(new { TileSources = tileSources }));

            // TODO: more tiles into database
            // Create and fill MBTiles databases
            var db1 = Repository.CreateEmptyDatabase(Mbtiles1FilePath);
            db1.AddMetadataItem(new MetadataItem(MetadataItem.KeyName, "World Countries"));
            db1.AddMetadataItem(new MetadataItem(MetadataItem.KeyFormat, "png"));
            db1.AddTile(0, 0, 0, new TileDataStub(0, 0, 0).ToByteArray());

            var db2 = Repository.CreateEmptyDatabase(Mbtiles2FilePath);
            db2.AddMetadataItem(new MetadataItem(MetadataItem.KeyName, "Satellite Imagery"));
            db2.AddMetadataItem(new MetadataItem(MetadataItem.KeyFormat, "jpg"));
            db2.AddMetadataItem(new MetadataItem(MetadataItem.KeyMinZoom, "0"));
            db2.AddMetadataItem(new MetadataItem(MetadataItem.KeyMaxZoom, "5"));
            db2.AddTile(0, 0, 0, new TileDataStub(0, 0, 0).ToByteArray());

            // Create files Z\X\Y structure
            for (var z = tileSources[2].MinZoom.Value; z <= tileSources[2].MaxZoom.Value; z++)
            {
                Directory.CreateDirectory(Path.Join(LocalFilesPath, z.ToString(CultureInfo.InvariantCulture)));
                for (var x = 0; x < 1 << z; x++)
                {
                    var zxPath = Path.Join(LocalFilesPath, z.ToString(CultureInfo.InvariantCulture), x.ToString(CultureInfo.InvariantCulture));
                    Directory.CreateDirectory(zxPath);
                    for (var y = 0; y < 1 << z; y++)
                    {
                        var tilePath = Path.Join(zxPath, y.ToString(CultureInfo.InvariantCulture)) + "." + tileSources[2].Format;
                        var tile = new TileDataStub(x, y, z);
                        File.WriteAllBytes(tilePath, tile.ToByteArray());
                    }
                }
            }
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
            // TODO: ? nunit tests console runner

            var testRunner = new TestRunner(BaseUrl);
            await testRunner.GetTmsServicesAsync();
            await testRunner.GetTmsTileMapServiceAsync();
            await testRunner.GetTmsTileMap1Async();
            await testRunner.GetTmsTileMap2Async();
            await testRunner.GetTmsTileMap3Async();
            await testRunner.GetWmtsCapabilitiesAsync();
            await testRunner.GetTileXyzAsync();
            await testRunner.GetTileTmsAsync();
            await testRunner.GetTileWmtsAsync();

            await Task.Delay(1000); // To get this line on console after all default logging messages
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
