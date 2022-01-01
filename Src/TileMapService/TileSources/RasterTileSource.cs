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
        private TileSourceConfiguration configuration;

        private M.RasterProperties rasterProperties;

        public RasterTileSource(TileSourceConfiguration configuration)
        {
            // TODO: report real WMS layer bounds from raster bounds
            // TODO: EPSG:4326 tile response (for WMTS, WMS)
            // TODO: support for MapInfo RASTER files
            // TODO: support for multiple rasters (directory with rasters)

            if (String.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException("Source identifier is null or empty string");
            }

            if (String.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException("Source location is null or empty string");
            }

            this.configuration = configuration; // Will be changed later in InitAsync
        }

        #region ITileSource implementation

        Task ITileSource.InitAsync()
        {
            Tiff.SetErrorHandler(new DisableErrorHandler()); // TODO: ? redirect output?

            this.rasterProperties = ReadGeoTiffProperties(this.configuration.Location);

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                this.configuration.Id :
                this.configuration.Title;

            var minZoom = this.configuration.MinZoom ?? 0;
            var maxZoom = this.configuration.MaxZoom ?? 24;

            // Re-create configuration
            this.configuration = new TileSourceConfiguration
            {
                Id = this.configuration.Id,
                Type = this.configuration.Type,
                Format = "png", // TODO: ? multiple output formats ?
                Title = title,
                Tms = false,
                Srs = U.SrsCodes.EPSG3857, // TODO: only EPSG:3857 'output' SRS currently supported
                Location = this.configuration.Location,
                ContentType = U.EntitiesConverter.TileFormatToContentType("png"),
                MinZoom = minZoom,
                MaxZoom = maxZoom,
            };

            return Task.CompletedTask;
        }

        async Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            if ((z < this.configuration.MinZoom) || (z > this.configuration.MaxZoom))
            {
                return null;
            }
            else
            {
                var tileBounds = U.WebMercator.GetTileBounds(x, U.WebMercator.FlipYCoordinate(y, z), z);
                var tileCoordinates = BuildTileCoordinatesList(this.rasterProperties, tileBounds);
                if (tileCoordinates.Count == 0)
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
                canvas.Clear(new SKColor(0));

                DrawGeoTiffTilesToRasterCanvas(canvas, width, width, tileBounds, tileCoordinates, 0, this.rasterProperties.TileWidth, this.rasterProperties.TileHeight);

                var imageFormat = U.ImageHelper.SKEncodedImageFormatFromMediaType(this.configuration.ContentType);
                using SKImage image = surface.Snapshot();
                using SKData data = image.Encode(imageFormat, 90); // TODO: ? parameter

                return await Task.FromResult(data.ToArray());
            }
        }

        TileSourceConfiguration ITileSource.Configuration
        {
            get
            {
                return this.configuration;
            }
        }

        #endregion

        #region GeoTIFF files processing

        private const string ModeOpenReadTiff = "r";

        private static M.RasterProperties ReadGeoTiffProperties(string path)
        {
            using var tiff = Tiff.Open(path, ModeOpenReadTiff);

            var planarConfig = (PlanarConfig)tiff.GetField(TiffTag.PLANARCONFIG)[0].ToInt();
            if (planarConfig != PlanarConfig.CONTIG)
            {
                throw new FormatException($"Only single image plane storage organization ({PlanarConfig.CONTIG}) is supported");
            }

            if (!tiff.IsTiled())
            {
                throw new FormatException($"Only tiled storage scheme is supported");
            }

            var imageWidth = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            var imageHeight = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            var tileWidth = tiff.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            var tileHeight = tiff.GetField(TiffTag.TILELENGTH)[0].ToInt();

            // ModelPixelScale  https://freeimage.sourceforge.io/fnet/html/CC586183.htm
            var modelPixelScale = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            var pixelSizesCount = modelPixelScale[0].ToInt();
            var pixelSizes = modelPixelScale[1].ToDoubleArray();

            // ModelTiePoints  https://freeimage.sourceforge.io/fnet/html/38F9430A.htm
            var tiePointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);
            var tiePointsCount = tiePointTag[0].ToInt();
            var tiePoints = tiePointTag[1].ToDoubleArray();

            if ((tiePoints.Length != 6) || (tiePoints[0] != 0) || (tiePoints[1] != 0) || (tiePoints[2] != 0) || (tiePoints[5] != 0))
            {
                throw new FormatException($"Only single tie point is supported"); // TODO: Only simple tie points scheme is supported
            }

            var modelTransformation = tiff.GetField(TiffTag.GEOTIFF_MODELTRANSFORMATIONTAG);
            if (modelTransformation != null)
            {
                throw new FormatException($"Only simple projection without transformation is supported");
            }

            var srId = 0;

            // Simple check SRS of GeoTIFF tie points
            var geoKeys = tiff.GetField(TiffTag.GEOTIFF_GEOKEYDIRECTORYTAG);
            if (geoKeys != null)
            {
                var geoDoubleParams = tiff.GetField(TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG);
                double[] doubleParams = null;
                if (geoDoubleParams != null)
                {
                    doubleParams = geoDoubleParams[1].ToDoubleArray();
                }

                var geoAsciiParams = tiff.GetField(TiffTag.GEOTIFF_GEOASCIIPARAMSTAG);

                // Array of GeoTIFF GeoKeys values
                var keys = geoKeys[1].ToUShortArray();
                if (keys.Length > 4)
                {
                    // Header={KeyDirectoryVersion, KeyRevision, MinorRevision, NumberOfKeys}
                    var keyDirectoryVersion = keys[0];
                    var keyRevision = keys[1];
                    var minorRevision = keys[2];
                    var numberOfKeys = keys[3];
                    for (var keyIndex = 4; keyIndex < keys.Length;)
                    {
                        switch (keys[keyIndex])
                        {
                            case (ushort)GeoTiff.Key.GTModelTypeGeoKey:
                                {
                                    var modelType = (GeoTiff.ModelType)keys[keyIndex + 3];
                                    if (!((modelType == GeoTiff.ModelType.Projected) || (modelType == GeoTiff.ModelType.Geographic)))
                                    {
                                        throw new FormatException("Only coordinate systems ModelTypeProjected (1) or ModelTypeGeographic (2) are supported");
                                    }

                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)GeoTiff.Key.GTRasterTypeGeoKey:
                                {
                                    var rasterType = (GeoTiff.RasterType)keys[keyIndex + 3]; // TODO: use RasterTypeCode value
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)GeoTiff.Key.GTCitationGeoKey:
                                {
                                    var gtc = keys[keyIndex + 3];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)GeoTiff.Key.GeographicTypeGeoKey:
                                {
                                    var geographicType = keys[keyIndex + 3];
                                    if (geographicType != 4326)
                                    {
                                        throw new FormatException("Only EPSG:4326 geodetic coordinate system is supported");
                                    }

                                    srId = geographicType;
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)GeoTiff.Key.GeogCitationGeoKey:
                                {
                                    var geogCitation = keys[keyIndex + 3];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)GeoTiff.Key.GeogAngularUnitsGeoKey:
                                {
                                    var geogAngularUnit = (GeoTiff.AngularUnits)keys[keyIndex + 3];
                                    if (geogAngularUnit != GeoTiff.AngularUnits.Degree)
                                    {
                                        throw new FormatException("Only degree angular unit is supported");
                                    }

                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)GeoTiff.Key.GeogSemiMajorAxisGeoKey:
                                {
                                    var geogSemiMajorAxis = doubleParams[keys[keyIndex + 3]];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)GeoTiff.Key.GeogSemiMinorAxisGeoKey:
                                {
                                    var geogSemiMinorAxis = doubleParams[keys[keyIndex + 3]];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)GeoTiff.Key.GeogInvFlatteningGeoKey:
                                {
                                    var geogInvFlattening = doubleParams[keys[keyIndex + 3]];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)GeoTiff.Key.ProjectedCSTypeGeoKey:
                                {
                                    var projectedCSType = keys[keyIndex + 3];
                                    if (projectedCSType != 3857)
                                    {
                                        throw new FormatException($"Only EPSG:3857 projected coordinate system is supported (input was: {projectedCSType})");
                                    }

                                    // TODO: UTM (EPSG:32636 and others) support
                                    srId = projectedCSType;
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)GeoTiff.Key.ProjLinearUnitsGeoKey:
                                {
                                    var linearUnit = (GeoTiff.LinearUnits)keys[keyIndex + 3];
                                    if (linearUnit != GeoTiff.LinearUnits.Meter)
                                    {
                                        throw new FormatException("Only meter linear unit is supported");
                                    }

                                    keyIndex += 4;
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                    }
                }
            }

            M.GeographicalBounds geographicalBounds = null;
            M.Bounds projectedBounds = null;
            double pixelWidth = 0, pixelHeight = 0;

            switch (srId)
            {
                case 4326: // TODO: const
                    {
                        geographicalBounds = new M.GeographicalBounds(
                            tiePoints[3],
                            tiePoints[4] - imageHeight * pixelSizes[1],
                            tiePoints[3] + imageWidth * pixelSizes[0],
                            tiePoints[4]);

                        projectedBounds = new M.Bounds(
                            U.WebMercator.X(tiePoints[3]),
                            U.WebMercator.Y(tiePoints[4] - imageHeight * pixelSizes[1]),
                            U.WebMercator.X(tiePoints[3] + imageWidth * pixelSizes[0]),
                            U.WebMercator.Y(tiePoints[4]));

                        pixelWidth = U.WebMercator.X(tiePoints[3] + pixelSizes[0]) - U.WebMercator.X(tiePoints[3]);
                        pixelHeight = U.WebMercator.Y(tiePoints[4]) - U.WebMercator.Y(tiePoints[4] - pixelSizes[1]);

                        break;
                    }
                case 3857: // TODO: const
                    {
                        projectedBounds = new M.Bounds(
                            tiePoints[3],
                            tiePoints[4] - imageHeight * pixelSizes[1],
                            tiePoints[3] + imageWidth * pixelSizes[0],
                            tiePoints[4]);

                        geographicalBounds = new M.GeographicalBounds(
                            U.WebMercator.Longitude(tiePoints[3]),
                            U.WebMercator.Latitude(tiePoints[4] - imageHeight * pixelSizes[1]),
                            U.WebMercator.Longitude(tiePoints[3] + imageWidth * pixelSizes[0]),
                            U.WebMercator.Latitude(tiePoints[4]));

                        pixelWidth = pixelSizes[0];
                        pixelHeight = pixelSizes[1];

                        break;
                    }
                default:
                    {
                        throw new ArgumentException();
                    }
            }

            var result = new M.RasterProperties
            {
                Srid = srId,
                ImageWidth = imageWidth,
                ImageHeight = imageHeight,
                TileWidth = tileWidth,
                TileHeight = tileHeight,
                TileSize = tiff.TileSize(),
                ProjectedBounds = projectedBounds,
                GeographicalBounds = geographicalBounds,
                PixelWidth = pixelWidth,
                PixelHeight = pixelHeight,
            };

            return result;
        }

        private static byte[] ReadTiffTile(string path, int tileWidth, int tileHeight, int tileSize, int pixelX, int pixelY)
        {
            var tileBuffer = new byte[tileSize];

            using (var tiff = Tiff.Open(path, ModeOpenReadTiff))
            {
                // https://bitmiracle.github.io/libtiff.net/help/articles/KB/grayscale-color.html#reading-a-color-image

                ////var compression = (Compression)tiff.GetField(TiffTag.COMPRESSION)[0].ToInt();
                ////var bps = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
                ////var sf = (SampleFormat)tiff.GetField(TiffTag.SAMPLEFORMAT)[0].ToInt();
                ////var d = tiff.NumberOfDirectories();
                ////var s = tiff.NumberOfStrips();
                ////var t = tiff.NumberOfTiles();

                var l = tiff.ReadTile(tileBuffer, 0, pixelX, pixelY, 0, 0);
            }

            const int ARGBPixelDataSize = 4;
            var size = tileWidth * tileHeight * ARGBPixelDataSize;
            var imageBuffer = new byte[size];

            // Flip vertical
            for (var row = tileHeight - 1; row != -1; row--)
            {
                for (var col = 0; col < tileWidth; col++)
                {
                    var pixelNumber = row * tileWidth + col;
                    var srcOffset = pixelNumber * 3; // TODO: bpp is always 3 bytes = BGR ?
                    var destOffset = pixelNumber * ARGBPixelDataSize;

                    imageBuffer[destOffset + 0] = tileBuffer[srcOffset + 0];
                    imageBuffer[destOffset + 1] = tileBuffer[srcOffset + 1];
                    imageBuffer[destOffset + 2] = tileBuffer[srcOffset + 2];
                    imageBuffer[destOffset + 3] = 255;
                }
            }

            return imageBuffer;
        }

        #endregion

        #region Coordinates utils

        private static List<GeoTiff.TileCoordinates> BuildTileCoordinatesList(
            M.RasterProperties rasterProperties,
            M.Bounds bounds)
        {
            var tileCoordMin = GetGeoTiffTileCoordinatesAtPoint(
                rasterProperties,
                Math.Max(bounds.Left, rasterProperties.ProjectedBounds.Left),
                Math.Min(bounds.Top, rasterProperties.ProjectedBounds.Top));

            var tileCoordMax = GetGeoTiffTileCoordinatesAtPoint(
                rasterProperties,
                Math.Min(bounds.Right, rasterProperties.ProjectedBounds.Right),
                Math.Min(bounds.Bottom, rasterProperties.ProjectedBounds.Bottom));

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
            return (x - rasterProperties.ProjectedBounds.Left) / rasterProperties.PixelWidth;
        }

        private static double YToGeoTiffPixelY(M.RasterProperties rasterProperties, double y)
        {
            return (rasterProperties.ProjectedBounds.Top - y) / rasterProperties.PixelHeight;
        }

        #endregion

        private void DrawGeoTiffTilesToRasterCanvas(
            SKCanvas outputCanvas,
            int width, int height,
            M.Bounds tileBounds,
            IList<GeoTiff.TileCoordinates> sourceTileCoordinates,
            int backgroundColor,
            int sourceTileWidth,
            int sourceTileHeight)
        {
            var tileMinX = sourceTileCoordinates.Min(t => t.X);
            var tileMinY = sourceTileCoordinates.Min(t => t.Y);
            var tilesCountX = sourceTileCoordinates.Max(t => t.X) - tileMinX + 1;
            var tilesCountY = sourceTileCoordinates.Max(t => t.Y) - tileMinY + 1;
            var canvasWidth = tilesCountX * sourceTileWidth;
            var canvasHeight = tilesCountY * sourceTileHeight;

            // TODO: ? scale before draw to reduce memory allocation
            // TODO: check max canvas size

            var imageInfo = new SKImageInfo(
                width: canvasWidth,
                height: canvasHeight,
                colorType: SKColorType.Rgba8888,
                alphaType: SKAlphaType.Premul);

            using var surface = SKSurface.Create(imageInfo);
            using var canvas = surface.Canvas;
            canvas.Clear(new SKColor((uint)backgroundColor)); // TODO: ? uint parameter

            // Draw all source tiles without scaling
            foreach (var sourceTile in sourceTileCoordinates)
            {
                var pixelX = sourceTile.X * this.rasterProperties.TileWidth;
                var pixelY = sourceTile.Y * this.rasterProperties.TileHeight;

                if ((pixelX >= this.rasterProperties.ImageWidth) || (pixelY >= this.rasterProperties.ImageHeight))
                {
                    continue;
                }

                var imageBuffer = ReadTiffTile(
                    this.configuration.Location,
                    this.rasterProperties.TileWidth,
                    this.rasterProperties.TileHeight,
                    this.rasterProperties.TileSize,
                    pixelX,
                    pixelY);

                const int PixelDataSize = 4;
                var stride = this.rasterProperties.TileWidth * PixelDataSize;
                var handle = GCHandle.Alloc(imageBuffer, GCHandleType.Pinned);

                try
                {
                    var offsetX = (sourceTile.X - tileMinX) * sourceTileWidth;
                    var offsetY = (sourceTile.Y - tileMinY) * sourceTileHeight;

                    var sourceImageInfo = new SKImageInfo(
                        width: this.rasterProperties.TileWidth,
                        height: this.rasterProperties.TileHeight,
                        colorType: SKColorType.Rgba8888,
                        alphaType: SKAlphaType.Premul);

                    using var sourceImage = SKImage.FromPixels(sourceImageInfo, handle.AddrOfPinnedObject());

                    canvas.DrawImage(sourceImage, new SKRect(offsetX, offsetY, offsetX + sourceImage.Width, offsetY + sourceImage.Height));

                    // For debug
                    ////using var borderPen = new SKPaint { Color = SKColors.Magenta, StrokeWidth = 5.0f, IsStroke = true, };
                    ////canvas.DrawRect(new SKRect(offsetX, offsetY, offsetX + sourceImage.Width, offsetY + sourceImage.Height), borderPen);
                    ////canvas.DrawText($"R = {sourceTile.Y * this.rasterProperties.TileHeight}", offsetX, offsetY, new SKFont(SKTypeface.FromFamilyName("Arial"), 36.0f), new SKPaint { Color = SKColors.Magenta });
                }
                finally
                {
                    handle.Free();
                }
            }

            // TODO: ! better image transformation / reprojection between coordinate systems
            var pixelOffsetX = XToGeoTiffPixelX(this.rasterProperties, tileBounds.Left) - sourceTileWidth * tileMinX;
            var pixelOffsetY = YToGeoTiffPixelY(this.rasterProperties, tileBounds.Top) - sourceTileHeight * tileMinY;
            var pixelWidth = XToGeoTiffPixelX(this.rasterProperties, tileBounds.Right) - XToGeoTiffPixelX(this.rasterProperties, tileBounds.Left);
            var pixelHeight = YToGeoTiffPixelY(this.rasterProperties, tileBounds.Bottom) - YToGeoTiffPixelY(this.rasterProperties, tileBounds.Top);

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

        class DisableErrorHandler : TiffErrorHandler
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
