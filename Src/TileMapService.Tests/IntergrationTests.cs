using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

using MBT = TileMapService.MBTiles;

namespace TileMapService.Tests
{
    /// <summary>
    /// Class contains end-to-end test methods.
    /// </summary>
    /// <see>https://docs.nunit.org/articles/nunit/getting-started/dotnet-core-and-dotnet-standard.html</see>
    [TestFixture]
    public class IntergrationTests
    {
        #region Test environment configuration (port number, path to temporary files)

        private readonly int portNumber = 5000;

        private string BaseUrl => $"http://localhost:{portNumber}";

        private static string TestDataPath => Path.Join(Path.GetTempPath(), "TileMapServiceTestData");

        private static string SettingsFilePath => Path.Join(TestDataPath, "testsettings.json");

        private static string Mbtiles1FilePath => Path.Join(TestDataPath, "test1.mbtiles");

        private static string Mbtiles2FilePath => Path.Join(TestDataPath, "test2.mbtiles");

        private static string Mbtiles3FilePath => Path.Join(TestDataPath, "test3.mbtiles");

        private static string LocalFilesPath => Path.Join(TestDataPath, "tiles3");

        #endregion

        private HttpClient client;

        // TODO: compare expected documents with MapCache capabilities output
        // TODO: check specific XML tags, not entire XML document

        [OneTimeSetUp]
        public async Task Setup()
        {
            RemoveTestData();
            PrepareTestData();

            this.client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
            };

