using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TileMapService.Controllers
{
    /// <summary>
    /// Serving tiles using Tile Map Service (TMS) protocol; with metadata.
    /// </summary>
    [Route("tms")]
    public class TmsController : Controller
    {
        private readonly ITileSourceFabric tileSources;

        public TmsController(ITileSourceFabric tileSources)
        {
            this.tileSources = tileSources;
        }

        private string BaseUrl
        {
            get
            {
                return $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
            }
        }

        [HttpGet("")]
        public IActionResult GetCapabilitiesServices()
        {
            using (var ms = new MemoryStream())
            {
                new CapabilitiesDocumentBuilder(this.BaseUrl, this.tileSources).GetServices().Save(ms);
                return File(ms.ToArray(), Utils.TextXml);
            }
        }

        [HttpGet("1.0.0")]
        public IActionResult GetCapabilitiesTileMaps()
        {
            using (var ms = new MemoryStream())
            {
                new CapabilitiesDocumentBuilder(this.BaseUrl, this.tileSources).GetTileMaps().Save(ms);
                return File(ms.ToArray(), Utils.TextXml);
            }
        }

        [HttpGet("1.0.0/{tileset}")]
        public IActionResult GetCapabilitiesTileSets(string tileset)
        {
            using (var ms = new MemoryStream())
            {
                new CapabilitiesDocumentBuilder(this.BaseUrl, this.tileSources).GetTileSets(tileset).Save(ms);
                return File(ms.ToArray(), Utils.TextXml);
            }
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates 
        /// URL format according to TMS 1.0.0 specs, like http://localhost:5000/tms/1.0.0/world/3/4/5.png
        /// </summary>
        /// <param name="tileset">Tileset name</param>
        /// <param name="x">X coordinate (tile column)</param>
        /// <param name="y">Y coordinate (tile row), Y axis goes up from the bottom</param>
        /// <param name="z">Z coordinate (zoom level)</param>
        /// <param name="extension"></param>
        /// <returns></returns>
        [HttpGet("1.0.0/{tileset}/{z}/{x}/{y}.{extension}")]
        public async Task<IActionResult> GetTileAsync(string tileset, int x, int y, int z, string extension)
        {
            if (String.IsNullOrEmpty(tileset) || String.IsNullOrEmpty(extension))
            {
                return BadRequest();
            }

            if (this.tileSources.TileSources.ContainsKey(tileset))
            {
                // TODO: check extension == tileset.Configuration.Format
                var tileSource = this.tileSources.TileSources[tileset];
                var data = await tileSource.GetTileAsync(x, y, z);
                if (data != null)
                {
                    return File(data, tileSource.ContentType); // TODO: file name
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
    }
}
