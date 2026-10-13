L.SketchConstructionResults = L.LayerCollection.extend({
    options: {
        markerOptions: {
            icon: L.icon({
                iconUrl: webgis.css.imgResource('sketch_marker_construction_point.png', 'markers'),
                iconSize: [13, 13],
                iconAnchor: [7, 7],
                popupAnchor: [0, -7]
            })
        },
        markerOptionsTemp: {
            icon: L.icon({
                iconUrl: webgis.css.imgResource('sketch_marker_construction_point_temp.png', 'markers'),
                iconSize: [13, 13],
                iconAnchor: [7, 7],
                popupAnchor: [0, -7]
            })
        },
        projEvent: 'constructed',
        display: 'markers'
    },

    _setLatLngs: function (latLngs) {
        
    },

    setVertices: function (vertices) {
        this.options.vertices = vertices;
        this.rebuild();
    },

    onAdd: function () {
        this.setVertices(this.options.vertices);
    },

    rebuild: function () {
        if (this.options.vertices && this.options.vertices.length > 0) {
            var count = this.getChildLayerCount();

            if (this.options.display === 'polyline') {
                var latLngs = [];

                for (var v in this.options.vertices) {
                    if (this.options.projEvent) {
                        this.options.vertices[v].projEvent = this.options.vertices[v].projEvent || this.options.projEvent;
                    }

                    latLngs.push({ lng: this.options.vertices[v].x, lat: this.options.vertices[v].y });
                }

                var polyline = L.polyline(latLngs, { color: this.options.color || 'green', weight: this.options.addClick ? 5 : 1, className: 'webgis-leaflet-interactive' });
                this.addChildLayer(polyline);

                if (this.options.addClick) {
                    polyline.on('click', function (e) {
                        //e.originalEvent.preventDefault();
                        L.DomEvent.stopPropagation(e);

                        if (this.undoText) {
                            this.sketch.addUndo(this.undoText);
                        }

                        this.sketch.addVertices(this.vertices, true);

                        webgis.sketch.construct.cancel(this.sketch);

                        return true;
                    }, {
                        sketch: this.options.sketch,
                        vertices: this.options.vertices,
                        undoText: this.options.undoText
                    })
                }
            }
            else {
                for (var v in this.options.vertices) {
                    if (this.options.projEvent) {
                        this.options.vertices[v].projEvent = this.options.vertices[v].projEvent || this.options.projEvent;
                    }

                    var latLng = { lng: this.options.vertices[v].x, lat: this.options.vertices[v].y };

                    var marker = null;
                    if (v >= count) {  // create
                        marker = L.marker(latLng, this.options.addClick ? this.options.markerOptions : this.options.markerOptionsTemp);
                        this.addChildLayer(marker);

                        if (this.options.addClick) {
                            marker.on('click', function (e) {
                                this.sketch.addOrSetCurrentContextVertex(this.vertex, true);
                            }, {
                                sketch: this.options.sketch,
                                vertex: this.options.vertices[v]
                            });
                        }
                    } else { // set latlng
                        var marker = this.getChildLayer(v);
                        marker.setLatLng(latLng);
                    }
                }
            }
        }
    }
});

L.sketchConstructionResults = function (options) {
    return new L.SketchConstructionResults(null, options);
};
