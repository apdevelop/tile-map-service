using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using SkiaSharp;

using TileMapService.Utils;
using TileMapService.Wms;

using EC = TileMapService.Utils.EntitiesConverter;

namespace TileMapService.Controllers
{
    /// <summary>
    /// Serving tiles using Web Map Service (WMS) protocol.
    /// </summary>
    /// <remarks>
    /// Supports currently only EPSG:3857 output CRS; WMS versions 1.1.1 and 1.3.0.
    /// </remarks>
    [Route("wms")]
    public class WmsController : ControllerBase
    {
        private readonly ITileSourceFabric tileSourceFabric;

        public WmsController(ITileSourceFabric tileSourceFabric)
        {
            this.tileSourceFabric = tileSourceFabric;
        }

#pragma warning disable S107
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> ProcessWmsRequestAsync(
            string service = Identifiers.Wms,
            string version = Identifiers.Version111,
            string? request = null,
            string? layers = null,
            ////string styles = null,
            string? srs = null, // WMS version 1.1.1
            string? crs = null, // WMS version 1.3.0
            string? bbox = null,
            int width = 0,
            int height = 0,
            string? format = null,
            // Optional GetMap request parameters
            bool? transparent = false,
            string bgcolor = Identifiers.DefaultBackgroundColor,
            string exceptions = MediaTypeNames.Application.OgcServiceExceptionXml,
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
            ////int feature_count = 1,
            CancellationToken cancellationToken = default)
#pragma warning restore S107
        {
            //// $"WMS [{Request.GetOwinContext().Request.RemoteIpAddress}:{Request.GetOwinContext().Request.RemotePort}] {Request.RequestUri}";

            if (string.Compare(service, Identifiers.Wms, StringComparison.OrdinalIgnoreCase) != 0)
            {
                var message = $"Unknown service: '{service}' (should be '{Identifiers.Wms}')";
                return this.ResponseWithExceptionReport(Identifiers.InvalidParameterValue, message, "service");
            }

            if ((string.Compare(version, Identifiers.Version111, StringComparison.Ordinal) != 0) &&
                (string.Compare(version, Identifiers.Version130, StringComparison.Ordinal) != 0))
            {
                var message = $"Unsupported {nameof(version)}: {version} (should be one of: {Identifiers.Version111}, {Identifiers.Version130})";
                return this.ResponseWithExceptionReport(Identifiers.InvalidParameterValue, message, "version");
            }

            if ((string.Compare(exceptions, MediaTypeNames.Application.OgcServiceExceptionXml, StringComparison.Ordinal) != 0) &&
                (string.Compare(exceptions, "XML", StringComparison.Ordinal) != 0))
            {
                var message = $"Unsupported {nameof(exceptions)}: {exceptions} (should be {MediaTypeNames.Application.OgcServiceExceptionXml})";
                return this.ResponseWithExceptionReport(Identifiers.InvalidParameterValue, message, "exceptions");
            }

            if (string.Compare(request, Identifiers.GetCapabilities, StringComparison.Ordinal) == 0)
            {
                return this.ProcessGetCapabilitiesRequest(WmsHelper.GetWmsVersion(version));
            }
            else if (string.Compare(request, Identifiers.GetMap, StringComparison.Ordinal) == 0)
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
                    bgcolor,
                    cancellationToken);
            } // TODO: GetFeatureInfo request
            ////else if (string.Compare(request, Identifiers.GetFeatureInfo, StringComparison.Ordinal) == 0)
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
                return this.ResponseWithServiceExceptionReport(Identifiers.OperationNotSupported, message, version);
            }
        }

        private FileContentResult ProcessGetCapabilitiesRequest(Wms.Version version)
        {
            var layers = EC.SourcesToLayers(this.tileSourceFabric.Sources)
                .Where(l => l.Srs == SrsCodes.EPSG3857) // TODO: sources with EPSG:4326 support
                .Where(l => l.Format == ImageFormats.Png || l.Format == ImageFormats.Jpeg) // Only raster formats
                .Select(l => new Layer
                {
                    Name = l.Identifier,
                    Title = string.IsNullOrEmpty(l.Title) ? l.Identifier : l.Title,
                    Abstract = l.Abstract,
                    IsQueryable = false,
                    GeographicalBounds = l.GeographicalBounds,
                })
                .ToList();

            var xmlDoc = new CapabilitiesUtility(BaseUrl + "/wms").CreateCapabilitiesDocument(
                version,
                new ServiceProperties
                {
                    Title = this.tileSourceFabric.ServiceProperties.Title,
                    Abstract = this.tileSourceFabric.ServiceProperties.Abstract,
                    Keywords = this.tileSourceFabric.ServiceProperties.KeywordsList,
                },
                layers,
                SupportedOutputFormats);

            return File(EC.XmlDocumentToUTF8ByteArray(xmlDoc), MediaTypeNames.Text.Xml);
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
                string bgcolor,
                CancellationToken cancellationToken)
        {
            // TODO: config ?
            const int MinSize = 1;
            const int MaxSize = 32768;

            if (string.IsNullOrEmpty(format))
            {
                var message = "No map output format was specified";
                return this.ResponseWithServiceExceptionReport(Identifiers.InvalidFormat, message, version);
            }

            var isFormatSupported = ResponseHelper.IsFormatInList(SupportedOutputFormats, format);

            if (!isFormatSupported)
            {
                var message = $"Image output format '{format}' is not supported";
                return this.ResponseWithServiceExceptionReport(Identifiers.InvalidFormat, message, version);
            }

            if (string.IsNullOrEmpty(layers))
            {
                var message = "GetMap request must include a valid LAYERS parameter.";
                return this.ResponseWithServiceExceptionReport(Identifiers.LayerNotDefined, message, version);
            }

            if (width < MinSize || width > MaxSize || height < MinSize || height > MaxSize)
            {
                var message = $"Missing or invalid requested map size. Parameters WIDTH and HEIGHT must present and be positive integers (got WIDTH={width}, HEIGHT={height}).";
                return this.ResponseWithServiceExceptionReport(Identifiers.MissingOrInvalidParameter, message, version);
            }

            if (string.IsNullOrEmpty(srs))
            {
                var message = $"GetMap request must include a valid {(WmsHelper.GetWmsVersion(version) == Wms.Version.Version130 ? "CRS" : "SRS")} parameter.";
                return this.ResponseWithServiceExceptionReport(Identifiers.MissingBBox, message, version);
            }

            // TODO: EPSG:4326 output support
            if (string.Compare(srs, Identifiers.EPSG3857, StringComparison.OrdinalIgnoreCase) != 0)
            {
                var message = $"SRS '{srs}' is not supported, only {Identifiers.EPSG3857} is currently supported.";
                return this.ResponseWithServiceExceptionReport(Identifiers.InvalidSRS, message, version);
            }

            if (bbox == null)
            {
                var message = "GetMap request must include a valid BBOX parameter.";
                return this.ResponseWithServiceExceptionReport(Identifiers.MissingBBox, message, version);
            }

            var boundingBox = Models.Bounds.FromCommaSeparatedString(bbox);
            if (boundingBox == null)
            {
                var message = $"GetMap request must include a valid BBOX parameter with 4 coordinates (got '{bbox}').";
                return this.ResponseWithServiceExceptionReport(null, message, version);
            }

            if (boundingBox.Right <= boundingBox.Left || boundingBox.Top <= boundingBox.Bottom)
            {
                var message = $"GetMap request must include a valid BBOX parameter with minX < maxX and minY < maxY coordinates (got '{bbox}').";
                return this.ResponseWithServiceExceptionReport(null, message, version);
            }

            var layersList = layers.Split(LayersSeparator, StringSplitOptions.RemoveEmptyEntries);

            var isTransparent = transparent ?? false;
            var backgroundColor = EC.ArgbColorFromString(bgcolor, isTransparent);
            var imageData = await this.CreateMapImageAsync(width, height, boundingBox, format, this.tileSourceFabric.ServiceProperties.JpegQuality, isTransparent, backgroundColor, layersList, cancellationToken);

            return File(imageData, format);
        }

        private async Task<byte[]> CreateMapImageAsync(
            int width,
            int height,
            Models.Bounds boundingBox,
            string mediaType,
            int quality,
            bool isTransparent,
            uint backgroundColor,
            IList<string> layerNames,
            CancellationToken cancellationToken)
        {
            var imageInfo = new SKImageInfo(
                width: width,
                height: height,
                colorType: SKColorType.Rgba8888,
                alphaType: SKAlphaType.Premul);

            using var surface = SKSurface.Create(imageInfo);
            using var canvas = surface.Canvas;
            canvas.Clear(new SKColor(backgroundColor));

            foreach (var layerName in layerNames.Where(this.tileSourceFabric.Contains))
            {
                await WmsHelper.DrawLayerAsync( // TODO: ? pass required format to avoid conversions
                    this.tileSourceFabric.Get(layerName),
                    width,
                    height,
                    boundingBox,
                    canvas,
                    isTransparent,
                    backgroundColor,
                    cancellationToken);
            }

            using SKImage image = surface.Snapshot();

            if (string.Compare(mediaType, MediaTypeNames.Image.Tiff, StringComparison.OrdinalIgnoreCase) == 0)
            {
                using var bitmap = SKBitmap.FromImage(image);
                // TODO: improve performance of pixels processing, maybe using unsafe/pointers
                var pixels = bitmap.Pixels.SelectMany(p => new byte[] { p.Red, p.Green, p.Blue, p.Alpha }).ToArray();
                var tiff = ImageHelper.CreateTiffImage(pixels, image.Width, image.Height, boundingBox, true);
                return tiff;
            }
            else
            {
                var imageFormat = ImageHelper.SKEncodedImageFormatFromMediaType(mediaType);
                using SKData data = image.Encode(imageFormat, quality);
                return data.ToArray();
            }
        }

        // TODO: more output formats
        private static readonly string[] SupportedOutputFormats =
        [
            MediaTypeNames.Image.Png,
            MediaTypeNames.Image.Jpeg,
            MediaTypeNames.Image.Tiff,
        ];

        private string BaseUrl => $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";

        private static readonly char[] LayersSeparator = [','];

        private FileContentResult ResponseWithExceptionReport(string exceptionCode, string message, string locator)
        {
            var xmlDoc = new ExceptionReport(exceptionCode, message, locator).ToXml();
            Response.ContentType = MediaTypeNames.Application.Xml;
            Response.StatusCode = (int)HttpStatusCode.OK;
            return File(EC.XmlDocumentToUTF8ByteArray(xmlDoc), Response.ContentType);
        }

        private FileContentResult ResponseWithServiceExceptionReport(string? code, string message, string version)
        {
            var xmlDoc = new ServiceExceptionReport(code, message, version).ToXml();
            Response.ContentType = MediaTypeNames.Text.XmlUtf8;
            Response.StatusCode = (int)HttpStatusCode.OK;
            return File(EC.XmlDocumentToUTF8ByteArray(xmlDoc), Response.ContentType);
        }
    }
}
