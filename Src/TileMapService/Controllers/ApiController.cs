using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace TileMapService.Controllers
{
    /// <summary>
    /// Custom API endpoint for managing tile sources.
    /// </summary>
    [Route("api")]
    public class ApiController : Controller
    {
        private readonly ITileSourceFabric tileSourceFabric;

        public ApiController(ITileSourceFabric tileSourceFabric)
        {
            this.tileSourceFabric = tileSourceFabric;
        }

        [HttpGet("sources")]
        public IActionResult GetSources()
        {
            // Simple authorization - allow only for local requests
            if (Request.IsLocal())
            {
                var result = this.tileSourceFabric.Sources;
                return Ok(result);
            }
            else
            {
                return Forbid();
            }
        }

        // TODO: ? full set of CRUS actions for sources
    }
}
