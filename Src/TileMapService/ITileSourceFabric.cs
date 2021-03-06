using System.Collections.Generic;
using System.Threading.Tasks;

namespace TileMapService
{
    public interface ITileSourceFabric
    {
        Task InitAsync();

        bool Contains(string id);

        TileSources.ITileSource Get(string id);

        List<TileSourceConfiguration> Sources { get; }
    }
}
