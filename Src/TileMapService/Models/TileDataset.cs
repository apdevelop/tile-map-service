namespace TileMapService.Models
{
    class TileDataset
    {
        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }

        public byte[]? ImageData { get; set; }

        public TileDataset()
        {

        }

        public TileDataset(int x, int y, int z, byte[] imageData)
        {
            X = x;
            Y = y;
            Z = z;
            ImageData = imageData;
        }
    }
}
