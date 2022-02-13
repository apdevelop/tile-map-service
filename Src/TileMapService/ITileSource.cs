using System.Threading.Tasks;

namespace TileMapService
{
    /// <summary>
    /// Represents common set of methods of tile source of any type.
    /// </summary>
    public interface ITileSource
    {
        /// <summary>
        /// Asynchronously performs initialization of tile source, building actual tile source configuration.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitAsync();

        /// <summary>
        /// Asynchronously gets tile image contents with given coordinates from source.
        /// </summary>
        /// <param name="x">Tile X coordinate (column).</param>
        /// <param name="y">Tile Y coordinate (row), Y axis goes up from the bottom (TMS scheme).</param>
        /// <param name="z">Tile Z coordinate (zoom level).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<byte[]?> GetTileAsync(int x, int y, int z);

        /// <summary>
        /// Gets the actual tile source configuration.
        /// </summary>
        SourceConfiguration Configuration { get; }
    }
}
