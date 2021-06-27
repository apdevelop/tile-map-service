namespace TileMapService.Models
{
    class GeographicalBounds
    {
        private GeographicalPoint pointMin;

        private GeographicalPoint pointMax;

        public GeographicalBounds()
        {

        }

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

        public double MinLongitude
        {
            get
            {
                return this.pointMin.Longitude;
            }
        }

        public double MaxLongitude
        {
            get
            {
                return this.pointMax.Longitude;
            }
        }

        public double MinLatitude
        {
            get
            {
                return this.pointMin.Latitude;
            }
        }

        public double MaxLatitude
        {
            get
            {
                return this.pointMax.Latitude;
            }
        }
    }
}
