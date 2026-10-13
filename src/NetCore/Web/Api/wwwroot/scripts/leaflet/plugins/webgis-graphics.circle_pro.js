L.CirclePro = L.LayerCollection.extend({
    options: {
        color: '#ff0000',
        fillColor: '#eeffff',
        fillOpacity: 0.0,
        weight: 1,
        radius: 5000,
        calcCrs: null
    },

    _center: null,
    _circlePoint: null,

    _setLatLngs: function (latLngs) {
        if (latLngs.length > 0) {
            this.setLatLng(latLngs[0]);
        }
    },

    onAdd: function () {
        this.rebuild();
    },

    setLatLng: function (latLng) {
        this._center = latLng;
        this.rebuild();
    },

    // rquired for Stecktools: XY Absolute, Richtung u Entfernung
    getLatLngs: function () {
        return [
            this._center,
            this._circlePoint
        ];
    },

    setCirclePoint: function (latLng) {
        this._circlePoint = latLng;
        this.rebuild();
    },
    getCenterPoint: function () {
        return this._center ? { lng: this._center.lng, lat: this._center.lat } : null;
    },
    getCirclePoint: function () {
        return this._circlePoint ? { lng: this._circlePoint.lng, lat: this._circlePoint.lat } : null;
    },
    getRadius: function() {
        if (this._center && this._circlePoint) {
            if (this.options.calcCrs) {
                var vertices = [{ x: this._center.lng, y: this._center.lat }, { x: this._circlePoint.lng, y: this._circlePoint.lat }];
                webgis.complementProjected(this.options.calcCrs, vertices);

                return Math.sqrt(Math.pow(vertices[1].X - vertices[0].X, 2) + Math.pow(vertices[1].Y - vertices[0].Y, 2));
            } else {
                return this._center.distanceTo(this._circlePoint);
            }
        }

        return 0.0;
    },

    rebuild: function () {
        if (this._center && this._circlePoint && this._map) {
            var radius = this.getRadius();

            var midPoint = [(this._center.lat + this._circlePoint.lat) / 2.0, (this._center.lng + this._circlePoint.lng) / 2.0];

            var count = this.getChildLayerCount();
            if (count === 0) {
                var c = L.circle(this._center, {
                    radius: radius,
                    color: this.options.color,
                    fillColor: this.options.fillColor,
                    weight: this.options.weight
                });

                c._circlepro = { type: 'circle' };
                this.addChildLayer(c);

                line = L.polyline([this._center, this._circlePoint], { color: this.options.color, weight: this.options.weight });
                line._circlepro = { type: 'line' };
                this.addChildLayer(line);

                this.addChildLayer(this._addText(midPoint));

            } else {
                for (var i = 0; i < count; i++) {
                    var c = this.getChildLayer(i);
                    switch (c._circlepro.type) {
                        case 'circle':
                            c.setLatLng(this._center);
                            c.setRadius(this.getRadius());
                            break;
                        case 'text':
                            this._addText(midPoint, c);
                            break;
                        case 'line':
                            c.setLatLngs([this._center, this._circlePoint ])
                            break;
                    }
                }
            }
        }
    },

    _addText: function (latLng, text) {
        var label = this._circleLableText();
        if (!label)
            return;

        if (!text) {
            text = L.svgText(latLng, { text: label });
            text._circlepro = { type: 'text' };
            this.addChildLayer(text);
        } else {
            text.setLatLng(latLng);
            text.setText(label);
        }
    },

    _circleLableText: function (circle) {
        var radius = this.getRadius();
        if (radius === 0.0)
            return '';

        var label;
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

L.circlePro = function (latLngs, options) {
    return new L.CirclePro(latLngs, options);
};
