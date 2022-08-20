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
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="path">Full path to MBTiles database file.</param>
        public CacheRepository(string path)
        {
            this.connectionString = new SqliteConnectionStringBuilder
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
            using var connection = new SqliteConnection(this.connectionString);
            byte[]? result = null;
            string? tileId = null;

            // TODO: index / memory cache (hashset) for xyz columns
            var command1Text = $"SELECT {ColumnTileId} FROM {TableMap} WHERE (({ColumnZoomLevel} = @{ColumnZoomLevel}) AND ({ColumnTileColumn} = @{ColumnTileColumn}) AND ({ColumnTileRow} = @{ColumnTileRow}))";
            using (var command = new SqliteCommand(command1Text, connection))
            {
                command.Parameters.AddRange(new[]
                {
                    new SqliteParameter($"@{ColumnTileColumn}", tileColumn),
                    new SqliteParameter($"@{ColumnTileRow}", tileRow),
                    new SqliteParameter($"@{ColumnZoomLevel}", zoomLevel),
                });

                connection.Open();
                using (var dr = command.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        tileId = (string)dr[0];
                    }

                    dr.Close();
                }

                if (tileId == null)
                {
                    return null;
                }
            }

            {
                var command2Text = $"SELECT {ColumnTileData} FROM {TableImages} WHERE {ColumnTileId} = @{ColumnTileId}";
                using var command = new SqliteCommand(command2Text, connection);
                command.Parameters.Add(new SqliteParameter($"@{ColumnTileId}", tileId));

                connection.Open();
                using var dr = command.ExecuteReader();
                if (dr.Read())
                {
                    result = (byte[])dr[0];
                }

                dr.Close();
            }

            return result;
        }

        #endregion

        #region Create / Insert methods

        public static CacheRepository CreateEmpty(string path)
        {
            var repository = new CacheRepository(path);

            const string queryText = @"
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

            repository.ExecuteSqlQuery(queryText);

            // TODO: fill metadata table

            return repository;
        }

        public void AddTile(int tileColumn, int tileRow, int zoomLevel, byte[] tileData)
        {
            var blankColor = Utils.ImageHelper.CheckIfImageIsBlank(tileData);
            var tileId = (blankColor == null) ?
                TileIdFromCoordinates(tileColumn, tileRow, zoomLevel) :
                "#" + blankColor.Value.ToString("X8");

            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            var isImageExists = false;

            // Check if image with this id is stored
            {
                var command0Text = $"SELECT COUNT({ColumnTileId}) FROM {TableImages} WHERE {ColumnTileId} = @{ColumnTileId}";
                using var command = new SqliteCommand(command0Text, connection, transaction);
                command.Parameters.Add(new SqliteParameter($"@{ColumnTileId}", tileId));

                connection.Open();
                using var dr = command.ExecuteReader();
                if (dr.Read())
                {
                    isImageExists = Convert.ToInt64(dr[0]) == 1;
                }

                dr.Close();
            }

            if (!isImageExists) // Store image if not exists with this id
            {
                var command1Text = @$"INSERT INTO {TableImages} 
                            ({ColumnTileId}, {ColumnTileData}) 
                            VALUES
                            (@{ColumnTileId}, @{ColumnTileData})";

                using var command = new SqliteCommand(command1Text, connection, transaction);
                command.Parameters.Add(new SqliteParameter($"@{ColumnTileId}", tileId));
                command.Parameters.Add(new SqliteParameter($"@{ColumnTileData}", tileData));
                command.ExecuteNonQuery();
            }

            {
                var command2Text = @$"INSERT OR IGNORE INTO {TableMap} 
                        ({ColumnTileColumn}, {ColumnTileRow}, {ColumnZoomLevel}, {ColumnTileId}) 
                        VALUES
                        (@{ColumnTileColumn}, @{ColumnTileRow}, @{ColumnZoomLevel}, @{ColumnTileId})";

                using var command = new SqliteCommand(command2Text, connection, transaction);
                command.Parameters.Add(new SqliteParameter($"@{ColumnTileColumn}", tileColumn));
                command.Parameters.Add(new SqliteParameter($"@{ColumnTileRow}", tileRow));
                command.Parameters.Add(new SqliteParameter($"@{ColumnZoomLevel}", zoomLevel));
                command.Parameters.Add(new SqliteParameter($"@{ColumnTileId}", tileId));
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public void AddMetadataItem(MetadataItem item)
        {
            var commandText = @$"INSERT INTO {TableMetadata} 
                    ({ColumnMetadataName}, {ColumnMetadataValue}) 
                    VALUES
                    (@{ColumnMetadataName}, @{ColumnMetadataValue})";

            using var connection = new SqliteConnection(this.connectionString);
            using var command = new SqliteCommand(commandText, connection);
            command.Parameters.Add(new SqliteParameter($"@{ColumnMetadataName}", item.Name));
            command.Parameters.Add(new SqliteParameter($"@{ColumnMetadataValue}", item.Value));
            connection.Open();
            command.ExecuteNonQuery();
        }

        private void ExecuteSqlQuery(string commandText)
        {
            using var connection = new SqliteConnection(this.connectionString);
            using var command = new SqliteCommand(commandText, connection);
            connection.Open();
            command.ExecuteNonQuery();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string TileIdFromCoordinates(int tileColumn, int tileRow, int zoomLevel) => String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", tileColumn, tileRow, zoomLevel);

        #endregion
    }
}
