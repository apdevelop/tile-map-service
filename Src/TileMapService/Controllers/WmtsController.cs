using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using TileMapService.Utils;
using TileMapService.Wmts;

namespace TileMapService.Controllers
{
    /// <summary>
    /// Serving tiles using Web Map Tile Service (WMTS) protocol.
    /// </summary>
    [Route("wmts")]
    public class WmtsController : Controller
    {
        private readonly ITileSourceFabric tileSourceFabric;

        public WmtsController(ITileSourceFabric tileSourceFabric)
        {
            this.tileSourceFabric = tileSourceFabric;
        }

        /// <summary>
        /// Process WMTS requests (including GetTile using Key-Value Pairs (KVP) syntax).
        /// </summary>
        /// <param name="service">Name of service (must be "WMTS").</param>
        /// <param name="request">Name of request (one of "GetCapabilities", "GetTile").</param>
        /// <param name="version">WMTS version (must be "1.0.0")</param>
        /// <param name="layer">Style  identifier.</param>
        /// <param name="layer">Layer identifier.</param>
        /// <param name="tileMatrix">TileMatrix identifier.</param>
        /// <param name="tileRow">Row index of a tile matrix.</param>
        /// <param name="tileCol">Column index of a tile matrix.</param>
        /// <param name="format">Output format (MIME type) of the tile.</param>
        /// <returns></returns>
        [HttpGet("")]
        public async Task<IActionResult> ProcessRequestAsync(
            string? service = null,
            string? request = null,
            string version = Identifiers.Version100,
            string? layer = null,
#pragma warning disable IDE0060
            string style = "default", // Not used
#pragma warning restore IDE0060
            string format = MediaTypeNames.Image.Png,
            ////string tileMatrixSet = null,
            string? tileMatrix = null,
            int tileRow = 0,
            int tileCol = 0)
        {
            // TODO: check requirements of standard
            if ((String.Compare(service, Identifiers.WMTS, StringComparison.Ordinal) != 0) &&
                (String.Compare(service, Identifiers.WMS, StringComparison.Ordinal) != 0)) // QGIS compatibility
            {
                return ResponseWithBadRequestError(Identifiers.MissingParameterValue, "SERVICE parameter is not defined");
            }

            if (String.Compare(version, Identifiers.Version100, StringComparison.Ordinal) != 0)
            {
                return ResponseWithBadRequestError(Identifiers.InvalidParameterValue, $"Invalid VERSION parameter value (must be {Identifiers.Version100})");
            }

            if (String.Compare(request, Identifiers.GetCapabilities, StringComparison.Ordinal) == 0)
            {
                return this.ProcessGetCapabilitiesRequest();
            }
            else if (String.Compare(request, Identifiers.GetTile, StringComparison.Ordinal) == 0)
            {
                if (String.IsNullOrEmpty(tileMatrix))
                {
                    return ResponseWithBadRequestError(Identifiers.MissingParameter, "TILEMATRIX parameter is not defined");
                }

                if (String.IsNullOrEmpty(layer))
                {
                    return ResponseWithBadRequestError(Identifiers.MissingParameter, "LAYER parameter is not defined");
                }

                if (!this.tileSourceFabric.Contains(layer))
                {
                    return ResponseWithNotFoundError(Identifiers.NotFound, $"Specified layer '{layer}' was not found");
                }

                if (String.IsNullOrEmpty(format))
                {
                    return ResponseWithBadRequestError(Identifiers.MissingParameter, "FORMAT parameter is not defined");
                }

                return await GetTileAsync(layer, tileCol, tileRow, Int32.Parse(tileMatrix), format);
            }
            else
            {
                return ResponseWithBadRequestError(Identifiers.MissingParameter, "Invaid request"); // TODO: more detailed
            }
        }

        /// <summary>
        /// Process GetCapabilities request using RESTful syntax.
        /// </summary>
        /// <remarks>
        /// http://<wmts-url>/<wmts-version>/WMTSCapabilities.xml
        /// </remarks>
        /// <param name="version">Version identifier.</param>
        [HttpGet("{version}/WMTSCapabilities.xml")]
        public IActionResult ProcessGetCapabilitiesRestfulRequest(
            string version = Identifiers.Version100)
        {
            if (String.Compare(version, Identifiers.Version100, StringComparison.Ordinal) != 0)
            {
                return ResponseWithBadRequestError(Identifiers.InvalidParameterValue, $"Invalid VERSION parameter value (must be {Identifiers.Version100})");
            }

            return this.ProcessGetCapabilitiesRequest();
        }

