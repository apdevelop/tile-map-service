using System.IO;

namespace TileMapService.Tests
{
    /// <summary>
    /// Test environment configuration (port number, path to temporary files)
    /// </summary>
    internal static class TestConfiguration
    {
        public static readonly int portNumber = 5000;

        public static string BaseUrl => $"http://localhost:{portNumber}";

        public static string DataPath => Path.Join(Path.GetTempPath(), "TileMapServiceTestData");
    }
}
