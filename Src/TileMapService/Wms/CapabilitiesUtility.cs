using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace TileMapService.Wms
{
    class CapabilitiesUtility
    {
        private readonly string baseUrl;

        private XmlDocument? doc;

        public CapabilitiesUtility(string baseUrl)
        {
            this.baseUrl = baseUrl;
        }

        public XmlDocument CreateCapabilitiesDocument(
            Version version,
            Service service,
            IList<Layer> layers,
            IList<string> getMapFormats)
        {
            // TODO: EPSG:4326 support

            var rootNodeName = GetRootNodeName(version);

            doc = new XmlDocument();
            var rootElement = doc.CreateElement(String.Empty, rootNodeName, String.Empty);
            doc.AppendChild(rootElement);

            var versionAttribute = doc.CreateAttribute("version");
            switch (version)
            {
                case Version.Version111: { versionAttribute.Value = Identifiers.Version111; break; }
                case Version.Version130: { versionAttribute.Value = Identifiers.Version130; break; }
                default: throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
            }

            rootElement.Attributes.Append(versionAttribute);

            var serviceElement = doc.CreateElement(Identifiers.ServiceElement);
            rootElement.AppendChild(serviceElement);

            var serviceName = doc.CreateElement("Name");
            serviceName.InnerText = "OGC:WMS";
            serviceElement.AppendChild(serviceName);

            var serviceTitle = doc.CreateElement("Title");
            serviceTitle.InnerText = service.Title ?? String.Empty;
            serviceElement.AppendChild(serviceTitle);

            var serviceAbstract = doc.CreateElement("Abstract");
            serviceAbstract.InnerText = service.Abstract ?? String.Empty;
            serviceElement.AppendChild(serviceAbstract);

            var serviceOnlineResource = CreateOnlineResourceElement(this.baseUrl);
            serviceElement.AppendChild(serviceOnlineResource);

            var capability = doc.CreateElement(Identifiers.CapabilityElement);
            rootElement.AppendChild(capability);

            string capabilitiesFormat;
            switch (version)
            {
                case Version.Version111: { capabilitiesFormat = MediaTypeNames.Application.OgcWmsCapabilitiesXml; break; }
                case Version.Version130: { capabilitiesFormat = MediaTypeNames.Text.Xml; break; }
                default: throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
            }

            var capabilityRequest = doc.CreateElement("Request");
            capabilityRequest.AppendChild(CreateRequestElement(Identifiers.GetCapabilities, new[] { capabilitiesFormat }));
            capabilityRequest.AppendChild(CreateRequestElement(Identifiers.GetMap, getMapFormats));
            // TODO: ? capabilityRequest.AppendChild(CreateRequestElement(Identifiers.GetFeatureInfo, getFeatureInfoFormats));
            capability.AppendChild(capabilityRequest);

            var capabilityException = doc.CreateElement("Exception");
            var capabilityExceptionFormat = doc.CreateElement("Format");

            switch (version)
            {
                case Version.Version111: { capabilityExceptionFormat.InnerText = MediaTypeNames.Application.OgcServiceExceptionXml; break; }
                case Version.Version130: { capabilityExceptionFormat.InnerText = "XML"; break; }
                default: throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
            }

            capabilityException.AppendChild(capabilityExceptionFormat);
            capability.AppendChild(capabilityException);

            foreach (var layer in layers)
            {
                capability.AppendChild(CreateLayerElement(version, layer));
            }

            return doc;
        }

        // TODO: uniform API for build / parse Capabilities XML document

        /// <summary>
        /// Extracts list of Layers from input Capabilities XML document.
        /// </summary>
        /// <param name="xmlDoc">Capabilities XML document.</param>
        /// <returns>List of Layers (flatten, without hierarchy).</returns>
        public static List<Layer> GetLayers(XmlDocument xmlDoc)
        {
            var version = GetVersion(xmlDoc);

            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            string xpath, defaultNsPrefix;
            string? bboxElementName;
            switch (version)
            {
                case Version.Version111:
                    {
                        bboxElementName = "LatLonBoundingBox";
                        defaultNsPrefix = String.Empty;
                        xpath = @$"/{Identifiers.WMT_MS_CapabilitiesElement}//Capability//{Identifiers.LayerElement}[not(descendant::*[local-name() = '{Identifiers.LayerElement}'])]";
                        break;
                    }
                case Version.Version130:
                    {
                        // TODO: ! support for 1.3.0 bboxElementName = "EX_GeographicBoundingBox";
                        bboxElementName = null;
                        defaultNsPrefix = "ns:";
                        nsManager.AddNamespace("ns", "http://www.opengis.net/wms");
                        xpath = $@"/ns:{Identifiers.WMS_CapabilitiesElement}//ns:Capability//ns:{Identifiers.LayerElement}[not(descendant::*[local-name() = '{Identifiers.LayerElement}'])]";
                        break;
                    }
                default: throw new InvalidOperationException($"WMS version '{version}' is not supported.");
            }

            var result = new List<Layer>();

            var layers = xmlDoc.SelectNodes(xpath, nsManager);
            if (layers != null)
            {
                foreach (XmlNode layer in layers)
                {
                    var layerName = layer.SelectSingleNode(defaultNsPrefix + "Name", nsManager);
                    var layerTitle = layer.SelectSingleNode(defaultNsPrefix + "Title", nsManager);
                    var layerQueryable = layer.Attributes?[Identifiers.QueryableAttribute];

                    XmlAttribute? minxAttribute = null, minyAttribute = null, maxxAttribute = null, maxyAttribute = null;
                    var bbox = bboxElementName != null ? layer.SelectSingleNode(defaultNsPrefix + bboxElementName, nsManager) : null;
                    if (bbox != null && bbox.Attributes != null)
                    {
                        minxAttribute = bbox.Attributes["minx"];
                        minyAttribute = bbox.Attributes["miny"];
                        maxxAttribute = bbox.Attributes["maxx"];
                        maxyAttribute = bbox.Attributes["maxy"];
                    }

                    result.Add(new Layer
                    {
                        Name = layerName != null ? layerName.InnerText : String.Empty,
                        Title = layerTitle != null ? layerTitle.InnerText : String.Empty,
                        IsQueryable = layerQueryable != null && layerQueryable.Value == "1",
                        GeographicalBounds = bbox != null && bbox.Attributes != null ?
                            new Models.GeographicalBounds(
                                minxAttribute != null ? Double.Parse(minxAttribute.Value, CultureInfo.InvariantCulture) : 0.0,
                                minyAttribute != null ? Double.Parse(minyAttribute.Value, CultureInfo.InvariantCulture) : 0.0,
                                maxxAttribute != null ? Double.Parse(maxxAttribute.Value, CultureInfo.InvariantCulture) : 0.0,
                                maxyAttribute != null ? Double.Parse(maxyAttribute.Value, CultureInfo.InvariantCulture) : 0.0) :
                            null,
                    });
                }
            }

            return result;
        }

        private static Version GetVersion(XmlDocument xmlDoc)
        {
            var rootElement = xmlDoc.DocumentElement;
            if (rootElement == null)
            {
                throw new FormatException("Root Element was not found");
            }

            var versionAttribute = rootElement.Attributes["version"];
            if (versionAttribute == null)
            {
                throw new FormatException("Version attribute was not found");
            }

            switch (versionAttribute.Value)
            {
                case Identifiers.Version111:
                    {
                        if (rootElement.Name == Identifiers.WMT_MS_CapabilitiesElement)
                        {
                            return Version.Version111;
                        }
                        else
                        {
                            throw new FormatException();
                        }
                    }
                case Identifiers.Version130:
                    {
                        if (rootElement.Name == Identifiers.WMS_CapabilitiesElement)
                        {
                            return Version.Version130;
                        }
                        else
                        {
                            throw new FormatException();
                        }
                    }
                default: throw new FormatException("Version attribute and root node name mismatch");
            }
        }

        private XmlElement CreateRequestElement(string name, IEnumerable<string> formats)
        {
            if (this.doc == null)
            {
                throw new InvalidOperationException();
            }

            var request = doc.CreateElement(name);

            foreach (var format in formats)
            {
                var requestFormat = doc.CreateElement("Format");
                requestFormat.InnerText = format;
                request.AppendChild(requestFormat);
            }

            var requestDCPType = doc.CreateElement("DCPType");
            request.AppendChild(requestDCPType);

            var requestDCPTypeHTTP = doc.CreateElement("HTTP");
            requestDCPType.AppendChild(requestDCPTypeHTTP);

            var requestDCPTypeHTTPGet = doc.CreateElement("Get");
            requestDCPTypeHTTP.AppendChild(requestDCPTypeHTTPGet);

            var serviceOnlineResource = CreateOnlineResourceElement(this.baseUrl);
            requestDCPTypeHTTPGet.AppendChild(serviceOnlineResource);

            return request;
        }

        private XmlElement CreateOnlineResourceElement(string href)
        {
            if (this.doc == null)
            {
                throw new InvalidOperationException();
            }

            var serviceOnlineResource = doc.CreateElement("OnlineResource");

            var hrefAttribute = doc.CreateAttribute("xlink", "href", XlinkNamespaceUri);
            hrefAttribute.Value = href;
            serviceOnlineResource.Attributes.Append(hrefAttribute);

            var typeAttribute = doc.CreateAttribute("xlink", "type", XlinkNamespaceUri);
            typeAttribute.Value = "simple";
            serviceOnlineResource.Attributes.Append(typeAttribute);

            return serviceOnlineResource;
        }

        private XmlElement CreateLayerElement(Version version, Layer layer)
        {
            if (this.doc == null)
            {
                throw new InvalidOperationException();
            }

            var layerElement = doc.CreateElement(Identifiers.LayerElement);

            var queryableAttribute = doc.CreateAttribute(Identifiers.QueryableAttribute);
            queryableAttribute.Value = layer.IsQueryable ? "1" : "0";
            layerElement.Attributes.Append(queryableAttribute);

            var layerTitle = doc.CreateElement(Identifiers.TitleElement);
            layerTitle.InnerText = layer.Title ?? String.Empty;
            layerElement.AppendChild(layerTitle);

            var layerName = doc.CreateElement(Identifiers.NameElement);
            layerName.InnerText = layer.Name ?? String.Empty;
            layerElement.AppendChild(layerName);

            var layerAbstract = doc.CreateElement(Identifiers.AbstractElement);
            layerAbstract.InnerText = layer.Abstract ?? String.Empty;
            layerElement.AppendChild(layerAbstract);

            string layerSrsElementName;
            switch (version)
            {
                case Version.Version111: { layerSrsElementName = Identifiers.Srs; break; }
                case Version.Version130: { layerSrsElementName = Identifiers.Crs; break; }
                default: throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
            }

            var layerSrs = doc.CreateElement(layerSrsElementName);
            layerSrs.InnerText = Identifiers.EPSG3857; // TODO: EPSG:4326 support
            layerElement.AppendChild(layerSrs);

            var geoBounds = layer.GeographicalBounds ?? new Models.GeographicalBounds(-180, -90, 180, 90);

            switch (version)
            {
                case Version.Version111:
                    {
                        var latlonBoundingBox = doc.CreateElement("LatLonBoundingBox");

                        var minxAttribute = doc.CreateAttribute("minx");
                        minxAttribute.Value = geoBounds.MinLongitude.ToString(CultureInfo.InvariantCulture);
                        latlonBoundingBox.Attributes.Append(minxAttribute);

                        var minyAttribute = doc.CreateAttribute("miny");
                        minyAttribute.Value = geoBounds.MinLatitude.ToString(CultureInfo.InvariantCulture);
                        latlonBoundingBox.Attributes.Append(minyAttribute);

                        var maxxAttribute = doc.CreateAttribute("maxx");
                        maxxAttribute.Value = geoBounds.MaxLongitude.ToString(CultureInfo.InvariantCulture);
                        latlonBoundingBox.Attributes.Append(maxxAttribute);

                        var maxyAttribute = doc.CreateAttribute("maxy");
                        maxyAttribute.Value = geoBounds.MaxLatitude.ToString(CultureInfo.InvariantCulture);
                        latlonBoundingBox.Attributes.Append(maxyAttribute);

                        layerElement.AppendChild(latlonBoundingBox);
                        break;
                    }
                case Version.Version130:
                    {
                        var geographicBoundingBox = doc.CreateElement("EX_GeographicBoundingBox");

                        var westBoundLongitude = doc.CreateElement("westBoundLongitude");
                        westBoundLongitude.InnerText = geoBounds.MinLongitude.ToString(CultureInfo.InvariantCulture);
                        geographicBoundingBox.AppendChild(westBoundLongitude);

                        var eastBoundLongitude = doc.CreateElement("eastBoundLongitude");
                        eastBoundLongitude.InnerText = geoBounds.MaxLongitude.ToString(CultureInfo.InvariantCulture);
                        geographicBoundingBox.AppendChild(eastBoundLongitude);

                        var southBoundLatitude = doc.CreateElement("southBoundLatitude");
                        southBoundLatitude.InnerText = geoBounds.MinLatitude.ToString(CultureInfo.InvariantCulture);
                        geographicBoundingBox.AppendChild(southBoundLatitude);

                        var northBoundLatitude = doc.CreateElement("northBoundLatitude");
                        northBoundLatitude.InnerText = geoBounds.MaxLatitude.ToString(CultureInfo.InvariantCulture);
                        geographicBoundingBox.AppendChild(northBoundLatitude);

                        layerElement.AppendChild(geographicBoundingBox);
                        break;
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
                    }
            }

            {
                var boundingBoxElement = doc.CreateElement("BoundingBox");

                string boundingBoxSrsAttributeName;
                switch (version)
                {
                    case Version.Version111: { boundingBoxSrsAttributeName = Identifiers.Srs; break; }
                    case Version.Version130: { boundingBoxSrsAttributeName = Identifiers.Crs; break; }
                    default: throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
                }

                var boundingBoxSrsAttribute = doc.CreateAttribute(boundingBoxSrsAttributeName);
                boundingBoxSrsAttribute.Value = Identifiers.EPSG3857; // TODO: other CRS
                boundingBoxElement.Attributes.Append(boundingBoxSrsAttribute);

                var minxAttribute = doc.CreateAttribute("minx");
                minxAttribute.Value = Utils.WebMercator.X(geoBounds.MinLongitude).ToString("E16", CultureInfo.InvariantCulture);
                boundingBoxElement.Attributes.Append(minxAttribute);

                var minyAttribute = doc.CreateAttribute("miny");
                minyAttribute.Value = Utils.WebMercator.Y(geoBounds.MinLatitude).ToString("E16", CultureInfo.InvariantCulture);
                boundingBoxElement.Attributes.Append(minyAttribute);

                var maxxAttribute = doc.CreateAttribute("maxx");
                maxxAttribute.Value = Utils.WebMercator.X(geoBounds.MaxLongitude).ToString("E16", CultureInfo.InvariantCulture);
                boundingBoxElement.Attributes.Append(maxxAttribute);

                var maxyAttribute = doc.CreateAttribute("maxy");
                maxyAttribute.Value = Utils.WebMercator.Y(geoBounds.MaxLatitude).ToString("E16", CultureInfo.InvariantCulture);
                boundingBoxElement.Attributes.Append(maxyAttribute);

                layerElement.AppendChild(boundingBoxElement);
            }

            return layerElement;
        }

        private static string GetRootNodeName(Version version)
        {
            return version switch
            {
                Version.Version111 => Identifiers.WMT_MS_CapabilitiesElement,
                Version.Version130 => Identifiers.WMS_CapabilitiesElement,
                _ => throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported."),
            };
        }

        private const string XlinkNamespaceUri = "http://www.w3.org/1999/xlink";
    }
}
