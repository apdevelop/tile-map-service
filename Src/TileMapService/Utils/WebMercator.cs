using System;
using System.Runtime.CompilerServices;

namespace TileMapService.Utils
{
    /// <summary>
    /// Various utility functions for EPSG:3857 / Web Mercator SRS and tile system.
    /// </summary>
    static class WebMercator
    {
        // Assumes SRS = EPSG:3857 / Web Mercator / Spherical Mercator
        // Based on https://docs.microsoft.com/en-us/bingmaps/articles/bing-maps-tile-system

        public const int TileSize = 256; // TODO: support for high resolution tiles

        private static readonly double EarthRadius = 6378137.0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Longitude(double x)
        {
            return (x / (EarthRadius * Math.PI / 180.0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Latitude(double y)
        {
            var fi = 2.0 * Math.Atan(Math.Exp(y / EarthRadius)) - Math.PI / 2.0;
            return MathHelper.RadiansToDegrees(fi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Models.Bounds GetTileBounds(int tileX, int tileY, int zoomLevel)
        {
            // PostGIS ST_TileEnvelope: https://postgis.net/docs/ST_TileEnvelope.html
            return new Models.Bounds(
                TileXtoEpsg3857X(tileX, zoomLevel),
                TileYtoEpsg3857Y(tileY + 1, zoomLevel),
                TileXtoEpsg3857X(tileX + 1, zoomLevel),
                TileYtoEpsg3857Y(tileY, zoomLevel));
        }

        public static Models.GeographicalBounds GetTileGeographicalBounds(int tileX, int tileY, int zoomLevel)
        {
            return new Models.GeographicalBounds(
                new Models.GeographicalPoint
                {
                    Longitude = PixelXToLongitude(TileSize * tileX, zoomLevel),
                    Latitude = PixelYToLatitude(TileSize * tileY + TileSize, zoomLevel),
                },
                new Models.GeographicalPoint
                {
                    Longitude = PixelXToLongitude(TileSize * tileX + TileSize, zoomLevel),
                    Latitude = PixelYToLatitude(TileSize * tileY, zoomLevel),
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TileXtoEpsg3857X(int tileX, int zoomLevel)
        {
            var mapSize = (double)MapSize(zoomLevel);
            var pixelX = tileX * TileSize;
            var x = (Utils.MathHelper.Clip(pixelX, 0.0, mapSize) / mapSize) - 0.5;
            var longitude = 360.0 * x;

            return EarthRadius * MathHelper.DegreesToRadians(longitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TileYtoEpsg3857Y(int tileY, int zoomLevel)
        {
            var mapSize = (double)MapSize(zoomLevel);
            var pixelY = tileY * TileSize;
            var y = 0.5 - (MathHelper.Clip(pixelY, 0.0, mapSize) / mapSize);
            var latitude = 90.0 - 360.0 * Math.Atan(Math.Exp(-y * 2.0 * Math.PI)) / Math.PI;

            return EarthRadius * MathHelper.Artanh(Math.Sin(MathHelper.DegreesToRadians(latitude)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TileCount(int zoomLevel)
        {
            return 1 << zoomLevel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MapSize(int zoomLevel)
        {
            return TileSize << zoomLevel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MapSize(double width, double longitudeMin, double longitudeMax)
        {
            if (width <= 0.0)
            {
                throw new ArgumentOutOfRangeException("width", width, "width <= 0.0");
            }

            if (longitudeMin >= longitudeMax)
            {
                throw new ArgumentException("longitudeMin >= longitudeMax");
            }

            var mapSize = width / ((longitudeMax - longitudeMin) / 360.0);

            return mapSize;
        }

        /// <summary>
        /// Flips tile Y coordinate (according to XYZ/TMS coordinate systems conversion).
        /// </summary>
        /// <param name="y">Tile Y coordinate.</param>
        /// <param name="zoom">Tile zoom level.</param>
        /// <returns>Flipped tile Y coordinate.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FlipYCoordinate(int y, int zoomLevel)
        {
            return (1 << zoomLevel) - y - 1;
        }

        public static double TileCoordinateXAtZoom(double longitude, int zoomLevel)
        {
            return LongitudeToPixelXAtZoom(longitude, zoomLevel) / (double)TileSize;
        }

        public static double TileCoordinateYAtZoom(double latitude, int zoomLevel)
        {
            return LatitudeToPixelYAtZoom(latitude, zoomLevel) / (double)TileSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LongitudeToPixelXAtZoom(double longitude, int zoomLevel)
        {
            return LongitudeToPixelX(longitude, (double)MapSize(zoomLevel));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LatitudeToPixelYAtZoom(double latitude, int zoomLevel)
        {
            return LatitudeToPixelY(latitude, (double)MapSize(zoomLevel));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LongitudeToPixelX(double longitude, double mapSize)
        {
            return ((longitude + 180.0) / 360.0) * mapSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LatitudeToPixelY(double latitude, double mapSize)
        {
            var sinLatitude = Math.Sin(MathHelper.DegreesToRadians(latitude));
            return (0.5 - Math.Log((1.0 + sinLatitude) / (1.0 - sinLatitude)) / (4.0 * Math.PI)) * mapSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double PixelXToLongitude(double pixelX, int zoomLevel)
        {
            var mapSize = (double)MapSize(zoomLevel);
            var x = (MathHelper.Clip(pixelX, 0.0, mapSize) / mapSize) - 0.5;

            return 360.0 * x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double PixelYToLatitude(double pixelY, int zoomLevel)
        {
            var mapSize = (double)MapSize(zoomLevel);
            var y = 0.5 - (MathHelper.Clip(pixelY, 0.0, mapSize) / mapSize);

            return 90.0 - 360.0 * Math.Atan(Math.Exp(-y * 2.0 * Math.PI)) / Math.PI;
        }
    }
}
