using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TileMapService.TileSources
{
    public class TileSourceFabric : ITileSourceFabric
    {
        private readonly Dictionary<string, ITileSource> tileSources;

        public TileSourceFabric(IConfiguration configuration)
        {
            this.tileSources = configuration
                    .GetSection("TileSources")
                    .Get<IList<TileSourceConfiguration>>()
                    .ToDictionary(c => c.Id, c => CreateTileSource(c));
        }

        #region ITileSourceFabric implementation

        async Task ITileSourceFabric.InitAsync()
        {
            foreach (var tileSource in this.tileSources)
            {
                await tileSource.Value.InitAsync(); // TODO: in parallel ?
            }
        }

        bool ITileSourceFabric.Contains(string id)
        {
            return this.tileSources.ContainsKey(id);
        }

        ITileSource ITileSourceFabric.Get(string id)
        {
            return this.tileSources[id];
        }

        List<TileSourceConfiguration> ITileSourceFabric.Sources
        {
            get
            {
                return this.tileSources
                        .Select(s => s.Value.Configuration)
                        .ToList();
            }
        }

        #endregion

        private static ITileSource CreateTileSource(TileSourceConfiguration config)
        {
            switch (config.Type.ToLowerInvariant())
            {
                case TileSourceConfiguration.TypeLocalFiles: return new LocalFilesTileSource(config);
                case TileSourceConfiguration.TypeMBTiles: return new MBTilesTileSource(config);
                default: throw new NotSupportedException();
            }
        }
    }
}
