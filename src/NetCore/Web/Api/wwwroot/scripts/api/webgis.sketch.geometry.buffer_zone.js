// Geometry type 'buffer_zone' (Pufferzone)
//   sourceType point   => 1 vertex, further clicks move the point (sketch stays open)
//   sourceType multipoint => every click adds a point (no mover line)
//   sourceType line    => polyline (>= 2 vertices)
//   sourceType polygon => closed ring (>= 3 vertices), width can be negative (inner buffer)
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

    this.isValid = function () {
        return state.vertices.length >= L.BufferZone.minVertexCount(sketch.getBufferZoneSourceType());
    };

    this.calcProperties = function (xParam, yParam) {
        return {
            length: L.BufferZone.isPointSource(sketch.getBufferZoneSourceType()) ? 0 : webgis.calc.length(sketch.partVertices(0), xParam, yParam),
            set_values: true
        };
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

    // "take sketch from feature geometry": json.type is overwritten with 'buffer_zone' (graphics._resetGeoJsonType)
    // => detect the geometry by its structure (Point, MultiPoint/LineString, Polygon). Only the first part/ring is used (single part type)
    this.fromJson = function (json, crsId, readOnly) {
        var coords = json.coordinates, COORDS = json.COORDINATES;
        if (!coords || !coords.length)
            return;

        var list, LIST;
        if (typeof coords[0] === 'number') {               // Point
            list = [coords];
            LIST = COORDS && COORDS.length ? [COORDS] : null;
        } else if (Array.isArray(coords[0][0])) {           // Polygon (rings)
            list = coords[0];
            LIST = COORDS ? COORDS[0] : null;
            if (list.length > 1 && list[0][0] === list[list.length - 1][0] && list[0][1] === list[list.length - 1][1]) {
                list = list.slice(0, list.length - 1);     // closing vertex
            }
        } else {                                            // MultiPoint, LineString (partindex => first part)
            var end = json.partindex && json.partindex.length > 1 ? json.partindex[1] : coords.length;
            list = coords.slice(0, end);
            LIST = COORDS;
        }

        if (sketch.getBufferZoneSourceType() === 'point') {
            list = list.slice(0, 1);
        }

        for (var i = 0; i < list.length; i++) {
            var xy = crsId ? webgis.unproject(crsId, list[i]) : list[i];
            sketch.addVertices([LIST && LIST.length > i ? this._jsonVertex(json, xy, LIST[i]) : { x: xy[0], y: xy[1] }], false, readOnly);
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
        this.sourceType = frameworkElement.getSourceType();
        this.width = frameworkElement.getWidth();
        this.buffer = frameworkElement.getBuffer();
    };
});
