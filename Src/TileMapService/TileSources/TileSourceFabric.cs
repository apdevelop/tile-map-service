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
                await tileSource.Value.InitAsync(); // TODO: execute in parallel ?
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
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return (config.Type.ToLowerInvariant()) switch
            {
                TileSourceConfiguration.TypeLocalFiles => new LocalFilesTileSource(config),
                TileSourceConfiguration.TypeMBTiles => new MBTilesTileSource(config),
                TileSourceConfiguration.TypeXyz => new HttpTileSource(config),
                TileSourceConfiguration.TypeTms => new HttpTileSource(config),
                TileSourceConfiguration.TypeWmts => new HttpTileSource(config),
                TileSourceConfiguration.TypeWms => new HttpTileSource(config),
                TileSourceConfiguration.TypeGeoTiff => new RasterTileSource(config),
                _ => throw new ArgumentOutOfRangeException(nameof(config.Type), $"Unknown tile source type '{config.Type}'"),
            };
        }
    }
}
