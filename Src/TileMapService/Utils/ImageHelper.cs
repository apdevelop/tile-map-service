using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace TileMapService.Utils
{
    class ImageHelper
    {
        private const int PixelDataSize = 4;

        public static byte[] CreateEmptyPngImage(int width, int height, int backgroundColor)
        {
            var colorArray = BitConverter.GetBytes(backgroundColor);

            var size = width * height * PixelDataSize;
            var stride = width * PixelDataSize;
            var imageBuffer = new byte[size];

            for (var ix = 0; ix < width; ix++)
            {
                for (var iy = 0; iy < height; iy++)
                {
                    var offset = (iy * width + ix) * PixelDataSize;
                    imageBuffer[offset + 0] = colorArray[0];
                    imageBuffer[offset + 1] = colorArray[1];
                    imageBuffer[offset + 2] = colorArray[2];
                    imageBuffer[offset + 3] = colorArray[3];
                }
            }

            var handle = GCHandle.Alloc(imageBuffer, GCHandleType.Pinned);

            try
            {
                using (var image = new Bitmap(width, height, stride, PixelFormat.Format32bppArgb, handle.AddrOfPinnedObject()))
                {
                    using (var ms = new MemoryStream())
                    {
                        image.Save(ms, ImageFormat.Png);
                        return ms.ToArray();
                    }
                }
            }
            finally
            {
                handle.Free();
            }
        }

        public static ImageFormat ImageFormatFromMediaType(string format)
        {
            if (format == MediaTypeNames.Image.Png)
            {
                return ImageFormat.Png;
            }
            else if (format == MediaTypeNames.Image.Jpeg)
            {
                return ImageFormat.Jpeg;
            }
            else
            {
                throw new ArgumentException("format");
            }
        }

        public static byte[] SaveImageToByteArray(Image image, ImageFormat imageFormat, int jpegQuality = 90)
        {
            using (var ms = new MemoryStream())
            {
                if (imageFormat == ImageFormat.Jpeg)
                {
                    var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                    var encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)jpegQuality);
                    image.Save(ms, jpegEncoder, encoderParameters);

                    return ms.ToArray();
                }
                else
                {
                    image.Save(ms, imageFormat);

                    return ms.ToArray();
                }
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }
    }
}
