using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using TileMapService.TileSources;

namespace TileMapService
{
    class TileSourceFabric : ITileSourceFabric
    {
        private readonly ILogger<TileSourceFabric> logger;

        private readonly ILoggerFactory loggerFactory;

        private readonly Dictionary<string, ITileSource> tileSources;

        private readonly ServiceProperties serviceProperties;

        public TileSourceFabric(IConfiguration configuration, ILogger<TileSourceFabric> logger)
        {
            this.logger = logger;
            this.loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            var sources = configuration
                    .GetSection("Sources")
                    .Get<IList<SourceConfiguration>>();

            if (sources == null)
            {
                throw new InvalidOperationException($"Sources section is not defined in configuration file.");
            }
            else
            {
                this.tileSources = sources
                        .Where(c => !String.IsNullOrEmpty(c.Id)) // Skip disabled sources
                        .ToDictionary(c => c.Id, c => CreateTileSource(c));
            }

            var serviceProps = configuration
                    .GetSection("Service")
                    .Get<ServiceProperties>();

            this.serviceProperties = serviceProps ?? new ServiceProperties();
        }

        #region ITileSourceFabric implementation

        async Task ITileSourceFabric.InitAsync()
        {
            foreach (var tileSource in this.tileSources)
            {
                // TODO: ? execute in parallel
                // TODO: ? exclude or set flag if initialization error
                try
                {
                    await tileSource.Value.InitAsync();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Error initializing '{tileSource.Value.Configuration.Id}' source.");
                }
            }
        }

        bool ITileSourceFabric.Contains(string id) => this.tileSources.ContainsKey(id);

        ITileSource ITileSourceFabric.Get(string id) => this.tileSources[id];

        List<SourceConfiguration> ITileSourceFabric.Sources => this.tileSources
                        .Select(s => s.Value.Configuration)
                        .ToList();

        ServiceProperties ITileSourceFabric.ServiceProperties => this.serviceProperties;

        #endregion

        private ITileSource CreateTileSource(SourceConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (String.IsNullOrEmpty(config.Type))
            {
                throw new InvalidOperationException("config.Type is null or empty");
            }

            return config.Type.ToLowerInvariant() switch
            {
                SourceConfiguration.TypeLocalFiles => new LocalFilesTileSource(config),
                SourceConfiguration.TypeMBTiles => new MBTilesTileSource(config),
                SourceConfiguration.TypePostGIS => new PostGisTileSource(config),
                SourceConfiguration.TypeXyz => new HttpTileSource(config, loggerFactory.CreateLogger<HttpTileSource>()),
                SourceConfiguration.TypeTms => new HttpTileSource(config, loggerFactory.CreateLogger<HttpTileSource>()),
                SourceConfiguration.TypeWmts => new HttpTileSource(config, loggerFactory.CreateLogger<HttpTileSource>()),
                SourceConfiguration.TypeWms => new HttpTileSource(config, loggerFactory.CreateLogger<HttpTileSource>()),
                SourceConfiguration.TypeGeoTiff => new RasterTileSource(config),
                _ => throw new InvalidOperationException($"Unknown tile source type '{config.Type}'"),
            };
        }
    }
}
