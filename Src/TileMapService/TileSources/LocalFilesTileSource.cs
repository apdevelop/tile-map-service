using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using TileMapService.Models;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with tiles stored in separate files.
    /// </summary>
    class LocalFilesTileSource : ITileSource
    {
        private SourceConfiguration configuration;

        public LocalFilesTileSource(SourceConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException("Source identifier is null or empty string");
            }

            if (string.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException("Source location is null or empty string");
            }

            this.configuration = configuration; // Will be changed later in InitAsync
        }

        #region ITileSource implementation

        Task ITileSource.InitAsync()
        {
            if (string.IsNullOrEmpty(configuration.Location))
            {
                throw new InvalidOperationException("configuration.Location is null or empty");
            }

            if (string.IsNullOrEmpty(configuration.Format)) // TODO: from first file, if any
            {
                throw new InvalidOperationException("configuration.Format is null or empty");
            }

            // Configuration values priority:
            // 1. Default values for local files source type.
            // 2. Actual values (from first found tile properties).
            // 3. Values from configuration file - overrides given above, if provided.

            // Detect zoom levels range - build list of folders
            var zoomLevels = new List<int>();
            var xIndex = configuration.Location.IndexOf("{x}", StringComparison.OrdinalIgnoreCase);
            var yIndex = configuration.Location.IndexOf("{y}", StringComparison.OrdinalIgnoreCase);
            var zIndex = configuration.Location.IndexOf("{z}", StringComparison.OrdinalIgnoreCase);
            if ((zIndex < yIndex) && (zIndex < xIndex))
            {
                var baseFolder = new Uri(configuration.Location[..zIndex]).LocalPath;
                foreach (var directory in Directory.GetDirectories(baseFolder))
                {
                    if (int.TryParse(Path.GetFileName(directory), out int zoomLevel)) // Directory name is integer number
                    {
                        zoomLevels.Add(zoomLevel);
                    }
                }
            }

            var title = string.IsNullOrEmpty(configuration.Title)
                ? configuration.Id
                : configuration.Title;

            var srs = string.IsNullOrWhiteSpace(configuration.Srs)
                ? Utils.SrsCodes.EPSG3857
                : configuration.Srs.Trim().ToUpper();

            var minZoom = configuration.MinZoom ?? (zoomLevels.Count > 0 ? zoomLevels.Min(z => z) : 0);
            var maxZoom = configuration.MaxZoom ?? (zoomLevels.Count > 0 ? zoomLevels.Max(z => z) : 20);

            // TODO: TileWidh, TileHeight from file properties (with supported image extension)

            // Re-create configuration
            configuration = new SourceConfiguration
            {
                Id = configuration.Id,
                Type = configuration.Type,
                Format = configuration.Format, // TODO: from file properties (extension)
                Title = title,
                Abstract = configuration.Abstract,
                Attribution = configuration.Attribution,
                Tms = configuration.Tms ?? false, // Default is tms=false for file storage
                Srs = srs,
                Location = configuration.Location,
                ContentType = Utils.EntitiesConverter.TileFormatToContentType(configuration.Format), // TODO: from file properties
                MinZoom = minZoom,
                MaxZoom = maxZoom,
                GeographicalBounds = null, // TODO: compute bounds (need to scan all folders ?)
                Cache = null, // Not used for local files source
            };

            // TODO: tile width, tile height from first tile

            return Task.CompletedTask;
        }

        async Task<byte[]?> ITileSource.GetTileAsync(int x, int y, int z, CancellationToken cancellationToken)
        {
            if (z < configuration.MinZoom || z > configuration.MaxZoom)
            {
                return null;
            }
            else
            {
                if (string.IsNullOrEmpty(configuration.Location))
                {
                    throw new InvalidOperationException("configuration.Location is null or empty");
                }

                y = configuration.Tms != null && configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z);
                var path = GetLocalFilePath(configuration.Location, x, y, z);
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists)
                {
                    using var fileStream = fileInfo.OpenRead();
                    var buffer = new byte[fileInfo.Length];
                    await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                    return buffer;
                }
                else
                {
                    return null;
                }
            }
        }

        SourceConfiguration ITileSource.Configuration => configuration;

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetLocalFilePath(string template, int x, int y, int z) =>
            template
                .Replace("{x}", x.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{y}", y.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{z}", z.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    }
}
