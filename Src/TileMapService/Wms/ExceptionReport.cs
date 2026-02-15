using System.Xml;

namespace TileMapService.Wms
{
    class ExceptionReport
    {
        private readonly string exceptionCode;

        private readonly string message;

        private readonly string locator;

        public ExceptionReport(string exceptionCode, string message, string locator)
        {
            this.exceptionCode = exceptionCode;
            this.message = message;
            this.locator = locator;
        }

        public XmlDocument ToXml()
        {
            var doc = new XmlDocument();
            var rootElement = doc.CreateElement(Identifiers.OwsPrefix, Identifiers.ExceptionReportElement, Identifiers.OwsNamespaceUri);
            rootElement.SetAttribute("xmlns:xs", "http://www.w3.org/2001/XMLSchema");
            rootElement.SetAttribute("xmlns:ows", Identifiers.OwsNamespaceUri);
            rootElement.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            rootElement.SetAttribute(Identifiers.VersionAttribute, "1.0.0");
            // TODO: ? xsi:schemaLocation

            var exceptionElement = doc.CreateElement(Identifiers.OwsPrefix, Identifiers.ExceptionElement, Identifiers.OwsNamespaceUri);
            exceptionElement.SetAttribute(Identifiers.ExceptionCodeAttribute, this.exceptionCode);
            exceptionElement.SetAttribute(Identifiers.LocatorAttribute, this.locator);

            var exceptionTextElement = doc.CreateElement(Identifiers.OwsPrefix, Identifiers.ExceptionTextElement, Identifiers.OwsNamespaceUri);
            exceptionTextElement.AppendChild(doc.CreateTextNode(this.message));

            exceptionElement.AppendChild(exceptionTextElement);
            rootElement.AppendChild(exceptionElement);

            doc.AppendChild(rootElement);

            return doc;
        }
    }
}
