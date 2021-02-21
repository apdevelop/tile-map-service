(function () {

    L.GridLayer.TileGrid = L.GridLayer.extend({
        createTile: function (coords) {
            var isTms = this.getCurrentBaseMap().options.tms;
            var tile = document.createElement('div');
            tile.innerHTML =
                '<span style="display: inline-block; line-height: 256px; vertical-align: middle; font-size: 32px; font-weight: bold; text-shadow: -1px 0 white, 0 1px white, 1px 0 white, 0 -1px white;">' +
                '(' + coords.x.toString() + ', ' +
                (isTms ? (this.flipY(coords.y, coords.z)) : (coords.y)).toString() + ', ' +
                coords.z.toString() + ')' +
                '</span>';
            tile.style.outline = '1px solid magenta';
            tile.style.textAlign = 'center';

            return tile;
        },
        getCurrentBaseMap: function () {
            var result;
            this._map.eachLayer(function (l) {
                if ((l instanceof L.TileLayer) && (l.options.pane === 'tilePane')) {
                    result = l;
                }
            }, this);

            return result;
        },
        flipY: function (y, z) {
            // Converting TMS tile coordinates to Google/Bing/OSM tile coordinates
            return (1 << z) - y - 1;
        }
    });

    L.gridLayer.tileGrid = function (options) {
        return new L.GridLayer.TileGrid(options);
    };

})();
