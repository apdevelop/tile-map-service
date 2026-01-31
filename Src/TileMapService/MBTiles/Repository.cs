using System;
using System.Collections.Generic;

using Microsoft.Data.Sqlite;

namespace TileMapService.MBTiles
{
    /// <summary>
    /// Repository for MBTiles (<see href="https://github.com/mapbox/mbtiles-spec">MBTiles Specification</see>) database access.
    /// </summary>
    /// <remarks>
    /// Supports only Spherical Mercator tile grid and TMS tiling scheme (Y axis is going up).
    /// SQLite doesn't support asynchronous I/O, so the async ADO.NET methods will execute synchronously in Microsoft.Data.Sqlite.
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

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="path">Full path to MBTiles database file.</param>
        /// <param name="isFullAccess">Allows database modification if true.</param>
        public Repository(string path, bool isFullAccess = false)
        {
            connectionString = new SqliteConnectionStringBuilder
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
        public byte[]? ReadTile(int tileColumn, int tileRow, int zoomLevel)
        {
            const string ReadTileDataCommandText = $"SELECT {ColumnTileData} FROM {TableTiles} WHERE ({ColumnZoomLevel} = @zoomLevel) AND ({ColumnTileColumn} = @tileColumn) AND ({ColumnTileRow} = @tileRow)";

            using var connection = new SqliteConnection(connectionString);
            using var command = new SqliteCommand(ReadTileDataCommandText, connection);
            command.Parameters.AddRange(
            [
                new SqliteParameter("@tileColumn", tileColumn),
                new SqliteParameter("@tileRow", tileRow),
                new SqliteParameter("@zoomLevel", zoomLevel),
            ]);

            connection.Open();
            using var reader = command.ExecuteReader();
            return reader.Read() && !reader.IsDBNull(0) ? (byte[])reader[0] : null;
        }

        public byte[]? ReadFirstTile()
        {
            const string ReadFirstTileCommandText = $"SELECT {ColumnTileData} FROM {TableTiles} LIMIT 1";

            using var connection = new SqliteConnection(connectionString);
            using var command = new SqliteCommand(ReadFirstTileCommandText, connection);

            connection.Open();
            using var reader = command.ExecuteReader();
            return reader.Read() && !reader.IsDBNull(0) ? (byte[])reader[0] : null;
        }

        public (int Min, int Max)? ReadZoomLevelRange()
        {
            const string ReadMinMaxCommandText = $"SELECT MIN({ColumnZoomLevel}), MAX({ColumnZoomLevel}) FROM {TableTiles}";

            using var connection = new SqliteConnection(connectionString);
            using var command = new SqliteCommand(ReadMinMaxCommandText, connection);

            connection.Open();
            using var reader = command.ExecuteReader();
            return reader.Read() && !reader.IsDBNull(0) && !reader.IsDBNull(1)
                ? (Min: reader.GetInt32(0), Max: reader.GetInt32(1))
                : null;
        }

        /// <summary>
        /// Reads all metadata key/value items from database.
        /// </summary>
        /// <returns>Metadata records.</returns>
        public MetadataItem[] ReadMetadata()
        {
            const string ReadMetadataCommandText = $"SELECT {ColumnMetadataName}, {ColumnMetadataValue} FROM {TableMetadata}";

            var result = new List<MetadataItem>();

            using var connection = new SqliteConnection(connectionString);
            using var command = new SqliteCommand(ReadMetadataCommandText, connection);

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    throw new InvalidOperationException("Metadata name cannot be NULL.");
                }

                result.Add(new MetadataItem(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
            }

            return [.. result];
        }

        #endregion

        #region Create / Update methods

        public static Repository CreateEmptyDatabase(string path)
        {
            var repository = new Repository(path, true);

            const string CreateMetadataCommand = $"CREATE TABLE {TableMetadata} ({ColumnMetadataName} text, {ColumnMetadataValue} text)";
            repository.ExecuteSqlQuery(CreateMetadataCommand);

            const string CreateTilesCommand = $"CREATE TABLE {TableTiles} ({ColumnZoomLevel} integer, {ColumnTileColumn} integer, {ColumnTileRow} integer, {ColumnTileData} blob)";
            repository.ExecuteSqlQuery(CreateTilesCommand);

            const string CreateTileIndexCommand = $"CREATE UNIQUE INDEX {IndexTile} ON {TableTiles} ({ColumnZoomLevel}, {ColumnTileColumn}, {ColumnTileRow})";
            repository.ExecuteSqlQuery(CreateTileIndexCommand);

            return repository;
        }

        /// <summary>
        /// Inserts tile image with coordinates into 'tiles' table.
        /// </summary>
        /// <param name="tileColumn">Tile X coordinate (column).</param>
        /// <param name="tileRow">Tile Y coordinate (row), Y axis goes up from the bottom (TMS scheme).</param>
        /// <param name="zoomLevel">Tile Z coordinate (zoom level).</param>
        /// <param name="tileData">Tile image contents.</param>
        public void AddTile(int tileColumn, int tileRow, int zoomLevel, byte[] tileData)
        {
            const string InsertTileCommandText = @$"INSERT INTO {TableTiles} 
                    ({ColumnTileColumn}, {ColumnTileRow}, {ColumnZoomLevel}, {ColumnTileData}) 
                    VALUES
                    (@tileColumn, @tileRow, @zoomLevel, @tileData)";

            using var connection = new SqliteConnection(connectionString);
            using var command = new SqliteCommand(InsertTileCommandText, connection);
            command.Parameters.AddRange(
            [
                new SqliteParameter("@tileColumn", tileColumn),
                new SqliteParameter("@tileRow", tileRow),
                new SqliteParameter("@zoomLevel", zoomLevel),
                new SqliteParameter("@tileData", tileData),
            ]);

            connection.Open();
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts given metadata item into 'metadata' table.
        /// </summary>
        /// <param name="item">Key/value metadata item.</param>
        public void AddMetadataItem(MetadataItem item)
        {
            const string InsertMetadataCommandText = @$"INSERT INTO {TableMetadata} 
                    ({ColumnMetadataName}, {ColumnMetadataValue}) 
                    VALUES
                    (@{ColumnMetadataName}, @{ColumnMetadataValue})";

            using var connection = new SqliteConnection(connectionString);
            using var command = new SqliteCommand(InsertMetadataCommandText, connection);
            command.Parameters.AddRange(
            [
                new SqliteParameter($"@{ColumnMetadataName}", item.Name),
                new SqliteParameter($"@{ColumnMetadataValue}", item.Value),
            ]);

            connection.Open();
            command.ExecuteNonQuery();
        }

        private void ExecuteSqlQuery(string commandText)
        {
            using var connection = new SqliteConnection(connectionString);
            using var command = new SqliteCommand(commandText, connection);
            connection.Open();
            command.ExecuteNonQuery();
        }

        #endregion
    }
}
