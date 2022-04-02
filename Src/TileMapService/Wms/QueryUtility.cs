using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Http.Extensions;

namespace TileMapService.Wms
{
    class QueryUtility
    {
        private const string WmsQueryService = "service";
        private const string WmsQueryVersion = "version";
        private const string WmsQueryRequest = "request";
        private const string WmsQuerySrs = "srs";
        private const string WmsQueryCrs = "crs";
        private const string WmsQueryBBox = "bbox";
        private const string WmsQueryFormat = "format";
        private const string WmsQueryTransparent = "transparent";
        private const string WmsQueryBackgroundColor = "bgcolor";
        private const string WmsQueryWidth = "width";
        private const string WmsQueryHeight = "height";

        private const string EPSG3857 = Utils.SrsCodes.EPSG3857; // TODO: EPSG:4326 support

        public static string GetCapabilitiesUrl(string baseUrl)
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
            qb.Add(WmsQueryFormat, format); // TODO: check WMS capabilities
                                           
            // TODO: ? transparency/bgcolor if needed?
            qb.Add(WmsQueryTransparent, "true");

            return baseUri + qb.ToQueryString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetMapUrl(
            string baseUrl,
            int width,
            int height,
            Models.Bounds boundingBox,
            bool isTransparent,
            uint backgroundColor,
            string format)
        {
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
            qb.Add(WmsQueryBBox, boundingBox.ToBBoxString());
            qb.Add(WmsQueryWidth, width.ToString(CultureInfo.InvariantCulture));
            qb.Add(WmsQueryHeight, height.ToString(CultureInfo.InvariantCulture));
            qb.Add(WmsQueryFormat, format); // TODO: check WMS capabilities

            if (isTransparent)
            {
                qb.Add(WmsQueryTransparent, "true");
            }

            qb.Add(WmsQueryBackgroundColor, "0x" + backgroundColor.ToString("X8"));

            return baseUri + qb.ToQueryString();
        }
    }
}
