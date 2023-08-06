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
            ServiceProperties service,
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

            var serviceNameElement = doc.CreateElement("Name");
            serviceNameElement.InnerText = "OGC:WMS";
            serviceElement.AppendChild(serviceNameElement);

            var serviceTitleElement = doc.CreateElement("Title");
            serviceTitleElement.InnerText = service.Title ?? String.Empty;
            serviceElement.AppendChild(serviceTitleElement);

            var serviceAbstractElement = doc.CreateElement("Abstract");
            serviceAbstractElement.InnerText = service.Abstract ?? String.Empty;
            serviceElement.AppendChild(serviceAbstractElement);

            var serviceKeywordListElement = doc.CreateElement("KeywordList");
            if (service.Keywords != null)
            {
                foreach (var keyword in service.Keywords)
                {
                    if (!String.IsNullOrWhiteSpace(keyword))
                    {
                        var serviceKeywordElement = doc.CreateElement("Keyword");
                        serviceKeywordElement.InnerText = keyword;
                        serviceKeywordListElement.AppendChild(serviceKeywordElement);
                    }
                }
            }

            serviceElement.AppendChild(serviceKeywordListElement);

            var serviceOnlineResource = CreateOnlineResourceElement(this.baseUrl);
            serviceElement.AppendChild(serviceOnlineResource);

            var capabilityElement = doc.CreateElement(Identifiers.CapabilityElement);
            rootElement.AppendChild(capabilityElement);

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
            capabilityElement.AppendChild(capabilityRequest);

            var capabilityException = doc.CreateElement("Exception");
            var capabilityExceptionFormat = doc.CreateElement("Format");

            switch (version)
            {
                case Version.Version111: { capabilityExceptionFormat.InnerText = MediaTypeNames.Application.OgcServiceExceptionXml; break; }
                case Version.Version130: { capabilityExceptionFormat.InnerText = "XML"; break; }
                default: throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
            }

            capabilityException.AppendChild(capabilityExceptionFormat);
            capabilityElement.AppendChild(capabilityException);

            foreach (var layer in layers)
            {
                capabilityElement.AppendChild(CreateLayerElement(version, layer));
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
                        bboxElementName = "EX_GeographicBoundingBox";
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

                    string? minx = null, miny = null, maxx = null, maxy = null;
                    var bbox = bboxElementName != null ? layer.SelectSingleNode(defaultNsPrefix + bboxElementName, nsManager) : null;
                    if (bbox != null)
                    {
                        switch (version)
                        {
                            case Version.Version111:
                                {
                                    if (bbox.Attributes != null)
                                    {
                                        minx = bbox.Attributes["minx"]?.Value;
                                        miny = bbox.Attributes["miny"]?.Value;
                                        maxx = bbox.Attributes["maxx"]?.Value;
                                        maxy = bbox.Attributes["maxy"]?.Value;
                                    }

                                    break;
                                }
                            case Version.Version130:
                                {
                                    minx = bbox.SelectSingleNode(defaultNsPrefix + "westBoundLongitude", nsManager)?.InnerText;
                                    maxx = bbox.SelectSingleNode(defaultNsPrefix + "eastBoundLongitude", nsManager)?.InnerText;
                                    miny = bbox.SelectSingleNode(defaultNsPrefix + "southBoundLatitude", nsManager)?.InnerText;
                                    maxy = bbox.SelectSingleNode(defaultNsPrefix + "northBoundLatitude", nsManager)?.InnerText;
                                    break;
                                }
                        }
                    }

                    result.Add(new Layer
                    {
                        Name = layerName != null ? layerName.InnerText : String.Empty,
                        Title = layerTitle != null ? layerTitle.InnerText : String.Empty,
                        IsQueryable = layerQueryable != null && layerQueryable.Value == "1",
                        GeographicalBounds = bbox != null && bbox.Attributes != null ?
                            new Models.GeographicalBounds(
                                minx != null ? Double.Parse(minx, CultureInfo.InvariantCulture) : 0.0,
                                miny != null ? Double.Parse(miny, CultureInfo.InvariantCulture) : 0.0,
                                maxx != null ? Double.Parse(maxx, CultureInfo.InvariantCulture) : 0.0,
                                maxy != null ? Double.Parse(maxy, CultureInfo.InvariantCulture) : 0.0) :
                            null,
                    });
                }
            }

            return result;
        }

        private static Version GetVersion(XmlDocument xmlDoc)
        {
            var rootElement = xmlDoc.DocumentElement ?? throw new FormatException("Root Element was not found.");
            var versionAttribute = rootElement.Attributes["version"] ?? throw new FormatException("Version attribute was not found.");

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
