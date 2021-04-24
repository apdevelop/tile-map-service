using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static string TileFormatToContentType(string format)
        {
            return format switch // TODO: const / enum
            {
                "png" => MediaTypeNames.Image.Png,
                "jpg" => MediaTypeNames.Image.Jpeg,
                "pbf" => MediaTypeNames.Application.XProtobuf,
                // TODO: other possible types
                _ => format,
            };
        }

        public static List<Models.Layer> SourcesToLayers(IList<TileSourceConfiguration> sources)
        {
            return sources
               .Select(c => new Models.Layer
               {
                   Identifier = c.Id,
                   Title = c.Title,
                   ContentType = c.ContentType,
                   Format = c.Format,
                   MinZoom = c.MinZoom.Value,
                   MaxZoom = c.MaxZoom.Value,
               })
               .ToList();
        }

        /// <summary>
        /// Saves <paramref name="xml"/> document with header to byte array using UTF-8 encoding.
        /// </summary>
        /// <param name="xml">XML Document.</param>
        /// <returns>Contents of XML document.</returns>
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
