

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

L.AngleGraphics = L.LayerCollection.extend({
    options: {
        color: '#aaaaaa',
        weight: 1
    },

    _latLngs: null,
    _setLatLngs: function (latLngs) {
        this.setLatLngs(latLngs);
    },

    onAdd: function () {
        this.rebuild();
    },
    setLatLngs: function (latLngs) {
        this._latLngs = latLngs;
        this.rebuild();
    },
    getLatLngs: function () { return this._latLngs; },
    rebuild: function () {
        if (this._latLngs && this._latLngs.length >= 2) {

            var count = this.getChildLayerCount();
            if (count === 0) {
                var line = L.polyline([this._latLngs[0], this._latLngs[1]], this.options);
                this.addChildLayer(line);

                var rectangle = L.polyline([], this.options);
                this.addChildLayer(rectangle);
                this._rebuild_rect(rectangle);

                var arrow = L.polyline([], this.options);
                this.addChildLayer(arrow);
                this._rebuild_arrow(arrow);
            } else {
                var line = this.getChildLayer(0);
                line.setLatLngs([this._latLngs[0], this._latLngs[1]]);

                this._rebuild_rect(this.getChildLayer(1));
                this._rebuild_arrow(this.getChildLayer(2));
            }
        }
    },
    _rebuild_rect: function (element) {
        var p = [
            { x: this._latLngs[0].lng, y: this._latLngs[0].lat },
            { x: this._latLngs[1].lng, y: this._latLngs[1].lat }
        ];

        var xProp = "x", yProp = "y";

        if (webgis.calc._canProject()) {
            p = webgis.calc._projectToCalcCrs(p);
            xProp = "X"; yProp = "Y";
        }

        var ox1 = p[0][xProp], oy1 = p[0][yProp];
        var ox2 = p[1][xProp], oy2 = p[1][yProp];

        var dx = ox2 - ox1;
        var dy = oy2 - oy1;

        rect = [{}, {}, {}, {}, {}];
        rect[0][yProp] = oy1 - dy / 2 + dx / 4; rect[0][xProp] = ox1 - dx / 2 - dy / 4;
        rect[1][yProp] = oy1 + dy / 2 + dx / 4; rect[1][xProp] = ox1 + dx / 2 - dy / 4;
        rect[2][yProp] = oy1 + dy / 2 - dx / 4; rect[2][xProp] = ox1 + dx / 2 + dy / 4;
        rect[3][yProp] = oy1 - dy / 2 - dx / 4; rect[3][xProp] = ox1 - dx / 2 + dy / 4;
        rect[4][yProp] = oy1 - dy / 2 + dx / 4; rect[4][xProp] = ox1 - dx / 2 - dy / 4;

        if (webgis.calc._canProject()) {
            rect = webgis.calc._unprojectFromCalcCrs(rect);
        }

        element.setLatLngs(this._toLatLngs(rect));
    },
    _rebuild_arrow: function (element) {
        var p = [
            { x: this._latLngs[0].lng, y: this._latLngs[0].lat },
            { x: this._latLngs[1].lng, y: this._latLngs[1].lat }
        ];

        var xProp = "x", yProp = "y";

        if (webgis.calc._canProject()) {
            p = webgis.calc._projectToCalcCrs(p);
            xProp = "X"; yProp = "Y";
            //console.log(p);
        }

        var ox1 = p[0][xProp], oy1 = p[0][yProp];
        var ox2 = p[1][xProp], oy2 = p[1][yProp];

        var dx = ox2 - ox1;
        var dy = oy2 - oy1;

        arrow = [{}, {}, {}];
        arrow[0][yProp] = oy1 - dy / 2 - dx / 4; arrow[0][xProp] = ox1 - dx / 2 + dy / 4;
        arrow[1][yProp] = oy1 + dx / 4;          arrow[1][xProp] = ox1 - dy / 4;
        arrow[2][yProp] = oy1 + dy / 2 - dx / 4; arrow[2][xProp] = ox1 + dx / 2 + dy / 4;

        if (webgis.calc._canProject()) {
            arrow = webgis.calc._unprojectFromCalcCrs(arrow);
            //console.log(arrow);
        }

        element.setLatLngs(this._toLatLngs(arrow));
    },
    _toLatLngs: function (coords) {
        var latLngs = [];

        for (var i in coords) {
            latLngs.push([coords[i].y, coords[i].x]);
        }

        return latLngs;
    }
});

L.angleGraphics = function (latLngs, options) {
    return new L.AngleGraphics(latLngs, options);
}

