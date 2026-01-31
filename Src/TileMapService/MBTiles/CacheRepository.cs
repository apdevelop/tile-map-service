using System;
using System.Globalization;
using System.Runtime.CompilerServices;

using Microsoft.Data.Sqlite;

namespace TileMapService.MBTiles
{
    /// <summary>
    /// Special repository for MBTiles format cache.
    /// </summary>
    /// <remarks>
    /// Supports only Spherical Mercator tile grid and TMS tiling scheme (Y axis is going up).
    /// SQLite doesn't support asynchronous I/O. so the async ADO.NET methods will execute synchronously in Microsoft.Data.Sqlite.
    /// Structure of database is the same as in MapCache https://mapserver.org/mapcache/caches.html#mbtiles-caches
    /// </remarks>
    public class CacheRepository
    {
        /// <summary>
        /// Connection string for SQLite database.
        /// </summary>
        private readonly string connectionString;

        #region MBTiles database objects names

        private const string TableImages = "images";

        private const string ColumnTileId = "tile_id";

        private const string TableMap = "map";

        private const string ColumnTileColumn = "tile_column";

        private const string ColumnTileRow = "tile_row";

        private const string ColumnZoomLevel = "zoom_level";

        private const string ColumnTileData = "tile_data";

        private const string TableMetadata = "metadata";

        private const string ColumnMetadataName = "name";

        private const string ColumnMetadataValue = "value";

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheRepository"/> class.
        /// </summary>
        /// <param name="path">Full path to MBTiles database file.</param>
        public CacheRepository(string path)
        {
            connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate,
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
            string? tileId = null;

            // TODO: index / memory cache (hashset) for xyz columns
            const string Command1Text = $"SELECT {ColumnTileId} FROM {TableMap} WHERE (({ColumnZoomLevel} = @{ColumnZoomLevel}) AND ({ColumnTileColumn} = @{ColumnTileColumn}) AND ({ColumnTileRow} = @{ColumnTileRow}))";
            using var connection = new SqliteConnection(connectionString);
            using var command1 = new SqliteCommand(Command1Text, connection);
            command1.Parameters.AddRange(
            [
                new SqliteParameter($"@{ColumnTileColumn}", tileColumn),
                new SqliteParameter($"@{ColumnTileRow}", tileRow),
                new SqliteParameter($"@{ColumnZoomLevel}", zoomLevel),
            ]);

            connection.Open();
            using var reader1 = command1.ExecuteReader();
            if (reader1.Read())
            {
                tileId = (string)reader1[0];
            }

            if (tileId == null)
            {
                return null;
            }

            const string Command2Text = $"SELECT {ColumnTileData} FROM {TableImages} WHERE {ColumnTileId} = @{ColumnTileId}";
            using var command2 = new SqliteCommand(Command2Text, connection);
            command2.Parameters.Add(new SqliteParameter($"@{ColumnTileId}", tileId));

            connection.Open();
            using var reader2 = command2.ExecuteReader();
            return reader2.Read() && !reader2.IsDBNull(0) ? (byte[])reader2[0] : null;
        }

        #endregion

        #region Create / Insert methods

        public static CacheRepository CreateEmpty(string path)
        {
            var repository = new CacheRepository(path);

            const string QueryText = @"
                create table if not exists images(
                  tile_id text,
                  tile_data blob,
                  primary key(tile_id));

                create table if not exists map (
                  zoom_level integer,
                  tile_column integer,
                  tile_row integer,
                  tile_id text,
                  foreign key(tile_id) references images(tile_id),
                  primary key(tile_row,tile_column,zoom_level));

                create table if not exists metadata(
                  name text,
                  value text);

                create view if not exists tiles
                  as select
                     map.zoom_level as zoom_level,
                     map.tile_column as tile_column,
                     map.tile_row as tile_row,
                     images.tile_data as tile_data
                  from map
                     join images on images.tile_id = map.tile_id;
                ";

            repository.ExecuteSqlQuery(QueryText);

            // TODO: fill metadata table

            return repository;
        }

        public void AddTile(int tileColumn, int tileRow, int zoomLevel, byte[] tileData)
        {
            var blankColor = Utils.ImageHelper.CheckIfImageIsBlank(tileData);
            var tileId = blankColor == null
                ? TileIdFromCoordinates(tileColumn, tileRow, zoomLevel)
                : "#" + blankColor.Value.ToString("X8");

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            var isImageExists = false;

            // Check if image with this id is stored

            const string Command1Text = $"SELECT COUNT({ColumnTileId}) FROM {TableImages} WHERE {ColumnTileId} = @{ColumnTileId}";
            using var command1 = new SqliteCommand(Command1Text, connection, transaction);
            command1.Parameters.Add(new SqliteParameter($"@{ColumnTileId}", tileId));

            connection.Open();
            using var reader = command1.ExecuteReader();
            if (reader.Read())
            {
                isImageExists = Convert.ToInt64(reader[0]) == 1;
            }

            if (!isImageExists) // Store image if not exists with this id
            {
                var command2Text = @$"INSERT INTO {TableImages} 
                            ({ColumnTileId}, {ColumnTileData}) 
                            VALUES
                            (@{ColumnTileId}, @{ColumnTileData})";

                using var command2 = new SqliteCommand(command2Text, connection, transaction);
                command2.Parameters.AddRange(
                [
                    new SqliteParameter($"@{ColumnTileId}", tileId),
                    new SqliteParameter($"@{ColumnTileData}", tileData),
                ]);

                command2.ExecuteNonQuery();
            }

            const string Command3Text = @$"INSERT OR IGNORE INTO {TableMap} 
                        ({ColumnTileColumn}, {ColumnTileRow}, {ColumnZoomLevel}, {ColumnTileId}) 
                        VALUES
                        (@{ColumnTileColumn}, @{ColumnTileRow}, @{ColumnZoomLevel}, @{ColumnTileId})";

            using var command3 = new SqliteCommand(Command3Text, connection, transaction);
            command3.Parameters.AddRange(
            [
                new SqliteParameter($"@{ColumnTileColumn}", tileColumn),
                new SqliteParameter($"@{ColumnTileRow}", tileRow),
                new SqliteParameter($"@{ColumnZoomLevel}", zoomLevel),
                new SqliteParameter($"@{ColumnTileId}", tileId),
            ]);

            command3.ExecuteNonQuery();

            transaction.Commit();
        }

        private void ExecuteSqlQuery(string commandText)
        {
            using var connection = new SqliteConnection(connectionString);
            using var command = new SqliteCommand(commandText, connection);
            connection.Open();
            command.ExecuteNonQuery();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string TileIdFromCoordinates(int tileColumn, int tileRow, int zoomLevel) =>
            String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", tileColumn, tileRow, zoomLevel);

        #endregion
    }
}
