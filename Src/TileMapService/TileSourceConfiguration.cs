using System.Text.Json.Serialization;

namespace TileMapService
{
    /// <summary>
    /// Represents tile source configuration and properties.
    /// </summary>
    public class TileSourceConfiguration
    {
        /// <summary>
        /// String identifier of tile source.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Name of tiles format (jpg, png).
        /// </summary>
        [JsonPropertyName("format")]
        public string Format { get; set; }

        /// <summary>
        /// User-friendly (displayed) name of tile source.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// Location of tiles in URL-like format.
        /// </summary>
        [JsonPropertyName("location")]
        public string Location { get; set; }

        /// <summary>
        /// TMS type Y coordinate (true: Y going from bottom to top; false: from top to bottom, like in OSM tiles).
        /// </summary>
        [JsonPropertyName("tms")]
        public bool? Tms { get; set; }

        [JsonIgnore]
        public string ContentType { get; set; }

        // TODO: bounds, minzoom, maxzoom, center, attribution,.. (MBTiles metadata as example).
    }
}
