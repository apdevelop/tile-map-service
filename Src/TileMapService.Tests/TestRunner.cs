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

            Assert.IsTrue(CompareXml(expectedXml, actualXml));
        }

        public async Task GetTmsTileMapServiceAsync()
        {
            var r = await client.GetAsync("/tms/1.0.0");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_TileMapService.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();

            Assert.IsTrue(CompareXml(expectedXml, actualXml));
        }

        public async Task GetTmsTileMap1Async()
        {
            var r = await client.GetAsync("/tms/1.0.0/source1");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_TileMap1.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();

            Assert.IsTrue(CompareXml(expectedXml, actualXml));
        }

        public async Task GetTmsTileMap2Async()
        {
            var r = await client.GetAsync("/tms/1.0.0/source2");
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);

            var expectedXml = Encoding.UTF8.GetString(ReadResource("Expected.tms_capabilities_TileMap2.xml"));
            var actualXml = await r.Content.ReadAsStringAsync();

            Assert.IsTrue(CompareXml(expectedXml, actualXml));
        }

        #region Utility methods

        private static bool CompareXml(string xml1, string xml2)
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
                        var xmldiff = new XmlDiff(XmlDiffOptions.IgnoreChildOrder | XmlDiffOptions.IgnoreWhitespace);
                        return xmldiff.Compare(xmlReader1, xmlReader2, writer);
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
