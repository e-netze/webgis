// Sketch component: move and rotate the whole sketch (or the selected vertices)
// Session: start => preview (gray polylines, one per part) => move/rotate (apply to the vertex model) => end
// The calculations are in the vertex model (translate, rotate, translateLatLngs, rotateLatLngs).
// The sketch facade delegates startSketchMoving, moveSketchPreview, moveSketch, ... to this component.
// See docs/architecture/sketch-geometry-types.md

webgis.sketch.transform = function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    var _movingVertexIndex = -1, _moveElements = [], _moveRefLatLng = null;
    var _rotatingVertexIndex = -1, _rotateElements = [], _rotateRefAngle = 0.0, _startAngle = 0.0;

    /********* Preview elements *********/
    var _addPreviewElements = function () {
        var elements = [], parts = state.model.toLatLngs(ctx.type().hasClosingSegment);
        for (var p = 0; p < parts.length; p++) {
            var polyline = L.polyline([], { color: 'gray', opacity: .8, weight: 4 });
            for (var l = 0; l < parts[p].length; l++) {
                polyline.addLatLng(parts[p][l]);
            }
            elements.push(polyline);
        }
        for (var e in elements) {
            sketch._addToMap(elements[e]);
        }
        return elements;
    };
    var _removePreviewElements = function (elements) {
        for (var e in elements) {
            sketch.map.frameworkElement.removeLayer(elements[e]);
        }
    };
    var _previewLatLngs = function (elements) {
        return elements.map(function (element) { return element.getLatLngs(); });
    };
    var _setPreviewLatLngs = function (elements, latLngParts) {
        for (var p in elements) {
            elements[p].setLatLngs(latLngParts[p]);
        }
    };
    var _isValidIndex = function (index) {
        return index >= 0 && index < state.vertices.length;
    };
    var _targetLatLng = function (latlng) {
        return sketch._snappedVertex != null ? { lng: sketch._snappedVertex.x, lat: sketch._snappedVertex.y } : latlng;
    };
    var _resetConstruction = function () {
        state.ortho = false;
        sketch._fixedDirectionVector = null;
        sketch._fixedDistance = null;
    };
    // the sketch shows the new vertices after a hard redraw
    var _reload = function () {
        sketch.startSuppressUndo();
        sketch.load(sketch.store());  // Force hard redaw
        sketch.stopSuppressUndo();
    };

    /********* Move *********/
    this.startMoving = function (index) {
        sketch.endSketchRotating();

        if (_isValidIndex(index)) {
            sketch.endSketchMoving();

            sketch.hide();
            var vertices = state.vertices;

            if (vertices.length > 1) {
                _moveElements = _addPreviewElements();
            }

            _moveRefLatLng = { lat: vertices[index].y, lng: vertices[index].x };
            _movingVertexIndex = index;

            _resetConstruction();
        } else {
            sketch.endSketchMoving();
        }
    };
    this.isMoving = function () {
        return _movingVertexIndex >= 0;
    };
    this.endMoving = function () {
        if (sketch.isSketchMoving() === false)
            return;

        _movingVertexIndex = -1;
        _removePreviewElements(_moveElements);
        _moveElements = [];
        sketch.show();
    };
    this.movePreview = function (latlng) {
        if (_isValidIndex(_movingVertexIndex)) {
            var target = _targetLatLng(latlng);
            var deltaLng = target.lng - _moveRefLatLng.lng;
            var deltaLat = target.lat - _moveRefLatLng.lat;
            _moveRefLatLng = sketch._snappedVertex != null ? L.latLng(sketch._snappedVertex.y, sketch._snappedVertex.x) : latlng;

            var latLngParts = _previewLatLngs(_moveElements);
            state.model.translateLatLngs(latLngParts, deltaLng, deltaLat, sketch.hasSelectedVertices());
            _setPreviewLatLngs(_moveElements, latLngParts);
        }
    };
    this.move = function (latlng) {
        if (_isValidIndex(_movingVertexIndex)) {
            sketch.addUndo(webgis.l10n.get("sketch-tool-move"));

            var vertex = state.vertices[_movingVertexIndex], target = _targetLatLng(latlng);
            var deltaX = target.lng - vertex.x;
            var deltaY = target.lat - vertex.y;
            _moveRefLatLng = null;

            state.model.translate(deltaX, deltaY, sketch.hasSelectedVertices());
        }

        _reload();
        sketch.endSketchMoving();
    };

    /********* Rotate *********/
    this.startRotating = function (index, startAngle) {
        sketch.endSketchMoving();

        if (_isValidIndex(index)) {
            sketch.endSketchRotating();

            sketch.hide();

            if (state.vertices.length > 1) {
                _rotateElements = _addPreviewElements();
            }

            _rotateRefAngle = 0.0;
            _startAngle = startAngle || 0.0;
            _rotatingVertexIndex = index;

            _resetConstruction();
        } else {
            sketch.endSketchRotating();
        }
    };
    this.isRotating = function () {
        return _rotatingVertexIndex >= 0;
    };
    this.endRotating = function () {
        if (sketch.isSketchRotating() === false)
            return;

        _rotatingVertexIndex = -1;
        _removePreviewElements(_rotateElements);
        _rotateElements = [];
        sketch.show();
    };
    // [reference vertex, target vertex] projected (center of the rotation = reference vertex)
    var _rotationVertices = function (latlng, crs) {
        var target = _targetLatLng(latlng);
        var vertices = [
            { x: sketch._referenceVertex().x, y: sketch._referenceVertex().y },
            { x: target.lng, y: target.lat }
        ];
        webgis.complementProjected(crs, vertices);
        return vertices;
    };
    var _rotationAngle = function (vertices) {
        return Math.atan2(vertices[1].Y - vertices[0].Y, vertices[1].X - vertices[0].X) + _startAngle;
    };
    this.rotatePreview = function (latlng) {
        if (_isValidIndex(_rotatingVertexIndex)) {
            var crs = sketch.map.calcCrs();
            var vertices = _rotationVertices(latlng, crs);

            var angle = _rotationAngle(vertices);
            var x = angle;
            angle -= _rotateRefAngle;
            _rotateRefAngle = x;

            var latLngParts = _previewLatLngs(_rotateElements);
            state.model.rotateLatLngs(latLngParts, vertices[0], angle, crs, sketch.hasSelectedVertices());
            _setPreviewLatLngs(_rotateElements, latLngParts);
        }
    };
    this.rotate = function (latlng) {
        if (_isValidIndex(_rotatingVertexIndex)) {
            sketch.addUndo(webgis.l10n.get("sketch-tool-rotate"));

            var crs = sketch.map.calcCrs();
            var vertices = _rotationVertices(latlng, crs);

            var angle = _rotationAngle(vertices);
            _rotateRefAngle = 0.0;

            state.model.rotate(vertices[0], angle, crs, sketch.hasSelectedVertices());
        }

        _reload();
        sketch.endSketchRotating();
    };

    // the vertex the sketch is moved/rotated with (_referenceVertex)
    this.referenceVertex = function () {
        return state.vertices[this.isMoving() ? _movingVertexIndex : _rotatingVertexIndex];
    };
};