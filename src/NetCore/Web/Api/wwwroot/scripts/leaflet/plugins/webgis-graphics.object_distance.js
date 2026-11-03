// MapMarkup measure tool 'object_distance' (Objektabstand):
//   shortest connection from object A to any number of objects B (features of the map layers), calculated by the server ('object-distance-calc')
//   data: { a: { ref, layer, oid, geometry }, lines: [{ ref, layer, oid, length, touching, geometry, from: latLng, to: latLng }] }
//     ref: "<theme-id>|<oid>", geometry: GeoJSON (WGS84) of the feature, only for highlighting while editing (not stored)
//   each line is drawn like a dimline (end markers) and labeled with its length (parallel to the line), optional with its number
//   touching/intersecting objects (length 0) => no line
//   highlightSources: the geometries of A and B are highlighted (sketch only, not the committed element => not printed/exported)
// requires the shared helpers of webgis-graphics.view_angle.js (L.ViewAngle.*) and webgis-graphics.perpendicular.js (L.Perpendicular.*)
L.ObjectDistance = L.LayerCollection.extend({
    options: {
        color: '#000000',
        weight: 2,
        fontColor: 'black',
        fontSize: 14,
        lengthUnit: 'm',     // m | km
        showNumber: false,
        highlightSources: false,
        attributes: {}
    },

    _calcCrs: 0,
    _a: null,
    _lines: null,
    _setLatLngs: function (latLngs) { },

    _setStyle: function (style) {
        for (var i in style) {
            switch (i.toLocaleLowerCase()) {
                case 'font-size':
                case 'fontsize':
                case 'size':
                    this.options.fontSize = style[i];
                    break;
                case 'fill':
                case 'font-color':
                    this.options.fontColor = style[i];
                    break;
            }
        }
    },

    // the children (markers, labels, highlights) get their own styles => rebuild instead of passing the style to the children
    setStyle: function (style) {
        L.setOptions(this, style);
        if (this._setStyle) this._setStyle(style);
        this.rebuild();
        return this;
    },

    onAdd: function () {
        this.rebuild();
    },

    setData: function (data) {
        this._a = data && data.a ? data.a : null;
        this._lines = data && data.lines ? data.lines : [];
        this.rebuild();
    },
    getData: function () {
        return {
            a: this._a,
            lines: (this._lines || []).slice()
        };
    },
    getLines: function () { return this._lines || []; },

    // all end points of the lines => bounds, ...
    getLatLngs: function () {
        var latLngs = [];
        for (var line of this.getLines()) {
            latLngs.push(line.from, line.to);
        }
        return latLngs;
    },
    getCalcCrs: function () { return this._calcCrs; },

    setLengthUnit: function (unit) {
        this.options.lengthUnit = unit;
        this.rebuild();
    },
    getLengthUnit: function () { return this.options.lengthUnit; },
    setShowNumber: function (showNumber) {
        this.options.showNumber = showNumber === true;
        this.rebuild();
    },
    getShowNumber: function () { return this.options.showNumber === true; },
    setHighlightSources: function (highlight) {
        this.options.highlightSources = highlight === true;
        this.rebuild();
    },

    redraw: function () {
        this.rebuild();
    },
    rebuild: function () {
        if (!this._map)
            return;

        this.trimChildren(0);

        if (this.options.highlightSources === true) {
            if (this._a && this._a.geometry) {
                this._addHighlight(this._a.geometry, L.ObjectDistance.highlightColorA);
            }
            for (var line of this.getLines()) {
                if (line.geometry) {
                    this._addHighlight(line.geometry, L.ObjectDistance.highlightColorB);
                }
            }
        }

        var lines = this.getLines(), lineOptions = { color: this.options.color, weight: this.options.weight, opacity: 1.0 };
        for (var i = 0; i < lines.length; i++) {
            var line = lines[i];
            if (!line.from || !line.to || line.touching === true)
                continue;

            var calc = L.ViewAngle.toCalc([line.from, line.to]);
            this._calcCrs = calc.crsId;
            var dx = calc.points[1].X - calc.points[0].X, dy = calc.points[1].Y - calc.points[0].Y;
            if (dx === 0 && dy === 0)
                continue;

            this._addLine([line.from, line.to], lineOptions);
            this._addEndMarker(line.from);
            this._addEndMarker(line.to);
            this._addParallelLabel(line.from, line.to, dx, dy, L.ObjectDistance.label(line, i, this.options));
        }
    },
    _addHighlight: function (geometry, color) {
        var highlight = L.geoJSON(geometry, {
            interactive: false,
            style: function () {
                return { color: color, weight: 4, opacity: 0.8, fillColor: color, fillOpacity: 0.15 };
            },
            pointToLayer: function (feature, latLng) {
                return L.circleMarker(latLng, { radius: 8, color: color, weight: 3, opacity: 0.8, fillColor: color, fillOpacity: 0.3, interactive: false });
            }
        });
        this.addChildLayer(highlight);
    },
    _addLine: function (latLngs, options) {
        var line = L.polyline(latLngs, options);
        this.addChildLayer(line);
        line.on('click', function (e) {
            this.fireEvent('click', e);
        }, this);
    },
    _addEndMarker: function (latLng) {
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
    // label parallel to the line (dx, dy: calc crs), above the line at its midpoint
    _addParallelLabel: function (from, to, dx, dy, text) {
        var angle = L.Perpendicular.textAngle(dx, dy);

        this.addChildLayer(L.svgLabel(L.latLng((from.lat + to.lat) * 0.5, (from.lng + to.lng) * 0.5), {
            text: text,
            rotation: -angle,
            fontSize: this.options.fontSize,
            fontColor: this.options.fontColor,
            alignmentBaseline: 'ideographic'
        }));
    },
    // all lines (touching objects => line with length 0), the print/export uses the same order (numbers)
    toGeoJSON: function () {
        var coordinates = [];
        for (var line of this.getLines()) {
            if (line.from && line.to) {
                coordinates.push([[line.from.lng, line.from.lat], [line.to.lng, line.to.lat]]);
            }
        }

        return {
            geometry: {
                type: 'MultiLineString',
                coordinates: coordinates
            }
        };
    },
    toPreviewGeoJSON: function () {
        return this.toGeoJSON();
    }
});

L.objectDistance = function (latLngs, options) {
    return new L.ObjectDistance(latLngs, options);
};

L.ObjectDistance.highlightColorA = '#ff6600';
L.ObjectDistance.highlightColorB = '#0099ff';

// line: { length }, index: 0 based => "12,34m" or "1: 12,34m" (showNumber)
L.ObjectDistance.label = function (line, index, options) {
    var text = L.Perpendicular.formatLength(line.length || 0, options.lengthUnit);
    return options.showNumber === true ? (index + 1) + ': ' + text : text;
};
