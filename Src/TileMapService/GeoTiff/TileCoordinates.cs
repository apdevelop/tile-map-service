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
            this.X = x;
            this.Y = y;
        }

        public override string ToString()
        {
            return $"X = {this.X}  Y = {this.Y}";
        }
    }
}
