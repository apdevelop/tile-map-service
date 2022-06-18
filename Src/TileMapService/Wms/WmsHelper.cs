using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SkiaSharp;

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
                default: throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
            }

            return wmsVersion;
        }

        public static async Task DrawLayerAsync(
            ITileSource source,
            int width,
            int height,
            Models.Bounds boundingBox,
            SKCanvas outputCanvas,
            bool isTransparent,
            uint backgroundColor)
        {
            // TODO: check SRS support in source
            if ((String.Compare(source.Configuration.Type, SourceConfiguration.TypeWms, StringComparison.OrdinalIgnoreCase) == 0) &&
                (source.Configuration.Cache == null))
            {
                // Cascading GetMap request to WMS source as single GetMap request
                var imageData = await ((TileSources.HttpTileSource)source).GetWmsMapAsync(width, height, boundingBox, isTransparent, backgroundColor);
                if (imageData != null)
                {
                    using var sourceImage = SKImage.FromEncodedData(imageData);
                    outputCanvas.DrawImage(sourceImage, SKRect.Create(0, 0, sourceImage.Width, sourceImage.Height));
                }
            }
            else if (String.Compare(source.Configuration.Type, SourceConfiguration.TypeGeoTiff, StringComparison.OrdinalIgnoreCase) == 0)
            {
                // Get part of GeoTIFF source image in single request
                using var image = await ((TileSources.RasterTileSource)source).GetImagePartAsync(width, height, boundingBox, backgroundColor);
                if (image != null)
                {
                    outputCanvas.DrawImage(image, SKRect.Create(0, 0, image.Width, image.Height));
                }
            }
            else
            {
                var tileCoordinates = WmsHelper.BuildTileCoordinatesList(boundingBox, width);
                var sourceTiles = await GetSourceTilesAsync(source, tileCoordinates);
                if (sourceTiles.Count > 0)
                {
                    WmsHelper.DrawWebMercatorTilesToRasterCanvas(outputCanvas, width, height, boundingBox, sourceTiles, backgroundColor, WebMercator.TileSize);
                }
            }
        }

        private static async Task<List<Models.TileDataset>> GetSourceTilesAsync(
            ITileSource source,
            IList<Models.TileCoordinates> tileCoordinates)
        {
            var sourceTiles = new List<Models.TileDataset>(tileCoordinates.Count);
            foreach (var tc in tileCoordinates)
            {
                // 180 degrees
                var tileCount = WebMercator.TileCount(tc.Z);
                var x = tc.X % tileCount;

                var tileData = await source.GetTileAsync(x, WebMercator.FlipYCoordinate(tc.Y, tc.Z), tc.Z);
                if (tileData != null)
                {
                    sourceTiles.Add(new Models.TileDataset(tc.X, tc.Y, tc.Z, tileData));
                }
            }

            return sourceTiles;
        }

        private static void DrawWebMercatorTilesToRasterCanvas(
            SKCanvas outputCanvas,
            int width,
            int height,
            Models.Bounds boundingBox,
            IList<Models.TileDataset> sourceTiles,
            uint backgroundColor,
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
            canvas.Clear(new SKColor(backgroundColor));

            // Draw all tiles
            foreach (var sourceTile in sourceTiles)
            {
                var offsetX = (sourceTile.X - tileMinX) * tileSize;
                var offsetY = (sourceTile.Y - tileMinY) * tileSize;
                using var sourceImage = SKImage.FromEncodedData(sourceTile.ImageData);
                canvas.DrawImage(sourceImage, SKRect.Create(offsetX, offsetY, tileSize, tileSize)); // Source tile scaled to dest rectangle, if needed
            }

            // Clip and scale to requested size of output image
            var geoBBox = EntitiesConverter.MapRectangleToGeographicalBounds(boundingBox);
            var pixelOffsetX = WebMercator.LongitudeToPixelXAtZoom(geoBBox.MinLongitude, zoom) - tileSize * tileMinX;
            var pixelOffsetY = WebMercator.LatitudeToPixelYAtZoom(geoBBox.MaxLatitude, zoom) - tileSize * tileMinY;
            var pixelWidth = WebMercator.LongitudeToPixelXAtZoom(geoBBox.MaxLongitude, zoom) - WebMercator.LongitudeToPixelXAtZoom(geoBBox.MinLongitude, zoom);
            var pixelHeight = WebMercator.LatitudeToPixelYAtZoom(geoBBox.MinLatitude, zoom) - WebMercator.LatitudeToPixelYAtZoom(geoBBox.MaxLatitude, zoom);
            var sourceRectangle = SKRect.Create((float)pixelOffsetX, (float)pixelOffsetY, (float)pixelWidth, (float)pixelHeight);
            var destRectangle = SKRect.Create(0, 0, width, height);

            using SKImage canvasImage = surface.Snapshot();
            outputCanvas.DrawImage(canvasImage, sourceRectangle, destRectangle, new SKPaint { FilterQuality = SKFilterQuality.High, });
        }

        public static List<Models.TileCoordinates> BuildTileCoordinatesList(Models.Bounds boundingBox, int width)
        {
            var geoBBox = EntitiesConverter.MapRectangleToGeographicalBounds(boundingBox);
            var zoomLevel = FindOptimalTileZoomLevel(width, geoBBox);
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

        private static int FindOptimalTileZoomLevel(int width, Models.GeographicalBounds geoBBox)
        {
            var mapSize = WebMercator.MapSize(width, geoBBox.MinLongitude, geoBBox.MaxLongitude);
            var minZoom = 0;
            var minDistance = Double.MaxValue;
            for (var zoom = 0; zoom < 24; zoom++) // TODO: range?
            {
                var mapSizeAtZoom = WebMercator.MapSize(zoom, WebMercator.DefaultTileSize); // TODO: ? use tile size parameter instead of const
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
