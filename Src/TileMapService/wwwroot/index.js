(() => {

    fetch('/api/sources')
        .then((response) => response.json())
        .then((sources) => {
            createOlMap3857('mapOL3857', sources);
            createOlMap4326('mapOL4326', sources);
        });

    // TODO: WMS layers

    const createOlMap3857 = (target, sources) => {
        const rasterTileLayers = filterRasterSources(sources, 'EPSG:3857')
            .map((s) => new ol.layer.Tile({
                title: s.title,
                visible: false,
                type: 'base',
                source: new ol.source.XYZ({
                    url: '/xyz/' + s.id + '/?x={x}&y={y}&z={z}',
                    attributions: s.attribution
                }),
            }));

        // https://openlayers.org/workshop/en/vectortile/map.html
        const vectorTileLayers = filterVectorSources(sources, 'EPSG:3857')
            .map((s) => new ol.layer.VectorTile({
                title: s.title,
                visible: false,
                type: 'overlay',
                source: new ol.source.VectorTile({
                    attributions: s.attribution,
                    format: new ol.format.MVT(),
                    url: '/xyz/' + s.id + '/?x={x}&y={y}&z={z}',
                }),
                style: new ol.style.Style({
                    fill: new ol.style.Fill({
                        color: 'rgba(255, 255, 255, 0.1)',
                    }),
                    stroke: new ol.style.Stroke({
                        color: '#319FD3',
                        width: 2,
                    }),
                    image: new ol.style.Circle({
                        radius: 6,
                        stroke: new ol.style.Stroke({
                            color: 'black',
                            width: 1
                        }),
                        fill: new ol.style.Fill({ color: 'rgba(255, 0, 0, 0.5)' }),
                    }),
                }),
            }));

        const debugLayer = new ol.layer.Tile({
            source: new ol.source.TileDebug({
                template: 'z:{z} x:{x} y:{y}',
                zDirection: 1,
            }),
        });

        const allLayers = [].concat(rasterTileLayers, vectorTileLayers, [debugLayer]);

        createOlMap(target, 'EPSG:3857', allLayers);
    }

    const createOlMap4326 = (target, sources) => {
        fetch('/wmts/?SERVICE=WMTS&REQUEST=GetCapabilities')
            .then((response) => response.text())
            .then((text) => {
                const parser = new ol.format.WMTSCapabilities();
                const result = parser.read(text);

                const tileLayers = filterRasterSources(sources, 'EPSG:4326')
                    .map((s) => {
                        const options = ol.source.WMTS.optionsFromCapabilities(result, {
                            layer: s.id,
                            matrixSet: 'EPSG:4326',
                            crossOrigin: true,
                        });

                        return new ol.layer.Tile({
                            title: s.title,
                            visible: false,
                            type: 'base',
                            source: new ol.source.WMTS(options),
                            opacity: 0.5,
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
                        ////    }),
                        ////});
                    });

                createOlMap(target, 'EPSG:4326', tileLayers);
            });
    }

    const createOlMap = (target, projection, layers) => {
        const map = new ol.Map({
            target: target,
            controls: ol.control.defaults.defaults({
                zoom: true,
                attribution: true,
                rotate: false,
            }),
            layers: layers,
            interactions: ol.interaction.defaults.defaults({
                zoomDelta: 1,
                zoomDuration: 0,
            }),
            view: new ol.View({
                projection: projection,
                center: projection === 'EPSG:3857'
                    ? ol.proj.fromLonLat([0, 30])
                    : [0, 30],
                zoom: 0,
            }),
        });

        layers[0].setVisible(true);

        // https://github.com/walkermatt/ol-layerswitcher
        map.addControl(new ol.control.LayerSwitcher({
            startActive: true,
            activationMode: 'click',
            reverse: false,
            tipLabel: 'Layers',
            collapseLabel: '',
        }));
    }

    const filterRasterSources = (list, srs) =>
        list.filter((s) => s.format !== 'mvt' && s.format !== 'pbf' && s.srs === srs);

    const filterVectorSources = (list, srs) =>
        list.filter((s) => (s.format === 'mvt' || s.format === 'pbf') && s.srs === srs);

})();
