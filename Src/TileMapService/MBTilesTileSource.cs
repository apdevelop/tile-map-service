using System;
using System.Threading.Tasks;

namespace TileMapService
{
    class MBTilesTileSource : ITileSource
    {
        private TileSetConfiguration configuration;

        private readonly string contentType;

        public MBTilesTileSource(TileSetConfiguration configuration)
        {
            this.configuration = configuration;
            this.contentType = Utils.GetContentType(this.configuration.Format); // TODO: from db metadata
        }

        async Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            if (!this.configuration.Tms)
            {
                y = Utils.FromTmsY(y, z);
            }

            var connectionString = GetMBTilesConnectionString(this.configuration.Source);

            return await new MBTilesRepository(connectionString).ReadTileDataAsync(x, y, z);
        }

        private static string GetLocalFilePath(string source)
        {
            var uriString = source.Replace(Utils.MBTilesScheme, Utils.LocalFileScheme, StringComparison.Ordinal);
            var uri = new Uri(uriString);

            return uri.LocalPath;
        }

        private static string GetMBTilesConnectionString(string source)
        {
            // https://github.com/aspnet/Microsoft.Data.Sqlite/wiki/Connection-Strings

            return $"Data Source={GetLocalFilePath(source)}; Mode=ReadOnly;";
        }

        TileSetConfiguration ITileSource.Configuration => this.configuration;

        string ITileSource.ContentType => this.contentType;
    }
}
