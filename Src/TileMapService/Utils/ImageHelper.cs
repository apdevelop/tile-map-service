using System;
using System.IO;

using BitMiracle.LibTiff.Classic;
using SkiaSharp;

using TileMapService.GeoTiff;

using M = TileMapService.Models;

namespace TileMapService.Utils
{
    public static class ImageHelper
    {
        public static byte[] CreateEmptyImage(
            int width,
            int height,
            uint color,
            SKEncodedImageFormat format,
            int quality)
        {
            var imageInfo = new SKImageInfo(
                width: width,
                height: height,
                colorType: SKColorType.Rgba8888,
                alphaType: SKAlphaType.Premul);

            using var surface = SKSurface.Create(imageInfo);
            using var canvas = surface.Canvas;
            canvas.Clear(new SKColor(color));

            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(format, quality);

            return data.ToArray();
        }

        private const string ModeTiffReading = "r";

        private const string ModeTiffWriting = "w";

        private static Tiff.TiffExtendProc? parentExtender;

        public static byte[] CreateTiffImage(
            byte[] rgbaPixels,
            int width,
            int height,
            M.Bounds boundingBox,
            bool writeGeoTiffTags)
        {
            if (writeGeoTiffTags)
            {
                parentExtender = Tiff.SetTagExtender(GeoTiffTagExtender); // TODO: ! mutex
            }

            var stride = rgbaPixels.Length / height;
            using var ms = new MemoryStream();
            var tiffStream = new TiffStream();
            using (var tiff = Tiff.ClientOpen(string.Empty, ModeTiffWriting, ms, tiffStream))
            {
                tiff.SetField(TiffTag.IMAGEWIDTH, width);
                tiff.SetField(TiffTag.IMAGELENGTH, height);
                tiff.SetField(TiffTag.COMPRESSION, Compression.LZW);
                tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                tiff.SetField(TiffTag.ROWSPERSTRIP, height);
                tiff.SetField(TiffTag.XRESOLUTION, 96.0);
                tiff.SetField(TiffTag.YRESOLUTION, 96.0);
                tiff.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);
                tiff.SetField(TiffTag.BITSPERSAMPLE, 8);
                tiff.SetField(TiffTag.SAMPLESPERPIXEL, 4);
                tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                tiff.SetField(TiffTag.EXTRASAMPLES, 1, new[] { (short)ExtraSample.UNASSALPHA });

                if (writeGeoTiffTags)
                {
                    // Projection parameters
                    var values = new double[19];
                    values[0] = 6378137;
                    values[1] = 6378137;
                    values[16] = 1;
                    if (!tiff.SetField(TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, values.Length, values))
                    {
                        throw new InvalidOperationException($"Error writing {TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG}.");
                    }

                    // Scales
                    // https://freeimage.sourceforge.io/fnet/html/CC586183.htm
                    var scaleX = (boundingBox.Right - boundingBox.Left) / width;
                    var scaleY = (boundingBox.Top - boundingBox.Bottom) / height;
                    double[] scales = [scaleX, scaleY, 0];
                    if (!tiff.SetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG, scales.Length, scales))
                    {
                        throw new InvalidOperationException($"Error writing {TiffTag.GEOTIFF_MODELPIXELSCALETAG}.");
                    }

                    // Tie point
                    // https://freeimage.sourceforge.io/fnet/html/38F9430A.htm
                    double[] tiePoints = [0, 0, 0, boundingBox.Left, boundingBox.Top, 0];
                    if (!tiff.SetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG, tiePoints.Length, tiePoints))
                    {
                        throw new InvalidOperationException($"Error writing {TiffTag.GEOTIFF_MODELTIEPOINTTAG}.");
                    }

