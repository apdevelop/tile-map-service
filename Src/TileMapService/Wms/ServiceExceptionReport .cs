using System;
using System.Xml;

namespace TileMapService.Wms
{
    class ServiceExceptionReport
    {
        private readonly string? code;

        private readonly string message;

        private readonly string version;

        public ServiceExceptionReport(string? code, string message, string version)
        {
            this.code = code;
            this.message = message;
            this.version = version;
        }

        public XmlDocument ToXml()
        {
            var doc = new XmlDocument();
            var rootElement = doc.CreateElement(String.Empty, Identifiers.ServiceExceptionReportElement, Identifiers.OgcNamespaceUri);
            rootElement.SetAttribute("xmlns", Identifiers.OgcNamespaceUri);
            rootElement.SetAttribute(Identifiers.VersionAttribute, this.version);
            // TODO: ? xsi:schemaLocation

            var exceptionElement = doc.CreateElement(String.Empty, Identifiers.ServiceExceptionElement, Identifiers.OgcNamespaceUri);
            if (!String.IsNullOrEmpty(this.code))
            {
                exceptionElement.SetAttribute(Identifiers.CodeAttribute, this.code);
            }

            exceptionElement.AppendChild(doc.CreateTextNode(this.message));

            rootElement.AppendChild(exceptionElement);

            doc.AppendChild(rootElement);

            return doc;
        }
    }
}
