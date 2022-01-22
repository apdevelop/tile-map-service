using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with tiles stored in MBTiles SQLite database.
    /// </summary>
    /// <remarks>
    /// Supports only Spherical Mercator tile grid and TMS tiling scheme (Y axis is going up).
    /// See https://github.com/mapbox/mbtiles-spec/blob/master/1.3/spec.md
    /// </remarks>
    class MBTilesTileSource : ITileSource
    {
        private SourceConfiguration configuration;

        private MBTiles.Repository repository;

        public MBTilesTileSource(SourceConfiguration configuration)
        {
            if (String.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException();
            }

            if (String.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException();
            }

            this.configuration = configuration; // Will be changed later in InitAsync
        }

        #region ITileSource implementation

        Task ITileSource.InitAsync()
        {
            // Configuration values priority:
            // 1. Default values for MBTiles source type.
            // 2. Actual values (MBTiles metadata).
            // 3. Values from configuration file - overrides given above, if provided.

            this.repository = new MBTiles.Repository(configuration.Location, false);
            var metadata = new MBTiles.Metadata(this.repository.ReadMetadata());

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                    (!String.IsNullOrEmpty(metadata.Name) ? metadata.Name : this.configuration.Id) :
                    this.configuration.Title;

            var format = String.IsNullOrEmpty(this.configuration.Format) ?
                    (!String.IsNullOrEmpty(metadata.Format) ? metadata.Format : ImageFormats.Png) :
                    this.configuration.Format;

            // Re-create configuration
            this.configuration = new SourceConfiguration
            {
                Id = this.configuration.Id,
                Type = this.configuration.Type,
                Format = format,
                Title = title,
                Tms = this.configuration.Tms ?? true, // Default true for the MBTiles, following the Tile Map Service Specification.
                Srs = Utils.SrsCodes.EPSG3857, // MBTiles supports only Spherical Mercator tile grid
                Location = this.configuration.Location,
                ContentType = Utils.EntitiesConverter.TileFormatToContentType(format),
                MinZoom = this.configuration.MinZoom ?? metadata.MinZoom ?? 0, // TODO: ? Check actual SELECT MIN/MAX(zoom_level) ?
                MaxZoom = this.configuration.MaxZoom ?? metadata.MaxZoom ?? 24,
            };

            return Task.CompletedTask;
        }

        Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            var tileData = this.repository.ReadTileData(x, this.configuration.Tms.Value ?
                y :
                Utils.WebMercator.FlipYCoordinate(y, z), z);

            // TODO: pass gzipped data as-is with setting HTTP headers?
            // pbf as a format refers to gzip-compressed vector tile data in Mapbox Vector Tile format, 
            // which uses Google Protocol Buffers as encoding format.
            if (this.configuration.Format == ImageFormats.Protobuf)
            {
                using (var compressedStream = new MemoryStream(tileData))
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    zipStream.CopyTo(resultStream);
                    tileData = resultStream.ToArray();
                }
            }

            return Task.FromResult(tileData);
        }

        SourceConfiguration ITileSource.Configuration
        {
            get
            {
                return this.configuration;
            }
        }

        #endregion
    }
}
