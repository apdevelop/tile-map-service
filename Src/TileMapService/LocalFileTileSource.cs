using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace TileMapService
{
    class LocalFileTileSource : ITileSource
    {
        private readonly TileSourceConfiguration configuration;

        private readonly string contentType;

        public LocalFileTileSource(TileSourceConfiguration configuration)
        {
            this.configuration = configuration;
            this.contentType = Utils.GetContentType(this.configuration.Format);
        }

        async Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            var path = GetLocalFilePath(this.configuration.Source, x, this.configuration.Tms ? y : Utils.FlipYCoordinate(y, z), z);
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

        private static string GetLocalFilePath(string template, int x, int y, int z)
        {
            var uriString = template
                            .Replace("{x}", x.ToString(CultureInfo.InvariantCulture))
                            .Replace("{y}", y.ToString(CultureInfo.InvariantCulture))
                            .Replace("{z}", z.ToString(CultureInfo.InvariantCulture));

            return new Uri(uriString).LocalPath;
        }

        TileSourceConfiguration ITileSource.Configuration => this.configuration;

        string ITileSource.ContentType => this.contentType;
    }
}
