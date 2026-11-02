// MapMarkup measure tool 'perpendicular' (Normalabstand):
//   latLngs[0 .. lineVertexCount-1] => reference line (polyline)
//   latLngs[lineVertexCount ..]     => points, for each point the perpendicular onto the reference line is drawn
//   the perpendicular is labeled with its length (parallel to the perpendicular), optional with the station of the foot point (m or km+m)
//   if the foot point is outside of the reference line, the line is extended (dashed)
// Calculation in the calc crs (webgis.calc), the server (print) uses the same geometry: PerpendicularElement.cs
// requires the shared helpers of webgis-graphics.view_angle.js (L.ViewAngle.*)
L.Perpendicular = L.LayerCollection.extend({
    options: {
        color: '#000000',
        weight: 2,
        fontColor: 'black',
        fontSize: 14,
        stationFormat: 'm',  // none | m (156,24m) | km (0+156,24)
        lengthUnit: 'm',     // m | km
        attributes: {}
    },

    _calcCrs: 0,
    _latLngs: null,
    _lineVertexCount: null,  // null => all vertices belong to the reference line (still drawing the line)
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

    // the children (dashed extensions, labels, ...) get their own styles => rebuild instead of passing the style to the children
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

    setLineVertexCount: function (count) {
        this._lineVertexCount = (count === null || count === undefined) ? null : count;
        this.rebuild();
    },
    getLineVertexCount: function () {
        var count = this._latLngs ? this._latLngs.length : 0;
        return this._lineVertexCount === null ? count : Math.min(this._lineVertexCount, count);
    },

    setStationFormat: function (format) {
        this.options.stationFormat = format;
        this.rebuild();
    },
    getStationFormat: function () { return this.options.stationFormat; },
    setLengthUnit: function (unit) {
        this.options.lengthUnit = unit;
        this.rebuild();
    },
    getLengthUnit: function () { return this.options.lengthUnit; },

    redraw: function () {
        this.rebuild();
    },
    rebuild: function () {
        if (!this._map)
            return;

        this.trimChildren(0);

        if (!this._latLngs || this._latLngs.length === 0)
            return;

        var lineCount = this.getLineVertexCount();
        var lineOptions = { color: this.options.color, weight: this.options.weight, opacity: 1.0 };
        var dashedOptions = L.extend({}, lineOptions, { dashArray: (this.options.weight * 3) + ',' + (this.options.weight * 3) });

        if (lineCount >= 2) {
            this._addLine(this._latLngs.slice(0, lineCount), lineOptions);
        }

        if (this._latLngs.length <= lineCount)
            return;

        var calc = L.ViewAngle.toCalc(this._latLngs);
        this._calcCrs = calc.crsId;

        var linePoints = calc.points.slice(0, lineCount), toLatLng = calc.toLatLng;
        for (var p = lineCount; p < calc.points.length; p++) {
            var point = calc.points[p];
            this._addPointMarker(this._latLngs[p]);

            var foot = L.Perpendicular.foot(linePoints, point);
            if (!foot)
                continue;

            if (foot.extension) {
                this._addLine([toLatLng(foot.extension.X, foot.extension.Y), toLatLng(foot.X, foot.Y)], dashedOptions);
            }

            if (foot.dist > 0) {
                var footLatLng = toLatLng(foot.X, foot.Y);
                this._addLine([this._latLngs[p], footLatLng], lineOptions);

                var s = Math.min(foot.dist, foot.segLength) * 0.1,
                    vx = (point.X - foot.X) / foot.dist, vy = (point.Y - foot.Y) / foot.dist;
                this._addLine([
                    toLatLng(foot.X + foot.ux * s, foot.Y + foot.uy * s),
                    toLatLng(foot.X + foot.ux * s + vx * s, foot.Y + foot.uy * s + vy * s),
                    toLatLng(foot.X + vx * s, foot.Y + vy * s)], L.extend({}, lineOptions, { weight: Math.max(1, this.options.weight / 2) }));

                this._addParallelLabel(footLatLng, this._latLngs[p], point.X - foot.X, point.Y - foot.Y,
                    L.Perpendicular.formatLength(foot.dist, this.options.lengthUnit), true);
            }

            var stationText = L.Perpendicular.formatStation(foot.station, this.options.stationFormat);
            if (stationText) {
                this._addParallelLabel(toLatLng(foot.X, foot.Y), null, foot.ux, foot.uy,
                    stationText,
                    L.Perpendicular.isLeftOfText(foot.ux, foot.uy, point.X - foot.X, point.Y - foot.Y) !== true);
            }
        }
    },
    _addLine: function (latLngs, options) {
        var line = L.polyline(latLngs, options);
        this.addChildLayer(line);
        line.on('click', function (e) {
            this.fireEvent('click', e);
        }, this);
    },
    _addPointMarker: function (latLng) {
        var marker = L.circleMarker(latLng, {
            radius: Math.max(3, this.options.weight + 1),
            color: this.options.color,
            weight: 1,
            fillColor: this.options.color,
            fillOpacity: 1.0
        });
        this.addChildLayer(marker);
        marker.on('click', function (e) {
            this.fireEvent('click', e);
        }, this);
    },
    // label parallel to the direction (dx, dy) (calc crs), at the midpoint of from-to (or at from, if to is null)
    // above => text above the direction (like dimline), otherwise below
    _addParallelLabel: function (from, to, dx, dy, text, above) {
        var angle = L.Perpendicular.textAngle(dx, dy);
        var latLng = to ? L.latLng((from.lat + to.lat) * 0.5, (from.lng + to.lng) * 0.5) : from;

        this.addChildLayer(L.svgLabel(latLng, {
            text: text,
            rotation: -angle,
            fontSize: this.options.fontSize,
            fontColor: this.options.fontColor,
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
    }
});

L.perpendicular = function (latLngs, options) {
    return new L.Perpendicular(latLngs, options);
};

// linePoints, point: calc crs {X,Y}
// => { X, Y (foot), dist, station, ux, uy (unit direction of the segment), segLength, extension: {X,Y} (line end, if the foot is outside) } or null
// the first/last segment is extended, if the foot is before the start/after the end of the line
L.Perpendicular.foot = function (linePoints, point) {
    if (!linePoints || linePoints.length < 2)
        return null;

    var best = null, station = 0, last = linePoints.length - 2;
    for (var i = 0; i <= last; i++) {
        var a = linePoints[i], b = linePoints[i + 1];
        var dx = b.X - a.X, dy = b.Y - a.Y, len = Math.sqrt(dx * dx + dy * dy);
        if (len > 0) {
            var t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (len * len);
            if ((t >= 0 && t <= 1) || (i === 0 && t < 0) || (i === last && t > 1)) {
                var X = a.X + t * dx, Y = a.Y + t * dy;
                var dist = Math.sqrt((point.X - X) * (point.X - X) + (point.Y - Y) * (point.Y - Y));
                if (best === null || dist < best.dist) {
                    best = {
                        X: X, Y: Y, dist: dist, station: station + t * len,
                        ux: dx / len, uy: dy / len, segLength: len,
                        extension: t < 0 ? { X: a.X, Y: a.Y } : (t > 1 ? { X: b.X, Y: b.Y } : null)
                    };
                }
            }
        }
        station += len;
    }

    // no valid foot on a segment (point is "behind" a vertex) => nearest vertex
    station = 0;
    for (var v = 0; v < linePoints.length; v++) {
        if (v > 0) {
            var sx = linePoints[v].X - linePoints[v - 1].X, sy = linePoints[v].Y - linePoints[v - 1].Y;
            station += Math.sqrt(sx * sx + sy * sy);
        }
        var vertex = linePoints[v], vDist = Math.sqrt((point.X - vertex.X) * (point.X - vertex.X) + (point.Y - vertex.Y) * (point.Y - vertex.Y));
        if (best === null || vDist < best.dist - 1e-9) {
            var n = linePoints[Math.min(v + 1, linePoints.length - 1)], m = linePoints[Math.max(v - 1, 0)];
            var ddx = n.X - m.X, ddy = n.Y - m.Y, dlen = Math.sqrt(ddx * ddx + ddy * ddy) || 1;
            best = { X: vertex.X, Y: vertex.Y, dist: vDist, station: station, ux: ddx / dlen, uy: ddy / dlen, segLength: dlen, extension: null };
        }
    }

    return best;
};

// direction (calc crs) => text angle in degrees (counter clockwise), always readable (not upside down)
L.Perpendicular.textAngle = function (dx, dy) {
    var angle = Math.atan2(dy, dx) * 180.0 / Math.PI;
    if (angle < 0)
        angle += 360.0;
    if (angle > 90.0 && angle < 270.0)
        angle += 180.0;
    return angle;
};

// is the vector (px, py) above (left of) the text written in direction (dx, dy)
L.Perpendicular.isLeftOfText = function (dx, dy, px, py) {
    var angle = L.Perpendicular.textAngle(dx, dy) * Math.PI / 180.0;
    return (-Math.sin(angle) * px + Math.cos(angle) * py) > 0;
};

L.Perpendicular.formatLength = function (meters, unit) {
    return unit === 'km'
        ? L.ViewAngle.formatNumber(meters / 1000.0) + 'km'
        : L.ViewAngle.formatNumber(meters) + 'm';
};

// format: none => null, m => 145,20m, km => 0+145,20 (km+m); negative before the start of the line
L.Perpendicular.formatStation = function (meters, format) {
    if (format === 'none')
        return null;
    if (format !== 'km')
        return L.ViewAngle.formatNumber(meters) + 'm';

    var value = Math.round(Math.abs(meters) * 100.0) / 100.0;
    var km = Math.floor(value / 1000.0), m = value - km * 1000.0;
    return (meters < 0 && value > 0 ? '-' : '') + km + '+' +
        m.toLocaleString(undefined, { minimumIntegerDigits: 3, minimumFractionDigits: 2, maximumFractionDigits: 2, useGrouping: false });
};
