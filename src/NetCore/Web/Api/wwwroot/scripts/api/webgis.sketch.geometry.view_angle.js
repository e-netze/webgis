// Geometry type 'view_angle' (Sichtfeld: apex + axis end, cone with a given angle)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('view_angle', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.angle = 30;        // degrees
    this.unit = 'deg';      // deg | gon | both
    this.labelLength = false;
    this.elementOptions = { angle: ['getAngle', 'setAngle'], unit: ['getUnit', 'setUnit'], labelLength: ['getLabelLength', 'setLabelLength'] };

    // apex + axis end: further clicks only move the axis end, the sketch stays open (properties can still be changed)
    this.tryAddVertex = function (vertex, vertices, fireEvents, coords) {
        if (state.vertices.length < 2)
            return false;

        if (fireEvents)
            this._replaceLastVertex(vertex);
        return true;
    };

    // mover line always starts at the apex
    this.referenceVertexIndex = function () {
        return state.vertices.length >= 2 ? 0 : null;
    };

    this.createFrameworkElement = function (latLngs) {
        var viewAngle = L.viewAngle(latLngs || [], {
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            fontColor: sketch.getTextColor(),
            fontSize: sketch.getTextSize(),
            angle: sketch.getViewAngleAngle(),
            unit: sketch.getViewAngleUnit(),
            labelLength: sketch.getViewAngleLabelLength()
        });
        viewAngle._webgis = { geometryType: state.geometryType };
        return viewAngle;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'view_angle';
        this.angle = frameworkElement.getAngle();
        this.unit = frameworkElement.getUnit();
        this.labelLength = frameworkElement.getLabelLength();
    };
});
