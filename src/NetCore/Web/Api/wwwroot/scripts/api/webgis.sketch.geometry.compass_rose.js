// Geometry type 'compass_rose'
// First vertex => center, following vertices => radius (see _radius in webgis.sketch.geometry.base.js)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults)

webgis.sketch.geometryTypes.define('compass_rose', '_radius', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.radius = 10.0;
    this.steps = 36;

    this.createFrameworkElement = function (latLngs) {
        var compassRose = L.compassRose(latLngs || [], {
            radius: sketch.getCompassRoseRadius(),
            steps: sketch.getCompassRoseSteps(),
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            calcCrs: sketch.map.calcCrs()
        });
        compassRose._webgis = { geometryType: state.geometryType };
        return compassRose;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'compass_rose';
        this.radius = frameworkElement.getRadius();
        this.steps = frameworkElement.getSteps();
    };
});
