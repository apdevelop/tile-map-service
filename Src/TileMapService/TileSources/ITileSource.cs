using System.Threading.Tasks;

namespace TileMapService.TileSources
{
    public interface ITileSource
    {
        Task InitAsync();

        /// <summary>
        /// Get tile from source.
        /// </summary>
        /// <param name="x">Tile X coordinate (column).</param>
        /// <param name="y">Tile Y coordinate (row), Y axis goes up from the bottom (TMS scheme).</param>
        /// <param name="z">Tile Z coordinate (zoom level).</param>
        /// <returns>Tile image contents.</returns>
        Task<byte[]> GetTileAsync(int x, int y, int z);

        TileSourceConfiguration Configuration { get; }
    }
}
