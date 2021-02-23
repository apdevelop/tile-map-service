using System.Threading.Tasks;

namespace TileMapService
{
    public interface ITileSource
    {
        /// <summary>
        /// Get tile from source
        /// </summary>
        /// <param name="x">X coordinate (column)</param>
        /// <param name="y">Y coordinate (row), Y axis goes up from the bottom (TMS scheme)</param>
        /// <param name="z">Z coordinate (zoom level)</param>
        /// <returns>Tile image contents</returns>
        Task<byte[]> GetTileAsync(int x, int y, int z);

        TileSourceConfiguration Configuration { get; }

        string ContentType { get; }
    }
}
