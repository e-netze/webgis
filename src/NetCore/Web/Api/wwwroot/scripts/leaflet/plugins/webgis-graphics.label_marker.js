L.LabelMarker = L.LayerCollection.extend({
    options: {
        icon: null,
        fontColor: 'black',
        fontSize: 14,
        fontSytle: '',
        label: 'Marker Label',
        labelPadding: 4,
        showLabel: true
    },

    _pos: null,

    _setLatLngs: function (latLngs) {
        if (latLngs.length === 1) {
            this.setLatLng(latLngs[0]);
        } else if (latLngs && latLngs.lat && latLngs.lng) {
            this.setLatLng(latLngs);
        }
    },

    onAdd: function () {
        this.rebuild();
    },

    setLatLng: function (latlng) {
        this._pos = latlng;
        this.redraw();
    },

    showLabel: function (show) {

        if (this.options.showLabel != show) {
            this.options.showLabel = show;
            this.removeAllChildLayers();
            this.rebuild();
        }
    },

    rebuild: function () {
        var label = this.options.label;
        if (this.options.labelPadding) {
            let padding = ''.padStart(this.options.labelPadding);
            label = padding + label.replaceAll('\n', '\n' + padding);
        }

        if (this.options.showLabel) {
            let text = L.svgText(this._pos, { text: label });
            this.addChildLayer(text);
        }
        var marker = L.marker(this._pos, { icon: this.options.icon });
        this.addChildLayer(marker);
    }
});

L.labelMarker = function (latLng, options) {
    return new L.LabelMarker(latLng, options);
};

//L.XPoint = L.LayerCollection.extend({
//    options: {
//        color: 'green',
//        weight: 2
//    },

//    initialize: function (latlng, options) {
//        L.setOptions(this, options);
//        this.setLatLng(latlng);
//    },
//    _pos: null,
//    setLatLng: function (latlng) {
//        this._pos = latlng;
//        this.redraw();
//    },

//    beforeAdd: function (map) {
//        // Renderer is set here because we need to call renderer.getEvents
//        // before this.getEvents.
//        this._renderer = map.getRenderer(this);

//        map.on('moveend', this.redraw, this);
//        map.on('zoomend', this.redraw, this);
//    },
//    onRemove: function () {
//        this._map.off('moveend', this.redraw, this);
//        this._map.off('zoomend', this.redraw, this);
//    },

//    redraw: function () {
//        this.removeAllChildLayers();

//        var len = 0.5;
//        var line1 = L.polyline([{ lng: this._pos.lng - len, lat: this._pos.lat - len }, { lng: this._pos.lng + len, lat: this._pos.lat + len }], this.options);
//        var line2 = L.polyline([{ lng: this._pos.lng + len, lat: this._pos.lat - len }, { lng: this._pos.lng + len, lat: this._pos.lat - len }], this.options);

//        console.log(line1.getLatLngs(), this.options);

//        this.addChildLayer(line1);
//        this.addChildLayer(line2);
//    }
//});

//L.xPoint = function (latLng, options) {
//    return new this.XPoint(latLng, options);
//};
