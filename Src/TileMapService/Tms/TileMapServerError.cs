using System;
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
            var rootElement = doc.CreateElement(String.Empty, "TileMapServerError", String.Empty);
            doc.AppendChild(rootElement);

            var messageElement = doc.CreateElement("Message");
            messageElement.AppendChild(doc.CreateTextNode(this.message));
            rootElement.AppendChild(messageElement);

            return doc;
        }
    }
}
