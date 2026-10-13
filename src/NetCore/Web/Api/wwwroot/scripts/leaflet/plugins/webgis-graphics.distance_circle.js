L.DistanceCircle = L.LayerCollection.extend({

    options: {
        color: '#ff0000',
        fillColor: '#eeffff',
        fillOpacity: 0.8,
        weight: 2,
        steps: 3,
        radius: 5000
    },

    _center: null,

    _setLatLngs: function (latLngs) {
        if (latLngs.length > 0) {
            this.setLatLng(latLngs[0]);
        }
    },

    onAdd: function () {
        this.rebuild();
    },

    //addLatLng: function (latLng) {
    //    this.setLatLng(latLng);
    //},
    setLatLng: function (latLng) {
        this._center = latLng;
        this.rebuild();
    },
    setRadius: function (radius) {
        this.options.radius = radius;
        this.rebuild();
    },
    getRadius: function () {
        return this.options.radius;
    },
    setSteps: function (steps) {
        this.options.steps = steps;
        this.removeAllChildLayers();
        this.rebuild();
    },
    getSteps: function () {
        return this.options.steps;
    },
    rebuild: function () {
        if (this._center && this._map) {
            var radius = this.options.radius;
            var steps = Math.max(this.options.steps || 3, 1);

            var outerCircle = null;
            var count = this.getChildLayerCount();
            if (count === 0) {
                for (var i = steps; i >= 0; i--) {
                    var c = L.circle(this._center, {
                        radius: (radius / steps) * i,
                        color: this.options.color,
                        fillColor: this.options.fillColor,
                        weight: this.options.weight
                    });
                    c._distcircle = { type: 'circle', step: i };
                    this.addChildLayer(c);

                    if (i === steps) {
                        this._addLineH(c);
                        this._addLineV(c);
                        this._addLineD1(c);
                        this._addLineD2(c);
                    }

                    c.on('click', function (e) {
                        this.fireEvent('click', e);
                    }, this);

                    this._addText(c);
                }
            } else {
                for (var i = 0; i < count; i++) {
                    var c = this.getChildLayer(i);
                    switch (c._distcircle.type) {
                        case 'circle':
                            outerCircle = c;
                            c.setLatLng(this._center);
                            c.setRadius((radius / steps) * c._distcircle.step);
                            break;
                        case 'text':
                            this._addText(outerCircle, c);
                            break;
                        case 'lineH':
                            this._addLineH(outerCircle, c);
                            break;
                        case 'lineV':
                            this._addLineV(outerCircle, c);
                            break;
                        case 'lineD1':
                            this._addLineD1(outerCircle, c);
                            break;
                        case 'lineD2':
                            this._addLineD2(outerCircle, c);
                            break;
                    }
                }
            }
        }
    },

    _addLineH: function (outerCircle, line) {
        if (!outerCircle)
            return;

        var latLngs = [
            [
                outerCircle.getBounds().getCenter().lat,
                outerCircle.getBounds().getWest()
            ],
            [
                this._center.lat,
                this._center.lng
            ],
            [
                outerCircle.getBounds().getCenter().lat,
                outerCircle.getBounds().getEast()
            ]];

        if (!line) {
            line = L.polyline(latLngs, { color: '#aaa', weight: 1 });
            line._distcircle = { type: 'lineH' };
            this.addChildLayer(line);
        } else {
            line.setLatLngs(latLngs);
        }
    },

    _addLineV: function (outerCircle, line) {
        if (!outerCircle)
            return;

        var latLngs = [
            [
                outerCircle.getBounds().getSouth(),
                outerCircle.getBounds().getCenter().lng,
            ],
            [
                this._center.lat,
                this._center.lng
            ],
            [
                outerCircle.getBounds().getNorth(),
                outerCircle.getBounds().getCenter().lng
            ]];

        if (!line) {
            line = L.polyline(latLngs, { color: '#aaa', weight: 1 });
            line._distcircle = { type: 'lineV' };
            this.addChildLayer(line);
        } else {
            line.setLatLngs(latLngs);
        }
    },

    _addLineD1: function (outerCircle, line) {
        if (!outerCircle)
            return;

        var latLngs = [
            [
                this._center.lat + (this._center.lat - outerCircle.getBounds().getSouth()) * Math.sin(Math.PI / 4.0),
                this._center.lng + (this._center.lng - outerCircle.getBounds().getWest()) * Math.cos(Math.PI / 4.0)
            ],
            [
                this._center.lat,
                this._center.lng
            ],
            [
                this._center.lat + (this._center.lat - outerCircle.getBounds().getSouth()) * Math.sin(5.0 * Math.PI / 4.0),
                this._center.lng + (this._center.lng - outerCircle.getBounds().getWest()) * Math.cos(5.0 * Math.PI / 4.0)
            ]];

        if (!line) {
            line = L.polyline(latLngs, { color: '#aaa', weight: 1 });
            line._distcircle = { type: 'lineD1' };
            this.addChildLayer(line);
        } else {
            line.setLatLngs(latLngs);
        }
    },

    _addLineD2: function (outerCircle, line) {
        if (!outerCircle)
            return;

        var latLngs = [
            [
                this._center.lat + (this._center.lat - outerCircle.getBounds().getSouth()) * Math.sin(3.0 * Math.PI / 4.0),
                this._center.lng + (this._center.lng - outerCircle.getBounds().getWest()) * Math.cos(3.0 * Math.PI / 4.0)
            ],
            [
                this._center.lat,
                this._center.lng
            ],
            [
                this._center.lat + (this._center.lat - outerCircle.getBounds().getSouth()) * Math.sin(7.0 * Math.PI / 4.0),
                this._center.lng + (this._center.lng - outerCircle.getBounds().getWest()) * Math.cos(7.0 * Math.PI / 4.0)
            ]];

        if (!line) {
            line = L.polyline(latLngs, { color: '#aaa', weight: 1 });
            line._distcircle = { type: 'lineD2' };
            this.addChildLayer(line);
        } else {
            line.setLatLngs(latLngs);
        }
    },

    _addText: function (outerCircle, text) {
        var label = this._circleLableText(outerCircle);
        if (!label)
            return;

        var latLng = [
            this._center.lat + (this._center.lat - outerCircle.getBounds().getSouth()) * Math.sin(Math.PI / 2.0),
            this._center.lng + (this._center.lng - outerCircle.getBounds().getWest()) * Math.cos(Math.PI / 2.0)
        ];

        if (!text) {
            text = L.svgText(latLng, { text: label });
            text._distcircle = { type: 'text' };
            this.addChildLayer(text);
        } else {
            text.setLatLng(latLng);
            text.setText(label);
        }
    },

    _circleLableText: function (circle) {
        if (!circle || circle.getRadius() === 0.0)
            return '';

        var radius = circle.getRadius(), label;
        if (radius > 1000) {
            radius /= 1000;
            label = (Math.round(radius * 10) / 10).toString() + 'km';
        } else {
            label = (Math.round(radius * 100) / 100).toString() + 'm';
        }

        return label;
    },

    toGeoJSON: function () {
        var geoJson = {
            geometry: {
                type: 'Point',
                coordinates: []
            }
        };

        if (this._center) {
            geoJson.geometry.coordinates = [this._center.lng, this._center.lat];
        }

        return geoJson;
    }

});

L.distanceCircle = function (latLngs, options) {
    return new L.DistanceCircle(latLngs, options);
};
