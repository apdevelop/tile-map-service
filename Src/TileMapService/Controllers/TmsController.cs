using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TileMapService.Controllers
{
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

        [HttpGet("1.0.0/{tilesetName}")]
        public IActionResult GetCapabilitiesTileSets(string tilesetName)
        {
            using (var ms = new MemoryStream())
            {
                new CapabilitiesDocumentBuilder(this.BaseUrl, this.tileSources).GetTileSets(tilesetName).Save(ms);
                return File(ms.ToArray(), Utils.TextXml);
            }
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates 
        /// URL format according to TMS 1.0.0 specs, like http://localhost:5000/tms/1.0.0/world/3/4/5.png
        /// </summary>
        /// <param name="tilesetName">Tileset name</param>
        /// <param name="x">Tile column</param>
        /// <param name="y">Tile row</param>
        /// <param name="z">Zoom level</param>
        /// <param name="formatExtension"></param>
        /// <returns></returns>
        [HttpGet("1.0.0/{tilesetName}/{z}/{x}/{y}.{formatExtension}")]
        public async Task<IActionResult> GetTileAsync(string tilesetName, int x, int y, int z, string formatExtension)
        {
            if (String.IsNullOrEmpty(tilesetName))
            {
                return BadRequest();
            }

            if (this.tileSources.TileSources.ContainsKey(tilesetName))
            {
                // TODO: check formatExtension == tileset.Configuration.Format
                var tileSource = this.tileSources.TileSources[tilesetName];
                var data = await tileSource.GetTileAsync(x, y, z);
                if (data != null)
                {
                    return File(data, tileSource.ContentType);
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return NotFound($"Specified tileset '{tilesetName}' not exists on server");
            }
        }
    }
}
