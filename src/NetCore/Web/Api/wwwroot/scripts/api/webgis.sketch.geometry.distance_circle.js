// Geometry type 'distance_circle' (concentric circles)
// First vertex => center, following vertices => radius (see _radius in webgis.sketch.geometry.base.js)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults)

webgis.sketch.geometryTypes.define('distance_circle', '_radius', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.radius = 5000.0;
    this.steps = 3;
    this.elementOptions = { radius: ['getRadius', 'setRadius'], steps: ['getSteps', 'setSteps'] };

    this.createFrameworkElement = function (latLngs) {
        var distanceCircle = L.distanceCircle(latLngs || [], {
            radius: sketch.getDistanceCircleRadius(),
            steps: sketch.getDistanceCircleSteps(),
            color: sketch.getColor(),
            fillColor: sketch.getFillColor(),
            weight: sketch.getWeight()
        });
        distanceCircle._webgis = { geometryType: state.geometryType };
        return distanceCircle;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'distance_circle';
        this.radius = frameworkElement.getRadius();
        this.steps = frameworkElement.getSteps();
    };
});
