using System;
using System.Globalization;
using System.Xml;

using TileMapService.Utils;

namespace TileMapService.Tms
{
    /// <summary>
    /// Contains methods for creating XML documents, describing TMS resources (<see href="https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification">Tile Map Service Specification</see>).
    /// </summary>
    class CapabilitiesUtility
    {
        private readonly string serviceTitle;

        private readonly string serviceAbstract;

        private readonly string baseUrl;

        private readonly Models.Layer[] layers;

        public CapabilitiesUtility(Capabilities capabilities)
        {
            if (capabilities.BaseUrl == null)
            {
                throw new ArgumentNullException(nameof(capabilities), "capabilities.BaseUrl is null.");
            }

            if (capabilities.Layers == null)
            {
                throw new ArgumentNullException(nameof(capabilities), "capabilities.Layers is null.");
            }

            this.serviceTitle = capabilities.ServiceTitle;
            this.serviceAbstract = capabilities.ServiceAbstract;
            this.baseUrl = capabilities.BaseUrl;
            this.layers = capabilities.Layers;
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
            var rootElement = doc.CreateElement(String.Empty, Identifiers.Services, String.Empty);
            doc.AppendChild(rootElement);

            var tileMapServiceElement = doc.CreateElement(Identifiers.TileMapService);
            rootElement.AppendChild(tileMapServiceElement);

            var titleAttribute = doc.CreateAttribute(Identifiers.TitleAttribute);
            titleAttribute.Value = this.serviceTitle;
            tileMapServiceElement.Attributes.Append(titleAttribute);

            var versionAttribute = doc.CreateAttribute(Identifiers.VersionAttribute);
            versionAttribute.Value = Identifiers.Version100;
            tileMapServiceElement.Attributes.Append(versionAttribute);

            var href = $"{this.baseUrl}/{Identifiers.Tms}/{Identifiers.Version100}/";
            var hrefAttribute = doc.CreateAttribute(Identifiers.HRefAttribute);
            hrefAttribute.Value = href;
            tileMapServiceElement.Attributes.Append(hrefAttribute);

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
            var rootElement = doc.CreateElement(String.Empty, Identifiers.TileMapService, String.Empty);
            doc.AppendChild(rootElement);

            var versionAttribute = doc.CreateAttribute(Identifiers.VersionAttribute);
            versionAttribute.Value = Identifiers.Version100;
            rootElement.Attributes.Append(versionAttribute);

            var titleElement = doc.CreateElement(Identifiers.TitleElement);
            titleElement.AppendChild(doc.CreateTextNode(this.serviceTitle));
            rootElement.AppendChild(titleElement);

            var abstractElement = doc.CreateElement(Identifiers.AbstractElement);
            abstractElement.AppendChild(doc.CreateTextNode(this.serviceAbstract));
            rootElement.AppendChild(abstractElement);

            var tileMapsElement = doc.CreateElement("TileMaps");
            rootElement.AppendChild(tileMapsElement);

            foreach (var layer in this.layers)
            {
                var tileMapElement = doc.CreateElement(Identifiers.TileMapElement);

                var titleAttribute = doc.CreateAttribute(Identifiers.TitleAttribute);
                titleAttribute.Value = layer.Title;
                tileMapElement.Attributes.Append(titleAttribute);

                var srsAttribute = CreateSrsAttribute(doc, layer.Srs);
                tileMapElement.Attributes.Append(srsAttribute);

                var profileAttribute = CreateProfileAttribute(doc, layer.Srs);
                tileMapElement.Attributes.Append(profileAttribute);

                var href = $"{this.baseUrl}/{Identifiers.Tms}/{Identifiers.Version100}/{layer.Identifier}";
                var hrefAttribute = doc.CreateAttribute(Identifiers.HRefAttribute);
                hrefAttribute.Value = href;
                tileMapElement.Attributes.Append(hrefAttribute);

                tileMapsElement.AppendChild(tileMapElement);
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
            var rootElement = doc.CreateElement(String.Empty, Identifiers.TileMapElement, String.Empty);
            doc.AppendChild(rootElement);

            var versionAttribute = doc.CreateAttribute(Identifiers.VersionAttribute);
            versionAttribute.Value = Identifiers.Version100;
            rootElement.Attributes.Append(versionAttribute);

            var tilemapservice = $"{this.baseUrl}/{Identifiers.Tms}/{Identifiers.Version100}";
            var tilemapserviceAttribute = doc.CreateAttribute("tilemapservice");
            tilemapserviceAttribute.Value = tilemapservice;
            rootElement.Attributes.Append(tilemapserviceAttribute);

            var titleElement = doc.CreateElement(Identifiers.TitleElement);
            titleElement.AppendChild(doc.CreateTextNode(layer.Title));
            rootElement.AppendChild(titleElement);

            var abstractElement = doc.CreateElement(Identifiers.AbstractElement);
            abstractElement.AppendChild(doc.CreateTextNode(layer.Abstract));
            rootElement.AppendChild(abstractElement);

            double unitsWidth, pixelsWidth;
            switch (layer.Srs)
            {
                case Utils.SrsCodes.EPSG3857:
                    {
                        // Assuming 1x1 tile grid at zoom level 0
                        unitsWidth = 20037508.342789 * 2;
                        pixelsWidth = layer.TileWidth;

                        var srsElement = doc.CreateElement(Identifiers.SrsElement);
                        srsElement.AppendChild(doc.CreateTextNode(Utils.SrsCodes.OSGEO41001));
                        rootElement.AppendChild(srsElement);

                        // GoogleMapsCompatible tile grid
                        var minX = layer.GeographicalBounds == null ? -20037508.342789 : Utils.WebMercator.X(layer.GeographicalBounds.MinLongitude);
                        var minY = layer.GeographicalBounds == null ? -20037508.342789 : Utils.WebMercator.Y(layer.GeographicalBounds.MinLatitude);
                        var maxX = layer.GeographicalBounds == null ? +20037508.342789 : Utils.WebMercator.X(layer.GeographicalBounds.MaxLongitude);
                        var maxY = layer.GeographicalBounds == null ? +20037508.342789 : Utils.WebMercator.Y(layer.GeographicalBounds.MaxLatitude);

                        var boundingBoxElement = CreateBoundingBoxElement(doc, minX, minY, maxX, maxY, "F6");
                        rootElement.AppendChild(boundingBoxElement);

                        var originElement = CreateOriginElement(doc, minX, minY, "F6");
                        rootElement.AppendChild(originElement);

                        break;
                    }
                case Utils.SrsCodes.EPSG4326:
                    {
                        // Assuming 2x1 tile grid at zoom level 0
                        unitsWidth = 360;
                        pixelsWidth = layer.TileWidth * 2;

                        var srsElement = doc.CreateElement(Identifiers.SrsElement);
                        srsElement.AppendChild(doc.CreateTextNode(Utils.SrsCodes.EPSG4326));
                        rootElement.AppendChild(srsElement);

                        var minX = layer.GeographicalBounds == null ? -180 : layer.GeographicalBounds.MinLongitude;
                        var minY = layer.GeographicalBounds == null ? -90 : layer.GeographicalBounds.MinLatitude;
                        var maxX = layer.GeographicalBounds == null ? +180 : layer.GeographicalBounds.MaxLongitude;
                        var maxY = layer.GeographicalBounds == null ? +90 : layer.GeographicalBounds.MaxLatitude;

                        var boundingBoxElement = CreateBoundingBoxElement(doc, minX, minY, maxX, maxY, "F6");
                        rootElement.AppendChild(boundingBoxElement);

                        var originElement = CreateOriginElement(doc, minX, minY, "F6");
                        rootElement.AppendChild(originElement);

                        break;
                    }
                default:
                    {
                        throw new NotImplementedException($"Unknown SRS '{layer.Srs}'");
                    }
            }

            var tileFormatElement = doc.CreateElement(Identifiers.TileFormatElement);

            var widthAttribute = doc.CreateAttribute("width");
            widthAttribute.Value = layer.TileWidth.ToString(CultureInfo.InvariantCulture);
            tileFormatElement.Attributes.Append(widthAttribute);

            var heightAttribute = doc.CreateAttribute("height");
            heightAttribute.Value = layer.TileHeight.ToString(CultureInfo.InvariantCulture);
            tileFormatElement.Attributes.Append(heightAttribute);

            var mimetypeAttribute = doc.CreateAttribute("mime-type");
            mimetypeAttribute.Value = layer.ContentType;
            tileFormatElement.Attributes.Append(mimetypeAttribute);

            var extensionAttribute = doc.CreateAttribute("extension");
            extensionAttribute.Value = layer.Format;
            tileFormatElement.Attributes.Append(extensionAttribute);

            rootElement.AppendChild(tileFormatElement);

            var tileSetsElement = doc.CreateElement("TileSets");
            var profileAttribute = CreateProfileAttribute(doc, layer.Srs);
            tileSetsElement.Attributes.Append(profileAttribute);

            rootElement.AppendChild(tileSetsElement);

            // TODO: four tiles at level 0 covering the whole earth
            for (var level = layer.MinZoom; level <= layer.MaxZoom; level++)
            {
                var unitsPerPixel = unitsWidth / (pixelsWidth * Math.Pow(2, level));

                var tileSetElement = doc.CreateElement(Identifiers.TileSetElement);

                var href = $"{this.baseUrl}/{Identifiers.Tms}/{Identifiers.Version100}/{layer.Identifier}/{level}";
                var hrefAttribute = doc.CreateAttribute(Identifiers.HRefAttribute);
                hrefAttribute.Value = href;
                tileSetElement.Attributes.Append(hrefAttribute);

                var unitsPerPixelAttribute = doc.CreateAttribute(Identifiers.UnitsPerPixelAttribute);
                unitsPerPixelAttribute.Value = unitsPerPixel.ToString(CultureInfo.InvariantCulture);
                tileSetElement.Attributes.Append(unitsPerPixelAttribute);

                var orderAttribute = doc.CreateAttribute("order");
                orderAttribute.Value = level.ToString(CultureInfo.InvariantCulture);
                tileSetElement.Attributes.Append(orderAttribute);

                tileSetsElement.AppendChild(tileSetElement);
            }

            return doc;
        }

        public static Models.Layer ParseTileMap(XmlDocument xml)
        {
            // TODO: some elements can be missed
            var srsElement = xml.SelectSingleNode($"/{Identifiers.TileMapElement}/{Identifiers.SrsElement}");
            // TODO: var boundingBoxElement = xml.SelectSingleNode($"/{Identifiers.TileMapElement}/{Identifiers.BoundingBoxElement}");
            var tileFormatElement = xml.SelectSingleNode($"/{Identifiers.TileMapElement}/{Identifiers.TileFormatElement}");
            ////var tileSets = xml.SelectNodes($"/{Identifiers.TileMapElement}/TileSets/TileSet")
            ////    .OfType<XmlNode>()
            ////    .OrderBy(n => Int32.Parse(n.Attributes["order"].Value, CultureInfo.InvariantCulture))
            ////    .ToArray();

            var width = tileFormatElement != null && tileFormatElement.Attributes != null && tileFormatElement.Attributes["width"] != null ?
                tileFormatElement.Attributes["width"]?.Value : null;
            var height = tileFormatElement != null && tileFormatElement.Attributes != null && tileFormatElement.Attributes["height"] != null ?
                tileFormatElement.Attributes["height"]?.Value : null;

            return new Models.Layer
            {
                TileWidth = width != null ? Int32.Parse(width, CultureInfo.InvariantCulture) : WebMercator.DefaultTileWidth,
                TileHeight = height != null ? Int32.Parse(height, CultureInfo.InvariantCulture) : WebMercator.DefaultTileHeight,
                ContentType = tileFormatElement?.Attributes?["mime-type"]?.Value,
                Srs = srsElement?.InnerText,
                // TODO: MinZoom, MaxZoom
            };
        }

        private static XmlAttribute CreateSrsAttribute(XmlDocument doc, string? srs)
        {
            var srsAttribute = doc.CreateAttribute("srs");

            switch (srs)
            {
                case Utils.SrsCodes.EPSG3857:
                    {
                        srsAttribute.Value = Utils.SrsCodes.OSGEO41001;
                        break;
                    }
                case Utils.SrsCodes.EPSG4326:
                    {
                        srsAttribute.Value = Utils.SrsCodes.EPSG4326;
                        break;
                    }
                default:
                    {
                        // TODO: local/none ?
                        throw new NotImplementedException($"Unknown SRS '{srs}'");
                    }
            }

            return srsAttribute;
        }

        private static XmlAttribute CreateProfileAttribute(XmlDocument doc, string? srs)
        {
            // https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification#Profiles
            var profileAttribute = doc.CreateAttribute("profile");

            switch (srs)
            {
                case Utils.SrsCodes.EPSG3857:
                    {
                        profileAttribute.Value = Identifiers.ProfileGlobalMercator;
                        break;
                    }
                case Utils.SrsCodes.EPSG4326:
                    {
                        profileAttribute.Value = Identifiers.ProfileGlobalGeodetic;
                        break;
                    }
                default:
                    {
                        // TODO: local/none ?
                        throw new NotImplementedException($"Unknown SRS '{srs}'");
                    }
            }

            return profileAttribute;
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
