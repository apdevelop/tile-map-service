namespace TileMapService.Wmts
{
    /// <summary>
    /// Represents Layer element attributes in WMTS Capabilities XML document.
    /// </summary>
    class Layer
    {
        public string Identifier { get; set; }

        public string Title { get; set; }

        public string Format { get; set; }
    }
}
