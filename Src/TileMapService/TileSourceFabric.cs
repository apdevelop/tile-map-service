using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TileMapService.TileSources;

namespace TileMapService
{
    public class TileSourceFabric : ITileSourceFabric
    {
        private readonly Dictionary<string, ITileSource> tileSources;

        public TileSourceFabric(IConfiguration configuration)
        {
            this.tileSources = configuration
                    .GetSection("Sources")
                    .Get<IList<SourceConfiguration>>()
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

        List<SourceConfiguration> ITileSourceFabric.Sources
        {
            get
            {
                return this.tileSources
                        .Select(s => s.Value.Configuration)
                        .ToList();
            }
        }

        #endregion

        private static ITileSource CreateTileSource(SourceConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return (config.Type.ToLowerInvariant()) switch
            {
                SourceConfiguration.TypeLocalFiles => new LocalFilesTileSource(config),
                SourceConfiguration.TypeMBTiles => new MBTilesTileSource(config),
                SourceConfiguration.TypeXyz => new HttpTileSource(config),
                SourceConfiguration.TypeTms => new HttpTileSource(config),
                SourceConfiguration.TypeWmts => new HttpTileSource(config),
                SourceConfiguration.TypeWms => new HttpTileSource(config),
                SourceConfiguration.TypeGeoTiff => new RasterTileSource(config),
                _ => throw new ArgumentOutOfRangeException(nameof(config.Type), $"Unknown tile source type '{config.Type}'"),
            };
        }
    }
}
