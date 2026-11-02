// MapMarkup measure tool 'azimuth' (Richtungswinkel), polyline/traverse:
//   for each segment the azimuth is measured clockwise from (grid) north of the calc crs
//   dashed north line at the segment start (25% of the segment length), arc from north to the segment at 15%
//   optional segment length at the segment midpoint, parallel to the segment (m | km)
// Calculation in the calc crs (webgis.calc), the server (print) uses the same geometry: AzimuthElement.cs
// requires the shared helpers of webgis-graphics.view_angle.js (L.ViewAngle.*)
L.Azimuth = L.LayerCollection.extend({
    options: {
        color: '#000000',
        weight: 2,
        fontColor: 'black',
        fontSize: 14,
        unit: 'deg',         // deg | gon | both
        labelLength: true,
        lengthUnit: 'm',     // m | km
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

    // the children (dashed north line, labels) get their own styles => rebuild instead of passing the style to the children
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
    setLabelLength: function (doLabel) {
        this.options.labelLength = doLabel;
        this.rebuild();
    },
    getLabelLength: function () { return this.options.labelLength; },
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

        if (!this._latLngs || this._latLngs.length < 2)
            return;

        var calc = L.ViewAngle.toCalc(this._latLngs);
        this._calcCrs = calc.crsId;

        var lineOptions = { color: this.options.color, weight: this.options.weight, opacity: 1.0 };
        var dashedOptions = L.extend({}, lineOptions, { dashArray: (this.options.weight * 3) + ',' + (this.options.weight * 3) });

        this._addLine(this._latLngs, lineOptions);

        var north = Math.PI / 2.0, toLatLng = calc.toLatLng;
        for (var i = 1; i < calc.points.length; i++) {
            var a = calc.points[i - 1], b = calc.points[i];
            var dx = b.X - a.X, dy = b.Y - a.Y, len = Math.sqrt(dx * dx + dy * dy);
            if (len === 0)
                continue;

            var azimuth = L.Azimuth.azimuth(dx, dy), azimuthRad = azimuth * Math.PI / 180.0;
            var at = (function (a) {
                return function (radius, direction) {
                    return toLatLng(a.X + radius * Math.cos(direction), a.Y + radius * Math.sin(direction));
                };
            })(a);

            this._addLine([this._latLngs[i - 1], at(len * 0.25, north)], dashedOptions);
            this._addLine(L.ViewAngle.arc(at, len * 0.15, north - azimuthRad, north), lineOptions);

            this._addLabel(at(len * 0.22, north - azimuthRad / 2.0), L.ViewAngle.formatAngle(azimuth, this.options.unit));

            if (this.options.labelLength === true) {
                this._addLengthLabel(this._latLngs[i - 1], this._latLngs[i], dx, dy, len);
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
    // segment length parallel to the segment (above the line), like dimline
    _addLengthLabel: function (from, to, dx, dy, len) {
        var angle = Math.atan2(dy, dx) * 180.0 / Math.PI;
        if (angle < 0)
            angle += 360.0;
        if (angle > 90.0 && angle < 270.0)
            angle += 180.0;

        var km = this.options.lengthUnit === 'km';
        this.addChildLayer(L.svgLabel(L.latLng((from.lat + to.lat) * 0.5, (from.lng + to.lng) * 0.5), {
            text: L.ViewAngle.formatNumber(km ? len / 1000.0 : len) + (km ? 'km' : 'm'),
            rotation: -angle,
            fontSize: this.options.fontSize,
            fontColor: this.options.fontColor,
            alignmentBaseline: 'ideographic'
        }));
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

L.azimuth = function (latLngs, options) {
    return new L.Azimuth(latLngs, options);
};

// calc crs delta => azimuth in degrees [0, 360), clockwise from north
L.Azimuth.azimuth = function (dx, dy) {
    var azimuth = (90.0 - Math.atan2(dy, dx) * 180.0 / Math.PI) % 360.0;
    return azimuth < 0 ? azimuth + 360.0 : azimuth;
};
