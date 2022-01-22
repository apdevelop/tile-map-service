using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace TileMapService.Wms
{
    class CapabilitiesBuilder
    {
        private const string XlinkNamespaceUri = "http://www.w3.org/1999/xlink";

        private readonly string baseUrl;

        private XmlDocument doc;

        public CapabilitiesBuilder(string baseUrl)
        {
            this.baseUrl = baseUrl;
        }

        public XmlDocument GetCapabilities(
            Version version,
            Service service,
            IList<Layer> layers,
            IList<string> getMapFormats,
            IList<string> getFeatureInfoFormats)
        {
            // TODO: EPSG:4326 support

            string rootNodeName;
            switch (version)
            {
                case Version.Version111: { rootNodeName = "WMT_MS_Capabilities"; break; }
                case Version.Version130: { rootNodeName = "WMS_Capabilities"; break; }
                default: throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
            }

            doc = new XmlDocument();
            var root = doc.CreateElement(String.Empty, rootNodeName, String.Empty);
            doc.AppendChild(root);

            var versionAttribute = doc.CreateAttribute("version");
            switch (version)
            {
                case Version.Version111: { versionAttribute.Value = Identifiers.Version111; break; }
                case Version.Version130: { versionAttribute.Value = Identifiers.Version130; break; }
                default: throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
            }

            root.Attributes.Append(versionAttribute);

            var serviceElement = doc.CreateElement(Identifiers.Service);
            root.AppendChild(serviceElement);

            var serviceName = doc.CreateElement("Name");
            serviceName.InnerText = "OGC:WMS";
            serviceElement.AppendChild(serviceName);

            var serviceTitle = doc.CreateElement("Title");
            serviceTitle.InnerText = service.Title;
            serviceElement.AppendChild(serviceTitle);

            var serviceAbstract = doc.CreateElement("Abstract");
            serviceAbstract.InnerText = service.Abstract;
            serviceElement.AppendChild(serviceAbstract);

            var serviceOnlineResource = CreateOnlineResourceElement(this.baseUrl);
            serviceElement.AppendChild(serviceOnlineResource);

            var capability = doc.CreateElement(Identifiers.Capability);
            root.AppendChild(capability);

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

        private XmlElement CreateRequestElement(string name, IEnumerable<string> formats)
        {
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
            var serviceOnlineResource = doc.CreateElement("OnlineResource");

            var hrefAttribute = doc.CreateAttribute("xlink", "href", XlinkNamespaceUri);
            hrefAttribute.Value = href;
            serviceOnlineResource.Attributes.Append(hrefAttribute);

            var typeAttribute = doc.CreateAttribute("xlink", "type", XlinkNamespaceUri);
            typeAttribute.Value = "simple";
            serviceOnlineResource.Attributes.Append(typeAttribute);

            return serviceOnlineResource;
        }

        private XmlElement CreateLayerElement(Version version, Layer layerProperties)
        {
            var layer = doc.CreateElement("Layer");

            var queryableAttribute = doc.CreateAttribute("queryable");
            queryableAttribute.Value = layerProperties.IsQueryable ? "1" : "0";
            layer.Attributes.Append(queryableAttribute);

            var layerTitle = doc.CreateElement(Identifiers.Title);
            layerTitle.InnerText = layerProperties.Title;
            layer.AppendChild(layerTitle);

            var layerName = doc.CreateElement(Identifiers.Name);
            layerName.InnerText = layerProperties.Name;
            layer.AppendChild(layerName);

            var layerAbstract = doc.CreateElement(Identifiers.Abstract);
            layerAbstract.InnerText = layerProperties.Abstract;
            layer.AppendChild(layerAbstract);

            string layerSrsNodeName;
            switch (version)
            {
                case Version.Version111: { layerSrsNodeName = Identifiers.Srs; break; }
                case Version.Version130: { layerSrsNodeName = Identifiers.Crs; break; }
                default: throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
            }

            var layerSrs = doc.CreateElement(layerSrsNodeName);
            layerSrs.InnerText = Identifiers.EPSG3857;
            layer.AppendChild(layerSrs);

            var geoBounds = new Models.GeographicalBounds(-180, -90, 180, 90); // TODO: from layer properties

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

                        layer.AppendChild(latlonBoundingBox);
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

                        layer.AppendChild(geographicBoundingBox);
                        break;
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
                    }
            }

            {
                var boundingBox = doc.CreateElement("BoundingBox");

                string boundingBoxSrsNodeName;
                switch (version)
                {
                    case Version.Version111: { boundingBoxSrsNodeName = Identifiers.Srs; break; }
                    case Version.Version130: { boundingBoxSrsNodeName = Identifiers.Crs; break; }
                    default: throw new ArgumentOutOfRangeException(nameof(version), $"WMS version '{version}' is not supported.");
                }

                var boundingBoxSrsAttribute = doc.CreateAttribute(boundingBoxSrsNodeName);
                boundingBoxSrsAttribute.Value = Identifiers.EPSG3857; // TODO: other CRS
                boundingBox.Attributes.Append(boundingBoxSrsAttribute);

                var minxAttribute = doc.CreateAttribute("minx");
                minxAttribute.Value = Utils.WebMercator.X(geoBounds.MinLongitude).ToString("E16", CultureInfo.InvariantCulture);
                boundingBox.Attributes.Append(minxAttribute);

                var minyAttribute = doc.CreateAttribute("miny");
                minyAttribute.Value = Utils.WebMercator.Y(geoBounds.MinLatitude).ToString("E16", CultureInfo.InvariantCulture);
                boundingBox.Attributes.Append(minyAttribute);

                var maxxAttribute = doc.CreateAttribute("maxx");
                maxxAttribute.Value = Utils.WebMercator.X(geoBounds.MaxLongitude).ToString("E16", CultureInfo.InvariantCulture);
                boundingBox.Attributes.Append(maxxAttribute);

                var maxyAttribute = doc.CreateAttribute("maxy");
                maxyAttribute.Value = Utils.WebMercator.Y(geoBounds.MaxLatitude).ToString("E16", CultureInfo.InvariantCulture);
                boundingBox.Attributes.Append(maxyAttribute);

                layer.AppendChild(boundingBox);
            }

            return layer;
        }
    }
}
