using System.Threading.Tasks;

namespace TileMapService
{
    public interface ITileSource
    {
        Task<byte[]> GetTileAsync(int x, int y, int z);

        TileSourceConfiguration Configuration { get; }

        string ContentType { get; }
    }
}
