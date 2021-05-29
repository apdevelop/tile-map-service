using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace TileMapService.Utils
{
    /// <summary>
    /// Various utility functions.
    /// </summary>
    static class EntitiesConverter
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

        public static List<Models.Layer> SourcesToLayers(IEnumerable<TileSourceConfiguration> sources)
        {
            return sources
               .Select(c => new Models.Layer
               {
                   Identifier = c.Id,
                   Title = c.Title,
                   ContentType = c.ContentType,
                   Format = c.Format,
                   Srs = c.Srs,
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
    }
}
