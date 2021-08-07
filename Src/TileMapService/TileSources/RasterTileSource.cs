using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using BitMiracle.LibTiff.Classic;

using M = TileMapService.Models;
using U = TileMapService.Utils;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with tiles from GeoTIFF raster image file.
    /// </summary>
    /// <remarks>
    /// Supports currently only EPSG:4326 coordinate system of input GeoTIFF.
    /// http://geotiff.maptools.org/spec/geotiff6.html
    /// </remarks>
    class RasterTileSource : ITileSource
    {
        private TileSourceConfiguration configuration;

        private GeoTiffMetadata geoTiffInfo;

        public RasterTileSource(TileSourceConfiguration configuration)
        {
            // TODO: EPSG:4326 tile response (WMTS)
            // TODO: support for MapInfo RASTER files
            // TODO: support for multiple rasters (directory with rasters)
            // TODO: report WMS layer bounds from raster bounds

            if (String.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException();
            }

            if (String.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException();
            }

            this.configuration = configuration; // Will be changed later in InitAsync
        }

        #region ITileSource implementation

        Task ITileSource.InitAsync()
        {
            Tiff.SetErrorHandler(new DisableErrorHandler()); // TODO: ? redirect output?

            this.geoTiffInfo = ReadGeoTiffProperties(this.configuration.Location);

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                this.configuration.Id :
                this.configuration.Title;

            var tms = this.configuration.Tms ?? false;
            var srs = String.IsNullOrWhiteSpace(this.configuration.Srs) ? U.SrsCodes.EPSG4326 : this.configuration.Srs.Trim().ToUpper();

            var minZoom = this.configuration.MinZoom ?? 0;
            var maxZoom = this.configuration.MaxZoom ?? 24;

            // Re-create configuration
            this.configuration = new TileSourceConfiguration
            {
                Id = this.configuration.Id,
                Type = this.configuration.Type,
                Format = this.configuration.Format, // TODO: from metadata
                Title = title,
                Tms = tms,
                Srs = srs,
                Location = this.configuration.Location,
                ContentType = U.EntitiesConverter.TileFormatToContentType(this.configuration.Format),
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
                var tileGeoBounds = U.WebMercator.GetTileGeographicalBounds(x, U.WebMercator.FlipYCoordinate(y, z), z);
                var tileCoordinates = BuildTileCoordinatesList(this.geoTiffInfo, tileGeoBounds);
                if (tileCoordinates.Count == 0)
                {
                    return null;
                }

                var emptyImage = U.ImageHelper.CreateEmptyPngImage(U.WebMercator.TileSize, U.WebMercator.TileSize, 0);
                using (var resultImageStream = new MemoryStream(emptyImage))
                {
                    using var resultImage = new Bitmap(resultImageStream);

                    DrawGeoTiffTilesToRasterCanvas(resultImage, tileGeoBounds, tileCoordinates, 0, this.geoTiffInfo.TileWidth, this.geoTiffInfo.TileHeight);

                    var imageFormat = U.ImageHelper.ImageFormatFromMediaType(this.configuration.ContentType);
                    var imageData = U.ImageHelper.SaveImageToByteArray(resultImage, imageFormat);

                    return await Task.FromResult(imageData);
                }
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

        private static GeoTiffMetadata ReadGeoTiffProperties(string path)
        {
            using (var tiff = Tiff.Open(path, ModeOpenReadTiff))
            {
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
                var tiePoints = tiePointTag[1].ToDoubleArray(); // TODO: ! check format

                if ((tiePoints.Length != 6) || (tiePoints[0] != 0) || (tiePoints[1] != 0) || (tiePoints[2] != 0) || (tiePoints[5] != 0))
                {
                    throw new FormatException($"Only single tie point is supported");
                }

                var t3 = tiff.GetField(TiffTag.GEOTIFF_MODELTRANSFORMATIONTAG);
                if (t3 != null)
                {
                    throw new FormatException($"Only simple projection without transformation is supported");
                }

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
                                        if (modelType != GeoTiff.ModelType.Geographic)
                                        {
                                            throw new FormatException("Only geographic coordinate system (ModelTypeGeographic = 2) is supported");
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
                                case (ushort)GeoTiff.Key.GeographicTypeGeoKey:
                                    {
                                        var geographicType = keys[keyIndex + 3];
                                        if (geographicType != 4326)
                                        {
                                            throw new FormatException("Only EPSG:4326 coordinate system is supported");
                                        }

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
                                            throw new FormatException("Only angular degree units is supported");
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
                                default:
                                    {
                                        break;
                                    }
                            }
                        }
                    }
                }

                // TODO: ? use projected values for internal processing?
                // Only simple tie points scheme is supported
                var result = new GeoTiffMetadata
                {
                    ImageWidth = imageWidth,
                    ImageHeight = imageHeight,
                    TileWidth = tileWidth,
                    TileHeight = tileHeight,
                    TileSize = tiff.TileSize(),
                    Bounds = new M.GeographicalBounds(
                        tiePoints[3],
                        tiePoints[4] - imageHeight * pixelSizes[1],
                        tiePoints[3] + imageWidth * pixelSizes[0],
                        tiePoints[4]),
                    PixelSizeX = pixelSizes[0],
                    PixelSizeY = pixelSizes[1],
                };

                return result;
            }
        }

        private const string ModeOpenReadTiff = "r";

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

                    // Source bytes order is BGR
                    imageBuffer[destOffset + 0] = tileBuffer[srcOffset + 2];
                    imageBuffer[destOffset + 1] = tileBuffer[srcOffset + 1];
                    imageBuffer[destOffset + 2] = tileBuffer[srcOffset + 0];
                    imageBuffer[destOffset + 3] = 255;
                }
            }

            return imageBuffer;
        }

        private static List<GeoTiff.TileCoordinates> BuildTileCoordinatesList(GeoTiffMetadata geoTiff, M.GeographicalBounds geoBounds)
        {
            var tileCoordMin = GetGeoTiff4326TileCoordinatesAtPoint(
                geoTiff,
                Math.Max(geoBounds.MinLongitude, geoTiff.Bounds.MinLongitude),
                Math.Min(geoBounds.MaxLatitude, geoTiff.Bounds.MaxLatitude));

            var tileCoordMax = GetGeoTiff4326TileCoordinatesAtPoint(
                geoTiff,
                Math.Min(geoBounds.MaxLongitude, geoTiff.Bounds.MaxLongitude),
                Math.Min(geoBounds.MinLatitude, geoTiff.Bounds.MinLatitude));

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

        private static GeoTiff.TileCoordinates GetGeoTiff4326TileCoordinatesAtPoint(
            GeoTiffMetadata geoTiff,
            double longitude,
            double latitude)
        {
            var x = LongitudeToGeoTiffPixelX(geoTiff, longitude) / (double)geoTiff.TileWidth;
            var y = LatitudeToGeoTiffPixelY(geoTiff, latitude) / (double)geoTiff.TileHeight;

            return new GeoTiff.TileCoordinates((int)Math.Floor(x), (int)Math.Floor(y));
        }

        private static double LongitudeToGeoTiffPixelX(GeoTiffMetadata geoTiff, double longitude)
        {
            return (longitude - geoTiff.Bounds.MinLongitude) / geoTiff.PixelSizeX;
        }

        private static double LatitudeToGeoTiffPixelY(GeoTiffMetadata geoTiff, double latitude)
        {
            return (geoTiff.Bounds.MaxLatitude - latitude) / geoTiff.PixelSizeY;
        }

        private void DrawGeoTiffTilesToRasterCanvas(
            Bitmap outputImage,
            M.GeographicalBounds tileGeoBounds,
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

            var canvas = U.ImageHelper.CreateEmptyPngImage(canvasWidth, canvasHeight, backgroundColor);

            using (var canvasImageStream = new MemoryStream(canvas))
            {
                using (var canvasImage = new Bitmap(canvasImageStream))
                {
                    using (var graphics = Graphics.FromImage(canvasImage))
                    {
                        // Draw all source tiles without scaling
                        foreach (var sourceTile in sourceTileCoordinates)
                        {
                            var pixelX = sourceTile.X * this.geoTiffInfo.TileWidth;
                            var pixelY = sourceTile.Y * this.geoTiffInfo.TileHeight;

                            if ((pixelX >= this.geoTiffInfo.ImageWidth) || (pixelY >= this.geoTiffInfo.ImageHeight))
                            {
                                continue;
                            }

                            var imageBuffer = ReadTiffTile(
                                this.configuration.Location,
                                this.geoTiffInfo.TileWidth,
                                this.geoTiffInfo.TileHeight,
                                this.geoTiffInfo.TileSize,
                                pixelX,
                                pixelY);

                            const int PixelDataSize = 4;
                            var stride = this.geoTiffInfo.TileWidth * PixelDataSize;
                            var handle = GCHandle.Alloc(imageBuffer, GCHandleType.Pinned);

                            Bitmap sourceImage = null;
                            try
                            {
                                var offsetX = (sourceTile.X - tileMinX) * sourceTileWidth;
                                var offsetY = (sourceTile.Y - tileMinY) * sourceTileHeight;

                                sourceImage = new Bitmap(this.geoTiffInfo.TileWidth, this.geoTiffInfo.TileHeight, stride, PixelFormat.Format32bppArgb, handle.AddrOfPinnedObject());

                                if ((sourceImage.HorizontalResolution == canvasImage.HorizontalResolution) &&
                                    (sourceImage.VerticalResolution == canvasImage.VerticalResolution))
                                {
                                    graphics.DrawImageUnscaled(sourceImage, offsetX, offsetY);
                                }
                                else
                                {
                                    graphics.DrawImage(sourceImage, new Rectangle(offsetX, offsetY, sourceImage.Width, sourceImage.Height));
                                }

                                // For debug
                                ////using var borderPen = new Pen(Color.Magenta, 5.0f);
                                ////graphics.DrawRectangle(borderPen, new Rectangle(offsetX, offsetY, sourceImage.Width, sourceImage.Height));
                                ////graphics.DrawString($"R = {sourceTile.Y * this.geoTiffInfo.TileHeight}", new Font("Arial", 36.0f), Brushes.Magenta, offsetX, offsetY);
                            }
                            finally
                            {
                                handle.Free();
                                sourceImage.Dispose();
                            }
                        }
                    }

                    // TODO: ! better image transform between coordinate systems
                    var pixelOffsetX = LongitudeToGeoTiffPixelX(this.geoTiffInfo, tileGeoBounds.MinLongitude) - sourceTileWidth * tileMinX;
                    var pixelOffsetY = LatitudeToGeoTiffPixelY(this.geoTiffInfo, tileGeoBounds.MaxLatitude) - sourceTileHeight * tileMinY;
                    var pixelWidth = LongitudeToGeoTiffPixelX(this.geoTiffInfo, tileGeoBounds.MaxLongitude) - LongitudeToGeoTiffPixelX(this.geoTiffInfo, tileGeoBounds.MinLongitude);
                    var pixelHeight = LatitudeToGeoTiffPixelY(this.geoTiffInfo, tileGeoBounds.MinLatitude) - LatitudeToGeoTiffPixelY(this.geoTiffInfo, tileGeoBounds.MaxLatitude);
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

        class GeoTiffMetadata // TODO: store source parameters as-is
        {
            public int ImageWidth { get; set; }

            public int ImageHeight { get; set; }

            public int TileWidth { get; set; }

            public int TileHeight { get; set; }

            public int TileSize { get; set; }

            public M.GeographicalBounds Bounds { get; set; }

            public double PixelSizeX { get; set; }

            public double PixelSizeY { get; set; }
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
