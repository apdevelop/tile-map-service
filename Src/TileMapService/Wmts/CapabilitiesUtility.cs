using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

using M = TileMapService.Models;

namespace TileMapService.Wmts
{
    /// <summary>
    /// WMTS capabilities document builder.
    /// Currently supports only Web Mercator (EPSG:3857) / "Google Maps Compatible" 256x256 tile sets.
    /// </summary>
    class CapabilitiesUtility
    {
        private readonly ServiceProperties service;

        private readonly string baseUrl;

        private readonly M.Layer[] layers;

        #region Constants

        private const string WmtsNamespaceUri = "http://www.opengis.net/wmts/1.0";

        private const string OwsPrefix = "ows";

        private const string XlinkPrefix = "xlink";

        #endregion

        // TODO: DTO classes for WMTS capabilities description (like Layer, Capabilities)

        public CapabilitiesUtility(
            ServiceProperties service,
            string baseUrl,
            IEnumerable<M.Layer> layers)
        {
            this.service = service;
            this.baseUrl = baseUrl;
            this.layers = layers.ToArray();
        }

        public XmlDocument GetCapabilities()
        {
            var doc = new XmlDocument();
            var rootElement = doc.CreateElement(String.Empty, "Capabilities", WmtsNamespaceUri);
            rootElement.SetAttribute("xmlns:" + OwsPrefix, Identifiers.OwsNamespaceUri);
            rootElement.SetAttribute("xmlns:" + XlinkPrefix, Identifiers.XlinkNamespaceUri);
            rootElement.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            rootElement.SetAttribute("xmlns:gml", "http://www.opengis.net/gml");
            rootElement.SetAttribute("version", Identifiers.Version100);
            doc.AppendChild(rootElement);

            var serviceIdentificationElement = doc.CreateElement(OwsPrefix, "ServiceIdentification", Identifiers.OwsNamespaceUri);

            var titleElement = doc.CreateElement(OwsPrefix, Identifiers.TitleElement, Identifiers.OwsNamespaceUri);
            titleElement.InnerText = this.service.Title ?? String.Empty;
            serviceIdentificationElement.AppendChild(titleElement);

            var abstractElement = doc.CreateElement(OwsPrefix, Identifiers.AbstractElement, Identifiers.OwsNamespaceUri);
            abstractElement.InnerText = this.service.Abstract ?? String.Empty;
            serviceIdentificationElement.AppendChild(abstractElement);

            var serviceKeywordListElement = doc.CreateElement(OwsPrefix, Identifiers.KeywordsElement, Identifiers.OwsNamespaceUri);
            if (service.Keywords != null)
            {
                foreach (var keyword in service.Keywords)
                {
                    if (!String.IsNullOrWhiteSpace(keyword))
                    {
                        var serviceKeywordElement = doc.CreateElement(OwsPrefix, Identifiers.KeywordElement, Identifiers.OwsNamespaceUri);
                        serviceKeywordElement.InnerText = keyword;
                        serviceKeywordListElement.AppendChild(serviceKeywordElement);
                    }
                }
            }

            serviceIdentificationElement.AppendChild(serviceKeywordListElement);

            var serviceTypeElement = doc.CreateElement(OwsPrefix, "ServiceType", Identifiers.OwsNamespaceUri);
            serviceTypeElement.InnerText = "OGC WMTS";
            serviceIdentificationElement.AppendChild(serviceTypeElement);

            var serviceTypeVersionElement = doc.CreateElement(OwsPrefix, "ServiceTypeVersion", Identifiers.OwsNamespaceUri);
            serviceTypeVersionElement.InnerText = Identifiers.Version100;
            serviceIdentificationElement.AppendChild(serviceTypeVersionElement);

            rootElement.AppendChild(serviceIdentificationElement);

            var serviceProviderElement = doc.CreateElement(OwsPrefix, "ServiceProvider", Identifiers.OwsNamespaceUri);
            var serviceContactElement = doc.CreateElement(OwsPrefix, "ServiceContact", Identifiers.OwsNamespaceUri);
            var contactInfoElement = doc.CreateElement(OwsPrefix, "ContactInfo", Identifiers.OwsNamespaceUri);
            contactInfoElement.InnerText = String.Empty;
            serviceContactElement.AppendChild(contactInfoElement);
            serviceProviderElement.AppendChild(serviceContactElement);

            rootElement.AppendChild(serviceProviderElement);

            var operationsMetadataElement = doc.CreateElement(OwsPrefix, Identifiers.OperationsMetadataElement, Identifiers.OwsNamespaceUri);
            operationsMetadataElement.AppendChild(CreateOperationElement(
                doc,
                [
                    new OperationProperties { Href = this.baseUrl + $"/{Identifiers.Version100}/WMTSCapabilities.xml", Encoding = Identifiers.RESTful },
                    new OperationProperties { Href = this.baseUrl + "?", Encoding = Identifiers.KVP },
                ],
                Identifiers.GetCapabilities));
            operationsMetadataElement.AppendChild(CreateOperationElement(
                doc,
                [
                    new OperationProperties { Href = this.baseUrl + $"/tile/{Identifiers.Version100}/", Encoding = Identifiers.RESTful },
                    new OperationProperties { Href = this.baseUrl + "?", Encoding = Identifiers.KVP },
                ],
                Identifiers.GetTile));
            // TODO: ? GetFeatureInfo
            rootElement.AppendChild(operationsMetadataElement);

            var contentsElement = doc.CreateElement(String.Empty, "Contents", WmtsNamespaceUri);

            var identifiers = new HashSet<string>();
            foreach (var layer in this.layers)
            {
                switch (layer.Srs)
                {
                    case Utils.SrsCodes.EPSG3857:
                        {
                            var identifier = String.Format(CultureInfo.InvariantCulture, "google3857_{0}-{1}", layer.MinZoom, layer.MaxZoom);
                            contentsElement.AppendChild(CreateLayerElement(doc, this.baseUrl, layer, identifier));
                            if (identifiers.Add(identifier))
                            {
                                contentsElement.AppendChild(CreateTileMatrixSetElement(
                                    doc,
                                    layer,
                                    identifier,
                                    "urn:ogc:def:crs:EPSG::3857",
                                    "urn:ogc:def:wkss:OGC:1.0:GoogleMapsCompatible"));
                            }

                            break;
                        }
                    case Utils.SrsCodes.EPSG4326:
                        {
                            var identifier = String.Format(CultureInfo.InvariantCulture, "WGS84_{0}-{1}", layer.MinZoom, layer.MaxZoom);
                            contentsElement.AppendChild(CreateLayerElement(doc, this.baseUrl, layer, identifier));
                            if (identifiers.Add(identifier))
                            {
                                contentsElement.AppendChild(CreateTileMatrixSetElement(
                                    doc,
                                    layer,
                                    identifier,
                                    "urn:ogc:def:crs:EPSG:6.3:4326",
                                    "urn:ogc:def:wkss:OGC:1.0:GoogleCRS84Quad"));
                            }

                            break;
                        }
                    default:
                        {
                            throw new NotImplementedException($"Unknown SRS '{layer.Srs}'.");
                        }
                }
            }

            rootElement.AppendChild(contentsElement);

            return doc;
        }

