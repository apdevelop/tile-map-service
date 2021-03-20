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

        #region Constants

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

        private const int TileWidth = 256; // TODO: other resolutions

        private const int TileHeight = 256;

        #endregion

        #region SRS constants

        /// <summary>
        /// EPSG:3857
        /// </summary>
        /// <remarks>
        /// WGS 84 / Pseudo-Mercator -- Spherical Mercator, Google Maps, OpenStreetMap, Bing, ArcGIS, ESRI
        /// https://epsg.io/3857
        /// </remarks>
        private const string EPSG3857 = "EPSG:3857";

        /// <summary>
        /// EPSG:4326
        /// </summary>
        /// <remarks>
        /// WGS 84 -- WGS84 - World Geodetic System 1984, used in GPS
        /// https://epsg.io/4326
        /// </remarks>
        private const string EPSG4326 = "EPSG:4326";

        /// <summary>
        /// EPSG:900913
        /// </summary>
        /// <remarks>
        /// Google Maps Global Mercator -- Spherical Mercator (unofficial - used in open source projects / OSGEO)
        /// https://epsg.io/900913
        /// </remarks>
        private const string EPSG900913 = "EPSG:900913";

        /// <summary>
        /// EPSG:41001
        /// </summary>
        /// <remarks>
        /// WGS84 / Simple Mercator - Spherical Mercator (unofficial deprecated OSGEO / Tile Map Service) 
        /// https://epsg.io/41001
        /// </remarks>
        private const string OSGEO41001 = "OSGEO:41001";

        #endregion

        public CapabilitiesDocumentBuilder(string baseUrl, IList<Models.Layer> layers)
        {
            this.baseUrl = baseUrl;
            this.layers = layers.ToList();
        }

        /// <summary>
        /// The root resource describes the available versions of the TileMapService (and possibly other services as well).
        /// </summary>
        /// <returns>XML document with available versions of the TileMapService.</returns>
        public XmlDocument GetServices()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement(String.Empty, "Services", String.Empty);
            doc.AppendChild(root);

            var tileMapService = doc.CreateElement("TileMapService");
            root.AppendChild(tileMapService);

            // TODO: title from configuration
            ////var titleAttribute = doc.CreateAttribute("title");
            ////titleAttribute.Value = "Tile Map Service";
            ////tileMapService.Attributes.Append(titleAttribute);

            var versionAttribute = doc.CreateAttribute("version");
            versionAttribute.Value = TileMapServiceVersion;
            tileMapService.Attributes.Append(versionAttribute);

            var href = $"{this.baseUrl}/tms/{TileMapServiceVersion}/";
            var hrefAttribute = doc.CreateAttribute("href");
            hrefAttribute.Value = href;
            tileMapService.Attributes.Append(hrefAttribute);

            return doc;
        }

        public XmlDocument GetTileMaps()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement(String.Empty, "TileMapService", String.Empty);
            doc.AppendChild(root);

            var versionAttribute = doc.CreateAttribute("version");
            versionAttribute.Value = TileMapServiceVersion;
            root.Attributes.Append(versionAttribute);

            var tileMaps = doc.CreateElement("TileMaps");
            root.AppendChild(tileMaps);

            foreach (var tileSource in this.layers)
            {
                var tileMap = doc.CreateElement("TileMap");

                var titleAttribute = doc.CreateAttribute("title");
                titleAttribute.Value = tileSource.Title;
                tileMap.Attributes.Append(titleAttribute);

                // TODO one tile source to several tile grids (test@WGS84, test@GoogleMapsCompatible and so on)
                var href = $"{this.baseUrl}/tms/{TileMapServiceVersion}/{tileSource.Identifier}";
                var hrefAttribute = doc.CreateAttribute("href");
                hrefAttribute.Value = href;
                tileMap.Attributes.Append(hrefAttribute);

                var profileAttribute = doc.CreateAttribute("profile");
                profileAttribute.Value = ProfileGlobalMercator;
                tileMap.Attributes.Append(profileAttribute);

                var srsAttribute = doc.CreateAttribute("srs");
                srsAttribute.Value = EPSG3857;
                tileMap.Attributes.Append(srsAttribute);

                tileMaps.AppendChild(tileMap);
            }

            return doc;
        }

        public XmlDocument GetTileSets(Models.Layer layer)
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement(String.Empty, "TileMap", String.Empty);
            doc.AppendChild(root);

            var versionAttribute = doc.CreateAttribute("version");
            versionAttribute.Value = TileMapServiceVersion;
            root.Attributes.Append(versionAttribute);

            var tilemapservice = $"{this.baseUrl}/tms/{TileMapServiceVersion}/";
            var tilemapserviceAttribute = doc.CreateAttribute("tilemapservice");
            tilemapserviceAttribute.Value = tilemapservice;
            root.Attributes.Append(tilemapserviceAttribute);

            var titleNode = doc.CreateElement("Title");
            titleNode.AppendChild(doc.CreateTextNode(layer.Title));
            root.AppendChild(titleNode);

            var srs = doc.CreateElement("SRS");
            srs.AppendChild(doc.CreateTextNode(EPSG3857));
            root.AppendChild(srs);

            // GoogleMapsCompatible tile grid
            const double MinX = -20037508.342789;
            const double MinY = -20037508.342789;
            const double MaxX = 20037508.342789;
            const double MaxY = 20037508.342789;

            var boundingBox = doc.CreateElement("BoundingBox");

            var minxAttribute = doc.CreateAttribute("minx");
            minxAttribute.Value = MinX.ToString("F6", CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(minxAttribute);

            var minyAttribute = doc.CreateAttribute("miny");
            minyAttribute.Value = MinY.ToString("F6", CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(minyAttribute);

            var maxxAttribute = doc.CreateAttribute("maxx");
            maxxAttribute.Value = MaxX.ToString("F6", CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(maxxAttribute);

            var maxyAttribute = doc.CreateAttribute("maxy");
            maxyAttribute.Value = MaxY.ToString("F6", CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(maxyAttribute);

            root.AppendChild(boundingBox);

            var origin = doc.CreateElement("Origin");

            var yAttribute = doc.CreateAttribute("y");
            yAttribute.Value = MinY.ToString("F6", CultureInfo.InvariantCulture);
            origin.Attributes.Append(yAttribute);

            var xAttribute = doc.CreateAttribute("x");
            xAttribute.Value = MinX.ToString("F6", CultureInfo.InvariantCulture);
            origin.Attributes.Append(xAttribute);

            root.AppendChild(origin);

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

                var href = $"{this.baseUrl}/tms/{TileMapServiceVersion}/{layer.Identifier}/{level}";
                var hrefAttribute = doc.CreateAttribute("href");
                hrefAttribute.Value = href;
                tileSet.Attributes.Append(hrefAttribute);

                var orderAttribute = doc.CreateAttribute("order");
                orderAttribute.Value = $"{level}";
                tileSet.Attributes.Append(orderAttribute);

                // TODO: ? units-per-pixel = 78271.516 / 2^n 
                // TODO: ? an initial zoom level that consists of four 256x256 pixel tiles covering the whole earth
                var unitsperpixel = (MaxX - MinX) / (((double)TileWidth) * Math.Pow(2, level));
                var unitsperpixelAttribute = doc.CreateAttribute("units-per-pixel");
                unitsperpixelAttribute.Value = unitsperpixel.ToString(CultureInfo.InvariantCulture);
                tileSet.Attributes.Append(unitsperpixelAttribute);

                tileSets.AppendChild(tileSet);
            }

            return doc;
        }
    }
}
