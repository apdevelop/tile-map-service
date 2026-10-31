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
            var rootElement = doc.CreateElement(string.Empty, Identifiers.ExceptionReportElement, Identifiers.OwsNamespaceUri);

            var exceptionElement = doc.CreateElement(string.Empty, Identifiers.ExceptionElement, Identifiers.OwsNamespaceUri);
            exceptionElement.SetAttribute(Identifiers.ExceptionCodeAttribute, exceptionCode);
            exceptionElement.AppendChild(doc.CreateTextNode(message));
            rootElement.AppendChild(exceptionElement);

            doc.AppendChild(rootElement);

            return doc;
        }
    }
}
