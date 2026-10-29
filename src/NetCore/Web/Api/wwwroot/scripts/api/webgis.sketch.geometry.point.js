// Geometry type 'point' (alias: 'symbol' is mapped in webgis.sketch.setGeometryType)
// The framework element is the draggable vertex marker itself.
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults)

webgis.sketch.geometryTypes.define('point', '_base', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.isSimplePoint = true;
    this.frameworkElementIsVertexMarker = true;
    this.wktType = 'POINT';

    this.createFrameworkElement = function (latLngs) {
        var marker = webgis.createMarker({ draggable: true, lat: 0, lng: 0, icon: 'sketch_vertex', vertex: 1 });
        marker.sketch = sketch;
        marker.vertex_index = 0;
        sketch._appendMarkerEvents(marker);
        return marker;
    };
    this.isValid = function () {
        return state.vertices.length > 0;
    };
    this.calcProperties = function (xParam, yParam) {
        var vertices = state.vertices;
        if (vertices.length === 1) {
            var pos = webgis.calc.project(vertices[0].x, vertices[0].y);
            return { posX: pos[xParam], posY: pos[yParam], srs: xParam === 'X' ? pos.srs : 4326 };
        }
        return {};
    };
    this.toJson = function (ret, crsId) {
        var vertices = state.vertices;
        if (vertices.length > 0) {
            ret.geometry.type = "Point";
            if (crsId)
                ret.geometry.SRID = crsId;
            ret.geometry.coordinates = crsId ? webgis.project(crsId, [vertices[0].x, vertices[0].y]) : [vertices[0].x, vertices[0].y];
            ret.geometry.COORDINATES = [vertices[0].X, vertices[0].Y];
            ret.valid = true;
        }
    };
    this.fromJson = function (json, crsId, readOnly) {
        this._fromJsonSingle(json, crsId);
    };
    this.fromJsonFast = function (json, crsId) {
        this._fromJsonFastSingle(json, crsId);
    };

    // a point has only one vertex => clear before adding
    this.beforeAddVertex = function (vertex) {
        var partIndex = state.partIndex;

        state.vertices.splice(0, state.vertices.length);  // clear
        partIndex.splice(0, partIndex.length);  // clear -> keep reference
        partIndex.push(0);
        state.currentFrameworkIndex = 0;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'point';
    };
});
