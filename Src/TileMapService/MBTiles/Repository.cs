using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace TileMapService.MBTiles
{
    /// <summary>
    /// Repository for MBTiles database access.
    /// </summary>
    /// <remarks>
    /// SQLite doesn't support asynchronous I/O. Async ADO.NET methods will execute synchronously in Microsoft.Data.Sqlite.
    /// Supports only Spherical Mercator tile grid and TMS tiling scheme (Y axis is going up).
    /// See https://github.com/mapbox/mbtiles-spec/blob/master/1.3/spec.md
    /// </remarks>
    public class Repository
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

        private const string IndexTile = "tile_index";

        // TODO: Grids / UTFGrid

        private static readonly string ReadTileDataCommandText = 
            $"SELECT {ColumnTileData} FROM {TableTiles} WHERE (({ColumnZoomLevel} = @zoom_level) AND ({ColumnTileColumn} = @tile_column) AND ({ColumnTileRow} = @tile_row))";

        private static readonly string ReadMetadataCommandText =
            $"SELECT {ColumnMetadataName}, {ColumnMetadataValue} FROM {TableMetadata}";

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="path">Full path to MBTiles database file.</param>
        /// <param name="isFullAccess">Allows database modification if true.</param>
        public Repository(string path, bool isFullAccess = false)
        {
            this.connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = isFullAccess ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared,
            }.ToString();
        }

        #region Read methods

        /// <summary>
        /// Reads tile image contents with given coordinates from database.
        /// </summary>
        /// <param name="tileColumn">Tile X coordinate (column).</param>
        /// <param name="tileRow">Tile Y coordinate (row), Y axis goes up from the bottom (TMS scheme).</param>
        /// <param name="zoomLevel">Tile Z coordinate (zoom level).</param>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/async">Async Limitations</seealso>
        /// <returns>Tile image contents.</returns>
        public byte[] ReadTileData(int tileColumn, int tileRow, int zoomLevel)
        {
            using (var connection = new SqliteConnection(this.connectionString))
            {
                using (var command = new SqliteCommand(ReadTileDataCommandText, connection))
                {
                    command.Parameters.AddRange(new[]
                    {
                        new SqliteParameter("@tile_column", tileColumn),
                        new SqliteParameter("@tile_row", tileRow),
                        new SqliteParameter("@zoom_level", zoomLevel),
                    });

                    connection.Open();
                    using (var dr = command.ExecuteReader())
                    {
                        byte[] result = null;

                        if (dr.Read())
                        {
                            result = (byte[])dr[0];
                        }

                        dr.Close();

                        return result;
                    }
                }
            }
        }

        /// <summary>
        /// Reads all metadata key/value items from database.
        /// </summary>
        /// <returns>Metadata records.</returns>
        public MetadataItem[] ReadMetadata()
        {
            using (var connection = new SqliteConnection(this.connectionString))
            {
                using (var command = new SqliteCommand(ReadMetadataCommandText, connection))
                {
                    var result = new List<MetadataItem>();

                    connection.Open();
                    using (var dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            result.Add(new MetadataItem
                            {
                                Name = dr.IsDBNull(0) ? null : dr.GetString(0),
                                Value = dr.IsDBNull(1) ? null : dr.GetString(1),
                            });
                        }

                        dr.Close();
                    }

                    return result.ToArray();
                }
            }
        }

        #endregion

        #region Create / Update methods

        public static Repository CreateEmptyDatabase(string path)
        {
            var repository = new Repository(path, true);

            var createMetadataCommand = $"CREATE TABLE {TableMetadata} ({ColumnMetadataName} text, {ColumnMetadataValue} text)";
            repository.ExecuteSqlQuery(createMetadataCommand);

            var createTilesCommand = $"CREATE TABLE {TableTiles} ({ColumnZoomLevel} integer, {ColumnTileColumn} integer, {ColumnTileRow} integer, {ColumnTileData} blob)";
            repository.ExecuteSqlQuery(createTilesCommand);

            var createTileIndexCommand = $"CREATE UNIQUE INDEX {IndexTile} ON {TableTiles} ({ColumnZoomLevel}, {ColumnTileColumn}, {ColumnTileRow})";
            repository.ExecuteSqlQuery(createTileIndexCommand);

            return repository;
        }

        public void AddTile(int tileColumn, int tileRow, int zoomLevel, byte[] tileData)
        {
            using (var connection = new SqliteConnection(this.connectionString))
            {
                var commandText = @$"INSERT INTO {TableTiles} 
                    ({ColumnTileColumn}, {ColumnTileRow}, {ColumnZoomLevel}, {ColumnTileData}) 
                    VALUES
                    (@{ColumnTileColumn}, @{ColumnTileRow}, @{ColumnZoomLevel}, @{ColumnTileData})";
                using (var command = new SqliteCommand(commandText, connection))
                {
                    command.Parameters.Add(new SqliteParameter($"@{ColumnTileColumn}", tileColumn));
                    command.Parameters.Add(new SqliteParameter($"@{ColumnTileRow}", tileRow));
                    command.Parameters.Add(new SqliteParameter($"@{ColumnZoomLevel}", zoomLevel));
                    command.Parameters.Add(new SqliteParameter($"@{ColumnTileData}", tileData));
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public void AddMetadataItem(MetadataItem item)
        {
            using (var connection = new SqliteConnection(this.connectionString))
            {
                var commandText = @$"INSERT INTO {TableMetadata} 
                    ({ColumnMetadataName}, {ColumnMetadataValue}) 
                    VALUES
                    (@{ColumnMetadataName}, @{ColumnMetadataValue})";
                using (var command = new SqliteCommand(commandText, connection))
                {
                    command.Parameters.Add(new SqliteParameter($"@{ColumnMetadataName}", item.Name));
                    command.Parameters.Add(new SqliteParameter($"@{ColumnMetadataValue}", item.Value));
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        private void ExecuteSqlQuery(string commandText)
        {
            using (var connection = new SqliteConnection(this.connectionString))
            {
                using (var command = new SqliteCommand(commandText, connection))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        #endregion
    }
}
