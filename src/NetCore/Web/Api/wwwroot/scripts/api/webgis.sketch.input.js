// Sketch component: map input handlers (mouse, keys, right click)
// - decides what happens on mouse move (snapping, construct, geometry type handlers, mover/trace line, move/rotate preview)
// - keyboard shortcuts (undo, ortho, trace, snapping, finish part)
// - mouse down/up and the construct context menu (right click)
// The visual feedback is done by webgis.sketch.mover, the bubble move by webgis.sketch.markers.
// The modes (ortho/trace/fan) stay in the facade, read via ctx.state.
// sketch._mapMousePressed, _isConstructContextMenuVisible, _snappedVertex, _fixedDirectionVector and
// _fixedDistance stay public properties of the sketch.
// See docs/architecture/sketch-geometry-types.md

webgis.sketch.input = function (ctx, components) {
    "use strict";

    var $ = webgis.$;
    var sketch = ctx.sketch, state = ctx.state;
    var _mover = components.mover, _markers = components.markers;

    /********* Mouse down/up, right click *********/
    this.onMouseDown = function (map, e) {
        if (e && e.originalEvent && e.originalEvent.button === 0) {  // Left button

            sketch._mapMousePressed = true;
        }
    };
    this.onMouseUp = function (map, e) {
        if (e && e.originalEvent && e.originalEvent.button === 0) {  // Left button
            ctx.type().onMapMouseUp();
            sketch._mapMousePressed = false;
        }
    };
    this.onRightClick = function (map, clickEvent) {
        if (map.toolSketch() !== sketch || webgis.usability.sketchContextMenu === false) {
            return;
        }

        sketch.ui.showContextMenu(clickEvent);

        sketch._isConstructContextMenuVisible = true;
    };

    /********* Mouse move *********/

    // the projected vertex at the mouse position (or the snapped vertex)
    var _mouseVertex = function (latlng) {
        return sketch._snappedVertex || sketch.map.construct.toProjectedVertex(sketch.map.calcCrs(), latlng);
    };
    // ortho mode, fixed direction, fixed distance => moves the mover position (ll) relative to the reference vertex
    var _applyFixedDirectionAndDistance = function (latlng, ll) {
        var p1, p0, p;
        if (sketch.getCurrentPartLength() === 1 && state.ortho === true) {
            p1 = sketch._referenceVertex();
            p0 = _mouseVertex(latlng);
            var dx = p1.x - p0.x, dy = p1.y - p0.y, len = Math.sqrt(dx * dx + dy * dy);
            dx /= len;
            dy /= len;

            var vec = sketch._fixedDirectionVector ?
                { X: sketch._fixedDirectionVector.X, Y: sketch._fixedDirectionVector.Y } :  // current
                { X: 1.0, Y: 0.0 };  // x-axis

            // cross product (mouse x vec)
            var w1 = dx * vec.Y - dy * vec.X;
            // cross product (mouse x orthogonal(vec))
            var w2 = dx * (-vec.X) - dy * vec.Y;

            // compare cross products (sin(alpah))
            if (Math.abs(w1) <= Math.abs(w2)) {
                sketch._fixedDirectionVector = { X: vec.X, Y: vec.Y };  // take vector
            } else {
                sketch._fixedDirectionVector = { X: -vec.Y, Y: vec.X };  // take ortogonal vector
            }
        }
        if (sketch._fixedDirectionVector) {
            p1 = sketch._referenceVertex();
            p0 = _mouseVertex(latlng);
            p = sketch.map.construct.projectOrthogonal(p0, p1, sketch._fixedDirectionVector);
            if (p) {
                ll = { lng: p.x, lat: p.y };
                sketch._snappedVertex = p;
            }
        }
        if (sketch._fixedDistance > 0) {
            p1 = sketch._referenceVertex();
            p0 = _mouseVertex(latlng);
            p = sketch.map.construct.projectLength(p0, p1, sketch._fixedDistance);
            if (p) {
                ll = { lng: p.x, lat: p.y };
                sketch._snappedVertex = p;
            }
        }
        return ll;
    };
    // trace mode: path along the topology from the last vertex to the snapped vertex
    // null => no trace result
    var _traceLatLngs = function (map) {
        var vertices = state.vertices;
        if (!state.trace ||
            vertices.length === 0 ||
            !sketch._snappedVertex ||
            sketch.isSketchMoving() !== false ||
            sketch.isSketchRotating() !== false) {
            return null;
        }

        var lastVertex = vertices[vertices.length - 1];
        var lastNodeIndex = map.construct.getTopVertexIndex(lastVertex.X, lastVertex.Y);
        var currentNodeIndex = map.construct.getTopVertexIndex(sketch._snappedVertex.X, sketch._snappedVertex.Y);
        if (!(lastNodeIndex >= 0 && currentNodeIndex >= 0)) {
            return null;
        }

        var result = map.construct.traceNodes(lastNodeIndex, currentNodeIndex);
        if (!result) {
            return null;
        }

        var traceLatLngs = [];
        for (var i in result) {
            var vertex = map.construct.getTopoVertexWGS84(result[i]);
            if (vertex)
                traceLatLngs.push([vertex[1], vertex[0]]);
        }
        return traceLatLngs;
    };

    this.onMouseMove = function (map, latlng) {
        if (sketch._isMarkerDragging === true) {
            _markers.bubbleMoveTo(latlng);
            return;
        }

        if (sketch._isConstructContextMenuVisible === true) {
            return;
        }

        if (!webgis.sketch.construct.suppressSnapping(sketch)) {
            sketch._showMoveMarker(latlng);
        }

        if (!latlng || sketch.isEditable() === false || sketch.isReadOnly() === true) {
            sketch.hideMoverLine();
            sketch.hideMoverMarker();
            sketch.hideConstructMover();
            sketch.hideTraceLine();
            return;
        }

        if (webgis.sketch.construct.onMapMouseMove(sketch, latlng)) {
            return;
        }

        var type = ctx.type();
        if (type.handleMapMouseMove(latlng)) {
            return;
        }

        var constructionMode = webgis.sketch.construct.getConstructionMode(sketch);
        if (constructionMode != null && constructionMode != 'normal') {
            return;
        }

        if (type.handleFreeDrawMouseMove(latlng)) {
            return;
        }

        var latLngs = [], ll = sketch._snappedVertex ? { lng: sketch._snappedVertex.x, lat: sketch._snappedVertex.y } : { lat: latlng.lat, lng: latlng.lng };
        var referenceVertex = sketch._referenceVertex();

        if (referenceVertex) {
            ll = _applyFixedDirectionAndDistance(latlng, ll);

            latLngs.push([referenceVertex.y, referenceVertex.x]);
            latLngs.push([ll.lat, ll.lng]);
        }

        var isPolygon = ctx.type().isPolygon === true;
        if (ctx.type().isLine === true || isPolygon) {
            if (!sketch.isClosed()) {
                var traceLatLngs = _traceLatLngs(map);
                if (traceLatLngs && traceLatLngs.length > 0) {
                    latLngs = traceLatLngs;
                }
                _mover.updateTraceLine(traceLatLngs ? latLngs : null);

                var vertices = state.vertices, model = state.model;
                if (isPolygon && vertices.length >= 2 && !sketch.isSketchMoving() && !sketch.isSketchRotating()) {
                    latLngs.push([vertices[model.currentPartStart()].y, vertices[model.currentPartStart()].x]);
                }
            }

            if (!sketch.isClosed() || sketch.isSketchMoving() || sketch.isSketchRotating() && state.suppressMover !== true) {
                sketch.showMoverLine(latLngs);
                var sketchProperties = state.sketchProperties;
                _mover.updateLineColor(latlng, sketchProperties && sketchProperties.maxBBox);
            }

            if (sketch.isSketchMoving() === true) {
                sketch.moveSketchPreview({ lat: latLngs[1][0], lng: latLngs[1][1] });
            }
            else if (sketch.isSketchRotating() === true) {
                sketch.rotateSketchPreview({ lat: latLngs[1][0], lng: latLngs[1][1] });
            }
        }
        else {
            sketch.hideMoverLine();
            sketch.hideTraceLine();
        }

        sketch.fireCurrentState();
    };

    /********* Keys *********/
    this.onKeyDown = function (map, e) {
        if (!webgis.usability.allowSketchShortcuts) {
            return;
        }

        var $focus = $(":focus");
        if ($focus.hasClass('webgis-input') ||
            $focus.hasClass('webgis-textarea') ||
            $focus.hasClass('webgis-search-input') ||
            $focus.hasClass('webgis-autocomplete') ||
            $focus.closest('.select2-container').length > 0) {
                return false;
        }

        var removeMoverLine = false;

        switch (e.key.toLocaleLowerCase()) {
            case 'z':
                if (e.ctrlKey === true) {  // Undo
                    sketch.undo();
                }
                break;
            case 'o':
                if (e.ctrlKey === false) {
                    sketch.toggleOrthoMode();
                }
                break;
            case 't':
                if (e.ctrlKey === false) {
                    sketch.toggleTraceMode();
                }
                break;
            case 'n':  // s is reserved for ctrl+s => save in editing
                if (e.ctrlKey === false) {
                    webgis.tools.onButtonClick(sketch.map, { type: 'clientbutton', command: 'snapping' });
                }
                break;
            case 'f':
                if (e.ctrlKey === false) {
                    var canCloseSection = ctx.type().canCloseSection();
                    if (canCloseSection === false || (canCloseSection === null && ctx.type().isLineOrPolygon !== true))
                        break;

                    if (!sketch.isLastPartClosed()) {
                        sketch.appendPart();
                        removeMoverLine = true;
                    }
                }
                break;
            default:
                removeMoverLine = webgis.sketch.construct.onMapKeyDown(sketch, e) === true;
                break;
        }

        if (removeMoverLine) {
            sketch.hideMoverLine();
        }
    };
    this.onKeyUp = function (map, e) {
        webgis.sketch.construct.onMapKeyUp(sketch, e);
    };
};
