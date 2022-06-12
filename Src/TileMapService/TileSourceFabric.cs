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
            this.loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            this.tileSources = configuration
                    .GetSection("Sources")
                    .Get<IList<SourceConfiguration>>()
                    .Where(c => !String.IsNullOrEmpty(c.Id)) // Skip disabled sources
                    .ToDictionary(c => c.Id, c => CreateTileSource(c));

            this.serviceProperties = configuration
                    .GetSection("Service")
                    .Get<ServiceProperties>();

            if (this.serviceProperties == null)
            {
                this.serviceProperties = new ServiceProperties();
            }
        }

        #region ITileSourceFabric implementation

        async Task ITileSourceFabric.InitAsync()
        {
            foreach (var tileSource in this.tileSources)
            {
                // TODO: execute in parallel ?
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

        ServiceProperties ITileSourceFabric.ServiceProperties
        {
            get
            {
                return this.serviceProperties;
            }
        }

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

            return (config.Type.ToLowerInvariant()) switch
            {
                SourceConfiguration.TypeLocalFiles => new LocalFilesTileSource(config),
                SourceConfiguration.TypeMBTiles => new MBTilesTileSource(config),
                SourceConfiguration.TypePostGIS => new PostGISTileSource(config),
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
