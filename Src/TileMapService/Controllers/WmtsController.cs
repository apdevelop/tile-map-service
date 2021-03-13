using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            string service = null,
            string request = null,
            string version = null,
            string layer = null,
            string style = null,
            string format = null,
            string tileMatrixSet = null,
            string tileMatrix = null,
            int tileRow = 0,
            int tileCol = 0)
        {
            // TODO: errors in XML format

            ////if (String.Compare(service, "WMTS", StringComparison.Ordinal) != 0)
            ////{
            ////    return BadRequest("service");
            ////}

            ////if (String.Compare(version, "1.0.0", StringComparison.Ordinal) != 0)
            ////{
            ////    return BadRequest("version");
            ////}

            if (String.Compare(request, "GetCapabilities", StringComparison.Ordinal) == 0)
            {
                return ProcessGetCapabilitiesRequest();
            }
            else if (String.Compare(request, "GetTile", StringComparison.Ordinal) == 0)
            {
                return await ProcessGetTileRequestAsync(layer, tileCol, tileRow, Int32.Parse(tileMatrix));
            }
            else
            {
                return BadRequest("request");
            }
        }

        private IActionResult ProcessGetCapabilitiesRequest()
        {
            var layers = Utils.SourcesToLayers(this.tileSourceFabric.Sources);
            var xmlDoc = new Wmts.CapabilitiesDocumentBuilder(BaseUrl + "/wmts", layers)
                .GetCapabilities(); // TODO: fix base URL

            return File(xmlDoc.ToUTF8ByteArray(), MediaTypeNames.Text.Xml);
        }

        private async Task<IActionResult> ProcessGetTileRequestAsync(string tileset, int x, int y, int z)
        {
            if (String.IsNullOrEmpty(tileset))
            {
                return BadRequest();
            }
            else if (this.tileSourceFabric.Contains(tileset))
            {
                var tileSource = this.tileSourceFabric.Get(tileset);
                var data = await tileSource.GetTileAsync(x, Utils.FlipYCoordinate(y, z), z); // Y axis goes down from the top
                if (data != null)
                {
                    return File(data, tileSource.Configuration.ContentType);
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return NotFound($"Specified tileset '{tileset}' not found");
            }
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
