L.GraphicsElementSeries = L.LayerCollection.extend({
    options: {
        type: 'rectangle',
        rotatable: true,
        rectangleOptions: {
            color: '#ffff00',
            weight: 1,
            fill: '#ffff00'
        },
        width: 500,
        height: 300
    },
    _latLngs: null,
    _setLatLngs: function (latLngs) {
        this.setLatLngs(latLngs);
    },
    setLatLngs: function (latLngs) {
        this._latLngs = latLngs;
        this.rebuild();
    },
    setLatLngAt: function (index, latLng) {
        if (this._latLngs && this._latLngs.length > index) {
            const latLngRotation = this._latLngs[index]._rotation || 0;

            this._latLngs[index] = latLng;
            this._latLngs[index]._rotation = latLngRotation;
            this.rebuildAt(index);
        }
    },
    addLatLng: function (latLng) {
        this._latLngs = this._latLngs || [];
        this._latLngs.push(latLng);
        this.rebuild();
    },
    getLatLngs: function () { return this._latLngs; },
    getBounds: function () {
        if (!this._latLngs || this._latLngs.length === 0) return null;

        let bounds = null;
        for (let latLng of this._latLngs) {
            let elementBounds = L.rotatableRectangle([latLng], {
                width: this.options.width,
                height: this.options.height,
                rotation: latLng._rotation || 0
            }).getBounds();
            if (bounds) {
                bounds.extend(elementBounds);
            } else {
                bounds = elementBounds;
            }
        }
        return bounds;
    },
    vertexToLatLng: function (vertex) {
        let latLng = L.latLng(vertex.y, vertex.x);
        if (!vertex.m) {
            vertex.m = (this._latLngs && this._latLngs.length > 0)
                ? this._latLngs[this._latLngs.length - 1]._rotation || 0
                : 0;
        }

        latLng._rotation = vertex.m;
        //console.log('vertexToLatLng', latLng);
        return latLng;
    },
    setVertexRotation: function (vertex, rotation) {
        vertex.m = rotation || 0;
    },
    setElementSize: function (width, height) {
        this.options.width = width;
        this.options.height = height;
        this.rebuild();
    },
    redraw: function () {
        this.rebuild();
    },
    rebuild: function () {
        //console.log('rebuild', this);
        if (!this._latLngs || this._latLngs.length === 0) return;

        this.removeAllChildLayers();
        let index = 0;
        for (let latLng of this._latLngs) {
            if (this.options.type === "rectangle") { 
                //let elementLatLngs = this._calcElementLatLngs(latLng);
                //polyline = L.polyline(elementLatLngs.latLngs, this.options.rectangleOptions);
                //this.addChildLayer(polyline);
                let rectangle = L.rotatableRectangle([latLng], {
                    color: this.options.rectangleOptions.color,
                    weight: this.options.rectangleOptions.weight,
                    fillColor: this.options.rectangleOptions.fill,
                    fillOpacity: this.options.rectangleOptions.fillOpacity || 0.2,
                    width: this.options.width,
                    height: this.options.height,
                    rotation: latLng._rotation || 0,
                    label: String(index + 1).padStart(3, '0'),
                    markerOptions: this.options.rotatable ? {
                        draggable: true,
                        icon: L.icon({
                            iconUrl: webgis.css.imgResource('rotate_26.png', 'markers'),
                            iconSize: [26, 26],
                            iconAnchor: [13, 13],
                            popupAnchor: [-13, -13]
                        })
                    } : null
                });
                rectangle.setRotation(latLng._rotation);
                rectangle._index = index++;
                rectangle.on("rotated", function (ev) {
                    this._latLngs[ev.sender._index]._rotation = ev.rotation;
                    this.fireEvent("element-rotated", { sender: this, elementIndex: ev.sender._index, rotation: ev.rotation });
                }, this);
                this.addChildLayer(rectangle); 
            }
        }
    },
    rebuildAt: function (index) {
        //console.log('rebuildat', index);
        if (!this._latLngs || this._latLngs.length <= index) return;

        var element = this.getChildLayer(index);
        element.setLatLng(this._latLngs[index]);
    },
});

L.graphicsElementSeries = function (latLngs, options) {
    //console.log('create L.GraphicsElementSeries', latLngs, options);
    return new L.GraphicsElementSeries(latLngs, options);
}
