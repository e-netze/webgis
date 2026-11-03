// MapMarkup measure tool 'view_angle' (Sichtfeld):
//   vertex 0 => apex, vertex 1 => end of the axis
//   the cone side lines (dashed) are as long as the axis
//   arc for the total angle at 60%, arcs for the half angles at 80% of the axis length
// Calculation in the calc crs (webgis.calc), the server (print) uses the same geometry: ViewAngleElement.cs
L.ViewAngle = L.LayerCollection.extend({
    options: {
        color: '#000000',
        weight: 2,
        fontColor: 'black',
        fontSize: 14,
        angle: 30,           // degrees
        unit: 'deg',         // deg | gon | both
        labelLength: false,
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

    // the children (dashed side lines, labels) get their own styles => rebuild instead of passing the style to the children
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

    setAngle: function (angle) {
        angle = parseFloat(angle);
        if (isNaN(angle) || angle <= 0 || angle >= 360)
            return;
        this.options.angle = angle;
        this.rebuild();
    },
    getAngle: function () { return this.options.angle; },
    setUnit: function (unit) {
        this.options.unit = unit;
        this.rebuild();
    },
    getUnit: function () { return this.options.unit; },
    setLabelLength: function (doLabel) {
        this.options.labelLength = doLabel;
        this.rebuild();
    },
    getLabelLength: function () { return this.options.labelLength; },

    redraw: function () {
        this.rebuild();
    },
    rebuild: function () { 
        if (!this._map)
            return;

        this.trimChildren(0);

        if (!this._latLngs || this._latLngs.length < 2)
            return;

        var calc = L.ViewAngle.toCalc(this._latLngs.slice(0, 2));
        this._calcCrs = calc.crsId;

        var a = calc.points[0], b = calc.points[1];
        var dx = b.X - a.X, dy = b.Y - a.Y, len = Math.sqrt(dx * dx + dy * dy);
        if (len === 0)
            return;

        var theta = Math.atan2(dy, dx), half = this.options.angle / 2.0 * Math.PI / 180.0;
        var toLatLng = calc.toLatLng;
        var at = function (radius, direction) {
            return toLatLng(a.X + radius * Math.cos(direction), a.Y + radius * Math.sin(direction));
        };

        var lineOptions = { color: this.options.color, weight: this.options.weight, opacity: 1.0 };
        var dashedOptions = L.extend({}, lineOptions, { dashArray: (this.options.weight * 3) + ',' + (this.options.weight * 3) });

        this._addLine([this._latLngs[0], this._latLngs[1]], lineOptions);
        this._addLine([this._latLngs[0], at(len, theta + half)], dashedOptions);
        this._addLine([this._latLngs[0], at(len, theta - half)], dashedOptions);

        this._addLine(L.ViewAngle.arc(at, len * 0.6, theta - half, theta + half), lineOptions);
        this._addLine(L.ViewAngle.arc(at, len * 0.8, theta - half, theta), lineOptions);
        this._addLine(L.ViewAngle.arc(at, len * 0.8, theta, theta + half), lineOptions);

        var unit = this.options.unit;
        this._addLabel(at(len * 0.5, theta), L.ViewAngle.formatAngle(this.options.angle, unit));
        this._addLabel(at(len * 0.85, theta + half / 2.0), L.ViewAngle.formatAngle(this.options.angle / 2.0, unit));
        this._addLabel(at(len * 0.85, theta - half / 2.0), L.ViewAngle.formatAngle(this.options.angle / 2.0, unit));

        if (this.options.labelLength === true) {
            this._addLabel(this._latLngs[1], L.ViewAngle.formatNumber(len) + ' m', 'start');
        }
    },
    _addLine: function (latLngs, options) {
        var line = L.polyline(latLngs, options);
        this.addChildLayer(line);
        line.on('click', function (e) {
            this.fireEvent('click', e);
        }, this);
    },
    _addLabel: function (latLng, text, anchor) {
        this.addChildLayer(L.svgLabel(latLng, {
            text: text,
            fontSize: this.options.fontSize,
            fontColor: this.options.fontColor,
            anchor: anchor || 'middle',
            alignmentBaseline: 'central'
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
    }
});

L.viewAngle = function (latLngs, options) {
    return new L.ViewAngle(latLngs, options);
};

// shared helpers for the angle measure tools (view_angle, intersection_angle)

// latLngs => { points: [{X,Y}], crsId, toLatLng(X,Y) }
// without a calc crs a local equirectangular approximation (meters) is used
L.ViewAngle.toCalc = function (latLngs) {
    var coords = latLngs.map(function (l) { return { x: l.lng, y: l.lat }; });

    if (webgis.calc._canProject()) {
        var crsId = webgis.calc.getCalcCrsId(coords);
        var projected = webgis.calc._projectToCalcCrs(coords);
        var crs = webgis.calc._crs[crsId];
        if (crsId > 0 && crs && crs.projection) {
            return {
                crsId: crsId,
                points: projected.map(function (c) { return { X: c.X, Y: c.Y }; }),
                toLatLng: function (X, Y) { return crs.projection.unproject(L.point(X, Y)); }
            };
        }
    }

    var kx = 111320.0 * Math.cos(coords[0].y * Math.PI / 180.0), ky = 110540.0;
    return {
        crsId: 0,
        points: coords.map(function (c) { return { X: c.x * kx, Y: c.y * ky }; }),
        toLatLng: function (X, Y) { return L.latLng(Y / ky, X / kx); }
    };
};

// at(radius, direction) => latLng
L.ViewAngle.arc = function (at, radius, from, to) {
    var steps = Math.max(8, Math.ceil(Math.abs(to - from) * 180.0 / Math.PI / 3.0)), latLngs = [];
    for (var i = 0; i <= steps; i++) {
        latLngs.push(at(radius, from + (to - from) * i / steps));
    }
    return latLngs;
};

L.ViewAngle.formatNumber = function (value) {
    return (Math.round(value * 100.0) / 100.0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
};

// angles at the vertices of dimline/dimpolygon (calc crs points {X,Y}), same geometry as AngleElementHelper.VertexAngles (print)
//   closed (polygon) => interior angle 0-360° (orientation of the ring), otherwise only at the inner vertices:
//   angleType 'inner' => angle between the segments 0-180°, 'deflection' => change of direction 0-180° with side L/R
// returns [{ center, radius, start, delta, degrees, side, extension }] (radians, extension => dashed prolongation of the incoming segment)
L.ViewAngle.vertexAngles = function (points, closed, angleType) {
    var pts = points.slice(), result = [];
    if (closed && pts.length > 1 && pts[0].X === pts[pts.length - 1].X && pts[0].Y === pts[pts.length - 1].Y)
        pts.pop();

    var n = pts.length;
    if (n < 3)
        return result;

    var signedArea = 0;
    if (closed) {
        for (var k = 0; k < n; k++) {
            var p1 = pts[k], p2 = pts[(k + 1) % n];
            signedArea += p1.X * p2.Y - p2.X * p1.Y;
        }
        if (signedArea === 0)
            return result;
    }

    var normalize = function (a) {  // => (-PI, PI]
        while (a > Math.PI) a -= 2.0 * Math.PI;
        while (a <= -Math.PI) a += 2.0 * Math.PI;
        return a;
    };

    for (var i = closed ? 0 : 1; i < (closed ? n : n - 1); i++) {
        var c = pts[i], prev = pts[(i - 1 + n) % n], next = pts[(i + 1) % n];
        var lenPrev = Math.sqrt((prev.X - c.X) * (prev.X - c.X) + (prev.Y - c.Y) * (prev.Y - c.Y)),
            lenNext = Math.sqrt((next.X - c.X) * (next.X - c.X) + (next.Y - c.Y) * (next.Y - c.Y));
        if (lenPrev === 0 || lenNext === 0)
            continue;

        var aPrev = Math.atan2(prev.Y - c.Y, prev.X - c.X), aNext = Math.atan2(next.Y - c.Y, next.X - c.X);
        var angle = { center: c, radius: 0.2 * Math.min(lenPrev, lenNext), side: '', extension: false };

        if (closed) {
            // counter clockwise ring => interior from the direction to the next vertex counter clockwise to the direction to the previous vertex
            var from = signedArea > 0 ? aNext : aPrev, to = signedArea > 0 ? aPrev : aNext;
            var interior = to - from;
            while (interior < 0) interior += 2.0 * Math.PI;
            while (interior >= 2.0 * Math.PI) interior -= 2.0 * Math.PI;
            angle.start = from;
            angle.delta = interior;
        } else if (angleType === 'deflection') {
            var aIn = aPrev + Math.PI;
            angle.start = aIn;
            angle.delta = normalize(aNext - aIn);
            angle.extension = true;
            angle.side = Math.abs(angle.delta) < 1e-9 ? '' : (angle.delta > 0 ? 'L' : 'R');
        } else {
            angle.start = aNext;
            angle.delta = normalize(aPrev - aNext);
        }

        angle.degrees = Math.abs(angle.delta) * 180.0 / Math.PI;
        result.push(angle);
    }

    return result;
};

// draws the vertex angles (see vertexAngles) into a L.LayerCollection: arcs, extensions and labels
// angleUnit: none | deg | gon | both
L.ViewAngle.addVertexAngles = function (collection, latLngs, closed, angleUnit, angleType, options) {
    if (!angleUnit || angleUnit === 'none' || !latLngs || latLngs.length < 3)
        return;

    var calc = L.ViewAngle.toCalc(latLngs);
    var lineOptions = { color: options.color, weight: options.weight, opacity: 1.0, fill: false };
    var dashedOptions = L.extend({}, lineOptions, { dashArray: (options.weight * 3) + ',' + (options.weight * 3) });

    L.ViewAngle.vertexAngles(calc.points, closed, angleType).forEach(function (angle) {
        var at = function (radius, direction) {
            return calc.toLatLng(angle.center.X + radius * Math.cos(direction), angle.center.Y + radius * Math.sin(direction));
        };

        if (angle.extension === true)
            collection.addChildLayer(L.polyline([at(0, angle.start), at(angle.radius * 1.5, angle.start)], dashedOptions));
        if (angle.delta !== 0)
            collection.addChildLayer(L.polyline(L.ViewAngle.arc(at, angle.radius, angle.start, angle.start + angle.delta), lineOptions));

        collection.addChildLayer(L.svgLabel(at(angle.radius * 1.4, angle.start + angle.delta / 2.0), {
            text: L.ViewAngle.formatAngle(angle.degrees, angleUnit) + (angle.side ? ' ' + angle.side : ''),
            fontSize: options.fontSize,
            fontColor: options.fontColor,
            anchor: 'middle',
            alignmentBaseline: 'central'
        }));
    });
};

// degrees => text in the given unit (deg | gon | both)
L.ViewAngle.formatAngle = function (degrees, unit) {
    var deg = L.ViewAngle.formatNumber(degrees) + '°',
        gon = L.ViewAngle.formatNumber(degrees * 10.0 / 9.0) + ' gon';

    switch (unit) {
        case 'gon': return gon;
        case 'both': return deg + ' / ' + gon;
        default: return deg;
    }
};
