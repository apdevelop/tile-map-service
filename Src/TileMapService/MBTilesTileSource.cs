using System;
using System.Threading.Tasks;

namespace TileMapService
{
    class MBTilesTileSource : ITileSource
    {
        private readonly TileSourceConfiguration configuration;

        private readonly string contentType;

        private readonly MBTilesRepository repository;

        public MBTilesTileSource(TileSourceConfiguration configuration)
        {
            this.configuration = configuration;
            this.contentType = Utils.GetContentType(this.configuration.Format); // TODO: from db metadata
            var connectionString = GetMBTilesConnectionString(this.configuration.Source);
            this.repository = new MBTilesRepository(connectionString);
        }

        async Task<byte[]> ITileSource.GetTileAsync(int x, int y, int z)
        {
            return await this.repository.ReadTileDataAsync(x, this.configuration.Tms ? y : Utils.FlipYCoordinate(y, z), z);
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

        TileSourceConfiguration ITileSource.Configuration => this.configuration;

        string ITileSource.ContentType => this.contentType;
    }
}
