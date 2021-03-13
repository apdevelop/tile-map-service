using System;
using System.Globalization;

namespace TileMapService.Models
{
    class GeographicalPointWithZoom
    {
        public double Longitude { get; set; }

        public double Latitude { get; set; }

        public int ZoomLevel { get; set; }

        public static GeographicalPointWithZoom FromMBTilesMetadataString(string s)
        {
            // TODO: check input format
            var items = s.Split(',');
            return new GeographicalPointWithZoom
            {
                Longitude = Double.Parse(items[0], CultureInfo.InvariantCulture),
                Latitude = Double.Parse(items[1], CultureInfo.InvariantCulture),
                ZoomLevel = Int32.Parse(items[2], CultureInfo.InvariantCulture),
            };
        }
    }
}

