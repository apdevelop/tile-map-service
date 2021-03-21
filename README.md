# Tile Map Service for .NET 5
Basic implementation of tile server for .NET 5 platform. Provides access to raster tiles stored in MBTiles database or local file system. Serving tiles using XYZ, TMS and WMTS protocols.

### Demo page
![Demo page](https://github.com/apdevelop/tile-map-service-net5/blob/master/Docs/demo-page.png)

### Features
* Supported tile sources:
  * [MBTiles](https://github.com/mapbox/mbtiles-spec).
  * Local file system (each tile in separate file).
* Supported protocols for serving tiles: 
  * XYZ ([Tiled web map](https://en.wikipedia.org/wiki/Tiled_web_map)) [http://localhost:5000/xyz](http://localhost:5000/xyz/{tileset}/?x={x}&y={y}&z={z}).
  * TMS ([Tile Map Service](https://en.wikipedia.org/wiki/Tile_Map_Service)) [http://localhost:5000/tms](http://localhost:5000/tms).
  * WMTS ([Web Map Tile Service](https://en.wikipedia.org/wiki/Web_Map_Tile_Service))  [http://localhost:5000/wmts](http://localhost:5000/wmts?request=GetCapabilities).
* Coordinate system / tile grid: currently [Web Mercator / Spherical Mercator / EPSG:3857](https://en.wikipedia.org/wiki/Web_Mercator_projection) only.
* Formats: currently raster (PNG, JPEG) images only.
* Configuration in JSON file.

### Technologies
Developed using MS Visual Studio 2019 (v16.8.5) with .NET 5 SDK (v5.0.103).
Using
* [Microsoft.Data.Sqlite](https://docs.microsoft.com/ru-ru/dotnet/standard/data/sqlite/) for working with SQLite database
* [Leaflet](https://github.com/Leaflet) for map demo page

### Configuration file

Tile sources are defined in [appsettings.json](https://github.com/apdevelop/tile-map-service-net5/blob/master/Docs/appsettings.md) configuration file.

### Running framework-dependent deployment

Check presence of .NET 5 runtime on target system using command:

`dotnet --info`

The `Microsoft.AspNetCore.App 5.0.3` (or later version) should present in list.

Run the application using command:

`dotnet TileMapService.dll`

After start, it will listen on default TCP port 5000 (using in-process `Kestrel` web server) 
and tile service with demo page will be available on http://localhost:5000/ address; to enable remote calls allow connections to this port in firewall settings.

### TODOs
* Full implementation of MBTiles specification.
* Support for more formats (vector tiles) and coordinate systems.
* Include test dataset(s) created from free data.
* Compare with reference implementations.
* Integration / end-to-end tests.
* HTTP tile source.
* Extended diagnostics and logging.
* Performance tests.
* Live demo.

### Some MBTiles sample datasets
* [World Countries MBTiles by ArcGIS / EsriAndroidTeam](https://www.arcgis.com/home/item.html?id=7b650618563741ca9a5186c1aa69126e)
* [Satellite Lowres raster tiles Planet by MapTiler](https://data.maptiler.com/downloads/dataset/satellite-lowres/)

### References
* [MBTiles Specification](https://github.com/mapbox/mbtiles-spec)
* [Tile Map Service Specification](https://wiki.osgeo.org/index.php?title=Tile_Map_Service_Specification)
* [OpenGIS Web Map Tile Service Implementation Standard](https://www.ogc.org/standards/wmts)
* [Using TMS in Leaflet](https://leafletjs.com/examples/wms/wms.html)
* [QGIS as OGC Data Client](https://docs.qgis.org/2.18/en/docs/user_manual/working_with_ogc/ogc_client_support.html)
