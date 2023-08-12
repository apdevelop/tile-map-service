using System;
using System.Runtime.CompilerServices;

namespace TileMapService.Utils
{
    /// <summary>
    /// Various utility functions for EPSG:3857 / Web Mercator SRS and tile system.
    /// (<see href="https://docs.microsoft.com/en-us/bingmaps/articles/bing-maps-tile-system">Bing Maps Tile System</see>)
    /// </summary>
    public static class WebMercator
    {
        public const int TileSize = 256; // TODO: cusom resolution values

        public const int DefaultTileSize = 256;

        /// <summary>
        /// Default tile width, pixels.
        /// </summary>
        public const int DefaultTileWidth = 256;

        /// <summary>
        /// Default tile height, pixels.
        /// </summary>
        public const int DefaultTileHeight = 256;

        private const double EarthRadius = 6378137.0;

        private const double MaxLatitude = 85.0511287798;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInsideBBox(int x, int y, int z, string? srs)
        {
            int xmin, xmax;
            int ymin = 0;
            int ymax = (1 << z) - 1;
            switch (srs)
            {
                case SrsCodes.EPSG3857: { xmin = 0; xmax = (1 << z) - 1; break; }
                case SrsCodes.EPSG4326: { xmin = 0; xmax = 2 * (1 << z) - 1; break; }
                default: throw new ArgumentOutOfRangeException(nameof(srs));
            }

            return x >= xmin && x <= xmax && y >= ymin && y <= ymax;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Longitude(double x) =>
            x / (EarthRadius * Math.PI / 180.0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Latitude(double y) =>
            MathHelper.RadiansToDegrees(2.0 * Math.Atan(Math.Exp(y / EarthRadius)) - Math.PI / 2.0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double X(double longitude) =>
            EarthRadius * MathHelper.DegreesToRadians(longitude);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Y(double latitude) =>
            EarthRadius * MathHelper.Artanh(Math.Sin(MathHelper.DegreesToRadians(Math.Max(Math.Min(MaxLatitude, latitude), -MaxLatitude))));

        /// <summary>
        /// Computes tile bounds for given coordinates (x, y, z).
        /// </summary>
        /// <param name="tileX">Tile X coordinate.</param>
        /// <param name="tileY">Tile Y coordinate.</param>
        /// <param name="zoomLevel">Zoom level.</param>
        /// <returns>Tile bounds.</returns>
        /// <remarks>
        /// Similar to PostGIS ST_TileEnvelope function: https://postgis.net/docs/ST_TileEnvelope.html
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Models.Bounds GetTileBounds(int tileX, int tileY, int zoomLevel)
        {
            return new Models.Bounds(
                TileXtoEpsg3857X(tileX, zoomLevel),
                TileYtoEpsg3857Y(tileY + 1, zoomLevel),
                TileXtoEpsg3857X(tileX + 1, zoomLevel),
                TileYtoEpsg3857Y(tileY, zoomLevel));
        }

        public static Models.GeographicalBounds GetTileGeographicalBounds(int tileX, int tileY, int zoomLevel)
        {
            return new Models.GeographicalBounds(
                new Models.GeographicalPoint(PixelXToLongitude(TileSize * tileX, zoomLevel), PixelYToLatitude(TileSize * tileY + TileSize, zoomLevel)),
                new Models.GeographicalPoint(PixelXToLongitude(TileSize * tileX + TileSize, zoomLevel), PixelYToLatitude(TileSize * tileY, zoomLevel)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TileXtoEpsg3857X(int tileX, int zoomLevel)
        {
            var mapSize = (double)MapSize(zoomLevel, DefaultTileSize);
            var pixelX = tileX * TileSize;
            var x = (Utils.MathHelper.Clip(pixelX, 0.0, mapSize) / mapSize) - 0.5;
            var longitude = 360.0 * x;

            return EarthRadius * MathHelper.DegreesToRadians(longitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TileYtoEpsg3857Y(int tileY, int zoomLevel)
        {
            var mapSize = (double)MapSize(zoomLevel, DefaultTileSize);
            var pixelY = tileY * TileSize;
            var y = 0.5 - (MathHelper.Clip(pixelY, 0.0, mapSize) / mapSize);
            var latitude = 90.0 - 360.0 * Math.Atan(Math.Exp(-y * 2.0 * Math.PI)) / Math.PI;

            return EarthRadius * MathHelper.Artanh(Math.Sin(MathHelper.DegreesToRadians(latitude)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TileCount(int zoomLevel) =>
            1 << zoomLevel;

        /// <summary>
        /// Returns entire world map image size in pixels at given zoom level.
        /// </summary>
        /// <param name="zoomLevel">Zoom level.</param>
        /// <param name="tileSize">Tile size (width and height) in pixels.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MapSize(int zoomLevel, int tileSize) =>
            tileSize << zoomLevel;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MapSize(double width, double longitudeMin, double longitudeMax)
        {
            if (width <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be greater than zero.");
            }

            if (longitudeMin >= longitudeMax)
            {
                throw new ArgumentException("longitudeMin >= longitudeMax");
            }

            var mapSize = width / ((longitudeMax - longitudeMin) / 360.0);

            return mapSize;
        }

        /// <summary>
        /// Flips tile Y coordinate (according to XYZ-TMS coordinate systems conversion).
        /// </summary>
        /// <param name="y">Tile Y coordinate.</param>
        /// <param name="zoomLevel">Tile zoom level.</param>
        /// <returns>Flipped tile Y coordinate.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FlipYCoordinate(int y, int zoomLevel) => (1 << zoomLevel) - y - 1;

        public static double TileCoordinateXAtZoom(double longitude, int zoomLevel) =>
            LongitudeToPixelXAtZoom(longitude, zoomLevel) / (double)TileSize;

        public static double TileCoordinateYAtZoom(double latitude, int zoomLevel) =>
            LatitudeToPixelYAtZoom(latitude, zoomLevel) / (double)TileSize;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LongitudeToPixelXAtZoom(double longitude, int zoomLevel) =>
            LongitudeToPixelX(longitude, (double)MapSize(zoomLevel, DefaultTileSize));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LatitudeToPixelYAtZoom(double latitude, int zoomLevel) =>
            LatitudeToPixelY(latitude, (double)MapSize(zoomLevel, DefaultTileSize));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LongitudeToPixelX(double longitude, double mapSize) =>
            ((longitude + 180.0) / 360.0) * mapSize;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LatitudeToPixelY(double latitude, double mapSize)
        {
            var sinLatitude = Math.Sin(MathHelper.DegreesToRadians(latitude));
            return (0.5 - Math.Log((1.0 + sinLatitude) / (1.0 - sinLatitude)) / (4.0 * Math.PI)) * mapSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double PixelXToLongitude(double pixelX, int zoomLevel)
        {
            var mapSize = (double)MapSize(zoomLevel, DefaultTileSize);
            var x = (MathHelper.Clip(pixelX, 0.0, mapSize) / mapSize) - 0.5;

            return 360.0 * x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double PixelYToLatitude(double pixelY, int zoomLevel)
        {
            var mapSize = (double)MapSize(zoomLevel, DefaultTileSize);
            var y = 0.5 - (MathHelper.Clip(pixelY, 0.0, mapSize) / mapSize);

            return 90.0 - 360.0 * Math.Atan(Math.Exp(-y * 2.0 * Math.PI)) / Math.PI;
        }
    }
}
