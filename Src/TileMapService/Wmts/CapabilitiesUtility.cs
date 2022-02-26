using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace TileMapService.Wmts
{
    /// <summary>
    /// WMTS capabilities document builder.
    /// Currently supports only Web Mercator (EPSG:3857) / "Google Maps Compatible" 256x256 tile sets.
    /// </summary>
    class CapabilitiesUtility
    {
        private readonly string baseUrl;

        private readonly Models.Layer[] layers;

        #region Constants

        private const string WmtsNamespaceUri = "http://www.opengis.net/wmts/1.0";

        private const string OwsPrefix = "ows";

        private const string XlinkPrefix = "xlink";

        private const string XlinkNamespaceUri = "http://www.w3.org/1999/xlink";

        #endregion

        // TODO: DTO classes for WMTS capabilities description (like Layer, Capabilities)

        public CapabilitiesUtility(
            string baseUrl,
            IEnumerable<Models.Layer> layers)
        {
            this.baseUrl = baseUrl;
            this.layers = layers.ToArray();
        }

        public XmlDocument GetCapabilities()
        {
            var doc = new XmlDocument();
            var rootElement = doc.CreateElement(String.Empty, "Capabilities", WmtsNamespaceUri);
            rootElement.SetAttribute("xmlns:" + OwsPrefix, Identifiers.OwsNamespaceUri);
            rootElement.SetAttribute("xmlns:" + XlinkPrefix, XlinkNamespaceUri);
            rootElement.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            rootElement.SetAttribute("xmlns:gml", "http://www.opengis.net/gml");
            rootElement.SetAttribute("version", Identifiers.Version100);
            doc.AppendChild(rootElement);

            var serviceIdentificationElement = doc.CreateElement(OwsPrefix, "ServiceIdentification", Identifiers.OwsNamespaceUri);

            var titleElement = doc.CreateElement(OwsPrefix, "Title", Identifiers.OwsNamespaceUri);
            titleElement.InnerText = "WMTS Service";
            serviceIdentificationElement.AppendChild(titleElement);

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

            var operationsMetadataElement = doc.CreateElement(OwsPrefix, "OperationsMetadata", Identifiers.OwsNamespaceUri);
            operationsMetadataElement.AppendChild(CreateOperationElement(doc, this.baseUrl, "GetCapabilities"));
            operationsMetadataElement.AppendChild(CreateOperationElement(doc, this.baseUrl, "GetTile"));
            // TODO: GetFeatureInfo
            rootElement.AppendChild(operationsMetadataElement);

            var contentsElement = doc.CreateElement(String.Empty, "Contents", WmtsNamespaceUri);

            // TODO: EPSG:4326 support
            var identifiers = new HashSet<string>();
            foreach (var layer in this.layers)
            {
                switch (layer.Srs)
                {
                    case Utils.SrsCodes.EPSG3857:
                        {
                            var identifier = String.Format(CultureInfo.InvariantCulture, "google3857_{0}-{1}", layer.MinZoom, layer.MaxZoom);
                            contentsElement.AppendChild(CreateLayerElement(doc, layer, identifier));

                            if (!identifiers.Contains(identifier))
                            {
                                identifiers.Add(identifier);
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
                            contentsElement.AppendChild(CreateLayerElement(doc, layer, identifier));

                            if (!identifiers.Contains(identifier))
                            {
                                identifiers.Add(identifier);
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
                            throw new NotImplementedException($"Unknown SRS '{layer.Srs}'");
                        }
                }
            }

            rootElement.AppendChild(contentsElement);

            return doc;
        }

        private static XmlElement CreateOperationElement(XmlDocument doc, string baseUrl, string name)
        {
            var operationElement = doc.CreateElement(OwsPrefix, "Operation", Identifiers.OwsNamespaceUri);
            operationElement.SetAttribute("name", name);

            var DCP = doc.CreateElement(OwsPrefix, "DCP", Identifiers.OwsNamespaceUri);
            var HTTP = doc.CreateElement(OwsPrefix, "HTTP", Identifiers.OwsNamespaceUri);

            var getElement = doc.CreateElement(OwsPrefix, "Get", Identifiers.OwsNamespaceUri);
            var hrefAttribute = doc.CreateAttribute(XlinkPrefix, "href", XlinkNamespaceUri);
            hrefAttribute.Value = baseUrl + "?";
            getElement.Attributes.Append(hrefAttribute);

            var constraintElement = doc.CreateElement(OwsPrefix, "Constraint", Identifiers.OwsNamespaceUri);
            constraintElement.SetAttribute("name", "GetEncoding");

            var allowedValuesElement = doc.CreateElement(OwsPrefix, "AllowedValues", Identifiers.OwsNamespaceUri);

            var valueElement = doc.CreateElement(OwsPrefix, "Value", Identifiers.OwsNamespaceUri);
            valueElement.InnerText = "KVP";

            allowedValuesElement.AppendChild(valueElement);
            constraintElement.AppendChild(allowedValuesElement);
            getElement.AppendChild(constraintElement);
            HTTP.AppendChild(getElement);
            DCP.AppendChild(HTTP);
            operationElement.AppendChild(DCP);

            return operationElement;
        }

        private static XmlElement CreateLayerElement(
            XmlDocument doc,
            Models.Layer layer,
            string tileMatrixSetIdentifier)
        {
            var layerElement = doc.CreateElement(String.Empty, "Layer", WmtsNamespaceUri);

            var titleElement = doc.CreateElement(OwsPrefix, "Title", Identifiers.OwsNamespaceUri);
            titleElement.InnerText = layer.Title ?? String.Empty;
            layerElement.AppendChild(titleElement);

            // TODO: Abstract element

            var identifierElement = doc.CreateElement(OwsPrefix, "Identifier", Identifiers.OwsNamespaceUri);
            identifierElement.InnerText = layer.Identifier ?? String.Empty;
            layerElement.AppendChild(identifierElement);

            var styleElement = doc.CreateElement(String.Empty, "Style", WmtsNamespaceUri);
            var styleIdentifierElement = doc.CreateElement(OwsPrefix, "Identifier", Identifiers.OwsNamespaceUri);
            styleIdentifierElement.InnerText = "normal";
            styleElement.SetAttribute("isDefault", "true");
            styleElement.AppendChild(styleIdentifierElement);
            layerElement.AppendChild(styleElement);

            var formatElement = doc.CreateElement(String.Empty, "Format", WmtsNamespaceUri);
            formatElement.InnerText = layer.ContentType ?? String.Empty;
            layerElement.AppendChild(formatElement);

            const string LowerCornerElementName = "LowerCorner";
            const string UpperCornerElementName = "UpperCorner";
            var wgs84BoundingBoxElement = doc.CreateElement(OwsPrefix, "WGS84BoundingBox", Identifiers.OwsNamespaceUri);

            static string FormatPoint(Models.GeographicalPoint point)
            {
                return String.Format( // TODO: rounding in other implementations ?
                    CultureInfo.InvariantCulture,
                    "{0:0.000000##########} {1:0.000000##########}",
                    point.Longitude,
                    point.Latitude);
            }

            switch (layer.Srs)
            {
                case Utils.SrsCodes.EPSG3857:
                    {
                        // Default values if source properties unknown
                        var lowerCorner = layer.GeographicalBounds == null ?
                            new Models.GeographicalPoint(-180, -85.05112878) :
                            layer.GeographicalBounds.Min;
                        var upperCorner = layer.GeographicalBounds == null ?
                            new Models.GeographicalPoint(180, 85.05112878) :
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
                        var lowerCorner = new Models.GeographicalPoint(-180, -90);
                        var upperCorner = new Models.GeographicalPoint(180, 90);

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

            return layerElement;
        }

        private static XmlElement CreateTileMatrixSetElement(
            XmlDocument doc,
            Models.Layer layer,
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
                        var lowerCornerElement = doc.CreateElement(OwsPrefix, "LowerCorner", Identifiers.OwsNamespaceUri);
                        lowerCornerElement.InnerText = "-20037508.342789 -20037508.342789";
                        boundingBoxElement.AppendChild(lowerCornerElement);

                        var upperCornerElement = doc.CreateElement(OwsPrefix, "UpperCorner", Identifiers.OwsNamespaceUri);
                        upperCornerElement.InnerText = "20037508.342789 20037508.342789";
                        boundingBoxElement.AppendChild(upperCornerElement);
                        break;
                    }
                case Utils.SrsCodes.EPSG4326:
                    {
                        var lowerCornerElement = doc.CreateElement(OwsPrefix, "LowerCorner", Identifiers.OwsNamespaceUri);
                        lowerCornerElement.InnerText = "-90.000000 -180.000000";
                        boundingBoxElement.AppendChild(lowerCornerElement);

                        var upperCornerElement = doc.CreateElement(OwsPrefix, "UpperCorner", Identifiers.OwsNamespaceUri);
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
    }
}
