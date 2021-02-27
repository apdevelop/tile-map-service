using System.Collections.Generic;

namespace TileMapService
{
    public interface ITileSourceFabric
    {
        bool Contains(string sourceName);

        TileSources.ITileSource Get(string sourceName);

        List<TileSourceConfiguration> Sources { get; }
    }
}
