namespace TileMapService
{
    public class TileSourceConfiguration
    {
        public string Format { get; set; } // TODO: get from actual source properties

        public string Name { get; set; }

        // TODO: User-friendly name (Title)

        public string Source { get; set; }

        /// <summary>
        /// TMS type Y coord (true: Y going from bottom to top; false: from top to bottom, like in OSM tiles)
        /// </summary>
        public bool Tms { get; set; } // TODO: default true for MBTiles
    }
}
