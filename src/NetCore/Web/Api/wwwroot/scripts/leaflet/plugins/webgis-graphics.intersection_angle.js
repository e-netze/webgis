// MapMarkup measure tool 'intersection_angle' (Schnittwinkel):
//   vertices 0,1 => line 1, vertices 2,3 => line 2
//   angle between the direction vectors (0-180°) at the intersection of the (extended) lines,
//   optional the adjacent angle (180° - angle), both or all (additionally mirrored at the intersection);
//   dashed extensions if the intersection is outside of a segment
//   arc radius: 30% of the shorter of the two distances "intersection => farthest endpoint of the line"
// Uses the shared helpers of webgis-graphics.view_angle.js (load order!)
// Calculation in the calc crs (webgis.calc), the server (print) uses the same geometry: IntersectionAngleElement.cs
L.IntersectionAngle = L.LayerCollection.extend({
    options: {
        color: '#000000',
        weight: 2,
        fontColor: 'black',
        fontSize: 14,
        unit: 'both',         // deg | gon | both
        angles: 'angle',      // angle | adjacent | both | all
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

    // the children (dashed extensions, labels) get their own styles => rebuild instead of passing the style to the children
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
    setAngles: function (angles) {
        this.options.angles = angles;
        this.rebuild();
    },
    getAngles: function () { return this.options.angles; },

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

        this._addLine([this._latLngs[0], this._latLngs[1]], lineOptions);
        if (this._latLngs.length < 4)
            return;
        this._addLine([this._latLngs[2], this._latLngs[3]], lineOptions);

        var calc = L.ViewAngle.toCalc(this._latLngs.slice(0, 4));
        this._calcCrs = calc.crsId;

        var p = calc.points, toLatLng = calc.toLatLng;
        var d1 = { X: p[1].X - p[0].X, Y: p[1].Y - p[0].Y }, d2 = { X: p[3].X - p[2].X, Y: p[3].Y - p[2].Y };
        var len1 = Math.sqrt(d1.X * d1.X + d1.Y * d1.Y), len2 = Math.sqrt(d2.X * d2.X + d2.Y * d2.Y);
        if (len1 === 0 || len2 === 0)
            return;

        var cross = d1.X * d2.Y - d1.Y * d2.X;
        if (Math.abs(cross) / (len1 * len2) < 1e-9) {
            var mid = toLatLng((p[0].X + p[1].X + p[2].X + p[3].X) / 4.0, (p[0].Y + p[1].Y + p[2].Y + p[3].Y) / 4.0);
            this._addLabel(mid, webgis.l10n.get('mapmarkup-intersection-angle-parallel'));
            return;
        }

        var w = { X: p[2].X - p[0].X, Y: p[2].Y - p[0].Y };
        var t = (w.X * d2.Y - w.Y * d2.X) / cross, u = (w.X * d1.Y - w.Y * d1.X) / cross;
        var s = { X: p[0].X + t * d1.X, Y: p[0].Y + t * d1.Y }, sLatLng = toLatLng(s.X, s.Y);

        // extensions
        if (t < 0) this._addLine([sLatLng, this._latLngs[0]], dashedOptions);
        else if (t > 1) this._addLine([this._latLngs[1], sLatLng], dashedOptions);
        if (u < 0) this._addLine([sLatLng, this._latLngs[2]], dashedOptions);
        else if (u > 1) this._addLine([this._latLngs[3], sLatLng], dashedOptions);

        var dist = function (pt) { return Math.sqrt((pt.X - s.X) * (pt.X - s.X) + (pt.Y - s.Y) * (pt.Y - s.Y)); };
        var radius = 0.3 * Math.min(Math.max(dist(p[0]), dist(p[1])), Math.max(dist(p[2]), dist(p[3])));

        var a1 = Math.atan2(d1.Y, d1.X), a2 = Math.atan2(d2.Y, d2.X);
        var normalize = function (delta) {
            while (delta > Math.PI) delta -= 2.0 * Math.PI;
            while (delta <= -Math.PI) delta += 2.0 * Math.PI;
            return delta;
        };
        var deltaAngle = normalize(a2 - a1), deltaAdjacent = normalize(a2 + Math.PI - a1);

        var at = function (r, direction) {
            return toLatLng(s.X + r * Math.cos(direction), s.Y + r * Math.sin(direction));
        };

        var sectors = L.IntersectionAngle.sectors(this.options.angles, a1, deltaAngle, deltaAdjacent);
        for (var i = 0; i < sectors.length; i++) {
            var start = sectors[i].start, delta = sectors[i].delta;
            if (radius > 0) {
                this._addLine(L.ViewAngle.arc(at, radius, start, start + delta), lineOptions);
            }
            this._addLabel(at(radius > 0 ? radius * 1.4 : len1 * 0.1, start + delta / 2.0), L.ViewAngle.formatAngle(Math.abs(delta) * 180.0 / Math.PI, this.options.unit));
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
    // highlight (e.g. hovering the graphics list): both lines as separate parts (not connected)
    toPreviewGeoJSON: function () {
        var geoJson = this.toGeoJSON();
        if (geoJson.geometry.coordinates.length > 2) {
            geoJson.geometry.partindex = [2];
        }
        return geoJson;
    }
});

L.intersectionAngle = function (latLngs, options) {
    return new L.IntersectionAngle(latLngs, options);
};

// angle: a1 => a2, adjacent: a1 => a2 + 180°, all: additionally mirrored at the intersection (vertical angles)
L.IntersectionAngle.sectors = function (angles, a1, deltaAngle, deltaAdjacent) {
    var sectors = [];
    if (angles !== 'adjacent') sectors.push({ start: a1, delta: deltaAngle });
    if (angles === 'adjacent' || angles === 'both' || angles === 'all') sectors.push({ start: a1, delta: deltaAdjacent });
    if (angles === 'all') {
        sectors.push({ start: a1 + Math.PI, delta: deltaAngle });
        sectors.push({ start: a1 + Math.PI, delta: deltaAdjacent });
    }
    return sectors;
};
