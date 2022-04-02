using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Http.Extensions;

namespace TileMapService.Wmts
{
    class QueryUtility
    {
        private const string WmtsQueryService = "service";
        private const string WmtsQueryVersion = "version";
        private const string WmtsQueryRequest = "request";
        private const string WmtsQueryFormat = "format";

        internal const string WmtsQueryLayer = "layer";
        private const string WmtsQueryStyle = "style";
        private const string WmtsQueryTilematrixSet = "tilematrixset";

        private const string WmtsQueryTileMatrix = "tilematrix";
        private const string WmtsQueryTileCol = "tilecol";
        private const string WmtsQueryTileRow = "tilerow";

        public static string GetCapabilitiesUrl(string url)
        {
            // Rebuilding url from configuration  https://stackoverflow.com/a/43407008/1182448
            // All parameters with values are taken from provided url
            // Mandatory parameters added if needed, with default (standard) values

            var baseUri = Utils.UrlHelper.GetQueryBase(url);
            var items = Utils.UrlHelper.GetQueryParameters(url);

            items.RemoveAll(kvp => kvp.Key == WmtsQueryLayer);
            items.RemoveAll(kvp => kvp.Key == WmtsQueryStyle);
            items.RemoveAll(kvp => kvp.Key == WmtsQueryFormat);
            items.RemoveAll(kvp => kvp.Key == WmtsQueryTileMatrix);
            items.RemoveAll(kvp => kvp.Key == WmtsQueryTilematrixSet);

            var qb = new QueryBuilder(items);
            if (!items.Any(kvp => kvp.Key == WmtsQueryService))
            {
                qb.Add(WmtsQueryService, Identifiers.WMTS);
            }

            if (!items.Any(kvp => kvp.Key == WmtsQueryRequest))
            {
                qb.Add(WmtsQueryRequest, Identifiers.GetCapabilities);
            }

            return baseUri + qb.ToQueryString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetTileKvpUrl(string baseUrl, string format, int x, int y, int z)
        {
            // TODO: choose WMTS query with parameters or ResourceUrl with placeholders
            if (IsResourceUrl(baseUrl))
            {
                return baseUrl
                    .Replace("{" + WmtsQueryTileCol + "}", x.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{" + WmtsQueryTileRow + "}", y.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{" + WmtsQueryTileMatrix + "}", z.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
            }
            else
            {
                var baseUri = Utils.UrlHelper.GetQueryBase(baseUrl);
                var items = Utils.UrlHelper.GetQueryParameters(baseUrl);

                items.RemoveAll(kvp => kvp.Key == WmtsQueryFormat);
                items.RemoveAll(kvp => kvp.Key == WmtsQueryTileMatrix);
                items.RemoveAll(kvp => kvp.Key == WmtsQueryTileCol);
                items.RemoveAll(kvp => kvp.Key == WmtsQueryTileRow);

                var qb = new QueryBuilder(items);
                if (!items.Any(kvp => kvp.Key == WmtsQueryService))
                {
                    qb.Add(WmtsQueryService, Identifiers.WMTS);
                }

                if (!items.Any(kvp => kvp.Key == WmtsQueryVersion))
                {
                    qb.Add(WmtsQueryVersion, Identifiers.Version100);
                }

                if (!items.Any(kvp => kvp.Key == WmtsQueryRequest))
                {
                    qb.Add(WmtsQueryRequest, Identifiers.GetTile);
                }

                qb.Add(WmtsQueryFormat, format);

                qb.Add(WmtsQueryTileMatrix, z.ToString(CultureInfo.InvariantCulture));
                qb.Add(WmtsQueryTileCol, x.ToString(CultureInfo.InvariantCulture));
                qb.Add(WmtsQueryTileRow, y.ToString(CultureInfo.InvariantCulture));

                return baseUri + qb.ToQueryString();
            }
        }

        public static bool IsResourceUrl(string url)
        {
            return
                (url.IndexOf("{" + WmtsQueryTileMatrix + "}", StringComparison.InvariantCultureIgnoreCase) > 0) &&
                (url.IndexOf("{" + WmtsQueryTileRow + "}", StringComparison.InvariantCultureIgnoreCase) > 0) &&
                (url.IndexOf("{" + WmtsQueryTileCol + "}", StringComparison.InvariantCultureIgnoreCase) > 0);
        }
    }
}
