using System;
using System.Globalization;

namespace TileMapService.Models
{
    class Bounds
    {
        public double Left { get; set; }

        public double Bottom { get; set; }

        public double Right { get; set; }

        public double Top { get; set; }

        public Bounds()
        { 
        
        }

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
        public static Bounds FromMBTilesMetadataString(string s)
        {
            // TODO: check input format
            var items = s.Split(',');
            return new Bounds
            {
                Left = Double.Parse(items[0], CultureInfo.InvariantCulture),
                Bottom = Double.Parse(items[1], CultureInfo.InvariantCulture),
                Right = Double.Parse(items[2], CultureInfo.InvariantCulture),
                Top = Double.Parse(items[3], CultureInfo.InvariantCulture),
            };
        }

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