        // TODO: uniform API for build / parse Capabilities XML document

        /// <summary>
        /// Extracts list of Layers from input Capabilities XML document.
        /// </summary>
        /// <param name="xmlDoc">Capabilities XML document.</param>
        /// <returns>List of Layers (flatten, without hierarchy).</returns>
        public static List<M.Layer> GetLayers(XmlDocument xmlDoc)
        {
            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);

            nsManager.AddNamespace(OwsPrefix, Identifiers.OwsNamespaceUri);
            nsManager.AddNamespace("ns", WmtsNamespaceUri);

            const string LayersXPath = "/ns:Capabilities/ns:Contents/ns:Layer";
            const string TileMatrixSetsXPath = "/ns:Capabilities/ns:Contents/ns:TileMatrixSet";

            var tileMatrixSets = xmlDoc.SelectNodes(TileMatrixSetsXPath, nsManager);

            var result = new List<M.Layer>();
            var layers = xmlDoc.SelectNodes(LayersXPath, nsManager);
            if (layers != null)
            {
                foreach (XmlNode layer in layers)
                {
                    var layerIdentifier = layer.SelectSingleNode(OwsPrefix + ":" + "Identifier", nsManager);
                    var layerTitle = layer.SelectSingleNode(OwsPrefix + ":" + "Title", nsManager);
                    var wgs84bbox = layer.SelectSingleNode(OwsPrefix + ":" + "WGS84BoundingBox", nsManager);
                    var tileMatrixSetLinks = layer.SelectNodes("ns:TileMatrixSetLink", nsManager);

                    M.GeographicalBounds? geographicalBounds = null;
                    if (wgs84bbox != null)
                    {
                        var lowerCorner = wgs84bbox.SelectSingleNode(OwsPrefix + ":" + Identifiers.LowerCornerElement, nsManager);
                        var upperCorner = wgs84bbox.SelectSingleNode(OwsPrefix + ":" + Identifiers.UpperCornerElement, nsManager);
                        if (lowerCorner != null &&
                            upperCorner != null &&
                            !String.IsNullOrEmpty(lowerCorner.InnerText) &&
                            !String.IsNullOrEmpty(lowerCorner.InnerText))
                        {
                            var lowerCornerValues = lowerCorner.InnerText.Split(' ');
                            var upperCornerValues = upperCorner.InnerText.Split(' ');
                            var minx = Double.Parse(lowerCornerValues[0], CultureInfo.InvariantCulture);
                            var miny = Double.Parse(lowerCornerValues[1], CultureInfo.InvariantCulture);
                            var maxx = Double.Parse(upperCornerValues[0], CultureInfo.InvariantCulture);
                            var maxy = Double.Parse(upperCornerValues[1], CultureInfo.InvariantCulture);
                            geographicalBounds = new M.GeographicalBounds(minx, miny, maxx, maxy);
                        }

                        // TODO: use ResourceURL format="image/jpeg"
                    }

                    // Get list of TileMatrixSets for layer
                    var layersTileMatrixSets = new List<M.TileMatrixSet>();
                    if (tileMatrixSetLinks?.Count > 0 && tileMatrixSets?.Count > 0)
                    {
                        foreach (XmlNode tmsLink in tileMatrixSetLinks)
                        {
                            var tileMatrixSetId = tmsLink.SelectSingleNode("ns:TileMatrixSet", nsManager)?.InnerText;
                            var tileMatrixSet = tileMatrixSets.Cast<XmlNode>()
                                .FirstOrDefault(n => n.SelectSingleNode(OwsPrefix + ":" + "Identifier", nsManager)?.InnerText == tileMatrixSetId);
                            var tileMatrixList = tileMatrixSet?.SelectNodes("ns:TileMatrix", nsManager);
                            var supportedCRS = tileMatrixSet?.SelectSingleNode(OwsPrefix + ":" + "SupportedCRS", nsManager)?.InnerText;

                            if (tileMatrixList != null)
                            {
                                var tmss = new List<M.TileMatrix>();
                                foreach (XmlNode tileMatrix in tileMatrixList)
                                {
                                    var tileMatrixIdentifier = tileMatrix.SelectSingleNode(OwsPrefix + ":" + "Identifier", nsManager)?.InnerText;
                                    tmss.Add(new M.TileMatrix { Identifier = tileMatrixIdentifier });
                                }

                                layersTileMatrixSets.Add(new M.TileMatrixSet
                                {
                                    Identifier = tileMatrixSetId,
                                    SupportedCRS = supportedCRS,
                                    TileMatrices = tmss.ToArray(),
                                });
                            }
                        }
                    }

                    result.Add(new M.Layer
                    {
                        Identifier = layerIdentifier != null ? layerIdentifier.InnerText : String.Empty,
                        Title = layerTitle != null ? layerTitle.InnerText : String.Empty,
                        GeographicalBounds = geographicalBounds,
                        TileMatrixSet = layersTileMatrixSets.ToArray(),
                    });
                }
            }

