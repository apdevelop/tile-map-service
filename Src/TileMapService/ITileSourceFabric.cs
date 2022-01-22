using System.Collections.Generic;
using System.Threading.Tasks;

namespace TileMapService
{
    public interface ITileSourceFabric
    {
        Task InitAsync();

        bool Contains(string id);

        ITileSource Get(string id);

        List<SourceConfiguration> Sources { get; }
    }
}
