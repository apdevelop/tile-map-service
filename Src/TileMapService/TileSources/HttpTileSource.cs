using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using MBT = TileMapService.MBTiles;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with tiles from other HTTP (also TMS, WMTS) service.
    /// </summary>
    class HttpTileSource : ITileSource
    {
        private SourceConfiguration configuration;

        private HttpClient client;

        private MBT.CacheRepository cache = null;

        public HttpTileSource(SourceConfiguration configuration)
        {
            if (String.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException("Source identifier is null or empty string");
            }

            if (String.IsNullOrEmpty(configuration.Location))
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
            // 2. Actual values (from source metadata).
            // 3. Values from configuration file - overrides given above, if provided.

            this.client = new HttpClient(); // TODO: custom headers from configuration

            // TODO: read and use metadata from TMS, WMTS sources - implement TMS, WMTS capabilities client and DTO classes
            var geographicalBounds = await GetGeographicalBoundsAsync();

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                this.configuration.Id :
                this.configuration.Title;

            var minZoom = this.configuration.MinZoom ?? 0;
            var maxZoom = this.configuration.MaxZoom ?? 24;

            // Default is tms=false for simple XYZ tile services
            var tms = this.configuration.Tms ?? (this.configuration.Type.ToLowerInvariant() == SourceConfiguration.TypeTms);
            var srs = String.IsNullOrWhiteSpace(this.configuration.Srs) ? Utils.SrsCodes.EPSG3857 : this.configuration.Srs.Trim().ToUpper();

            // Re-create configuration
            this.configuration = new SourceConfiguration
            {
                Id = this.configuration.Id,
                Type = this.configuration.Type.ToLowerInvariant(),
                Format = this.configuration.Format, // TODO: from source metadata
                Title = title,
                Tms = tms,
                Srs = srs,
                Location = this.configuration.Location,
                ContentType = Utils.EntitiesConverter.TileFormatToContentType(this.configuration.Format), // TODO: from metadata
                MinZoom = minZoom,
                MaxZoom = maxZoom,
                GeographicalBounds = geographicalBounds,
                Cache = (srs == Utils.SrsCodes.EPSG3857) ? this.configuration.Cache : null, // Only Web Mercator is supported due to mbtiles format limits
            };

            if (this.configuration.Cache != null)
            {
                var dbpath = this.configuration.Cache.DbFile;
                if (File.Exists(dbpath))
                {
                    this.cache = new MBT.CacheRepository(dbpath);
                }
                else
                {
                    this.cache = MBT.CacheRepository.CreateEmpty(dbpath);
                }
            }
        }

        private async Task<Models.GeographicalBounds> GetGeographicalBoundsAsync()
        {
            Models.GeographicalBounds geographicalBounds = null;

            if (this.configuration.Type.ToLowerInvariant() == SourceConfiguration.TypeWms)
            {
                var sourceLayerName = Utils.UrlHelper.GetQueryParameters(this.configuration.Location).First(p => p.Key == "layers");
                var url = Wms.QueryUtility.GetCapabilitiesWmsUrl(this.configuration.Location);
                var r = await this.client.GetAsync(url);
                if (r.IsSuccessStatusCode)
                {
                    var xml = await r.Content.ReadAsStringAsync();
                    if (!String.IsNullOrEmpty(xml))
                    {
                        var doc = new System.Xml.XmlDocument();
                        doc.LoadXml(xml);
                        var layers = Wms.CapabilitiesUtility.GetLayers(doc);
                        var layer = layers.FirstOrDefault(l => l.Name == sourceLayerName.Value);
                        if (layer != null)
                        {
                            geographicalBounds = layer.GeographicalBounds;
                        }
                    }
                }
            }

            return geographicalBounds;
        }

        async Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            if ((z < this.configuration.MinZoom) || (z > this.configuration.MaxZoom))
            {
                return null;
            }
            else
            {
                // Check if exists in cache
                if (this.cache != null)
                {
                    var data = this.cache.ReadTileData(x, y, z);
                    if (data != null)
                    {
                        return data;
                    }
                }

                var url = GetSourceUrl(x, y, z);
                var response = await client.GetAsync(url);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (response.Content.Headers.ContentType.MediaType == MediaTypeNames.Application.OgcServiceExceptionXml)
                    {
                        var message = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine(message); // TODO: log error
                        return null;
                    }

                    // TODO: more types checks of ContentType

                    var data = await response.Content.ReadAsByteArrayAsync();

                    if (this.cache != null)
                    {
                        this.cache.AddTile(x, y, z, data);
                    }

                    return data;
                }
                else
                {
                    return null;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetSourceUrl(int x, int y, int z)
        {
            return (this.configuration.Type.ToLowerInvariant()) switch
            {
                SourceConfiguration.TypeXyz => GetTileXyzUrl(this.configuration.Location, x, this.configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z), z),
                SourceConfiguration.TypeTms => GetTileTmsUrl(this.configuration.Location, this.configuration.Format, x, this.configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z), z),
                SourceConfiguration.TypeWmts => GetTileWmtsUrl(this.configuration.Location, this.configuration.ContentType, x, this.configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z), z),
                SourceConfiguration.TypeWms => Wms.QueryUtility.GetTileUrl(this.configuration.Location, this.configuration.ContentType, x, this.configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z), z),
                _ => throw new InvalidOperationException($"Source type '{this.configuration.Type}' is not supported."),
            };
        }

        SourceConfiguration ITileSource.Configuration
        {
            get
            {
                return this.configuration;
            }
        }

        #endregion

        #region GetTile urls

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

        #endregion
    }
}
