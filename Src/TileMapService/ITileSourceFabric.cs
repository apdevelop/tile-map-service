using System.Collections.Generic;

namespace TileMapService
{
    public interface ITileSourceFabric
    {
        Dictionary<string, ITileSource> TileSources { get; }
    }
}
