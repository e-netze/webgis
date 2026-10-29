// Geometry type 'hectoline' (line with labeled stations in a given interval)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('hectoline', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.unit = 'm';
    this.interval = 100;

    this.createFrameworkElement = function (latLngs) {
        var hectoline = L.hectoLine(latLngs || [], {
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            fontColor: sketch.getTextColor(),
            fontSize: sketch.getTextSize(),
            interval: sketch.getHectolineInterval(),
            unit: sketch.getHectolineUnit()
        });
        hectoline._webgis = { geometryType: state.geometryType };
        return hectoline;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'hectoline';
        this.unit = frameworkElement.getUnit();
        this.interval = frameworkElement.getInterval();
    };
});
