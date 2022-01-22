using System;

using SkiaSharp;

namespace TileMapService.Utils
{
    internal class ImageHelper
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

        public static SKEncodedImageFormat SKEncodedImageFormatFromMediaType(string mediaType)
        {
            return mediaType switch
            {
                MediaTypeNames.Image.Png => SKEncodedImageFormat.Png,
                MediaTypeNames.Image.Jpeg => SKEncodedImageFormat.Jpeg,
                _ => throw new ArgumentOutOfRangeException(nameof(mediaType), $"Media type '{mediaType}' is not supported."),
            };
        }
    }
}
