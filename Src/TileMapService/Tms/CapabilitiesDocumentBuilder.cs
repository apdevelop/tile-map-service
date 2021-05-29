using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace TileMapService.Tms
{
    /// <summary>
    /// Contains methods for creating XML documents, describing TMS resources.
    /// </summary>
    /// <see href="https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification">Tile Map Service Specification</see>
    class CapabilitiesDocumentBuilder
    {
        private readonly string baseUrl;

        private readonly List<Models.Layer> layers;

        private const int TileWidth = 256; // TODO: other resolutions

        private const int TileHeight = 256;

        #region Constants

        private const string Tms = "tms";

        private const string Services = "Services";

        private const string TileMapService = "TileMapService";   
        
        private const string TileMap = "TileMap";

        private const string VersionAttribute = "version";

        private const string HrefAttribute = "href";

        private const string TileMapServiceVersion = "1.0.0";

        private const string ProfileNone = "none";

        /// <summary>
        /// EPSG:4326
        /// </summary>
        private const string ProfileGlobalGeodetic = "global-geodetic";

        /// <summary>
        /// OSGEO:41001
        /// </summary>
        private const string ProfileGlobalMercator = "global-mercator";

        private const string ProfileLocal = "local";

        #endregion Constants

        public CapabilitiesDocumentBuilder(string baseUrl, IEnumerable<Models.Layer> layers)
        {
            this.baseUrl = baseUrl;
            this.layers = layers.ToList();
        }

        /// <summary>
        /// The root resource describes the available versions of the TileMapService (and possibly other services as well).
        /// </summary>
        /// <remarks>
        /// https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification#Root_Resource
        /// </remarks>
        /// <returns>XML document with available versions of the TileMapService.</returns>
        public XmlDocument GetRootResource()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement(String.Empty, Services, String.Empty);
            doc.AppendChild(root);

            var tileMapService = doc.CreateElement(TileMapService);
            root.AppendChild(tileMapService);

            // TODO: title attribute from source configuration
            ////var titleAttribute = doc.CreateAttribute("title");
            ////titleAttribute.Value = "...";
            ////tileMapService.Attributes.Append(titleAttribute);

            var versionAttribute = doc.CreateAttribute(VersionAttribute);
            versionAttribute.Value = TileMapServiceVersion;
            tileMapService.Attributes.Append(versionAttribute);

            var href = $"{this.baseUrl}/{Tms}/{TileMapServiceVersion}/";
            var hrefAttribute = doc.CreateAttribute(HrefAttribute);
            hrefAttribute.Value = href;
            tileMapService.Attributes.Append(hrefAttribute);

            return doc;
        }

        /// <summary>
        /// The TileMapService resource provides description metadata about the service and lists the available TileMaps.
        /// </summary>
        /// <remarks>
        /// https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification#TileMapService_Resource
        /// </remarks>
        /// <returns></returns>
        public XmlDocument GetTileMapService()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement(String.Empty, TileMapService, String.Empty);
            doc.AppendChild(root);

            var versionAttribute = doc.CreateAttribute(VersionAttribute);
            versionAttribute.Value = TileMapServiceVersion;
            root.Attributes.Append(versionAttribute);

            var tileMaps = doc.CreateElement("TileMaps");
            root.AppendChild(tileMaps);

            foreach (var layer in this.layers)
            {
                var tileMap = doc.CreateElement(TileMap);

                var titleAttribute = doc.CreateAttribute("title");
                titleAttribute.Value = layer.Title;
                tileMap.Attributes.Append(titleAttribute);

                // TODO: Title, Abstract for TileMapService

                var href = $"{this.baseUrl}/{Tms}/{TileMapServiceVersion}/{layer.Identifier}";
                var hrefAttribute = doc.CreateAttribute(HrefAttribute);
                hrefAttribute.Value = href;
                tileMap.Attributes.Append(hrefAttribute);

                // https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification#Profiles
                var profileAttribute = doc.CreateAttribute("profile");
                switch (layer.Srs)
                {
                    case Utils.SrsCodes.EPSG3857: { profileAttribute.Value = ProfileGlobalMercator; break; }
                    case Utils.SrsCodes.EPSG4326: { profileAttribute.Value = ProfileGlobalGeodetic; break; }
                    default: { throw new NotImplementedException($"Unknown SRS '{layer.Srs}'"); } // TODO: local/none ?
                }

                tileMap.Attributes.Append(profileAttribute);

                var srsAttribute = doc.CreateAttribute("srs");
                srsAttribute.Value = layer.Srs;
                tileMap.Attributes.Append(srsAttribute);

                tileMaps.AppendChild(tileMap);
            }

            return doc;
        }

        /// <summary>
        /// The TileMap is a cartographically complete map representation.
        /// </summary>
        /// <param name="layer">Properties of layer.</param>
        /// <remarks>
        /// https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification#TileMap_Resource
        /// </remarks>
        /// <returns></returns>
        public XmlDocument GetTileMap(Models.Layer layer)
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement(String.Empty, TileMap, String.Empty);
            doc.AppendChild(root);

            var versionAttribute = doc.CreateAttribute(VersionAttribute);
            versionAttribute.Value = TileMapServiceVersion;
            root.Attributes.Append(versionAttribute);

            var tilemapservice = $"{this.baseUrl}/{Tms}/{TileMapServiceVersion}/";
            var tilemapserviceAttribute = doc.CreateAttribute("tilemapservice");
            tilemapserviceAttribute.Value = tilemapservice;
            root.Attributes.Append(tilemapserviceAttribute);

            var titleNode = doc.CreateElement("Title");
            titleNode.AppendChild(doc.CreateTextNode(layer.Title));
            root.AppendChild(titleNode);

            // TODO: Abstract for TileMap

            var srs = doc.CreateElement("SRS");
            srs.AppendChild(doc.CreateTextNode(layer.Srs));
            root.AppendChild(srs);

            // TODO: real boundingBox values (from MBTiles metadata, for example)
            var unitsWidth = 0.0;
            var pixelsWidth = 0.0;
            switch (layer.Srs)
            {
                case Utils.SrsCodes.EPSG3857:
                    {
                        // GoogleMapsCompatible tile grid
                        const double MinX = -20037508.342789;
                        const double MinY = -20037508.342789;
                        const double MaxX = +20037508.342789;
                        const double MaxY = +20037508.342789;
                        var boundingBox = CreateBoundingBox(doc, MinX, MinY, MaxX, MaxY, "F6");
                        root.AppendChild(boundingBox);

                        var origin = CreateOrigin(doc, MinX, MinY, "F6");
                        root.AppendChild(origin);

                        unitsWidth = MaxX - MinX;
                        pixelsWidth = TileWidth;
                        break;
                    }
                case Utils.SrsCodes.EPSG4326:
                    {
                        const double MinX = -180;
                        const double MinY = -90;
                        const double MaxX = +180;
                        const double MaxY = +90;
                        var boundingBox = CreateBoundingBox(doc, MinX, MinY, MaxX, MaxY, "F0");
                        root.AppendChild(boundingBox);

                        var origin = CreateOrigin(doc, MinX, MinY, "F0");
                        root.AppendChild(origin);

                        unitsWidth = MaxX - MinX;
                        pixelsWidth = TileWidth * 2;
                        break;
                    }
                default:
                    {
                        throw new NotImplementedException($"Unknown SRS '{layer.Srs}'");
                    }
            }

            var tileFormat = doc.CreateElement("TileFormat");

            var extensionAttribute = doc.CreateAttribute("extension");
            extensionAttribute.Value = layer.Format; // TODO: jpg/jpeg ?
            tileFormat.Attributes.Append(extensionAttribute);

            var mimetypeAttribute = doc.CreateAttribute("mime-type");
            mimetypeAttribute.Value = layer.ContentType;
            tileFormat.Attributes.Append(mimetypeAttribute);

            var heightAttribute = doc.CreateAttribute("height");
            heightAttribute.Value = TileHeight.ToString(CultureInfo.InvariantCulture);
            tileFormat.Attributes.Append(heightAttribute);

            var widthAttribute = doc.CreateAttribute("width");
            widthAttribute.Value = TileWidth.ToString(CultureInfo.InvariantCulture);
            tileFormat.Attributes.Append(widthAttribute);

            root.AppendChild(tileFormat);

            var tileSets = doc.CreateElement("TileSets");
            root.AppendChild(tileSets);

            for (var level = layer.MinZoom; level <= layer.MaxZoom; level++)
            {
                var tileSet = doc.CreateElement("TileSet");

                var href = $"{this.baseUrl}/{Tms}/{TileMapServiceVersion}/{layer.Identifier}/{level}";
                var hrefAttribute = doc.CreateAttribute(HrefAttribute);
                hrefAttribute.Value = href;
                tileSet.Attributes.Append(hrefAttribute);

                var orderAttribute = doc.CreateAttribute("order");
                orderAttribute.Value = level.ToString(CultureInfo.InvariantCulture);
                tileSet.Attributes.Append(orderAttribute);

                var unitsPerPixel = unitsWidth / (pixelsWidth * Math.Pow(2, level));
                var unitsPerPixelAttribute = doc.CreateAttribute("units-per-pixel");
                unitsPerPixelAttribute.Value = unitsPerPixel.ToString(CultureInfo.InvariantCulture);
                tileSet.Attributes.Append(unitsPerPixelAttribute);

                tileSets.AppendChild(tileSet);
            }

            return doc;
        }

        private static XmlElement CreateBoundingBox(XmlDocument doc, double minX, double minY, double maxX, double maxY, string format)
        {
            var boundingBox = doc.CreateElement("BoundingBox");

            var minxAttribute = doc.CreateAttribute("minx");
            minxAttribute.Value = minX.ToString(format, CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(minxAttribute);

            var minyAttribute = doc.CreateAttribute("miny");
            minyAttribute.Value = minY.ToString(format, CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(minyAttribute);

            var maxxAttribute = doc.CreateAttribute("maxx");
            maxxAttribute.Value = maxX.ToString(format, CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(maxxAttribute);

            var maxyAttribute = doc.CreateAttribute("maxy");
            maxyAttribute.Value = maxY.ToString(format, CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(maxyAttribute);

            return boundingBox;
        }

        private static XmlElement CreateOrigin(XmlDocument doc, double originX, double originY, string format)
        {
            var origin = doc.CreateElement("Origin");

            var xAttribute = doc.CreateAttribute("x");
            xAttribute.Value = originX.ToString(format, CultureInfo.InvariantCulture);
            origin.Attributes.Append(xAttribute);

            var yAttribute = doc.CreateAttribute("y");
            yAttribute.Value = originY.ToString(format, CultureInfo.InvariantCulture);
            origin.Attributes.Append(yAttribute);

            return origin;
        }
    }
}
