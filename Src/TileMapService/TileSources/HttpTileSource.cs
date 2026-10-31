using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.Extensions.Logging;

using TileMapService.Models;
using MBT = TileMapService.MBTiles;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with tiles from other web service (TMS, WMTS, WMS).
    /// </summary>
    class HttpTileSource : ITileSource
    {
        private readonly ILogger<HttpTileSource> logger;

        private SourceConfiguration configuration;

        private HttpClient? client;

        private MBT.CacheRepository? cache = null;

        public HttpTileSource(SourceConfiguration configuration, ILogger<HttpTileSource> logger)
        {
            this.logger = logger;

            if (string.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException("Source identifier is null or empty string");
            }

            if (string.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException("Source location is null or empty string");
            }

            this.configuration = configuration; // Will be changed later in InitAsync
        }

        #region ITileSource implementation

        async Task ITileSource.InitAsync()
        {
            // Configuration values priority:
            // 1. Default values for http source type.
            // 2. Actual values (from source metadata / capabilities).
            // 3. Values from configuration file - overrides given above, if provided.

            client = new HttpClient { Timeout = TimeSpan.FromSeconds(15), }; // TODO: add custom headers from configuration

            var sourceCapabilities = await GetSourceCapabilitiesAsync().ConfigureAwait(false);

            // TODO: combine capabilies with configuration
            var title = string.IsNullOrEmpty(configuration.Title)
                ? configuration.Id
                : configuration.Title;

            var minZoom = configuration.MinZoom ?? 0;
            var maxZoom = configuration.MaxZoom ?? 24;

            if (string.IsNullOrEmpty(configuration.Type))
            {
                throw new InvalidOperationException("configuration.Type is null or empty");
            }

            if (string.IsNullOrEmpty(configuration.Format)) // TODO: get from metadata
            {
                throw new InvalidOperationException("configuration.Format is null or empty");
            }

            // Default is tms=false for simple XYZ tile services
            var tms = configuration.Tms ?? (string.Compare(configuration.Type, SourceConfiguration.TypeTms, StringComparison.InvariantCultureIgnoreCase) == 0);
            var srs = string.IsNullOrWhiteSpace(configuration.Srs) ? Utils.SrsCodes.EPSG3857 : configuration.Srs.Trim().ToUpper();

            // Re-create configuration
            configuration = new SourceConfiguration
            {
                Id = configuration.Id,
                Type = configuration.Type.ToLowerInvariant(),
                Format = configuration.Format, // TODO: from source service/layer capabilities
                Title = title,
                Abstract = configuration.Abstract, // TODO: from source layer capabilities
                Attribution = configuration.Attribution,
                Tms = tms,
                Srs = srs,
                Location = configuration.Location,
                ContentType = Utils.EntitiesConverter.TileFormatToContentType(configuration.Format), // TODO: from source service capabilities
                MinZoom = minZoom,
                MaxZoom = maxZoom,
                GeographicalBounds = sourceCapabilities?.GeographicalBounds,
                TileWidth = sourceCapabilities != null ? sourceCapabilities.TileWidth : Utils.WebMercator.DefaultTileWidth,
                TileHeight = sourceCapabilities != null ? sourceCapabilities.TileHeight : Utils.WebMercator.DefaultTileHeight,
                Cache = srs == Utils.SrsCodes.EPSG3857 ? configuration.Cache : null, // Only Web Mercator is supported in MBTiles specification
                Wmts = configuration.Wmts,
                Wms = configuration.Wms,
                PostGis = null,
                TileMatrixSet = sourceCapabilities != null ? sourceCapabilities.TileMatrixSet : [], // Actual for WMTS sources
            };

            if (configuration.Cache != null)
            {
                var dbpath = configuration.Cache.DbFile;
                if (string.IsNullOrEmpty(dbpath))
                {
                    throw new InvalidOperationException("DBpath is null or empty string");
                }

                if (File.Exists(dbpath))
                {
                    cache = new MBT.CacheRepository(dbpath);
                }
                else
                {
                    cache = MBT.CacheRepository.CreateEmpty(dbpath);
                }
            }

            logger.LogInformation($"Source '{configuration.Id}' initialization completed.");
        }

        private async Task<Layer?> GetSourceCapabilitiesAsync()
        {
            if (client == null)
            {
                throw new InvalidOperationException("HTTP client was not initialized.");
            }

            if (string.IsNullOrEmpty(configuration.Type))
            {
                throw new InvalidOperationException("configuration.Type is null or empty");
            }

            if (string.IsNullOrEmpty(configuration.Location))
            {
                throw new InvalidOperationException("configuration.Location is null or empty");
            }

            var result = new Layer
            {
                GeographicalBounds = null,
                TileWidth = Utils.WebMercator.DefaultTileWidth,
                TileHeight = Utils.WebMercator.DefaultTileHeight,
            };

            // TODO: separate WMS/WMTS/TMS capabilities client classes
            var sourceType = configuration.Type.ToLowerInvariant();
            switch (sourceType)
            {
                case SourceConfiguration.TypeTms:
                    {
                        var r = await client.GetAsync(configuration.Location).ConfigureAwait(false);
                        if (r.IsSuccessStatusCode)
                        {
                            var xml = await r.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(xml))
                            {
                                var doc = new XmlDocument();
                                doc.LoadXml(xml);

                                var properties = Tms.CapabilitiesUtility.ParseTileMap(doc);
                                result.TileWidth = properties.TileWidth;
                                result.TileHeight = properties.TileHeight;
                            }
                        }

                        break;
                    }
                case SourceConfiguration.TypeWmts:
                    {
                        string? sourceLayerIdentifier = null, capabilitiesUrl = null;
                        if (configuration.Wmts != null)
                        {
                            capabilitiesUrl = string.IsNullOrWhiteSpace(configuration.Wmts.CapabilitiesUrl) || !configuration.Wmts.CapabilitiesUrl.EndsWith(".xml")
                                ? Wmts.QueryUtility.GetCapabilitiesKvpUrl(configuration.Location)
                                : configuration.Wmts.CapabilitiesUrl;
                            sourceLayerIdentifier = configuration.Wmts.Layer;
                        }
                        else
                        {
                            capabilitiesUrl = Wmts.QueryUtility.GetCapabilitiesKvpUrl(configuration.Location);
                            sourceLayerIdentifier = Utils.UrlHelper.GetQueryParameters(configuration.Location)
                                .First(p => string.Compare(p.Key, Wmts.QueryUtility.WmtsQueryLayer, StringComparison.OrdinalIgnoreCase) == 0)
                                .Value;
                        }

                        var r = await client.GetAsync(capabilitiesUrl).ConfigureAwait(false);
                        if (r.IsSuccessStatusCode)
                        {
                            var xml = await r.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(xml))
                            {
                                var doc = new XmlDocument();
                                doc.LoadXml(xml);
                                var layers = Wmts.CapabilitiesUtility.GetLayers(doc);
                                var layer = layers.FirstOrDefault(l => l.Identifier == sourceLayerIdentifier);
                                if (layer != null)
                                {
                                    result.GeographicalBounds = layer.GeographicalBounds;
                                    result.TileMatrixSet = layer.TileMatrixSet;
                                }

                                // TODO: detect KVP/RESTful syntax support for GetTile from source capabilities
                                // TODO: use ResourceURL resourceType="tile" template="...", change Location ?
                                // TODO: ContentType/Format
                            }
                        }

                        break;
                    }
                case SourceConfiguration.TypeWms:
                    {
                        string? sourceLayerIdentifier = null;
                        if (configuration.Wms != null)
                        {
                            sourceLayerIdentifier = configuration.Wms.Layer;
                        }
                        else
                        {
                            sourceLayerIdentifier = Utils.UrlHelper.GetQueryParameters(configuration.Location)
                                .First(p => string.Compare(p.Key, Wms.QueryUtility.WmsQueryLayers, StringComparison.OrdinalIgnoreCase) == 0)
                                .Value;
                        }

                        // TODO: cache XML Capabilities documents from same source
                        var url = Wms.QueryUtility.GetCapabilitiesUrl(configuration);
                        var r = await client.GetAsync(url).ConfigureAwait(false);
                        if (r.IsSuccessStatusCode)
                        {
                            var xml = await r.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(xml))
                            {
                                var doc = new XmlDocument();
                                doc.LoadXml(xml);
                                var layers = Wms.CapabilitiesUtility.GetLayers(doc);
                                var layer = layers.FirstOrDefault(l => l.Name == sourceLayerIdentifier);
                                if (layer != null)
                                {
                                    result.GeographicalBounds = layer.GeographicalBounds;
                                }

                                // TODO: ContentType/Format
                            }
                        }

                        break;
                    }
            }

            return result;
        }

        async Task<byte[]?> ITileSource.GetTileAsync(int x, int y, int z, CancellationToken cancellationToken)
        {
            if (z < configuration.MinZoom || z > configuration.MaxZoom)
            {
                return null;
            }
            else
            {
                // Check if exists in cache
                if (cache != null)
                {
                    var data = cache.ReadTile(x, y, z);
                    if (data != null)
                    {
                        return data;
                    }
                }

                if (client == null)
                {
                    throw new InvalidOperationException("HTTP client was not initialized.");
                }

                // TODO: use metatiles to avoid clipping at tile borders

                var url = GetSourceTileUrl(x, y, z);
                var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (response.Content.Headers.ContentType != null)
                    {
                        if (response.Content.Headers.ContentType.MediaType == MediaTypeNames.Application.OgcServiceExceptionXml ||
                            response.Content.Headers.ContentType.MediaType == MediaTypeNames.Text.Xml)
                        {
                            var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                            logger.LogWarning($"Error reading tile: '{message}'."); // TODO: ? parse XML doc
                            return null;
                        }
                    }

                    // TODO: more checks of Content-Type, response size, etc.
                    var data = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                    cache?.AddTile(x, y, z, data);

                    return data;
                }
                else
                {
                    return null;
                }
            }
        }

        internal async Task<byte[]?> GetWmsMapAsync(
            int width,
            int height,
            Models.Bounds boundingBox,
            bool isTransparent,
            uint backgroundColor,
            CancellationToken cancellationToken)
        {
            if (client == null)
            {
                throw new InvalidOperationException("HTTP client was not initialized.");
            }

            if (string.IsNullOrEmpty(configuration.Location))
            {
                throw new InvalidOperationException("configuration.Location is null or empty");
            }

            var url = Wms.QueryUtility.GetMapUrl(configuration, width, height, boundingBox, isTransparent, backgroundColor);
            var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Content.Headers.ContentType != null &&
                    response.Content.Headers.ContentType.MediaType == MediaTypeNames.Application.OgcServiceExceptionXml)
                {
                    var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    logger.LogWarning($"Error reading tile: '{message}'.");
                    return null;
                }

                // TODO: more checks of Content-Type, response size, etc.

                var data = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

                return data;
            }
            else
            {
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetSourceTileUrl(int x, int y, int z)
        {
            if (string.IsNullOrEmpty(configuration.Location))
            {
                throw new InvalidOperationException("configuration.Location is null or empty");
            }

            if (string.IsNullOrEmpty(configuration.Type))
            {
                throw new InvalidOperationException("configuration.Type is null or empty");
            }

            if (string.IsNullOrEmpty(configuration.Format))
            {
                throw new InvalidOperationException("configuration.Format is null or empty");
            }

            if (string.IsNullOrEmpty(configuration.ContentType))
            {
                throw new InvalidOperationException("configuration.ContentType is null or empty");
            }

            y = configuration.Tms != null && configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z);

            return configuration.Type.ToLowerInvariant() switch
            {
                SourceConfiguration.TypeXyz => GetTileXyzUrl(configuration.Location, x, y, z),
                SourceConfiguration.TypeTms => GetTileTmsUrl(configuration.Location, configuration.Format, x, y, z),
                SourceConfiguration.TypeWmts => Wmts.QueryUtility.GetTileKvpUrl(configuration, x, y, z),
                SourceConfiguration.TypeWms => Wms.QueryUtility.GetTileUrl(configuration, x, y, z),
                _ => throw new InvalidOperationException($"Source type '{configuration.Type}' is not supported."),
            };
        }

        SourceConfiguration ITileSource.Configuration => configuration;

        #endregion

        #region GetTile urls

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetTileXyzUrl(string baseUrl, int x, int y, int z)
        {
            return baseUrl
                .Replace("{x}", x.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{y}", y.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{z}", z.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
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

        #endregion
    }
}