        /// <summary>
        /// Process GetTile request using RESTful syntax.
        /// </summary>
        /// <remarks>
        /// http://<wmts-url>/tile/<wmts-version>/<layer>/<style>/<tilematrixset>/<tilematrix>/<tilerow>/<tilecol>.<format>
        /// </remarks>
        /// <param name="version">Version identifier.</param>
        /// <param name="layer">Layer identifier.</param>
        /// <param name="tileMatrix">TileMatrix identifier.</param>
        /// <param name="tileRow">Row index of a tile matrix.</param>
        /// <param name="tileCol">Column index of a tile matrix.</param>
        /// <param name="format">Output format (MIME type) of the tile.</param>
        /// <returns></returns>
        [HttpGet("tile/{version}/{layer}/{style}/{tilematrixset}/{tilematrix}/{tilerow}/{tilecol}.{format}")]
        public async Task<IActionResult> ProcessGetTileRestfulRequestAsync(
            string? tileMatrixSet,
            string? tileMatrix,
            int tileRow,
            int tileCol,
            string version = Identifiers.Version100,
            string? layer = null,
#pragma warning disable IDE0060
            string style = "default", // Not used
#pragma warning restore IDE0060
             string format = "png")
        {
            if (String.Compare(version, Identifiers.Version100, StringComparison.Ordinal) != 0)
            {
                return ResponseWithBadRequestError(Identifiers.InvalidParameterValue, "Invalid VERSION parameter value (1.0.0 available only)");
            }

            if (String.IsNullOrEmpty(tileMatrixSet))
            {
                return ResponseWithBadRequestError(Identifiers.MissingParameter, "TILEMATRIXSET parameter is not defined");
            }

            if (String.IsNullOrEmpty(tileMatrix))
            {
                return ResponseWithBadRequestError(Identifiers.MissingParameter, "TILEMATRIX parameter is not defined");
            }

            if (String.IsNullOrEmpty(layer))
            {
                return ResponseWithBadRequestError(Identifiers.MissingParameter, "LAYER parameter is not defined");
            }

            if (!this.tileSourceFabric.Contains(layer))
            {
                return ResponseWithNotFoundError(Identifiers.NotFound, $"Specified layer '{layer}' was not found");
            }

            if (String.IsNullOrEmpty(format))
            {
                return ResponseWithBadRequestError(Identifiers.MissingParameter, "FORMAT parameter is not defined");
            }

            format = EntitiesConverter.TileFormatToContentType(format);

            return await GetTileAsync(layer, tileCol, tileRow, Int32.Parse(tileMatrix), format);
        }

        private IActionResult ProcessGetCapabilitiesRequest()
        {
            var layers = EntitiesConverter.SourcesToLayers(this.tileSourceFabric.Sources)
                .Where(l => l.Format == ImageFormats.Png || l.Format == ImageFormats.Jpeg) // Only raster formats
                .ToList();

            var xmlDoc = new CapabilitiesUtility(
                new Wmts.ServiceProperties
                {
                    Title = this.tileSourceFabric.ServiceProperties.Title,
                    Abstract = this.tileSourceFabric.ServiceProperties.Abstract,
                    Keywords = this.tileSourceFabric.ServiceProperties.KeywordsList,
                },
                BaseUrl + "/wmts",
                layers)
                .GetCapabilities(); // TODO: fix base URL

            return File(xmlDoc.ToUTF8ByteArray(), MediaTypeNames.Text.Xml);
        }

        private async Task<IActionResult> GetTileAsync(string tileset, int tileCol, int tileRow, int tileMatrix, string format)
        {
            var tileSource = this.tileSourceFabric.Get(tileset);
            var data = await tileSource.GetTileAsync(tileCol, WebMercator.FlipYCoordinate(tileRow, tileMatrix), tileMatrix); // In WMTS Y axis goes down from the top
            if (data != null)
            {
                if (String.Compare(format, tileSource.Configuration.ContentType) == 0)
                {
                    // Return original source image
                    return File(data, format);
                }
                else
                {
                    // Convert source image to requested output format
                    var outputImage = ImageHelper.ConvertImageToFormat(data, format, 90);
                    if (outputImage != null)
                    {
                        return File(outputImage, format); // TODO: quality parameter
                    }
                    else
                    {
                        return ResponseWithNotFoundError(Identifiers.NotFound, "Specified tile was not found");
                    }
                }
            }
            else
            {
                return ResponseWithNotFoundError(Identifiers.NotFound, "Specified tile was not found");
            }
        }

        private IActionResult ResponseWithBadRequestError(string exceptionCode, string message)
        {
            var xmlDoc = new ExceptionReport(exceptionCode, message).ToXml();
            Response.ContentType = MediaTypeNames.Text.Xml + "; charset=utf-8"; // TODO: better way?
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
            // TODO: content-disposition: filename="message.xml"
            return File(xmlDoc.ToUTF8ByteArray(), Response.ContentType);
        }

        private IActionResult ResponseWithNotFoundError(string exceptionCode, string message)
        {
            var xmlDoc = new ExceptionReport(exceptionCode, message).ToXml();
            Response.ContentType = MediaTypeNames.Text.Xml + "; charset=utf-8"; // TODO: better way?
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            // TODO: content-disposition: filename="message.xml"
            return File(xmlDoc.ToUTF8ByteArray(), Response.ContentType);
        }

        private string BaseUrl
        {
            get
            {
                return $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
            }
        }
    }
}
