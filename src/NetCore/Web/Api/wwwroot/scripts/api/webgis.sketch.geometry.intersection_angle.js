// Geometry type 'intersection_angle' (Schnittwinkel: line 1 = vertices 0,1, line 2 = vertices 2,3)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('intersection_angle', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.unit = 'both';     // deg | gon | both
    this.angles = 'angle';  // angle | adjacent | both | all
    this.closeAfterVertices = 4;
    this.elementOptions = { unit: ['getUnit', 'setUnit'], angles: ['getAngles', 'setAngles'] };

    this.isValid = function () {
        return state.vertices.length >= 4;
    };

    this.tryAddVertex = function (vertex, vertices, fireEvents, coords) {
        return state.vertices.length >= this.closeAfterVertices;  // ignore further vertices
    };

    // first line finished => the next vertex starts the second line, no mover line from the last vertex
    this.handleMapMouseMove = function (latlng) {
        if (state.vertices.length !== 2 || sketch.isClosed() || sketch.isSketchMoving() || sketch.isSketchRotating()) {
            return false;
        }

        sketch.hideMoverLine();
        sketch.hideTraceLine();
        sketch.fireCurrentState();
        return true;
    };

    this.createFrameworkElement = function (latLngs) {
        var intersectionAngle = L.intersectionAngle(latLngs || [], {
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            fontColor: sketch.getTextColor(),
            fontSize: sketch.getTextSize(),
            unit: sketch.getIntersectionAngleUnit(),
            angles: sketch.getIntersectionAngleAngles()
        });
        intersectionAngle._webgis = { geometryType: state.geometryType };
        return intersectionAngle;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'intersection_angle';
        this.unit = frameworkElement.getUnit();
        this.angles = frameworkElement.getAngles();
    };
});
