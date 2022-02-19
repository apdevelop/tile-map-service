using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace TileMapService.Tests
{
    class TestsUtility
    {
        public static async Task<IHost> CreateAndRunServiceHostAsync(string json, int port)
        {
            // https://docs.microsoft.com/ru-ru/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0
            var host = Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration(configurationBuilder =>
                    {
                        var configuration = new ConfigurationBuilder()
                            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
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
                            options.Listen(IPAddress.Loopback, port);
                        });
                    })
                    .Build();

            await (host.Services.GetService(typeof(ITileSourceFabric)) as ITileSourceFabric).InitAsync();

            var _ = host.RunAsync();

            return host;
        }

        public static string UpdateXmlContents(string xml, int portNumber)
        {
            var s11 = "http://localhost:5000";
            var s12 = "http://localhost:" + portNumber.ToString(CultureInfo.InvariantCulture);

            return xml.Replace(s11, s12);
        }

        public static void CompareXml(string expectedXml, string actualXml)
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

        public static byte[] ReadResource(string id)
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
    }
}
