### Configuration file structure

Configuration values priority:
* Default values for given tile source type.
* Actual values (`MBTiles` metadata, files properties).
* Values from configuration file - highest priority, overrides given above, if provided.

Tile sources are defined in `TileSources` section of `appsettings.json` file.

#### type
Type: `String`<br>
Required: `true`

Used to define source type, must be one of `file`, `mbtiles`, `xyz`, `tms`, `wmts`, `wms`, `geotiff` (case insensitive).

#### id
Type: `String`<br>
Required: `true`

String identifier of tile source (case sensitive).

#### format
Type: `String`<br>
Required: `false`

Name of tiles raster image format (must be `png` or `jpg`).

#### title
Type: `String`<br>
Required: `false`

User-friendly title (displayed name) of tile source.

#### location
Type: `String`<br>
Required: `true`

Location of tiles. 
Path template for `file`, full path for `mbtiles`, `geotiff` types, url template for `xyz` and `tms`, base url for `wmts` and `wms`.
Template string uses `{x}`, `{y}`, `{z}` as placeholders for corresponding coordinate values.

WMS location should contain base url of WMS service along with `version`, `layers`, `srs`/`crs` values. 
Other values, like `styles`, `transparent`, `bgcolor` and so on are optional. The only `srs`/`crs` currently supported is `EPSG:3857` or compatible.

#### srs
Type: `String`<br>
Required: `false`
Default: `EPSG:3857`

Spatial reference system (SRS), EPSG code. Possible values are `EPSG:3857` and `EPSG:4326`.

#### tms
Type: `Boolean`<br>
Required: `false`

TMS type Y coordinate (true: Y going from bottom to top; false: from top to bottom, like in OSM tiles).

#### minzoom
Type: `Number`<br>
Required: `false`

Minimum zoom of tile source.

#### maxzoom
Type: `Number`<br>
Required: `false`

Maximum zoom of tile source.
