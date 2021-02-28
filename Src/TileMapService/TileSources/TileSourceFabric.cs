using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TileMapService.TileSources
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

        #region ITileSourceFabric implementation

        bool ITileSourceFabric.Contains(string sourceName)
        {
            return this.tileSources.ContainsKey(sourceName);
        }

        ITileSource ITileSourceFabric.Get(string sourceName)
        {
            return this.tileSources[sourceName];
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
            if (IsLocalFileScheme(config.Location))
            {
                return new LocalFile(config);
            }
            else if (IsMBTilesScheme(config.Location))
            {
                return new MBTiles(config);
            }
            else
            {
                return null;
            }
        }

        private static bool IsMBTilesScheme(string source)
        {
            return source.StartsWith(Utils.MBTilesScheme, StringComparison.Ordinal);
        }

        private static bool IsLocalFileScheme(string source)
        {
            return source.StartsWith(Utils.LocalFileScheme, StringComparison.Ordinal);
        }
    }
}
