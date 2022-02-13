using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace TileMapService.Tms
{
    /// <summary>
    /// Contains methods for creating XML documents, describing TMS resources (<see href="https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification">Tile Map Service Specification</see>).
    /// </summary>
    class CapabilitiesUtility
    {
        private readonly string baseUrl;

        private readonly List<Models.Layer> layers;

        private const int TileWidth = 256; // TODO: cusom resolution values

        private const int TileHeight = 256;

        public CapabilitiesUtility(string baseUrl, IEnumerable<Models.Layer> layers)
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
            var root = doc.CreateElement(String.Empty, Identifiers.Services, String.Empty);
            doc.AppendChild(root);

            var tileMapService = doc.CreateElement(Identifiers.TileMapService);
            root.AppendChild(tileMapService);

            // TODO: title attribute from source configuration
            ////var titleAttribute = doc.CreateAttribute("title");
            ////titleAttribute.Value = "...";
            ////tileMapService.Attributes.Append(titleAttribute);

            var versionAttribute = doc.CreateAttribute(Identifiers.VersionAttribute);
            versionAttribute.Value = Identifiers.Version100;
            tileMapService.Attributes.Append(versionAttribute);

            var href = $"{this.baseUrl}/{Identifiers.Tms}/{Identifiers.Version100}/";
            var hrefAttribute = doc.CreateAttribute(Identifiers.HrefAttribute);
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
            var root = doc.CreateElement(String.Empty, Identifiers.TileMapService, String.Empty);
            doc.AppendChild(root);

            var versionAttribute = doc.CreateAttribute(Identifiers.VersionAttribute);
            versionAttribute.Value = Identifiers.Version100;
            root.Attributes.Append(versionAttribute);

            var tileMaps = doc.CreateElement("TileMaps");
            root.AppendChild(tileMaps);

            foreach (var layer in this.layers)
            {
                var tileMap = doc.CreateElement(Identifiers.TileMap);

                var titleAttribute = doc.CreateAttribute("title");
                titleAttribute.Value = layer.Title;
                tileMap.Attributes.Append(titleAttribute);

                // TODO: Title, Abstract for TileMapService

                var href = $"{this.baseUrl}/{Identifiers.Tms}/{Identifiers.Version100}/{layer.Identifier}";
                var hrefAttribute = doc.CreateAttribute(Identifiers.HrefAttribute);
                hrefAttribute.Value = href;
                tileMap.Attributes.Append(hrefAttribute);

                // https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification#Profiles
                var profileAttribute = doc.CreateAttribute("profile");
                switch (layer.Srs)
                {
                    case Utils.SrsCodes.EPSG3857: { profileAttribute.Value = Identifiers.ProfileGlobalMercator; break; }
                    case Utils.SrsCodes.EPSG4326: { profileAttribute.Value = Identifiers.ProfileGlobalGeodetic; break; }
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
            var root = doc.CreateElement(String.Empty, Identifiers.TileMap, String.Empty);
            doc.AppendChild(root);

            var versionAttribute = doc.CreateAttribute(Identifiers.VersionAttribute);
            versionAttribute.Value = Identifiers.Version100;
            root.Attributes.Append(versionAttribute);

            var tilemapservice = $"{this.baseUrl}/{Identifiers.Tms}/{Identifiers.Version100}/";
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

            double unitsWidth, pixelsWidth;

            switch (layer.Srs)
            {
                case Utils.SrsCodes.EPSG3857:
                    {
                        // GoogleMapsCompatible tile grid
                        var minX = layer.GeographicalBounds == null ? -20037508.342789 : Utils.WebMercator.X(layer.GeographicalBounds.MinLongitude);
                        var minY = layer.GeographicalBounds == null ? -20037508.342789 : Utils.WebMercator.Y(layer.GeographicalBounds.MinLatitude);
                        var maxX = layer.GeographicalBounds == null ? +20037508.342789 : Utils.WebMercator.X(layer.GeographicalBounds.MaxLongitude);
                        var maxY = layer.GeographicalBounds == null ? +20037508.342789 : Utils.WebMercator.Y(layer.GeographicalBounds.MaxLatitude);
                        var boundingBox = CreateBoundingBoxElement(doc, minX, minY, maxX, maxY, "F6");
                        root.AppendChild(boundingBox);

                        var origin = CreateOriginElement(doc, minX, minY, "F6");
                        root.AppendChild(origin);

                        unitsWidth = maxX - minX;
                        pixelsWidth = TileWidth;
                        break;
                    }
                case Utils.SrsCodes.EPSG4326:
                    {
                        // TODO: custom bounds from source properties
                        const double MinX = -180;
                        const double MinY = -90;
                        const double MaxX = +180;
                        const double MaxY = +90;
                        var boundingBox = CreateBoundingBoxElement(doc, MinX, MinY, MaxX, MaxY, "F0");
                        root.AppendChild(boundingBox);

                        var origin = CreateOriginElement(doc, MinX, MinY, "F0");
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
            extensionAttribute.Value = layer.Format;
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

                var href = $"{this.baseUrl}/{Identifiers.Tms}/{Identifiers.Version100}/{layer.Identifier}/{level}";
                var hrefAttribute = doc.CreateAttribute(Identifiers.HrefAttribute);
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

        private static XmlElement CreateBoundingBoxElement(XmlDocument doc, double minX, double minY, double maxX, double maxY, string format)
        {
            var boundingBoxElement = doc.CreateElement("BoundingBox");

            var minxAttribute = doc.CreateAttribute("minx");
            minxAttribute.Value = minX.ToString(format, CultureInfo.InvariantCulture);
            boundingBoxElement.Attributes.Append(minxAttribute);

            var minyAttribute = doc.CreateAttribute("miny");
            minyAttribute.Value = minY.ToString(format, CultureInfo.InvariantCulture);
            boundingBoxElement.Attributes.Append(minyAttribute);

            var maxxAttribute = doc.CreateAttribute("maxx");
            maxxAttribute.Value = maxX.ToString(format, CultureInfo.InvariantCulture);
            boundingBoxElement.Attributes.Append(maxxAttribute);

            var maxyAttribute = doc.CreateAttribute("maxy");
            maxyAttribute.Value = maxY.ToString(format, CultureInfo.InvariantCulture);
            boundingBoxElement.Attributes.Append(maxyAttribute);

            return boundingBoxElement;
        }

        private static XmlElement CreateOriginElement(XmlDocument doc, double originX, double originY, string format)
        {
            var originElement = doc.CreateElement("Origin");

            var xAttribute = doc.CreateAttribute("x");
            xAttribute.Value = originX.ToString(format, CultureInfo.InvariantCulture);
            originElement.Attributes.Append(xAttribute);

            var yAttribute = doc.CreateAttribute("y");
            yAttribute.Value = originY.ToString(format, CultureInfo.InvariantCulture);
            originElement.Attributes.Append(yAttribute);

            return originElement;
        }
    }
}
