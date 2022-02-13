using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using TileMapService.Utils;

namespace TileMapService.Controllers
{
    /// <summary>
    /// TMS endpoint - serving tiles using Tile Map Service protocol (<see href="https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification">Tile Map Service Specification</see>).
    /// </summary>
    [Route("tms")]
    public class TmsController : Controller
    {
        private readonly ITileSourceFabric tileSourceFabric;

        public TmsController(ITileSourceFabric tileSourceFabric)
        {
            this.tileSourceFabric = tileSourceFabric;
        }

        [HttpGet("")]
        public IActionResult GetRootResource()
        {
            // TODO: services/root.xml
            var layers = EntitiesConverter.SourcesToLayers(this.tileSourceFabric.Sources);
            var xmlDoc = new Tms.CapabilitiesUtility(this.BaseUrl, layers).GetRootResource();

            return File(xmlDoc.ToUTF8ByteArray(), MediaTypeNames.Text.Xml);
        }

        [HttpGet("1.0.0")]
        public IActionResult GetTileMapService()
        {
            // TODO: services/tilemapservice.xml
            var layers = EntitiesConverter.SourcesToLayers(this.tileSourceFabric.Sources);
            var xmlDoc = new Tms.CapabilitiesUtility(this.BaseUrl, layers).GetTileMapService();

            return File(xmlDoc.ToUTF8ByteArray(), MediaTypeNames.Text.Xml);
        }

        [HttpGet("1.0.0/{tileset}")]
        public IActionResult GetTileMap(string tileset)
        {
            // TODO: services/basemap.xml
            var layers = EntitiesConverter.SourcesToLayers(this.tileSourceFabric.Sources);
            var layer = layers.SingleOrDefault(l => l.Identifier == tileset);
            if (layer == null)
            {
                return NotFound();
            }

            var xmlDoc = new Tms.CapabilitiesUtility(this.BaseUrl, layers).GetTileMap(layer);

            return File(xmlDoc.ToUTF8ByteArray(), MediaTypeNames.Text.Xml);
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
        [HttpGet("1.0.0/{tileset}/{z}/{x}/{y}.{extension}")]
        public async Task<IActionResult> GetTileAsync(string tileset, int x, int y, int z, string extension)
        {
            // TODO: z can be a string, not integer number
            if (String.IsNullOrEmpty(tileset) || String.IsNullOrEmpty(extension))
            {
                return BadRequest();
            }

            if (this.tileSourceFabric.Contains(tileset))
            {
                // TODO: implement conversion of source format to requested output format
                var tileSource = this.tileSourceFabric.Get(tileset);
                var data = await tileSource.GetTileAsync(x, y, z);
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
