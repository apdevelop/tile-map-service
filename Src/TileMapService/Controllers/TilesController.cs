using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace TileMapService.Controllers
{
    [Route("tiles")]
    public class TilesController : Controller
    {
        private readonly ITileSourceFabric tileSources;

        public TilesController(ITileSourceFabric tileSources)
        {
            this.tileSources = tileSources;
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates 
        /// URL in Google Maps format, like http://localhost/TMS/world/?x=1&y=2&z=3
        /// </summary>
        /// <param name="tileset">Tileset name</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z">Zoom level</param>
        /// <returns></returns>
        [HttpGet("{tileset}")]
        public async Task<IActionResult> GetTile1Async(string tileset, int x, int y, int z)
        {
            return await this.ReadTileAsync(tileset, x, y, z);
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates 
        /// URL in OSM format, like http://localhost/TMS/world/3/1/2
        /// </summary>
        /// <param name="tileset">Tileset name</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z">Zoom level</param>
        /// <returns></returns>
        [HttpGet("{tileset}/{z}/{x}/{y}")]
        public async Task<IActionResult> GetTile2Async(string tileset, int x, int y, int z)
        {
            return await ReadTileAsync(tileset, x, y, z);
        }

        private async Task<IActionResult> ReadTileAsync(string tileset, int x, int y, int z)
        {
            if (!String.IsNullOrEmpty(tileset))
            {
                if (this.tileSources.TileSources.ContainsKey(tileset))
                {
                    var tileSource = this.tileSources.TileSources[tileset];
                    var data = await tileSource.GetTileAsync(x, Utils.FromTmsY(y, z), z);
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
                    return NotFound($"Specified tileset '{tileset}' not found on server");
                }
            }
            {
                return BadRequest();
            }
        }
    }
}
