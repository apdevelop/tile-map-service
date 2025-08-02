using System.Linq;

using NUnit.Framework;

using U = TileMapService.Utils;

namespace TileMapService.Tests
{
    [TestFixture]
    public class UtilsTests
    {
        [Test]
        public void WebMercatorIsInsideBBox()
        {
            Assert.Multiple(() =>
            {
                Assert.That(U.WebMercator.IsInsideBBox(0, 0, 0, U.SrsCodes.EPSG3857));
                Assert.That(U.WebMercator.IsInsideBBox(0, 0, 2, U.SrsCodes.EPSG3857));
                Assert.That(U.WebMercator.IsInsideBBox(1, 1, 2, U.SrsCodes.EPSG3857));
                Assert.That(U.WebMercator.IsInsideBBox(3, 3, 2, U.SrsCodes.EPSG3857));
                Assert.That(U.WebMercator.IsInsideBBox(1, 2, 0, U.SrsCodes.EPSG3857), Is.False);
                Assert.That(U.WebMercator.IsInsideBBox(2, 2, 1, U.SrsCodes.EPSG3857), Is.False);
            });
        }

        [Test]
        public void BuildTileCoordinatesList()
        {
            // BBOX is larger than standard EPSG:3857 bounds
            var boundingBox = Models.Bounds.FromCommaSeparatedString("-20037508,-25776731,20037508,25776731");
            var coordinates = Wms.WmsHelper.BuildTileCoordinatesList(boundingBox, 546);
            Assert.That(coordinates, Has.Length.EqualTo(4));
            Assert.That(coordinates.All(c => c.Z == 1));
            Assert.That(coordinates[0].X, Is.EqualTo(0));
            Assert.That(coordinates[0].Y, Is.EqualTo(0));
        }
    }
}