            return result;
        }

        private static XmlElement CreateOperationElement(XmlDocument doc, OperationProperties[] props, string name)
        {
            var operationElement = doc.CreateElement(OwsPrefix, "Operation", Identifiers.OwsNamespaceUri);
            operationElement.SetAttribute("name", name);

            var DCPElement = doc.CreateElement(OwsPrefix, "DCP", Identifiers.OwsNamespaceUri);
            var HTTPElement = doc.CreateElement(OwsPrefix, "HTTP", Identifiers.OwsNamespaceUri);

            foreach (var prop in props)
            {
                var getElement = doc.CreateElement(OwsPrefix, "Get", Identifiers.OwsNamespaceUri);
                var hrefAttribute = doc.CreateAttribute(XlinkPrefix, "href", Identifiers.XlinkNamespaceUri);
                hrefAttribute.Value = prop.Href;
                getElement.Attributes.Append(hrefAttribute);

                var constraintElement = doc.CreateElement(OwsPrefix, "Constraint", Identifiers.OwsNamespaceUri);
                constraintElement.SetAttribute("name", "GetEncoding");

                var allowedValuesElement = doc.CreateElement(OwsPrefix, "AllowedValues", Identifiers.OwsNamespaceUri);
                var valueElement = doc.CreateElement(OwsPrefix, "Value", Identifiers.OwsNamespaceUri);
                valueElement.InnerText = prop.Encoding;
                allowedValuesElement.AppendChild(valueElement);

                constraintElement.AppendChild(allowedValuesElement);
                getElement.AppendChild(constraintElement);

                HTTPElement.AppendChild(getElement);
            }

            DCPElement.AppendChild(HTTPElement);
            operationElement.AppendChild(DCPElement);

            return operationElement;
        }

