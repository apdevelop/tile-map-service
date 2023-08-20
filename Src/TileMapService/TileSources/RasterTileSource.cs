using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using BitMiracle.LibTiff.Classic;
using SkiaSharp;

using M = TileMapService.Models;
using U = TileMapService.Utils;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with tiles from GeoTIFF raster image file.
    /// </summary>
    /// <remarks>
    /// Supports currently only EPSG:4326 and EPSG:3857 coordinate system of input GeoTIFF.
    /// http://geotiff.maptools.org/spec/geotiff6.html
    /// </remarks>
    class RasterTileSource : ITileSource
    {
        private SourceConfiguration configuration;

        private M.RasterProperties? rasterProperties;

        public RasterTileSource(SourceConfiguration configuration)
        {
            // TODO: report real WMS layer bounds from raster bounds
            // TODO: EPSG:4326 tile response (for WMTS, WMS)
            // TODO: add support for MapInfo RASTER files
            // TODO: add support for multiple rasters (directory with rasters)

            if (String.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException("Source identifier is null or empty string.");
            }

            if (String.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException("Source location is null or empty string.");
            }

            this.configuration = configuration; // Will be changed later in InitAsync
        }

        #region ITileSource implementation

        Task ITileSource.InitAsync()
        {
            if (String.IsNullOrEmpty(this.configuration.Location))
            {
                throw new InvalidOperationException("configuration.Location is null or empty.");
            }

            Tiff.SetErrorHandler(new DisableErrorHandler()); // TODO: ? redirect output?

            this.rasterProperties = U.ImageHelper.ReadGeoTiffProperties(this.configuration.Location);

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                this.configuration.Id :
                this.configuration.Title;

            var minZoom = this.configuration.MinZoom ?? 0;
            var maxZoom = this.configuration.MaxZoom ?? 24;

            // Re-create configuration
            this.configuration = new SourceConfiguration
            {
                Id = this.configuration.Id,
                Type = this.configuration.Type,
                Format = ImageFormats.Png, // TODO: ? multiple output formats ?
                Title = title,
                Abstract = this.configuration.Abstract,
                Tms = false,
                Srs = U.SrsCodes.EPSG3857, // TODO: only EPSG:3857 'output' SRS currently supported
                Location = this.configuration.Location,
                ContentType = U.EntitiesConverter.TileFormatToContentType(ImageFormats.Png), // TODO: other output formats
                MinZoom = minZoom,
                MaxZoom = maxZoom,
                GeographicalBounds = this.rasterProperties.GeographicalBounds,
                Cache = null, // Not used for local raster file source
                PostGis = null,
            };

            return Task.CompletedTask;
        }

        async Task<byte[]?> ITileSource.GetTileAsync(int x, int y, int z)
        {
            if (this.rasterProperties == null)
            {
                throw new InvalidOperationException("rasterProperties property is null.");
            }

            if (String.IsNullOrEmpty(this.configuration.ContentType))
            {
                throw new InvalidOperationException("configuration.ContentType property is null.");
            }

            if ((z < this.configuration.MinZoom) || (z > this.configuration.MaxZoom))
            {
                return null;
            }
            else
            {
                var requestedTileBounds = U.WebMercator.GetTileBounds(x, U.WebMercator.FlipYCoordinate(y, z), z);
                var sourceTileCoordinates = this.BuildTileCoordinatesList(requestedTileBounds);
                if (sourceTileCoordinates.Count == 0)
                {
                    return null;
                }

                var width = U.WebMercator.TileSize;
                var height = U.WebMercator.TileSize;
                var imageInfo = new SKImageInfo(
                    width: width,
                    height: height,
                    colorType: SKColorType.Rgba8888,
                    alphaType: SKAlphaType.Premul);

                using var surface = SKSurface.Create(imageInfo);
                using var canvas = surface.Canvas;
                canvas.Clear(new SKColor(0)); // TODO: pass and use backgroundColor

                DrawGeoTiffTilesToRasterCanvas(canvas, width, height, requestedTileBounds, sourceTileCoordinates, 0, this.rasterProperties.TileWidth, this.rasterProperties.TileHeight);
                var imageFormat = U.ImageHelper.SKEncodedImageFormatFromMediaType(this.configuration.ContentType);
                using SKImage image = surface.Snapshot();
                using SKData data = image.Encode(imageFormat, 90); // TODO: pass quality parameter

                return await Task.FromResult(data.ToArray());
            }
        }

        SourceConfiguration ITileSource.Configuration => this.configuration;

        #endregion

        internal async Task<SKImage?> GetImagePartAsync(
            int width,
            int height,
            M.Bounds boundingBox,
            uint backgroundColor)
        {
            if (this.rasterProperties == null)
            {
                throw new InvalidOperationException($"Property {nameof(this.rasterProperties)} is null.");
            }

            var sourceTileCoordinates = this.BuildTileCoordinatesList(boundingBox);
            if (sourceTileCoordinates.Count == 0)
            {
                return null;
            }

            var imageInfo = new SKImageInfo(
                width: width,
                height: height,
                colorType: SKColorType.Rgba8888,
                alphaType: SKAlphaType.Premul);

            using var surface = SKSurface.Create(imageInfo);
            using var canvas = surface.Canvas;
            canvas.Clear(new SKColor(backgroundColor));

            DrawGeoTiffTilesToRasterCanvas(canvas, width, height, boundingBox, sourceTileCoordinates, backgroundColor, this.rasterProperties.TileWidth, this.rasterProperties.TileHeight);

            return await Task.FromResult(surface.Snapshot());
        }

        #region Coordinates utils

        private List<GeoTiff.TileCoordinates> BuildTileCoordinatesList(M.Bounds bounds)
        {
            if (this.rasterProperties == null)
            {
                throw new InvalidOperationException(nameof(rasterProperties));
            }

            if (this.rasterProperties.ProjectedBounds == null)
            {
                throw new ArgumentException("ProjectedBounds property is null.");
            }

            var tileCoordMin = GetGeoTiffTileCoordinatesAtPoint(
                this.rasterProperties,
                Math.Max(bounds.Left, this.rasterProperties.ProjectedBounds.Left),
                Math.Min(bounds.Top, this.rasterProperties.ProjectedBounds.Top));

            var tileCoordMax = GetGeoTiffTileCoordinatesAtPoint(
                this.rasterProperties,
                Math.Min(bounds.Right, this.rasterProperties.ProjectedBounds.Right),
                Math.Min(bounds.Bottom, this.rasterProperties.ProjectedBounds.Bottom));

            var tileCoordinates = new List<GeoTiff.TileCoordinates>();
            for (var tileX = tileCoordMin.X; tileX <= tileCoordMax.X; tileX++)
            {
                for (var tileY = tileCoordMin.Y; tileY <= tileCoordMax.Y; tileY++)
                {
                    tileCoordinates.Add(new GeoTiff.TileCoordinates(tileX, tileY));
                }
            }

            return tileCoordinates;
        }

        private static GeoTiff.TileCoordinates GetGeoTiffTileCoordinatesAtPoint(
            M.RasterProperties rasterProperties,
            double x,
            double y)
        {
            var tileX = (int)Math.Floor(XToGeoTiffPixelX(rasterProperties, x) / (double)rasterProperties.TileWidth);
            var tileY = (int)Math.Floor(YToGeoTiffPixelY(rasterProperties, y) / (double)rasterProperties.TileHeight);

            return new GeoTiff.TileCoordinates(tileX, tileY);
        }

        private static double XToGeoTiffPixelX(M.RasterProperties rasterProperties, double x)
        {
            if (rasterProperties.ProjectedBounds == null)
            {
                throw new ArgumentNullException(nameof(rasterProperties), "rasterProperties.ProjectedBounds is null.");
            }

            return (x - rasterProperties.ProjectedBounds.Left) / rasterProperties.PixelWidth;
        }

        private static double YToGeoTiffPixelY(M.RasterProperties rasterProperties, double y)
        {
            if (rasterProperties.ProjectedBounds == null)
            {
                throw new ArgumentNullException(nameof(rasterProperties), "rasterProperties.ProjectedBounds is null.");
            }

            return (rasterProperties.ProjectedBounds.Top - y) / rasterProperties.PixelHeight;
        }

        #endregion

        private void DrawGeoTiffTilesToRasterCanvas(
            SKCanvas outputCanvas,
            int outputWidth,
            int outputHeight,
            M.Bounds tileBounds,
            IList<GeoTiff.TileCoordinates> sourceTileCoordinates,
            uint backgroundColor,
            int sourceTileWidth,
            int sourceTileHeight)
        {
            // TODO: support for non-tiled tiff images

            if (this.rasterProperties == null)
            {
                throw new InvalidOperationException("rasterProperties is null.");
            }

            if (String.IsNullOrEmpty(this.configuration.Location))
            {
                throw new InvalidOperationException("configuration.Location is null or empty.");
            }

            var tileMinX = sourceTileCoordinates.Min(t => t.X);
            var tileMinY = sourceTileCoordinates.Min(t => t.Y);
            var tileMaxY = sourceTileCoordinates.Max(t => t.Y);
            var tilesCountX = sourceTileCoordinates.Max(t => t.X) - tileMinX + 1;
            var tilesCountY = sourceTileCoordinates.Max(t => t.Y) - tileMinY + 1;
            var canvasWidth = tilesCountX * sourceTileWidth;
            var canvasHeight = tilesCountY * sourceTileHeight;

            // TODO: ? scale before draw to reduce memory allocation
            // TODO: check max canvas size
            using var surface = SKSurface.Create(new SKImageInfo(
                width: canvasWidth,
                height: canvasHeight,
                colorType: SKColorType.Rgba8888,
                alphaType: SKAlphaType.Premul));

            using var canvas = surface.Canvas;
            canvas.Clear(new SKColor(backgroundColor));

            // Flip input tiff tile vertically
            canvas.Scale(1, -1, 0, canvasHeight / 2.0f);
            // Draw all source tiles without scaling
            foreach (var sourceTile in sourceTileCoordinates)
            {
                var pixelX = sourceTile.X * this.rasterProperties.TileWidth;
                var pixelY = sourceTile.Y * this.rasterProperties.TileHeight;

                if ((pixelX >= this.rasterProperties.ImageWidth) || (pixelY >= this.rasterProperties.ImageHeight))
                {
                    continue;
                }

                var tiffTileBuffer = U.ImageHelper.ReadTiffTile(
                    this.configuration.Location,
                    this.rasterProperties.TileWidth,
                    this.rasterProperties.TileHeight,
                    pixelX,
                    pixelY);

                var tiffTileHandle = GCHandle.Alloc(tiffTileBuffer, GCHandleType.Pinned);

                try
                {
                    var offsetX = (sourceTile.X - tileMinX) * sourceTileWidth;
                    var offsetY = (tileMaxY - sourceTile.Y) * sourceTileHeight;

                    var tiffTileImageInfo = new SKImageInfo(
                        width: this.rasterProperties.TileWidth,
                        height: this.rasterProperties.TileHeight,
                        colorType: SKColorType.Rgba8888,
                        alphaType: SKAlphaType.Premul);

                    using var tiffTileImage = SKImage.FromPixels(tiffTileImageInfo, tiffTileHandle.AddrOfPinnedObject());

                    canvas.DrawImage(tiffTileImage, SKRect.Create(offsetX, offsetY, tiffTileImage.Width, tiffTileImage.Height));

                    // For debug
                    ////using var borderPen = new SKPaint { Color = SKColors.Magenta, StrokeWidth = 3.0f, IsStroke = true, };
                    ////canvas.DrawRect(new SKRect(offsetX, offsetY, offsetX + tiffTileImage.Width, offsetY + tiffTileImage.Height), borderPen);
                    ////canvas.DrawText($"X = {sourceTile.X}  Y = {sourceTile.Y}", offsetX, offsetY, new SKFont(SKTypeface.FromFamilyName("Arial"), 72.0f), new SKPaint { Color = SKColors.Magenta });
                }
                finally
                {
                    tiffTileHandle.Free();
                }
            }

            // TODO: ! better image transformation / reprojection between coordinate systems
            // Clip and scale to requested size of output image
            var sourceOffsetX = XToGeoTiffPixelX(this.rasterProperties, tileBounds.Left) - sourceTileWidth * tileMinX;
            var sourceOffsetY = YToGeoTiffPixelY(this.rasterProperties, tileBounds.Top) - sourceTileHeight * tileMinY;
            var sourceWidth = XToGeoTiffPixelX(this.rasterProperties, tileBounds.Right) - XToGeoTiffPixelX(this.rasterProperties, tileBounds.Left);
            var sourceHeight = YToGeoTiffPixelY(this.rasterProperties, tileBounds.Bottom) - YToGeoTiffPixelY(this.rasterProperties, tileBounds.Top);

            using SKImage canvasImage = surface.Snapshot();
            outputCanvas.DrawImage(
                canvasImage,
                SKRect.Create((float)sourceOffsetX, (float)sourceOffsetY, (float)sourceWidth, (float)sourceHeight),
                SKRect.Create(0, 0, outputWidth, outputHeight),
                new SKPaint { FilterQuality = SKFilterQuality.High, });
        }

        private class DisableErrorHandler : TiffErrorHandler
        {
            public override void WarningHandler(Tiff tiff, string method, string format, params object[] args)
            {

            }

            public override void WarningHandlerExt(Tiff tiff, object clientData, string method, string format, params object[] args)
            {

            }
        }
    }
}
