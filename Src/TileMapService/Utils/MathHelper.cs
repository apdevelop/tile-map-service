using System;
using System.Runtime.CompilerServices;

namespace TileMapService.Utils
{
    static class MathHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clip(double value, double minValue, double maxValue)
        {
            return Math.Min(Math.Max(value, minValue), maxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RadiansToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Artanh(double x)
        {
            // https://en.wikipedia.org/wiki/Inverse_hyperbolic_function#Inverse_hyperbolic_tangent
            return 0.5 * Math.Log((1.0 + x) / (1.0 - x));
        }
    }
}