                    // Keys
                    var keys = new ushort[]
                    {
                        1, 1, 0, 25, // Header = [KeyDirectoryVersion, KeyRevision, MinorRevision, NumberOfKeys]

                        (ushort)Key.GTModelTypeGeoKey, 0, 1, (ushort)ModelType.Projected,
                        (ushort)Key.GTRasterTypeGeoKey, 0, 1, (ushort)RasterType.RasterPixelIsArea,
                        (ushort)Key.GeogAngularUnitsGeoKey, 0, 1, (ushort)AngularUnits.Degree,

                        (ushort)Key.GeogSemiMajorAxisGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 0,
                        (ushort)Key.GeogSemiMinorAxisGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 1,

                        (ushort)Key.ProjectedCSTypeGeoKey, 0, 1, 3857, // EPSG:3857

                        (ushort)Key.ProjCoordTransGeoKey, 0, 1, (ushort)CoordinateTransformation.Mercator,
                        (ushort)Key.ProjLinearUnitsGeoKey, 0, 1, (ushort)LinearUnits.Meter,

                        (ushort)Key.ProjStdParallel1GeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 2,
                        (ushort)Key.ProjStdParallel2GeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 3,
                        (ushort)Key.ProjNatOriginLongGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 4,
                        (ushort)Key.ProjNatOriginLatGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 5,
                        (ushort)Key.ProjFalseEastingGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 6,
                        (ushort)Key.ProjFalseNorthingGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 7,
                        (ushort)Key.ProjFalseOriginLongGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 8,
                        (ushort)Key.ProjFalseOriginLatGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 9,
                        (ushort)Key.ProjFalseOriginEastingGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 10,
                        (ushort)Key.ProjFalseOriginNorthingGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 11,
                        (ushort)Key.ProjCenterLongGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 12,
                        (ushort)Key.ProjCenterLatGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 13,
                        (ushort)Key.ProjCenterEastingGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 14,
                        (ushort)Key.ProjCenterNorthingGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 15,
                        (ushort)Key.ProjScaleAtNatOriginGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 16,
                        // TODO: ? ProjScaleAtCenterGeoKey = 3093
                        (ushort)Key.ProjAzimuthAngleGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 17,
                        (ushort)Key.ProjStraightVertPoleLongGeoKey, (ushort)TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 1, 18,
                    };

                    if (!tiff.SetField(TiffTag.GEOTIFF_GEOKEYDIRECTORYTAG, keys.Length, keys))
                    {
                        throw new InvalidOperationException($"Error writing {TiffTag.GEOTIFF_GEOKEYDIRECTORYTAG}.");
                    }
                }

                for (var rowIndex = 0; rowIndex < height; rowIndex++)
                {
                    tiff.WriteScanline(rgbaPixels, rowIndex * stride, rowIndex, 0);
                }

