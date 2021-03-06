using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace TileMapService
{
    /// <summary>
    /// Various utility functions and constants.
    /// </summary>
    static class Utils
    {
        /// <summary>
        /// Media type identifiers.
        /// </summary>
        /// <remarks>
        /// Structure is similar to <see cref="System.Net.Mime.MediaTypeNames"/> class.
        /// </remarks>
        internal static class MediaTypeNames
        {
            public static class Image
            {
                public const string Png = "image/png";

                public const string Jpeg = "image/jpeg";
            }

            public static class Text
            {
                public const string Xml = "text/xml";
            }
        }

        public static readonly string LocalFileScheme = "file:///";

        public static readonly string MBTilesScheme = "mbtiles:///";

        public static string TileFormatToContentType(string format)
        {
            switch (format)
            {
                case "png": return MediaTypeNames.Image.Png;
                case "jpg": return MediaTypeNames.Image.Jpeg;
                // TODO: ? other MBTiles possible types
                default: return format;
            }
        }

        /// <summary>
        /// Saves <paramref name="xml"/> document with header to byte array using UTF-8 encoding.
        /// </summary>
        /// <param name="xml">XML Document</param>
        /// <returns></returns>
        public static byte[] ToUTF8ByteArray(this XmlDocument xml)
        {
            using (var ms = new MemoryStream())
            {
                using (var xw = XmlWriter.Create(new StreamWriter(ms, Encoding.UTF8)))
                {
                    xml.Save(xw);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Flips tile Y coordinate (according to XYZ/TMS coordinate systems conversion).
        /// </summary>
        /// <param name="y">Tile Y coordinate.</param>
        /// <param name="zoom">Tile zoom level.</param>
        /// <returns>Flipped tile Y coordinate.</returns>
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static int FlipYCoordinate(int y, int zoom)
        {
            return (1 << zoom) - y - 1;
        }
    }
}