        private static XmlElement CreateLayerElement(
            XmlDocument doc,
            string baseUrl,
            M.Layer layer,
            string tileMatrixSetIdentifier)
        {
            var layerElement = doc.CreateElement(String.Empty, Identifiers.LayerElement, WmtsNamespaceUri);

            var titleElement = doc.CreateElement(OwsPrefix, "Title", Identifiers.OwsNamespaceUri);
            titleElement.InnerText = layer.Title ?? String.Empty;
            layerElement.AppendChild(titleElement);

            var abstractElement = doc.CreateElement(OwsPrefix, "Abstract", Identifiers.OwsNamespaceUri);
            abstractElement.InnerText = layer.Abstract ?? String.Empty;
            layerElement.AppendChild(abstractElement);

            var identifierElement = doc.CreateElement(OwsPrefix, "Identifier", Identifiers.OwsNamespaceUri);
            identifierElement.InnerText = layer.Identifier ?? String.Empty;
            layerElement.AppendChild(identifierElement);

            const string StyleNormal = "normal";

            var styleElement = doc.CreateElement(String.Empty, "Style", WmtsNamespaceUri);
            var styleIdentifierElement = doc.CreateElement(OwsPrefix, "Identifier", Identifiers.OwsNamespaceUri);
            styleIdentifierElement.InnerText = StyleNormal;
            styleElement.SetAttribute("isDefault", "true");
            styleElement.AppendChild(styleIdentifierElement);
            layerElement.AppendChild(styleElement);

            var formatElement = doc.CreateElement(String.Empty, "Format", WmtsNamespaceUri);
            formatElement.InnerText = layer.ContentType ?? String.Empty;
            layerElement.AppendChild(formatElement);

            const string LowerCornerElementName = "LowerCorner";
            const string UpperCornerElementName = "UpperCorner";
            var wgs84BoundingBoxElement = doc.CreateElement(OwsPrefix, "WGS84BoundingBox", Identifiers.OwsNamespaceUri);

            static string FormatPoint(M.GeographicalPoint point) =>
                String.Format( // TODO: rounding rule ?
                    CultureInfo.InvariantCulture,
                    "{0:0.000000##########} {1:0.000000##########}",
                    point.Longitude,
                    point.Latitude);

            switch (layer.Srs)
            {
                case Utils.SrsCodes.EPSG3857:
                    {
                        // Default values if source properties unknown
                        var lowerCorner = layer.GeographicalBounds == null ?
                            new M.GeographicalPoint(-180, -85.05112878) :
                            layer.GeographicalBounds.Min;
                        var upperCorner = layer.GeographicalBounds == null ?
                            new M.GeographicalPoint(180, 85.05112878) :
                            layer.GeographicalBounds.Max;

                        var lowerCornerElement = doc.CreateElement(OwsPrefix, LowerCornerElementName, Identifiers.OwsNamespaceUri);
                        lowerCornerElement.InnerText = FormatPoint(lowerCorner);
                        wgs84BoundingBoxElement.AppendChild(lowerCornerElement);

                        var upperCornerElement = doc.CreateElement(OwsPrefix, UpperCornerElementName, Identifiers.OwsNamespaceUri);
                        upperCornerElement.InnerText = FormatPoint(upperCorner);
                        wgs84BoundingBoxElement.AppendChild(upperCornerElement);

                        layerElement.AppendChild(wgs84BoundingBoxElement);
                        break;
                    }
                case Utils.SrsCodes.EPSG4326:
                    {
                        // TODO: custom bounds from source properties
                        var lowerCorner = new M.GeographicalPoint(-180, -90);
                        var upperCorner = new M.GeographicalPoint(180, 90);

                        var lowerCornerElement = doc.CreateElement(OwsPrefix, LowerCornerElementName, Identifiers.OwsNamespaceUri);
                        lowerCornerElement.InnerText = FormatPoint(lowerCorner);
                        wgs84BoundingBoxElement.AppendChild(lowerCornerElement);

                        var upperCornerElement = doc.CreateElement(OwsPrefix, UpperCornerElementName, Identifiers.OwsNamespaceUri);
                        upperCornerElement.InnerText = FormatPoint(upperCorner);
                        wgs84BoundingBoxElement.AppendChild(upperCornerElement);

                        layerElement.AppendChild(wgs84BoundingBoxElement);
                        break;
                    }
                default:
                    {
                        throw new NotImplementedException($"Unknown SRS '{layer.Srs}'");
                    }
            }

            var tileMatrixSetLinkElement = doc.CreateElement(String.Empty, "TileMatrixSetLink", WmtsNamespaceUri);

            var tileMatrixSetElement = doc.CreateElement(String.Empty, "TileMatrixSet", WmtsNamespaceUri);
            tileMatrixSetElement.InnerText = tileMatrixSetIdentifier;

            tileMatrixSetLinkElement.AppendChild(tileMatrixSetElement);
            layerElement.AppendChild(tileMatrixSetLinkElement);

            // https://<wmts-url>/tile/<wmts-version>/<layer>/<style>/<tilematrixset>/<tilematrix>/<tilerow>/<tilecol>.<format>
            var resourceURLElement = doc.CreateElement(String.Empty, Identifiers.ResourceURLElement, WmtsNamespaceUri);
            resourceURLElement.SetAttribute("format", layer.ContentType ?? String.Empty);
            resourceURLElement.SetAttribute("template", $"{baseUrl}/tile/{Identifiers.Version100}/{layer.Identifier}/{{Style}}/{{TileMatrixSet}}/{{TileMatrix}}/{{TileRow}}/{{TileCol}}.{layer.Format}");
            resourceURLElement.SetAttribute("resourceType", "tile");
            layerElement.AppendChild(resourceURLElement);

            return layerElement;
        }

