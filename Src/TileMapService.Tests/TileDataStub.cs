using System;
using System.Text;
using System.Text.Json;

namespace TileMapService.Tests
{
    class TileDataStub : IEquatable<TileDataStub>
    {
        private readonly int tileColumn;

        private readonly int tileRow;

        private readonly int zoomLevel;

        public TileDataStub(int tileColumn, int tileRow, int zoomLevel)
        {
            this.tileColumn = tileColumn;
            this.tileRow = tileRow;
            this.zoomLevel = zoomLevel;
        }

        class TileDataStubCoordinates
        {
            public int x { get; set; }

            public int y { get; set; }

            public int z { get; set; }
        }

        public TileDataStub(byte[] tileData)
        {
            var s = Encoding.UTF8.GetString(tileData);
            var coodinates = JsonSerializer.Deserialize<TileDataStubCoordinates>(s);

            this.tileColumn = coodinates.x;
            this.tileRow = coodinates.y;
            this.zoomLevel = coodinates.z;
        }

        public byte[] ToByteArray()
        {
            // Tiles aren't actually raster images, but stubs (binary raw data with coordinate values)
            var coordinates = new TileDataStubCoordinates
            {
                x = this.tileColumn,
                y = this.tileRow,
                z = this.zoomLevel,
            };

            var json = JsonSerializer.Serialize(coordinates);

            return Encoding.UTF8.GetBytes(json);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TileDataStub);
        }

        public bool Equals(TileDataStub other)
        {
            return other != null &&
                this.tileColumn == other.tileColumn &&
                this.tileRow == other.tileRow &&
                this.zoomLevel == other.zoomLevel;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.tileColumn, this.tileRow, this.zoomLevel);
        }
    }
}
