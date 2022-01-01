using System;
using System.IO;

using SkiaSharp;

namespace TileMapService.Utils
{
    public class ImageHelper
    {
        public static byte[] CreateEmptyPngImage(int width, int height, int backgroundColor)
        {
            var imageInfo = new SKImageInfo(
                width: width,
                height: height,
                colorType: SKColorType.Rgba8888,
                alphaType: SKAlphaType.Premul);

            using var surface = SKSurface.Create(imageInfo);
            using var canvas = surface.Canvas;
            canvas.Clear(new SKColor((uint)backgroundColor)); // TODO: ? uint parameter

            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode();

            return data.ToArray();
        }

        public static SKEncodedImageFormat SKEncodedImageFormatFromMediaType(string format)
        {
            if (format == MediaTypeNames.Image.Png)
            {
                return SKEncodedImageFormat.Png;
            }
            else if (format == MediaTypeNames.Image.Jpeg)
            {
                return SKEncodedImageFormat.Jpeg;
            }
            else
            {
                throw new ArgumentException("format");
            }
        }
    }
}
