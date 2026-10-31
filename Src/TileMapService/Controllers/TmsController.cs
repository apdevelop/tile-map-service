using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using TileMapService.Utils;

using EC = TileMapService.Utils.EntitiesConverter;

namespace TileMapService.Controllers
{
    /// <summary>
    /// TMS endpoint - serving tiles using Tile Map Service protocol (<see href="https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification">Tile Map Service Specification</see>).
    /// </summary>
    [Route("tms")]
    public class TmsController : ControllerBase
    {
        private const string Version = "1.0.0";

        private readonly ITileSourceFabric tileSourceFabric;

        public TmsController(ITileSourceFabric tileSourceFabric)
        {
            this.tileSourceFabric = tileSourceFabric;
        }

        [HttpGet("")]
        public IActionResult GetRootResource()
        {
            // TODO: services/root.xml
            var capabilities = GetCapabilities();
            var xmlDoc = new Tms.CapabilitiesUtility(capabilities).GetRootResource();

            return File(EC.XmlDocumentToUTF8ByteArray(xmlDoc), MediaTypeNames.Text.Xml);
        }

        [HttpGet(Version)]
        public IActionResult GetTileMapService()
        {
            // TODO: services/tilemapservice.xml
            var capabilities = GetCapabilities();
            var xmlDoc = new Tms.CapabilitiesUtility(capabilities).GetTileMapService();

            return File(EC.XmlDocumentToUTF8ByteArray(xmlDoc), MediaTypeNames.Text.Xml);
        }

        [HttpGet(Version + "/{tileset}")]
        public IActionResult GetTileMap(string tileset)
        {
            // TODO: services/basemap.xml
            var capabilities = GetCapabilities();
            var layer = capabilities.Layers.SingleOrDefault(l => l.Identifier == tileset);
            if (layer == null)
            {
                return NotFound(); // TODO: errors in XML format
            }
            else
            {
                var xmlDoc = new Tms.CapabilitiesUtility(capabilities).GetTileMap(layer);
                return File(EC.XmlDocumentToUTF8ByteArray(xmlDoc), MediaTypeNames.Text.Xml);
            }
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates.
        /// </summary>
        /// <param name="tileset">Tileset (source) name.</param>
        /// <param name="x">Tile X coordinate (column).</param>
        /// <param name="y">Tile Y coordinate (row), Y axis goes up from the bottom.</param>
        /// <param name="z">Tile Z coordinate (zoom level).</param>
        /// <param name="extension">File extension.</param>
        /// <returns>Response with tile contents.</returns>
        [HttpGet(Version + "/{tileset}/{z}/{x}/{y}.{extension}")]
        public async Task<IActionResult> GetTileAsync(string tileset, int x, int y, int z, string extension, CancellationToken cancellationToken)
        {
            // TODO: z can be a string, not integer number
            if (string.IsNullOrEmpty(tileset) || string.IsNullOrEmpty(extension))
            {
                return BadRequest();
            }

            if (tileSourceFabric.Contains(tileset))
            {
                // TODO: ? convert source format to requested output format
                var tileSource = tileSourceFabric.Get(tileset);

                if (!WebMercator.IsInsideBBox(x, y, z, tileSource.Configuration.Srs))
                {
                    return ResponseWithNotFoundError("The requested tile is outside the bounding box of the tile map.");
                }

                var data = await tileSource.GetTileAsync(x, y, z, cancellationToken);
                var result = ResponseHelper.CreateFileResponse(
                    data,
                    EC.ExtensionToMediaType(extension),
                    tileSource.Configuration.ContentType,
                    tileSourceFabric.ServiceProperties.JpegQuality);

                return result != null
                    ? File(result.FileContents, result.ContentType)
                    : NotFound();
            }
            else
            {
                return NotFound($"Specified tileset '{tileset}' not found");
            }
        }

        private string BaseUrl => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        private Tms.Capabilities GetCapabilities() =>
            new()
            {
                BaseUrl = BaseUrl,
                Layers = EC.SourcesToLayers(tileSourceFabric.GetAllSources()),
                ServiceTitle = tileSourceFabric.ServiceProperties.Title,
                ServiceAbstract = tileSourceFabric.ServiceProperties.Abstract,
            };

        private FileContentResult ResponseWithNotFoundError(string message)
        {
            var xmlDoc = new Tms.TileMapServerError(message).ToXml();
            Response.ContentType = MediaTypeNames.Text.XmlUtf8;
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return File(EC.XmlDocumentToUTF8ByteArray(xmlDoc), Response.ContentType);
        }
    }
}
