using System;
using System.Globalization;

namespace TileMapService.Models
{
    /// <summary>
    /// Represents rectangular area, defined by two opposite corners.
    /// </summary>
    public class Bounds
    {
        /// <summary>
        /// X coordinate of bottom left corner.
        /// </summary>
        public double Left { get; set; }

        /// <summary>
        /// Y coordinate of bottom left corner.
        /// </summary>
        public double Bottom { get; set; }

        /// <summary>
        /// X coordinate of top right corner.
        /// </summary>
        public double Right { get; set; }

        /// <summary>
        /// Y coordinate of top right corner.
        /// </summary>
        public double Top { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Bounds"/> with zero coordinates.
        /// </summary>
        public Bounds()
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="Bounds"/> from given coordinates of corners.
        /// </summary>
        /// <param name="left">X coordinate of bottom left corner.</param>
        /// <param name="bottom">Y coordinate of bottom left corner.</param>
        /// <param name="right">X coordinate of top right corner.</param>
        /// <param name="top">Y coordinate of top right corner.</param>
        /// <returns>The new instance of <see cref="Bounds"/>.</returns>
        public Bounds(double left, double bottom, double right, double top)
        {
            this.Left = left;
            this.Bottom = bottom;
            this.Right = right;
            this.Top = top;
        }

        /// <summary>
        /// Creates <see cref="Bounds"/> from string.
        /// </summary>
        /// <param name="s">OpenLayers Bounds format: left, bottom, right, top), string of comma-separated numbers.</param>
        /// <returns></returns>
        public static Bounds FromCommaSeparatedString(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            var items = s.Split(',');
            if (items.Length != 4)
            {
                throw new FormatException("String should contain 4 comma-separated values");
            }

            return new Bounds
            {
                Left = Double.Parse(items[0], CultureInfo.InvariantCulture),
                Bottom = Double.Parse(items[1], CultureInfo.InvariantCulture),
                Right = Double.Parse(items[2], CultureInfo.InvariantCulture),
                Top = Double.Parse(items[3], CultureInfo.InvariantCulture),
            };
        }

        /// <summary>
        /// Converts instance to its string representation.
        /// </summary>
        /// <returns>The string representation of the instance.</returns>
        public string ToBBoxString()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3}",
                this.Left,
                this.Bottom,
                this.Right,
                this.Top);
        }
    }
}
