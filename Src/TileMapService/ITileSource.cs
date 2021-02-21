using System.Threading.Tasks;

namespace TileMapService
{
    interface ITileSource
    {
        Task<byte[]> GetTileAsync(int x, int y, int z);

        TileSetConfiguration Configuration { get; }

        string ContentType { get; }
    }
}
