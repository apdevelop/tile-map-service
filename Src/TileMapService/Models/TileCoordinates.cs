namespace TileMapService.Models
{
    class TileCoordinates
    {
        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }

        public TileCoordinates()
        {

        }

        public TileCoordinates(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public override string ToString()
        {
            return $"X={this.X} Y={this.Y} Z={this.Z}";
        }
    }
}
