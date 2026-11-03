// Geometry type 'perpendicular' (Normalabstand)
//   part 0      => reference line (polyline), finished with "close section" ('f' key, context menu, mobile button)
//   part 1      => points, for each point the perpendicular onto the reference line is drawn
// One framework element (L.perpendicular) for all vertices, lineVertexCount = start of part 1
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('perpendicular', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.stationFormat = 'm';  // none | m | km
    this.lengthUnit = 'm';  // m | km
    this.createsFrameworkElementPerPart = false;
    this.elementOptions = { stationFormat: ['getStationFormat', 'setStationFormat'], lengthUnit: ['getLengthUnit', 'setLengthUnit'] };

    // null => the reference line is not finished, all vertices belong to the line
    var _lineVertexCount = function (model) {
        return model.count() > 1 ? model.start(1) : null;
    };

    this.isValid = function () {
        var model = state.model;
        return model.count() > 1 && model.start(1) >= 2 && state.vertices.length > model.start(1);
    };

    this.canCloseSection = function () {
        return state.model.count() === 1 && state.vertices.length >= 2;
    };

    this.calcProperties = function (xParam, yParam) {
        return {
            length: webgis.calc.length(sketch.partVertices(0), xParam, yParam),
            set_values: true
        };
    };

    this.render = function (model) {
        var element = sketch._createFrameworkElement();
        element.setLineVertexCount(_lineVertexCount(model));
        for (var i = 0; i < model.vertices.length; i++) {
            sketch._addAsLatLng(element, model.vertices[i]);
        }
        return [element];
    };
    this.renderAddedVertex = function (model, elements, currentIndex, vertex) {
        elements[0].setLineVertexCount(_lineVertexCount(model));
        sketch._addAsLatLng(elements[0], vertex);
        elements[0].sketch_vertex_coords = vertex;
        return 0;
    };
    this.renderAppendPart = function (model, elements, latLngs) {
        if (!elements || elements.length === 0 || model.vertices.length === 0) {
            elements = [sketch._createFrameworkElement(latLngs)];
        }
        elements[0].setLineVertexCount(_lineVertexCount(model));
        return elements;
    };

    // reference line finished => points, no mover line from the last vertex
    this.handleMapMouseMove = function (latlng) {
        if (state.model.count() <= 1 || sketch.isSketchMoving() || sketch.isSketchRotating()) {
            return false;
        }

        sketch.hideMoverLine();
        sketch.hideTraceLine();
        sketch.fireCurrentState();
        return true;
    };

    this.onVertexMarkerDrag = function (marker, latlng, latLngs) {
        var lineCount = _lineVertexCount(state.model);
        if (lineCount !== null && parseInt(marker.vertex_index) >= lineCount) {
            marker.sketch.hideMoverLine();  // point => no connection to the other points
        } else {
            marker.sketch.showMoverLine(latLngs);
        }
    };

    this.cloneGraphicElements = function () {
        var element = sketch._createFrameworkElement();
        element.setLineVertexCount(_lineVertexCount(state.model));
        element.setLatLngs(state.vertices.map(function (v) { return L.latLng(v.y, v.x); }));
        return [element];
    };

    /********* perpendicular specific (sketch facade: addPerpendicularPoints) *********/

    // reference line with at least 2 vertices (finished or not)
    this.hasReferenceLine = function () {
        return sketch.partVertices(0).length >= 2;
    };
    // add from selection: points [[lng, lat], ...] => an unfinished reference line is finished,
    // points already in the sketch (< 1 cm) are skipped, one undo step => { added, skipped } or null (no reference line)
    this.addPoints = function (points) {
        if (!this.hasReferenceLine())
            return null;

        var existing = state.model.count() > 1 ? state.vertices.slice(state.model.start(1)) : [];
        var _isDuplicate = function (x, y) {
            for (var i = 0; i < existing.length; i++) {
                var kx = 111320.0 * Math.cos(y * Math.PI / 180.0);
                var dx = (existing[i].x - x) * kx, dy = (existing[i].y - y) * 110540.0;
                if (dx * dx + dy * dy < 0.0001)
                    return true;
            }
            return false;
        };

        var vertices = [], skipped = 0;
        for (var p = 0; p < (points || []).length; p++) {
            var x = points[p][0], y = points[p][1];
            if (_isDuplicate(x, y)) {
                skipped++;
                continue;
            }
            var vertex = { x: x, y: y };
            vertices.push(vertex);
            existing.push(vertex);
        }

        if (vertices.length > 0) {
            sketch.addUndo(webgis.l10n.get('perpendicular-undo-add-from-selection'));
            if (state.model.count() === 1) {
                sketch.appendPart(null, true);
            }
            sketch.addVertices(vertices, false);
        }
        return { added: vertices.length, skipped: skipped };
    };

    this.fromJson = function (json, crsId, readOnly) {
        var count = json.coordinates.length, lineVertexCount = parseInt(json.lineVertexCount);
        for (var i = 0; i < count; i++) {
            var xy = crsId ? webgis.unproject(crsId, json.coordinates[i]) : json.coordinates[i];
            if (json.COORDINATES && json.COORDINATES.length > i) {
                sketch.addVertices([this._jsonVertex(json, xy, json.COORDINATES[i])], false, readOnly);
            } else {
                sketch.addVertices([{ x: xy[0], y: xy[1] }], false, readOnly);
            }
            if (i + 1 === lineVertexCount && lineVertexCount < count) {
                sketch.appendPart(null, true);
            }
        }
    };

    this.createFrameworkElement = function (latLngs) {
        var perpendicular = L.perpendicular(latLngs || [], {
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            fontColor: sketch.getTextColor(),
            fontSize: sketch.getTextSize(),
            stationFormat: sketch.getPerpendicularStationFormat(),
            lengthUnit: sketch.getPerpendicularLengthUnit()
        });
        perpendicular._webgis = { geometryType: state.geometryType };
        return perpendicular;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'perpendicular';
        jsonGeometry.lineVertexCount = frameworkElement.getLineVertexCount();
        this.stationFormat = frameworkElement.getStationFormat();
        this.lengthUnit = frameworkElement.getLengthUnit();
    };
});
