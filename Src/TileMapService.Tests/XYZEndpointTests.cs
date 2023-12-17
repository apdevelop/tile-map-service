using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using NUnit.Framework;

using MBT = TileMapService.MBTiles;

namespace TileMapService.Tests
{
    /// <summary>
    /// End-to-end tests for XYZ endpoint.
    /// </summary>
    [TestFixture]
    public class XYZEndpointTests
    {
        private IHost serviceHost;

        private HttpClient client;

        private static string MbtilesFilePath1 => Path.Join(TestConfiguration.DataPath, "test-xyz-world-mercator-hd.mbtiles");

        private static string MbtilesFilePath2 => Path.Join(TestConfiguration.DataPath, "test-xyz-small-area.mbtiles");

        private static string LocalFilesPath => Path.Join(TestConfiguration.DataPath, "test-xyz-world-geodetic");

        [OneTimeSetUp]
        public async Task Setup()
        {
            RemoveTestData();
            await PrepareTestDataAsync();

            var tileSources = new[]
            {
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeMBTiles,
                    Id = "world-mercator-hd",
                    Title = "world-mercator-hd (png)",
                    Location = MbtilesFilePath1,
                },
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeMBTiles,
                    Id = "small-area",
                    Title = "small-area (jpeg)",
                    Location = MbtilesFilePath2,
                },
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeLocalFiles,
                    Title = "World Geodetic EPSG:4326",
                    Id = "world-geodetic",
                    Location = LocalFilesPath + "\\{z}\\{x}\\{y}.jpg",
                    Srs = Utils.SrsCodes.EPSG4326,
                    Format = ImageFormats.Jpeg,
                    MinZoom = 0,
                    MaxZoom = 3,
                },
            };

            this.client = new HttpClient
            {
                BaseAddress = new Uri(TestConfiguration.BaseUrl),
            };

            var json = JsonSerializer.Serialize(
                new
                {
                    Sources = tileSources,
                    Service = new
                    {
                        keywords = "service,tile"
                    }
                });

            this.serviceHost = await TestsUtility.CreateAndRunServiceHostAsync(json, TestConfiguration.PortNumber);

            // TODO: proxy source
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await this.serviceHost.StopAsync();
            this.serviceHost.Dispose();

            // Skip deleting files to analyze them
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                RemoveTestData();
            }

            this.client.Dispose();
        }

        [Test]
        public async Task GetTileAsync()
        {
            {
                var r1 = await client.GetAsync("/xyz/world-mercator-hd/0/0/0.png");
                Assert.Multiple(() =>
                {
                    Assert.That(r1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(r1.Content.Headers.ContentType.MediaType, Is.EqualTo(MediaTypeNames.Image.Png));
                });

                var expected1 = new MBT.Repository(MbtilesFilePath1).ReadTile(0, 0, 0);
                var actual1 = await r1.Content.ReadAsByteArrayAsync();
                Assert.That(actual1, Is.EqualTo(expected1));
            }

            {
                var r2 = await client.GetAsync("/xyz/world-mercator-hd/?x=0&y=0&z=0");
                Assert.Multiple(() =>
                {
                    Assert.That(r2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(r2.Content.Headers.ContentType.MediaType, Is.EqualTo(MediaTypeNames.Image.Png));
                });

                var expected2 = new MBT.Repository(MbtilesFilePath1).ReadTile(0, 0, 0);
                var actual2 = await r2.Content.ReadAsByteArrayAsync();
                Assert.That(actual2, Is.EqualTo(expected2));
            }

            {
                var r3 = await client.GetAsync("/xyz/small-area/?x=0&y=255&z=8");
                Assert.Multiple(() =>
                {
                    Assert.That(r3.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(r3.Content.Headers.ContentType.MediaType, Is.EqualTo(MediaTypeNames.Image.Jpeg));
                });

                var expected3 = new MBT.Repository(MbtilesFilePath2).ReadTile(0, 0, 8);
                var actual3 = await r3.Content.ReadAsByteArrayAsync();
                Assert.That(actual3, Is.EqualTo(expected3));
            }
        }

        [Test]
        public async Task GetTileChangeFormatAsync()
        {
            var db = new MBT.Repository(MbtilesFilePath1);
            var original = db.ReadTile(0, 0, 0);

            {
                var r = await client.GetAsync("/xyz/world-mercator-hd/0/0/0.jpeg");
                Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var expected = Utils.ImageHelper.ConvertImageToFormat(original, MediaTypeNames.Image.Jpeg, 90);
                var actual = await r.Content.ReadAsByteArrayAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(r.Content.Headers.ContentType.MediaType, Is.EqualTo(MediaTypeNames.Image.Jpeg));
                    Assert.That(Utils.ImageHelper.GetImageMediaType(actual), Is.EqualTo(MediaTypeNames.Image.Jpeg));
                    Assert.That(actual, Is.EqualTo(expected));
                });
            }

            {
                var r = await client.GetAsync("/xyz/world-mercator-hd/0/0/0.webp");
                Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var expected = Utils.ImageHelper.ConvertImageToFormat(original, MediaTypeNames.Image.Webp, 90);
                var actual = await r.Content.ReadAsByteArrayAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(r.Content.Headers.ContentType.MediaType, Is.EqualTo(MediaTypeNames.Image.Webp));
                    Assert.That(Utils.ImageHelper.GetImageMediaType(actual), Is.EqualTo(MediaTypeNames.Image.Webp));
                    Assert.That(actual, Is.EqualTo(expected));
                });
            }
        }

        #region Utility methods

        private static async Task PrepareTestDataAsync()
        {
            if (!Directory.Exists(TestConfiguration.DataPath))
            {
                Directory.CreateDirectory(TestConfiguration.DataPath);
            }

            // 1. MBTiles database 1
            const int HDTileSize = 512;
            var db1 = MBT.Repository.CreateEmptyDatabase(MbtilesFilePath1);
            db1.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyName, "World Mercator HD"));
            db1.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyFormat, ImageFormats.Png));
            db1.AddTile(0, 0, 0, Utils.ImageHelper.CreateEmptyImage(HDTileSize, HDTileSize, 0, SkiaSharp.SKEncodedImageFormat.Png, 0));
            db1.AddTile(0, 0, 5, Utils.ImageHelper.CreateEmptyImage(HDTileSize, HDTileSize, 5, SkiaSharp.SKEncodedImageFormat.Png, 0));

            // 2. MBTiles database 2
            const int TileSize = 256;
            var db2 = MBT.Repository.CreateEmptyDatabase(MbtilesFilePath2);
            db2.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyName, "Small Area"));
            db2.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyFormat, ImageFormats.Jpeg));
            db2.AddTile(0, 0, 8, Utils.ImageHelper.CreateEmptyImage(TileSize, TileSize, 8, SkiaSharp.SKEncodedImageFormat.Jpeg, 85));
            db2.AddTile(0, 0, 10, Utils.ImageHelper.CreateEmptyImage(TileSize, TileSize, 10, SkiaSharp.SKEncodedImageFormat.Jpeg, 85));

            // 2. Local files for EPSG:4326
            // Create files Z\X\Y structure
            for (var z = 0; z <= 3; z++)
            {
                Directory.CreateDirectory(Path.Join(LocalFilesPath, z.ToString(CultureInfo.InvariantCulture)));
                for (var x = 0; x < 2 * (1 << z); x++) // At Z=0 world covered by two tiles [0][1]
                {
                    var zxPath = Path.Join(LocalFilesPath, z.ToString(CultureInfo.InvariantCulture), x.ToString(CultureInfo.InvariantCulture));
                    Directory.CreateDirectory(zxPath);
                    for (var y = 0; y < 1 << z; y++)
                    {
                        var tilePath = Path.Join(zxPath, y.ToString(CultureInfo.InvariantCulture)) + ".jpg";
                        var tile = Utils.ImageHelper.CreateEmptyImage(256, 256, (uint)(z * 32768 + x * 4096 + y * 256), SkiaSharp.SKEncodedImageFormat.Jpeg, 50);
                        await File.WriteAllBytesAsync(tilePath, tile);
                    }
                }
            }
        }

        private static void RemoveTestData()
        {
            if (Directory.Exists(TestConfiguration.DataPath))
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                if (File.Exists(MbtilesFilePath1))
                {
                    File.Delete(MbtilesFilePath1);
                }

                if (File.Exists(MbtilesFilePath2))
                {
                    File.Delete(MbtilesFilePath2);
                }

                if (Directory.Exists(LocalFilesPath))
                {
                    Directory.Delete(LocalFilesPath, true);
                }
            }
        }

        #endregion
    }
}
