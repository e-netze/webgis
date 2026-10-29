// Geometry type 'elementseries' (series of graphic elements, eg. symbols along a line)
// Each vertex is an element. The framework element (L.graphicsElementSeries) is moved directly
// while dragging a vertex marker (no mover line).
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults)

webgis.sketch.geometryTypes.define('elementseries', '_base', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.isLineOrPolygon = true;
    this.addVertexUndoKey = 'sketch-add-element';
    this.wktType = 'POINT';

    // isValid: not overridden => always false (base) => intended: series sketch (MapSeriesPrint) is never printed (showSketch) or offered for download
    this.createFrameworkElement = function (latLngs) {
        let series = L.graphicsElementSeries(latLngs || [], {});
        sketch._setElementAdvancedProperties(series, state.sketchAdvancedProperties); // to set element size to the last vales
        series.on("element-rotated", function (ev) {
            var vertices = state.vertices;
            if (!vertices || vertices.length <= ev.elementIndex) return;
            this.addUndo(webgis.l10n.get("sketch-rotate-element"));
            ev.sender.setVertexRotation(vertices[ev.elementIndex], ev.rotation);
        }, sketch);
        return series;
    };
    this.addsVertexMarker = function () {
        return true;
    };

    this.onVertexMarkerDrag = function (marker, latlng, latLngs) {
        var frameworkElements = state.frameworkElements;
        if (frameworkElements.length === 1) {
            frameworkElements[0].setLatLngAt(marker.vertex_index, latlng);
        }
    };
    this.onVertexMarkerDragEnd = function (marker, latlng) {
        var frameworkElements = state.frameworkElements;
        if (frameworkElements.length === 1) {
            frameworkElements[0].setLatLngAt(marker.vertex_index, latlng);
        }
    };
});
