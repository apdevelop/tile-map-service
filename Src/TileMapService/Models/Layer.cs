namespace TileMapService.Models
{
    /// <summary>
    /// Represents tile source properties in TMS and WMTS Capabilities XML document.
    /// </summary>
    class Layer
    {
        public string Identifier { get; set; }

        public string Title { get; set; }

        // TODO: Abstract

        public string ContentType { get; set; }

        public string Format { get; set; }

        public string Srs { get; set; }

        public int MinZoom { get; set; }

        public int MaxZoom { get; set; }
    }
}
