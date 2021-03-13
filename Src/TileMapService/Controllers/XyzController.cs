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
        private readonly ITileSourceFabric tileSourceFabric;

        public XyzController(ITileSourceFabric tileSourceFabric)
        {
            this.tileSourceFabric = tileSourceFabric;
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates.
        /// Url template: xyz/{tileset}/?x={x}&y={y}&z={z}
        /// </summary>
        /// <param name="id">Tileset identifier.</param>
        /// <param name="x">Tile X coordinate (column).</param>
        /// <param name="y">Tile Y coordinate (row), Y axis goes down from the top.</param>
        /// <param name="z">Tile Z coordinate (zoom level).</param>
        /// <returns>Response with tile contents.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTileWithUrlQueryParametersAsync(string id, int x, int y, int z)
        {
            if (String.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            return await this.ReadTileAsync(id, x, y, z);
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates.
        /// Url template: xyz/{tileset}/{z}/{x}/{y}.{extension}
        /// </summary>
        /// <param name="id">Tileset identifier.</param>
        /// <param name="x">Tile X coordinate (column).</param>
        /// <param name="y">Tile Y coordinate (row), Y axis goes down from the top.</param>
        /// <param name="z">Tile Z coordinate (zoom level).</param>
        /// <param name="extension">File extension.</param>
        /// <returns>Response with tile contents.</returns>
        [HttpGet("{id}/{z}/{x}/{y}.{extension}")]
        public async Task<IActionResult> GetTileWithUrlPathAsync(string id, int x, int y, int z, string extension)
        {
            if (String.IsNullOrEmpty(id) || String.IsNullOrEmpty(extension))
            {
                return BadRequest();
            }

            // TODO: check extension == tileset.Configuration.Format
            return await this.ReadTileAsync(id, x, y, z);
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates.
        /// Url template: xyz/{tileset}/{z}/{x}/{y}
        /// </summary>
        /// <param name="id">Tileset identifier.</param>
        /// <param name="x">Tile X coordinate (column).</param>
        /// <param name="y">Tile Y coordinate (row), Y axis goes down from the top.</param>
        /// <param name="z">Tile Z coordinate (zoom level).</param>
        /// <returns>Response with tile contents.</returns>
        [HttpGet("{tileset}/{z}/{x}/{y}")]
        public async Task<IActionResult> GetTileWithUrlPathAsync(string id, int x, int y, int z)
        {
            if (String.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            return await this.ReadTileAsync(id, x, y, z);
        }

        private async Task<IActionResult> ReadTileAsync(string id, int x, int y, int z)
        {
            if (String.IsNullOrEmpty(id))
            {
                return BadRequest();
            }
            else if (this.tileSourceFabric.Contains(id))
            {
                var tileSource = this.tileSourceFabric.Get(id);
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
                return NotFound($"Specified tileset '{id}' not found");
            }
        }
    }
}
