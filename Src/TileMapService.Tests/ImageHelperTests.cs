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

            Assert.AreEqual(80, props.ImageWidth);
            Assert.AreEqual(60, props.ImageHeight);
            Assert.AreEqual(3857, props.Srid);
            Assert.AreEqual(616131.764, props.ProjectedBounds.Left, 0.001);
            Assert.AreEqual(637857.616, props.ProjectedBounds.Right, 0.001);
            Assert.AreEqual(-166644.204, props.ProjectedBounds.Bottom, 0.001);
            Assert.AreEqual(-150349.815, props.ProjectedBounds.Top, 0.001);
        }

        [Test]
        public void ReadGeoTiffProperties4326()
        {
            var props = U.ImageHelper.ReadGeoTiffProperties(Path.Combine(TestConfiguration.DataPath, Sample1_4326));

            Assert.AreEqual(80, props.ImageWidth);
            Assert.AreEqual(60, props.ImageHeight);
            Assert.AreEqual(4326, props.Srid);
            Assert.AreEqual(5.534806, props.GeographicalBounds.MinLongitude, 0.001);
            Assert.AreEqual(5.729972, props.GeographicalBounds.MaxLongitude, 0.001);
            Assert.AreEqual(-1.496728, props.GeographicalBounds.MinLatitude, 0.001);
            Assert.AreEqual(-1.350353, props.GeographicalBounds.MaxLatitude, 0.001);
        }

        [Test]
        public void CreateAndReadGeoTiffProperties()
        {
            var path = Path.Combine(TestConfiguration.DataPath, "generated-image.tiff");

            var pixels = new byte[64 * 32 * 4]; // 64x32 RGBA
            var image = U.ImageHelper.CreateTiffImage(pixels, 64, 32, new Models.Bounds(10, 5, 650, 325), true);
            File.WriteAllBytes(path, image);

            var props = U.ImageHelper.ReadGeoTiffProperties(path);

            Assert.AreEqual(64, props.ImageWidth);
            Assert.AreEqual(32, props.ImageHeight);
            Assert.AreEqual(3857, props.Srid);
        }
    }
}
