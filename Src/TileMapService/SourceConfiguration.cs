using System;
using System.Text.Json.Serialization;

namespace TileMapService
{
    /// <summary>
    /// Represents source configuration and properties.
    /// </summary>
    /// <remarks>
    /// The [JsonPropertyName("...")] attribute is actually ignored on properties when loading configuration
    /// https://stackoverflow.com/questions/60470583/handling-key-names-with-periods-in-net-core-appsettings-configuration
    /// https://github.com/dotnet/runtime/issues/36010
    /// </remarks>
    public class SourceConfiguration
    {
        /// <summary>
        /// Type of source, "file", "mbtiles", "postgis", "xyz", "geotiff", "tms", "wmts", "wms".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = String.Empty;

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
        /// PostGIS database.
        /// </summary>
        [JsonIgnore]
        public const string TypePostGIS = "postgis";

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
        /// Web Map Service (WMS protocol).
        /// </summary>
        [JsonIgnore]
        public const string TypeWms = "wms";

        /// <summary>
        /// GeoTIFF local file.
        /// </summary>
        [JsonIgnore]
        public const string TypeGeoTiff = "geotiff";
        #endregion

        /// <summary>
        /// String identifier of tile source (case-sensitive).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Name of image format ("png", "jpg", "mvt", "pbf").
        /// </summary>
        [JsonPropertyName("format")]
        public string? Format { get; set; } // TODO: implement conversion of source formats to output formats

        /// <summary>
        /// User-friendly title (displayed name) of source.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = String.Empty;

        /// <summary>
        /// Detailed text description of source.
        /// </summary>
        [JsonPropertyName("abstract")]
        public string Abstract { get; set; } = String.Empty;

        /// <summary>
        /// An attribution (HTML) string, which explains the sources of data and/or style for the map.
        /// </summary>
        [JsonPropertyName("attribution")]
        public string? Attribution { get; set; }

        /// <summary>
        /// Location of tiles (path template for "file", full path for "mbtiles", url template for "http").
        /// </summary>
        [JsonPropertyName("location")]
        public string? Location { get; set; }

        /// <summary>
        /// TMS type Y coordinate (true: Y going from bottom to top; false: from top to bottom, like in OSM tiles).
        /// </summary>
        [JsonPropertyName("tms")]
        public bool? Tms { get; set; }

        /// <summary>
        /// MIME type identifier of image format.
        /// </summary>
        [JsonIgnore]
        public string? ContentType { get; set; }

        [JsonPropertyName("minzoom")]
        public int? MinZoom { get; set; }

        [JsonPropertyName("maxzoom")]
        public int? MaxZoom { get; set; }

        /// <summary>
        /// Spatial reference system (SRS), "EPSG:4326" format.
        /// </summary>
        [JsonPropertyName("srs")]
        public string? Srs { get; set; }

        [JsonIgnore] // TODO: allow reading from config file
        public int TileWidth { get; set; } = Utils.WebMercator.DefaultTileWidth;

        [JsonIgnore] // TODO: allow reading from config file
        public int TileHeight { get; set; } = Utils.WebMercator.DefaultTileHeight;

        /// <summary>
        /// Maximum extent of the tiles coordinates in EPSG:4326 coordinate system.
        /// </summary>
        [JsonIgnore] // TODO: allow reading from config file
        public Models.GeographicalBounds? GeographicalBounds { get; set; }

        /// <summary>
        /// Cache configuration, if used.
        /// </summary>
        [JsonPropertyName("cache")]
        public SourceCacheConfiguration? Cache { get; set; }

        /// <summary>
        /// WMTS source type configuration.
        /// </summary>
        [JsonPropertyName("wmts")]
        public WmtsSourceConfiguration? Wmts { get; set; }

        /// <summary>
        /// WMS source type configuration.
        /// </summary>
        [JsonPropertyName("wms")]
        public WmsSourceConfiguration? Wms { get; set; }

        /// <summary>
        /// PostGIS source type configuration.
        /// </summary>
        [JsonPropertyName("postgis")]
        public PostGisSourceTableConfiguration? PostGis { get; set; }
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
        public string? Type { get; set; }

        /// <summary>
        /// Full path to cache database file.
        /// </summary>
        [JsonPropertyName("dbfile")]
        public string? DbFile { get; set; }
    }

    /// <summary>
    /// PostGIS source type configuration.
    /// </summary>
    public class PostGisSourceTableConfiguration
    {
        /// <summary>
        /// Table name.
        /// </summary>
        [JsonPropertyName("table")]
        public string? Table { get; set; }

        /// <summary>
        /// Name of geometry field.
        /// </summary>
        [JsonPropertyName("geometry")]
        public string? Geometry { get; set; }

        /// <summary>
        /// List of fields with object attributes in form of CSV string.
        /// </summary>
        [JsonPropertyName("fields")]
        public string? Fields { get; set; }
    }

    /// <summary>
    /// WMTS source type configuration.
    /// </summary>
    public class WmtsSourceConfiguration
    {
        /// <summary>
        /// WMTS Capabilities document URL.
        /// </summary>
        [JsonPropertyName("capabilitiesurl")]
        public string? CapabilitiesUrl { get; set; }

        /// <summary>
        /// Layer identifier.
        /// </summary>
        [JsonPropertyName("layer")]
        public string? Layer { get; set; }

        /// <summary>
        /// Style identifier.
        /// </summary>
        [JsonPropertyName("style")]
        public string? Style { get; set; }

        /// <summary>
        /// TileMatrixSet identifier.
        /// </summary>
        [JsonPropertyName("tilematrixset")]
        public string? TileMatrixSet { get; set; }
    }

    /// <summary>
    /// WMS source type configuration.
    /// </summary>
    public class WmsSourceConfiguration
    {
        /// <summary>
        /// Layer identifier.
        /// </summary>
        [JsonPropertyName("layer")]
        public string? Layer { get; set; } // TODO: ? multiple layers

        public string? Version { get; set; }
    }
}
