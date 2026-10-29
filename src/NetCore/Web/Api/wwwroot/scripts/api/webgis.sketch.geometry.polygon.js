// Geometry type 'polygon'
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _polygon)

webgis.sketch.geometryTypes.define('polygon', '_polygon', function (ctx, parent) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.isPlainPath = true;
    this.hasClosingSegment = true;
    this.wktType = 'POLYGON';

    this.createFrameworkElement = function (latLngs) {
        // Interactive = true -> Sonst lässt sich das Polygon beim Redling nicht mehr auswählen
        var polygon = L.polygon(latLngs || [], { interactive: true, color: sketch.getColor(), fillColor: sketch.getFillColor(), opacity: sketch.getOpacity(), fillOpacity: sketch.getFillOpacity(), weight: sketch.getWeight(), dashArray: sketch._toDashArray(sketch.getLineStyle()) });
        polygon._webgis = { geometryType: state.geometryType, lineStyle: sketch.getLineStyle() };
        // Kein Click Event -> Man kann sonst nicht mehr Punkte auf die Fläche setzen!!!
        return polygon;
    };

    /********* Rendering *********/
    // one element with all parts as rings => fill-rule evenodd draws inner rings as holes
    // fan mode => default (one element per ray)
    var _rings = function (model) {
        var rings = model.toLatLngs(false);
        if (model.vertices.length > 0 && model.isPartStart(model.vertices.length)) {  // closed => empty ring for the next part
            rings.push([]);
        }
        return rings;
    };
    this.render = function (model) {
        if (sketch.isInFanMode()) {
            return parent.render.call(this, model);
        }
        return [sketch._createFrameworkElement(_rings(model))];
    };
    this.renderAddedVertex = function (model, elements, currentIndex, vertex) {
        if (sketch.isInFanMode()) {
            return parent.renderAddedVertex.call(this, model, elements, currentIndex, vertex);
        }
        var element = elements[0], rings = element.getLatLngs();
        // add to the last ring (leaflet adds to the first ring by default)
        element.addLatLng(L.latLng(vertex.y, vertex.x), Array.isArray(rings[0]) ? rings[rings.length - 1] : undefined);
        element.sketch_vertex_coords = vertex;
        return 0;
    };
    this.renderAppendPart = function (model, elements, latLngs) {
        if (sketch.isInFanMode()) {
            return parent.renderAppendPart.call(this, model, elements, latLngs);
        }
        if (model.vertices.length === 0) {
            return [sketch._createFrameworkElement(latLngs)];
        }
        elements[0].setLatLngs(_rings(model));
        return elements;
    };
    this.infoTextLines = function (infoText, properties) {
        infoText.push(webgis.l10n.get("area") + ": " + (Math.round(properties.area * 100) / 100) + 'm²');
        infoText.push(webgis.l10n.get("circumference") + ": " + (Math.round(properties.circumference * 100) / 100) + 'm');
    };
    this.toJson = function (ret, crsId) {
        this._toJsonPath(ret, crsId, 'Polygon', 3);
    };
    this.fromJsonFast = function (json, crsId) {
        this._fromJsonFastRings(json, crsId);
    };
});
