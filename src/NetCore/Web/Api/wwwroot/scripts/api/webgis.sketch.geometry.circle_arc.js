// Geometry type 'circle_arc' (Kreisbogen: vertices 0,1,2 => start, intermediate point, end of the arc)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('circle_arc', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.unit = 'both';     // deg | gon | both
    this.labels = 'all';    // all | radius | length | angle
    this.elementOptions = { unit: ['getUnit', 'setUnit'], labels: ['getLabels', 'setLabels'] };

    this.isValid = function () {
        return state.vertices.length >= 3;
    };

    // arc finished: further clicks only move the end of the arc, the sketch stays open (properties can still be changed)
    this.tryAddVertex = function (vertex, vertices, fireEvents, coords) {
        if (state.vertices.length < 3)
            return false;

        if (fireEvents)
            this._replaceLastVertex(vertex);
        return true;
    };

    // mover line starts at the intermediate point
    this.referenceVertexIndex = function () {
        return state.vertices.length >= 3 ? 1 : null;
    };

    this.createFrameworkElement = function (latLngs) {
        var circleArc = L.circleArc(latLngs || [], {
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            fontColor: sketch.getTextColor(),
            fontSize: sketch.getTextSize(),
            unit: sketch.getCircleArcUnit(),
            labels: sketch.getCircleArcLabels()
        });
        circleArc._webgis = { geometryType: state.geometryType };
        return circleArc;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'circle_arc';
        this.unit = frameworkElement.getUnit();
        this.labels = frameworkElement.getLabels();
    };
});
