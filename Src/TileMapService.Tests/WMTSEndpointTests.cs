﻿using System;
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

        private static string MbtilesFilePath1 => Path.Join(TestConfiguration.DataPath, "test-wmts-world-mercator-hd.mbtiles");

        private static string MbtilesFilePath2 => Path.Join(TestConfiguration.DataPath, "test-wmts-small-area.mbtiles");

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
                    Location = MbtilesFilePath1,
                },
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeMBTiles,
                    Id = "small-area",
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
                        keywords = "wmts,service,tile"
                    }
                });

            this.serviceHost = await TestsUtility.CreateAndRunServiceHostAsync(json, TestConfiguration.PortNumber);

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

            this.client.Dispose();
        }

        [Test]
        public async Task GetWmtsCapabilitiesAsync()
        {
            // 1. Service
            var r = await client.GetAsync("/wmts" + "?service=WMTS&request=GetCapabilities");
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var wmtsXml = await r.Content.ReadAsStringAsync();
            var xml = new XmlDocument();
            xml.LoadXml(wmtsXml);
            var nsManager = new XmlNamespaceManager(xml.NameTable);
            nsManager.AddNamespace("ns", "http://www.opengis.net/wmts/1.0");
            nsManager.AddNamespace("ows", "http://www.opengis.net/ows/1.1");

            var attributes = xml.SelectSingleNode("/ns:Capabilities", nsManager).Attributes;
            Assert.That(attributes["version"].Value, Is.EqualTo("1.0.0"));

            // 2. Service properties
            var keywords = xml.SelectNodes("/ns:Capabilities/ows:ServiceIdentification/ows:Keywords/ows:Keyword", nsManager);
            Assert.That(keywords, Has.Count.EqualTo(3));

            // 3. Layers (Sources)
            var layers = xml.SelectNodes("/ns:Capabilities/ns:Contents/ns:Layer", nsManager);
            Assert.That(layers, Has.Count.EqualTo(3));

            var tileMatrixSet = xml.SelectSingleNode("/ns:Capabilities/ns:Contents/ns:Layer/ns:TileMatrixSetLink/ns:TileMatrixSet", nsManager).InnerText;
            Assert.That(tileMatrixSet, Is.Not.Empty);

            var tileMatrix = xml.SelectSingleNode("/ns:Capabilities/ns:Contents/ns:TileMatrixSet[ows:Identifier='" + tileMatrixSet + "']/ns:TileMatrix[ows:Identifier='0']", nsManager);
            Assert.Multiple(() =>
            {
                Assert.That(tileMatrix, Is.Not.Null);
                Assert.That(Int32.Parse(tileMatrix.SelectSingleNode("//ns:TileWidth", nsManager).InnerText, CultureInfo.InvariantCulture), Is.EqualTo(512));
                Assert.That(Int32.Parse(tileMatrix.SelectSingleNode("//ns:TileHeight", nsManager).InnerText, CultureInfo.InvariantCulture), Is.EqualTo(512));
            });
        }

        [Test]
        public async Task GetWmtsCapabilitiesMissingParameterValueErrorAsync()
        {
            var r = await client.GetAsync("/wmts" + "?request=GetCapabilities");
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            var wmtsXml = await r.Content.ReadAsStringAsync();
            var xml = new XmlDocument();
            xml.LoadXml(wmtsXml);
            var nsManager = new XmlNamespaceManager(xml.NameTable);
            nsManager.AddNamespace("ows", "http://www.opengis.net/ows/1.1");

            var messageNode = xml.SelectSingleNode("/ows:ExceptionReport/ows:Exception", nsManager);
            Assert.That(messageNode, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(messageNode.InnerText, Has.Length.GreaterThan(10));
                Assert.That(messageNode.Attributes["exceptionCode"].Value, Is.EqualTo("MissingParameterValue"));
            });
        }

        [Test]
        public async Task GetWmtsCapabilitiesInvalidParameterValueErrorAsync()
        {
            var r = await client.GetAsync("/wmts" + "?service=WMTS&request=GetCapabilities&version=1.1.0");
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            var wmtsXml = await r.Content.ReadAsStringAsync();

            var xml = new XmlDocument();
            xml.LoadXml(wmtsXml);
            var nsManager = new XmlNamespaceManager(xml.NameTable);
            nsManager.AddNamespace("ows", "http://www.opengis.net/ows/1.1");

            var messageNode = xml.SelectSingleNode("/ows:ExceptionReport/ows:Exception", nsManager);
            Assert.That(messageNode, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(messageNode.InnerText, Has.Length.GreaterThan(10));
                Assert.That(messageNode.Attributes["exceptionCode"].Value, Is.EqualTo("InvalidParameterValue"));
            });
        }

        [Test]
        public async Task GetTile000KvpSyntaxAsync()
        {
            var db = new MBT.Repository(MbtilesFilePath1);
            var expected = db.ReadTile(0, 0, 0);

            var r = await client.GetAsync("/wmts/?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=world-mercator-hd&STYLE=normal&FORMAT=image/png&TILEMATRIXSET=EPSG:3857&TILEMATRIX=0&TILEROW=0&TILECOL=0");
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var actual = await r.Content.ReadAsByteArrayAsync();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetTile000KvpSyntaxChangeFormatAsync()
        {
            var db = new MBT.Repository(MbtilesFilePath1);
            var original = db.ReadTile(0, 0, 0);

            {
                var expected = Utils.ImageHelper.ConvertImageToFormat(original, MediaTypeNames.Image.Jpeg, 90);
                var r = await client.GetAsync("/wmts/?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=world-mercator-hd&STYLE=normal&FORMAT=image/jpeg&TILEMATRIXSET=EPSG:3857&TILEMATRIX=0&TILEROW=0&TILECOL=0");
                Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var actual = await r.Content.ReadAsByteArrayAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(r.Content.Headers.ContentType.MediaType, Is.EqualTo(MediaTypeNames.Image.Jpeg));
                    Assert.That(Utils.ImageHelper.GetImageMediaType(actual), Is.EqualTo(MediaTypeNames.Image.Jpeg));
                    Assert.That(actual, Is.EqualTo(expected));
                });
            }

            {
                var expected = Utils.ImageHelper.ConvertImageToFormat(original, MediaTypeNames.Image.Webp, 90);
                var r = await client.GetAsync("/wmts/?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=world-mercator-hd&STYLE=normal&FORMAT=image/webp&TILEMATRIXSET=EPSG:3857&TILEMATRIX=0&TILEROW=0&TILECOL=0");
                Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var actual = await r.Content.ReadAsByteArrayAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(r.Content.Headers.ContentType.MediaType, Is.EqualTo(MediaTypeNames.Image.Webp));
                    Assert.That(Utils.ImageHelper.GetImageMediaType(actual), Is.EqualTo(MediaTypeNames.Image.Webp));
                    Assert.That(actual, Is.EqualTo(expected));
                });
            }
        }

        [Test]
        public async Task GetTile000RestfulSyntaxAsync()
        {
            var db = new MBT.Repository(MbtilesFilePath1);
            var expected = db.ReadTile(0, 0, 0);

            var r = await client.GetAsync("/wmts/tile/1.0.0/world-mercator-hd/normal/EPSG:3857/0/0/0.png");
            Assert.Multiple(() =>
            {
                Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(r.Content.Headers.ContentType.MediaType, Is.EqualTo(MediaTypeNames.Image.Png));
            });

            var actual = await r.Content.ReadAsByteArrayAsync();
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetTileOutOfBBoxErrorAsync()
        {
            var r = await client.GetAsync("/wmts/?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=world-mercator-hd&STYLE=normal&FORMAT=image/png&TILEMATRIXSET=EPSG:3857&TILEMATRIX=0&TILEROW=0&TILECOL=1");
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            var wmtsXml = await r.Content.ReadAsStringAsync();

            var xml = new XmlDocument();
            xml.LoadXml(wmtsXml);
            var nsManager = new XmlNamespaceManager(xml.NameTable);
            nsManager.AddNamespace("ows", "http://www.opengis.net/ows/1.1");

            var messageNode = xml.SelectSingleNode("/ows:ExceptionReport/ows:Exception", nsManager);
            Assert.That(messageNode, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(messageNode.InnerText, Has.Length.GreaterThan(10));
                Assert.That(messageNode.Attributes["exceptionCode"].Value, Is.EqualTo("Not Found"));
            });
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
