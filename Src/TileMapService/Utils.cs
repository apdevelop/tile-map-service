using System;
using System.Runtime.CompilerServices;

namespace TileMapService
{
    /// <summary>
    /// Various utility functions (for all types of tile sources)
    /// </summary>
    static class Utils
    {
        public static readonly string ImagePng = "image/png";

        public static readonly string ImageJpeg = "image/jpeg";

        public static readonly string TextXml = "text/xml";

        public static readonly string LocalFileScheme = "file:///";

        public static readonly string MBTilesScheme = "mbtiles:///";

        public static readonly string TileMapServiceVersion = "1.0.0";

        public static readonly string EPSG3857 = "EPSG:3857";

        public static string GetContentType(string tileFormat)
        {
            string mediaType;
            switch (tileFormat)
            {
                case "png": { mediaType = ImagePng; break; }
                case "jpg": { mediaType = ImageJpeg; break; }
                default: throw new ArgumentException($"Bad tileFormat: {tileFormat}");
            }

            return mediaType;
        }

        public static bool IsMBTilesScheme(string source)
        {
            return source.StartsWith(MBTilesScheme, StringComparison.Ordinal);
        }

        public static bool IsLocalFileScheme(string source)
        {
            return source.StartsWith(LocalFileScheme, StringComparison.Ordinal);
        }

        /// <summary>
        /// Flips Y coordinate (according to XYZ/TMS coordinate systems conversion)
        /// </summary>
        /// <param name="y">Y tile coordinate</param>
        /// <param name="zoom">Zoom level</param>
        /// <returns>Flipped Y coordinate</returns>
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static int FlipYCoordinate(int y, int zoom)
        {
            return (1 << zoom) - y - 1;
        }
    }
}
