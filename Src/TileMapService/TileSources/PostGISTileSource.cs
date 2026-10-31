using System;
using System.Threading;
using System.Threading.Tasks;

using Npgsql;

using TileMapService.Models;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with vector tiles (MVT) from PostgreSQL database with PostGIS extension.
    /// </summary>
    class PostGisTileSource : ITileSource
    {
        private SourceConfiguration configuration;

        private readonly string connectionString;

        public PostGisTileSource(SourceConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException("Source identifier is null or empty string.");
            }

            if (string.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException("Source location is null or empty string.");
            }

            this.connectionString = configuration.Location;
            this.configuration = configuration; // Will be changed later in InitAsync
        }

        #region ITileSource implementation

        Task ITileSource.InitAsync()
        {
            var title = string.IsNullOrEmpty(configuration.Title)
                ? configuration.Id
                : configuration.Title;

            var minZoom = configuration.MinZoom ?? 0;
            var maxZoom = configuration.MaxZoom ?? 20;

            // Re-create configuration
            configuration = new SourceConfiguration
            {
                Id = configuration.Id,
                Type = configuration.Type,
                Format = ImageFormats.MapboxVectorTile,
                Title = title,
                Abstract = configuration.Abstract,
                Attribution = configuration.Attribution,
                Tms = configuration.Tms ?? true,
                Srs = Utils.SrsCodes.EPSG3857,
                Location = configuration.Location,
                ContentType = Utils.EntitiesConverter.TileFormatToContentType(ImageFormats.MapboxVectorTile),
                MinZoom = minZoom,
                MaxZoom = maxZoom,
                GeographicalBounds = null,
                TileWidth = Utils.WebMercator.DefaultTileWidth,
                TileHeight = Utils.WebMercator.DefaultTileHeight,
                Cache = null, // TODO: ? possible to implement
                PostGis = configuration.PostGis,
            };

            return Task.CompletedTask;
        }

        async Task<byte[]?> ITileSource.GetTileAsync(int x, int y, int z, CancellationToken cancellationToken)
        {
            if (z < configuration.MinZoom ||
                z > configuration.MaxZoom ||
                x < 0 ||
                y < 0 ||
                x > Utils.WebMercator.TileCount(z) ||
                y > Utils.WebMercator.TileCount(z))
            {
                return null;
            }
            else
            {
                var postgis = configuration.PostGis ?? throw new InvalidOperationException("PostGIS connection options must be defined.");

                if (string.IsNullOrWhiteSpace(postgis.Table))
                {
                    throw new InvalidOperationException("Table name must be defined.");
                }

                if (string.IsNullOrWhiteSpace(postgis.Geometry))
                {
                    throw new InvalidOperationException("Table geometry field must be defined.");
                }

                var fields = !string.IsNullOrWhiteSpace(postgis.Fields)
                    ? postgis.Fields.Split(FieldsSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : null;

                return await this.ReadPostGisVectorTileAsync(
                    postgis.Table,
                    postgis.Geometry,
                    fields,
                    x,
                    Utils.WebMercator.FlipYCoordinate(y, z),
                    z,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        SourceConfiguration ITileSource.Configuration => configuration;

        private static readonly string[] FieldsSeparator = [","];

        #endregion

        private async Task<byte[]?> ReadPostGisVectorTileAsync(
            string tableName,
            string geometry,
            string[]? fields,
            int x,
            int y,
            int z,
            CancellationToken cancellationToken = default)
        {
            // https://blog.crunchydata.com/blog/dynamic-vector-tiles-from-postgis
            // https://postgis.net/docs/ST_AsMVT.html
            // https://postgis.net/docs/ST_AsMVTGeom.html

            var commandText = $@"
                    WITH mvtgeom AS
                    (
                        SELECT ST_AsMVTGeom({geometry}, ST_TileEnvelope({z},{x},{y})) AS geom 
                            {(fields != null && fields.Length > 0 ? ", " + string.Join(',', fields) : string.Empty)}
                        FROM ""{tableName}""
                        WHERE ST_Intersects({geometry}, ST_TileEnvelope({z},{x},{y}))
                    )
                    SELECT ST_AsMVT(mvtgeom.*, '{configuration.Id}')
                    FROM mvtgeom";

            // TODO: ? other SRS, using ST_Transform if needed
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var command = new NpgsqlCommand(commandText, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
                ? reader[0] as byte[]
                : null;
        }
    }
}
