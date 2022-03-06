namespace TileMapService.Wms
{
    /// <summary>
    /// Represents layer properties in WMS Capabilities XML document.
    /// </summary>
    class Layer // TODO: ? use shared TileMapService.Models.Layer DTO
    {
        public string? Title { get; set; }

        public string? Name { get; set; }

        public string?  Abstract { get; set; }

        public bool IsQueryable { get; set; }

        public Models.GeographicalBounds? GeographicalBounds { get; set; }
    }
}
