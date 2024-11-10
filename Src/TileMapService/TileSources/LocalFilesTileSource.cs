using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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
            if (String.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException("Source identifier is null or empty string");
            }

            if (String.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException("Source location is null or empty string");
            }

            this.configuration = configuration; // Will be changed later in InitAsync
        }

        #region ITileSource implementation

        Task ITileSource.InitAsync()
        {
            if (String.IsNullOrEmpty(this.configuration.Location))
            {
                throw new InvalidOperationException("configuration.Location is null or empty");
            }

            if (String.IsNullOrEmpty(this.configuration.Format)) // TODO: from first file, if any
            {
                throw new InvalidOperationException("configuration.Format is null or empty");
            }

            // Configuration values priority:
            // 1. Default values for local files source type.
            // 2. Actual values (from first found tile properties).
            // 3. Values from configuration file - overrides given above, if provided.

            // Detect zoom levels range - build list of folders
            var zoomLevels = new List<int>();
            var xIndex = this.configuration.Location.IndexOf("{x}", StringComparison.OrdinalIgnoreCase);
            var yIndex = this.configuration.Location.IndexOf("{y}", StringComparison.OrdinalIgnoreCase);
            var zIndex = this.configuration.Location.IndexOf("{z}", StringComparison.OrdinalIgnoreCase);
            if ((zIndex < yIndex) && (zIndex < xIndex))
            {
                var baseFolder = new Uri(this.configuration.Location[..zIndex]).LocalPath;
                foreach (var directory in Directory.GetDirectories(baseFolder))
                {
                    if (Int32.TryParse(Path.GetFileName(directory), out int zoomLevel)) // Directory name is integer number
                    {
                        zoomLevels.Add(zoomLevel);
                    }
                }
            }

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                this.configuration.Id :
                this.configuration.Title;

            var srs = String.IsNullOrWhiteSpace(this.configuration.Srs) ? Utils.SrsCodes.EPSG3857 : this.configuration.Srs.Trim().ToUpper();

            var minZoom = this.configuration.MinZoom ?? (zoomLevels.Count > 0 ? zoomLevels.Min(z => z) : 0);
            var maxZoom = this.configuration.MaxZoom ?? (zoomLevels.Count > 0 ? zoomLevels.Max(z => z) : 20);

            // TODO: TileWidh, TileHeight from file properties (with supported image extension)

            // Re-create configuration
            this.configuration = new SourceConfiguration
            {
                Id = this.configuration.Id,
                Type = this.configuration.Type,
                Format = this.configuration.Format, // TODO: from file properties (extension)
                Title = title,
                Abstract = this.configuration.Abstract,
                Attribution = this.configuration.Attribution,
                Tms = this.configuration.Tms ?? false, // Default is tms=false for file storage
                Srs = srs,
                Location = this.configuration.Location,
                ContentType = Utils.EntitiesConverter.TileFormatToContentType(this.configuration.Format), // TODO: from file properties
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
            if (z < this.configuration.MinZoom || z > this.configuration.MaxZoom)
            {
                return null;
            }
            else
            {
                if (String.IsNullOrEmpty(this.configuration.Location))
                {
                    throw new InvalidOperationException("configuration.Location is null or empty");
                }

                y = this.configuration.Tms != null && this.configuration.Tms.Value ? y : Utils.WebMercator.FlipYCoordinate(y, z);
                var path = GetLocalFilePath(this.configuration.Location, x, y, z);
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists)
                {
                    using var fileStream = fileInfo.OpenRead();
                    var buffer = new byte[fileInfo.Length];
                    await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    return buffer;
                }
                else
                {
                    return null;
                }
            }
        }

        SourceConfiguration ITileSource.Configuration => this.configuration;

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetLocalFilePath(string template, int x, int y, int z)
        {
            return template
                    .Replace("{x}", x.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                    .Replace("{y}", y.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                    .Replace("{z}", z.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        }
    }
}
