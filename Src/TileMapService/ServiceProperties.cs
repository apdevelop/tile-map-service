using System;
using System.Text.Json.Serialization;

namespace TileMapService
{
    /// <summary>
    /// Represents entire service properties.
    /// </summary>
    public class ServiceProperties
    {
        /// <summary>
        /// User-friendly title (displayed name) of service.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = String.Empty;

        /// <summary>
        /// Detailed text description of service.
        /// </summary>
        [JsonPropertyName("abstract")]
        public string Abstract { get; set; } = String.Empty;

        /// <summary>
        /// Keywords describing service.
        /// </summary>
        [JsonPropertyName("keywords")]
        public string Keywords { get; set; } = String.Empty;

        public string[]? KeywordsList
        {
            get
            {
                return String.IsNullOrWhiteSpace(this.Keywords)
                    ? null
                    : this.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        /// <summary>
        /// Quality (compression level) for JPEG output in WMS endpoint.
        /// </summary>
        [JsonPropertyName("jpegQuality")] // TODO: ? separate output options config ?
        public int JpegQuality { get; set; } = 90;
    }
}
