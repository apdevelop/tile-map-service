using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TileMapService.Controllers
{
    /// <summary>
    /// Serving tiles using Tile Map Service (TMS) protocol; with metadata.
    /// </summary>
    [Route("tms")]
    public class TmsController : Controller
    {
        private readonly ITileSourceFabric tileSources;

        public TmsController(ITileSourceFabric tileSources)
        {
            this.tileSources = tileSources;
        }

        private string BaseUrl
        {
            get
            {
                return $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}";
            }
        }

        [HttpGet("")]
        public IActionResult GetCapabilitiesServices()
        {
            // TODO: services/root.xml
            var xmlDoc = new Tms.CapabilitiesDocumentBuilder(this.BaseUrl, this.tileSources).GetServices();

            return XmlResponse(xmlDoc);
        }

        [HttpGet("1.0.0")]
        public IActionResult GetCapabilitiesTileMaps()
        {
            var xmlDoc = new Tms.CapabilitiesDocumentBuilder(this.BaseUrl, this.tileSources).GetTileMaps();
           
            return XmlResponse(xmlDoc);
        }

        [HttpGet("1.0.0/{tileset}")]
        public IActionResult GetCapabilitiesTileSets(string tileset)
        {
            var xmlDoc = new Tms.CapabilitiesDocumentBuilder(this.BaseUrl, this.tileSources).GetTileSets(tileset);

            return XmlResponse(xmlDoc);
        }

        /// <summary>
        /// Get tile from tileset with specified coordinates 
        /// URL format according to TMS 1.0.0 specs, like http://localhost:5000/tms/1.0.0/world/3/4/5.png
        /// </summary>
        /// <param name="tileset">Tileset name</param>
        /// <param name="x">X coordinate (tile column)</param>
        /// <param name="y">Y coordinate (tile row), Y axis goes up from the bottom</param>
        /// <param name="z">Z coordinate (zoom level)</param>
        /// <param name="extension"></param>
        /// <returns></returns>
        [HttpGet("1.0.0/{tileset}/{z}/{x}/{y}.{extension}")]
        public async Task<IActionResult> GetTileAsync(string tileset, int x, int y, int z, string extension)
        {
            if (String.IsNullOrEmpty(tileset) || String.IsNullOrEmpty(extension))
            {
                return BadRequest();
            }

            if (this.tileSources.Contains(tileset))
            {
                // TODO: check extension == tileset.Configuration.Format
                var tileSource = this.tileSources.Get(tileset);
                var data = await tileSource.GetTileAsync(x, y, z);
                if (data != null)
                {
                    return File(data, tileSource.ContentType); // TODO: file name
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return NotFound($"Specified tileset '{tileset}' not found");
            }
        }

        private IActionResult XmlResponse(XmlDocument xml)
        {
            using (var ms = new MemoryStream())
            {
                using (var xw = XmlWriter.Create(new StreamWriter(ms, Encoding.UTF8)))
                {
                    xml.Save(xw);
                }

                return File(ms.ToArray(), MediaTypeNames.Text.Xml);
            }
        }
    }
}
