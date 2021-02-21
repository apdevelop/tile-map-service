using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

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

        public static IList<TileSetConfiguration> GetTileSetConfigurations(this IConfiguration configuration)
        {
            return configuration
                .GetSection("tilesets")
                .Get<IList<TileSetConfiguration>>();
        }

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

        // https://alastaira.wordpress.com/2011/07/06/converting-tms-tile-coordinates-to-googlebingosm-tile-coordinates/

        /// <summary>
        /// Convert Y tile coordinate of TMS standard (flip)
        /// </summary>
        /// <param name="y"></param>
        /// <param name="zoom">Zoom level</param>
        /// <returns></returns>
        public static int FromTmsY(int tmsY, int zoom)
        {
            return (1 << zoom) - tmsY - 1;
        }
    }
}
