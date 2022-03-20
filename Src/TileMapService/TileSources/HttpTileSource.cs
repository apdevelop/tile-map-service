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
    /// Represents tile source with tiles from other web service (TMS, WMTS, WMS).
    /// </summary>
    class HttpTileSource : ITileSource
    {
        private SourceConfiguration configuration;

        private HttpClient? client;

        private MBT.CacheRepository? cache = null;

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
            var sourceCapabilities = await GetSourceCapabilitiesAsync();

            // TODO: combine capabilies with configuration
            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                this.configuration.Id :
                this.configuration.Title;

            var minZoom = this.configuration.MinZoom ?? 0;
            var maxZoom = this.configuration.MaxZoom ?? 24;

            if (String.IsNullOrEmpty(this.configuration.Type))
            {
                throw new InvalidOperationException("configuration.Type is null or empty");
            }

            if (String.IsNullOrEmpty(this.configuration.Format)) // TODO: get from metadata
            {
                throw new InvalidOperationException("configuration.Format is null or empty");
            }

            // Default is tms=false for simple XYZ tile services
            var tms = this.configuration.Tms ?? (this.configuration.Type.ToLowerInvariant() == SourceConfiguration.TypeTms);
            var srs = String.IsNullOrWhiteSpace(this.configuration.Srs) ? Utils.SrsCodes.EPSG3857 : this.configuration.Srs.Trim().ToUpper();

            // Re-create configuration
            this.configuration = new SourceConfiguration
            {
                Id = this.configuration.Id,
                Type = this.configuration.Type.ToLowerInvariant(),
                Format = this.configuration.Format, // TODO: from source service capabilities
                Title = title,
                Tms = tms,
                Srs = srs,
                Location = this.configuration.Location,
                ContentType = Utils.EntitiesConverter.TileFormatToContentType(this.configuration.Format), // TODO: from source service capabilities
                MinZoom = minZoom,
                MaxZoom = maxZoom,
                GeographicalBounds = sourceCapabilities?.GeographicalBounds,
                TileWidth = sourceCapabilities != null ? sourceCapabilities.TileWidth : Utils.WebMercator.DefaultTileWidth,
                TileHeight = sourceCapabilities != null ? sourceCapabilities.TileHeight : Utils.WebMercator.DefaultTileHeight,
                Cache = (srs == Utils.SrsCodes.EPSG3857) ? this.configuration.Cache : null, // Only Web Mercator is supported due to mbtiles format limits
            };

            if (this.configuration.Cache != null)
            {
                var dbpath = this.configuration.Cache.DbFile;
                if (String.IsNullOrEmpty(dbpath))
                {
                    throw new InvalidOperationException("DBpath is null or empty string");
                }

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

        private async Task<Models.Layer?> GetSourceCapabilitiesAsync()
        {
            if (this.client == null)
            {
                throw new InvalidOperationException("HTTP client was not initialized.");
            }

            if (String.IsNullOrEmpty(this.configuration.Type))
            {
                throw new InvalidOperationException("configuration.Type is null or empty");
            }

            if (String.IsNullOrEmpty(this.configuration.Location))
            {
                throw new InvalidOperationException("configuration.Location is null or empty");
            }

            var result = new Models.Layer
            {
                GeographicalBounds = null,
                TileWidth = Utils.WebMercator.DefaultTileWidth,
                TileHeight = Utils.WebMercator.DefaultTileHeight,
            };

            // TODO: separate WMS/WMTS/TMS capabilities client classes
            var sourceType = this.configuration.Type.ToLowerInvariant();
            switch (sourceType)
            {
                case SourceConfiguration.TypeTms:
                    {
                        var r = await this.client.GetAsync(this.configuration.Location);
                        if (r.IsSuccessStatusCode)
                        {
                            var xml = await r.Content.ReadAsStringAsync();
                            if (!String.IsNullOrEmpty(xml))
                            {
                                var doc = new System.Xml.XmlDocument();
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
                        var sourceLayerName = Utils.UrlHelper.GetQueryParameters(this.configuration.Location).First(p => p.Key == "layer");
                        var url = Wmts.QueryUtility.GetCapabilitiesWmsUrl(this.configuration.Location);
                        var r = await this.client.GetAsync(url);
                        if (r.IsSuccessStatusCode)
                        {
                            var xml = await r.Content.ReadAsStringAsync();
                            if (!String.IsNullOrEmpty(xml))
                            {
                                var doc = new System.Xml.XmlDocument();
                                doc.LoadXml(xml);
                                var layers = Wmts.CapabilitiesUtility.GetLayers(doc);
                                var layer = layers.FirstOrDefault(l => l.Identifier == sourceLayerName.Value);
                                if (layer != null)
                                {
                                    result.GeographicalBounds = layer.GeographicalBounds;
                                }

                                // TODO: use ResourceURL resourceType="tile" template="..."
                            }
                        }

                        break;
                    }
                case SourceConfiguration.TypeWms:
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
                                    result.GeographicalBounds = layer.GeographicalBounds;
                                }
                            }
                        }

                        break;
                    }
            }

            return result;
        }

        async Task<byte[]?> ITileSource.GetTileAsync(int x, int y, int z)
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
                    var data = this.cache.ReadTile(x, y, z);
                    if (data != null)
                    {
                        return data;
                    }
                }

                if (this.client == null)
                {
                    throw new InvalidOperationException("HTTP client was not initialized.");
                }

                var url = GetSourceUrl(x, y, z);
                var response = await client.GetAsync(url);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (response.Content.Headers.ContentType != null && response.Content.Headers.ContentType.MediaType == MediaTypeNames.Application.OgcServiceExceptionXml)
                    {
                        var message = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine(message); // TODO: write error details to log
                        return null;
                    }

                    // TODO: more checks of Content-Type, response size, etc.

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
            if (String.IsNullOrEmpty(this.configuration.Location))
            {
                throw new InvalidOperationException("configuration.Location is null or empty");
            }

            if (String.IsNullOrEmpty(this.configuration.Type))
            {
                throw new InvalidOperationException("configuration.Type is null or empty");
            }

            if (String.IsNullOrEmpty(this.configuration.Format))
            {
                throw new InvalidOperationException("configuration.Format is null or empty");
            }

            if (String.IsNullOrEmpty(this.configuration.ContentType))
            {
                throw new InvalidOperationException("configuration.ContentType is null or empty");
            }

            y = this.configuration.Tms != null && this.configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z);
            return (this.configuration.Type.ToLowerInvariant()) switch
            {
                SourceConfiguration.TypeXyz => GetTileXyzUrl(this.configuration.Location, x, y, z),
                SourceConfiguration.TypeTms => GetTileTmsUrl(this.configuration.Location, this.configuration.Format, x, y, z),
                SourceConfiguration.TypeWmts => Wmts.QueryUtility.GetTileUrl(this.configuration.Location, this.configuration.ContentType, x, y, z),
                SourceConfiguration.TypeWms => Wms.QueryUtility.GetTileUrl(this.configuration.Location, this.configuration.ContentType, x, y, z),
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

        #endregion
    }
}
