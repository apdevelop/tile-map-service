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
        internal const string WmsQueryLayers = "layers";
        private const string WmsQuerySrs = "srs";
        private const string WmsQueryCrs = "crs";
        private const string WmsQueryBBox = "bbox";
        private const string WmsQueryFormat = "format";
        private const string WmsQueryTransparent = "transparent";
        private const string WmsQueryBackgroundColor = "bgcolor";
        private const string WmsQueryWidth = "width";
        private const string WmsQueryHeight = "height";

        private const string EPSG3857 = Utils.SrsCodes.EPSG3857; // TODO: EPSG:4326 support

        public static string GetCapabilitiesUrl(SourceConfiguration configuration)
        {
            var location = configuration.Location;
            if (String.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException("Location must be valid string");
            }

            var baseUri = Utils.UrlHelper.GetQueryBase(location);
            var items = Utils.UrlHelper.GetQueryParameters(location);

            // Version
            var wmsVersion = Identifiers.Version111; // Default WMS version is 1.1.1
            if (configuration.Wms != null && !String.IsNullOrWhiteSpace(configuration.Wms.Version))
            {
                wmsVersion = configuration.Wms.Version;
            }
            else if (items.Any(kvp => kvp.Key == WmsQueryVersion))
            {
                wmsVersion = items.First(kvp => kvp.Key == WmsQueryVersion).Value;
            }

            var qb = new QueryBuilder
            {
                { WmsQueryService, Identifiers.Wms },
                { WmsQueryRequest, Identifiers.GetCapabilities }
            };

            // GeoServer specific parameter
            if (items.Any(kvp => kvp.Key == "map")) // TODO: add custom parameters to WMS configuration
            {
                qb.Add("map", items.First(kvp => kvp.Key == "map").Value);
            }

            return baseUri + qb.ToQueryString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetTileUrl(
            SourceConfiguration configuration,
            int x, int y, int z)
        {
            return GetMapUrl(
                configuration,
                Utils.WebMercator.TileSize, // TODO: ? high resolution tiles ?
                Utils.WebMercator.TileSize,
                Utils.WebMercator.GetTileBounds(x, y, z),
                true,
                0xFFFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetMapUrl(
            SourceConfiguration configuration,
            int width,
            int height,
            Models.Bounds boundingBox,
            bool isTransparent,
            uint backgroundColor)
        {
            var location = configuration.Location;
            if (String.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException("Location must be valid string");
            }

            var baseUri = Utils.UrlHelper.GetQueryBase(location);
            var items = Utils.UrlHelper.GetQueryParameters(location);

            // Version
            var wmsVersion = Identifiers.Version111; // Default WMS version is 1.1.1
            if (configuration.Wms != null && !String.IsNullOrWhiteSpace(configuration.Wms.Version))
            {
                wmsVersion = configuration.Wms.Version;
            }
            else if (items.Any(kvp => kvp.Key == WmsQueryVersion))
            {
                wmsVersion = items.First(kvp => kvp.Key == WmsQueryVersion).Value;
            }

            // Layers
            var layers = String.Empty;
            if (configuration.Wms != null && !String.IsNullOrWhiteSpace(configuration.Wms.Layer))
            {
                layers = configuration.Wms.Layer; // TODO: ? multiple layers
            }
            else if (items.Any(kvp => kvp.Key == WmsQueryLayers))
            {
                layers = items.First(kvp => kvp.Key == WmsQueryLayers).Value;
            }

            // Format
            var format = MediaTypeNames.Image.Png;
            if (!String.IsNullOrWhiteSpace(configuration.ContentType))
            {
                format = configuration.ContentType;
            }

            var qb = new QueryBuilder
            {
                { WmsQueryService, Identifiers.Wms },
                { WmsQueryRequest, Identifiers.GetMap },
                { WmsQueryVersion, wmsVersion },
                { WmsQueryLayers, layers },
                { (wmsVersion == Identifiers.Version130) ? WmsQueryCrs : WmsQuerySrs, EPSG3857 }, // TODO: EPSG:4326 support
                { WmsQueryBBox, boundingBox.ToBBoxString() },
                { WmsQueryWidth, width.ToString(CultureInfo.InvariantCulture) },
                { WmsQueryHeight, height.ToString(CultureInfo.InvariantCulture) },
                { WmsQueryFormat, format },
            };

            // GeoServer specific parameter
            if (items.Any(kvp => kvp.Key == "map")) // TODO: add custom parameters to WMS configuration
            {
                qb.Add("map", items.First(kvp => kvp.Key == "map").Value);
            }

            if (isTransparent)
            {
                qb.Add(WmsQueryTransparent, "true");
            }

            qb.Add(WmsQueryBackgroundColor, "0x" + backgroundColor.ToString("X8"));

            return baseUri + qb.ToQueryString();
        }
    }
}
