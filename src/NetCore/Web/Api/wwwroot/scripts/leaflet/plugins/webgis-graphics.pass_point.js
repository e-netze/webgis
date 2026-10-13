L.PassPoint = L.LayerCollection.extend({
    options: {
        color: '#ff0000',
        weight: 2,
        marker1Options: {
            draggable: true,
            icon: L.icon({
                iconUrl: webgis.css.imgResource('marker-pin_32.png', 'markers'),
                iconSize: [32, 32],
                iconAnchor: [16, 32],
                popupAnchor: [0, -32]
            })},
        marker2Options: {
            draggable: true,
            icon: L.icon({
                iconUrl: webgis.css.imgResource('marker-draggable-outline-bottom_32.png', 'markers'),
                iconSize: [32, 32],
                iconAnchor: [16, 0],
                popupAnchor: [0, 2]
            })
        },
        marker2activeOptions: {
            draggable: true,
            icon: L.icon({
                iconUrl: webgis.css.imgResource('marker-draggable-bottom_32.png', 'markers'),
                iconSize: [32, 32],
                iconAnchor: [16, 0],
                popupAnchor: [0, 2]
            })
        }
    },

    _latLngs: null,
    _setLatLngs: function (latLngs) {
        this.setLatLngs(latLngs);
    },

    _active: false,
    
    onAdd: function () {
        this.rebuild();
    },

    setLatLngs: function (latLngs) {
        this._latLngs = latLngs;
        this.rebuild();
    },
    setLatLng: function (index, latLng) {
        if (this._latLngs && this._latLngs.length > index) {
            this._latLngs[index] = latLng;
            this.rebuild();
        }
    },
    getLatLngs: function () { return this._latLngs; },

    setActive: function (active) {
        this._active = active ? true : false;
        if (this.getChildLayerCount() >= 3) {
            var marker = this.getChildLayer(2);
            if (this._active === true) {
                marker.setIcon(this.options.marker2activeOptions.icon);
            } else {
                marker.setIcon(this.options.marker2Options.icon);
            }
        }
    },
    isActive: function () { return this._active === true },

    rebuild: function () {
        if (this._latLngs && this._latLngs.length === 2) {
            if (this.getChildLayerCount() === 0) {
                var me = this;

                var line = L.polyline(this._latLngs, this.options);
                this.addChildLayer(line);

                var marker1 = L.marker(this._latLngs[0], this.options.marker1Options);
                marker1._parent = this;
                marker1.on('drag', function () {
                    var latlng = this.getLatLng();
                    me.setLatLng(0, { lng: latlng.lng, lat: latlng.lat });
                }, marker1);
                marker1.on('dragend', function (e) {
                    me.fireEvent('marker1-changed', e)
                }, marker1);
                this.addChildLayer(marker1);

                var marker2 = L.marker(this._latLngs[1],
                    this._active === true ? this.options.marker2activeOptions :
                                            this.options.marker2Options);
                marker2.on('drag', function () {
                    var latlng = this.getLatLng();
                    me.setLatLng(1, { lng: latlng.lng, lat: latlng.lat });
                }, marker2);
                marker2.on('dragend', function (e) {
                    me.fireEvent('marker2-changed', e)
                }, marker2);

                marker1.bindPopup("", {
                    maxWidth: "100%"
                });
                marker1.on('popupopen', function (e) {
                    var me = this;
                    var $element = webgis.$(e.popup.getElement()).find('.leaflet-popup-content');
                    if ($element.find('.webgis-button').length === 0) {
                        webgis.$("<br>").appendTo($element);
                        webgis.$("<button>")
                            .text("Passpunkt entfernen")
                            .addClass('webgis-button')
                            .data('layer', this)
                            .click(function () {
                                var layer = $(this).data('layer');
                                layer._map.removeLayer(layer);
                                layer.fireEvent('passpoint-removed')
                            })
                            .appendTo($element);
                    }
                    e.popup.update();
                }, this);

                this.addChildLayer(marker2);
            }

            var line = this.getChildLayer(0);
            if (line) {
                line.setLatLngs(this._latLngs);
            }
            var marker1 = this.getChildLayer(1);
            if (marker1) {
                marker1.setLatLng(this._latLngs[0]);
            }
            var marker2 = this.getChildLayer(2);
            if (marker2) {
                marker2.setLatLng(this._latLngs[1]);
            }
        }
    }
});

L.passPoint = function (latLngs, options) {
    return new L.PassPoint(latLngs, options);
};
