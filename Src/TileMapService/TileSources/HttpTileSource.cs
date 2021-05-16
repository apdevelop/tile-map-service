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

            // TODO: read metadata for TMS, WMTS and WMS sources

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
                    case TileSourceConfiguration.TypeWms: { url = GetTileWmsUrl(this.configuration.Location, this.configuration.ContentType, x, this.configuration.Tms.Value ? y : Utils.FlipYCoordinate(y, z), z); break; }
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
        private static string GetTileXyzUrl(string baseUrl, int x, int y, int z)
        {
            return baseUrl
                    .Replace("{x}", x.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{y}", y.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{z}", z.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetTileTmsUrl(string baseUrl, string format, int x, int y, int z)
        {
            return baseUrl +
                "/" + z.ToString(CultureInfo.InvariantCulture) +
                "/" + x.ToString(CultureInfo.InvariantCulture) +
                "/" + y.ToString(CultureInfo.InvariantCulture) +
                "." + format; // TODO: get actual source extension
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetTileWmtsUrl(string baseUrl, string format, int x, int y, int z)
        {
            return baseUrl +
                "&Service=WMTS" +
                "&Request=GetTile" +
                "&Version=1.0.0" +
                "&Format=" + format.Replace("/", "%2F") +
                "&TileMatrix=" + z.ToString(CultureInfo.InvariantCulture) +
                "&TileCol=" + x.ToString(CultureInfo.InvariantCulture) +
                "&TileRow=" + y.ToString(CultureInfo.InvariantCulture);
        }

        // Assumes SRS = EPSG:3857 / Web Mercator / Spherical Mercator
        // Based on https://docs.microsoft.com/en-us/bingmaps/articles/bing-maps-tile-system

        // TODO: ? separate class

        private const int TileSize = 256; // TODO: support for high resolution tiles

        private static readonly double EarthRadius = 6378137.0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetTileWmsUrl(string baseUrl, string format, int x, int y, int z)
        {
            // TODO: EPSG:4326 support
            // TODO: better url processing, simplify config (srs/crs depending on version), url encode, checking values duplication
            // version, layers, styles, srs/crs must be defined in url template (location)
            var minx = TileXtoEpsg3857X(x, z);
            var maxx = TileXtoEpsg3857X(x + 1, z);
            var miny = TileXtoEpsg3857Y(y + 1, z);
            var maxy = TileXtoEpsg3857Y(y, z);
            var bbox = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", minx, miny, maxx, maxy);

            var result = baseUrl +
                "&service=WMS" +
                "&request=GetMap" +
                "&bbox=" + bbox +
                "&width=" + TileSize.ToString(CultureInfo.InvariantCulture) +
                "&height=" + TileSize.ToString(CultureInfo.InvariantCulture) +
                "&format=" + format.Replace("/", "%2F"); // TODO: use GetCapabilities

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double TileXtoEpsg3857X(int tileX, int zoomLevel)
        {
            var mapSize = (double)MapSize(zoomLevel);
            var pixelX = tileX * TileSize;
            var x = (MathHelper.Clip(pixelX, 0.0, mapSize) / mapSize) - 0.5;
            var longitude = 360.0 * x;

            return EarthRadius * MathHelper.DegreesToRadians(longitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double TileXtoEpsg3857Y(int tileY, int zoomLevel)
        {
            var mapSize = (double)MapSize(zoomLevel);
            var pixelY = tileY * TileSize;
            var y = 0.5 - (MathHelper.Clip(pixelY, 0.0, mapSize) / mapSize);
            var latitude = 90.0 - 360.0 * Math.Atan(Math.Exp(-y * 2.0 * Math.PI)) / Math.PI;

            return EarthRadius * MathHelper.Artanh(Math.Sin(MathHelper.DegreesToRadians(latitude)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MapSize(int zoomLevel)
        {
            return TileSize << zoomLevel;
        }
    }
}
