using System;
using System.Collections.Generic;
using System.Linq;

namespace TileMapService.Utils
{
    static class ResponseHelper
    {
        private static readonly string[] SupportedTileFormats =
        [
            MediaTypeNames.Image.Png,
            MediaTypeNames.Image.Jpeg,
            MediaTypeNames.Image.Webp,
        ];

        public static bool IsTileFormatSupported(string mediaType)
        {
            return ResponseHelper.IsFormatInList(SupportedTileFormats, mediaType);
        }

        public static bool IsFormatInList(IList<string> mediaTypes, string mediaType)
        {
            return mediaTypes.Any(mt =>
                string.Compare(mediaType, mt, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public static FileResponse? CreateFileResponse(byte[]? imageContents, string mediaType, string? sourceContentType, int quality)
        {
            if (imageContents != null && imageContents.Length > 0)
            {
                if (string.Compare(mediaType, sourceContentType, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // Return original source image
                    return new FileResponse { FileContents = imageContents, ContentType = mediaType };
                }
                else
                {
                    var isFormatSupported = ResponseHelper.IsTileFormatSupported(mediaType);
                    // Convert source image to requested output format, if possible
                    if (isFormatSupported)
                    {
                        var outputImage = ImageHelper.ConvertImageToFormat(imageContents, mediaType, quality);
                        return outputImage != null
                            ? new FileResponse { FileContents = outputImage, ContentType = mediaType }
                            : null;
                    }
                    else
                    {
                        // Conversion was not possible
                        return new FileResponse { FileContents = imageContents, ContentType = mediaType };
                    }
                }
            }
            else
            {
                return null;
            }
        }
    }

    class FileResponse
    {
#pragma warning disable CS8618
        public byte[] FileContents { get; set; }

        public string ContentType { get; set; }
#pragma warning restore CS8618
    }
}