            await this.CreateAndRunServiceHostAsync();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            // Skip deleting files to analyze them
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                RemoveTestData();
            }
        }

        [Test]
        public async Task GetTmsServicesAsync()
        {
            var r = await client.GetAsync("/tms");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_Services.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();
            expectedXml = UpdateXmlContents(expectedXml);

            CompareXml(expectedXml, actualXml);
        }

        [Test]
        public async Task GetTmsTileMapServiceAsync()
        {
            var r = await client.GetAsync("/tms/1.0.0");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_TileMapService.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();
            expectedXml = UpdateXmlContents(expectedXml);

            CompareXml(expectedXml, actualXml);
        }

        [Test]
        public async Task GetTmsTileMap1Async()
        {
            var r = await client.GetAsync("/tms/1.0.0/world-countries");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_TileMap1.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();
            expectedXml = UpdateXmlContents(expectedXml);

            CompareXml(expectedXml, actualXml);
        }

        [Test]
        public async Task GetTmsTileMap2Async()
        {
            var r = await client.GetAsync("/tms/1.0.0/world-satellite-imagery");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_TileMap2.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();
            expectedXml = UpdateXmlContents(expectedXml);

            CompareXml(expectedXml, actualXml);
        }

        [Test]
        public async Task GetTmsTileMap3Async()
        {
            var r = await client.GetAsync("/tms/1.0.0/source3");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_TileMap3.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();
            expectedXml = UpdateXmlContents(expectedXml);

            CompareXml(expectedXml, actualXml);
        }

        [Test]
        public async Task GetWmtsCapabilitiesAsync()
        {
            var r = await client.GetAsync("/wmts?request=GetCapabilities");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.wmts_GetCapabilities.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();
            expectedXml = UpdateXmlContents(expectedXml);

            CompareXml(expectedXml, actualXml);
        }

        [Test]
        public async Task GetTileXyzAsync()
        {
            var expected000 = new TileDataStub(0, 0, 0);

            var r1 = await client.GetAsync("/xyz/world-countries?x=0&y=0&z=0");
            Assert.AreEqual(HttpStatusCode.OK, r1.StatusCode);
            var actual1 = new TileDataStub(await r1.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual1);

            var r2 = await client.GetAsync("/xyz/world-satellite-imagery/0/0/0.jpg");
            Assert.AreEqual(HttpStatusCode.OK, r2.StatusCode);
            var actual2 = new TileDataStub(await r2.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual2);

            var expected312 = new TileDataStub(3, 1, 2);
            var r3 = await client.GetAsync("/xyz/source3/2/3/1.png");
            Assert.AreEqual(HttpStatusCode.OK, r3.StatusCode);
            var actual3 = new TileDataStub(await r3.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected312, actual3);

            var r4 = await client.GetAsync("/xyz/httpproxy/?x=0&y=0&z=0");
            Assert.AreEqual(HttpStatusCode.OK, r4.StatusCode);
            var actual4 = new TileDataStub(await r4.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual4);
        }

        [Test]
        public async Task GetTileTmsAsync()
        {
            var expected000 = new TileDataStub(0, 0, 0);

            var r1 = await client.GetAsync("/tms/1.0.0/world-countries/0/0/0.png");
            Assert.AreEqual(HttpStatusCode.OK, r1.StatusCode);
            var actual1 = new TileDataStub(await r1.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual1);

            var r2 = await client.GetAsync("/tms/1.0.0/world-satellite-imagery/0/0/0.jpg");
            Assert.AreEqual(HttpStatusCode.OK, r2.StatusCode);
            var actual2 = new TileDataStub(await r2.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual2);

            var expected312 = new TileDataStub(3, 1, 2);
            var r3 = await client.GetAsync("/tms/1.0.0/source3/2/3/2.png"); // TMS inverts Y axis
            Assert.AreEqual(HttpStatusCode.OK, r3.StatusCode);
            var actual3 = new TileDataStub(await r3.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected312, actual3);

            var r4 = await client.GetAsync("/tms/1.0.0/httpproxy/0/0/0.png");
            Assert.AreEqual(HttpStatusCode.OK, r4.StatusCode);
            var actual4 = new TileDataStub(await r4.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual4);
        }

        [Test]
        public async Task GetTileWmtsAsync()
        {
            var expected000 = new TileDataStub(0, 0, 0);

            var r1 = await client.GetAsync("/wmts?layer=world-countries&tilematrixset=EPSG%3A3857&Service=WMTS&Request=GetTile&Version=1.0.0&TileMatrix=0&TileCol=0&TileRow=0");
            Assert.AreEqual(HttpStatusCode.OK, r1.StatusCode);
            var actual1 = new TileDataStub(await r1.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual1);

            var r2 = await client.GetAsync("/wmts?layer=world-satellite-imagery&tilematrixset=EPSG%3A3857&Service=WMTS&Request=GetTile&Version=1.0.0&TileMatrix=0&TileCol=0&TileRow=0");
            Assert.AreEqual(HttpStatusCode.OK, r2.StatusCode);
            var actual2 = new TileDataStub(await r2.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual2);

            var expected312 = new TileDataStub(3, 1, 2);
            var r3 = await client.GetAsync("/wmts?layer=source3&tilematrixset=EPSG%3A3857&Service=WMTS&Request=GetTile&Version=1.0.0&TileMatrix=2&TileCol=3&TileRow=1");
            Assert.AreEqual(HttpStatusCode.OK, r3.StatusCode);
            var actual3 = new TileDataStub(await r3.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected312, actual3);

            var r4 = await client.GetAsync("/wmts?layer=httpproxy&tilematrixset=EPSG%3A3857&Service=WMTS&Request=GetTile&Version=1.0.0&TileMatrix=0&TileCol=0&TileRow=0");
            Assert.AreEqual(HttpStatusCode.OK, r4.StatusCode);
            var actual4 = new TileDataStub(await r4.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual4);
        }

        // TODO: Test for use of metadata bounds from mbtiles in WMTS source capabilities

        #region Utility methods

        private string UpdateXmlContents(string xml)
        {
            var s11 = "http://localhost:5000";
            var s12 = "http://localhost:" + this.portNumber.ToString(CultureInfo.InvariantCulture);

            return xml.Replace(s11, s12);
        }

        private static void CompareXml(string expectedXml, string actualXml)
        {
            var comparer = new NetBike.XmlUnit.XmlComparer
            {
                NormalizeText = true,
                Analyzer = NetBike.XmlUnit.XmlAnalyzer.Custom()
                    .SetEqual(NetBike.XmlUnit.XmlComparisonType.NodeListSequence)
            };

            using var expectedReader = new StringReader(expectedXml);
            using var actualReader = new StringReader(actualXml);

            var result = comparer.Compare(expectedReader, actualReader);

            if (!result.IsEqual)
            {
                Assert.Fail(result.Differences.First().Difference.ToString());
            }
        }

        private static byte[] ReadResource(string id)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetName().Name + "." + id;

            byte[] data = null;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                data = new byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);
            }

            return data;
        }

        #endregion

        #region setup / teardown

        private void PrepareTestData()
        {
            if (!Directory.Exists(TestDataPath))
            {
                Directory.CreateDirectory(TestDataPath);
            }

            // Create config file
            var tileSources = new[]
            {
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeMBTiles,
                    Id = "world-countries",
                    Location = Mbtiles1FilePath,
                    MinZoom = 0,
                    MaxZoom = 18,
                },
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeMBTiles,
                    Id = "world-satellite-imagery",
                    Title = null, // will be used from mbtiles metadata
                    Location = Mbtiles2FilePath,
                },
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeMBTiles,
                    Id = "caspian-sea",
                    Title = null, // will be used from mbtiles metadata
                    Location = Mbtiles3FilePath,
                },
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeLocalFiles,
                    Id = "source3",
                    Title = "Tile Source 3",
                    Location = LocalFilesPath + "\\{z}\\{x}\\{y}.png",
                    MinZoom = 0,
                    MaxZoom = 2,
                    Format = ImageFormats.Png,
                    Tms = false,
                },
                new SourceConfiguration
                {
                    Type = SourceConfiguration.TypeXyz,
                    Id = "httpproxy",
                    Title = "HTTP proxy to world-countries",
                    Location = this.BaseUrl + "/xyz/world-countries/{z}/{x}/{y}.png",
                    Format = ImageFormats.Png,
                    MinZoom = 0,
                    MaxZoom = 2,
                },
                // TODO: EPSG:4326 source
            };

            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(new { Sources = tileSources }));

            // Create and fill MBTiles databases
            {
                var db1 = MBT.Repository.CreateEmptyDatabase(Mbtiles1FilePath);
                db1.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyName, "World Countries"));
                db1.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyFormat, ImageFormats.Png));
                db1.AddTile(0, 0, 0, new TileDataStub(0, 0, 0).ToByteArray());
            }

            {
                var db2 = MBT.Repository.CreateEmptyDatabase(Mbtiles2FilePath);
                db2.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyName, "World Satellite Imagery"));
                db2.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyFormat, ImageFormats.Jpeg));
                db2.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyMinZoom, "0"));
                db2.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyMaxZoom, "5"));
                db2.AddTile(0, 0, 0, new TileDataStub(0, 0, 0).ToByteArray());
            }

            {
                var db3 = MBT.Repository.CreateEmptyDatabase(Mbtiles3FilePath);
                db3.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyName, "Caspian Sea"));
                db3.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyFormat, ImageFormats.Png));
                db3.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyMinZoom, "5"));
                db3.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyMaxZoom, "10"));
                db3.AddMetadataItem(new MBT.MetadataItem(MBT.MetadataItem.KeyBounds, "46.4012447459838,35.79530426558146,55.064448928947485,47.40446363441855"));
            }

            CreateLocalTiles(tileSources[3]);
        }

        private static void CreateLocalTiles(SourceConfiguration tileSource)
        {
            // Create files Z\X\Y structure
            for (var z = tileSource.MinZoom.Value; z <= tileSource.MaxZoom.Value; z++)
            {
                Directory.CreateDirectory(Path.Join(LocalFilesPath, z.ToString(CultureInfo.InvariantCulture)));
                for (var x = 0; x < 1 << z; x++)
                {
                    var zxPath = Path.Join(LocalFilesPath, z.ToString(CultureInfo.InvariantCulture), x.ToString(CultureInfo.InvariantCulture));
                    Directory.CreateDirectory(zxPath);
                    for (var y = 0; y < 1 << z; y++)
                    {
                        var tilePath = Path.Join(zxPath, y.ToString(CultureInfo.InvariantCulture)) + "." + tileSource.Format;
                        var tile = new TileDataStub(x, y, z);
                        File.WriteAllBytes(tilePath, tile.ToByteArray());
                    }
                }
            }
        }

        private async Task CreateAndRunServiceHostAsync()
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
                            options.Listen(IPAddress.Loopback, portNumber);
                        });
                    })
                    .Build();

            await (host.Services.GetService(typeof(ITileSourceFabric)) as ITileSourceFabric).InitAsync();

            var _ = host.RunAsync();
        }

        private static void RemoveTestData()
        {
            if (Directory.Exists(TestDataPath))
            {
                var dir = new DirectoryInfo(TestDataPath);
                dir.Delete(true);
            }
        }

        #endregion
    }
}
