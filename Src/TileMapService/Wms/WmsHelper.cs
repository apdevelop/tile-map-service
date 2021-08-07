using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

using TileMapService.Utils;

namespace TileMapService.Wms
{
    static class WmsHelper
    {
        public static Version GetWmsVersion(string version)
        {
            Version wmsVersion;
            switch (version)
            {
                case Identifiers.Version111: { wmsVersion = Wms.Version.Version111; break; }
                case Identifiers.Version130: { wmsVersion = Wms.Version.Version130; break; }
                default: throw new ArgumentOutOfRangeException();
            }

            return wmsVersion;
        }

        public static void DrawWebMercatorTilesToRasterCanvas(
            Bitmap outputImage,
            Models.Bounds boundingBox,
            IList<Models.TileDataset> sourceTiles,
            int backgroundColor,
            int tileSize)
        {
            var zoom = sourceTiles[0].Z;
            var tileMinX = sourceTiles.Min(t => t.X);
            var tileMinY = sourceTiles.Min(t => t.Y);
            var tilesCountX = sourceTiles.Max(t => t.X) - tileMinX + 1;
            var tilesCountY = sourceTiles.Max(t => t.Y) - tileMinY + 1;
            var canvasWidth = tilesCountX * tileSize;
            var canvasHeight = tilesCountY * tileSize;

            var canvas = ImageHelper.CreateEmptyPngImage(canvasWidth, canvasHeight, backgroundColor);

            using (var canvasImageStream = new MemoryStream(canvas))
            {
                using (var canvasImage = new Bitmap(canvasImageStream))
                {
                    using (var graphics = Graphics.FromImage(canvasImage))
                    {
                        // Draw all tiles without scaling
                        foreach (var sourceTile in sourceTiles)
                        {
                            var offsetX = (sourceTile.X - tileMinX) * tileSize;
                            var offsetY = (sourceTile.Y - tileMinY) * tileSize;
                            using (var sourceStream = new MemoryStream(sourceTile.ImageData))
                            {
                                using (var sourceImage = Image.FromStream(sourceStream))
                                {
                                    if ((sourceImage.HorizontalResolution == canvasImage.HorizontalResolution) &&
                                        (sourceImage.VerticalResolution == canvasImage.VerticalResolution))
                                    {
                                        graphics.DrawImageUnscaled(sourceImage, offsetX, offsetY);
                                    }
                                    else
                                    {
                                        graphics.DrawImage(sourceImage, new Rectangle(offsetX, offsetY, sourceImage.Width, sourceImage.Height));
                                    }
                                }
                            }
                        }
                    }

                    var geoBBox = EntitiesConverter.MapRectangleToGeographicalBounds(boundingBox);
                    var pixelOffsetX = WebMercator.LongitudeToPixelXAtZoom(geoBBox.MinLongitude, zoom) - tileSize * tileMinX;
                    var pixelOffsetY = WebMercator.LatitudeToPixelYAtZoom(geoBBox.MaxLatitude, zoom) - tileSize * tileMinY;
                    var pixelWidth = WebMercator.LongitudeToPixelXAtZoom(geoBBox.MaxLongitude, zoom) - WebMercator.LongitudeToPixelXAtZoom(geoBBox.MinLongitude, zoom);
                    var pixelHeight = WebMercator.LatitudeToPixelYAtZoom(geoBBox.MinLatitude, zoom) - WebMercator.LatitudeToPixelYAtZoom(geoBBox.MaxLatitude, zoom);
                    var sourceRectangle = new Rectangle(
                        (int)Math.Round(pixelOffsetX),
                        (int)Math.Round(pixelOffsetY),
                        (int)Math.Round(pixelWidth),
                        (int)Math.Round(pixelHeight));

                    // Clip and scale to requested size of output image
                    var destRectangle = new Rectangle(0, 0, outputImage.Width, outputImage.Height);
                    using (var graphics = Graphics.FromImage(outputImage))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic;
                        graphics.DrawImage(canvasImage, destRectangle, sourceRectangle, GraphicsUnit.Pixel);
                    }
                }
            }
        }

        public static List<Models.TileCoordinates> BuildTileCoordinatesList(Models.Bounds boundingBox, int width)
        {
            var geoBBox = EntitiesConverter.MapRectangleToGeographicalBounds(boundingBox);
            var zoomLevel = FindOptimalTileZoom(width, geoBBox);
            var tileCoordMin = GetTileCoordinatesAtPoint(geoBBox.MinLongitude, geoBBox.MinLatitude, zoomLevel);
            var tileCoordMax = GetTileCoordinatesAtPoint(geoBBox.MaxLongitude, geoBBox.MaxLatitude, zoomLevel);

            var tileCoordinates = new List<Models.TileCoordinates>();
            for (var tileX = tileCoordMin.X; tileX <= tileCoordMax.X; tileX++)
            {
                for (var tileY = tileCoordMax.Y; tileY <= tileCoordMin.Y; tileY++)
                {
                    tileCoordinates.Add(new Models.TileCoordinates(tileX, tileY, zoomLevel));
                }
            }

            return tileCoordinates;
        }

        private static int FindOptimalTileZoom(int width, Models.GeographicalBounds geoBBox)
        {
            var mapSize = WebMercator.MapSize(width, geoBBox.MinLongitude, geoBBox.MaxLongitude);

            var minZoom = 0;
            var minDistance = Double.MaxValue;
            for (var zoom = 0; zoom < 24; zoom++) // TODO: range?
            {
                var mapSizeAtZoom = WebMercator.MapSize(zoom);
                var distance = Math.Abs(mapSize - mapSizeAtZoom);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minZoom = zoom;
                }
            }

            return minZoom;
        }

        private static Models.TileCoordinates GetTileCoordinatesAtPoint(double longitude, double latitude, int zoomLevel)
        {
            return new Models.TileCoordinates(
                (int)Math.Floor(WebMercator.TileCoordinateXAtZoom(longitude, zoomLevel)),
                (int)Math.Floor(WebMercator.TileCoordinateYAtZoom(latitude, zoomLevel)),
                zoomLevel);
        }
    }
}
