using System.Collections.Generic;
using System.Threading.Tasks;

namespace TileMapService
{
    public interface ITileSourceFabric
    {
        Task InitAsync();

        bool Contains(string sourceName);

        TileSources.ITileSource Get(string sourceName);

        List<TileSourceConfiguration> Sources { get; }
    }
}
