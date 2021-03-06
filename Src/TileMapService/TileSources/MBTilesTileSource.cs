using Microsoft.Data.Sqlite;
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

        private readonly MBTiles.Repository repository;

        public MBTilesTileSource(TileSourceConfiguration configuration)
        {
            if (String.IsNullOrEmpty(configuration.Name))
            {
                throw new ArgumentException();
            }

            if (String.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException();
            }

            this.configuration = configuration; // May be changed later in InitAsync
            this.repository = new MBTiles.Repository(CreateSqliteConnectionString(configuration.Location));
        }

        #region ITileSource implementation

        async Task ITileSource.InitAsync()
        {
            // Configuration values priority:
            // 1. Default values for MBTiles.
            // 2. Actual values (MBTiles metadata).
            // 3. Values from configuration file - overrides given above, if provided.

            var metadata = new MBTiles.Metadata(await this.repository.ReadMetadataAsync());

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                    (!String.IsNullOrEmpty(metadata.Name) ? metadata.Name : this.configuration.Name) :
                    this.configuration.Title;

            var format = String.IsNullOrEmpty(this.configuration.Format) ?
                    (!String.IsNullOrEmpty(metadata.Format) ? metadata.Format : "png") :
                    this.configuration.Format;

            this.configuration = new TileSourceConfiguration
            {
                Name = this.configuration.Name,
                Format = format,
                Title = title,
                Tms = this.configuration.Tms ?? true, // Default true for the MBTiles, following the Tile Map Service Specification.
                Location = this.configuration.Location,
                ContentType = Utils.TileFormatToContentType(format),
            };
        }

        async Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            return await this.repository.ReadTileDataAsync(x, this.configuration.Tms.Value ? y : Utils.FlipYCoordinate(y, z), z);
        }

        TileSourceConfiguration ITileSource.Configuration
        {
            get
            {
                return this.configuration;
            }
        }

        #endregion

        /// <summary>
        /// Creates connection string for MBTiles database.
        /// </summary>
        /// <param name="source">Full path to MBTiles database file.</param>
        /// <returns>Connection string.</returns>
        private static string CreateSqliteConnectionString(string source)
        {
            return new SqliteConnectionStringBuilder
            {
                DataSource = GetLocalFilePath(source),
                Mode = SqliteOpenMode.ReadOnly,
            }.ToString();
        }

        private static string GetLocalFilePath(string source)
        {
            var uriString = source.Replace(Utils.MBTilesScheme, Utils.LocalFileScheme, StringComparison.Ordinal);
            var uri = new Uri(uriString);

            return uri.LocalPath;
        }
    }
}
