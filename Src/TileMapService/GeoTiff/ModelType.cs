namespace TileMapService.GeoTiff
{
    /// <summary>
    /// Model Type.
    /// </summary>
    enum ModelType
    {
        /// <summary>
        /// Projection Coordinate System.
        /// </summary>
        Projected = 1,

        /// <summary>
        /// Geographic latitude-longitude System.
        /// </summary>
        Geographic = 2,

        /// <summary>
        /// Geocentric (X,Y,Z) Coordinate System.
        /// </summary>
        Geocentric = 3,
    }
}
