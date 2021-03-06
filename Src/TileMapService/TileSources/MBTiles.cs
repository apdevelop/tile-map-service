using Microsoft.Data.Sqlite;
using System;
using System.Threading.Tasks;

namespace TileMapService.TileSources
{
    class MBTiles : ITileSource
    {
        private readonly TileSourceConfiguration configuration;

        private readonly MBTilesRepository repository;

        public MBTiles(TileSourceConfiguration configuration)
        {
            // TODO: configuration values priority:
            // 1. Default values for MBTiles.
            // 2. Actual values (MBTiles metadata).
            // 3. Values from configuration file.

            this.configuration = new TileSourceConfiguration
            {
                Format = configuration.Format,
                Name = configuration.Name,
                Title = configuration.Title,
                Tms = configuration.Tms,
                Location = configuration.Location,
                ContentType = Utils.TileFormatToContentType(configuration.Format), // TODO: from db metadata
            };

            var connectionString = CreateSqliteConnectionString(this.configuration.Location);
            this.repository = new MBTilesRepository(connectionString);
        }

        #region ITileSource implementation

        async Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            return await this.repository.ReadTileDataAsync(x, this.configuration.Tms ? y : Utils.FlipYCoordinate(y, z), z);
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
        /// Creates connection string for SQLite MBTiles database.
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
