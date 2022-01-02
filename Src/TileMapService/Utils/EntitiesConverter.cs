using System;
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
            return format switch
            {
                ImageFormats.Png => MediaTypeNames.Image.Png,
                ImageFormats.Jpeg => MediaTypeNames.Image.Jpeg,
                ImageFormats.Protobuf => MediaTypeNames.Application.XProtobuf,
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

        public static int GetArgbColorFromString(string rgbHexColor, bool isTransparent)
        {
            if (rgbHexColor.StartsWith("0x"))
            {
                rgbHexColor = rgbHexColor.Substring(2);
            }

            return BitConverter.ToInt32(
                new[]
                {
                    Convert.ToByte(rgbHexColor.Substring(4, 2), 16),
                    Convert.ToByte(rgbHexColor.Substring(2, 2), 16),
                    Convert.ToByte(rgbHexColor.Substring(0, 2), 16),
                    (byte)(isTransparent ? 0x00 : 0xFF),
                },
            0);
        }

        public static Models.GeographicalBounds MapRectangleToGeographicalBounds(Models.Bounds rectangle)
        {
            return new Models.GeographicalBounds(
                new Models.GeographicalPoint(WebMercator.Longitude(rectangle.Left), WebMercator.Latitude(rectangle.Bottom)),
                new Models.GeographicalPoint(WebMercator.Longitude(rectangle.Right), WebMercator.Latitude(rectangle.Top)));
        }
    }
}
