namespace TileMapService.Utils
{
    public static class SrsCodes // TODO: class name ?
    {
        /// <summary>
        /// EPSG:3857
        /// </summary>
        /// <remarks>
        /// WGS 84 / Pseudo-Mercator -- Spherical Mercator, Google Maps, OpenStreetMap, Bing, ArcGIS, ESRI
        /// https://epsg.io/3857
        /// </remarks>
        public const string EPSG3857 = "EPSG:3857";

        internal const int _3857 = 3857;

        /// <summary>
        /// EPSG:4326
        /// </summary>
        /// <remarks>
        /// WGS 84 -- WGS84 - World Geodetic System 1984, used in GPS
        /// https://epsg.io/4326
        /// </remarks>
        public const string EPSG4326 = "EPSG:4326";

        internal const int _4326 = 4326;

        /// <summary>
        /// EPSG:900913
        /// </summary>
        /// <remarks>
        /// Google Maps Global Mercator -- Spherical Mercator (unofficial - used in open source projects / OSGEO)
        /// https://epsg.io/900913
        /// </remarks>
        public const string EPSG900913 = "EPSG:900913";

        /// <summary>
        /// EPSG:41001
        /// </summary>
        /// <remarks>
        /// WGS84 / Simple Mercator - Spherical Mercator (unofficial deprecated OSGEO / Tile Map Service) 
        /// https://epsg.io/41001
        /// </remarks>
        public const string OSGEO41001 = "OSGEO:41001";
    }
}
