// Geometry type 'intersection_angle' (Schnittwinkel: line 1 = vertices 0,1, line 2 = vertices 2,3)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('intersection_angle', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.unit = 'both';     // deg | gon | both
    this.angles = 'angle';  // angle | adjacent | both | all
    this.elementOptions = { unit: ['getUnit', 'setUnit'], angles: ['getAngles', 'setAngles'] };

    this.isValid = function () {
        return state.vertices.length >= 4;
    };

    // both lines finished: further clicks only move the end of the second line, the sketch stays open (properties can still be changed)
    this.tryAddVertex = function (vertex, vertices, fireEvents, coords) {
        if (state.vertices.length < 4)
            return false;

        if (fireEvents)
            this._replaceLastVertex(vertex);
        return true;
    };

    // mover line starts at the first vertex of the second line
    this.referenceVertexIndex = function () {
        return state.vertices.length >= 4 ? 2 : null;
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
