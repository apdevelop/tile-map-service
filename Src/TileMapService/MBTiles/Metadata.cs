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

            // TODO: version, json
        }

        public string Name { get; private set; }

        public string Format { get; private set; }

        public Models.Bounds Bounds { get; private set; }

        public Models.GeographicalPointWithZoom Center { get; private set; }

        public int? MinZoom { get; private set; }

        public int? MaxZoom { get; private set; }

        public string Attribution { get; private set; }

        public string Description { get; private set; }

        public string Type { get; private set; }

        private MetadataItem GetItem(string name)
        {
            return this.metadataItems.FirstOrDefault(i => i.Name == name);
        }
    }
}
