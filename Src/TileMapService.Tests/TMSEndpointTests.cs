using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.Extensions.Hosting;
using NUnit.Framework;

using MBT = TileMapService.MBTiles;

namespace TileMapService.Tests
{
    /// <summary>
    /// End-to-end tests for Tile Map Service (TMS) endpoint.
    /// </summary>
    [TestFixture]
    public class TMSEndpointTests
    {
        private IHost serviceHost;

        private IHost serviceHost2;

        private HttpClient client;

        private static string MbtilesFilePath1 => Path.Join(TestConfiguration.DataPath, "test-tms-world-mercator-hd.mbtiles");

        private static string MbtilesFilePath2 => Path.Join(TestConfiguration.DataPath, "test-tms-small-area.mbtiles");

        private static string LocalFilesPath => Path.Join(TestConfiguration.DataPath, "test-tms-world-geodetic");

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
                    Abstract = "World map",
                    Location = MbtilesFilePath1,
                },
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeMBTiles,
                    Id = "small-area",
                    Abstract = "Small area map",
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

            var json = JsonSerializer.Serialize(new { Sources = tileSources });
            this.serviceHost = await TestsUtility.CreateAndRunServiceHostAsync(json, TestConfiguration.PortNumber);

            var tileSources2 = new[]
            {
                // TODO: add TMS proxy test (init source after service started)
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeTms,
                    Id = "tms-proxy",
                    Title = "Proxy to world-mercator-hd",
                    Format = "png",
                    Location = TestConfiguration.BaseUrl + "/tms/1.0.0/world-mercator-hd",
                },
            };

            var json2 = JsonSerializer.Serialize(new { Sources = tileSources2 });
            this.serviceHost2 = await TestsUtility.CreateAndRunServiceHostAsync(json2, TestConfiguration.PortNumber + 1);
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await this.serviceHost.StopAsync();
            this.serviceHost.Dispose();

            await this.serviceHost2.StopAsync();
            this.serviceHost2.Dispose();

            // Skip deleting files to analyze them
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                RemoveTestData();
            }

            this.client.Dispose();
        }

        [Test]
        public async Task GetTmsCapabilitiesAsync()
        {
            // 1. Service
            var r = await client.GetAsync("/tms");
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var tmsXml = await r.Content.ReadAsStringAsync();
            var xml = new XmlDocument();
            xml.LoadXml(tmsXml);
            var attributes = xml.SelectSingleNode("/Services/TileMapService").Attributes;
            Assert.That(attributes["version"].Value, Is.EqualTo("1.0.0"));
            var href = attributes["href"].Value;

            // 2. Sources
            r = await client.GetAsync(href);
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            tmsXml = await r.Content.ReadAsStringAsync();
            xml.LoadXml(tmsXml);
            var sources = xml.SelectNodes("/TileMapService/TileMaps/TileMap");
            Assert.That(sources, Has.Count.EqualTo(3));
            var hrefSource1 = sources[0].Attributes["href"].Value;
            var hrefSource2 = sources[1].Attributes["href"].Value;
            var hrefSource3 = sources[2].Attributes["href"].Value;

            // 3 TileSets
            r = await client.GetAsync(hrefSource1);
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            tmsXml = await r.Content.ReadAsStringAsync();
            xml.LoadXml(tmsXml);
            Assert.That(xml.SelectSingleNode("/TileMap/Abstract").InnerText, Is.EqualTo("World map"));
            var tileFormat = xml.SelectSingleNode("/TileMap/TileFormat");
            Assert.Multiple(() =>
            {
                Assert.That(Int32.Parse(tileFormat.Attributes["width"].Value, CultureInfo.InvariantCulture), Is.EqualTo(512));
                Assert.That(Int32.Parse(tileFormat.Attributes["height"].Value, CultureInfo.InvariantCulture), Is.EqualTo(512));
                Assert.That(tileFormat.Attributes["extension"].Value, Is.EqualTo("png"));
            });

            var tileSets = xml.SelectNodes("/TileMap/TileSets/TileSet");
            Assert.That(tileSets, Has.Count.EqualTo(6)); // Z=0..5 from tiles zoomLevel range in table
            Assert.That(tileSets[0].Attributes["units-per-pixel"].Value, Is.EqualTo("78271.51696401954"));

            r = await client.GetAsync(hrefSource2);
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            tmsXml = await r.Content.ReadAsStringAsync();
            xml.LoadXml(tmsXml);
            Assert.That(xml.SelectSingleNode("/TileMap/Abstract").InnerText, Is.EqualTo("Small area map"));
            tileFormat = xml.SelectSingleNode("/TileMap/TileFormat");
            Assert.Multiple(() =>
            {
                Assert.That(Int32.Parse(tileFormat.Attributes["width"].Value, CultureInfo.InvariantCulture), Is.EqualTo(256));
                Assert.That(Int32.Parse(tileFormat.Attributes["height"].Value, CultureInfo.InvariantCulture), Is.EqualTo(256));
                Assert.That(tileFormat.Attributes["extension"].Value, Is.EqualTo("jpg"));
            });

            tileSets = xml.SelectNodes("/TileMap/TileSets/TileSet");
            Assert.That(tileSets, Has.Count.EqualTo(3)); // Z=8..10 from tiles zoomLevel range in table
            Assert.That(tileSets[0].Attributes["units-per-pixel"].Value, Is.EqualTo("611.4962262814026"));

            r = await client.GetAsync(hrefSource3);
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            tmsXml = await r.Content.ReadAsStringAsync();
            xml.LoadXml(tmsXml);
            Assert.That(xml.SelectSingleNode("/TileMap/Abstract").InnerText, Is.EqualTo(String.Empty));
            
            tileFormat = xml.SelectSingleNode("/TileMap/TileFormat");
            Assert.Multiple(() =>
            {
                Assert.That(Int32.Parse(tileFormat.Attributes["width"].Value, CultureInfo.InvariantCulture), Is.EqualTo(256));
                Assert.That(Int32.Parse(tileFormat.Attributes["height"].Value, CultureInfo.InvariantCulture), Is.EqualTo(256));
                Assert.That(tileFormat.Attributes["extension"].Value, Is.EqualTo("jpg")); // TODO: or "jpeg" ?
            });

            tileSets = xml.SelectNodes("/TileMap/TileSets/TileSet");
            Assert.That(tileSets, Has.Count.EqualTo(4));
            Assert.Multiple(() =>
            {
                Assert.That(tileSets[0].Attributes["units-per-pixel"].Value, Is.EqualTo("0.703125")); // = 360 degrees / (2 * 256 pixels)
                Assert.That(tileSets[1].Attributes["units-per-pixel"].Value, Is.EqualTo("0.3515625")); // = 360 degrees / (4 * 256 pixels)
                Assert.That(tileSets[2].Attributes["units-per-pixel"].Value, Is.EqualTo("0.17578125")); // = 360 degrees / (8 * 256 pixels)
                Assert.That(tileSets[3].Attributes["units-per-pixel"].Value, Is.EqualTo("0.087890625")); // = 360 degrees / (16 * 256 pixels)
            });
        }

        [Test]
        public async Task GetTmsCapabilities2Async()
        {
            var url = $"http://localhost:{TestConfiguration.PortNumber + 1}/tms/1.0.0/tms-proxy";
            var r = await client.GetAsync(url);
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var tmsXml = await r.Content.ReadAsStringAsync();
            var xml = new XmlDocument();
            xml.LoadXml(tmsXml);
            
            var tileFormat = xml.SelectSingleNode("/TileMap/TileFormat");
            Assert.Multiple(() =>
            {
                Assert.That(Int32.Parse(tileFormat.Attributes["width"].Value, CultureInfo.InvariantCulture), Is.EqualTo(512));
                Assert.That(Int32.Parse(tileFormat.Attributes["height"].Value, CultureInfo.InvariantCulture), Is.EqualTo(512));
                Assert.That(tileFormat.Attributes["extension"].Value, Is.EqualTo("png"));
            });

            ////var tileSets = xml.SelectNodes("/TileMap/TileSets/TileSet");
            ////Assert.That(tileSets, Has.Count.EqualTo(6)); // Z=0..5 from tiles zoomLevel range in table
        }

        [Test]
        public async Task GetWebMercatorTmsTile000Async()
        {
            var db = new MBT.Repository(MbtilesFilePath1);
            var expected = db.ReadTile(0, 0, 0);

            var r = await client.GetAsync("/tms/1.0.0/world-mercator-hd/0/0/0.png");
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var actual = await r.Content.ReadAsByteArrayAsync();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetWebMercatorTmsTileChangeFormatAsync()
        {
            var db = new MBT.Repository(MbtilesFilePath1);
            var original = db.ReadTile(0, 0, 0);

            {
                var r = await client.GetAsync("/tms/1.0.0/world-mercator-hd/0/0/0.png");
                Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var actual = await r.Content.ReadAsByteArrayAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(r.Content.Headers.ContentType.MediaType, Is.EqualTo(MediaTypeNames.Image.Png));
                    Assert.That(actual, Is.EqualTo(original));
                });
            }

            {
                var expected = Utils.ImageHelper.ConvertImageToFormat(original, MediaTypeNames.Image.Jpeg, 90);
                var r = await client.GetAsync("/tms/1.0.0/world-mercator-hd/0/0/0.jpeg");
                Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var actual = await r.Content.ReadAsByteArrayAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(r.Content.Headers.ContentType.MediaType, Is.EqualTo(MediaTypeNames.Image.Jpeg));
                    Assert.That(actual, Is.EqualTo(expected));
                });
            }

            {
                var expected = Utils.ImageHelper.ConvertImageToFormat(original, MediaTypeNames.Image.Webp, 90);
                var r = await client.GetAsync("/tms/1.0.0/world-mercator-hd/0/0/0.webp");
                Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var actual = await r.Content.ReadAsByteArrayAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(r.Content.Headers.ContentType.MediaType, Is.EqualTo(MediaTypeNames.Image.Webp));
                    Assert.That(actual, Is.EqualTo(expected));
                });
            }
        }

        [Test]
        public async Task GetSmallAreaTmsTile800Async()
        {
            var db = new MBT.Repository(MbtilesFilePath2);
            var expected = db.ReadTile(0, 0, 8);

            var r = await client.GetAsync("/tms/1.0.0/small-area/8/0/0.jpg");
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var actual = await r.Content.ReadAsByteArrayAsync();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetGeodeticTmsTileLevel0Async()
        {
            var expected0 = await File.ReadAllBytesAsync(Path.Join(LocalFilesPath, "0", "0", "0.jpg"));
            var expected1 = await File.ReadAllBytesAsync(Path.Join(LocalFilesPath, "0", "1", "0.jpg"));

            var r0 = await client.GetAsync("/tms/1.0.0/world-geodetic/0/0/0.jpg");
            Assert.That(r0.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var r1 = await client.GetAsync("/tms/1.0.0/world-geodetic/0/1/0.jpg");
            Assert.That(r1.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var actual0 = await r0.Content.ReadAsByteArrayAsync();
            var actual1 = await r1.Content.ReadAsByteArrayAsync();

            Assert.Multiple(() =>
            {
                Assert.That(actual0, Is.EqualTo(expected0));
                Assert.That(actual1, Is.EqualTo(expected1));
            });
        }

        [Test]
        public async Task GetTmsTileOutOfBBoxErrorAsync()
        {
            var r = await client.GetAsync("/tms/1.0.0/world-geodetic/0/4/0.jpg");
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            var tmsXml = await r.Content.ReadAsStringAsync();

            var xml = new XmlDocument();
            xml.LoadXml(tmsXml);
            var messageNode = xml.SelectSingleNode("/TileMapServerError/Message");
            Assert.That(messageNode, Is.Not.Null);
            Assert.That(messageNode.InnerText, Has.Length.GreaterThan(10));
        }

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

            // 3. Local files for EPSG:4326
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
    }
}
