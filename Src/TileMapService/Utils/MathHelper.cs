using System;
using System.Runtime.CompilerServices;

namespace TileMapService.Utils
{
    static class MathHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clip(double value, double minValue, double maxValue) => Math.Min(Math.Max(value, minValue), maxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RadiansToDegrees(double radians) => radians * (180.0 / Math.PI);

        /// <summary>
        /// Returns the inverse hyperbolic tangent of a specified number.
        /// </summary>
        /// <param name="x">The number whose inverse hyperbolic tangent is to be found.</param>
        /// <remarks>See https://en.wikipedia.org/wiki/Inverse_hyperbolic_function#Inverse_hyperbolic_tangent</remarks>
        /// <returns>The inverse hyperbolic tangent of a specified number, measured in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Artanh(double x) => 0.5 * Math.Log((1.0 + x) / (1.0 - x));
    }
}
