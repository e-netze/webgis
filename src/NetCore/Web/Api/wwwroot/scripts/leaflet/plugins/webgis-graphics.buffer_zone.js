// MapMarkup measure tool 'buffer_zone' (Pufferzone):
//   latLngs => source geometry (sourceType: point => 1 vertex, multipoint => points, line, polygon => closed ring)
//   polygons can have a negative width (inner buffer)
//   the buffer (rings, WGS84) is calculated by the server (MapMarkup tool command 'buffer-zone-calc', BufferZoneCalculator.cs)
//   buffer = { key, rings }: only drawn, if the key matches the current geometry/options (L.BufferZone.key) => no stale buffer while the response is pending
//   the buffer width is labeled with a dimension line (perpendicular to the first segment, outside for polygons)
// The server (print) uses the same geometry: BufferZoneElement.cs
// requires the shared helpers of webgis-graphics.view_angle.js (L.ViewAngle.*) and webgis-graphics.perpendicular.js (L.Perpendicular.*)
L.BufferZone = L.LayerCollection.extend({
    options: {
        color: '#000000',
        weight: 2,
        fillColor: '#aaaaaa',
        fillOpacity: 0.2,
        fontColor: 'black',
        fontSize: 14,
        sourceType: 'line',  // point | multipoint | line | polygon
        width: 10,           // meters, polygon: negative => inner buffer
        buffer: null,        // { key, rings: [[[lng,lat],...],...] }
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

    setSourceType: function (sourceType) {
        this.options.sourceType = sourceType;
        this.rebuild();
    },
    getSourceType: function () { return this.options.sourceType; },
    setWidth: function (width) {
        this.options.width = width;
        this.rebuild();
    },
    getWidth: function () { return this.options.width; },
    setBuffer: function (buffer) {
        this.options.buffer = buffer || null;
        this.rebuild();
    },
    getBuffer: function () { return this.options.buffer; },

    getBufferKey: function () {
        return L.BufferZone.key(this._latLngs, this.options.sourceType, this.options.width);
    },
    // rings of the buffer, if it matches the current geometry, otherwise null
    getValidBufferRings: function () {
        var buffer = this.options.buffer;
        if (!buffer || !buffer.rings || buffer.rings.length === 0)
            return null;
        return buffer.key === this.getBufferKey() ? buffer.rings : null;
    },
    isValidSource: function () {
        return (this._latLngs ? this._latLngs.length : 0) >= L.BufferZone.minVertexCount(this.options.sourceType);
    },

    redraw: function () {
        this.rebuild();
    },
    rebuild: function () {
        if (!this._map)
            return;

        this.trimChildren(0);

        if (!this._latLngs || this._latLngs.length === 0)
            return;

        var sourceType = this.options.sourceType;
        var rings = this.getValidBufferRings();
        var lineOptions = { color: this.options.color, weight: this.options.weight, opacity: 1.0 };

        if (rings) {
            var polygon = L.polygon(rings.map(function (ring) {
                return ring.map(function (c) { return L.latLng(c[1], c[0]); });
            }), {
                color: this.options.color,
                weight: 1,
                opacity: 1.0,
                fillColor: this.options.fillColor,
                fillOpacity: this.options.fillOpacity
            });
            this._addChild(polygon);
        }

        if (sourceType === 'point') {
            this._addPointMarker(this._latLngs[0]);
        } else if (sourceType === 'multipoint') {
            for (var m = 0; m < this._latLngs.length; m++) {
                this._addPointMarker(this._latLngs[m]);
            }
        } else if (sourceType === 'polygon' && this._latLngs.length > 2) {
            this._addChild(L.polyline(this._latLngs.concat([this._latLngs[0]]), lineOptions));
        } else if (this._latLngs.length > 1) {
            this._addChild(L.polyline(this._latLngs, lineOptions));
        } else {
            this._addPointMarker(this._latLngs[0]);
        }

        var width = parseFloat(this.options.width);
        if (rings && L.BufferZone.isValidWidth(sourceType, width)) {
            var calc = L.ViewAngle.toCalc(L.BufferZone.isPointSource(sourceType) ? this._latLngs.slice(0, 1) : this._latLngs);
            this._calcCrs = calc.crsId;

            var dim = L.BufferZone.dimension(calc.points, sourceType, width);
            if (dim) {
                var from = calc.toLatLng(dim.from.X, dim.from.Y), to = calc.toLatLng(dim.to.X, dim.to.Y);
                this._addChild(L.polyline([from, to], L.extend({}, lineOptions, { weight: Math.max(1, this.options.weight / 2) })));
                this._addParallelLabel(from, to, dim.to.X - dim.from.X, dim.to.Y - dim.from.Y,
                    L.Perpendicular.formatLength(width, 'm'));
            }
        }
    },
    _addChild: function (layer) {
        this.addChildLayer(layer);
        layer.on('click', function (e) {
            this.fireEvent('click', e);
        }, this);
    },
    _addPointMarker: function (latLng) {
        this._addChild(L.circleMarker(latLng, {
            radius: Math.max(3, this.options.weight + 1),
            color: this.options.color,
            weight: 1,
            fillColor: this.options.color,
            fillOpacity: 1.0
        }));
    },
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
    toGeoJSON: function () {
        var geoJson = {
            geometry: {
                type: 'LineString',
                coordinates: []
            }
        };

        if (this._latLngs) {
            var count = this.options.sourceType === 'point' ? Math.min(1, this._latLngs.length) : this._latLngs.length;
            for (var l = 0; l < count; l++) {
                geoJson.geometry.coordinates.push([this._latLngs[l].lng, this._latLngs[l].lat]);
            }
        }

        return geoJson;
    },
    // highlight (e.g. hovering the graphics list): the buffer, otherwise the source
    toPreviewGeoJSON: function () {
        var rings = this.getValidBufferRings();
        if (rings) {
            return { geometry: { type: 'Polygon', coordinates: rings } };
        }

        var geoJson = this.toGeoJSON();
        if (this.options.sourceType === 'point' && geoJson.geometry.coordinates.length > 0) {
            return { geometry: { type: 'Point', coordinates: geoJson.geometry.coordinates[0] } };
        }
        if (this.options.sourceType === 'multipoint') {
            return { geometry: { type: 'MultiPoint', coordinates: geoJson.geometry.coordinates } };
        }
        return geoJson;
    }
});

L.bufferZone = function (latLngs, options) {
    return new L.BufferZone(latLngs, options);
};

L.BufferZone.minVertexCount = function (sourceType) {
    switch (sourceType) {
        case 'polygon': return 3;
        case 'line': return 2;
        default: return 1;
    }
};

L.BufferZone.isPointSource = function (sourceType) {
    return sourceType === 'point' || sourceType === 'multipoint';
};

// "take sketch from feature geometry": geometry type sent to the server (IdentifyDefault.OnSketchFromGeometry) => layers to query
L.BufferZone.queryGeometryType = function (sourceType) {
    if (L.BufferZone.isPointSource(sourceType))
        return 'point';
    return sourceType === 'polygon' ? 'polygon' : 'polyline';
};

// negative width (inner buffer) only for polygons, same as BufferZoneCalculator.IsValidWidth
L.BufferZone.isValidWidth = function (sourceType, width) {
    width = parseFloat(width);
    return isFinite(width) && (sourceType === 'polygon' ? width !== 0 : width > 0);
};

// identifies the geometry/options a buffer was calculated for
L.BufferZone.key = function (latLngs, sourceType, width) {
    var count = latLngs ? (sourceType === 'point' ? Math.min(1, latLngs.length) : latLngs.length) : 0;
    var coords = [];
    for (var i = 0; i < count; i++) {
        coords.push(latLngs[i].lng.toFixed(8) + ',' + latLngs[i].lat.toFixed(8));
    }
    return (sourceType || 'line') + '|' + parseFloat(width) + '|' + coords.join(';');
};

// dimension line of the buffer width (calc crs {X,Y}) => { from, to } or null
// point/multipoint (or < 2 points): from the (first) point to the east
// line/polygon: perpendicular to the first segment from its midpoint, for polygons outside (negative width => inside)
// same algorithm: BufferZoneElement.cs
L.BufferZone.dimension = function (points, sourceType, width) {
    if (!points || points.length === 0 || !width)
        return null;

    var from = points[0], nx = 1, ny = 0;

    if (!L.BufferZone.isPointSource(sourceType) && points.length >= 2) {
        var a = points[0], b = points[1];
        var dx = b.X - a.X, dy = b.Y - a.Y, len = Math.sqrt(dx * dx + dy * dy);
        if (len > 0) {
            nx = -dy / len; ny = dx / len;
            from = { X: (a.X + b.X) * 0.5, Y: (a.Y + b.Y) * 0.5 };

            if (sourceType === 'polygon' && points.length >= 3) {
                var eps = len * 1e-3;
                if (L.BufferZone.pointInPolygon(points, from.X + nx * eps, from.Y + ny * eps)) {
                    nx = -nx; ny = -ny;
                }
            }
        }
    }

    return { from: { X: from.X, Y: from.Y }, to: { X: from.X + nx * width, Y: from.Y + ny * width } };
};

// ray casting
L.BufferZone.pointInPolygon = function (points, x, y) {
    var inside = false;
    for (var i = 0, j = points.length - 1; i < points.length; j = i++) {
        var xi = points[i].X, yi = points[i].Y, xj = points[j].X, yj = points[j].Y;
        if (((yi > y) !== (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi)) {
            inside = !inside;
        }
    }
    return inside;
};
