// Geometry type 'polygon'
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _polygon)

webgis.sketch.geometryTypes.define('polygon', '_polygon', function (ctx) {
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
