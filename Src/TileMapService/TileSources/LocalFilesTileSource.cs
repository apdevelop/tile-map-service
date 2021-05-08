using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TileMapService.TileSources
{
    /// <summary>
    /// Represents tile source with tiles stored in separate files.
    /// </summary>
    class LocalFilesTileSource : ITileSource
    {
        private TileSourceConfiguration configuration;

        public LocalFilesTileSource(TileSourceConfiguration configuration)
        {
            if (String.IsNullOrEmpty(configuration.Id))
            {
                throw new ArgumentException();
            }

            if (String.IsNullOrEmpty(configuration.Location))
            {
                throw new ArgumentException();
            }

            this.configuration = configuration; // Will be changed later in InitAsync
        }

        #region ITileSource implementation

        Task ITileSource.InitAsync()
        {
            // Configuration values priority:
            // 1. Default values for local files source type.
            // 2. Actual values (from first found tile properties).
            // 3. Values from configuration file - overrides given above, if provided.

            // Detect zoom levels range - build list of folders
            var zoomLevels = new List<int>();
            var xIndex = this.configuration.Location.IndexOf("{x}", StringComparison.InvariantCultureIgnoreCase);
            var yIndex = this.configuration.Location.IndexOf("{y}", StringComparison.InvariantCultureIgnoreCase);
            var zIndex = this.configuration.Location.IndexOf("{z}", StringComparison.InvariantCultureIgnoreCase);
            if ((zIndex < yIndex) && (zIndex < xIndex))
            {
                var baseFolder = new Uri(this.configuration.Location.Substring(0, zIndex)).LocalPath;
                foreach (var directory in Directory.GetDirectories(baseFolder))
                {
                    if (Int32.TryParse(Path.GetFileName(directory), out int zoomLevel))
                    {
                        zoomLevels.Add(zoomLevel);
                    }
                }
            }

            var title = String.IsNullOrEmpty(this.configuration.Title) ?
                this.configuration.Id :
                this.configuration.Title;

            var minZoom = this.configuration.MinZoom ?? (zoomLevels.Count > 0 ? zoomLevels.Min(z => z) : 0);
            var maxZoom = this.configuration.MaxZoom ?? (zoomLevels.Count > 0 ? zoomLevels.Max(z => z) : 24);

            // Re-create configuration
            this.configuration = new TileSourceConfiguration
            {
                Id = this.configuration.Id,
                Type = this.configuration.Type,
                Format = this.configuration.Format, // TODO: from file properties (extension)
                Title = title,
                Tms = this.configuration.Tms ?? false, // Default is tms=false for file storage
                Location = this.configuration.Location,
                ContentType = Utils.TileFormatToContentType(this.configuration.Format), // TODO: from file properties
                MinZoom = minZoom,
                MaxZoom = maxZoom,
            };

            return Task.CompletedTask;
        }

        async Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            if ((z < this.configuration.MinZoom) || (z > this.configuration.MaxZoom))
            {
                return null;
            }
            else
            {
                var path = GetLocalFilePath(this.configuration.Location, x, this.configuration.Tms.Value ? y : Utils.FlipYCoordinate(y, z), z);
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists)
                {
                    using (var fileStream = fileInfo.OpenRead())
                    {
                        var buffer = new byte[fileInfo.Length];
                        await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                        return buffer;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        TileSourceConfiguration ITileSource.Configuration
        {
            get
            {
                return this.configuration;
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetLocalFilePath(string template, int x, int y, int z)
        {
            return template
                    .Replace("{x}", x.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{y}", y.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)
                    .Replace("{z}", z.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
