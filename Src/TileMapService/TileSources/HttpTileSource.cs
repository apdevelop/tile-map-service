using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                ContentType = Utils.EntitiesConverter.TileFormatToContentType(this.configuration.Format), // TODO: from metadata
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
                    case TileSourceConfiguration.TypeXyz: { url = GetTileXyzUrl(this.configuration.Location, x, this.configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z), z); break; }
                    case TileSourceConfiguration.TypeTms: { url = GetTileTmsUrl(this.configuration.Location, this.configuration.Format, x, this.configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z), z); break; }
                    case TileSourceConfiguration.TypeWmts: { url = GetTileWmtsUrl(this.configuration.Location, this.configuration.ContentType, x, this.configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z), z); break; }
                    case TileSourceConfiguration.TypeWms: { url = GetTileWmsUrl(this.configuration.Location, this.configuration.ContentType, x, this.configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z), z); break; }
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
            // TODO: separate WMTS utility class
            const string WmtsQueryService = "service";
            const string WmtsQueryVersion = "version";
            const string WmtsQueryRequest = "request";
            const string WmtsQueryFormat = "format";

            const string WmtsQueryTileMatrix = "tilematrix";
            const string WmtsQueryTileCol = "tilecol";
            const string WmtsQueryTileRow = "tilerow";

            const string WMTS = "WMTS";
            const string Version100 = "1.0.0";
            const string GetTile = "GetTile";

            var baseUri = Utils.UrlHelper.GetQueryBase(baseUrl);
            var items = Utils.UrlHelper.GetQueryParameters(baseUrl);

            items.RemoveAll(kvp => kvp.Key == WmtsQueryFormat);
            items.RemoveAll(kvp => kvp.Key == WmtsQueryTileMatrix);
            items.RemoveAll(kvp => kvp.Key == WmtsQueryTileCol);
            items.RemoveAll(kvp => kvp.Key == WmtsQueryTileRow);

            var qb = new QueryBuilder(items);
            if (!items.Any(kvp => kvp.Key == WmtsQueryService))
            {
                qb.Add(WmtsQueryService, WMTS);
            }

            if (!items.Any(kvp => kvp.Key == WmtsQueryVersion))
            {
                qb.Add(WmtsQueryVersion, Version100);
            }

            if (!items.Any(kvp => kvp.Key == WmtsQueryRequest))
            {
                qb.Add(WmtsQueryRequest, GetTile);
            }

            qb.Add(WmtsQueryFormat, format);

            qb.Add(WmtsQueryTileMatrix, z.ToString(CultureInfo.InvariantCulture));
            qb.Add(WmtsQueryTileCol, x.ToString(CultureInfo.InvariantCulture));
            qb.Add(WmtsQueryTileRow, y.ToString(CultureInfo.InvariantCulture));

            return baseUri + qb.ToQueryString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetTileWmsUrl(string baseUrl, string format, int x, int y, int z)
        {
            // TODO: separate WMS utility class
            const string WmsQueryService = "service";
            const string WmsQueryVersion = "version";
            const string WmsQueryRequest = "request";
            const string WmsQuerySrs = "srs";
            const string WmsQueryCrs = "crs";
            const string WmsQueryBBox = "bbox";
            const string WmsQueryFormat = "format";

            const string WmsQueryWidth = "width";
            const string WmsQueryHeight = "height";

            const string WMS = "WMS";
            const string Version111 = "1.1.1";
            const string Version130 = "1.3.0";
            const string GetMap = "GetMap";

            const string EPSG3857 = "EPSG:3857"; // TODO: EPSG:4326 support

            // Rebuilding url from configuration  https://stackoverflow.com/a/43407008/1182448
            // All parameters with values are taken from provided url
            // Mandatory parameters added if needed, with default (standard) values

            var baseUri = Utils.UrlHelper.GetQueryBase(baseUrl);
            var items = Utils.UrlHelper.GetQueryParameters(baseUrl);

            // Will be replaced
            items.RemoveAll(kvp => kvp.Key == WmsQuerySrs);
            items.RemoveAll(kvp => kvp.Key == WmsQueryCrs);
            items.RemoveAll(kvp => kvp.Key == WmsQueryBBox);
            items.RemoveAll(kvp => kvp.Key == WmsQueryWidth);
            items.RemoveAll(kvp => kvp.Key == WmsQueryHeight);
            items.RemoveAll(kvp => kvp.Key == WmsQueryFormat);

            var qb = new QueryBuilder(items);
            if (!items.Any(kvp => kvp.Key == WmsQueryService))
            {
                qb.Add(WmsQueryService, WMS);
            }

            var wmsVersion = String.Empty;
            if (!items.Any(kvp => kvp.Key == WmsQueryVersion))
            {
                qb.Add(WmsQueryVersion, Version111); // Default WMS version is 1.1.1
                wmsVersion = Version111;
            }
            else
            {
                wmsVersion = items.First(kvp => kvp.Key == WmsQueryVersion).Value;
            }

            if (!items.Any(kvp => kvp.Key == WmsQueryRequest))
            {
                qb.Add(WmsQueryRequest, GetMap);
            }

            qb.Add((wmsVersion == Version130) ? WmsQueryCrs : WmsQuerySrs, EPSG3857);
            qb.Add(WmsQueryBBox, Utils.WebMercator.GetTileBounds(x, y, z).ToBBoxString());
            qb.Add(WmsQueryWidth, Utils.WebMercator.TileSize.ToString(CultureInfo.InvariantCulture));
            qb.Add(WmsQueryHeight, Utils.WebMercator.TileSize.ToString(CultureInfo.InvariantCulture));
            qb.Add(WmsQueryFormat, format); // TODO: use WMS GetCapabilities

            return baseUri + qb.ToQueryString();
        }
    }
}
