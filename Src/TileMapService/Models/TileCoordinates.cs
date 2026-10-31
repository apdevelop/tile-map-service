namespace TileMapService.Models
{
    public class TileCoordinates
    {
        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }

        public TileCoordinates()
        {

        }

        public TileCoordinates(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString() => $"X={X} Y={Y} Z={Z}";
    }
}
