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

        private static string MbtilesFilePath => Path.Join(TestConfiguration.DataPath, "test-tms-world-mercator-hd.mbtiles");

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
                    Location = MbtilesFilePath,
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
            this.serviceHost = await TestsUtility.CreateAndRunServiceHostAsync(json, TestConfiguration.portNumber);

            var tileSources2 = new[]
            {
                // TODO: add TMS proxy test (init source after service started
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
            this.serviceHost2 = await TestsUtility.CreateAndRunServiceHostAsync(json2, TestConfiguration.portNumber + 1);
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
        }

        [Test]
        public async Task GetTmsCapabilitiesAsync()
        {
            // 1. Service
            var r = await client.GetAsync("/tms");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            var tmsXml = await r.Content.ReadAsStringAsync();
            var xml = new XmlDocument();
            xml.LoadXml(tmsXml);
            var attributes = xml.SelectSingleNode("/Services/TileMapService").Attributes;
            Assert.AreEqual("1.0.0", attributes["version"].Value);
            var href = attributes["href"].Value;

            // 2. Sources
            r = await client.GetAsync(href);
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            tmsXml = await r.Content.ReadAsStringAsync();
            xml.LoadXml(tmsXml);
            var sources = xml.SelectNodes("/TileMapService/TileMaps/TileMap");
            Assert.AreEqual(2, sources.Count);
            var hrefSource1 = sources[0].Attributes["href"].Value;
            var hrefSource2 = sources[1].Attributes["href"].Value;

            // 3 TileSets
            r = await client.GetAsync(hrefSource1);
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            tmsXml = await r.Content.ReadAsStringAsync();
            xml.LoadXml(tmsXml);
            var tileFormat = xml.SelectSingleNode("/TileMap/TileFormat");
            Assert.AreEqual(512, Int32.Parse(tileFormat.Attributes["width"].Value, CultureInfo.InvariantCulture));
            Assert.AreEqual(512, Int32.Parse(tileFormat.Attributes["height"].Value, CultureInfo.InvariantCulture));
            Assert.AreEqual("png", tileFormat.Attributes["extension"].Value);
            var tileSets = xml.SelectNodes("/TileMap/TileSets/TileSet");
            Assert.AreEqual(6, tileSets.Count); // Z=0..5 from tiles zoomLevel range in table
            Assert.AreEqual("78271.51696401954", tileSets[0].Attributes["units-per-pixel"].Value);

            r = await client.GetAsync(hrefSource2);
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            tmsXml = await r.Content.ReadAsStringAsync();
            xml.LoadXml(tmsXml);
            tileFormat = xml.SelectSingleNode("/TileMap/TileFormat");
            Assert.AreEqual(256, Int32.Parse(tileFormat.Attributes["width"].Value, CultureInfo.InvariantCulture));
            Assert.AreEqual(256, Int32.Parse(tileFormat.Attributes["height"].Value, CultureInfo.InvariantCulture));
            Assert.AreEqual("jpg", tileFormat.Attributes["extension"].Value); // TODO: or "jpeg" ?
            tileSets = xml.SelectNodes("/TileMap/TileSets/TileSet");
            Assert.AreEqual(4, tileSets.Count);
            Assert.AreEqual("0.703125", tileSets[0].Attributes["units-per-pixel"].Value); // = 360 degrees / (2 * 256 pixels)
            Assert.AreEqual("0.3515625", tileSets[1].Attributes["units-per-pixel"].Value); // = 360 degrees / (4 * 256 pixels)
            Assert.AreEqual("0.17578125", tileSets[2].Attributes["units-per-pixel"].Value); // = 360 degrees / (8 * 256 pixels)
            Assert.AreEqual("0.087890625", tileSets[3].Attributes["units-per-pixel"].Value); // = 360 degrees / (16 * 256 pixels)
        }

        [Test]
        public async Task GetTmsCapabilities2Async()
        {
            var url = $"http://localhost:{TestConfiguration.portNumber + 1}/tms/1.0.0/tms-proxy";
            var r = await client.GetAsync(url);
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            var tmsXml = await r.Content.ReadAsStringAsync();
            var xml = new XmlDocument();
            xml.LoadXml(tmsXml);
            var tileFormat = xml.SelectSingleNode("/TileMap/TileFormat");
            Assert.AreEqual(512, Int32.Parse(tileFormat.Attributes["width"].Value, CultureInfo.InvariantCulture));
            Assert.AreEqual(512, Int32.Parse(tileFormat.Attributes["height"].Value, CultureInfo.InvariantCulture));
            Assert.AreEqual("png", tileFormat.Attributes["extension"].Value);
            ////var tileSets = xml.SelectNodes("/TileMap/TileSets/TileSet");
            ////Assert.AreEqual(6, tileSets.Count); // Z=0..5 from tiles zoomLevel range in table
        }

        [Test]
        public async Task GetMercatorTmsTile000Async()
        {
            var db = new MBT.Repository(MbtilesFilePath);
            var expected = db.ReadTile(0, 0, 0);

            var r = await client.GetAsync("/tms/1.0.0/world-mercator-hd/0/0/0.png");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            var actual = await r.Content.ReadAsByteArrayAsync();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public async Task GetGeodeticTmsTileLevel0Async()
        {
            var expected0 = await File.ReadAllBytesAsync(Path.Join(LocalFilesPath, "0", "0", "0.jpg"));
            var expected1 = await File.ReadAllBytesAsync(Path.Join(LocalFilesPath, "0", "1", "0.jpg"));

            var r0 = await client.GetAsync("/tms/1.0.0/world-geodetic/0/0/0.jpg");
            Assert.AreEqual(HttpStatusCode.OK, r0.StatusCode);
            var r1 = await client.GetAsync("/tms/1.0.0/world-geodetic/0/1/0.jpg");
            Assert.AreEqual(HttpStatusCode.OK, r1.StatusCode);

            var actual0 = await r0.Content.ReadAsByteArrayAsync();
            var actual1 = await r1.Content.ReadAsByteArrayAsync();

            Assert.AreEqual(expected0, actual0);
            Assert.AreEqual(expected1, actual1);
        }

        [Test]
        public async Task GetTmsTileOutOfBBoxErrorAsync()
        {
            var r = await client.GetAsync("/tms/1.0.0/world-geodetic/0/4/0.jpg");
            Assert.AreEqual(HttpStatusCode.NotFound, r.StatusCode);
            var tmsXml = await r.Content.ReadAsStringAsync();

            var xml = new XmlDocument();
            xml.LoadXml(tmsXml);
            var messageNode = xml.SelectSingleNode("/TileMapServerError/Message");
            Assert.IsNotNull(messageNode);
            Assert.IsTrue(messageNode.InnerText.Length > 10);
        }

        private static async Task PrepareTestDataAsync()
        {
            if (!Directory.Exists(TestConfiguration.DataPath))
            {
                Directory.CreateDirectory(TestConfiguration.DataPath);
            }

            // 1. MBTiles database
            const int HDTileSize = 512;
            var db = MBT.Repository.CreateEmptyDatabase(MbtilesFilePath);
            db.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyName, "World Mercator EPSG:3857 HD"));
            db.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyFormat, ImageFormats.Png));
            db.AddTile(0, 0, 0, Utils.ImageHelper.CreateEmptyImage(HDTileSize, HDTileSize, 0, SkiaSharp.SKEncodedImageFormat.Png, 0));
            db.AddTile(0, 0, 5, Utils.ImageHelper.CreateEmptyImage(HDTileSize, HDTileSize, 5, SkiaSharp.SKEncodedImageFormat.Png, 0));

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
                if (File.Exists(MbtilesFilePath))
                {
                    File.Delete(MbtilesFilePath);
                }

                if (Directory.Exists(LocalFilesPath))
                {
                    Directory.Delete(LocalFilesPath, true);
                }
            }
        }
    }
}
