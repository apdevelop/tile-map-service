using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace TileMapService.Wmts
{
    /// <summary>
    /// WMTS capabilities document builder.
    /// Supports only Web Mercator (EPSG:3857) / "Google Maps Compatible" 256x256 tile sets.
    /// </summary>
    class CapabilitiesDocumentBuilder
    {
        private readonly string baseUrl;

        private readonly List<Models.Layer> layers;

        private const int TileWidth = 256; // TODO: other resolutions

        private const int TileHeight = 256;

        #region Constants

        private const string WmtsNamespaceUri = "http://www.opengis.net/wmts/1.0";

        private const string OwsPrefix = "ows";

        private const string OwsNamespaceUri = "http://www.opengis.net/ows/1.1";

        private const string XlinkPrefix = "xlink";

        private const string XlinkNamespaceUri = "http://www.w3.org/1999/xlink";

        private const string Version100 = "1.0.0";

        #endregion

        // TODO: DTO classes for WMTS capabilities description (like Layer)

        public CapabilitiesDocumentBuilder(string baseUrl, IEnumerable<Models.Layer> layers)
        {
            this.baseUrl = baseUrl;
            this.layers = layers.ToList();
        }

        public XmlDocument GetCapabilities()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement(String.Empty, "Capabilities", WmtsNamespaceUri);
            root.SetAttribute("xmlns:" + OwsPrefix, OwsNamespaceUri);
            root.SetAttribute("xmlns:" + XlinkPrefix, XlinkNamespaceUri);
            root.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            root.SetAttribute("xmlns:gml", "http://www.opengis.net/gml");
            root.SetAttribute("version", Version100);
            doc.AppendChild(root);

            var serviceIdentificationElement = doc.CreateElement(OwsPrefix, "ServiceIdentification", OwsNamespaceUri);

            var titleElement = doc.CreateElement(OwsPrefix, "Title", OwsNamespaceUri);
            titleElement.InnerText = "WMTS Service";
            serviceIdentificationElement.AppendChild(titleElement);

            var serviceTypeElement = doc.CreateElement(OwsPrefix, "ServiceType", OwsNamespaceUri);
            serviceTypeElement.InnerText = "OGC WMTS";
            serviceIdentificationElement.AppendChild(serviceTypeElement);

            var serviceTypeVersionElement = doc.CreateElement(OwsPrefix, "ServiceTypeVersion", OwsNamespaceUri);
            serviceTypeVersionElement.InnerText = Version100;
            serviceIdentificationElement.AppendChild(serviceTypeVersionElement);

            root.AppendChild(serviceIdentificationElement);

            var serviceProviderElement = doc.CreateElement(OwsPrefix, "ServiceProvider", OwsNamespaceUri);
            var serviceContactElement = doc.CreateElement(OwsPrefix, "ServiceContact", OwsNamespaceUri);
            var contactInfoElement = doc.CreateElement(OwsPrefix, "ContactInfo", OwsNamespaceUri);
            contactInfoElement.InnerText = String.Empty;
            serviceContactElement.AppendChild(contactInfoElement);
            serviceProviderElement.AppendChild(serviceContactElement);

            root.AppendChild(serviceProviderElement);

            var operationsMetadataElement = doc.CreateElement(OwsPrefix, "OperationsMetadata", OwsNamespaceUri);
            operationsMetadataElement.AppendChild(CreateOperationElement(doc, this.baseUrl, "GetCapabilities"));
            operationsMetadataElement.AppendChild(CreateOperationElement(doc, this.baseUrl, "GetTile"));
            // TODO: GetFeatureInfo
            root.AppendChild(operationsMetadataElement);

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

            root.AppendChild(contentsElement);

            return doc;
        }

        private static XmlElement CreateOperationElement(XmlDocument doc, string baseUrl, string name)
        {
            var operationElement = doc.CreateElement(OwsPrefix, "Operation", OwsNamespaceUri);
            operationElement.SetAttribute("name", name);

            var DCP = doc.CreateElement(OwsPrefix, "DCP", OwsNamespaceUri);
            var HTTP = doc.CreateElement(OwsPrefix, "HTTP", OwsNamespaceUri);

            var getElement = doc.CreateElement(OwsPrefix, "Get", OwsNamespaceUri);
            var hrefAttribute = doc.CreateAttribute(XlinkPrefix, "href", XlinkNamespaceUri);
            hrefAttribute.Value = baseUrl + "?";
            getElement.Attributes.Append(hrefAttribute);

            var constraintElement = doc.CreateElement(OwsPrefix, "Constraint", OwsNamespaceUri);
            constraintElement.SetAttribute("name", "GetEncoding");

            var allowedValuesElement = doc.CreateElement(OwsPrefix, "AllowedValues", OwsNamespaceUri);

            var valueElement = doc.CreateElement(OwsPrefix, "Value", OwsNamespaceUri);
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

            var titleElement = doc.CreateElement(OwsPrefix, "Title", OwsNamespaceUri);
            titleElement.InnerText = layer.Title;
            layerElement.AppendChild(titleElement);

            // TODO: Abstract element

            var identifierElement = doc.CreateElement(OwsPrefix, "Identifier", OwsNamespaceUri);
            identifierElement.InnerText = layer.Identifier;
            layerElement.AppendChild(identifierElement);

            var styleElement = doc.CreateElement(String.Empty, "Style", WmtsNamespaceUri);
            var styleIdentifierElement = doc.CreateElement(OwsPrefix, "Identifier", OwsNamespaceUri);
            styleIdentifierElement.InnerText = "normal";
            styleElement.SetAttribute("isDefault", "true");
            styleElement.AppendChild(styleIdentifierElement);
            layerElement.AppendChild(styleElement);

            var formatElement = doc.CreateElement(String.Empty, "Format", WmtsNamespaceUri);
            formatElement.InnerText = layer.ContentType;
            layerElement.AppendChild(formatElement);

            var wgs84BoundingBoxElement = doc.CreateElement(OwsPrefix, "WGS84BoundingBox", OwsNamespaceUri);

            switch (layer.Srs)
            {
                case Utils.SrsCodes.EPSG3857:
                    {
                        var lowerCornerElement = doc.CreateElement(OwsPrefix, "LowerCorner", OwsNamespaceUri);
                        lowerCornerElement.InnerText = "-180.000000 -85.05112878";
                        wgs84BoundingBoxElement.AppendChild(lowerCornerElement);

                        var upperCornerElement = doc.CreateElement(OwsPrefix, "UpperCorner", OwsNamespaceUri);
                        upperCornerElement.InnerText = "180.000000 85.05112878";
                        wgs84BoundingBoxElement.AppendChild(upperCornerElement);

                        layerElement.AppendChild(wgs84BoundingBoxElement);
                        break;
                    }
                case Utils.SrsCodes.EPSG4326:
                    {
                        var lowerCornerElement = doc.CreateElement(OwsPrefix, "LowerCorner", OwsNamespaceUri);
                        lowerCornerElement.InnerText = "-180.000000 -90.000000";
                        wgs84BoundingBoxElement.AppendChild(lowerCornerElement);

                        var upperCornerElement = doc.CreateElement(OwsPrefix, "UpperCorner", OwsNamespaceUri);
                        upperCornerElement.InnerText = "180.000000 90.000000";
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

            var identifierElement = doc.CreateElement(OwsPrefix, "Identifier", OwsNamespaceUri);
            identifierElement.InnerText = identifier;
            tileMatrixSetElement.AppendChild(identifierElement);

            var boundingBoxElement = doc.CreateElement(OwsPrefix, "BoundingBox", OwsNamespaceUri);
            boundingBoxElement.SetAttribute("crs", supportedCrs);

            switch (layer.Srs)
            {
                case Utils.SrsCodes.EPSG3857:
                    {
                        var lowerCornerElement = doc.CreateElement(OwsPrefix, "LowerCorner", OwsNamespaceUri);
                        lowerCornerElement.InnerText = "-20037508.342789 -20037508.342789";
                        boundingBoxElement.AppendChild(lowerCornerElement);

                        var upperCornerElement = doc.CreateElement(OwsPrefix, "UpperCorner", OwsNamespaceUri);
                        upperCornerElement.InnerText = "20037508.342789 20037508.342789";
                        boundingBoxElement.AppendChild(upperCornerElement);
                        break;
                    }
                case Utils.SrsCodes.EPSG4326:
                    {
                        var lowerCornerElement = doc.CreateElement(OwsPrefix, "LowerCorner", OwsNamespaceUri);
                        lowerCornerElement.InnerText = "-90.000000 -180.000000";
                        boundingBoxElement.AppendChild(lowerCornerElement);

                        var upperCornerElement = doc.CreateElement(OwsPrefix, "UpperCorner", OwsNamespaceUri);
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

            var supportedCRSElement = doc.CreateElement(OwsPrefix, "SupportedCRS", OwsNamespaceUri);
            supportedCRSElement.InnerText = supportedCrs;
            tileMatrixSetElement.AppendChild(supportedCRSElement);

            var wellKnownScaleSetElement = doc.CreateElement(String.Empty, "WellKnownScaleSet", WmtsNamespaceUri);
            wellKnownScaleSetElement.InnerText = wellKnownScaleSet;
            tileMatrixSetElement.AppendChild(wellKnownScaleSetElement);

            for (var zoom = layer.MinZoom; zoom <= layer.MaxZoom; zoom++)
            {
                var tileMatrixElement = doc.CreateElement(String.Empty, "TileMatrix", WmtsNamespaceUri);

                var tileMatrixIdentifierElement = doc.CreateElement(OwsPrefix, "Identifier", OwsNamespaceUri);
                tileMatrixIdentifierElement.InnerText = zoom.ToString(CultureInfo.InvariantCulture);
                tileMatrixElement.AppendChild(tileMatrixIdentifierElement);

                double scaleDenominator;
                int matrixWidth, matrixHeight;
                switch (layer.Srs)
                {
                    case Utils.SrsCodes.EPSG3857:
                        {
                            scaleDenominator = 5.590822640287179E8;
                            matrixWidth = (1 << zoom);
                            matrixHeight = (1 << zoom);

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
                            matrixHeight = (1 << zoom);

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

                var tileWidthElement = doc.CreateElement(String.Empty, "TileWidth", WmtsNamespaceUri);
                tileWidthElement.InnerText = TileWidth.ToString(CultureInfo.InvariantCulture);
                tileMatrixElement.AppendChild(tileWidthElement);

                var tileHeightElement = doc.CreateElement(String.Empty, "TileHeight", WmtsNamespaceUri);
                tileHeightElement.InnerText = TileHeight.ToString(CultureInfo.InvariantCulture);
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
