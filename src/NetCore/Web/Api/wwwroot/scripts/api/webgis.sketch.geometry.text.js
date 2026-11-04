// Geometry type 'text' (svg text)
// Only one vertex: a new vertex replaces the old one.
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults)

webgis.sketch.geometryTypes.define('text', '_base', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.redrawOnVertexMoved = true;
    this.vertexMarkerIcon = 'sketch_vertex_text';
    this.text = null;
    this.rotation = 0;
    this.elementOptions = { text: ['getText', 'setText'], rotation: ['getRotation', 'setRotation'] };

    this.createFrameworkElement = function (latLngs) {
        var text = L.svgText(latLngs || [0, 0], {
            text: this.text || 'Text...',
            fontColor: sketch.getTextColor(),
            fontSize: sketch.getTextSize(),
            fontStyle: sketch.getTextStyle(),
            rotation: this.rotation || 0,
            refResolution: state.txtRefResolution
        });
        text._webgis = { geometryType: state.geometryType };
        return text;
    };
    // the committed element (clone) never gets a rotation handle
    this.cloneGraphicElements = function () {
        var element = this.createFrameworkElement();
        if (state.vertices.length > 0) {
            var vertex = state.vertices[state.vertices.length - 1];
            element.setLatLng(L.latLng(vertex.y, vertex.x));
        }
        return [element];
    };
    // rotation handle: only for the editable sketch of the map graphics (MapMarkup) with a placed text
    this.refreshRotationHandle = function () {
        var element = state.frameworkElements[state.currentFrameworkIndex];
        if (!element || !element.showRotationHandle)
            return;

        var show = state.geometryType === 'text' &&
            state.vertices.length > 0 &&
            !sketch.isReadOnly() &&
            sketch.map && sketch.map.graphics && sketch.map.graphics._sketch === sketch;

        if (show && !element._webgisRotationEvents) {
            element._webgisRotationEvents = true;
            element.on('rotating rotated', function (e) {
                this.rotation = e.rotation;
                sketch.events.fire(e.type === 'rotated' ? 'ontextrotated' : 'ontextrotating', sketch, e.rotation);
            }, this);
        }
        element.showRotationHandle(show === true);
    };
    // move the text (and its rotation handle) live with the vertex marker
    this.onVertexMarkerDrag = function (marker, latlng, latLngs) {
        var element = state.frameworkElements[state.currentFrameworkIndex];
        if (element && element.setLatLng) {
            element.setLatLng(L.latLng(latlng.lat, latlng.lng));
        }
    };
    this.isValid = function () {
        return state.vertices.length > 0;
    };
    this.fromJson = function (json, crsId, readOnly) {
        this._fromJsonSingle(json, crsId);
    };
    this.pushVertex = function (coords) {
        state.model.setVertices([coords]);
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
        this.rotation = frameworkElement.getRotation ? frameworkElement.getRotation() : 0;
    };
});
