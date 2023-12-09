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
    }
}
