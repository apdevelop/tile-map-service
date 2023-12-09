using System.IO;

using NUnit.Framework;

using U = TileMapService.Utils;

namespace TileMapService.Tests
{
    [TestFixture]
    public class ImageHelperTests
    {
        private const string Sample1_3857 = "GeoTiff.sample1-epsg-3857.tiff";
        private const string Sample1_4326 = "GeoTiff.sample1-epsg-4326.tiff";

        [OneTimeSetUp]
        public void Setup()
        {
            RemoveTestData();
            PrepareTestData();
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

        private static void PrepareTestData()
        {
            if (!Directory.Exists(TestConfiguration.DataPath))
            {
                Directory.CreateDirectory(TestConfiguration.DataPath);
            }

            foreach (var name in new[] { Sample1_3857, Sample1_4326 })
            {
                File.WriteAllBytes(Path.Combine(TestConfiguration.DataPath, name), TestsUtility.ReadResource(name));
            }
        }

        private static void RemoveTestData()
        {
            if (Directory.Exists(TestConfiguration.DataPath))
            {
                new DirectoryInfo(TestConfiguration.DataPath).Delete(true);
            }
        }

        [Test]
        public void ReadGeoTiffProperties3857()
        {
            var props = U.ImageHelper.ReadGeoTiffProperties(Path.Combine(TestConfiguration.DataPath, Sample1_3857));

            Assert.Multiple(() =>
            {
                Assert.That(props.ImageWidth, Is.EqualTo(80));
                Assert.That(props.ImageHeight, Is.EqualTo(60));
                Assert.That(props.Srid, Is.EqualTo(3857));
                Assert.That(props.ProjectedBounds.Left, Is.EqualTo(616131.764).Within(0.001));
                Assert.That(props.ProjectedBounds.Right, Is.EqualTo(637857.616).Within(0.001));
                Assert.That(props.ProjectedBounds.Bottom, Is.EqualTo(-166644.204).Within(0.001));
                Assert.That(props.ProjectedBounds.Top, Is.EqualTo(-150349.815).Within(0.001));
            });
        }

        [Test]
        public void ReadGeoTiffProperties4326()
        {
            var props = U.ImageHelper.ReadGeoTiffProperties(Path.Combine(TestConfiguration.DataPath, Sample1_4326));

            Assert.Multiple(() =>
            {
                Assert.That(props.ImageWidth, Is.EqualTo(80));
                Assert.That(props.ImageHeight, Is.EqualTo(60));
                Assert.That(props.Srid, Is.EqualTo(4326));
                Assert.That(props.GeographicalBounds.MinLongitude, Is.EqualTo(5.534806).Within(0.001));
                Assert.That(props.GeographicalBounds.MaxLongitude, Is.EqualTo(5.729972).Within(0.001));
                Assert.That(props.GeographicalBounds.MinLatitude, Is.EqualTo(-1.496728).Within(0.001));
                Assert.That(props.GeographicalBounds.MaxLatitude, Is.EqualTo(-1.350353).Within(0.001));
            });
        }

        [Test]
        public void CreateAndReadGeoTiffProperties()
        {
            var path = Path.Combine(TestConfiguration.DataPath, "generated-image.tiff");

            var pixels = new byte[64 * 32 * 4]; // 64x32 RGBA
            var image = U.ImageHelper.CreateTiffImage(pixels, 64, 32, new Models.Bounds(10, 5, 650, 325), true);
            File.WriteAllBytes(path, image);

            var props = U.ImageHelper.ReadGeoTiffProperties(path);
            Assert.Multiple(() =>
            {
                Assert.That(props.ImageWidth, Is.EqualTo(64));
                Assert.That(props.ImageHeight, Is.EqualTo(32));
                Assert.That(props.Srid, Is.EqualTo(3857));
            });
        }
    }
}
