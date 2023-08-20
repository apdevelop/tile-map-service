namespace TileMapService.Models
{
    public class RasterProperties // TODO: store source parameters as-is
    {
        public int Srid { get; set; }

        public int ImageWidth { get; set; }

        public int ImageHeight { get; set; }

        public int TileWidth { get; set; }

        public int TileHeight { get; set; }

        public int TileSize { get; set; }

        /// <summary>
        /// Bounds in EPSG:4326 SRS.
        /// </summary>
        public GeographicalBounds? GeographicalBounds { get; set; }

        /// <summary>
        /// Bounds in EPSG:3857 SRS.
        /// </summary>
        public Bounds? ProjectedBounds { get; set; }

        public double PixelWidth { get; set; }

        public double PixelHeight { get; set; }
    }
}
