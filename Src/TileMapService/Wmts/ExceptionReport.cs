using System;
using System.Xml;

namespace TileMapService.Wmts
{
    class ExceptionReport
    {
        private readonly string message;

        private readonly string exceptionCode;

        public ExceptionReport(string exceptionCode, string message)
        {
            this.exceptionCode = exceptionCode;
            this.message = message;
        }

        public XmlDocument ToXml()
        {
            var doc = new XmlDocument();
            var rootElement = doc.CreateElement(String.Empty, Identifiers.ExceptionReportElement, Identifiers.OwsNamespaceUri);

            var exceptionElement = doc.CreateElement(String.Empty, Identifiers.ExceptionElement, Identifiers.OwsNamespaceUri);
            exceptionElement.SetAttribute(Identifiers.ExceptionCodeAttribute, this.exceptionCode);
            exceptionElement.AppendChild(doc.CreateTextNode(this.message));
            rootElement.AppendChild(exceptionElement);

            doc.AppendChild(rootElement);

            return doc;
        }
    }
}
