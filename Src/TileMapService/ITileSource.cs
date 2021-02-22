using System.Threading.Tasks;

namespace TileMapService
{
    public interface ITileSource
    {
        /// <summary>
        /// Read tile from source
        /// </summary>
        /// <param name="x">X coordinate (column)</param>
        /// <param name="y">Y coordinate (row)</param>
        /// <param name="z">Z coordinate (zoom level)</param>
        /// <returns></returns>
        Task<byte[]> GetTileAsync(int x, int y, int z);

        TileSourceConfiguration Configuration { get; }

        string ContentType { get; }
    }
}
