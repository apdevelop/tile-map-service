using SkiaSharp;
using System;
using System.Collections.Generic;
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
                case Identifiers.Version111: { wmsVersion = Version.Version111; break; }
                case Identifiers.Version130: { wmsVersion = Version.Version130; break; }
                default: throw new ArgumentOutOfRangeException();
            }

            return wmsVersion;
        }

        public static void DrawWebMercatorTilesToRasterCanvas(
            SKCanvas outputCanvas,
            int width, int height,
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

            var imageInfo = new SKImageInfo(
                width: canvasWidth,
                height: canvasHeight,
                colorType: SKColorType.Rgba8888,
                alphaType: SKAlphaType.Premul);

            using var surface = SKSurface.Create(imageInfo);
            using var canvas = surface.Canvas;
            canvas.Clear(new SKColor((uint)backgroundColor)); // TODO: ? uint parameter

            // Draw all tiles without scaling
            foreach (var sourceTile in sourceTiles)
            {
                var offsetX = (sourceTile.X - tileMinX) * tileSize;
                var offsetY = (sourceTile.Y - tileMinY) * tileSize;
                using var sourceImage = SKImage.FromEncodedData(sourceTile.ImageData);
                canvas.DrawImage(sourceImage, new SKRect(offsetX, offsetY, offsetX + sourceImage.Width, offsetY + sourceImage.Height));
            }

            var geoBBox = EntitiesConverter.MapRectangleToGeographicalBounds(boundingBox);
            var pixelOffsetX = WebMercator.LongitudeToPixelXAtZoom(geoBBox.MinLongitude, zoom) - tileSize * tileMinX;
            var pixelOffsetY = WebMercator.LatitudeToPixelYAtZoom(geoBBox.MaxLatitude, zoom) - tileSize * tileMinY;
            var pixelWidth = WebMercator.LongitudeToPixelXAtZoom(geoBBox.MaxLongitude, zoom) - WebMercator.LongitudeToPixelXAtZoom(geoBBox.MinLongitude, zoom);
            var pixelHeight = WebMercator.LatitudeToPixelYAtZoom(geoBBox.MinLatitude, zoom) - WebMercator.LatitudeToPixelYAtZoom(geoBBox.MaxLatitude, zoom);
            var sourceRectangle = new SKRect(
                (int)Math.Round(pixelOffsetX),
                (int)Math.Round(pixelOffsetY),
                (int)Math.Round(pixelOffsetX) + (int)Math.Round(pixelWidth),
                (int)Math.Round(pixelOffsetY) + (int)Math.Round(pixelHeight));

            using SKImage canvasImage = surface.Snapshot();

            // Clip and scale to requested size of output image
            var destRectangle = new SKRect(0, 0, width, height);
            outputCanvas.DrawImage(canvasImage, sourceRectangle, destRectangle);
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
