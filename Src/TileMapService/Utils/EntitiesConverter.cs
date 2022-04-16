using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using TileMapService.Models;

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
                ImageFormats.MapboxVectorTile => MediaTypeNames.Application.MapboxVectorTile,
                // TODO: other possible types
                _ => format,
            };
        }

        public static string ExtensionToMediaType(string extension)
        {
            return extension switch
            {
                "png" => MediaTypeNames.Image.Png,
                "jpg" => MediaTypeNames.Image.Jpeg,
                "jpeg" => MediaTypeNames.Image.Jpeg,
                "webp" => MediaTypeNames.Image.Webp,
                "tif" => MediaTypeNames.Image.Tiff,
                "tiff" => MediaTypeNames.Image.Tiff,
                "mvt" => MediaTypeNames.Application.MapboxVectorTile,
                // TODO: other possible types
                _ => extension,
            };
        }

        public static bool IsFormatInList(IList<string> mediaTypes, string mediaType)
        {
            return mediaTypes.Any(mt =>
                String.Compare(mediaType, mt, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public static List<Layer> SourcesToLayers(IEnumerable<SourceConfiguration> sources)
        {
            return sources
               .Select(c => SourceConfigurationToLayer(c))
               .ToList();
        }

        private static Layer SourceConfigurationToLayer(SourceConfiguration c)
        {
            return new Layer
            {
                Identifier = c.Id,
                Title = c.Title,
                Abstract = c.Abstract,
                ContentType = c.ContentType,
                Format = c.Format,
                Srs = c.Srs,
                MinZoom = c.MinZoom != null ? c.MinZoom.Value : 0,
                MaxZoom = c.MaxZoom != null ? c.MaxZoom.Value : 24,
                GeographicalBounds = c.GeographicalBounds,
                TileWidth = c.TileWidth,
                TileHeight = c.TileHeight,
            };
        }

        /// <summary>
        /// Saves <paramref name="xml"/> document with header to byte array using UTF-8 encoding.
        /// </summary>
        /// <param name="xml">XML Document.</param>
        /// <returns>Contents of XML document.</returns>
        public static byte[] ToUTF8ByteArray(this XmlDocument xml)
        {
            using var ms = new MemoryStream();
            using (var xw = XmlWriter.Create(new StreamWriter(ms, Encoding.UTF8)))
            {
                xml.Save(xw);
            }

            return ms.ToArray();
        }

        public static uint GetArgbColorFromString(string rgbHexColor, bool isTransparent)
        {
            if (rgbHexColor.StartsWith("0x"))
            {
                rgbHexColor = rgbHexColor[2..];
            }

            return BitConverter.ToUInt32(
                new[]
                {
                    Convert.ToByte(rgbHexColor.Substring(4, 2), 16),
                    Convert.ToByte(rgbHexColor.Substring(2, 2), 16),
                    Convert.ToByte(rgbHexColor.Substring(0, 2), 16),
                    (byte)(isTransparent ? 0x00 : 0xFF),
                },
            0);
        }

        public static GeographicalBounds MapRectangleToGeographicalBounds(Bounds rectangle)
        {
            return new GeographicalBounds(
                new GeographicalPoint(WebMercator.Longitude(rectangle.Left), WebMercator.Latitude(rectangle.Bottom)),
                new GeographicalPoint(WebMercator.Longitude(rectangle.Right), WebMercator.Latitude(rectangle.Top)));
        }
    }
}
