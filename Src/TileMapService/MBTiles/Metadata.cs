using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TileMapService.MBTiles
{
    /// <summary>
    /// Represents metadata set from MBTiles database.
    /// </summary>
    class Metadata
    {
        private readonly List<MetadataItem> metadataItems;

        public Metadata(IEnumerable<MetadataItem> metadata)
        {
            this.metadataItems = metadata.ToList();

            this.Name = GetItem(MetadataItem.KeyName)?.Value;
            this.Format = GetItem(MetadataItem.KeyFormat)?.Value;

            var bounds = GetItem(MetadataItem.KeyBounds);
            if (bounds != null)
            {
                if (!String.IsNullOrEmpty(bounds.Value))
                {
                    this.Bounds = Models.Bounds.FromMBTilesMetadataString(bounds.Value);
                }
            }

            var center = GetItem(MetadataItem.KeyCenter);
            if (center != null)
            {
                if (!String.IsNullOrEmpty(center.Value))
                {
                    this.Center = Models.GeographicalPointWithZoom.FromMBTilesMetadataString(center.Value);
                }
            }

            var minzoom = GetItem(MetadataItem.KeyMinZoom);
            if (minzoom != null)
            {
                if (Int32.TryParse(minzoom.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minZoomValue))
                {
                    this.MinZoom = minZoomValue;
                }
            }

            var maxzoom = GetItem(MetadataItem.KeyMaxZoom);
            if (maxzoom != null)
            {
                if (Int32.TryParse(maxzoom.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxZoomValue))
                {
                    this.MaxZoom = maxZoomValue;
                }
            }

            this.Attribution = GetItem(MetadataItem.KeyAttribution)?.Value;
            this.Description = GetItem(MetadataItem.KeyDescription)?.Value;
            this.Type = GetItem(MetadataItem.KeyType)?.Value;
            this.Version = GetItem(MetadataItem.KeyVersion)?.Value;
            this.Json = GetItem(MetadataItem.KeyJson)?.Value;
        }

        /// <summary>
        /// The human-readable name of the tileset.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The file format of the tile data: pbf, jpg, png, webp, or an IETF media type for other formats.
        /// </summary>
        public string Format { get; private set; }

        /// <summary>
        /// The maximum extent of the rendered map area (as WGS 84 latitude and longitude values, in the OpenLayers Bounds format: left, bottom, right, top), string of comma-separated numbers.
        /// </summary>
        public Models.Bounds Bounds { get; private set; }

        /// <summary>
        /// The longitude, latitude, and zoom level of the default view of the map, string of comma-separated numbers.
        /// </summary>
        public Models.GeographicalPointWithZoom Center { get; private set; }

        /// <summary>
        /// The lowest zoom level (number) for which the tileset provides data.
        /// </summary>
        public int? MinZoom { get; private set; }

        /// <summary>
        /// The highest zoom level (number) for which the tileset provides data.
        /// </summary>
        public int? MaxZoom { get; private set; }

        /// <summary>
        /// An attribution (HTML) string, which explains the sources of data and/or style for the map.
        /// </summary>
        public string Attribution { get; private set; }

        /// <summary>
        /// A description of the tileset content.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Type of tileset: "overlay" or "baselayer".
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// The version (revision) of the tileset.
        /// </summary>
        /// <remarks>
        /// Version is a number, according to specification, but actually can be a string in real datasets.
        /// </remarks>
        public string Version { get; private set; }

        /// <summary>
        /// Lists the layers that appear in the vector tiles and the names and types of the attributes of features that appear in those layers in case of pbf format.
        /// </summary>
        /// <remarks>
        /// See https://github.com/mapbox/mbtiles-spec/blob/master/1.3/spec.md#vector-tileset-metadata
        /// </remarks>
        public string Json { get; private set; }

        private MetadataItem GetItem(string name)
        {
            return this.metadataItems.FirstOrDefault(i => i.Name == name);
        }
    }
}
