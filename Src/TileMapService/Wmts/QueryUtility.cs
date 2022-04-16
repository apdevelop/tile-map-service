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

        public static string GetCapabilitiesKvpUrl(string url)
        {
            var baseUri = Utils.UrlHelper.GetQueryBase(url);

            var qb = new QueryBuilder
            {
                { WmtsQueryService, Identifiers.WMTS },
                { WmtsQueryRequest, Identifiers.GetCapabilities },
                { WmtsQueryVersion, Identifiers.Version100 },
            };

            return baseUri + qb.ToQueryString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetTileKvpUrl(
            SourceConfiguration configuration,
            int x, int y, int z)
        {
            if (String.IsNullOrWhiteSpace(configuration.Location))
            {
                throw new ArgumentException("Location must be valid string");
            }

            // TODO: choose WMTS query with parameters or ResourceUrl with placeholders
            var baseUrl = configuration.Location;
            if (IsResourceUrl(baseUrl))
            {
                return baseUrl
                    .Replace("{" + WmtsQueryTileCol + "}", x.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                    .Replace("{" + WmtsQueryTileRow + "}", y.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                    .Replace("{" + WmtsQueryTileMatrix + "}", z.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                var baseUri = Utils.UrlHelper.GetQueryBase(baseUrl);
                var items = Utils.UrlHelper.GetQueryParameters(baseUrl);

                // Layer
                var layer = String.Empty;
                if (configuration.Wmts != null && !String.IsNullOrWhiteSpace(configuration.Wmts.Layer))
                {
                    layer = configuration.Wmts.Layer;
                }
                else if (items.Any(kvp => kvp.Key == WmtsQueryLayer))
                {
                    layer = items.First(kvp => kvp.Key == WmtsQueryLayer).Value;
                }

                // Style
                var style = "normal";
                if (configuration.Wmts != null && !String.IsNullOrWhiteSpace(configuration.Wmts.Style))
                {
                    style = configuration.Wmts.Style;
                }
                else if (items.Any(kvp => kvp.Key == WmtsQueryStyle))
                {
                    style = items.First(kvp => kvp.Key == WmtsQueryStyle).Value;
                }

                // TileMatrixSet
                var tileMatrixSet = String.Empty;
                if (configuration.Wmts != null && !String.IsNullOrWhiteSpace(configuration.Wmts.TileMatrixSet))
                {
                    tileMatrixSet = configuration.Wmts.TileMatrixSet;
                }
                else if (items.Any(kvp => kvp.Key == WmtsQueryTilematrixSet))
                {
                    tileMatrixSet = items.First(kvp => kvp.Key == WmtsQueryTilematrixSet).Value;
                }

                // Format
                var format = MediaTypeNames.Image.Png;
                if (!String.IsNullOrWhiteSpace(configuration.ContentType))
                {
                    format = configuration.ContentType;
                }

                var qb = new QueryBuilder
                {
                    { WmtsQueryService, Identifiers.WMTS },
                    { WmtsQueryRequest, Identifiers.GetTile },
                    { WmtsQueryVersion, Identifiers.Version100 },
                    { WmtsQueryLayer, layer },
                    { WmtsQueryStyle, style },
                    { WmtsQueryTilematrixSet, tileMatrixSet },
                    { WmtsQueryFormat, format },
                    { WmtsQueryTileMatrix, z.ToString(CultureInfo.InvariantCulture) },
                    { WmtsQueryTileCol, x.ToString(CultureInfo.InvariantCulture) },
                    { WmtsQueryTileRow, y.ToString(CultureInfo.InvariantCulture) },
                };

                return baseUri + qb.ToQueryString();
            }
        }

        private static bool IsResourceUrl(string url)
        {
            return
                (url.IndexOf("{" + WmtsQueryTileMatrix + "}", StringComparison.OrdinalIgnoreCase) > 0) &&
                (url.IndexOf("{" + WmtsQueryTileRow + "}", StringComparison.OrdinalIgnoreCase) > 0) &&
                (url.IndexOf("{" + WmtsQueryTileCol + "}", StringComparison.OrdinalIgnoreCase) > 0);
        }
    }
}
