using System;
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

        [HttpGet("")]
        public async Task<IActionResult> ProcessRequestAsync(
            string? service = null,
            string? request = null,
            string version = Identifiers.Version100,
            string? layer = null,
            ////string style = null,
            ////string format = null,
            ////string tileMatrixSet = null,
            string? tileMatrix = null,
            int tileRow = 0,
            int tileCol = 0)
        {
            // TODO: check requirements of standard
            if (String.Compare(service, "WMTS", StringComparison.Ordinal) != 0)
            {
                return ResponseWithBadRequestError("MissingParameterValue", "SERVICE parameter is not defined");
            }

            if (String.Compare(version, "1.0.0", StringComparison.Ordinal) != 0)
            {
                return ResponseWithBadRequestError("InvalidParameterValue", "Invalid VERSION parameter value (1.0.0 available only)");
            }

            if (String.Compare(request, Identifiers.GetCapabilities, StringComparison.Ordinal) == 0)
            {
                return ProcessGetCapabilitiesRequest();
            }
            else if (String.Compare(request, Identifiers.GetTile, StringComparison.Ordinal) == 0)
            {
                if (String.IsNullOrEmpty(tileMatrix))
                {
                    return ResponseWithBadRequestError("MissingParameter", "TILEMATRIX parameter is not defined");
                }

                if (String.IsNullOrEmpty(layer))
                {
                    return ResponseWithBadRequestError("MissingParameter", "LAYER parameter is not defined");
                }

                if (!this.tileSourceFabric.Contains(layer))
                {
                    return ResponseWithNotFoundError("Not Found", $"Specified layer '{layer}' was not found");
                }

                return await ProcessGetTileRequestAsync(layer, tileCol, tileRow, Int32.Parse(tileMatrix));
            }
            else
            {
                return ResponseWithBadRequestError("MissingParameter", "Invaid request"); // TODO: more detailed
            }
        }

        private IActionResult ProcessGetCapabilitiesRequest()
        {
            var layers = EntitiesConverter.SourcesToLayers(this.tileSourceFabric.Sources);
            var xmlDoc = new CapabilitiesUtility(BaseUrl + "/wmts", layers)
                .GetCapabilities(); // TODO: fix base URL

            return File(xmlDoc.ToUTF8ByteArray(), MediaTypeNames.Text.Xml);
        }

        private async Task<IActionResult> ProcessGetTileRequestAsync(string tileset, int x, int y, int z)
        {
            var tileSource = this.tileSourceFabric.Get(tileset);
            var data = await tileSource.GetTileAsync(x, WebMercator.FlipYCoordinate(y, z), z); // Y axis goes down from the top
            if (data != null)
            {
                return File(data, tileSource.Configuration.ContentType);
            }
            else
            {
                return ResponseWithNotFoundError("Not Found", "Specified tile was not found");
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
