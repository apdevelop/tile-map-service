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
            if (!this.configuration.Tms)
            {
                y = Utils.FromTmsY(y, z);
            }

            var path = GetLocalFilePath(this.configuration.Source, x, y, z);
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                var buffer = new byte[fileInfo.Length];
                using (var fileStream = fileInfo.OpenRead())
                {
                    await fileStream.ReadAsync(buffer, 0, buffer.Length);
                    return buffer;
                }
            }
            else
            {
                return null;
            }
        }

        private static string GetLocalFilePath(string source, int x, int y, int z)
        {
            var uriString = String.Format(
                        CultureInfo.InvariantCulture,
                        source.Replace("{x}", "{0}").Replace("{y}", "{1}").Replace("{z}", "{2}"),
                        x,
                        y,
                        z);
            var uri = new Uri(uriString);

            return uri.LocalPath;
        }

        TileSourceConfiguration ITileSource.Configuration => this.configuration;

        string ITileSource.ContentType => this.contentType;
    }
}
