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
        private readonly IEnumerable<MetadataItem> metadata;

        public Metadata(IEnumerable<MetadataItem> metadata)
        {
            this.metadata = metadata;

            this.Name = GetItem(MetadataItem.KeyName)?.Value;
            this.Format = GetItem(MetadataItem.KeyFormat)?.Value;

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

            // TODO: read all metadata values, use for capabilities document
        }

        // TODO: bounds, center, version, json

        public string Name { get; private set; }

        public string Format { get; private set; }

        public int? MinZoom { get; private set; }

        public int? MaxZoom { get; private set; }

        public string Attribution { get; private set; }

        public string Description { get; private set; }

        public string Type { get; private set; }

        private MetadataItem GetItem(string name)
        {
            return this.metadata.FirstOrDefault(i => i.Name == name);
        }
    }
}