        private static XmlElement CreateTileMatrixSetElement(
            XmlDocument doc,
            M.Layer layer,
            string identifier,
            string supportedCrs,
            string wellKnownScaleSet)
        {
            var tileMatrixSetElement = doc.CreateElement(String.Empty, "TileMatrixSet", WmtsNamespaceUri);

            var identifierElement = doc.CreateElement(OwsPrefix, "Identifier", Identifiers.OwsNamespaceUri);
            identifierElement.InnerText = identifier;
            tileMatrixSetElement.AppendChild(identifierElement);

            var boundingBoxElement = doc.CreateElement(OwsPrefix, "BoundingBox", Identifiers.OwsNamespaceUri);
            boundingBoxElement.SetAttribute("crs", supportedCrs);

            switch (layer.Srs)
            {
                case Utils.SrsCodes.EPSG3857:
                    {
                        var lowerCornerElement = doc.CreateElement(OwsPrefix, Identifiers.LowerCornerElement, Identifiers.OwsNamespaceUri);
                        lowerCornerElement.InnerText = "-20037508.342789 -20037508.342789";
                        boundingBoxElement.AppendChild(lowerCornerElement);

                        var upperCornerElement = doc.CreateElement(OwsPrefix, Identifiers.UpperCornerElement, Identifiers.OwsNamespaceUri);
                        upperCornerElement.InnerText = "20037508.342789 20037508.342789";
                        boundingBoxElement.AppendChild(upperCornerElement);
                        break;
                    }
                case Utils.SrsCodes.EPSG4326:
                    {
                        var lowerCornerElement = doc.CreateElement(OwsPrefix, Identifiers.LowerCornerElement, Identifiers.OwsNamespaceUri);
                        lowerCornerElement.InnerText = "-90.000000 -180.000000";
                        boundingBoxElement.AppendChild(lowerCornerElement);

                        var upperCornerElement = doc.CreateElement(OwsPrefix, Identifiers.UpperCornerElement, Identifiers.OwsNamespaceUri);
                        upperCornerElement.InnerText = "90.000000 180.000000";
                        boundingBoxElement.AppendChild(upperCornerElement);
                        break;
                    }
                default:
                    {
                        throw new NotImplementedException($"Unknown SRS '{layer.Srs}'");
                    }
            }

            tileMatrixSetElement.AppendChild(boundingBoxElement);

            var supportedCRSElement = doc.CreateElement(OwsPrefix, "SupportedCRS", Identifiers.OwsNamespaceUri);
            supportedCRSElement.InnerText = supportedCrs;
            tileMatrixSetElement.AppendChild(supportedCRSElement);

            var wellKnownScaleSetElement = doc.CreateElement(String.Empty, "WellKnownScaleSet", WmtsNamespaceUri);
            wellKnownScaleSetElement.InnerText = wellKnownScaleSet;
            tileMatrixSetElement.AppendChild(wellKnownScaleSetElement);

            for (var zoom = layer.MinZoom; zoom <= layer.MaxZoom; zoom++)
            {
                var tileMatrixElement = doc.CreateElement(String.Empty, "TileMatrix", WmtsNamespaceUri);

                var tileMatrixIdentifierElement = doc.CreateElement(OwsPrefix, "Identifier", Identifiers.OwsNamespaceUri);
                tileMatrixIdentifierElement.InnerText = zoom.ToString(CultureInfo.InvariantCulture);
                tileMatrixElement.AppendChild(tileMatrixIdentifierElement);

                double scaleDenominator;
                int matrixWidth, matrixHeight;
                switch (layer.Srs)
                {
                    case Utils.SrsCodes.EPSG3857:
                        {
                            scaleDenominator = 5.590822640287179E8;
                            matrixWidth = 1 << zoom;
                            matrixHeight = 1 << zoom;

                            var scaleDenominatorElement = doc.CreateElement(String.Empty, "ScaleDenominator", WmtsNamespaceUri);
                            scaleDenominatorElement.InnerText = (scaleDenominator / ((double)(1 << zoom))).ToString(CultureInfo.InvariantCulture);
                            tileMatrixElement.AppendChild(scaleDenominatorElement);

                            var topLeftCornerElement = doc.CreateElement(String.Empty, "TopLeftCorner", WmtsNamespaceUri);
                            topLeftCornerElement.InnerText = "-20037508.342789 20037508.342789"; // TODO: const
                            tileMatrixElement.AppendChild(topLeftCornerElement);
                            break;
                        }
                    case Utils.SrsCodes.EPSG4326:
                        {
                            scaleDenominator = 279541132.01435887813568115234;
                            matrixWidth = 2 * (1 << zoom);
                            matrixHeight = 1 << zoom;

                            var scaleDenominatorElement = doc.CreateElement(String.Empty, "ScaleDenominator", WmtsNamespaceUri);
                            scaleDenominatorElement.InnerText = (scaleDenominator / ((double)(1 << zoom))).ToString(CultureInfo.InvariantCulture);
                            tileMatrixElement.AppendChild(scaleDenominatorElement);

                            var topLeftCornerElement = doc.CreateElement(String.Empty, "TopLeftCorner", WmtsNamespaceUri);
                            topLeftCornerElement.InnerText = "90.000000 -180.000000"; // TODO: const
                            tileMatrixElement.AppendChild(topLeftCornerElement);
                            break;
                        }
                    default:
                        {
                            throw new NotImplementedException($"Unknown SRS '{layer.Srs}'");
                        }
                }

                var tileWidthElement = doc.CreateElement(String.Empty, Identifiers.TileWidthElement, WmtsNamespaceUri);
                tileWidthElement.InnerText = layer.TileWidth.ToString(CultureInfo.InvariantCulture);
                tileMatrixElement.AppendChild(tileWidthElement);

                var tileHeightElement = doc.CreateElement(String.Empty, Identifiers.TileHeightElement, WmtsNamespaceUri);
                tileHeightElement.InnerText = layer.TileHeight.ToString(CultureInfo.InvariantCulture);
                tileMatrixElement.AppendChild(tileHeightElement);

                var matrixWidthElement = doc.CreateElement(String.Empty, "MatrixWidth", WmtsNamespaceUri);
                matrixWidthElement.InnerText = matrixWidth.ToString(CultureInfo.InvariantCulture);
                tileMatrixElement.AppendChild(matrixWidthElement);

                var matrixHeightElement = doc.CreateElement(String.Empty, "MatrixHeight", WmtsNamespaceUri);
                matrixHeightElement.InnerText = matrixHeight.ToString(CultureInfo.InvariantCulture);
                tileMatrixElement.AppendChild(matrixHeightElement);

                tileMatrixSetElement.AppendChild(tileMatrixElement);
            }

            return tileMatrixSetElement;
        }

        class OperationProperties
        {
            public string Href { get; set; } = String.Empty;

            public string Encoding { get; set; } = String.Empty;
        }
    }
}
