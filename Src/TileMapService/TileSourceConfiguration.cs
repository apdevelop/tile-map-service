namespace TileMapService
{
    public class TileSourceConfiguration
    {
        public string Format { get; set; }

        public string Name { get; set; }

        public string Source { get; set; }

        /// <summary>
        /// TMS type Y coord (true: Y going from bottom to top; false: from top to bottom, like in OSM tiles)
        /// </summary>
        public bool Tms { get; set; } // TODO: ? name it FlipY with default true for MBTiles
    }
}
