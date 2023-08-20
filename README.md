# Tile Map Service for .NET 5 / .NET 7
Simple and lightweight implementation of tile server basic features for .NET 5 / .NET 7 platforms. Provides access to tiles stored in several source types and serving them using various protocols.

### Demo page
![Demo page](https://github.com/apdevelop/tile-map-service/blob/master/Docs/demo-page.png)

### Features
* Supported tile sources:

| Source type               | EPSG:3857  | EPSG:4326 | Notes                                                                                                  |
| ------------------------- |:----------:|:---------:|--------------------------------------------------------------------------------------------------------|
| Local file system         | &#10003;   | &#10003;  | Each tile in separate file in Z/X/Y.ext folder structure                                               |
| MBTiles (SQLite)          | &#10003;   | &mdash;   | [MBTiles 1.3 Specification](https://github.com/mapbox/mbtiles-spec/blob/master/1.3/spec.md)            |
| GeoTIFF local file        | &#10003;   | &#10003;  | [GeoTIFF](https://en.wikipedia.org/wiki/GeoTIFF) basic support with `EPSG:3857` or `EPSG:4326` source image SRS only  |
| XYZ tile service          | &#10003;   | &#10003;  | [XYZ](https://en.wikipedia.org/wiki/Tiled_web_map) with local cache for `EPSG:3857` SRS                |
| TMS tile service          | &#10003;   | &#10003;  | [TMS](https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification) with local cache for `EPSG:3857` SRS |
| WMTS tile service         | &#10003;   | &#10003;  | [WMTS](https://www.ogc.org/standards/wmts) with local cache for `EPSG:3857` SRS                        |
| WMS service               | &#10003;   | &mdash;   | [WMS](https://en.wikipedia.org/wiki/Web_Map_Service), versions 1.1.1 and 1.3.0, cache for `EPSG:3857` SRS  |
| PostGIS database          | &#10003;   | &mdash;   | [Mapbox Vector Tiles](https://github.com/mapbox/vector-tile-spec) from `geometry` column with `EPSG:3857` SRS only |

* Supported protocols (service endpoints) for serving tiles: 

| Endpoint                                                                          | EPSG:3857 | EPSG:4326 | Endpoint&nbsp;Url | Formats   | Notes                                     |
| --------------------------------------------------------------------------------- |:---------:|:---------:|--------------|-----------|--------------------------------------------------------------------------------------------|
| XYZ ([Tiled web map](https://en.wikipedia.org/wiki/Tiled_web_map))                | &#10003;  | &#10003;  | `/xyz`       | png, jpeg, webp, mvt | Can be REST style url (/{z}/{x}/{y}.ext) or url with parameters (&x={x}&y={y}&z={z}) |
| TMS ([Tile Map Service](https://en.wikipedia.org/wiki/Tile_Map_Service))          | &#10003;  | &#10003;  | `/tms`       | png, jpeg, webp, mvt |                               |
| WMTS ([Web Map Tile Service](https://en.wikipedia.org/wiki/Web_Map_Tile_Service)) | &#10003;  | &mdash;   | `/wmts`      | png, jpeg, webp, mvt | Support both `RESTful` and `KVP` `GetTile` url syntax   |
| WMS ([Web Map Service](https://en.wikipedia.org/wiki/Web_Map_Service))            | &#10003;  | &mdash;   | `/wms`       | png, jpeg, tiff (geotiff) |WMS versions `1.1.1` and `1.3.0` |

* Coordinate system / tile grid support: [Web Mercator / Spherical Mercator / EPSG:3857](https://en.wikipedia.org/wiki/Web_Mercator_projection), basic support for geodetic `EPSG:4326`.
* Tile image formats: raster (`PNG`, `JPEG`, `WEBP`) 256&#215;256 pixels tiles, basic support of `TIFF` output and `PBF` / `MVT` (vector tiles).
* Local cache for tiles from external tile services sources (modified `mbtiles` format database file, `EPSG:3857` only), with blank tiles detection support.
* Configuration in JSON file.
* Reading sources configuration using `/api` endpoint (local requests only).

### Technologies
There are two separate solutions and corresponding projects, sharing the same source code files:

| Property           | .NET 5    | .NET 7    |
| ------------------ |:---------:|:---------:|
| SDK                | .NET 5.0  | .NET 7.0  |
| MS Visual Studio   | 2019      | 2022      |
| Status             | Legacy    | Active    |

Using
* [Microsoft.Data.Sqlite](https://docs.microsoft.com/ru-ru/dotnet/standard/data/sqlite/) for working with SQLite database.
* [SkiaSharp](https://github.com/mono/SkiaSharp) for raster images processing.
* [BitMiracle.LibTiff.NET](https://github.com/BitMiracle/libtiff.net) for reading source GeoTIFF files and creating output TIFF images.
* [Npgsql](https://github.com/npgsql/npgsql) .NET data provider for PostgreSQL.
* [Leaflet](https://github.com/Leaflet) for map demo page.
* [NUnit](https://nunit.org/) for tests.

### Configuration file

Tile sources are defined in [appsettings.json](https://github.com/apdevelop/tile-map-service-net5/blob/master/Docs/appsettings.md) configuration file.

### Running framework-dependent deployment

Check if .NET 5 or .NET 7 runtime is installed on target system:

`dotnet --info`

The `Microsoft.AspNetCore.App 5.0.3` / `7.0.5` (or later versions) should present in list.

*There is known issue for .NET 5 and libssl 3.x compatibility on Linux systems, use .NET 7 in this case.*

Run the application using command:

`dotnet tms.dll`

After start, it will listen on default TCP port 5000 (using in-process `Kestrel` web server) 
and tile service with demo page will be available on `http://localhost:5000/` address; to enable remote calls allow connections to this port in firewall settings.

### TODOs
* Support for more formats (image formats, vector tiles) and coordinate systems (tile grids).
* Flexible settings of tile sources.
* Configuration Web API / Web UI with authentication.
* WMS and Vector Tiles (mvt) client in Web UI.
* .NET 8 project and solution.
* Compare with reference implementations (servers and clients).
* Using metatiles for better tiles quality.
* Include test dataset(s) created from free data.
* Extended diagnostics, error handling and logging.
* Performance tests.
* Live demo.

### Some MBTiles sample datasets
* [World Countries MBTiles by ArcGIS / EsriAndroidTeam](https://www.arcgis.com/home/item.html?id=7b650618563741ca9a5186c1aa69126e)
* [Satellite Lowres raster tiles Planet by MapTiler](https://data.maptiler.com/downloads/dataset/satellite-lowres/)
* [Custom vector tiles from Georaphy Class example MVT](https://github.com/klokantech/vector-tiles-sample/releases/tag/v1.0)

All external tile sources (services) in the provided `appsettings.json` file are only for development / testing, not for production use.

### References
* [MBTiles Specification](https://github.com/mapbox/mbtiles-spec/)
* [Tile Map Service Specification](https://wiki.osgeo.org/wiki/Tile_Map_Service_Specification)
* [OpenGIS Web Map Tile Service Implementation Standard](https://www.ogc.org/standard/wmts/)
* [Serving Dynamic Vector Tiles from PostGIS](https://blog.crunchydata.com/blog/dynamic-vector-tiles-from-postgis)
* [GeoTIFF Format Specification](http://geotiff.maptools.org/spec/geotiffhome.html)
* [Using WMS and TMS in Leaflet](https://leafletjs.com/examples/wms/wms.html)
* [QGIS User Guide: Working with OGC / ISO protocols](https://docs.qgis.org/3.28/en/docs/user_manual/working_with_ogc/ogc_client_support.html)