L.SVGText = L.Layer.extend({
    options: {
        fontColor: 'black',
        fontSize: 14,
        fontSytle: '',
        text: 'SVG Text',
        refResolution: 0,
        textAnchor: '',
        attributes: {}
    },

    initialize: function (latlng, options) {
        L.setOptions(this, options);
        this.setLatLng(latlng);
    },

    beforeAdd: function (map) {
        // Renderer is set here because we need to call renderer.getEvents
        // before this.getEvents.
        this._renderer = map.getRenderer(this);

        map.on('moveend', this.redraw, this);
        map.on('zoomend', this.redraw, this);
        map.on('click', this._mapClick, this);
    },

    onAdd: function () {
        this.redraw();
    },

    onRemove: function () {
        if (this._textNode) {
            this._textNode.parentNode.removeChild(this._textNode);
            this._textNode = null;
            this._textNode2.parentNode.removeChild(this._textNode2);
            this._textNode2 = null;
        }

        this._map.off('moveend', this.redraw, this);
        this._map.off('zoomend', this.redraw, this);
        this._map.off('click', this._mapClick, this);
    },

    _pos: null,
    setLatLng: function (latlng) {
        this._pos = latlng;
        this.redraw();
    },

    setText: function (text) {
        this.options.text = text;
        this.redraw();
    },
    getText: function () {
        return this.options.text;
    },

    setStyle: function (style) {
        for (var i in style) {
            switch (i.toLocaleLowerCase()) {
                case 'fill':
                case 'font-color':
                    this._textNode.style['fill'] = this.options.fontColor = style[i];
                    this._textNode2.style['stroke'] = this._grayValue(this._textNode.style['fill']) < 150 ? '#fff' : '#000';
                    break;
                case 'font-size':
                case 'fontsize':
                case 'size':
                    //console.log('setsize', this.options.refResolution);
                    if (this.options.refResolution === 0) {
                        this.options.refResolution = this._currentMapResolution();
                    }
                    this.options.fontSize = style[i];
                    this._setRefSize();
                    this.redraw();
                    break;
                case 'font-style':
                case 'fontstyle':
                case 'style':
                    this.options.fontStyle = style[i];
                    switch (style[i]) {
                        case 'regular':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'normal';
                            break;
                        case 'italic':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'italic';
                            break;
                        case 'bold':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'normal';
                            this._textNode.style['font-weight'] = this._textNode2.style['font-weight'] = 'bold';
                            break;
                        case 'bolditalic':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'italic';
                            this._textNode.style['font-weight'] = this._textNode2.style['font-weight'] = 'bold';
                            break;
                        default:
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'normal';
                            this._textNode.style['font-weight'] = this._textNode2.style['font-weight'] = 'normal';
                            break;
                    }
                    break;
                default:
                    this._textNode.style[i] = style[i];
                    break;
            }
        }

        //console.log(this.options);
        //L.setOptions(this._textNode, style);
        return this;
    },

    redraw: function () {
        //console.log('text redraw', this._map, this._pos, this._textNode);

        if (!this._textNode) {
            this.rebuild();
        }

        if (this._map && this._pos && this._textNode) {
            var point = this._map.latLngToLayerPoint(this._pos);

            var text = (this.options.text || '').replace(/ /g, '\u00A0').split('\n');  // Non breakable spaces
            this._textNode.innerHTML = this._textNode2.innerHTML = '';

            for (var t in text) {
                var tspan = L.SVG.create('tspan');
                var tspan2 = L.SVG.create('tspan');

                tspan.setAttribute('x', point.x);
                tspan.setAttribute('dy', t > 0 ? this.options.fontSize * 1.3 : 0);

                tspan2.setAttribute('x', point.x);
                tspan2.setAttribute('dy', t > 0 ? this.options.fontSize * 1.3 : 0);

                if (this.options.textAnchor) {
                    tspan.setAttribute('text-anchor', this.options.textAnchor);
                    tspan2.setAttribute('text-anchor', this.options.textAnchor);
                }

                tspan.appendChild(document.createTextNode(text[t]));
                tspan2.appendChild(document.createTextNode(text[t]));

                this._textNode.appendChild(tspan);
                this._textNode2.appendChild(tspan2);
            }

            this._textNode.setAttribute('x', point.x);
            this._textNode.setAttribute('y', point.y);

            this._textNode2.setAttribute('x', point.x);
            this._textNode2.setAttribute('y', point.y);

            this._setRefSize();
        }

        return this;
    },

    _textNode: null,
    _textNodeStroke: null,
    rebuild: function () {
        if (!L.Browser.svg || typeof this._map === 'undefined') {
            return this;
        }

        if (!this._textNode) {
            var id = 'svgtext-' + L.Util.stamp(this);
            var id2 = 'svgtext-' + L.Util.stamp(this);
            var svg = this._map._renderer._container;

            this._textNode = L.SVG.create('text');
            this._textNode2 = L.SVG.create('text');
            for (var attr in this.options.attributes) {
                this._textNode.setAttribute(attr, this.options.attributes[attr]);
                this._textNode.setAttributeNS("http://www.w3.org/1999/xlink", "xlink:href", '#' + id);

                this._textNode2.setAttribute(attr, this.options.attributes[attr]);
                this._textNode2.setAttributeNS("http://www.w3.org/1999/xlink", "xlink:href", '#' + id2);
            }

            this._textNode2.style["stroke-width"] = 2;

            this.setStyle({'fill': this.options.fontColor });
            this.setStyle({'font-size': this.options.fontSize });
            this.setStyle({'font-style': this.options.fontStyle });

            svg.childNodes[0].appendChild(this._textNode2);
            svg.childNodes[0].appendChild(this._textNode);
        }
    },

    _mapClick: function (e) {
        if (this._pos && this._textNode) {
            var clickPoint = this._map.latLngToLayerPoint(e.latlng);
            var bbox = this._textNode.getBBox();
            
            if (clickPoint.x >= bbox.x && clickPoint.y >= bbox.y &&
                clickPoint.x <= bbox.x + bbox.width && clickPoint.y <= bbox.y + bbox.height) {

                e.originalEvent.preventDefault();
                this.fireEvent('click', e);
            }
        }
    },

    _parseColor: function (input) {
        try {
            if (input.substr(0, 1) === "#") {
                var collen = (input.length - 1) / 3;
                var fact = [17, 1, 0.062272][collen - 1];
                return [
                    Math.round(parseInt(input.substr(1, collen), 16) * fact),
                    Math.round(parseInt(input.substr(1 + collen, collen), 16) * fact),
                    Math.round(parseInt(input.substr(1 + 2 * collen, collen), 16) * fact)
                ];
            }
            else return input.split("(")[1].split(")")[0].split(",").map(Math.round);
        } catch(ex) { return null; }
    },
    _grayValue: function (input) {
        var col = this._parseColor(input);
        if (col) {
            return (col[0] + col[1] + col[2]) / 3;
        }

        return 0;
    },

    _currentMapResolution: function () {
        try {
            if (this._map.options.crs.options.resolutions) {
                //console.log(this._map.options.crs);
                return this._map.options.crs.options.resolutions[this._map.getZoom()];
            }
        } catch (ex) { console.log(ex); }

        return 0;
    },

    _setRefSize: function () {
        // Reference Scale: später einmal ...

        //console.log('_setRefSize',this.options.refResolution);
        //if (this.options.refResolution > 0) {
        //    var currentResolution = this._currentMapResolution();
        //    console.log('_setRefSize2',this.options.currentResolution);
        //    if (currentResolution > 0) {
        //        var factor = this.options.refResolution / currentResolution;

        //        console.log('refResolution', this.options.refResolution);
        //        console.log('currentResolution', currentResolution);
        //        console.log('factor', factor);

        //        this._textNode.style['font-size'] = this._textNode2.style['font-size'] = this.options.fontSize * factor;
        //        return;
        //    }
        //}

        this._textNode.style['font-size'] = this._textNode2.style['font-size'] = this.options.fontSize + 'px';
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

L.svgText = function (latLng, options) {
    return new L.SVGText(latLng, options);
};

L.SVGLabel = L.Layer.extend({
    options: {
        fontColor: 'black',
        fontSize: 14,
        fontSytle: '',
        text: 'SVG Label',
        anchor: 'middle',
        alignmentBaseline:'ideographic',
        rotation: 0,
        refResolution: 0,
        attributes: {}
    },

    initialize: function (latlng, options) {
        L.setOptions(this, options);
        this.setLatLng(latlng);
    },

    beforeAdd: function (map) {
        // Renderer is set here because we need to call renderer.getEvents
        // before this.getEvents.
        this._renderer = map.getRenderer(this);

        map.on('moveend', this.redraw, this);
        map.on('zoomend', this.redraw, this);
    },

    onAdd: function () {
        this.redraw();
    },

    onRemove: function () {
        if (this._textNode) {
            this._textNode.parentNode.removeChild(this._textNode);
            this._textNode = null;
            this._textNode2.parentNode.removeChild(this._textNode2);
            this._textNode2 = null;
        }

        this._map.off('moveend', this.redraw, this);
        this._map.off('zoomend', this.redraw, this);
    },

    _pos: null,
    setLatLng: function (latlng) {
        this._pos = latlng;
        this.redraw();
    },

    setText: function (text) {
        this.options.text = text;
        this.redraw();
    },
    getText: function () {
        return this.options.text;
    },

    setStyle: function (style) {
        for (var i in style) {
            switch (i.toLocaleLowerCase()) {
                case 'fill':
                case 'font-color':
                    this._textNode.style['fill'] = this.options.fontColor = style[i];
                    this._textNode2.style['stroke'] = this._grayValue(this._textNode.style['fill']) < 150 ? '#fff' : '#000';
                    break;
                case 'font-size':
                case 'fontsize':
                case 'size':
                    //console.log('setsize', this.options.refResolution);
                    if (this.options.refResolution === 0) {
                        this.options.refResolution = this._currentMapResolution();
                    }
                    this.options.fontSize = style[i];
                    this._setRefSize();
                    break;
                case 'font-style':
                case 'fontstyle':
                case 'style':
                    this.options.fontStyle = style[i];
                    switch (style[i]) {
                        case 'regular':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'normal';
                            break;
                        case 'italic':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'italic';
                            break;
                        case 'bold':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'normal';
                            this._textNode.style['font-weight'] = this._textNode2.style['font-weight'] = 'bold';
                            break;
                        case 'bolditalic':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'italic';
                            this._textNode.style['font-weight'] = this._textNode2.style['font-weight'] = 'bold';
                            break;
                        default:
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'normal';
                            this._textNode.style['font-weight'] = this._textNode2.style['font-weight'] = 'normal';
                            break;
                    }
                    break;
                default:
                    this._textNode.style[i] = style[i];
                    break;
            }
        }

        //console.log(this.options);
        //L.setOptions(this._textNode, style);
        return this;
    },

    redraw: function () {
        //console.log('text redraw', this._map, this._pos, this._textNode);

        if (!this._textNode) {
            this.rebuild();
        }

        if (this._map && this._pos && this._textNode) {
            var point = this._map.latLngToLayerPoint(this._pos);

            var text = (this.options.text || '').replace(/ /g, '\u00A0');  // Non breakable spaces
            this._textNode.innerHTML = this._textNode2.innerHTML = '';

            this._textNode.appendChild(document.createTextNode(text));
            this._textNode2.appendChild(document.createTextNode(text));


            this._textNode.setAttribute('text-anchor', this.options.anchor);
            this._textNode.setAttribute('alignment-baseline', this.options.alignmentBaseline);
            this._textNode.setAttribute('transform', 'translate(' + point.x + ',' + point.y + ') rotate(' + this.options.rotation + ')');

            this._textNode2.setAttribute('text-anchor', this.options.anchor);
            this._textNode2.setAttribute('alignment-baseline', this.options.alignmentBaseline);
            this._textNode2.setAttribute('transform', 'translate(' + point.x + ',' + point.y + ') rotate(' + this.options.rotation + ')');

            this._setRefSize();
        }

        return this;
    },

    _textNode: null,
    _textNodeStroke: null,
    rebuild: function () {
        if (!L.Browser.svg || typeof this._map === 'undefined') {
            return this;
        }

        if (!this._textNode) {
            var id = 'svgtext-' + L.Util.stamp(this);
            var id2 = 'svgtext-' + L.Util.stamp(this);
            var svg = this._map._renderer._container;

            this._textNode = L.SVG.create('text');
            this._textNode2 = L.SVG.create('text');
            for (var attr in this.options.attributes) {
                this._textNode.setAttribute(attr, this.options.attributes[attr]);
                this._textNode.setAttributeNS("http://www.w3.org/1999/xlink", "xlink:href", '#' + id);

                this._textNode2.setAttribute(attr, this.options.attributes[attr]);
                this._textNode2.setAttributeNS("http://www.w3.org/1999/xlink", "xlink:href", '#' + id2);
            }

            this._textNode2.style["stroke-width"] = 2;

            this.setStyle({ 'fill': this.options.fontColor });
            this.setStyle({ 'font-size': this.options.fontSize });
            this.setStyle({ 'font-style': this.options.fontStyle });

            svg.childNodes[0].appendChild(this._textNode2);
            svg.childNodes[0].appendChild(this._textNode);
        }
    },

    _parseColor: function (input) {
        try {
            if (input.substr(0, 1) === "#") {
                var collen = (input.length - 1) / 3;
                var fact = [17, 1, 0.062272][collen - 1];
                return [
                    Math.round(parseInt(input.substr(1, collen), 16) * fact),
                    Math.round(parseInt(input.substr(1 + collen, collen), 16) * fact),
                    Math.round(parseInt(input.substr(1 + 2 * collen, collen), 16) * fact)
                ];
            }
            else return input.split("(")[1].split(")")[0].split(",").map(Math.round);
        } catch (ex) { return null; }
    },
    _grayValue: function (input) {
        var col = this._parseColor(input);
        if (col) {
            return (col[0] + col[1] + col[2]) / 3;
        }

        return 0;
    },

    _currentMapResolution: function () {
        try {
            if (this._map.options.crs.options.resolutions) {
                //console.log(this._map.options.crs);
                return this._map.options.crs.options.resolutions[this._map.getZoom()];
            }
        } catch (ex) { console.log(ex); }

        return 0;
    },

    _setRefSize: function () {
        // Reference Scale: später einmal ...

        //console.log('_setRefSize',this.options.refResolution);
        //if (this.options.refResolution > 0) {
        //    var currentResolution = this._currentMapResolution();
        //    console.log('_setRefSize2',this.options.currentResolution);
        //    if (currentResolution > 0) {
        //        var factor = this.options.refResolution / currentResolution;

        //        console.log('refResolution', this.options.refResolution);
        //        console.log('currentResolution', currentResolution);
        //        console.log('factor', factor);

        //        this._textNode.style['font-size'] = this._textNode2.style['font-size'] = this.options.fontSize * factor;
        //        return;
        //    }
        //}

        this._textNode.style['font-size'] = this._textNode2.style['font-size'] = this.options.fontSize;
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

L.svgLabel = function (latLng, options) {
    return new L.SVGLabel(latLng, options);
};

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

L.DimLine = L.LayerCollection.extend({
    options: {
        color: '#ff0000',
        weight: 2,
        fontColor: 'black',
        fontSize: 13,
        fontSytle: '',
        circleMarkerOptions: { same_as_line: true, radius: 4, color: '#000', weight: 2, fillcolor: '#eee' },
        lengthUnit: 'm',
        labelTotalLength: false,
        attributes: {}
    },

    _calcCrs:0,
    _latLngs: null,
    _setLatLngs: function (latLngs) {
        this.setLatLngs(latLngs);
    },

    _setStyle: function (style) {
        for (var i in style) {
            switch (i.toLocaleLowerCase()) {
                case 'font-size':
                case 'fontsize':
                case 'size':
                    this.options.fontSize = style[i];
                    break;
            }
        }
    },

    onAdd: function () {
        this.rebuild();
    },
    setLatLngs: function (latLngs) {
        this._latLngs = latLngs;
        this.rebuild();
    },
    addLatLng: function (latLng) {
        if (this._latLngs)
            this._latLngs.push(latLng);
        else
            this._latLngs = [latLng];

        this.rebuild();
    },
    getLatLngs: function () { return this._latLngs; },
    getCalcCrs: function () { return this._calcCrs; },
    setLengthUnit: function (unit) {
        this.options.lengthUnit = unit;
        this.rebuild();
    },
    getLengthUnit: function () {
        return this.options.lengthUnit;
    },
    setLabelTotalLength: function (doLabel) {
        this.options.labelTotalLength = doLabel;
        this.rebuild();
    },
    getLabelTotalLength: function () {
        return this.options.labelTotalLength;
    },
    redraw: function () {
        this.rebuild();
    },
    rebuild: function () {
        if (this._latLngs && this._latLngs.length >= 2) {
            var count = this.getChildLayerCount();
            if (count === 0) {
                var line = L.polyline(this._latLngs, this.options);
                this.addChildLayer(line);
                line.on('click', function (e) {
                    this.fireEvent('click', e);
                }, this);
            } else {
                var line = this.getChildLayer(0);
                line.setLatLngs(this._latLngs);

                this.trimChildren(1);
            }

            if (this.options.circleMarkerOptions.same_as_line === true) {
                this.options.circleMarkerOptions.color = this.options.color;
                this.options.circleMarkerOptions.weight = this.options.weight;
            }

            let lengthFactor = 1.0;
            let lengthUnit = '';
            let totalLength = 0.0;

            switch (this.options.lengthUnit) {
                case 'km':
                    lengthFactor = 0.001;
                    lengthUnit = 'km';
                    break;
                default:
                    lengthFactor = 1.0;
                    lengthUnit = 'm';
                    break;
            }

            for (var i = 0; i < this._latLngs.length - 1; i++) {
                var p = [
                    { x: this._latLngs[i].lng,     y: this._latLngs[i].lat },
                    { x: this._latLngs[i + 1].lng, y: this._latLngs[i + 1].lat }
                ];
                var insertAt = { lng: (p[0].x + p[1].x) * .5, lat: (p[0].y + p[1].y) * .5 };

                var xProp = "x", yProp = "y";

                //console.log(p);

                if (webgis.calc._canProject()) {
                    this._calcCrs = webgis.calc.getCalcCrsId(p);
                    p = webgis.calc._projectToCalcCrs(p);
                    xProp = "X"; yProp = "Y";
                    //console.log(p);
                }

                var dx = p[1][xProp] - p[0][xProp];
                var dy = p[1][yProp] - p[0][yProp];
                var len = Math.sqrt(dx * dx + dy * dy);
                var angle = Math.atan2(dy, dx) * 180.0 / Math.PI;
                if (angle < 0)
                    angle += 360.0;

                //console.log(angle);

                if (angle > 90.0 && angle < 270)
                    angle += 180;

                var circle = L.circleMarker(this._latLngs[i], this.options.circleMarkerOptions);
                this.addChildLayer(circle);

                totalLength += len;

                var text = L.svgLabel(insertAt, {
                    text: Math.round(len * lengthFactor * 100.0) / 100.0 + lengthUnit,
                    rotation: -angle,
                    fontSize: this.options.fontSize,
                    alignmentBaseline: 'ideographic' //'central'
                });
                this.addChildLayer(text);
            }

            var circle = L.circleMarker(this._latLngs[this._latLngs.length - 1], this.options.circleMarkerOptions);
            this.addChildLayer(circle);

            if (this.options.labelTotalLength && this._latLngs.length > 2) {
                this.addChildLayer(L.svgText(this._latLngs[this._latLngs.length - 1], {
                    text: " ∑: " + Math.round(totalLength * lengthFactor * 100.0) / 100.0 + lengthUnit,
                    fontSize: this.options.fontSize * 1.2
                }));
            }
        }
    },
    toGeoJSON: function () {
        var geoJson = {
            geometry: {
                type: 'LineString',
                coordinates: []
            }
        };

           
        if (this._latLngs) {
            for (var l in this._latLngs) {
                geoJson.geometry.coordinates.push([this._latLngs[l].lng, this._latLngs[l].lat]);
            }
        }

        return geoJson;
    }
});

L.dimLine = function (latLngs, options) {
    return new L.DimLine(latLngs, options);
};

L.DimPolygon = L.LayerCollection.extend({
    options: {
        fillColor: '#ffff00',
        fillOpacity: 0.2,
        strokeColor: '#ff0000',
        strokeWeight: 2,
        fontColor: 'black',
        fontSize: 13,
        fontSytle: '',
        circleMarkerOptions: { same_as_line: true, radius: 4, color: '#000', weight: 2, fillcolor: '#eee' },
        areaUnit: 'm²',
        labelEdges: false,
        attributes: {}
    },

    _calcCrs: 0,
    _latLngs: null,
    _setLatLngs: function (latLngs) {
        this.setLatLngs(latLngs);
    },

    _setStyle: function (style) {
        for (var i in style) {
            switch (i.toLocaleLowerCase()) {
                case 'font-size':
                case 'fontsize':
                case 'size':
                    this.options.fontSize = style[i];
                    break;
            }
        }
    },

    onAdd: function () {
        this.rebuild();
    },
    setLatLngs: function (latLngs) {
        this._latLngs = latLngs;
        this.rebuild();
    },
    addLatLng: function (latLng) {
        if (this._latLngs)
            this._latLngs.push(latLng);
        else
            this._latLngs = [latLng];

        this.rebuild();
    },
    getLatLngs: function () { return this._latLngs; },
    getCalcCrs: function () { return this._calcCrs; },
    setAreaUnit: function (unit) {
        this.options.areaUnit = unit;
        this.rebuild();
    },
    getAreaUnit: function () {
        return this.options.areaUnit;
    },
    setLabelEdges: function (doLabel) {
        this.options.labelEdges = doLabel;
        this.rebuild();
    },
    getLabelEdges: function () {
        return this.options.labelEdges;
    },
    redraw: function () {
        this.rebuild();
    },
    rebuild: function () {
        if (this._latLngs && this._latLngs.length >= 2) {
            let count = this.getChildLayerCount();
            if (count === 0) {
                let polygon = L.polygon(this._latLngs, this.options);
                this.addChildLayer(polygon);
                polygon.on('click', function (e) {
                    this.fireEvent('click', e);
                }, this);
            } else {
                var polygon = this.getChildLayer(0);
                polygon.setLatLngs(this._latLngs);

                this.trimChildren(1);
            }

            if (this.options.circleMarkerOptions.same_as_line === true) {
                this.options.circleMarkerOptions.color = this.options.color;
                this.options.circleMarkerOptions.weight = this.options.weight;
            }

            let xProp = "x", yProp = "y";
            if (webgis.calc._canProject()) {
                xProp = "X"; yProp = "Y";
            }
            let calcVertices = [];
            let insertAtArea = { lng: 0, lat: 0 };

            let lengthFactor = 1.0;
            let areaFactor = 1.0;
            let lengthUnit = ''
            let areaUnit = '';

            switch (this.options.areaUnit) {
                case 'km':
                case 'km²':
                case 'km2':
                    lengthFactor = 1.0/1000.0;
                    areaFactor = 1.0/1000000.0;
                    lengthUnit = 'km';
                    areaUnit = 'km²';
                    break;
                case "ha":
                    lengthFactor = 1.0;  // use meters
                    areaFactor = 1.0 / 100.0;
                    lengthUnit = 'm';
                    areaUnit = 'ha';
                    break;
                case "a":
                case "ar":
                    lengthFactor = 1.0;  // use meters
                    areaFactor = 1.0 / 10.0;
                    lengthUnit = 'm';
                    areaUnit = 'a';
                    break;
                default:  // m
                    lengthFactor = 1.0;
                    areaFactor = 1.0;
                    lengthUnit = 'm';
                    areaUnit = 'm²';
            }


            for (let i = 0; i < this._latLngs.length; i++) {
                let p = [
                    { x: this._latLngs[i].lng, y: this._latLngs[i].lat },
                    { x: this._latLngs[(i + 1) % this._latLngs.length].lng, y: this._latLngs[(i + 1) % this._latLngs.length].lat }
                ];
                let insertAt = { lng: (p[0].x + p[1].x) * .5, lat: (p[0].y + p[1].y) * .5 };

               
                //console.log(p);

                if (webgis.calc._canProject()) {
                    this._calcCrs = webgis.calc.getCalcCrsId(p);
                    p = webgis.calc._projectToCalcCrs(p);
                }

                calcVertices.push({ x: this._latLngs[i].lng, y: this._latLngs[i].lat });
                insertAtArea.lng += (this._latLngs[i].lng - insertAtArea.lng) / calcVertices.length;
                insertAtArea.lat += (this._latLngs[i].lat - insertAtArea.lat) / calcVertices.length;

                let dx = p[1][xProp] - p[0][xProp];
                let dy = p[1][yProp] - p[0][yProp];
                let len = Math.sqrt(dx * dx + dy * dy);
                let angle = Math.atan2(dy, dx) * 180.0 / Math.PI;
                if (angle < 0)
                    angle += 360.0;

                //console.log(angle);

                if (angle > 90.0 && angle < 270)
                    angle += 180;

                let circle = L.circleMarker(this._latLngs[i], this.options.circleMarkerOptions);
                this.addChildLayer(circle);

                if (this.options.labelEdges) {
                    let text = L.svgLabel(insertAt, {
                        text: Math.round(len * lengthFactor * 100.0) / 100.0 + lengthUnit,
                        rotation: -angle,
                        fontSize: this.options.fontSize,
                        alignmentBaseline: 'ideographic' //'central'
                    });
                    this.addChildLayer(text);
                }
            }

            let area = webgis.calc.area(calcVertices, xProp, yProp);
            let circumference = webgis.calc.length(calcVertices, xProp, yProp, true);
            //console.log('calcVertices', calcVertices, 'area', area, 'circumference', circumference);

            if (area > 0) {
                this.addChildLayer(L.svgText(insertAtArea, {
                    text:
                        webgis.l10n.get('area')[0] + ": " + Math.round(area * areaFactor * 100.0) / 100.0 + areaUnit + '\n' +
                        webgis.l10n.get('circumference')[0] + ": " + Math.round(circumference * lengthFactor * 100.0) / 100.0 + lengthUnit,
                    fontSize: this.options.fontSize * 1.2,
                    textAnchor: 'middle'
                }));
            }

            let circle = L.circleMarker(this._latLngs[this._latLngs.length - 1], this.options.circleMarkerOptions);
            this.addChildLayer(circle);
        }
    },
    toGeoJSON: function () {
        let geoJson = {
            geometry: {
                type: 'Polygon',
                coordinates: []
            }
        };

        let ring = [];
        if (this._latLngs) {
            for (let l in this._latLngs) {
                ring.push([this._latLngs[l].lng, this._latLngs[l].lat]);
            }
        }

        geoJson.geometry.coordinates.push(ring);
        //geoJson.geometry.coordinates = ring;

        return geoJson;
    }
})

L.dimPolygon = function (latLngs, options) {
    return new L.DimPolygon(latLngs, options);
};

L.HectoLine = L.LayerCollection.extend({
    options: {
        color: '#ff0000',
        weight: 2,
        fontColor: 'black',
        fontSize: 13,
        fontSytle: '',
        circleMarkerOptions: { same_as_line:true, radius: 4, color: '#000', weight: 2, fillcolor: '#eee' },
        attributes: {},
        unit: 'm',
        interval:100
    },

    _calcCrs: 0,
    _latLngs: null,
    _setLatLngs: function (latLngs) {
        this.setLatLngs(latLngs);
    },

    _setStyle: function (style) {
        for (var i in style) {
            switch (i.toLocaleLowerCase()) {
                case 'font-size':
                case 'fontsize':
                case 'size':
                    this.options.fontSize = style[i];
                    break;
            }
        }
    },

    onAdd: function () {
        this.rebuild();
    },
    setLatLngs: function (latLngs) {
        this._latLngs = latLngs;
        this.rebuild();
    },
    addLatLng: function (latLng) {
        if (this._latLngs)
            this._latLngs.push(latLng);
        else
            this._latLngs = [latLng];

        this.rebuild();
    },
    getLatLngs: function () { return this._latLngs; },
    setUnit: function (unit) {
        this.options.unit = unit;
        this.rebuild();
    },
    getUnit: function () {
        return this.options.unit;
    },
    setInterval: function (interval) {
        this.options.interval = interval;
        this.rebuild();
    },
    getInterval: function () {
        return this.options.interval;
    },
    getCalcCrs: function () { return this._calcCrs; },
    redraw: function () {
        this.rebuild();
    },
    rebuild: function () {
        if (this._latLngs && this._latLngs.length >= 2) {
            var count = this.getChildLayerCount();
            if (count === 0) {
                var line = L.polyline(this._latLngs, this.options);
                this.addChildLayer(line);
                line.on('click', function (e) {
                    this.fireEvent('click', e);
                }, this);
            } else {
                var line = this.getChildLayer(0);
                line.setLatLngs(this._latLngs);

                this.trimChildren(1);
            }

            if (this.options.circleMarkerOptions.same_as_line === true) {
                this.options.circleMarkerOptions.color = this.options.color;
                this.options.circleMarkerOptions.weight = this.options.weight;
            }

            var path = this._projectLatLngs();
            var pathLength = webgis.calc.length(path, "X", "Y"), interval = this.options.interval;
            //console.log('path', path, pathLength);

            switch (this.options.unit) {
                case 'km':
                    interval *= 1000.0;
                    break;
            }

            if (interval >= 1.0 && interval > pathLength / 1000.0) {
                var isEndpoint = false;
                for (var stat = 0, to = pathLength + interval * 2; stat < to; stat += interval) {
                    if (stat > pathLength) {
                        stat = pathLength;
                        isEndpoint = true;
                    }

                    var pathPoint = webgis.calc.pathPoint(path, stat, "X", "Y");
                    if (pathPoint) {
                        var direction = pathPoint.direction;
                        pathPoint = webgis.calc.unproject(pathPoint.x, pathPoint.y);
                        var latLng = { lng: pathPoint.x, lat: pathPoint.y };
                        //console.log('latLng', latLng);
                        var circle = L.circleMarker(latLng, this.options.circleMarkerOptions);
                        this.addChildLayer(circle);

                        //console.log(direction);

                        var labelText = '';
                        switch (this.options.unit) {
                            case 'km':
                                var unitStat = stat / 1000.0;
                                labelText = Math.round(unitStat * 1000.0) / 1000.0 + 'km';
                                break;
                            default:
                                labelText = Math.round(stat * 100.0) / 100.0 + 'm';
                                break;
                        }

                        var label = L.svgLabel(latLng, { text: '  '+ labelText, rotation: 0/* -direction + 90.0*/, fontSize: this.options.fontSize, anchor: 'left', alignmentBaseline: 'ideographic' });
                        this.addChildLayer(label);
                    }
                    if (isEndpoint)
                        break;
                }
            }
        }
    },

    _projectLatLngs: function () {
        var result = [];

        if (this._latLngs && webgis.calc._canProject()) {
            for (var i = 0; i < this._latLngs.length; i++) {
                result.push({ x: this._latLngs[i].lng, y: this._latLngs[i].lat });
            }
            this._calcCrs = webgis.calc.getCalcCrsId(result);
            result = webgis.calc._projectToCalcCrs(result);
        }

        return result;
    },

    toGeoJSON: function () {
        var geoJson = {
            geometry: {
                type: 'LineString',
                coordinates: []
            }
        };


        if (this._latLngs) {
            for (var l in this._latLngs) {
                geoJson.geometry.coordinates.push([this._latLngs[l].lng, this._latLngs[l].lat]);
            }
        }

        return geoJson;
    }
});

L.hectoLine = function (latLngs, options) {
    return new L.HectoLine(latLngs, options);
};

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

L.DirectionLine = L.LayerCollection.extend({


    _setLatLngs: function (latLngs) {
        
    },

    getLatLngs: function () {
        var latLngs = [];

        if (this.options.vertices) {
            for (var v in this.options.vertices) {
                latLngs.push({ lng: this.options.vertices[v].x, lat: this.options.vertices[v].y });
            }
        }

        return latLngs;
    },

    setVertices: function (vertices) {
        this.options.vertices = vertices;
        this.rebuild();
    },
    getVertices: function () {
        return this.options.vertices;
    },

    onAdd: function () {
        this.setVertices(this.options.vertices); 
    },

    setDirectionVertex: function (vertex) {
        if (this.options.vertices && this.options.vertices.length === 2) {
            this.options.vertices[1] = vertex;

            this.rebuild();
        }
    },

    rebuild: function () {
        if (!this.options.sketch || !this.options.vertices || this.options.vertices.length !== 2) {
            return;
        }

        var polyline = this.getChildLayer(0);
        //var xPoint1 = this.getChildLayer(1);
        //var xPoint2 = this.getChildLayer(2);

        webgis.complementProjected(this.options.sketch.map.calcCrs(), this.options.vertices);

        var r = {
            x: this.options.vertices[1].X - this.options.vertices[0].X, y: this.options.vertices[1].Y - this.options.vertices[0].Y
        };
        var len = Math.sqrt(r.x * r.x + r.y * r.y);
        if (len < 1e-2) {
            return;
        }

        var extend = this.options.sketch.map.getProjectedExtent(), w = extend[2] - extend[0], h = extend[3] - extend[1];
        var diagLen = Math.sqrt(w * w + h * h), from = 0, to = 1;

        if (diagLen > len) {
            from = -Math.min(100, parseInt(diagLen / len) + 1);
            to = -from;
        }

        //console.log('from-to', from, to, diagLen, len);

        var vertices = [];
        for (var t = from; t <= to; t++) {
            vertices.push({ X: this.options.vertices[0].X + r.x * t, Y: this.options.vertices[0].Y + r.y * t });
        }

        webgis.complementWGS84(this.options.sketch.map.calcCrs(), vertices);

        var latLngs = [];
        for (var v in vertices) {
            latLngs.push({ lng: vertices[v].x, lat: vertices[v].y });
        }

        if (!polyline) {
            polyline = L.polyline(latLngs, this.options);
            this.addChildLayer(polyline);
        } else {
            polyline.setLatLngs(latLngs);
        }

        //if (!xPoint1) {
        //    xPoint1 = L.xPoint({ lng: this.options.vertices[0].x, lat: this.options.vertices[0].y }, { color: this.options.color, weight: this.options.weight });
        //    this.addChildLayer(xPoint1);
        //}
    }
});

L.directionLine = function (options) { 
    return new L.DirectionLine(null, options);
};

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

L.RotatableRectangle = L.LayerCollection.extend({
    options: {
        color: '#ff0000',
        weight: 2,
        fillColor: '#ffff00',
        fillOpacity: 0.2,
        width: 500,
        height: 300,
        rotation: 0,
        markerOptions: {
            draggable: true, 
            icon: L.icon({
                iconUrl: webgis.css.imgResource('rotate_26.png', 'markers'),
                iconSize: [26, 26],
                iconAnchor: [13, 13],
                popupAnchor: [-13, -13]
            })
        }
    },
    _center: null,
    _rotation: 0,
    _marker: null,

    _setLatLngs: function (latLngs) {
        if (latLngs && latLngs.length > 0) {
            this.setLatLng(latLngs[0]);
        }
    },

    setLatLng: function (latLng) {
        this._center = latLng;
        this.redraw();
    },

    setRotation: function (rotation) {
        this._rotation = rotation;
        this.redraw();
    },

    getLatLngs: function () {
        return [this._center];
    },

    onAdd: function () {
        this.redraw();
    },

    //onRemove: function () {
    //    this.removeAllChildLayers();
    //    if (this._marker) {
    //        this._map.removeLayer(this._marker);
    //        this._marker = null;
    //    }
    //},

    redraw: function () {
        this.removeAllChildLayers();
        if (!this._center || !this._map) return;

        // Calculate rectangle corners
        let rectCoords = this._calcRectangleCoords(this._center, this.options.width, this.options.height, this._rotation);

        // Draw rectangle
        let rectangle = L.polygon(rectCoords, {
            color: this.options.color,
            weight: this.options.weight,
            fillColor: this.options.fillColor,
            fillOpacity: this.options.fillOpacity
        });
        this.addChildLayer(rectangle);

        let northArrow = L.polyline(this._calcNorthArrowFromRectangle(rectCoords), {
            color: 'black',
            weight: 1
        }); 
        this.addChildLayer(northArrow);

        let hLine = L.polyline(this._calcHLineFromRectangle(rectCoords), {
            color: 'black',
            weight: 1,
            dashArray: '4,4'
        });
        this.addChildLayer(hLine);
        let vLine = L.polyline(this._calcVLineFromRectangle(rectCoords), {
            color: 'black',
            weight: 1,
            dashArray: '4,4'
        });
        this.addChildLayer(vLine);

        // Calculate marker position (top right corner)
        let markerPos = rectCoords[2]; // [top right]

        // Add draggable marker
        if (!this._marker) {
            this._marker = L.marker(markerPos, this.options.markerOptions);
            this._marker.on('drag', this._onMarkerDrag, this);
            this._marker.on('dragend', this._onMarkerDragEnd, this);
            //this._marker.addTo(this._map);
            this.addChildLayer(this._marker);
        } else {
            this._marker.setLatLng(markerPos);
        }
    },

    _calcRectangleCoords: function (center, width, height, rotation) {
        // Project center to map coordinates
        let p = [{ x: center.lng, y: center.lat }];
        let xProp = "x", yProp = "y";
        if (webgis.calc._canProject()) {
            p = webgis.calc._projectToCalcCrs(p);
            xProp = "X"; yProp = "Y";
        }
        let cx = p[0][xProp], cy = p[0][yProp];

        // Rectangle corners before rotation
        let corners = [
            { X: cx - width / 2, Y: cy - height / 2 }, // bottom left
            { X: cx - width / 2, Y: cy + height / 2 }, // top left
            { X: cx + width / 2, Y: cy + height / 2 }, // top right
            { X: cx + width / 2, Y: cy - height / 2 }, // bottom right
        ];

        // Apply rotation
        if (rotation && rotation !== 0) {
            const sinA = Math.sin(rotation * Math.PI / 180.0);
            const cosA = Math.cos(rotation * Math.PI / 180.0);
            for (let i = 0; i < corners.length; i++) {
                let dx = corners[i][xProp] - cx;
                let dy = corners[i][yProp] - cy;
                corners[i][xProp] = cx + (dx * cosA - dy * sinA);
                corners[i][yProp] = cy + (dx * sinA + dy * cosA);
            }
        }

        // Unproject if needed
        if (webgis.calc._canProject()) {
            corners = webgis.calc._unprojectFromCalcCrs(corners);
        }

        // Convert to LatLng
        return corners.map(c => L.latLng(c.y, c.x));
    },
    _calcNorthArrowFromRectangle: function (rectCoords) {
        return [
            L.latLng(rectCoords[0].lat, rectCoords[0].lng), // bottom left
            L.latLng((rectCoords[1].lat + rectCoords[2].lat) * 0.5, (rectCoords[1].lng + rectCoords[2].lng) * 0.5), // top
            L.latLng(rectCoords[3].lat, rectCoords[3].lng)  // bottom right
        ];
    },
    _calcHLineFromRectangle: function (rectCoords) {
        return [
            L.latLng((rectCoords[0].lat + rectCoords[1].lat) * 0.5, (rectCoords[0].lng + rectCoords[1].lng) * 0.5), // left
            L.latLng((rectCoords[2].lat + rectCoords[3].lat) * 0.5, (rectCoords[2].lng + rectCoords[3].lng) * 0.5)  // right
        ]
    },
    _calcVLineFromRectangle: function (rectCoords) {
        return [
            L.latLng((rectCoords[0].lat + rectCoords[3].lat) * 0.5, (rectCoords[0].lng + rectCoords[3].lng) * 0.5), // bottom
            L.latLng((rectCoords[1].lat + rectCoords[2].lat) * 0.5, (rectCoords[1].lng + rectCoords[2].lng) * 0.5)  // top
        ]
    },
            
    _onMarkerDrag: function (e) {
        if (!this._center) return;
        let markerLatLng = this._marker.getLatLng();

        var p = [
            { x: this._center.lng, y: this._center.lat },
            { x: markerLatLng.lng, y: markerLatLng.lat }
        ];
        let xProp = "x", yProp = "y";
        if (webgis.calc._canProject()) {
            p = webgis.calc._projectToCalcCrs(p);
            xProp = "X"; yProp = "Y";
        }
        
        // Calculate angle between center and marker
        let dx = p[1][xProp] - p[0][xProp];
        let dy = p[1][yProp] - p[0][yProp];
        let angle = Math.atan2(dy, dx) * 180.0 / Math.PI;
        let angleR = Math.atan2(this.options.height, this.options.width) * 180.0 / Math.PI;
        this._rotation = angle - angleR;

        let rectCoords = this._calcRectangleCoords(this._center, this.options.width, this.options.height, this._rotation);
        this.getChildLayer(0).setLatLngs(rectCoords); // Update rectangle
        this.getChildLayer(1).setLatLngs(this._calcNorthArrowFromRectangle(rectCoords)); // Update north arrow
        this.getChildLayer(2).setLatLngs(this._calcHLineFromRectangle(rectCoords)); // Update H line
        this.getChildLayer(3).setLatLngs(this._calcVLineFromRectangle(rectCoords)); // Update V line
    },

    _onMarkerDragEnd: function (e) {
        //console.log("onMarkerDragEnd", this._rotation);
        if (!this._center || !this._marker) return;
        let rectCoords = this._calcRectangleCoords(this._center, this.options.width, this.options.height, this._rotation);
        this._marker.setLatLng(rectCoords[2]); // Ensure marker is at top right corner

        // Optionally fire event or update state
        this.fireEvent('rotated', { sender: this, rotation: this._rotation });
    },

    toGeoJSON: function () {
        let geoJson = {
            geometry: {
                type: 'Polygon',
                coordinates: []
            }
        };
        let rectCoords = this._calcRectangleCoords(this._center, this.options.width, this.options.height, this._rotation);
        geoJson.geometry.coordinates.push(rectCoords.map(ll => [ll.lng, ll.lat]));
        return geoJson;
    }
});

L.rotatableRectangle = function (latLngs, options) {
    return new L.RotatableRectangle(latLngs, options);
};

L.GraphicsElementSeries = L.LayerCollection.extend({
    options: {
        type: 'rectangle',
        rotatable: true,
        rectangleOptions: {
            color: '#ff0000',
            weight: 2,
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