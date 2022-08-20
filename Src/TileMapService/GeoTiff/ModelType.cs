namespace TileMapService.GeoTiff
{
    /// <summary>
    /// Model type.
    /// </summary>
    enum ModelType
    {
        /// <summary>
        /// Projected coordinate system.
        /// </summary>
        Projected = 1,

        /// <summary>
        /// Geographic (latitude-longitude) coordinate system.
        /// </summary>
        Geographic = 2,

        /// <summary>
        /// Geocentric (X,Y,Z) coordinate system.
        /// </summary>
        Geocentric = 3,
    }
}
