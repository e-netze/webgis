// Geometry type 'text' (svg text)
// Only one vertex: a new vertex replaces the old one.
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults)

webgis.sketch.geometryTypes.define('text', '_base', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.redrawOnVertexMoved = true;
    this.vertexMarkerIcon = 'sketch_vertex_text';
    this.text = null;

    this.createFrameworkElement = function (latLngs) {
        var text = L.svgText(latLngs || [0, 0], {
            text: this.text || 'Text...',
            fontColor: sketch.getTextColor(),
            fontSize: sketch.getTextSize(),
            fontStyle: sketch.getTextStyle(),
            refResolution: state.txtRefResolution
        });
        text._webgis = { geometryType: state.geometryType };
        return text;
    };
    this.isValid = function () {
        return state.vertices.length > 0;
    };
    this.fromJson = function (json, crsId, readOnly) {
        this._fromJsonSingle(json, crsId);
    };
    this.pushVertex = function (coords) {
        state.vertices = [coords];
        var vertexFrameworkElements = state.vertexFrameworkElements;
        if (vertexFrameworkElements.length > 0) {
            sketch.map.frameworkElement.removeLayer(vertexFrameworkElements[0]);
        }
    };
    this.addsVertexMarker = function () {
        return true;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'text';
        this.text = frameworkElement.getText();
    };
});
