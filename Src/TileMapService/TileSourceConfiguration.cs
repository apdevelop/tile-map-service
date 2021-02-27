using System.Text.Json.Serialization;

namespace TileMapService
{
    /// <summary>
    /// Tile source configuration and properties.
    /// </summary>
    public class TileSourceConfiguration
    {
        [JsonPropertyName("format")]
        public string Format { get; set; } // TODO: get from actual source properties

        /// <summary>
        /// String identifier of tile source.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// User-friendly (human-readable) name of tile source.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// Uri of source.
        /// </summary>
        [JsonPropertyName("source")]
        public string Source { get; set; }

        /// <summary>
        /// TMS type Y coordinate (true: Y going from bottom to top; false: from top to bottom, like in OSM tiles).
        /// </summary>
        [JsonPropertyName("tms")]
        public bool Tms { get; set; } // TODO: default true for MBTiles
    }
}
