using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using SkiaSharp;

using TileMapService.Wms;
using TileMapService.Utils;
using U = TileMapService.Utils;

namespace TileMapService.Controllers
{
    /// <summary>
    /// Serving tiles using Web Map Service (WMS) protocol.
    /// </summary>
    /// <remarks>
    /// Supports currently only EPSG:3857; WMS versions 1.1.1 and 1.3.0.
    /// </remarks>
    [Route("wms")]
    public class WmsController : Controller
    {
        private readonly ITileSourceFabric tileSourceFabric;

        public WmsController(ITileSourceFabric tileSourceFabric)
        {
            this.tileSourceFabric = tileSourceFabric;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> ProcessWmsRequestAsync(
              string service = Identifiers.Wms,
              string version = Identifiers.Version111,
              string? request = null,
              string? layers = null,
              ////string styles = null,
              string? srs = null,
              string? crs = null, // WMS version 1.3.0
              string? bbox = null,
              int width = 0,
              int height = 0,
              string? format = null,
              // Optional GetMap request parameters
              bool? transparent = false,
              string bgcolor = Identifiers.DefaultBackgroundColor,
              string exceptions = MediaTypeNames.Application.OgcServiceExceptionXml
            ////string time = null,
            ////string sld = null,
            ////string sld_body = null,
            // GetFeatureInfo request parameters
            ////string query_layers = null,
            ////string info_format = MediaTypeNames.Text.Plain,
            ////int x = 0,
            ////int y = 0,
            ////int i = 0, // WMS version 1.3.0
            ////int j = 0, // WMS version 1.3.0
            ////int feature_count = 1
            )
        {
            //// $"WMS [{Request.GetOwinContext().Request.RemoteIpAddress}:{Request.GetOwinContext().Request.RemotePort}] {Request.RequestUri}";

            if (String.Compare(service, Identifiers.Wms, StringComparison.OrdinalIgnoreCase) != 0)
            {
                var message = $"Unknown service: '{service}' (should be '{Identifiers.Wms}')";
                return ResponseWithExceptionReport(Identifiers.InvalidParameterValue, message, "service");
            }

            if ((String.Compare(version, Identifiers.Version111, StringComparison.Ordinal) != 0) &&
                (String.Compare(version, Identifiers.Version130, StringComparison.Ordinal) != 0))
            {
                var message = $"Unsupported {nameof(version)}: {version} (should be one of: {Identifiers.Version111}, {Identifiers.Version130})";
                return ResponseWithExceptionReport(Identifiers.InvalidParameterValue, message, "version");
            }

            if ((String.Compare(exceptions, MediaTypeNames.Application.OgcServiceExceptionXml, StringComparison.Ordinal) != 0) &&
                (String.Compare(exceptions, "XML", StringComparison.Ordinal) != 0))
            {
                var message = $"Unsupported {nameof(exceptions)}: {exceptions} (should be {MediaTypeNames.Application.OgcServiceExceptionXml})";
                return ResponseWithExceptionReport(Identifiers.InvalidParameterValue, message, "exceptions");
            }

            if (String.Compare(request, Identifiers.GetCapabilities, StringComparison.Ordinal) == 0)
            {
                return this.ProcessGetCapabilitiesRequest(WmsHelper.GetWmsVersion(version));
            }
            else if (String.Compare(request, Identifiers.GetMap, StringComparison.Ordinal) == 0)
            {
                return await this.ProcessGetMapRequestAsync(
                    version,
                    layers,
                    WmsHelper.GetWmsVersion(version) == Wms.Version.Version130 ? crs : srs,
                    bbox,
                    width,
                    height,
                    format,
                    transparent,
                    bgcolor);
            }
            ////else if (String.Compare(request, Identifiers.GetFeatureInfo, StringComparison.Ordinal) == 0)
            ////{
            ////return await this.ProcessGetFeatureInfoRequestAsync(
            ////    bbox, width,
            ////    query_layers, info_format,
            ////    v == Wms.Version.v130 ? i : x,
            ////    v == Wms.Version.v130 ? j : y,
            ////    feature_count);
            ////}
            else
            {
                var message = $"Unsupported request: '{request}' ({Identifiers.GetCapabilities}, {Identifiers.GetMap}, {Identifiers.GetFeatureInfo})";
                return ResponseWithServiceExceptionReport(Identifiers.OperationNotSupported, message, version);
            }
        }

        private IActionResult ProcessGetCapabilitiesRequest(Wms.Version version)
        {
            var layers = U.EntitiesConverter.SourcesToLayers(this.tileSourceFabric.Sources)
                .Where(l => l.Srs == U.SrsCodes.EPSG3857) // TODO: EPSG:4326 support
                .Where(l => l.Format == ImageFormats.Png || l.Format == ImageFormats.Jpeg) // Only raster formats
                .Select(l => new Layer
                {
                    Name = l.Identifier,
                    Title = String.IsNullOrEmpty(l.Title) ? l.Identifier : l.Title,
                    Abstract = l.Abstract,
                    IsQueryable = false,
                    GeographicalBounds = l.GeographicalBounds,
                })
                .ToList();

            var xmlDoc = new CapabilitiesUtility(BaseUrl + "/wms").CreateCapabilitiesDocument(
                version,
                new Wms.ServiceProperties
                {
                    Title = this.tileSourceFabric.ServiceProperties.Title,
                    Abstract = this.tileSourceFabric.ServiceProperties.Abstract,
                    Keywords = this.tileSourceFabric.ServiceProperties.KeywordsList,
                },
                layers,
                new[]
                {
                    MediaTypeNames.Image.Png,
                    MediaTypeNames.Image.Jpeg,
                });

            return File(U.EntitiesConverter.ToUTF8ByteArray(xmlDoc), MediaTypeNames.Text.Xml);
        }

        private async Task<IActionResult> ProcessGetMapRequestAsync(
            string version,
            string? layers,
            string? srs,
            string? bbox,
            int width,
            int height,
            string? format,
            bool? transparent,
            string bgcolor)
        {
            // TODO: config ?
            const int MinSize = 1;
            const int MaxSize = 32768;

            if (String.IsNullOrEmpty(format))
            {
                var message = "No map output format was specified";
                return ResponseWithServiceExceptionReport(Identifiers.InvalidFormat, message, version);
            }

            var isFormatSupported = (String.Compare(format, MediaTypeNames.Image.Png, StringComparison.OrdinalIgnoreCase) == 0) ||
                                    (String.Compare(format, MediaTypeNames.Image.Jpeg, StringComparison.OrdinalIgnoreCase) == 0);

            if (!isFormatSupported)
            {
                var message = $"Image format '{format}' is not supported";
                return ResponseWithServiceExceptionReport(Identifiers.InvalidFormat, message, version);
            }

            if (String.IsNullOrEmpty(layers))
            {
                var message = "GetMap request must include a valid LAYERS parameter.";
                return ResponseWithServiceExceptionReport(Identifiers.LayerNotDefined, message, version);
            }

            if ((width < MinSize) ||
                (width > MaxSize) ||
                (height < MinSize) ||
                (height > MaxSize))
            {
                var message = $"Missing or invalid requested map size. Parameters WIDTH and HEIGHT must present and be positive integers (got WIDTH={width}, HEIGHT={height}).";
                return ResponseWithServiceExceptionReport(Identifiers.MissingOrInvalidParameter, message, version);
            }

            if (String.IsNullOrEmpty(srs))
            {
                var message = $"GetMap request must include a valid {(WmsHelper.GetWmsVersion(version) == Wms.Version.Version130 ? "CRS" : "SRS")} parameter.";
                return ResponseWithServiceExceptionReport(Identifiers.MissingBBox, message, version);
            }

            // TODO: EPSG:4326 output support
            if (String.Compare(srs, Identifiers.EPSG3857, StringComparison.OrdinalIgnoreCase) != 0)
            {
                var message = $"SRS '{srs}' is not supported, only {Identifiers.EPSG3857} is currently supported.";
                return ResponseWithServiceExceptionReport(Identifiers.InvalidSRS, message, version);
            }

            if (bbox == null)
            {
                var message = "GetMap request must include a valid BBOX parameter.";
                return ResponseWithServiceExceptionReport(Identifiers.MissingBBox, message, version);
            }

            var boundingBox = Models.Bounds.FromCommaSeparatedString(bbox);
            if (boundingBox == null)
            {
                var message = $"GetMap request must include a valid BBOX parameter with 4 coordinates (got '{bbox}').";
                return ResponseWithServiceExceptionReport(null, message, version);
            }

            if (boundingBox.Right <= boundingBox.Left || boundingBox.Top <= boundingBox.Bottom)
            {
                var message = $"GetMap request must include a valid BBOX parameter with minX < maxX and minY < maxY coordinates (got '{bbox}').";
                return ResponseWithServiceExceptionReport(null, message, version);
            }

            var layersList = layers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var isTransparent = transparent ?? false;
            var backgroundColor = U.EntitiesConverter.GetArgbColorFromString(bgcolor, isTransparent);
            var imageData = await CreateMapImageAsync(width, height, boundingBox, format, isTransparent, backgroundColor, layersList);

            return File(imageData, format);
        }

        private async Task<byte[]> CreateMapImageAsync(
            int width,
            int height,
            Models.Bounds boundingBox,
            string format,
            bool isTransparent,
            uint backgroundColor,
            IList<string> layerNames)
        {
            var imageInfo = new SKImageInfo(
                width: width,
                height: height,
                colorType: SKColorType.Rgba8888,
                alphaType: SKAlphaType.Premul);

            using var surface = SKSurface.Create(imageInfo);
            using var canvas = surface.Canvas;
            canvas.Clear(new SKColor(backgroundColor));

            foreach (var layerName in layerNames)
            {
                if (this.tileSourceFabric.Contains(layerName))
                {
                    await DrawLayerAsync(
                        this.tileSourceFabric.Get(layerName),
                        width,
                        height,
                        boundingBox,
                        canvas,
                        isTransparent,
                        backgroundColor,
                        format);
                }
            }

            var imageFormat = U.ImageHelper.SKEncodedImageFormatFromMediaType(format);
            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(imageFormat, 90); // TODO: ? parameter

            return data.ToArray();
        }

        private static async Task DrawLayerAsync(
            ITileSource source,
            int width,
            int height,
            Models.Bounds boundingBox,
            SKCanvas outputCanvas,
            bool isTransparent,
            uint backgroundColor,
            string format)
        {
            // TODO: check SRS support in source
            if ((String.Compare(source.Configuration.Type, SourceConfiguration.TypeWms) == 0) &&
                (source.Configuration.Cache == null))
            {
                // Cascading GetMap request to WMS source as single GetMap request
                var imageData = await ((TileSources.HttpTileSource)source).GetWmsMapAsync(width, height, boundingBox, isTransparent, backgroundColor, format);
                if (imageData != null)
                {
                    WmsHelper.DrawImageUnscaledToRasterCanvas(outputCanvas, imageData);
                }
            }
            else
            {
                var tileCoordinates = WmsHelper.BuildTileCoordinatesList(boundingBox, width);
                var sourceTiles = await GetSourceTilesAsync(source, tileCoordinates);
                if (sourceTiles.Count > 0)
                {
                    WmsHelper.DrawWebMercatorTilesToRasterCanvas(outputCanvas, width, height, boundingBox, sourceTiles, backgroundColor, U.WebMercator.TileSize);
                }
            }
        }

        private string BaseUrl
        {
            get
            {
                return $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
            }
        }

        private static async Task<List<Models.TileDataset>> GetSourceTilesAsync(
            ITileSource source,
            IList<Models.TileCoordinates> tileCoordinates)
        {
            var sourceTiles = new List<Models.TileDataset>(tileCoordinates.Count);
            foreach (var tc in tileCoordinates)
            {
                // 180 degrees
                var tileCount = U.WebMercator.TileCount(tc.Z);
                var x = tc.X % tileCount;

                var tileData = await source.GetTileAsync(x, U.WebMercator.FlipYCoordinate(tc.Y, tc.Z), tc.Z);
                if (tileData != null)
                {
                    sourceTiles.Add(new Models.TileDataset(tc.X, tc.Y, tc.Z, tileData));
                }
            }

            return sourceTiles;
        }

        private IActionResult ResponseWithExceptionReport(string exceptionCode, string message, string locator)
        {
            var xmlDoc = new ExceptionReport(exceptionCode, message, locator).ToXml();
            Response.ContentType = MediaTypeNames.Application.Xml;
            Response.StatusCode = (int)HttpStatusCode.OK;
            return File(xmlDoc.ToUTF8ByteArray(), Response.ContentType);
        }

        private IActionResult ResponseWithServiceExceptionReport(string? code, string message, string version)
        {
            var xmlDoc = new ServiceExceptionReport(code, message, version).ToXml();
            Response.ContentType = MediaTypeNames.Text.Xml + ";charset=UTF-8";
            Response.StatusCode = (int)HttpStatusCode.OK;
            return File(xmlDoc.ToUTF8ByteArray(), Response.ContentType);
        }
    }
}
