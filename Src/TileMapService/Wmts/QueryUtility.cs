using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Http.Extensions;

using TileMapService.Models;

namespace TileMapService.Wmts
{
    static class QueryUtility
    {
        #region Parameters names

        internal const string WmtsQueryService = "SERVICE";
        internal const string WmtsQueryVersion = "VERSION";
        internal const string WmtsQueryRequest = "REQUEST";
        internal const string WmtsQueryFormat = "FORMAT";
        internal const string WmtsQueryLayer = "LAYER";
        internal const string WmtsQueryStyle = "STYLE";
        internal const string WmtsQueryTilematrixSet = "TILEMATRIXSET";
        internal const string WmtsQueryTileMatrix = "TILEMATRIX";
        internal const string WmtsQueryTileColumn = "TILECOL";
        internal const string WmtsQueryTileRow = "TILEROW";

        #endregion

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
            if (string.IsNullOrWhiteSpace(configuration.Location))
            {
                throw new ArgumentException("Location must be valid string");
            }

            // TODO: choose WMTS query with parameters or ResourceUrl with placeholders
            var baseUrl = configuration.Location;
            if (IsResourceUrl(baseUrl))
            {
                return baseUrl
                    .Replace("{" + WmtsQueryTileColumn + "}", x.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                    .Replace("{" + WmtsQueryTileRow + "}", y.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                    .Replace("{" + WmtsQueryTileMatrix + "}", z.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                var baseUri = Utils.UrlHelper.GetQueryBase(baseUrl);
                var items = Utils.UrlHelper.GetQueryParameters(baseUrl);

                // Layer
                var layer = string.Empty;
                if (configuration.Wmts != null && !string.IsNullOrWhiteSpace(configuration.Wmts.Layer))
                {
                    layer = configuration.Wmts.Layer;
                }
                else if (items.Any(kvp => kvp.Key == WmtsQueryLayer))
                {
                    layer = items.First(kvp => kvp.Key == WmtsQueryLayer).Value;
                }

                // Style
                var style = "normal";
                if (configuration.Wmts != null && !string.IsNullOrWhiteSpace(configuration.Wmts.Style))
                {
                    style = configuration.Wmts.Style;
                }
                else if (items.Any(kvp => kvp.Key == WmtsQueryStyle))
                {
                    style = items.First(kvp => kvp.Key == WmtsQueryStyle).Value;
                }

                // TileMatrixSet
                var tileMatrixSet = string.Empty;
                if (configuration.Wmts != null && !string.IsNullOrWhiteSpace(configuration.Wmts.TileMatrixSet))
                {
                    tileMatrixSet = configuration.Wmts.TileMatrixSet;
                }
                else if (items.Any(kvp => kvp.Key == WmtsQueryTilematrixSet))
                {
                    tileMatrixSet = items.First(kvp => kvp.Key == WmtsQueryTilematrixSet).Value;
                }

                // Format
                var format = MediaTypeNames.Image.Png;
                if (!string.IsNullOrWhiteSpace(configuration.ContentType))
                {
                    format = configuration.ContentType;
                }

                // TileMatrix
                var tileMatrix = z.ToString(CultureInfo.InvariantCulture);
                var tms = configuration.TileMatrixSet.FirstOrDefault(t => t.Identifier == tileMatrixSet);
                if (tms != null && z < tms.TileMatrices.Length && !string.IsNullOrEmpty(tms.TileMatrices[z].Identifier))
                {
                    tileMatrix = tms.TileMatrices[z].Identifier!; // Assuming Zoom level indexed array
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
                    { WmtsQueryTileMatrix, tileMatrix },
                    { WmtsQueryTileColumn, x.ToString(CultureInfo.InvariantCulture) },
                    { WmtsQueryTileRow, y.ToString(CultureInfo.InvariantCulture) },
                };

                return baseUri + qb.ToQueryString();
            }
        }

        private static bool IsResourceUrl(string url) =>
            url.Contains("{" + WmtsQueryTileMatrix + "}", StringComparison.OrdinalIgnoreCase) &&
            url.Contains("{" + WmtsQueryTileRow + "}", StringComparison.OrdinalIgnoreCase) &&
            url.Contains("{" + WmtsQueryTileColumn + "}", StringComparison.OrdinalIgnoreCase);
    }
}
