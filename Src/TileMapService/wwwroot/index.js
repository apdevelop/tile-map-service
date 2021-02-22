(function () {
    // TODO: get layers list from service (TMS capabilities response ?)
    var baseMaps = {
        'World Countries (TMS)': L.tileLayer('/tms/1.0.0/world-countries/{z}/{x}/{y}.png', {
            attribution: 'Esri',
            maxZoom: 5,
            tms: true
        }),
        'Satellite Lowres (TMS)': L.tileLayer('/tms/1.0.0/satellite-lowres/{z}/{x}/{y}.jpg', {
            attribution: 'MapTiler AG',
            maxZoom: 5,
            tms: true
        }),
        'World Countries (XYZ)': L.tileLayer('/tiles/world-countries/?x={x}&y={y}&z={z}', {
            attribution: 'Esri',
            maxZoom: 5
        }),
        'Satellite Lowres (XYZ)': L.tileLayer('/tiles/satellite-lowres/?x={x}&y={y}&z={z}', {
            attribution: 'MapTiler AG',
            maxZoom: 5
        }),
    };

    var tileGrid = L.gridLayer.tileGrid({
        opacity: 1.0,
        zIndex: 2,
        pane: 'overlayPane'
    });

    var overlayMaps = {
        'Tile Grid': tileGrid
    };

    var map = L.map('map', {
        inertia: false,
        doubleClickZoom: false,
        layers: [baseMaps[Object.keys(baseMaps)[0]]]
    }).setView([0, 0], 0);

    L.control.layers(baseMaps, overlayMaps, {
        collapsed: false
    }).addTo(map);

    map.on('baselayerchange', function () {
        tileGrid.redraw(); // updates Tile Grid overlay after base map change
    });

})();
