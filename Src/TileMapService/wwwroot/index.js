(function () {

    fetch('/api/sources')
        .then(function (response) {
            return response.json();
        })
        .then((sources) => {
            createOlMap3857(sources);
            createOlMap4326(sources);
        });

    // TODO: WMS layers

    function createOlMap3857(sources) {

        var tileLayers = filterRasterSources(sources, 'EPSG:3857')
            .map(function (s) {
                return new ol.layer.Tile({
                    title: s.title,
                    visible: false,
                    type: 'base',
                    source: new ol.source.XYZ({
                        url: '/xyz/' + s.id + '/?x={x}&y={y}&z={z}',
                        attributions: s.attribution
                    })
                })
            });

        // https://openlayers.org/workshop/en/vectortile/map.html
        var vectorTileLayers = filterVectorSources(sources, 'EPSG:3857')
            .map(function (s) {
                return new ol.layer.VectorTile({
                    title: s.title,
                    visible: false,
                    type: 'overlay',
                    source: new ol.source.VectorTile({
                        attributions: s.attribution,
                        format: new ol.format.MVT(),
                        url: '/xyz/' + s.id + '/?x={x}&y={y}&z={z}'
                    }),
                    style:
                        new ol.style.Style({
                            fill: new ol.style.Fill({
                                color: 'rgba(255, 255, 255, 0.1)'
                            }),
                            stroke: new ol.style.Stroke({
                                color: '#319FD3',
                                width: 2
                            }),
                            image: new ol.style.Circle({
                                radius: 6,
                                stroke: new ol.style.Stroke({
                                    color: 'black',
                                    width: 1
                                }),
                                fill: new ol.style.Fill({ color: 'rgba(255, 0, 0, 0.5)' })
                            })
                        })
                })
            });

        var debugLayer = new ol.layer.Tile({
            source: new ol.source.TileDebug({
                template: 'z:{z} x:{x} y:{y}',
                zDirection: 1
            })
        });

        tileLayers = tileLayers.concat(vectorTileLayers);
        tileLayers.push(debugLayer);

        createOlMap('mapOL3857', 'EPSG:3857', tileLayers);
    }

    function createOlMap4326(sources) {
        fetch('/wmts/?SERVICE=WMTS&REQUEST=GetCapabilities')
            .then(function (response) {
                return response.text();
            })
            .then(function (text) {
                var parser = new ol.format.WMTSCapabilities();
                var result = parser.read(text);

                var tileLayers = filterRasterSources(sources, 'EPSG:4326')
                    .map(function (s) {
                        var options = ol.source.WMTS.optionsFromCapabilities(result, {
                            layer: s.id,
                            matrixSet: 'EPSG:4326',
                            crossOrigin: true
                        });

                        return new ol.layer.Tile({
                            title: s.title,
                            visible: false,
                            type: 'base',
                            source: new ol.source.WMTS(options),
                            opacity: 0.5
                        });
                        // TODO: WMS with EPSG:4326
                        ////return new ol.layer.Image({
                        ////    title: s.title,
                        ////    visible: false,
                        ////    type: 'base',
                        ////    source: new ol.source.ImageWMS({
                        ////        url: '/wms',
                        ////        params: { 'LAYERS': s.id },
                        ////        ratio: 1
                        ////    })
                        ////})
                    });

                createOlMap('mapOL4326', 'EPSG:4326', tileLayers);
            });
    }

    function createOlMap(target, projection, tileLayers) {
        var map = new ol.Map({
            target: target,
            controls: ol.control.defaults.defaults({
                zoom: true,
                attribution: true,
                rotate: false
            }),
            layers: tileLayers,
            interactions: ol.interaction.defaults.defaults({
                zoomDelta: 1,
                zoomDuration: 0
            }),
            view: new ol.View({
                projection: projection,
                center: projection === 'EPSG:3857' ? ol.proj.fromLonLat([0, 30]) : [0, 30],
                zoom: 0,
            })
        });

        tileLayers[0].setVisible(true);

        // https://github.com/walkermatt/ol-layerswitcher
        map.addControl(new ol.control.LayerSwitcher({
            startActive: true,
            activationMode: 'click',
            reverse: false,
            tipLabel: 'Layers',
            collapseLabel: ''
        }));
    }

    function filterRasterSources(list, srs) {
        return list.filter(function (s) {
            return s.format !== 'mvt' &&
                s.format !== 'pbf' &&
                s.srs === srs;
        });
    }

    function filterVectorSources(list, srs) {
        return list.filter(function (s) {
            return (s.format === 'mvt' ||
                s.format === 'pbf') &&
                s.srs === srs;
        });
    }

})();