                tiff.Close();
            }

            if (writeGeoTiffTags)
            {
                // restore previous tag extender
                Tiff.SetTagExtender(parentExtender);
            }

            return ms.ToArray();
        }

        public static byte[] ReadTiffTile(string path, int tileWidth, int tileHeight, int pixelX, int pixelY)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Error reading Tiff image: file '{path}' not found.");
            }

            // https://bitmiracle.github.io/libtiff.net/help/articles/KB/grayscale-color.html#reading-a-color-image

            using var tiff = Tiff.Open(path, ModeTiffReading);
            ////var compression = (Compression)tiff.GetField(TiffTag.COMPRESSION)[0].ToInt();
            ////var samplesPerPixel = tiff.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            ////var d = tiff.NumberOfDirectories();
            ////var s = tiff.NumberOfStrips();
            ////var t = tiff.NumberOfTiles();

            if (!tiff.IsTiled())
            {
                throw new FormatException($"Only tiled organization of image data is supported.");
            }

            var bitsPerSample = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            var sampleFormat = (SampleFormat)tiff.GetField(TiffTag.SAMPLEFORMAT)[0].ToInt();

            if (bitsPerSample != 8)
            {
                throw new FormatException($"Tiff image with {bitsPerSample} bits per sample is not supported.");
            }

            if (sampleFormat != SampleFormat.UINT)
            {
                throw new FormatException($"Tiff image with {sampleFormat} sample format is not supported.");
            }

            var tileBuffer = new int[tileWidth * tileHeight];
            if (tiff.ReadRGBATile(pixelX, pixelY, tileBuffer))
            {
                const int ARGBPixelDataSize = 4;
                var outputBuffer = new byte[tileWidth * tileHeight * ARGBPixelDataSize];
                Buffer.BlockCopy(tileBuffer, 0, outputBuffer, 0, outputBuffer.Length);
                return outputBuffer;
            }
            else
            {
                throw new InvalidOperationException("Error reading tiff image tile.");
            }
        }

        public static M.RasterProperties ReadGeoTiffProperties(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Source file '{path}' was not found.");
            }

            using var tiff = Tiff.Open(path, ModeTiffReading);

            var planarConfig = (PlanarConfig)tiff.GetField(TiffTag.PLANARCONFIG)[0].ToInt();
            if (planarConfig != PlanarConfig.CONTIG)
            {
                throw new FormatException($"Only single image plane storage organization ({PlanarConfig.CONTIG}) is supported.");
            }

            int tileWidth = 0, tileHeight = 0;
            if (tiff.IsTiled())
            {
                tileWidth = tiff.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                tileHeight = tiff.GetField(TiffTag.TILELENGTH)[0].ToInt();
            }

            var imageWidth = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            var imageHeight = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            var xResolution = tiff.GetField(TiffTag.XRESOLUTION);
            var yResolution = tiff.GetField(TiffTag.XRESOLUTION);

            // ModelPixelScale [units/px]  https://freeimage.sourceforge.io/fnet/html/CC586183.htm
            var modelPixelScale = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            if (modelPixelScale == null)
            {
                throw new FormatException($"GeoTIFF tag '{TiffTag.GEOTIFF_MODELPIXELSCALETAG}' was not found in {path}.");
            }

            var pixelSizesCount = modelPixelScale[0].ToInt();
            var pixelSizes = modelPixelScale[1].ToDoubleArray();

            // ModelTiePoints  https://freeimage.sourceforge.io/fnet/html/38F9430A.htm
            var tiePointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);
            if (tiePointTag == null)
            {
                throw new FormatException($"GeoTIFF tag '{TiffTag.GEOTIFF_MODELTIEPOINTTAG}' was not found in {path}.");
            }

            var tiePointsCount = tiePointTag[0].ToInt();
            var tiePoints = tiePointTag[1].ToDoubleArray();

            if (tiePoints.Length != 6 || tiePoints[0] != 0 || tiePoints[1] != 0 || tiePoints[2] != 0 || tiePoints[5] != 0)
            {
                throw new FormatException($"Only single tie point is supported."); // TODO: Only simple tie points scheme is supported
            }

            var modelTransformation = tiff.GetField(TiffTag.GEOTIFF_MODELTRANSFORMATIONTAG);
            if (modelTransformation != null)
            {
                throw new FormatException($"Only simple projection without transformation is supported.");
            }

            var srId = 0;

            // Simple check SRS of GeoTIFF tie points
            var geoKeys = tiff.GetField(TiffTag.GEOTIFF_GEOKEYDIRECTORYTAG);
            if (geoKeys != null)
            {
                var geoDoubleParams = tiff.GetField(TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG);
                double[]? doubleParams = null;
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
                            case (ushort)Key.GTModelTypeGeoKey:
                                {
                                    var modelType = (ModelType)keys[keyIndex + 3];
                                    if (!((modelType == ModelType.Projected) || (modelType == ModelType.Geographic)))
                                    {
                                        throw new FormatException("Only coordinate systems ModelTypeProjected (1) or ModelTypeGeographic (2) are supported.");
                                    }

                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GTRasterTypeGeoKey:
                                {
                                    var rasterType = (RasterType)keys[keyIndex + 3]; // TODO: use RasterTypeCode value
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GTCitationGeoKey:
                                {
                                    var gtc = keys[keyIndex + 3];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeographicTypeGeoKey:
                                {
                                    var geographicType = keys[keyIndex + 3];
                                    if (geographicType != SrsCodes._4326)
                                    {
                                        throw new FormatException($"Only {SrsCodes.EPSG4326} geodetic coordinate system is supported.");
                                    }

                                    srId = geographicType;
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeogCitationGeoKey:
                                {
                                    var geogCitation = keys[keyIndex + 3];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeogGeodeticDatumGeoKey:
                                {
                                    // 6.3.2.2 Geodetic Datum Codes
                                    var geodeticDatum = keys[keyIndex + 3];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeogPrimeMeridianGeoKey:
                                {
                                    // 6.3.2.4 Prime Meridian Codes
                                    var primeMeridian = keys[keyIndex + 3];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeogAngularUnitsGeoKey:
                                {
                                    var geogAngularUnit = (AngularUnits)keys[keyIndex + 3];
                                    if (geogAngularUnit != AngularUnits.Degree)
                                    {
                                        throw new FormatException("Only degree angular unit is supported.");
                                    }

                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeogAngularUnitSizeGeoKey:
                                {
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeogEllipsoidGeoKey:
                                {
                                    // 6.3.2.3 Ellipsoid Codes
                                    var geogEllipsoid = keys[keyIndex + 3];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeogSemiMajorAxisGeoKey:
                                {
                                    if (doubleParams == null)
                                    {
                                        throw new FormatException($"Double values were not found in '{TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG}' tag.");
                                    }

                                    var geogSemiMajorAxis = doubleParams[keys[keyIndex + 3]];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeogSemiMinorAxisGeoKey:
                                {
                                    if (doubleParams == null)
                                    {
                                        throw new FormatException($"Double values were not found in '{TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG}' tag.");
                                    }

                                    var geogSemiMinorAxis = doubleParams[keys[keyIndex + 3]];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeogInvFlatteningGeoKey:
                                {
                                    if (doubleParams == null)
                                    {
                                        throw new FormatException($"Double values were not found in '{TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG}' tag.");
                                    }

                                    var geogInvFlattening = doubleParams[keys[keyIndex + 3]];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeogAzimuthUnitsGeoKey:
                                {
                                    // 6.3.1.4 Angular Units Codes
                                    var geogAzimuthUnits = (AngularUnits)keys[keyIndex + 3];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.GeogPrimeMeridianLongGeoKey:
                                {
                                    var geogPrimeMeridianLong = keys[keyIndex + 3];
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.ProjectedCSTypeGeoKey:
                                {
                                    var projectedCSType = keys[keyIndex + 3];
                                    if (projectedCSType != SrsCodes._3857)
                                    {
                                        throw new FormatException($"Only {SrsCodes.EPSG3857} projected coordinate system is supported (input was: {projectedCSType}).");
                                    }

                                    // TODO: UTM (EPSG:32636 and others) support
                                    srId = projectedCSType;
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.PCSCitationGeoKey:
                                {
                                    keyIndex += 4;
                                    break;
                                }
                            case (ushort)Key.ProjLinearUnitsGeoKey:
                                {
                                    var linearUnit = (LinearUnits)keys[keyIndex + 3];
                                    if (linearUnit != LinearUnits.Meter)
                                    {
                                        throw new FormatException("Only meter linear unit is supported.");
                                    }

                                    keyIndex += 4;
                                    break;
                                }
                            default:
                                {
                                    keyIndex += 4; // Just skipping all unprocessed keys
                                    break;
                                }
                        }
                    }
                }
            }

            M.GeographicalBounds? geographicalBounds = null;
            M.Bounds? projectedBounds = null;
            double pixelWidth = 0, pixelHeight = 0;

            switch (srId)
            {
                case SrsCodes._4326:
                    {
                        geographicalBounds = new M.GeographicalBounds(
                            tiePoints[3],
                            tiePoints[4] - imageHeight * pixelSizes[1],
                            tiePoints[3] + imageWidth * pixelSizes[0],
                            tiePoints[4]);

                        projectedBounds = new M.Bounds(
                            WebMercator.X(tiePoints[3]),
                            WebMercator.Y(tiePoints[4] - imageHeight * pixelSizes[1]),
                            WebMercator.X(tiePoints[3] + imageWidth * pixelSizes[0]),
                            WebMercator.Y(tiePoints[4]));

                        pixelWidth = WebMercator.X(tiePoints[3] + pixelSizes[0]) - WebMercator.X(tiePoints[3]);
                        pixelHeight = WebMercator.Y(tiePoints[4]) - WebMercator.Y(tiePoints[4] - pixelSizes[1]);

                        break;
                    }
                case SrsCodes._3857:
                    {
                        projectedBounds = new M.Bounds(
                            tiePoints[3],
                            tiePoints[4] - imageHeight * pixelSizes[1],
                            tiePoints[3] + imageWidth * pixelSizes[0],
                            tiePoints[4]);

                        geographicalBounds = new M.GeographicalBounds(
                            WebMercator.Longitude(tiePoints[3]),
                            WebMercator.Latitude(tiePoints[4] - imageHeight * pixelSizes[1]),
                            WebMercator.Longitude(tiePoints[3] + imageWidth * pixelSizes[0]),
                            WebMercator.Latitude(tiePoints[4]));

                        pixelWidth = pixelSizes[0];
                        pixelHeight = pixelSizes[1];

                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException($"SRID '{srId}' is not supported.");
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

        /// <summary>
        /// Checks if input image is blank (all pixels are the same color).
        /// </summary>
        /// <param name="imageData">Image data.</param>
        /// <returns>ARGB color value if image is blank or null if not blank.</returns>
        public static uint? CheckIfImageIsBlank(byte[] imageData)
        {
            using var image = SKImage.FromEncodedData(imageData);
            using var bitmap = SKBitmap.FromImage(image);

            var pixelsize = sizeof(uint);
            var span = bitmap.GetPixelSpan();
            var zero = BitConverter.ToUInt32(span[..pixelsize]);

            for (var i = pixelsize; i < span.Length; i += pixelsize)
            {
                var pixel = BitConverter.ToUInt32(span.Slice(i, pixelsize));
                if (pixel != zero)
                {
                    return null;
                }
            }

            return zero;
        }

        public static string GetImageMediaType(byte[] imageData)
        {
            var codec = SKCodec.Create(new MemoryStream(imageData));
            return MediaTypeFromSKEncodedImageFormat(codec.EncodedFormat);
        }

        public static (int Width, int Height)? GetImageSize(byte[] imageData)
        {
            using var image = SKImage.FromEncodedData(imageData);
            return image != null ? (image.Width, image.Height) : null;
        }

        public static SKEncodedImageFormat SKEncodedImageFormatFromMediaType(string mediaType) =>
            mediaType switch
            {
                MediaTypeNames.Image.Png => SKEncodedImageFormat.Png,
                MediaTypeNames.Image.Jpeg => SKEncodedImageFormat.Jpeg,
                MediaTypeNames.Image.Webp => SKEncodedImageFormat.Webp,
                _ => throw new ArgumentOutOfRangeException(nameof(mediaType), $"Media type '{mediaType}' is not supported."),
            };

        public static string MediaTypeFromSKEncodedImageFormat(SKEncodedImageFormat format) =>
            format switch
            {
                SKEncodedImageFormat.Png => MediaTypeNames.Image.Png,
                SKEncodedImageFormat.Jpeg => MediaTypeNames.Image.Jpeg,
                SKEncodedImageFormat.Webp => MediaTypeNames.Image.Webp,
                _ => throw new ArgumentOutOfRangeException(nameof(format), $"Format '{format}' is not supported."),
            };

        public static byte[]? ConvertImageToFormat(byte[] originalImage, string mediaType, int quality)
        {
            using var image = SKImage.FromEncodedData(originalImage);
            if (image != null)
            {
                using var stream = new MemoryStream();
                image.Encode(SKEncodedImageFormatFromMediaType(mediaType), quality).SaveTo(stream);
                return stream.ToArray();
            }
            else
            {
                return null;
            }
        }

        private static void GeoTiffTagExtender(Tiff tiff)
        {
            // https://stackoverflow.com/a/52468691/1182448
            // https://github.com/BitMiracle/libtiff.net/blob/master/Samples/AddCustomTagsToExistingTiff/C%23/AddCustomTagsToExistingTiff.cs

            TiffFieldInfo[] tiffFieldInfo =
            [
                new(TiffTag.GEOTIFF_GEOKEYDIRECTORYTAG, 2, 2, TiffType.SHORT, FieldBit.Custom, false, true, "GeoKeyDirectoryTag"),
                new(TiffTag.GEOTIFF_GEODOUBLEPARAMSTAG, 2, 2, TiffType.DOUBLE, FieldBit.Custom, false, true, "GeoDoubleParamsTag"),
                new(TiffTag.GEOTIFF_MODELPIXELSCALETAG, 2, 2, TiffType.DOUBLE, FieldBit.Custom, false, true, "ModelPixelScaleTag"),
                new(TiffTag.GEOTIFF_MODELTIEPOINTTAG, 2, 2, TiffType.DOUBLE, FieldBit.Custom, false, true, "ModelTiepointTag"),
            ];

            tiff.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length);
        }
    }
}
