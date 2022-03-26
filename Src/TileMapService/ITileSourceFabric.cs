using System.Collections.Generic;
using System.Threading.Tasks;

namespace TileMapService
{
    public interface ITileSourceFabric
    {
        Task InitAsync();

        /// <summary>
        /// Returns true, if source with given id exists in Sources.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool Contains(string id);

        /// <summary>
        /// Gets the tile source by given identifier <paramref name="id"/>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ITileSource Get(string id);

        List<SourceConfiguration> Sources { get; }

        ServiceProperties ServiceProperties { get; }
    }
}
