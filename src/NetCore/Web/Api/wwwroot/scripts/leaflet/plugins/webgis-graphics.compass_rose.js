L.CompassRose = L.LayerCollection.extend({
    options: {
        color: '#ff0000',
        weight: 2,
        steps: 36,
        radius: 10,
        calcCrs: null
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
            let radius = this.options.radius;
            let steps = Math.max(this.options.steps || 8, 1);

            this.removeAllChildLayers();  // rebuild always!
            let count = this.getChildLayerCount();
            if (count === 0) {
                let circlePoints = this._circlePoints(this._center, radius, 360, true);
                let innerCirclePoints = this._circlePoints(this._center, radius / 10.0, 360, true);

                let circlePolygon = L.polygon(circlePoints, { color: this.options.color, weight: this.options.weight, fillColor: '#fff', fillOpacity: 0.0 });
                let innerCircleLine = L.polyline(innerCirclePoints, { color: this.options.color, weight: this.options.weight });

                this.addChildLayer(circlePolygon);
                this.addChildLayer(innerCircleLine);

                circlePolygon.on('click', function (e) { this.fireEvent('click', e); }, this);

                circlePoints = this._circlePoints(this._center, radius, 8);
                innerCirclePoints = this._circlePoints(this._center, radius / 10.0, 8);
                let labels = ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'];
                for (var i = 0; i < circlePoints.length; i++)
                {
                    let line = L.polyline([innerCirclePoints[i], circlePoints[i]], { color: this.options.color, weight: this.options.weight });
                    this.addChildLayer(line);

                    let p = [
                        { x: innerCirclePoints[i][1], y: innerCirclePoints[i][0] },
                        { x: circlePoints[i][1], y: circlePoints[i][0] }
                    ];
                    let insertAt = { lng: (p[0].x + p[1].x) * .5, lat: (p[0].y + p[1].y) * .5 };
                    let angle = 0;

                    let text = L.svgLabel(insertAt, {
                        text: labels[i],
                        rotation: -angle,
                        fontSize: this.options.fontSize,
                        alignmentBaseline: 'ideographic' //'central'
                    });
                    this.addChildLayer(text);
                }

                circlePoints = this._circlePoints(this._center, radius * 1.2, steps);
                innerCirclePoints = this._circlePoints(this._center, radius, steps);
                for (var i = 0; i < circlePoints.length; i++) {
                    let line = L.polyline([innerCirclePoints[i], circlePoints[i]], { color: this.options.color, weight: this.options.weight });
                    this.addChildLayer(line);

                    let p = [
                        { x: innerCirclePoints[i][1], y: innerCirclePoints[i][0] },
                        { x: circlePoints[i][1], y: circlePoints[i][0] }
                    ];
                    let insertAt = { lng: circlePoints[i][1], lat: circlePoints[i][0] };
                    let angle = 0;

                    let text = L.svgLabel(insertAt, {
                        text: parseInt(i * 360.0/steps) + '°',
                        rotation: -angle,
                        fontSize: this.options.fontSize,
                        alignmentBaseline: 'ideographic' //'central'
                    });
                    this.addChildLayer(text);
                }
            }
        }
    },

    _circlePoints: function (centerLatLng, radius, steps, closeIt) {
        let c = { x: centerLatLng.lng, y: centerLatLng.lat };
        webgis.complementProjected(this.options.calcCrs, [c]);

        let points = [];
        for (var i = 0; i < steps; i++) {
            let w = 2.0 * Math.PI / steps * i;

            points.push({ X: c.X + Math.sin(w) * radius, Y: c.Y + Math.cos(w) * radius });
        }
        if (closeIt) {
            points.push(points[0]);
        }

        webgis.complementWGS84(this.options.calcCrs, points);

        let result = [];
        for (var p of points) {
            result.push([p.y, p.x]);
        };

        return result;
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

L.compassRose = function (latLngs, options) {
    return new L.CompassRose(latLngs, options);
};
