using System;
using System.Globalization;
using System.Xml;

namespace TileMapService
{
    class CapabilitiesDocumentBuilder
    {
        private readonly string baseUrl;

        private readonly ITileSourceFabric tileSources;

        public CapabilitiesDocumentBuilder(string baseUrl, ITileSourceFabric tileSources)
        {
            this.baseUrl = baseUrl;
            this.tileSources = tileSources;
        }

        public XmlDocument GetServices()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement(String.Empty, "Services", String.Empty);
            doc.AppendChild(root);

            var child = doc.CreateElement("TileMapService");
            root.AppendChild(child);

            var href = $"{this.baseUrl}/tms/{Utils.TileMapServiceVersion}/";
            var hrefAttribute = doc.CreateAttribute("href");
            hrefAttribute.Value = href;
            child.Attributes.Append(hrefAttribute);

            var versionAttribute = doc.CreateAttribute("version");
            versionAttribute.Value = "1.0";
            child.Attributes.Append(versionAttribute);

            return doc;
        }

        public XmlDocument GetTileMaps()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement(String.Empty, "TileMapService", String.Empty);
            doc.AppendChild(root);

            var versionAttribute = doc.CreateAttribute("version");
            versionAttribute.Value = Utils.TileMapServiceVersion;
            root.Attributes.Append(versionAttribute);

            var tileMaps = doc.CreateElement("TileMaps");
            root.AppendChild(tileMaps);

            foreach (var tileSource in this.tileSources.TileSources)
            {
                var tileMap = doc.CreateElement("TileMap");

                var titleAttribute = doc.CreateAttribute("title");
                titleAttribute.Value = tileSource.Value.Configuration.Name;
                tileMap.Attributes.Append(titleAttribute);

                var href = $"{this.baseUrl}/tms/{Utils.TileMapServiceVersion}/{tileSource.Value.Configuration.Name}";
                var hrefAttribute = doc.CreateAttribute("href");
                hrefAttribute.Value = href;
                tileMap.Attributes.Append(hrefAttribute);

                var profileAttribute = doc.CreateAttribute("profile");
                profileAttribute.Value = "global-mercator";
                tileMap.Attributes.Append(profileAttribute);

                var srsAttribute = doc.CreateAttribute("srs");
                srsAttribute.Value = Utils.EPSG3857;
                tileMap.Attributes.Append(srsAttribute);

                tileMaps.AppendChild(tileMap);
            }

            return doc;
        }

        public XmlDocument GetTileSets(string tilemapName)
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement(String.Empty, "TileMap", String.Empty);
            doc.AppendChild(root);

            var versionAttribute = doc.CreateAttribute("version");
            versionAttribute.Value = Utils.TileMapServiceVersion;
            root.Attributes.Append(versionAttribute);

            var tilemapservice = $"{this.baseUrl}/tms/{Utils.TileMapServiceVersion}/";
            var tilemapserviceAttribute = doc.CreateAttribute("tilemapservice");
            tilemapserviceAttribute.Value = tilemapservice;
            root.Attributes.Append(tilemapserviceAttribute);


            var titleNode = doc.CreateElement("Title");
            titleNode.AppendChild(doc.CreateTextNode(tilemapName));
            root.AppendChild(titleNode);

            var abstractNode = doc.CreateElement("Abstract");
            abstractNode.AppendChild(doc.CreateTextNode(tilemapName));
            root.AppendChild(abstractNode);

            var srs = doc.CreateElement("SRS");
            srs.AppendChild(doc.CreateTextNode(Utils.EPSG3857));
            root.AppendChild(srs);

            const int TileSize = 256;
            const double maxy = 20037508.342789;
            const double maxx = 20037508.342789;
            const double miny = -20037508.342789;
            const double minx = -20037508.342789;

            var boundingBox = doc.CreateElement("BoundingBox");

            var maxyAttribute = doc.CreateAttribute("maxy");
            maxyAttribute.Value = maxy.ToString("F6", CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(maxyAttribute);

            var maxxAttribute = doc.CreateAttribute("maxx");
            maxxAttribute.Value = maxx.ToString("F6", CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(maxxAttribute);

            var minyAttribute = doc.CreateAttribute("miny");
            minyAttribute.Value = miny.ToString("F6", CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(minyAttribute);

            var minxAttribute = doc.CreateAttribute("minx");
            minxAttribute.Value = minx.ToString("F6", CultureInfo.InvariantCulture);
            boundingBox.Attributes.Append(minxAttribute);

            root.AppendChild(boundingBox);


            var origin = doc.CreateElement("Origin");

            var yAttribute = doc.CreateAttribute("y");
            yAttribute.Value = miny.ToString("F6", CultureInfo.InvariantCulture);
            origin.Attributes.Append(yAttribute);

            var xAttribute = doc.CreateAttribute("x");
            xAttribute.Value = minx.ToString("F6", CultureInfo.InvariantCulture);
            origin.Attributes.Append(xAttribute);

            root.AppendChild(origin);

            var tm = this.tileSources.TileSources[tilemapName];

            var tileFormat = doc.CreateElement("TileFormat");

            var extensionAttribute = doc.CreateAttribute("extension");
            extensionAttribute.Value = tm.Configuration.Format;
            tileFormat.Attributes.Append(extensionAttribute);

            var mimetypeAttribute = doc.CreateAttribute("mime-type");
            mimetypeAttribute.Value = tm.ContentType;
            tileFormat.Attributes.Append(mimetypeAttribute);

            var heightAttribute = doc.CreateAttribute("height");
            heightAttribute.Value = TileSize.ToString(CultureInfo.InvariantCulture);
            tileFormat.Attributes.Append(heightAttribute);

            var widthAttribute = doc.CreateAttribute("width");
            widthAttribute.Value = TileSize.ToString(CultureInfo.InvariantCulture);
            tileFormat.Attributes.Append(widthAttribute);

            root.AppendChild(tileFormat);


            var tileSets = doc.CreateElement("TileSets");
            root.AppendChild(tileSets);

            for (var level = 0; level <= 18; level++) // TODO: get real existing range
            {
                var tileSet = doc.CreateElement("TileSet");

                var href = $"{this.baseUrl}/tms/{Utils.TileMapServiceVersion}/{tm.Configuration.Name}/{tilemapName}/{level}";
                var hrefAttribute = doc.CreateAttribute("href");
                hrefAttribute.Value = href;
                tileSet.Attributes.Append(hrefAttribute);

                var orderAttribute = doc.CreateAttribute("order");
                orderAttribute.Value = $"{level}";
                tileSet.Attributes.Append(orderAttribute);

                var unitsperpixel = (maxx - minx) / (((double)TileSize) * Math.Pow(2, level));
                var unitsperpixelAttribute = doc.CreateAttribute("units-per-pixel");
                unitsperpixelAttribute.Value = unitsperpixel.ToString(CultureInfo.InvariantCulture);
                tileSet.Attributes.Append(unitsperpixelAttribute);

                tileSets.AppendChild(tileSet);
            }

            return doc;
        }
    }
}
