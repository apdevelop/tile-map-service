using System.Xml;

namespace TileMapService.Tms
{
    class TileMapServerError
    {
        private readonly string message;

        public TileMapServerError(string message)
        {
            this.message = message;
        }

        public XmlDocument ToXml()
        {
            var doc = new XmlDocument();
            var rootElement = doc.CreateElement(string.Empty, "TileMapServerError", string.Empty);

            var messageElement = doc.CreateElement("Message");
            messageElement.AppendChild(doc.CreateTextNode(this.message));
            rootElement.AppendChild(messageElement);

            doc.AppendChild(rootElement);

            return doc;
        }
    }
}
