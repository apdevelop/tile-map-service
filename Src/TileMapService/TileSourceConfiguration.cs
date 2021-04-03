using System.Text.Json.Serialization;

namespace TileMapService
{
    /// <summary>
    /// Represents tile source configuration and properties.
    /// </summary>
    public class TileSourceConfiguration
    {
        /// <summary>
        /// Type of tile source, "file", "mbtiles", "xyz", "tms", "wmts".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonIgnore]
        public const string TypeLocalFiles = "file";

        [JsonIgnore]
        public const string TypeMBTiles = "mbtiles";

        [JsonIgnore]
        public const string TypeXyz = "xyz";

        [JsonIgnore]
        public const string TypeTms = "tms";

        [JsonIgnore]
        public const string TypeWmts = "wmts";

        /// <summary>
        /// String identifier of tile source (case-sensitive).
        /// </summary>
        [JsonPropertyName("id")] // TODO: ! JsonPropertyName("...") actually ignored
        public string Id { get; set; }

        /// <summary>
        /// Name of tiles image format (png, jpg).
        /// </summary>
        [JsonPropertyName("format")]
        public string Format { get; set; }

        /// <summary>
        /// User-friendly title (displayed name) of tile source.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// Location of tiles (path template for "file", full path for "mbtiles", url template for "http").
        /// </summary>
        [JsonPropertyName("location")]
        public string Location { get; set; }

        /// <summary>
        /// TMS type Y coordinate (true: Y going from bottom to top; false: from top to bottom, like in OSM tiles).
        /// </summary>
        [JsonPropertyName("tms")]
        public bool? Tms { get; set; }

        /// <summary>
        /// MIME type identifier of image format.
        /// </summary>
        [JsonIgnore]
        public string ContentType { get; set; }

        [JsonPropertyName("minzoom")]
        public int? MinZoom { get; set; }

        [JsonPropertyName("maxzoom")]
        public int? MaxZoom { get; set; }

        // TODO: bounds, center, attribution,.. (MBTiles metadata as example).
    }
}
