// MapMarkup measure tool 'circle_arc' (Kreisbogen):
//   vertices 0,1,2 => start, intermediate point and end of a circular arc (the arc runs through all three points)
//   solid arc, dashed radius lines center => start/end, dashed radius line center => arc midpoint (radius label), dashed center cross
//   labels: radius (on the radius line to the arc midpoint), arc length (outside the arc midpoint), central angle (near the center)
//   collinear points => only the line and a note
// Uses the shared helpers of webgis-graphics.view_angle.js and webgis-graphics.perpendicular.js (load order!)
// Calculation in the calc crs (webgis.calc), the server (print, export) uses the same geometry: CircleArcCalculator.cs, CircleArcElement.cs
L.CircleArc = L.LayerCollection.extend({
    options: {
        color: '#000000',
        weight: 2,
        fontColor: 'black',
        fontSize: 14,
        unit: 'both',         // deg | gon | both
        labels: 'all',        // all | radius | length | angle
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

    // the children (dashed lines, labels) get their own styles => rebuild instead of passing the style to the children
    setStyle: function (style) {
        L.setOptions(this, style);
        if (this._setStyle) this._setStyle(style);
        this.rebuild();
        return this;
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

    setUnit: function (unit) {
        this.options.unit = unit;
        this.rebuild();
    },
    getUnit: function () { return this.options.unit; },
    setLabels: function (labels) {
        this.options.labels = labels;
        this.rebuild();
    },
    getLabels: function () { return this.options.labels; },

    // { calc, arc } or null (less than 3 vertices); arc is null if the points are collinear
    _calculate: function () {
        if (!this._latLngs || this._latLngs.length < 3)
            return null;

        var calc = L.ViewAngle.toCalc(this._latLngs.slice(0, 3));
        this._calcCrs = calc.crsId;

        return { calc: calc, arc: L.CircleArc.calc(calc.points) };
    },

    // measured values (export): { radius, length, angle (degrees) } or null
    getMeasures: function () {
        var result = this._calculate();
        if (!result || !result.arc)
            return null;

        return {
            radius: result.arc.radius,
            length: result.arc.length,
            angle: result.arc.angle
        };
    },

    redraw: function () {
        this.rebuild();
    },
    rebuild: function () {
        if (!this._map)
            return;

        this.trimChildren(0);

        if (!this._latLngs || this._latLngs.length < 2)
            return;

        var lineOptions = { color: this.options.color, weight: this.options.weight, opacity: 1.0 };
        var dashedOptions = L.extend({}, lineOptions, { dashArray: (this.options.weight * 3) + ',' + (this.options.weight * 3) });

        var result = this._calculate();
        if (!result) {
            this._addLine([this._latLngs[0], this._latLngs[1]], lineOptions);
            return;
        }

        var p = result.calc.points, toLatLng = result.calc.toLatLng, arc = result.arc;
        if (!arc) {
            this._addLine(this._latLngs.slice(0, 3), lineOptions);
            this._addLabel(toLatLng((p[0].X + p[1].X + p[2].X) / 3.0, (p[0].Y + p[1].Y + p[2].Y) / 3.0), webgis.l10n.get('mapmarkup-circle-arc-collinear'));
            return;
        }

        var c = arc.center, r = arc.radius;
        var at = function (radius, direction) {
            return toLatLng(c.X + radius * Math.cos(direction), c.Y + radius * Math.sin(direction));
        };
        var centerLatLng = toLatLng(c.X, c.Y), midDirection = arc.start + arc.sweep / 2.0, midLatLng = at(r, midDirection);

        // arc
        this._addLine(L.ViewAngle.arc(at, r, arc.start, arc.start + arc.sweep), lineOptions);

        // radius lines, center cross
        this._addLine([centerLatLng, at(r, arc.start)], dashedOptions);
        this._addLine([centerLatLng, at(r, arc.start + arc.sweep)], dashedOptions);
        this._addLine([centerLatLng, midLatLng], dashedOptions);
        var cross = r * L.CircleArc.crossFactor;
        this._addLine([toLatLng(c.X - cross, c.Y), toLatLng(c.X + cross, c.Y)], dashedOptions);
        this._addLine([toLatLng(c.X, c.Y - cross), toLatLng(c.X, c.Y + cross)], dashedOptions);

        var labels = this.options.labels || 'all';
        var dirX = Math.cos(midDirection), dirY = Math.sin(midDirection);

        if (labels === 'all' || labels === 'radius') {
            this._addParallelLabel(at(r * L.CircleArc.radiusLabelFactor, midDirection), dirX, dirY,
                'R = ' + L.CircleArc.formatLength(r), true);
        }
        if (labels === 'all' || labels === 'length') {
            // tangent at the arc midpoint, text outside of the arc (the center is below the text)
            var tX = -dirY, tY = dirX;
            this._addParallelLabel(midLatLng, tX, tY,
                'b = ' + L.CircleArc.formatLength(arc.length),
                L.Perpendicular.isLeftOfText(tX, tY, -dirX, -dirY) !== true);
        }
        if (labels === 'all' || labels === 'angle') {
            this._addLabel(at(r * L.CircleArc.angleLabelFactor, midDirection), 'α = ' + L.ViewAngle.formatAngle(arc.angle, this.options.unit));
        }
    },
    _addLine: function (latLngs, options) {
        var line = L.polyline(latLngs, options);
        this.addChildLayer(line);
        line.on('click', function (e) {
            this.fireEvent('click', e);
        }, this);
    },
    _addLabel: function (latLng, text) {
        this.addChildLayer(L.svgLabel(latLng, {
            text: text,
            fontSize: this.options.fontSize,
            fontColor: this.options.fontColor,
            anchor: 'middle',
            alignmentBaseline: 'central'
        }));
    },
    // label parallel to the direction (dx, dy) (calc crs), above => text above the direction, otherwise below
    _addParallelLabel: function (latLng, dx, dy, text, above) {
        this.addChildLayer(L.svgLabel(latLng, {
            text: text,
            rotation: -L.Perpendicular.textAngle(dx, dy),
            fontSize: this.options.fontSize,
            fontColor: this.options.fontColor,
            anchor: 'middle',
            alignmentBaseline: above ? 'ideographic' : 'hanging'
        }));
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
    },
    // highlight (e.g. hovering the graphics list): the arc itself
    toPreviewGeoJSON: function () {
        var result = this._calculate();
        if (!result || !result.arc)
            return this.toGeoJSON();

        var c = result.arc.center, toLatLng = result.calc.toLatLng;
        var latLngs = L.ViewAngle.arc(function (radius, direction) {
            return toLatLng(c.X + radius * Math.cos(direction), c.Y + radius * Math.sin(direction));
        }, result.arc.radius, result.arc.start, result.arc.start + result.arc.sweep);

        return {
            geometry: {
                type: 'LineString',
                coordinates: latLngs.map(function (l) { return [l.lng, l.lat]; })
            }
        };
    }
});

L.circleArc = function (latLngs, options) {
    return new L.CircleArc(latLngs, options);
};

L.CircleArc.crossFactor = 0.1;          // half size of the center cross (* radius)
L.CircleArc.radiusLabelFactor = 0.6;    // radius label on the radius line to the arc midpoint (* radius)
L.CircleArc.angleLabelFactor = 0.25;    // central angle label near the center (* radius)

// three points (calc crs) => { center, radius, start, sweep (radians, signed), length, angle (degrees) }
// or null if the points are collinear (or identical)
L.CircleArc.calc = function (p) {
    var ax = p[1].X - p[0].X, ay = p[1].Y - p[0].Y,
        bx = p[2].X - p[1].X, by = p[2].Y - p[1].Y;
    var lenA = Math.sqrt(ax * ax + ay * ay), lenB = Math.sqrt(bx * bx + by * by);
    var cross = ax * by - ay * bx;
    if (lenA === 0 || lenB === 0 || Math.abs(cross) / (lenA * lenB) < 1e-9)
        return null;

    var d = 2.0 * (p[0].X * (p[1].Y - p[2].Y) + p[1].X * (p[2].Y - p[0].Y) + p[2].X * (p[0].Y - p[1].Y));
    var s0 = p[0].X * p[0].X + p[0].Y * p[0].Y, s1 = p[1].X * p[1].X + p[1].Y * p[1].Y, s2 = p[2].X * p[2].X + p[2].Y * p[2].Y;
    var center = {
        X: (s0 * (p[1].Y - p[2].Y) + s1 * (p[2].Y - p[0].Y) + s2 * (p[0].Y - p[1].Y)) / d,
        Y: (s0 * (p[2].X - p[1].X) + s1 * (p[0].X - p[2].X) + s2 * (p[1].X - p[0].X)) / d
    };
    var radius = Math.sqrt((p[0].X - center.X) * (p[0].X - center.X) + (p[0].Y - center.Y) * (p[0].Y - center.Y));

    var start = Math.atan2(p[0].Y - center.Y, p[0].X - center.X), end = Math.atan2(p[2].Y - center.Y, p[2].X - center.X);
    var sweep;
    if (cross > 0) {  // counterclockwise
        sweep = end - start;
        while (sweep <= 0) sweep += 2.0 * Math.PI;
    } else {          // clockwise
        sweep = end - start;
        while (sweep >= 0) sweep -= 2.0 * Math.PI;
    }

    return {
        center: center,
        radius: radius,
        start: start,
        sweep: sweep,
        length: radius * Math.abs(sweep),
        angle: Math.abs(sweep) * 180.0 / Math.PI
    };
};

L.CircleArc.formatLength = function (meters) {
    return L.ViewAngle.formatNumber(meters) + ' m';
};
