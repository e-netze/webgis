L.PointSymbol = L.Layer.extend({
    options: {
        color: '#ff0000',
        size: 10
    },

    initialize: function (latlng, options) {
        L.setOptions(this, options);
        this.setLatLng(latlng);
    },

    beforeAdd: function (map) {
        // Renderer is set here because we need to call renderer.getEvents
        // before this.getEvents.
        // this._renderer = map.getRenderer(this);
    },

    onAdd: function () {
        this.redraw();
    },

    onRemove: function () {
        if (this._circle) {
            this._map.removeLayer(this._circle);
            this._circle = null;
        }
    },

    _pos: null,
    setLatLng: function (latlng) {
        this._pos = latlng;
        this.redraw();
    },

    setStyle: function (style) {
        L.setOptions(this, style);

        if (this._circle) {
            this._circle.setStyle(style);
        }

        for (var name in style) {
            if (name === "size") {
                this.setSize(style[name]);
            }
        }

        return this;
    },

    setSize: function (size) {
        this.options.size = size;
        if (this._circle) {
            this._circle.setRadius(size);
        }
    },

    redraw: function () {
        if (!this._circle) {
            this.rebuild();
        } else {
            this._circle.setLatLng(this._pos);
        }

        return this;
    },

    _circle: null,

    rebuild: function () {
        if (!this._circle && this._pos && this._map) {
            this._circle = L.circleMarker(this._pos, { radius: this.options.size, color: this.options.color, fillColor: this.options.color, opacity: 1.0, fillOpacity: 1.0, weight: 1 });
            this._circle.addTo(this._map);
            this._circle.on('click', function (e) {
                console.log('event', e);
                //e.originalEvent.preventDefault();
                L.DomEvent.preventDefault(e);
                this.fireEvent('click', e);
            }, this);
        }
    },

    toGeoJSON: function () {
        var geoJson = {
            geometry: {
                type: 'Point',
                coordinates: []
            }
        };

        if (this._pos) {
            geoJson.geometry.coordinates = [this._pos.lng, this._pos.lat];
        }

        return geoJson;
    }
});

L.pointSymbol = function(latLng, options){
    return new L.PointSymbol(latLng, options);
};
