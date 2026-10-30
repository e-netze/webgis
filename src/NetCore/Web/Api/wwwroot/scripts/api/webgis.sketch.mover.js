// Sketch component: visual mouse feedback while sketching
// - mover line (green/red) from the reference vertex to the mouse position
// - mover marker at the (snapped) mouse position + yellow construct line along the snapped topology edge
// - trace line (blue) along the topology in trace mode
// The decisions (when to show what) are made in the input handlers (sketch.onMapMouseMove, ...).
// sketch._moverLine, sketch._moverMarker, sketch._constructMover and sketch._snappedVertex stay public
// properties of the sketch (used by webgis.sketch.ui/construct and the geometry types).
// See docs/architecture/sketch-geometry-types.md

webgis.sketch.mover = function (ctx) {
    "use strict";

    var sketch = ctx.sketch;

    var _traceLine = null;

    var _removeLayer = function (layer) {
        sketch.map.frameworkElement.removeLayer(layer);
    };

    /********* Mover line *********/
    this.hideLine = function () {
        if (sketch._moverLine) {
            _removeLayer(sketch._moverLine);
            sketch._moverLine = null;
        }
    };
    this.showLine = function (latLngs) {
        if (sketch._moverLine == null) {
            sketch._moverLine = L.polyline(latLngs, { color: 'green', weight: 1 });
            sketch._addToMap(sketch._moverLine);
        }
        else {
            sketch._moverLine.setLatLngs(latLngs);
        }
    };
    // red => the sketch would exceed the max. bbox
    this.updateLineColor = function (latlng, maxBBox) {
        if (maxBBox && !sketch.isSketchMoving() && !sketch.isSketchRotating()) {
            var bbox = sketch.getBounds(true, latlng);
            if (bbox.width > maxBBox.width ||
                bbox.height > maxBBox.height) {
                sketch._moverLine.setStyle({ color: 'red' });
            }
            else {
                sketch._moverLine.setStyle({ color: 'green' });
            }
        }
    };

    /********* Trace line *********/
    this.hideTraceLine = function () {
        if (_traceLine != null) {
            _removeLayer(_traceLine);
            _traceLine = null;
        }
    };
    // latLngs null => remove the trace line
    this.updateTraceLine = function (latLngs) {
        if (latLngs) {
            if (_traceLine == null) {
                _traceLine = L.polyline(latLngs, { color: 'blue', weight: 3 });
                sketch._addToMap(_traceLine);
            }
            else {
                _traceLine.setLatLngs(latLngs);
            }
        }
        else {
            this.hideTraceLine();
        }
    };

    /********* Mover marker (snapping) *********/
    this.hideMarker = function () {
        if (sketch._moverMarker) {
            _removeLayer(sketch._moverMarker);
            sketch._moverMarker = null;
        }
    };
    this.hideConstructMover = function () {
        if (sketch._constructMover) {
            _removeLayer(sketch._constructMover);
            sketch._constructMover = null;
        }
    };
    var _showConstructMover = function (latLngs) {
        if (sketch._constructMover == null) {
            sketch._constructMover = L.polyline(latLngs, {
                color: 'yellow', weight: 2
            });
            sketch._addToMap(sketch._constructMover);
        }
        else {
            sketch._constructMover.setLatLngs(latLngs);
        }
    };
    // snaps the mouse position (latlng is modified!) and shows the mover marker => sketch._showMoveMarker
    this.showMarker = function (latlng) {
        if (!latlng) {
            sketch.hideConstructMover();
            return;
        }
        if (!sketch.map.construct.hasSnapping(sketch) || sketch.map.toolSketch() != sketch) {
            this.hideMarker();
            sketch._snappedVertex = null;
            return;
        }
        var snapResult = sketch.map.construct.performSnap(latlng.lng, latlng.lat, null, sketch);
        if (snapResult != null) {
            latlng.lng = snapResult.result.x;
            latlng.lat = snapResult.result.y;
            sketch._snappedVertex = snapResult.result;
        }
        else {
            sketch._snappedVertex = null;
        }
        // Captured before being overwritten below, so we can tell whether the snap state actually
        // changed (entered/left a snap target) - needed so the "leaving snap" fix below doesn't
        // fire currentstate on every single mouse move (which caused visible overlay flicker).
        var hadSnapResult = sketch._moverMarker != null && sketch._moverMarker.snapResult ? true : false;

        if (sketch._moverMarker == null) {
            sketch._moverMarker = webgis.createMarker({
                lat: latlng.lat, lng: latlng.lng, draggable: false, icon: "sketch_mover"
            });
            sketch._moverMarker.setZIndexOffset(-1000);
            sketch._addToMap(sketch._moverMarker);
        }
        else {
            sketch._moverMarker.setLatLng(latlng);
        }
        sketch._moverMarker.snapResult = snapResult;
        if (snapResult) {
            if (snapResult.vertices && snapResult.vertices.length == 2) {
                var v1 = sketch.map.construct.getTopoVertexWGS84(snapResult.vertices[0]);
                var v2 = sketch.map.construct.getTopoVertexWGS84(snapResult.vertices[1]);
                if (v1 && v2) {
                    _showConstructMover([[v1[1], v1[0]], [v2[1], v2[0]]]);
                }
            }
            else {
                sketch.hideConstructMover();
            }

            sketch.fireCurrentState();
        }
        else {
            sketch.hideConstructMover();

            // Only fire when a snap target was actually left (transition snapped -> unsnapped) -
            // NOT on every plain mouse move without snapping, otherwise the sketch-info overlay
            // re-renders constantly while just moving the mouse and visibly flickers. Callers like
            // the vertex-drag handler ('drag' event on marker) rely solely on this call to keep the
            // overlay's snapping section in sync, since - unlike onMapMouseMove - they don't fire
            // currentstate again on their own afterwards.
            if (hadSnapResult) {
                sketch.fireCurrentState();
            }
        }
    };
};
