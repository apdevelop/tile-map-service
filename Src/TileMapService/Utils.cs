using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace TileMapService
{
    /// <summary>
    /// Various utility functions.
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

        public static byte[] ToByteArray(this XmlDocument xml)
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
