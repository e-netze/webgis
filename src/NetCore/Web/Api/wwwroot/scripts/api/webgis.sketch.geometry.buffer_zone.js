// Geometry type 'buffer_zone' (Pufferzone)
//   sourceType point   => 1 vertex, further clicks move the point (sketch stays open)
//   sourceType multipoint => every click adds a point (no mover line)
//   sourceType line    => polyline (>= 2 vertices), multipart ("close section")
//   sourceType polygon => closed ring (>= 3 vertices), multipart/holes ("close section"), width can be negative (inner buffer)
// One framework element (L.bufferZone) for all vertices, partStarts = start index of each part
// The buffer is calculated by the server (webgis.map.graphics => 'buffer-zone-calc'), see webgis-graphics.buffer_zone.js
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('buffer_zone', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.sourceType = 'line';  // point | multipoint | line | polygon
    this.width = 10;           // meters (polygon: negative => inner buffer)
    this.buffer = null;        // { key, rings } => server response
    this.createsFrameworkElementPerPart = false;
    this.elementOptions = { sourceType: ['getSourceType', 'setSourceType'], width: ['getWidth', 'setWidth'], buffer: ['getBuffer', 'setBuffer'] };

    // start index of each (non empty) part, null => one part
    var _partStarts = function (model) {
        var starts = [];
        for (var i = 0; i < model.count(); i++) {
            var start = model.start(i);
            if (start < model.vertices.length && (starts.length === 0 || start > starts[starts.length - 1]))
                starts.push(start);
        }
        return starts.length > 1 ? starts : null;
    };
    var _parts = function () {
        return L.BufferZone.splitParts(state.vertices, _partStarts(state.model), sketch.getBufferZoneSourceType());
    };

    // GeoJSON type of the source feature ("take sketch from feature geometry") => sourceType
    var _sourceTypeOf = function (geometryType) {
        switch ((geometryType || '').toLowerCase()) {
            case 'point': return 'point';
            case 'multipoint': return 'multipoint';
            case 'linestring': case 'multilinestring': case 'polyline': return 'line';
            case 'polygon': case 'multipolygon': return 'polygon';
        }
        return null;
    };

    this.isValid = function () {
        var parts = _parts();
        return parts.length > 0 && parts[0].length >= L.BufferZone.minVertexCount(sketch.getBufferZoneSourceType());
    };

    // line/polygon: further parts (multipart, holes)
    this.canCloseSection = function () {
        var sourceType = sketch.getBufferZoneSourceType(), model = state.model;
        if (L.BufferZone.isPointSource(sourceType))
            return false;
        return state.vertices.length - model.start(model.count() - 1) >= L.BufferZone.minVertexCount(sourceType);
    };

    this.calcProperties = function (xParam, yParam) {
        var length = 0;
        if (!L.BufferZone.isPointSource(sketch.getBufferZoneSourceType())) {
            var parts = _parts();
            for (var p = 0; p < parts.length; p++) {
                length += webgis.calc.length(parts[p], xParam, yParam);
            }
        }
        return {
            length: length,
            set_values: true
        };
    };

    this.render = function (model) {
        var element = sketch._createFrameworkElement();
        element.setPartStarts(_partStarts(model));
        for (var i = 0; i < model.vertices.length; i++) {
            sketch._addAsLatLng(element, model.vertices[i]);
        }
        return [element];
    };
    this.renderAddedVertex = function (model, elements, currentIndex, vertex) {
        elements[0].setPartStarts(_partStarts(model));
        sketch._addAsLatLng(elements[0], vertex);
        elements[0].sketch_vertex_coords = vertex;
        return 0;
    };
    this.renderAppendPart = function (model, elements, latLngs) {
        if (!elements || elements.length === 0 || model.vertices.length === 0) {
            elements = [sketch._createFrameworkElement(latLngs)];
        }
        elements[0].setPartStarts(_partStarts(model));
        return elements;
    };
    this.cloneGraphicElements = function () {
        var element = sketch._createFrameworkElement();
        element.setPartStarts(_partStarts(state.model));
        element.setLatLngs(state.vertices.map(function (v) { return L.latLng(v.y, v.x); }));
        return [element];
    };

    // point: only one vertex, further clicks move the point
    this.tryAddVertex = function (vertex, vertices, fireEvents, coords) {
        if (sketch.getBufferZoneSourceType() !== 'point' || state.vertices.length < 1)
            return false;

        if (fireEvents)
            this._replaceLastVertex(vertex);
        return true;
    };

    // point/multipoint: no mover line
    this.handleMapMouseMove = function (latlng) {
        if (!L.BufferZone.isPointSource(sketch.getBufferZoneSourceType()) || state.vertices.length < 1 || sketch.isSketchMoving() || sketch.isSketchRotating()) {
            return false;
        }

        sketch.hideMoverLine();
        sketch.hideTraceLine();
        sketch.fireCurrentState();
        return true;
    };

    // switched to point => keep only the first vertex
    this.onSourceTypeChanged = function () {
        if (sketch.getBufferZoneSourceType() === 'point') {
            for (var i = state.vertices.length - 1; i > 0; i--) {
                sketch.removeVertex(i, true);
            }
        }
    };

    // json:
    //   stored element (graphics load): flat coordinates + partindex (bz-parts), sourceType is already set
    //   "take sketch from feature geometry": json.type is overwritten with 'buffer_zone', the original type is in json.sourceGeometryType
    //     (graphics._resetGeoJsonType) => sourceType is set from the feature geometry, all parts/rings are used
    this.fromJson = function (json, crsId, readOnly) {
        var coords = json.coordinates, COORDS = json.COORDINATES;
        if (!coords || !coords.length)
            return;

        var parts = [], PARTS = [];
        if (typeof coords[0] === 'number') {                                  // Point
            parts.push([coords]);
            PARTS.push(COORDS && COORDS.length ? [COORDS] : null);
        } else if (Array.isArray(coords[0][0])) {                             // Polygon (rings), MultiPolygon (polygons => rings)
            var multi = Array.isArray(coords[0][0][0]);
            for (var p = 0; p < (multi ? coords.length : 1); p++) {
                var rings = multi ? coords[p] : coords, RINGS = COORDS ? (multi ? COORDS[p] : COORDS) : null;
                for (var r = 0; r < rings.length; r++) {
                    var ring = rings[r], RING = RINGS ? RINGS[r] : null;
                    if (ring.length > 1 && ring[0][0] === ring[ring.length - 1][0] && ring[0][1] === ring[ring.length - 1][1]) {
                        ring = ring.slice(0, ring.length - 1);                // closing vertex
                    }
                    parts.push(ring);
                    PARTS.push(RING);
                }
            }
        } else {                                                              // MultiPoint, LineString (partindex => parts)
            var starts = (json.partindex || []).map(function (i) { return parseInt(i); }).filter(function (i) { return i > 0 && i < coords.length; });
            starts = [0].concat(starts);
            for (var s = 0; s < starts.length; s++) {
                var end = s + 1 < starts.length ? starts[s + 1] : coords.length;
                parts.push(coords.slice(starts[s], end));
                PARTS.push(COORDS ? COORDS.slice(starts[s], end) : null);
            }
        }

        var sourceType = _sourceTypeOf(json.sourceGeometryType);
        if (sourceType) {
            sketch.setBufferZoneSourceType(sourceType);
        }
        sourceType = sketch.getBufferZoneSourceType();
        if (L.BufferZone.isPointSource(sourceType)) {  // no parts
            var allParts = [].concat.apply([], parts);
            var ALLPARTS = PARTS.every(function (P) { return P; }) ? [].concat.apply([], PARTS) : null;
            parts = [sourceType === 'point' ? allParts.slice(0, 1) : allParts];
            PARTS = [ALLPARTS];
        }

        for (var i = 0; i < parts.length; i++) {
            if (parts[i].length === 0)
                continue;
            if (state.vertices.length > 0) {
                sketch.appendPart(null, true);
            }
            for (var v = 0; v < parts[i].length; v++) {
                var xy = crsId ? webgis.unproject(crsId, parts[i][v]) : parts[i][v];
                sketch.addVertices([PARTS[i] && PARTS[i].length > v ? this._jsonVertex(json, xy, PARTS[i][v]) : { x: xy[0], y: xy[1] }], false, readOnly);
            }
        }

        sketch.events.fire('onchanged', sketch);  // => buffer request (webgis.map.graphics)
    };

    this.createFrameworkElement = function (latLngs) {
        var bufferZone = L.bufferZone(latLngs || [], {
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            fillColor: sketch.getFillColor(),
            fillOpacity: sketch.getFillOpacity(),
            fontColor: sketch.getTextColor(),
            fontSize: sketch.getTextSize(),
            sourceType: sketch.getBufferZoneSourceType(),
            width: sketch.getBufferZoneWidth(),
            buffer: sketch.getBufferZoneBuffer()  // redraw(true) => keep the buffer (only drawn, if the key still matches)
        });
        bufferZone._webgis = { geometryType: state.geometryType };
        return bufferZone;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'buffer_zone';
        var partStarts = frameworkElement.getPartStarts();
        if (partStarts) {
            jsonGeometry.partindex = partStarts;
        }
        this.sourceType = frameworkElement.getSourceType();
        this.width = frameworkElement.getWidth();
        this.buffer = frameworkElement.getBuffer();
    };
});
