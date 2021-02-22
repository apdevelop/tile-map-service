(function () {

    // TODO: get layers list from TMS capabilities response
    // TODO: update Tile Grid overlay after base map change

    var baseMaps = {
        'World Countries': L.tileLayer('/tms/1.0.0/world-countries/{z}/{x}/{y}.png', {
            attribution: 'Esri',
            maxZoom: 5,
            tms: true
        }),
        'Satellite Lowres': L.tileLayer('/tms/1.0.0/satellite-lowres/{z}/{x}/{y}.jpg', {
            attribution: 'MapTiler AG',
            maxZoom: 5,
            tms: true
        })
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
        layers: [baseMaps['World Countries']]
    }).setView([0, 0], 0);


    L.control.layers(baseMaps, overlayMaps, {
        collapsed: false
    }).addTo(map);

})();
