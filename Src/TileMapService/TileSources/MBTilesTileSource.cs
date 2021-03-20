using System;
using System.Threading.Tasks;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with tiles stored in MBTiles database.
    /// </summary>
    class MBTilesTileSource : ITileSource
    {
        private TileSourceConfiguration configuration;

        private MBTiles.Repository repository;

        public MBTilesTileSource(TileSourceConfiguration configuration)
        {
            if (String.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException();
            }

            if (String.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException();
            }

            this.configuration = configuration; // May be changed later in InitAsync
        }

        #region ITileSource implementation

        Task ITileSource.InitAsync()
        {
            // Configuration values priority:
            // 1. Default values for MBTiles.
            // 2. Actual values (MBTiles metadata).
            // 3. Values from configuration file - overrides given above, if provided.

            this.repository = new MBTiles.Repository(configuration.Location, false);
            var metadata = new MBTiles.Metadata(this.repository.ReadMetadata());

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                    (!String.IsNullOrEmpty(metadata.Name) ? metadata.Name : this.configuration.Id) :
                    this.configuration.Title;

            var format = String.IsNullOrEmpty(this.configuration.Format) ?
                    (!String.IsNullOrEmpty(metadata.Format) ? metadata.Format : "png") :
                    this.configuration.Format;

            // Re-create configuration
            this.configuration = new TileSourceConfiguration
            {
                Id = this.configuration.Id,
                Format = format,
                Title = title,
                Tms = this.configuration.Tms ?? true, // Default true for the MBTiles, following the Tile Map Service Specification.
                Location = this.configuration.Location,
                ContentType = Utils.TileFormatToContentType(format),
                MinZoom = this.configuration.MinZoom ?? metadata.MinZoom ?? 0, // TODO: ? Check actual SELECT MIN/MAX(zoom_level) ?
                MaxZoom = this.configuration.MaxZoom ?? metadata.MaxZoom ?? 24,
            };

            return Task.CompletedTask;
        }

        Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            return Task.FromResult(this.repository.ReadTileData(x, this.configuration.Tms.Value ? y : Utils.FlipYCoordinate(y, z), z));
        }

        TileSourceConfiguration ITileSource.Configuration
        {
            get
            {
                return this.configuration;
            }
        }

        #endregion
    }
}
