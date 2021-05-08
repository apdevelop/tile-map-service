using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with tiles from other HTTP (also TMS, WMTS) service.
    /// </summary>
    class HttpTileSource : ITileSource
    {
        private TileSourceConfiguration configuration;

        private HttpClient client;

        public HttpTileSource(TileSourceConfiguration configuration)
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
            // 1. Default values for http source type.
            // 2. Actual values (from source metadata).
            // 3. Values from configuration file - overrides given above, if provided.

            // TODO: read metadata for TMS and WMTS sources

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                this.configuration.Id :
                this.configuration.Title;

            var minZoom = this.configuration.MinZoom ?? 0;
            var maxZoom = this.configuration.MaxZoom ?? 24;

            // Default is tms=false for simple XYZ tile services
            var tms = this.configuration.Tms ?? (this.configuration.Type.ToLowerInvariant() == TileSourceConfiguration.TypeTms);

            // Re-create configuration
            this.configuration = new TileSourceConfiguration
            {
                Id = this.configuration.Id,
                Type = this.configuration.Type,
                Format = this.configuration.Format, // TODO: from metadata
                Title = title,
                Tms = tms,
                Location = this.configuration.Location,
                ContentType = Utils.TileFormatToContentType(this.configuration.Format), // TODO: from metadata
                MinZoom = minZoom,
                MaxZoom = maxZoom,
            };

            this.client = new HttpClient(); // TODO: custom headers from configuration

            return Task.CompletedTask;
        }

        async Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            if ((z < this.configuration.MinZoom) || (z > this.configuration.MaxZoom))
            {
                return null;
            }
            else
            {
                string url;
                switch (this.configuration.Type.ToLowerInvariant())
                {
                    case TileSourceConfiguration.TypeXyz: { url = GetTileXyzUrl(this.configuration.Location, x, this.configuration.Tms.Value ? y : Utils.FlipYCoordinate(y, z), z); break; }
                    case TileSourceConfiguration.TypeTms: { url = GetTileTmsUrl(this.configuration.Location, this.configuration.Format, x, this.configuration.Tms.Value ? y : Utils.FlipYCoordinate(y, z), z); break; }
                    case TileSourceConfiguration.TypeWmts: { url = GetTileWmtsUrl(this.configuration.Location, this.configuration.ContentType, x, this.configuration.Tms.Value ? y : Utils.FlipYCoordinate(y, z), z); break; }
                    default: throw new ArgumentOutOfRangeException(nameof(this.configuration.Type), $"Unknown tile source type '{this.configuration.Type}'");
                }

                var response = await client.GetAsync(url);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    return null;
                }
            }
        }

        TileSourceConfiguration ITileSource.Configuration
        {
            get
            {
                return this.configuration;
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetTileXyzUrl(string location, int x, int y, int z)
        {
            return location
                    .Replace("{x}", x.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{y}", y.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{z}", z.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetTileTmsUrl(string location, string format, int x, int y, int z)
        {
            return location +
                "/" + z.ToString(CultureInfo.InvariantCulture) +
                "/" + x.ToString(CultureInfo.InvariantCulture) +
                "/" + y.ToString(CultureInfo.InvariantCulture) +
                "." + format; // TODO: get actual source extension
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetTileWmtsUrl(string location, string format, int x, int y, int z)
        {
            return location +
                "&Service=WMTS" +
                "&Request=GetTile" +
                "&Version=1.0.0" +
                "&Format=" + format.Replace("/", "%2F") +
                "&TileMatrix=" + z.ToString(CultureInfo.InvariantCulture) +
                "&TileCol=" + x.ToString(CultureInfo.InvariantCulture) +
                "&TileRow=" + y.ToString(CultureInfo.InvariantCulture);
        }
    }
}
