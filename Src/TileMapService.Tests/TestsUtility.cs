using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TileMapService.Tests
{
    static class TestsUtility
    {
        public static async Task<IHost> CreateAndRunServiceHostAsync(string json, int port)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
            builder.WebHost.ConfigureKestrel((context, serverOptions) => serverOptions.Listen(IPAddress.Loopback, port));
            builder.Services.AddControllers().AddApplicationPart(typeof(TileSourceFabric).Assembly);
            builder.Services.AddSingleton<ITileSourceFabric, TileSourceFabric>();

            var app = builder.Build();
            app.MapControllers();

            await (app.Services.GetService(typeof(ITileSourceFabric)) as ITileSourceFabric).InitAsync();

            _ = app.RunAsync();

            return app;
        }

        public static string UpdateXmlContents(string xml, int portNumber)
        {
            var s11 = "http://localhost:5000";
            var s12 = "http://localhost:" + portNumber.ToString(CultureInfo.InvariantCulture);

            return xml.Replace(s11, s12);
        }

        public static string CompareXml(string expectedXml, string actualXml)
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

            return result.IsEqual 
                ? null 
                : result.Differences.First().Difference.ToString();
        }

        public static byte[] ReadResource(string id)
        {
            var assembly = typeof(TestsUtility).Assembly;
            var resourceName = typeof(TestsUtility).Namespace + "." + id;

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
