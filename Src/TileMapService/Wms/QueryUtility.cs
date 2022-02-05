using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TileMapService.Wms
{
    class QueryUtility
    {
        const string WmsQueryService = "service";
        const string WmsQueryVersion = "version";
        const string WmsQueryRequest = "request";
        const string WmsQuerySrs = "srs";
        const string WmsQueryCrs = "crs";
        const string WmsQueryBBox = "bbox";
        const string WmsQueryFormat = "format";

        const string WmsQueryWidth = "width";
        const string WmsQueryHeight = "height";

        const string EPSG3857 = Utils.SrsCodes.EPSG3857; // TODO: EPSG:4326 support

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetTileUrl(string baseUrl, string format, int x, int y, int z)
        {
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
                qb.Add(WmsQueryService, Identifiers.Wms);
            }

            var wmsVersion = String.Empty;
            if (!items.Any(kvp => kvp.Key == WmsQueryVersion))
            {
                qb.Add(WmsQueryVersion, Identifiers.Version111); // Default WMS version is 1.1.1
                wmsVersion = Identifiers.Version111;
            }
            else
            {
                wmsVersion = items.First(kvp => kvp.Key == WmsQueryVersion).Value;
            }

            if (!items.Any(kvp => kvp.Key == WmsQueryRequest))
            {
                qb.Add(WmsQueryRequest, Identifiers.GetMap);
            }

            qb.Add((wmsVersion == Identifiers.Version130) ? WmsQueryCrs : WmsQuerySrs, EPSG3857);
            qb.Add(WmsQueryBBox, Utils.WebMercator.GetTileBounds(x, y, z).ToBBoxString());
            qb.Add(WmsQueryWidth, Utils.WebMercator.TileSize.ToString(CultureInfo.InvariantCulture));
            qb.Add(WmsQueryHeight, Utils.WebMercator.TileSize.ToString(CultureInfo.InvariantCulture));
            qb.Add(WmsQueryFormat, format); // TODO: use WMS GetCapabilities

            return baseUri + qb.ToQueryString();
        }

        public static string GetCapabilitiesWmsUrl(string baseUrl)
        {
            // Rebuilding url from configuration  https://stackoverflow.com/a/43407008/1182448
            // All parameters with values are taken from provided url
            // Mandatory parameters added if needed, with default (standard) values

            var baseUri = Utils.UrlHelper.GetQueryBase(baseUrl);
            var items = Utils.UrlHelper.GetQueryParameters(baseUrl);

            items.RemoveAll(kvp => kvp.Key == WmsQuerySrs);
            items.RemoveAll(kvp => kvp.Key == WmsQueryCrs);
            items.RemoveAll(kvp => kvp.Key == WmsQueryBBox);
            items.RemoveAll(kvp => kvp.Key == WmsQueryWidth);
            items.RemoveAll(kvp => kvp.Key == WmsQueryHeight);
            items.RemoveAll(kvp => kvp.Key == WmsQueryFormat);

            var qb = new QueryBuilder(items);
            if (!items.Any(kvp => kvp.Key == WmsQueryService))
            {
                qb.Add(WmsQueryService, Identifiers.Wms);
            }

            if (!items.Any(kvp => kvp.Key == WmsQueryRequest))
            {
                qb.Add(WmsQueryRequest, Identifiers.GetCapabilities);
            }

            return baseUri + qb.ToQueryString();
        }
    }
}
