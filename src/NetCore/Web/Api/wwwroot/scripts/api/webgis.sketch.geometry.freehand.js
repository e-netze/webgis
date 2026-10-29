// Geometry type 'freehand'
// Vertices are added while the left mouse button is pressed, mouse up closes the sketch.
// All vertices are in one framework element (no new element per part).
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults)

webgis.sketch.geometryTypes.define('freehand', '_base', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.createsFrameworkElementPerPart = false;
    this.lastVertexIndex = -1;  // last vertex index of a loaded (fromJson) freehand geometry

    this.createFrameworkElement = function (latLngs) {
        var freeline = L.polyline(latLngs || [], { color: sketch.getColor(), opacity: sketch.getOpacity(), weight: sketch.getWeight(), dashArray: sketch._toDashArray(sketch.getLineStyle()) });
        freeline._webgis = { geometryType: state.geometryType, lineStyle: sketch.getLineStyle() };
        return freeline;
    };
    this.isValid = function () {
        return state.vertices.length > 1;
    };
    this.fromJson = function (json, crsId, readOnly) {
        this.lastVertexIndex = json.coordinates.length - 1;
        this._fromJsonList(json, crsId);
    };
    this.addsVertexMarker = function () {
        return sketch._addFreehandVertexMarker() === true;
    };

    this.onMapMouseUp = function () {
        sketch.close();
    };
    this.handleFreeDrawMouseMove = function (latlng) {
        if (sketch._mapMousePressed && sketch.isClosed() === false && sketch.isReadOnly() === false) {
            sketch.addVertex({ x: latlng.lng, y: latlng.lat });
            return true;
        }
        return false;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'freehand';
    };
});
