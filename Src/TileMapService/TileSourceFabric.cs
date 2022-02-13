using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using TileMapService.TileSources;

namespace TileMapService
{
    class TileSourceFabric : ITileSourceFabric
    {
        private readonly Dictionary<string, ITileSource> tileSources;

        public TileSourceFabric(IConfiguration configuration)
        {
            this.tileSources = configuration
                    .GetSection("Sources")
                    .Get<IList<SourceConfiguration>>()
                    .Where(c => !String.IsNullOrEmpty(c.Id))
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

            if (String.IsNullOrEmpty(config.Type))
            {
                throw new InvalidOperationException("config.Type is null or empty");
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
                _ => throw new InvalidOperationException($"Unknown tile source type '{config.Type}'"),
            };
        }
    }
}
