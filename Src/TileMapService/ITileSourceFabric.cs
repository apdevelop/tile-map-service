using System.Collections.Generic;
using System.Threading.Tasks;

using TileMapService.Models;

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
        /// <param name="id">Identifier of tile source.</param>
        /// <returns></returns>
        ITileSource Get(string id);

        /// <summary>
        /// Returns list of sources configuration and properties.
        /// </summary>
        List<SourceConfiguration> Sources { get; }

        /// <summary>
        /// Returns entire service properties.
        /// </summary>
        ServiceProperties ServiceProperties { get; }
    }
}
