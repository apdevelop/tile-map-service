### Configuration file structure

Configuration values priority:
* Default values for given tile source type.
* Actual values (`MBTiles` metadata, files properties).
* Values from configuration file - highest priority, overrides given above, if provided.

Tile sources are defined in `TileSources` section of `appsettings.json` file.

#### type
Type: `String`<br>
Required: `true`

Used to define source type, must be `file` or `mbtiles` (case insensitive).

#### id
Type: `String`<br>
Required: `true`

String identifier of tile source (case sensitive).

#### format
Type: `String`<br>
Required: `false`

Name of tiles image format (must be `png` or `jpg`).

#### title
Type: `String`<br>
Required: `false`

User-friendly title (displayed name) of tile source.

#### location
Type: `String`<br>
Required: `true`

Location of tiles (path template for `file`, full path for `mbtiles` type).

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
