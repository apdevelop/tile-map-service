using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace TileMapService
{
    public class TileSourceFabric : ITileSourceFabric
    {
        private readonly Dictionary<string, ITileSource> tileSources;

        public TileSourceFabric(IConfiguration configuration)
        {
            // TODO: check and log configuration errors
            this.tileSources = configuration
                    .GetSection("TileSources")
                    .Get<IList<TileSourceConfiguration>>()
                    .ToDictionary(c => c.Name, c => CreateTileSource(c));
        }

        public Dictionary<string, ITileSource> TileSources
        {
            get
            {
                return this.tileSources;
            }
        }

        private static ITileSource CreateTileSource(TileSourceConfiguration config)
        {
            if (Utils.IsLocalFileScheme(config.Source))
            {
                return new LocalFileTileSource(config);
            }
            else if (Utils.IsMBTilesScheme(config.Source))
            {
                return new MBTilesTileSource(config);
            }
            else
            {
                return null;
            }
        }
    }
}
