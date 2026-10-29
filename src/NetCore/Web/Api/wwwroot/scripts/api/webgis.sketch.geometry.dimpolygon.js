// Geometry type 'dimpolygon' (dimension polygon, labels area and edge lengths)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _polygon)

webgis.sketch.geometryTypes.define('dimpolygon', '_polygon', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.areaUnit = 'm²';
    this.labelEdges = true;

    this.createFrameworkElement = function (latLngs) {
        var dimPolygon = L.dimPolygon(latLngs || [], {
            fillColor: sketch.getFillColor(),
            fillOpacity: sketch.getFillOpacity(),
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            fontSize: sketch.getTextSize(),
            fontColor: sketch.getTextColor(),
            areaUnit: sketch.getDimPolygonAreaUnit(),
            labelEdges: sketch.getDimPolygonLabelEdges()
        });
        dimPolygon._webgis = { geometryType: state.geometryType };
        return dimPolygon;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'dimpolygon';
        this.areaUnit = frameworkElement.getAreaUnit();
        this.labelEdges = frameworkElement.getLabelEdges();
    };
});
