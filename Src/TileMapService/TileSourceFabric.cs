namespace TileMapService
{
    class TileSourceFabric
    {
        public static ITileSource CreateTileSource(TileSetConfiguration configuration)
        {
            if (Utils.IsLocalFileScheme(configuration.Source))
            {
                return new LocalFileTileSource(configuration);
            }
            else if (Utils.IsMBTilesScheme(configuration.Source))
            {
                return new MBTilesTileSource(configuration);
            }
            else
            {
                return null;
            }
        }
    }
}
