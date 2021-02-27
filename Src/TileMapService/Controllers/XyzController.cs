using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace TileMapService.Controllers
{
    /// <summary>
    /// Minimalistic REST API for serving tiles ("Slippy Map" / "XYZ"); without metadata.
    /// </summary>
    [Route("xyz")]
    public class XyzController : Controller
    {
        private readonly ITileSourceFabric tileSources;

        public XyzController(ITileSourceFabric tileSources)
        {
            this.tileSources = tileSources;
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates 
        /// URL format like http://.../xyz/tileset/?x=1&y=2&z=3
        /// </summary>
        /// <param name="tileset">Tileset name</param>
        /// <param name="x">X coordinate (tile column)</param>
        /// <param name="y">Y coordinate (tile row), Y axis goes down from the top</param>
        /// <param name="z">Z coordinate (zoom level)</param>
        /// <returns></returns>
        [HttpGet("{tileset}")]
        public async Task<IActionResult> GetTileWithUrlQueryParametersAsync(string tileset, int x, int y, int z)
        {
            if (String.IsNullOrEmpty(tileset))
            {
                return BadRequest();
            }

            return await this.ReadTileAsync(tileset, x, y, z);
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates 
        /// URL format like http://.../xyz/tileset/3/1/2
        /// </summary>
        /// <param name="tileset">Tileset name</param>
        /// <param name="x">X coordinate (tile column)</param>
        /// <param name="y">Y coordinate (tile row), Y axis goes down from the top</param>
        /// <param name="z">Z coordinate (zoom level)</param>
        /// <returns></returns>
        [HttpGet("{tileset}/{z}/{x}/{y}.{extension}")]
        public async Task<IActionResult> GetTileWithUrlPathAsync(string tileset, int x, int y, int z, string extension)
        {
            if (String.IsNullOrEmpty(tileset) || String.IsNullOrEmpty(extension))
            {
                return BadRequest();
            }

            // TODO: check extension == tileset.Configuration.Format
            return await this.ReadTileAsync(tileset, x, y, z);
        }

        private async Task<IActionResult> ReadTileAsync(string tileset, int x, int y, int z)
        {
            if (String.IsNullOrEmpty(tileset))
            {
                return BadRequest();
            }

            if (this.tileSources.Contains(tileset))
            {
                var tileSource = this.tileSources.Get(tileset);
                var data = await tileSource.GetTileAsync(x, Utils.FlipYCoordinate(y, z), z);
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
