using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with tiles reading from other HTTP service.
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

            this.configuration = configuration; // May be changed later in InitAsync
        }

        #region ITileSource implementation

        Task ITileSource.InitAsync()
        {
            // Configuration values priority:
            // 1. Default values for http source.
            // 2. Actual values (from source metadata).
            // 3. Values from configuration file - overrides given above, if provided.

            // TODO: read metadata for TMS and WMTS sources

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                this.configuration.Id :
                this.configuration.Title;

            var minZoom = this.configuration.MinZoom.HasValue ?
                this.configuration.MinZoom.Value : 0;

            var maxZoom = this.configuration.MaxZoom.HasValue ?
                this.configuration.MaxZoom.Value : 24;

            // Re-create configuration
            this.configuration = new TileSourceConfiguration
            {
                Id = this.configuration.Id,
                Type = this.configuration.Type,
                Format = this.configuration.Format, // TODO: from metadata
                Title = title,
                Tms = this.configuration.Tms ?? false, // Default is TMS=false for simple XYZ tile services
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
                string url = null;
                switch (this.configuration.Type.ToLowerInvariant())
                {
                    case TileSourceConfiguration.TypeHttp: { url = GetTileXyzUrl(this.configuration.Location, x, this.configuration.Tms.Value ? y : Utils.FlipYCoordinate(y, z), z);  break; }
                    // TODO: case TileSourceConfiguration.TypeTms: { url = GetTileXyzUrl(this.configuration.Location, x, this.configuration.Tms.Value ? y : Utils.FlipYCoordinate(y, z), z); break; }
                    // TODO: case TileSourceConfiguration.TypeWmts: { url = GetTileXyzUrl(this.configuration.Location, x, this.configuration.Tms.Value ? y : Utils.FlipYCoordinate(y, z), z); break; }
                    default: throw new ArgumentOutOfRangeException(nameof(this.configuration.Type), $"Unknown tile source type '{this.configuration.Type}'");
                }

                var response = await client.GetAsync(url);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var buffer = await response.Content.ReadAsByteArrayAsync();

                    return buffer;
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

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static string GetTileXyzUrl(string template, int x, int y, int z)
        {
            // TODO: detailed type http -> TMS, WMTS requests
            return template
                    .Replace("{x}", x.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{y}", y.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{z}", z.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
