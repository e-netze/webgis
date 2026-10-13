

L.LayerCollection = L.Layer.extend({

    options: {},
    _childLayers: [], _setStyle:null,
    initialize: function (latlngs, options) {
        this._childLayers = [];
        L.setOptions(this, options);
        this._setLatLngs(latlngs);
    },

    beforeAdd: function (map) {
        // Renderer is set here because we need to call renderer.getEvents
        // before this.getEvents.
        //this._renderer = map.getRenderer(this);
    },

    onAdd: function () {
        
    },

    onRemove: function () {
        for (var l in this._childLayers) {
            this._map.removeLayer(this._childLayers[l]);
        }
        this._childLayers = [];
    },

    redraw: function () {
        for (var l in this._childLayers) {
            this._childLayers[l].redraw();
        }
        return this;
    },

    setStyle: function (style) {
        L.setOptions(this, style);

        if (this._setStyle) this._setStyle(style);
        for (var l in this._childLayers) {
            this._childLayers[l].setStyle(style);
        }
        return this;
    },

    bringToFront: function () {
        for (var l in this._childLayers) {
            this._childLayers[l].bringToFront();
        }
        
        return this;
    },

    // @method bringToBack(): this
    // Brings the layer to the bottom of all path layers.
    bringToBack: function () {
        for (var l in this.layers) {
            this.layers[l].bringToBack();
        }
        
        return this;
    },

    _reset: function () {
        for (var l in this._childLayers) {
            this._childLayers[l]._reset();
        }
    },

    _clickTolerance: function () {
        return this._renderer.options.tolerance;
    },

    addChildLayer: function (layer) {
        if (layer && this._map) {
            this._childLayers.push(layer);
            layer.addTo(this._map);
        }
    },

    getChildLayer: function (index) {
        if (index >= 0 && index < this._childLayers.length) {
            return this._childLayers[index];
        }

        return null;
    },

    getChildLayerCount: function () {
        return this._childLayers.length;
    },

    removeAllChildLayers: function () {
        if (this._childLayers.length > 0) {
            this.onRemove();
        }
    },

    trimChildren: function (from) {
        if (from < this._childLayers.length) {
            var newChildlayers = [];
            for (var i = 0; i < from; i++) {
                newChildlayers.push(this._childLayers[i]);
            }
            for (var i = from; i < this._childLayers.length; i++) {
                this._map.removeLayer(this._childLayers[i]);
            }
            this._childLayers = newChildlayers;
        }
    },

    toGeoJSON: function () {
        return {
            geometry: {
                coordinates: []
            }
        };
    }
});

//L.layerCollection = function (latLngs, layers) {
//    return new L.LayerCollection(latLngs, { layers: layers || [] });
//};
