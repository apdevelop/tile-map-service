using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace TileMapService.TileSources
{
    class LocalFiles : ITileSource
    {
        private readonly TileSourceConfiguration configuration;

        public LocalFiles(TileSourceConfiguration configuration)
        {
            // TODO: configuration values priority:
            // 1. Default values for local files.
            // 2. Actual values (from first found tile properties).
            // 3. Values from configuration file.

            this.configuration = new TileSourceConfiguration
            {
                Format = configuration.Format,
                Name = configuration.Name,
                Title = configuration.Title,
                Tms = configuration.Tms,
                Location = configuration.Location,
                ContentType = Utils.TileFormatToContentType(configuration.Format),
            };
        }

        #region ITileSource implementation

        async Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            var path = GetLocalFilePath(this.configuration.Location, x, this.configuration.Tms ? y : Utils.FlipYCoordinate(y, z), z);
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                using (var fileStream = fileInfo.OpenRead())
                {
                    var buffer = new byte[fileInfo.Length];
                    await fileStream.ReadAsync(buffer, 0, buffer.Length);
                    return buffer;
                }
            }
            else
            {
                return null;
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

        private static string GetLocalFilePath(string template, int x, int y, int z)
        {
            var uriString = template
                            .Replace("{x}", x.ToString(CultureInfo.InvariantCulture))
                            .Replace("{y}", y.ToString(CultureInfo.InvariantCulture))
                            .Replace("{z}", z.ToString(CultureInfo.InvariantCulture));

            return new Uri(uriString).LocalPath;
        }
    }
}
