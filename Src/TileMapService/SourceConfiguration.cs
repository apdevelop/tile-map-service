using System.Text.Json.Serialization;

namespace TileMapService
{
    /// <summary>
    /// Represents source configuration and properties.
    /// </summary>
    public class SourceConfiguration
    {
        /// <summary>
        /// Type of source, "file", "mbtiles", "xyz", "tms", "wmts", "wms", "geotiff".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        #region Types
        /// <summary>
        /// Local files in directories.
        /// </summary>
        [JsonIgnore]
        public const string TypeLocalFiles = "file";

        /// <summary>
        /// MBTiles database local file.
        /// </summary>
        [JsonIgnore]
        public const string TypeMBTiles = "mbtiles";

        /// <summary>
        /// Tile service with minimalistic REST API (Slippy Map).
        /// </summary>
        [JsonIgnore]
        public const string TypeXyz = "xyz";

        /// <summary>
        /// Tile service with TMS protocol support.
        /// </summary>
        [JsonIgnore]
        public const string TypeTms = "tms";

        /// <summary>
        /// Tile service with WMTS protocol support.
        /// </summary>
        [JsonIgnore]
        public const string TypeWmts = "wmts";

        /// <summary>
        /// Wem Map Service (WMS protocol).
        /// </summary>
        [JsonIgnore]
        public const string TypeWms = "wms";

        /// <summary>
        /// GeoTiff local file.
        /// </summary>
        [JsonIgnore]
        public const string TypeGeoTiff = "geotiff";
        #endregion

        /// <summary>
        /// String identifier of tile source (case-sensitive).
        /// </summary>
        [JsonPropertyName("id")] // TODO: ! JsonPropertyName("...") actually ignored
        public string Id { get; set; }

        /// <summary>
        /// Name of image format ("png", "jpg", "pbf").
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

        /// <summary>
        /// Spatial reference system (SRS), EPSG code.
        /// </summary>
        [JsonPropertyName("srs")]
        public string Srs { get; set; }

        // TODO: more custom properties, like abstract, attribution and so on.

        /// <summary>
        /// Maximum extent of the tiles coordinates in EPSG:4326 coordinate system.
        /// </summary>
        [JsonIgnore] // TODO: allow reading from config file
        public Models.GeographicalBounds GeographicalBounds { get; set; }

        /// <summary>
        /// Cache configuration, if used.
        /// </summary>
        [JsonPropertyName("cache")]
        public SourceCacheConfiguration Cache { get; set; }
    }

    /// <summary>
    /// Cache configuration.
    /// </summary>
    public class SourceCacheConfiguration
    {
        /// <summary>
        /// Type of cache ('mbtiles' is only valid value).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// Full path to cache database file.
        /// </summary>
        [JsonPropertyName("dbfile")]
        public string DbFile { get; set; }
    }
}
