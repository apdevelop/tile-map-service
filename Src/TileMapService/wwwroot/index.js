(function () {
    // using pure XMLHttpRequest (instead of axios and so on) for simplicity
    var xhr = new XMLHttpRequest();
    xhr.responseType = 'json';
    xhr.onload = function () {
        if (xhr.status === 200) {
            var sources = xhr.response;
            configureWebMercatorMap(createWebMercatorSources(sources));
            configureGeodeticMap(createGeodeticSources(sources));
        }
    }

    xhr.open('GET', '/api/sources', true);
    xhr.send();

    function createWebMercatorSources(list) {
        // TODO: xyzUrlTemplate, tmsUrlTemplate from server
        // TODO: passing format, srs parameters to server for filtering
        // TODO: choosing xyz/tms
        var baseMaps = {};
        list.filter(function (s) {
            return s.format !== 'mvt' && // TODO: isRaster
                s.format !== 'pbf' &&
                s.srs === 'EPSG:3857';
        })
            .forEach(function (s) { // convert array items to object
                baseMaps[s.title] = L.tileLayer('/xyz/' + s.id + '/?x={x}&y={y}&z={z}', {
                    attribution: s.attribution,
                    maxZoom: s.maxZoom
                });
            });

        return baseMaps;
    }

    function createGeodeticSources(list) {
        // TODO: xyzUrlTemplate, tmsUrlTemplate from server
        // TODO: ? choosing xyz/tms
        var baseMaps = {};
        list.filter(function (s) {
            return s.format !== 'mvt' && // TODO: isRaster
                s.format !== 'pbf' &&
                s.srs === 'EPSG:4326';
        })
            .forEach(function (s) {
                baseMaps[s.title + '-xyz'] = L.tileLayer('/xyz/' + s.id + '/?x={x}&y={y}&z={z}', {
                    attribution: s.attribution,
                    maxZoom: s.maxZoom,
                    tms: false
                });
                baseMaps[s.title + '-tms'] = L.tileLayer('/tms/1.0.0/' + s.id + '/{z}/{x}/{y}.png', {
                    attribution: s.attribution,
                    maxZoom: s.maxZoom,
                    tms: true
                });
            });

        return baseMaps;
    }

    function configureWebMercatorMap(baseMaps) {

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
    }

    function configureGeodeticMap(baseMaps) {
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
    }

    var baseMapsWebMercator = {
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
        'Caspian Sea (XYZ)': L.tileLayer('/xyz/caspiansea/?x={x}&y={y}&z={z}', {
            attribution: '',
            maxZoom: 10
        }),
        'World Countries (XYZ FS)': L.tileLayer('/xyz/world-countries-fs/{z}/{x}/{y}.png', {
            attribution: 'Esri',
            maxZoom: 5
        }),
        'ArcGis Online MapServer (WMTS)': L.tileLayer('/xyz/arcgisonline-wmts-demo/{z}/{x}/{y}.png', {
            attribution: 'Esri',
            maxZoom: 12
        }),
        'marine-vts.site Temperature (WMS)': L.tileLayer('/xyz/wms-demo-geoserver-temperature/{z}/{x}/{y}.png', {
            attribution: 'gis.marine-vts.site',
            maxZoom: 10
        }),
        'marine-vts.site Pressure (WMS)': L.tileLayer('/xyz/wms-demo-geoserver-pressure/{z}/{x}/{y}.png', {
            attribution: 'gis.marine-vts.site',
            maxZoom: 10
        }),
        'marine-vts.site SAR GRID (WMS)': L.tileLayer('/xyz/wms-demo-geoserver-sar-grid/{z}/{x}/{y}.png', {
            attribution: 'gis.marine-vts.site',
            maxZoom: 12
        }),
        'GeoTIFF': L.tileLayer('/xyz/geotiff/?x={x}&y={y}&z={z}', {
            maxZoom: 24
        }),
        'WMS with Caching': L.tileLayer('/xyz/wms-with-caching/{z}/{x}/{y}.png', {
            attribution: '',
            maxZoom: 10
        }),
    };

    var baseMapsGeodetic = {
        'MapCache Geodetic (server)': L.tileLayer('http://localhost:8088/mapcache/tms/1.0.0/test@WGS84/{z}/{x}/{y}.png', {
            maxZoom: 5,
            tms: true
        }),
        'MapCache Geodetic (local proxy / TMS)': L.tileLayer('/tms/1.0.0/mapcache-geodetic/{z}/{x}/{y}.png', {
            maxZoom: 5,
            tms: true
        }),
        'MapCache Geodetic (local proxy / XYZ)': L.tileLayer('/xyz/mapcache-geodetic/?z={z}&x={x}&y={y}', {
            maxZoom: 5,
            tms: false
        })
    };

})();
