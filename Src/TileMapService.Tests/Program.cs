using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace TileMapService.Tests
{
    class Program
    {
        #region Test environment configuration - path to temporary files

        private const int TestPort = 5002;

        private static string BaseUrl
        {
            get { return "http://localhost:" + TestPort.ToString(CultureInfo.InvariantCulture); }
        }

        private static string TestDataPath
        {
            get { return Path.Join(Path.GetTempPath(), "TileMapServiceTestData"); }
        }

        private static string SettingsFilePath
        {
            get { return Path.Join(TestDataPath, "testsettings.json"); }
        }

        private static string Mbtiles1FilePath
        {
            get { return Path.Join(TestDataPath, "test1.mbtiles"); }
        }

        private static string Mbtiles2FilePath
        {
            get { return Path.Join(TestDataPath, "test2.mbtiles"); }
        }

        private static string LocalFilesPath
        {
            get { return Path.Join(TestDataPath, ""); } // TODO: files source
        }

        #endregion

        public static async Task Main()
        {
            Cleanup();
            PrepareTestData();
            await CreateAndRunServiceHostAsync();
            await PerformTestsAsync();
            //Cleanup();

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
                    Type = TileSourceConfiguration.TypeLocalFiles,
                    Id = "source3",
                    Title = "Tile Source 3",
                    Location = LocalFilesPath,
                    MaxZoom = 3,
                    Tms = false,
                },
            };

            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(new { TileSources = tileSources }));

            // Create and fill MBTiles database
            var repository1 = MBTiles.Repository.CreateEmptyDatabase(Mbtiles1FilePath);
            repository1.AddMetadataItem(new MBTiles.MetadataItem(MBTiles.MetadataItem.KeyName, "Tile Source 1"));
            repository1.AddMetadataItem(new MBTiles.MetadataItem(MBTiles.MetadataItem.KeyFormat, "png"));
            // TODO: add tiles

            var repository2 = MBTiles.Repository.CreateEmptyDatabase(Mbtiles2FilePath);
            repository2.AddMetadataItem(new MBTiles.MetadataItem(MBTiles.MetadataItem.KeyName, "Satellite Imagery"));
            repository2.AddMetadataItem(new MBTiles.MetadataItem(MBTiles.MetadataItem.KeyFormat, "jpg"));
            repository2.AddMetadataItem(new MBTiles.MetadataItem(MBTiles.MetadataItem.KeyMinZoom, "0"));
            repository2.AddMetadataItem(new MBTiles.MetadataItem(MBTiles.MetadataItem.KeyMaxZoom, "5"));
            // TODO: add tiles
        }

        private static async Task CreateAndRunServiceHostAsync()
        {
            var host = CreateHostBuilder().Build();
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
            // TODO: get tiles

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

        private static IHostBuilder CreateHostBuilder()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(SettingsFilePath)
                .Build();

            // https://docs.microsoft.com/ru-ru/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0
            return Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration(builder =>
                    {
                        // https://stackoverflow.com/a/58594026/1182448
                        builder.Sources.Clear();
                        builder.AddConfiguration(configuration);
                    })
                    .ConfigureWebHostDefaults(webHostBuilder =>
                    {
                        webHostBuilder.UseStartup<Startup>();
                        webHostBuilder.UseKestrel(options =>
                        {
                            options.Listen(IPAddress.Loopback, TestPort);
                        });
                        // TODO: ? webHostBuilder.UseUrls(BaseUrl);
                    });
        }
    }
}
