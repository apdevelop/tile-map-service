namespace TileMapService.Models
{
    class TileDataset
    {
        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }

        public byte[] ImageData { get; set; }

        public TileDataset()
        {

        }

        public TileDataset(int x, int y, int z, byte[] imageData)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.ImageData = imageData;
        }
    }
}
