using System;
using System.Globalization;

namespace TileMapService.Models
{
    /// <summary>
    /// Represents bounds in geographical (longitude-latitude) coordinates.
    /// </summary>
    public class GeographicalBounds
    {
        private readonly GeographicalPoint pointMin;

        private readonly GeographicalPoint pointMax;

        public GeographicalBounds(GeographicalPoint pointMin, GeographicalPoint pointMax)
        {
            this.pointMin = pointMin;
            this.pointMax = pointMax;
        }

        public GeographicalBounds(double minLongitude, double minLatitude, double maxLongitude, double maxLatitude)
        {
            this.pointMin = new GeographicalPoint(minLongitude, minLatitude);
            this.pointMax = new GeographicalPoint(maxLongitude, maxLatitude);
        }

        /// <summary>
        /// Creates <see cref="GeographicalBounds"/> from string.
        /// </summary>
        /// <param name="s">OpenLayers Bounds format: left, bottom, right, top), string of comma-separated numbers.</param>
        /// <returns></returns>
        public static GeographicalBounds FromCommaSeparatedString(string s)
        {
            ArgumentNullException.ThrowIfNull(s);

            var items = s.Split(',');
            if (items.Length != 4)
            {
                throw new FormatException("String should contain 4 comma-separated values");
            }

            return new GeographicalBounds(
                new GeographicalPoint(
                    double.Parse(items[0], CultureInfo.InvariantCulture),
                    double.Parse(items[1], CultureInfo.InvariantCulture)),
                new GeographicalPoint(
                    double.Parse(items[2], CultureInfo.InvariantCulture),
                    double.Parse(items[3], CultureInfo.InvariantCulture))
            );
        }

        public GeographicalPoint Min => this.pointMin;

        public GeographicalPoint Max => this.pointMax;

        public double MinLongitude => this.pointMin.Longitude;

        public double MinLatitude => this.pointMin.Latitude;

        public double MaxLongitude => this.pointMax.Longitude;

        public double MaxLatitude => this.pointMax.Latitude;
    }
}
