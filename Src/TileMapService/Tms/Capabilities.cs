using System;

namespace TileMapService.Tms
{
    class Capabilities
    {
        public string ServiceTitle { get; set; } = string.Empty;

        public string ServiceAbstract { get; set; } = string.Empty;

        public string? BaseUrl { get; set; }

        public Models.Layer[] Layers { get; set; } = [];
    }
}
