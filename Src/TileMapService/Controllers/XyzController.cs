using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace TileMapService.Controllers
{
    /// <summary>
    /// Serving tiles using minimalistic REST API.
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
        /// Get tile from tileset with specified coordinates.
        /// </summary>
        /// <param name="tileset">Tileset (source) name.</param>
        /// <param name="x">Tile X coordinate (column).</param>
        /// <param name="y">Tile Y coordinate (row), Y axis goes down from the top.</param>
        /// <param name="z">Tile Z coordinate (zoom level).</param>
        /// <returns>Response with tile contents.</returns>
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
        /// Get tile from tileset with specified coordinates.
        /// </summary>
        /// <param name="tileset">Tileset name</param>
        /// <param name="x">Tile X coordinate (column)</param>
        /// <param name="y">Tile Y coordinate (row), Y axis goes down from the top</param>
        /// <param name="z">Tile Z coordinate (zoom level)</param>
        /// <param name="extension">File extension.</param>
        /// <returns>Response with tile contents.</returns>
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
    }
}
