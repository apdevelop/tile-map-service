using System.Collections.Generic;
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
            // TODO: all metadata values (bounds, zoom, center), useful for capabilities document
        }

        public string Name { get; private set; }

        public string Format { get; private set; }

        private MetadataItem GetItem(string name)
        {
            return this.metadata.FirstOrDefault(i => i.Name == name);
        }
    }
}
