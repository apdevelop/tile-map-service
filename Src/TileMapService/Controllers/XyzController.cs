using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using TileMapService.Utils;

using EC = TileMapService.Utils.EntitiesConverter;

namespace TileMapService.Controllers
{
    /// <summary>
    /// XYZ endpoint - serving tiles using minimalistic REST API, similar to OSM, Google Maps.
    /// </summary>
    [Route("xyz")]
    public class XyzController : ControllerBase
    {
        private readonly ITileSourceFabric tileSourceFabric;

        public XyzController(ITileSourceFabric tileSourceFabric)
        {
            this.tileSourceFabric = tileSourceFabric;
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates.
        /// Url template: xyz/{tileset}/?x={x}&amp;y={y}&amp;z={z}
        /// </summary>
        /// <param name="id">Tileset identifier.</param>
        /// <param name="x">Tile X coordinate (column).</param>
        /// <param name="y">Tile Y coordinate (row), Y axis goes down from the top.</param>
        /// <param name="z">Tile Z coordinate (zoom level).</param>
        /// <returns>Response with tile contents.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTileWithUrlQueryParametersAsync(string id, int x, int y, int z, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            if (!this.tileSourceFabric.Contains(id))
            {
                return NotFound($"Specified tileset '{id}' not found");
            }

            var tileSource = this.tileSourceFabric.Get(id);
            var mediaType = tileSource.Configuration.ContentType;

            return await this.GetTileAsync(id, x, y, z, mediaType, this.tileSourceFabric.ServiceProperties.JpegQuality, cancellationToken);
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
        public async Task<IActionResult> GetTileWithUrlPathAsync(string id, int x, int y, int z, string extension, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(extension))
            {
                return BadRequest();
            }

            if (!this.tileSourceFabric.Contains(id))
            {
                return NotFound($"Specified tileset '{id}' not found.");
            }

            return await this.GetTileAsync(id, x, y, z, EC.ExtensionToMediaType(extension), this.tileSourceFabric.ServiceProperties.JpegQuality, cancellationToken);
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
        public async Task<IActionResult> GetTileWithUrlPathAsync(string id, int x, int y, int z, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            if (!this.tileSourceFabric.Contains(id))
            {
                return NotFound($"Specified tileset '{id}' not found.");
            }

            var tileSource = this.tileSourceFabric.Get(id);
            var mediaType = tileSource.Configuration.ContentType;

            return await this.GetTileAsync(id, x, y, z, mediaType, this.tileSourceFabric.ServiceProperties.JpegQuality, cancellationToken);
        }

        private async Task<IActionResult> GetTileAsync(string id, int x, int y, int z, string? mediaType, int quality, CancellationToken cancellationToken)
        {
            var tileSource = this.tileSourceFabric.Get(id);

            if (!WebMercator.IsInsideBBox(x, y, z, tileSource.Configuration.Srs))
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(mediaType))
            {
                mediaType = MediaTypeNames.Image.Png;
            }

            var data = await tileSource.GetTileAsync(x, WebMercator.FlipYCoordinate(y, z), z, cancellationToken);
            var result = ResponseHelper.CreateFileResponse(
                data,
                mediaType,
                tileSource.Configuration.ContentType,
                quality);

            return result != null
                ? File(result.FileContents, result.ContentType)
                : NotFound();
        }
    }
}
