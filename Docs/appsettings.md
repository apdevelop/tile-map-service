### Configuration file structure

### Common service properties

Common service properties are defined in `Service` section of `appsettings.json` file and mostly common used in WMTS and WMS capabilities documents.

#### title
Type: `String`<br>
Required: `false`

User-friendly title (displayed name) of service.

#### abstract
Type: `String`<br>
Required: `false`

Detailed text description of service.

#### keywords
Type: `String`<br>
Required: `false`

Keywords describing service.

#### jpegQuality
Type: `Number`<br>
Required: `false`

Quality (compression level) for JPEG output in WMS endpoint, in 0..100 range, default 90.


### Tile sources

Tile sources are defined in `Sources` section of `appsettings.json` file.

Configuration values priority:
* Default values for given tile source type.
* Actual values (`MBTiles` metadata, files properties).
* Values from configuration file - highest priority, overrides given above, if provided.

#### type
Type: `String`<br>
Required: `true`

Used to define source type, must be one of `file`, `mbtiles`, `postgis`, `xyz`, `tms`, `wmts`, `wms`, `geotiff` (case insensitive).

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

#### abstract
Type: `String`<br>
Required: `false`

Detailed text description of tile source.

#### location
Type: `String`<br>
Required: `true`

Location of tiles. 
Path template for `file`, full path for `mbtiles`, `geotiff` types, url template for `xyz` and `tms`, base url for `wmts` and `wms`.
Template string uses `{x}`, `{y}`, `{z}` as placeholders for corresponding coordinate values.

WMS location should contain base url of WMS service along with `version`, `layers`, `srs`/`crs` values. 
Other values, like `styles`, `transparent`, `bgcolor` and so on are optional. The only `srs`/`crs` currently supported is `EPSG:3857` or compatible.

PostGIS location should contain connection string for PostgreSQL database.

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

#### cache
Type: `Object`<br>
Required: `false`

Caching option for sources of type `xyz`, `tms`, `wmts`, `wms`.

#### type
Type: `String`<br>
Required: `false`
Must be `mbtiles` string.

#### dbfile
Type: `String`<br>
Required: `true`
Full path to `mbtiles` database file to store cached tiles. File will be created automatically, if not exists.


#### wmts
Type: `Object`<br>
Required: `false`

Source of type `wmts` (WMTS service and layer) properties. This values has priority over `location` url parameters.

#### capabilitiesurl
Type: `String`<br>
Required: `true`
WMTS Capabilities document url like `http://example.com/wmts/1.0.0/WMTSCapabilities.xml` or just base WMTS service url like `http://example.com/wmts/`.

#### layer
Type: `String`<br>
Required: `true`
Layer identifier.

#### style
Type: `String`<br>
Required: `false`
Style identifier, `default` if not defined.

#### tilematrixset
Type: `String`<br>
Required: `true`
TileMatrixSet identifier.


#### wms
Type: `Object`<br>
Required: `false`

Source of type `wms` (WMS service and layer) properties. This values has priority over `location` url parameters.

#### layer
Type: `String`<br>
Required: `true`
Layer identifier (`layers` parameter is WMS request).

#### version
Type: `String`<br>
Required: `true`
WMS version identifier (must be `1.1.1` or `1.3.0`).


#### postgis
Type: `Object`<br>
Required: `false`

Table options for source of type `postgis` only.

#### table
Type: `String`<br>
Required: `true`
Name of table with features in database.

#### geometry
Type: `String`<br>
Required: `true`
Name of column with `geometry` data (with `EPSG:3857` SRS only) in table.

#### fields
Type: `String`<br>
Required: `false`
Comma-separated string with column names with additional attributes of features, like id, name and so on.
Empty string or not defined, if no additional attributes is required.