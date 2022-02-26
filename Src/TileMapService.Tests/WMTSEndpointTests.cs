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
    /// End-to-end tests for Web Map Tile Service (WMTS) endpoint.
    /// </summary>
    [TestFixture]
    public class WMTSEndpointTests
    {
        private IHost serviceHost;

        private HttpClient client;

        private static string MbtilesFilePath => Path.Join(TestConfiguration.DataPath, "test-wmts-world-mercator-hd.mbtiles");

        private static string LocalFilesPath => Path.Join(TestConfiguration.DataPath, "test-wmts-world-geodetic");

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
                // TODO: add small area source with bounding box
            };

            this.client = new HttpClient
            {
                BaseAddress = new Uri(TestConfiguration.BaseUrl),
            };

            var json = JsonSerializer.Serialize(new { Sources = tileSources });
            this.serviceHost = await TestsUtility.CreateAndRunServiceHostAsync(json, TestConfiguration.portNumber);

            // TODO: proxy source for WMTS
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
        }

        [Test]
        public async Task GetWmtsCapabilitiesAsync()
        {
            // 1. Service
            var r = await client.GetAsync("/wmts" + "?service=WMTS&request=GetCapabilities");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            var wmtsXml = await r.Content.ReadAsStringAsync();
            var xml = new XmlDocument();
            xml.LoadXml(wmtsXml);

            var nsManager = new XmlNamespaceManager(xml.NameTable);
            nsManager.AddNamespace("ns", "http://www.opengis.net/wmts/1.0");
            nsManager.AddNamespace("ows", "http://www.opengis.net/ows/1.1");

            var attributes = xml.SelectSingleNode("/ns:Capabilities", nsManager).Attributes;
            Assert.AreEqual("1.0.0", attributes["version"].Value);

            // 2. Layers (Sources)
            var layers = xml.SelectNodes("/ns:Capabilities/ns:Contents/ns:Layer", nsManager);
            Assert.AreEqual(2, layers.Count);
        }

        [Test]
        public async Task GetWmtsCapabilitiesMissingParameterValueErrorAsync()
        {
            var r = await client.GetAsync("/wmts" + "?request=GetCapabilities");
            Assert.AreEqual(HttpStatusCode.BadRequest, r.StatusCode);
            var wmtsXml = await r.Content.ReadAsStringAsync();

            var xml = new XmlDocument();
            xml.LoadXml(wmtsXml);
            var nsManager = new XmlNamespaceManager(xml.NameTable);
            nsManager.AddNamespace("ows", "http://www.opengis.net/ows/1.1");

            var messageNode = xml.SelectSingleNode("/ows:ExceptionReport/ows:Exception", nsManager);
            Assert.IsNotNull(messageNode);
            Assert.IsTrue(messageNode.InnerText.Length > 10);
            Assert.AreEqual("MissingParameterValue", messageNode.Attributes["exceptionCode"].Value);
        }

        [Test]
        public async Task GetWmtsCapabilitiesInvalidParameterValueErrorAsync()
        {
            var r = await client.GetAsync("/wmts" + "?service=WMTS&request=GetCapabilities&version=1.1.0");
            Assert.AreEqual(HttpStatusCode.BadRequest, r.StatusCode);
            var wmtsXml = await r.Content.ReadAsStringAsync();

            var xml = new XmlDocument();
            xml.LoadXml(wmtsXml);
            var nsManager = new XmlNamespaceManager(xml.NameTable);
            nsManager.AddNamespace("ows", "http://www.opengis.net/ows/1.1");

            var messageNode = xml.SelectSingleNode("/ows:ExceptionReport/ows:Exception", nsManager);
            Assert.IsNotNull(messageNode);
            Assert.IsTrue(messageNode.InnerText.Length > 10);
            Assert.AreEqual("InvalidParameterValue", messageNode.Attributes["exceptionCode"].Value);
        }

        [Test]
        public async Task GetWebMercatorTile000Async()
        {
            var db = new MBT.Repository(MbtilesFilePath);
            var expected = db.ReadTile(0, 0, 0);

            var r = await client.GetAsync("/wmts/?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=world-mercator-hd&STYLE=normal&FORMAT=image/png&TILEMATRIXSET=EPSG:3857&TILEMATRIX=0&TILEROW=0&TILECOL=0");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
            var actual = await r.Content.ReadAsByteArrayAsync();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public async Task GetTileOutOfBBoxErrorAsync()
        {
            var r = await client.GetAsync("/wmts/?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=world-mercator-hd&STYLE=normal&FORMAT=image/png&TILEMATRIXSET=EPSG:3857&TILEMATRIX=0&TILEROW=0&TILECOL=1");
            Assert.AreEqual(HttpStatusCode.NotFound, r.StatusCode);
            var wmtsXml = await r.Content.ReadAsStringAsync();

            var xml = new XmlDocument();
            xml.LoadXml(wmtsXml);
            var nsManager = new XmlNamespaceManager(xml.NameTable);
            nsManager.AddNamespace("ows", "http://www.opengis.net/ows/1.1");

            var messageNode = xml.SelectSingleNode("/ows:ExceptionReport/ows:Exception", nsManager);
            Assert.IsNotNull(messageNode);
            Assert.IsTrue(messageNode.InnerText.Length > 10);
            Assert.AreEqual("Not Found", messageNode.Attributes["exceptionCode"].Value);
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
