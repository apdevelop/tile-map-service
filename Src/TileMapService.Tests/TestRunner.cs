using Microsoft.XmlDiffPatch; // TODO: there is warning to use Microsoft.XmlDiffPatch in net5 project
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TileMapService.Tests
{
    class TestRunner
    {
        private readonly string baseUrl;

        private readonly HttpClient client;

        public TestRunner(string baseUrl)
        {
            this.baseUrl = baseUrl;
            this.client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
            };
        }

        // TODO: compare expected documents with MapCache capabilities output

        public async Task GetTmsServicesAsync()
        {
            var r = await client.GetAsync("/tms");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_Services.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();

            CompareXml(expectedXml, actualXml);
        }

        public async Task GetTmsTileMapServiceAsync()
        {
            var r = await client.GetAsync("/tms/1.0.0");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_TileMapService.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();

            CompareXml(expectedXml, actualXml);
        }

        public async Task GetTmsTileMap1Async()
        {
            var r = await client.GetAsync("/tms/1.0.0/source1");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_TileMap1.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();

            CompareXml(expectedXml, actualXml);
        }

        public async Task GetTmsTileMap2Async()
        {
            var r = await client.GetAsync("/tms/1.0.0/source2");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_TileMap2.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();

            CompareXml(expectedXml, actualXml);
        }

        public async Task GetTmsTileMap3Async()
        {
            var r = await client.GetAsync("/tms/1.0.0/source3");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_TileMap3.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();

            CompareXml(expectedXml, actualXml);
        }

        public async Task GetWmtsCapabilitiesAsync()
        {
            var r = await client.GetAsync("/wmts?request=GetCapabilities");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.wmts_GetCapabilities.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();

            CompareXml(expectedXml, actualXml);
        }

        public async Task GetTileXyzAsync()
        {
            var expected000 = new TileDataStub(0, 0, 0);

            var r1 = await client.GetAsync("/xyz/source1?x=0&y=0&z=0");
            Assert.AreEqual(HttpStatusCode.OK, r1.StatusCode);
            var actual1 = new TileDataStub(await r1.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual1);

            var r2 = await client.GetAsync("/xyz/source2/0/0/0.jpg");
            Assert.AreEqual(HttpStatusCode.OK, r2.StatusCode);
            var actual2 = new TileDataStub(await r2.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual2);

            var expected312 = new TileDataStub(3, 1, 2);
            var r3 = await client.GetAsync("/xyz/source3/2/3/1.png");
            Assert.AreEqual(HttpStatusCode.OK, r3.StatusCode);
            var actual3 = new TileDataStub(await r3.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected312, actual3);
        }

        public async Task GetTileTmsAsync()
        {
            var expected000 = new TileDataStub(0, 0, 0);

            var r1 = await client.GetAsync("/tms/1.0.0/source1/0/0/0.png");
            Assert.AreEqual(HttpStatusCode.OK, r1.StatusCode);
            var actual1 = new TileDataStub(await r1.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual1);

            var r2 = await client.GetAsync("/tms/1.0.0/source2/0/0/0.jpg");
            Assert.AreEqual(HttpStatusCode.OK, r2.StatusCode);
            var actual2 = new TileDataStub(await r2.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual2);

            var expected312 = new TileDataStub(3, 1, 2);
            var r3 = await client.GetAsync("/tms/1.0.0/source3/2/3/2.png"); // TMS inverts Y axis
            Assert.AreEqual(HttpStatusCode.OK, r3.StatusCode);
            var actual3 = new TileDataStub(await r3.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected312, actual3);
        }

        public async Task GetTileWmtsAsync()
        {
            var expected000 = new TileDataStub(0, 0, 0);

            var r1 = await client.GetAsync("/wmts?layer=source1&tilematrixset=EPSG%3A3857&Service=WMTS&Request=GetTile&Version=1.0.0&TileMatrix=0&TileCol=0&TileRow=0");
            Assert.AreEqual(HttpStatusCode.OK, r1.StatusCode);
            var actual1 = new TileDataStub(await r1.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual1);

            var r2 = await client.GetAsync("/wmts?layer=source2&tilematrixset=EPSG%3A3857&Service=WMTS&Request=GetTile&Version=1.0.0&TileMatrix=0&TileCol=0&TileRow=0");
            Assert.AreEqual(HttpStatusCode.OK, r2.StatusCode);
            var actual2 = new TileDataStub(await r2.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected000, actual2);

            var expected312 = new TileDataStub(3, 1, 2);
            var r3 = await client.GetAsync("/wmts?layer=source3&tilematrixset=EPSG%3A3857&Service=WMTS&Request=GetTile&Version=1.0.0&TileMatrix=2&TileCol=3&TileRow=1");
            Assert.AreEqual(HttpStatusCode.OK, r3.StatusCode);
            var actual3 = new TileDataStub(await r3.Content.ReadAsByteArrayAsync());
            Assert.AreEqual(expected312, actual3);
        }

        #region Utility methods

        private static void CompareXml(string xml1, string xml2)
        {
            var xmlDoc1 = new XmlDocument();
            xmlDoc1.LoadXml(xml1);

            var xmlDoc2 = new XmlDocument();
            xmlDoc2.LoadXml(xml2);

            var sb = new StringBuilder();

            using (var xmlReader1 = new XmlNodeReader(xmlDoc1))
            {
                using (var xmlReader2 = new XmlNodeReader(xmlDoc2))
                {
                    using (var writer = XmlWriter.Create(sb))
                    {
                        var xmldiff = new XmlDiff(XmlDiffOptions.IgnoreChildOrder | XmlDiffOptions.IgnoreComments | XmlDiffOptions.IgnoreWhitespace);
                        var result = xmldiff.Compare(xmlReader1, xmlReader2, writer);

                        Assert.IsTrue(result);
                    }
                }
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
    }
}
