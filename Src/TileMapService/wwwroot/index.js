(function () {
    // TODO: get layers list from service (from WMTS capabilities response)
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
        'World Countries (XYZ)': L.tileLayer('/xyz/world-countries/?x={x}&y={y}&z={z}', {
            attribution: 'Esri',
            maxZoom: 5
        }),
        'Satellite Lowres (XYZ)': L.tileLayer('/xyz/satellite-lowres/?x={x}&y={y}&z={z}', {
            attribution: 'MapTiler AG',
            maxZoom: 5
        }),
        'World Countries (XYZ FS)': L.tileLayer('/xyz/world-countries-fs/{z}/{x}/{y}.png', {
            attribution: 'Esri',
            maxZoom: 5
        }),
        'Demographics (WMTS)': L.tileLayer('/xyz/arcgisonline-wmts-demo/{z}/{x}/{y}.png', {
            attribution: 'Esri',
            maxZoom: 5
        }),
        'MapServer Demo (WMS)': L.tileLayer('/xyz/wms-demo-mapserver/{z}/{x}/{y}.png', {
            attribution: 'MapServer',
            maxZoom: 10
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

    var map = L.map('mapMercator', {
        inertia: false,
        doubleClickZoom: false,
        crs: L.CRS.EPSG3857, // Default
        layers: [baseMaps[Object.keys(baseMaps)[0]]]
    }).setView([0, 0], 0);

    L.control.layers(baseMaps, overlayMaps, {
        collapsed: false
    }).addTo(map);

    map.on('baselayerchange', function () {
        tileGrid.redraw(); // updates Tile Grid overlay after base map change
    });

})();

(function () {
    // TODO: get layers list from service (from WMTS capabilities response)
    var baseMaps = {
        'MapCache Geodetic (server)': L.tileLayer('http://localhost:8088/mapcache/tms/1.0.0/test@WGS84/{z}/{x}/{y}.png', {
            maxZoom: 5,
            tms: true
        }),
        'MapCache Geodetic (local proxy)': L.tileLayer('/tms/1.0.0/mapcache-geodetic/{z}/{x}/{y}.png', {
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

    var map = L.map('mapGeodetic', {
        inertia: false,
        doubleClickZoom: false,
        crs: L.CRS.EPSG4326,
        layers: [baseMaps[Object.keys(baseMaps)[0]]]
    }).setView([0, 0], 0);

    L.control.layers(baseMaps, overlayMaps, {
        collapsed: false
    }).addTo(map);

})();
