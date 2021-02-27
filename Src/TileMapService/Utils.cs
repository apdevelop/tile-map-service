using System;
using System.Runtime.CompilerServices;

namespace TileMapService
{
    /// <summary>
    /// Various utility functions (for all types of tile sources)
    /// </summary>
    static class Utils
    {
        private const string ImagePng = "image/png";

        private const string ImageJpeg = "image/jpeg";

        public static readonly string LocalFileScheme = "file:///";

        public static readonly string MBTilesScheme = "mbtiles:///";

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
