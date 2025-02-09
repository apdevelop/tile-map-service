using System;

namespace TileMapService.Tms
{
    class Capabilities
    {
        public string ServiceTitle { get; set; } = String.Empty;

        public string ServiceAbstract { get; set; } = String.Empty;

        public string? BaseUrl { get; set; }

        public Models.Layer[] Layers { get; set; } = Array.Empty<Models.Layer>();
    }
}
