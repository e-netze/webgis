// Geometry type 'object_distance' (Objektabstand)
//   a map click doesn't add a vertex: it queries the features at the click position (event 'onobjectdistancepick' => webgis.map.graphics,
//   server command 'object-distance-pick'), the first selected feature is object A, each further one adds an object B
//   the shortest connection A-B is calculated by the server ('object-distance-calc')
// Vertex model (vertex.od holds the object infos, copied by webgis.sketch.vertices.copyVertex => undo, load):
//   vertices[0]               => A, click position, od: { role: 'a', ref, layer, oid, geometry }
//   vertices[1 + 2i], [2 + 2i] => line i: from (on A) od: { role: 'b', ref, layer, oid, length, touching, geometry }, to (on B) od: { role: 'to' }
//   touching/intersecting objects => from == to (length 0, no line)
// One framework element (L.objectDistance) for all vertices, see webgis-graphics.object_distance.js
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults)
// _base (not _line): no vertex markers, no snapping, no move/rotate/upload/download (the vertices are calculated, not drawn)

webgis.sketch.geometryTypes.define('object_distance', '_base', function (ctx) {
    "use strict";

    var $ = webgis.$;
    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.lengthUnit = 'm';    // m | km
    this.showNumber = false;
    this.createsFrameworkElementPerPart = false;
    this.elementOptions = { lengthUnit: ['getLengthUnit', 'setLengthUnit'], showNumber: ['getShowNumber', 'setShowNumber'] };

    // vertices => data of L.objectDistance: { a, lines }
    var _data = function (vertices) {
        var data = { a: null, lines: [] };
        if (vertices.length === 0 || !vertices[0].od)
            return data;

        data.a = $.extend({}, vertices[0].od, { x: vertices[0].x, y: vertices[0].y });
        delete data.a.role;

        for (var i = 1; i + 1 < vertices.length; i += 2) {
            var from = vertices[i], to = vertices[i + 1];
            var line = $.extend({}, from.od || {}, { from: L.latLng(from.y, from.x), to: L.latLng(to.y, to.x) });
            delete line.role;
            data.lines.push(line);
        }
        return data;
    };

    // [lng, lat] (stored GeoJSON) or latLng (framework element) => { x, y }
    var _xy = function (p) {
        if (!p)
            return null;
        return Array.isArray(p) ? { x: p[0], y: p[1] } : { x: p.lng, y: p.lat };
    };

    var _lineVertices = function (line) {
        var from = _xy(line.from), to = _xy(line.to);
        if (!from || !to)
            return [];

        return [
            { x: from.x, y: from.y, od: { role: 'b', ref: line.ref, layer: line.layer, oid: line.oid, length: line.length, touching: line.touching === true, geometry: line.geometry || null } },
            { x: to.x, y: to.y, od: { role: 'to' } }
        ];
    };

    this.isValid = function () {
        return state.vertices.length >= 3;
    };

    this.calcProperties = function (xParam, yParam) {
        return {};
    };

    // vertices with object infos (load, undo, own pairs) => default handling, map clicks => query the features (no vertex)
    this.tryAddVertex = function (vertex, vertices, fireEvents, coords) {
        if (vertex.od)
            return false;

        if (fireEvents) {
            sketch.events.fire('onobjectdistancepick', sketch, vertex);
        }
        return true;
    };
    this.canCloseSection = function () {
        return false;
    };
    this.handleMapMouseMove = function (latlng) {
        sketch.hideMoverLine();
        sketch.hideTraceLine();
        return true;
    };

    this.render = function (model) {
        var element = sketch._createFrameworkElement();
        element.setData(_data(model.vertices));
        return [element];
    };
    this.renderAddedVertex = function (model, elements, currentIndex, vertex) {
        elements[0].setData(_data(model.vertices));
        return 0;
    };
    this.renderAppendPart = function (model, elements, latLngs) {
        if (!elements || elements.length === 0 || model.vertices.length === 0) {
            elements = [sketch._createFrameworkElement()];
        }
        elements[0].setData(_data(model.vertices));
        return elements;
    };
    // committed element => without the highlighted objects A/B
    this.cloneGraphicElements = function () {
        var element = this._createElement(false);
        element.setData(_data(state.vertices));
        return [element];
    };

    /********* object_distance specific (sketch facade: getObjectDistanceData, setObjectDistanceA, ...) *********/

    this.getData = function () {
        return _data(state.vertices);
    };
    // candidate: { ref, layer, oid, geometry } (server 'object-distance-pick'), latLng: click position
    this.setA = function (candidate, latLng) {
        if (state.vertices.length > 0)
            return;

        sketch.addUndo(webgis.l10n.get('object-distance-undo-select-a'));
        sketch.addVertices([{ x: latLng.lng, y: latLng.lat, od: { role: 'a', ref: candidate.ref, layer: candidate.layer, oid: candidate.oid, geometry: candidate.geometry || null } }], false);
    };
    // line: { ref, layer, oid, geometry, length, touching, from: [lng, lat], to: [lng, lat] }
    this.addLine = function (line) {
        var vertices = _lineVertices(line);
        if (state.vertices.length === 0 || vertices.length === 0)
            return;

        sketch.addUndo(webgis.l10n.get('object-distance-undo-add-b'));
        sketch.addVertices(vertices, false);
    };
    this.removeLine = function (index) {
        var from = 1 + 2 * index;
        if (index < 0 || from + 1 >= state.vertices.length)
            return;

        sketch.addUndo(webgis.l10n.get('object-distance-undo-remove-b'));
        sketch.removeVertex(from + 1, true);
        sketch.removeVertex(from, true);
    };

    // json.od: { a: { ref, layer, oid, x, y }, lines: [...] } => stored element (graphics.fromGeoJson) or framework element (fromFrameworkElement)
    this.fromJson = function (json, crsId, readOnly) {
        var od = json.od;
        if (!od || !od.a)
            return;

        var a = od.a, lines = od.lines || [];
        var aXY = a.x !== undefined && a.y !== undefined ? { x: a.x, y: a.y } : (lines.length > 0 ? _xy(lines[0].from) : null);
        if (!aXY)
            return;

        var vertices = [{ x: aXY.x, y: aXY.y, od: { role: 'a', ref: a.ref, layer: a.layer, oid: a.oid, geometry: a.geometry || null } }];
        for (var i = 0; i < lines.length; i++) {
            vertices = vertices.concat(_lineVertices(lines[i]));
        }
        sketch.addVertices(vertices, false, readOnly);
    };

    this._createElement = function (highlightSources) {
        var objectDistance = L.objectDistance([], {
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            fontColor: sketch.getTextColor(),
            fontSize: sketch.getTextSize(),
            lengthUnit: sketch.getObjectDistanceLengthUnit(),
            showNumber: sketch.getObjectDistanceShowNumber(),
            highlightSources: highlightSources
        });
        objectDistance._webgis = { geometryType: state.geometryType };
        return objectDistance;
    };
    this.createFrameworkElement = function (latLngs) {
        return this._createElement(true);
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'object_distance';
        jsonGeometry.od = frameworkElement.getData();
        this.lengthUnit = frameworkElement.getLengthUnit();
        this.showNumber = frameworkElement.getShowNumber();
    };
});
