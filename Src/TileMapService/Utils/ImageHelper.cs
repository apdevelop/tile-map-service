using System;
using System.IO;

using BitMiracle.LibTiff.Classic;
using SkiaSharp;

namespace TileMapService.Utils
{
    public class ImageHelper
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

        public static byte[] CreateTiffImage(byte[] raster, int width, int height)
        {
            using var ms = new MemoryStream();
            var stm = new TiffStream();
            using (var tiff = Tiff.ClientOpen(String.Empty, "w", ms, stm))
            {
                tiff.SetField(TiffTag.IMAGEWIDTH, width);
                tiff.SetField(TiffTag.IMAGELENGTH, height);
                tiff.SetField(TiffTag.COMPRESSION, Compression.LZW);
                tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);

                tiff.SetField(TiffTag.ROWSPERSTRIP, height);

                ////tif.SetField(TiffTag.XRESOLUTION, horizontalResolution);
                ////tif.SetField(TiffTag.YRESOLUTION, verticalResolution);

                tiff.SetField(TiffTag.BITSPERSAMPLE, 8);
                tiff.SetField(TiffTag.SAMPLESPERPIXEL, 4);

                tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                tiff.SetField(TiffTag.EXTRASAMPLES, 1, new short[] { (short)ExtraSample.UNASSALPHA });

                // TODO: Write GeoTIFF tags 

                var stride = raster.Length / height;
                for (int i = 0, offset = 0; i < height; i++)
                {
                    tiff.WriteScanline(raster, offset, i, 0);
                    offset += stride;
                }

                tiff.Close();
            }

            return ms.ToArray();
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
            var zero = BitConverter.ToUInt32(span.Slice(0, pixelsize));

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
            return image != null ?
                (image.Width, image.Height) :
                null;
        }

        public static SKEncodedImageFormat SKEncodedImageFormatFromMediaType(string mediaType)
        {
            return mediaType switch
            {
                MediaTypeNames.Image.Png => SKEncodedImageFormat.Png,
                MediaTypeNames.Image.Jpeg => SKEncodedImageFormat.Jpeg,
                MediaTypeNames.Image.Webp => SKEncodedImageFormat.Webp,
                _ => throw new ArgumentOutOfRangeException(nameof(mediaType), $"Media type '{mediaType}' is not supported."),
            };
        }

        public static string MediaTypeFromSKEncodedImageFormat(SKEncodedImageFormat format)
        {
            return format switch
            {
                SKEncodedImageFormat.Png => MediaTypeNames.Image.Png,
                SKEncodedImageFormat.Jpeg => MediaTypeNames.Image.Jpeg,
                SKEncodedImageFormat.Webp => MediaTypeNames.Image.Webp,
                _ => throw new ArgumentOutOfRangeException(nameof(format), $"Format '{format}' is not supported."),
            };
        }

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
    }
}
