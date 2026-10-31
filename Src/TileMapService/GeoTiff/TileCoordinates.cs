namespace TileMapService.GeoTiff
{
    class TileCoordinates
    {
        public int X { get; set; }

        public int Y { get; set; }

        public TileCoordinates()
        {

        }

        public TileCoordinates(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"X = {X}  Y = {Y}";
    }
}
