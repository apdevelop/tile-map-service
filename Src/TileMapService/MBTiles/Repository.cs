using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;

namespace TileMapService.MBTiles
{
    /// <summary>
    /// Repository for MBTiles 1.3 (SQLite) database access (in read only mode).
    /// </summary>
    /// <remarks>
    /// See https://github.com/mapbox/mbtiles-spec/blob/master/1.3/spec.md
    /// </remarks>
    class Repository
    {
        /// <summary>
        /// Connection string for SQLite database.
        /// </summary>
        private readonly string connectionString;

        #region MBTiles database objects names

        private const string TableTiles = "tiles";

        private const string ColumnTileColumn = "tile_column";

        private const string ColumnTileRow = "tile_row";

        private const string ColumnZoomLevel = "zoom_level";

        private const string ColumnTileData = "tile_data";

        private const string TableMetadata = "metadata";

        private const string ColumnMetadataName = "name";

        private const string ColumnMetadataValue = "value";

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="connectionString">Connection string for SQLite database.</param>
        public Repository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Asynchronously reads tile image contents with given coordinates from source.
        /// </summary>
        /// <param name="tileColumn">Tile X coordinate (column).</param>
        /// <param name="tileRow">Tile Y coordinate (row), Y axis goes up from the bottom (TMS scheme).</param>
        /// <param name="zoomLevel">Tile Z coordinate (zoom level).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task<byte[]> ReadTileDataAsync(int tileColumn, int tileRow, int zoomLevel)
        {
            byte[] result = null;

            var commandText = $"SELECT {ColumnTileData} FROM {TableTiles} WHERE (({ColumnZoomLevel} = @zoom_level) AND ({ColumnTileColumn} = @tile_column) AND ({ColumnTileRow} = @tile_row))";
            using (var connection = new SqliteConnection(this.connectionString))
            {
                using (var command = new SqliteCommand(commandText, connection))
                {
                    command.Parameters.AddRange(new[]
                    {
                        new SqliteParameter("@tile_column", tileColumn),
                        new SqliteParameter("@tile_row", tileRow),
                        new SqliteParameter("@zoom_level", zoomLevel),
                    });

                    await connection.OpenAsync().ConfigureAwait(false);
                    using (var dr = await command.ExecuteReaderAsync().ConfigureAwait(false)) // TODO: fine tune with CommandBehavior ?
                    {
                        if (await dr.ReadAsync().ConfigureAwait(false))
                        {
                            result = (byte[])dr[0];
                        }

                        dr.Close();
                    }
                }

                connection.Close();
            }

            return result;
        }

        /// <summary>
        /// Asynchronously reads all metadata key/value items.
        /// </summary>
        /// <returns>Metadata records.</returns>
        public async Task<MetadataItem[]> ReadMetadataAsync()
        {
            var result = new List<MetadataItem>();
            using (var connection = new SqliteConnection(this.connectionString))
            {
                var commandText = $"SELECT {ColumnMetadataName}, {ColumnMetadataValue} FROM {TableMetadata}";
                using (var command = new SqliteCommand(commandText, connection))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    using (var dr = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        while (await dr.ReadAsync().ConfigureAwait(false))
                        {
                            result.Add(new MetadataItem
                            {
                                Name = dr.IsDBNull(0) ? null : dr.GetString(0),
                                Value = dr.IsDBNull(1) ? null : dr.GetString(1),
                            });
                        }

                        await dr.CloseAsync().ConfigureAwait(false);
                    }

                    await connection.CloseAsync().ConfigureAwait(false);
                }
            }

            return result.ToArray();
        }
    }
}
