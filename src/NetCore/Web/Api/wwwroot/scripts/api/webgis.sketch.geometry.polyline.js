// Geometry type 'polyline' (alias: 'linestring', other names like 'line', 'multilinestring'
// are mapped in webgis.sketch.setGeometryType)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('polyline', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.isPlainPath = true;
    this.supportsMergeParts = true;
    this.wktType = 'LINESTRING';

    this.createFrameworkElement = function (latLngs) {
        state.geometryType = 'polyline';
        var polyline = L.polyline(latLngs || [], { color: sketch.getColor(), opacity: sketch.getOpacity(), weight: sketch.getWeight(), dashArray: sketch._toDashArray(sketch.getLineStyle()) });
        polyline._webgis = { geometryType: state.geometryType, lineStyle: sketch.getLineStyle() };
        return polyline;
    };
    this.isValid = function () {
        state.geometryType = 'polyline';
        // ToDo: Parts
        return state.vertices.length > 1;
    };
    this.infoTextLines = function (infoText, properties) {
        infoText.push(webgis.l10n.get("length") + ": " + (Math.round)(properties.length * 100) / 100 + 'm');
    };
    this.toJson = function (ret, crsId) {
        this._toJsonPath(ret, crsId, 'LineString', 2);
    };
    this.fromJson = function (json, crsId, readOnly) {
        if (json.type.toLowerCase() === 'multilinestring') {
            this._fromJsonRings(json, crsId, readOnly);
        } else {
            this._fromJsonList(json, crsId);
        }
    };
    this.fromJsonFast = function (json, crsId) {
        if (json.type.toLowerCase() === 'multilinestring') {
            this._fromJsonFastRings(json, crsId);
        } else {
            this._fromJsonFastList(json, crsId);
        }
    };
});

webgis.sketch.geometryTypes.alias('linestring', 'polyline');
