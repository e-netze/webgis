// Geometry type 'circle'
// First vertex => center, following vertices => radius (see _radius in webgis.sketch.geometry.base.js)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults)

webgis.sketch.geometryTypes.define('circle', '_radius', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.wktType = 'POLYGON';
    this.radius = 0;

    this.createFrameworkElement = function (latLngs) {
        var circle = L.circle(latLngs || [0, 0], { radius: this.radius, interactive: true, color: sketch.getColor(), fillColor: sketch.getFillColor(), opacity: sketch.getOpacity(), fillOpacity: sketch.getFillOpacity(), weight: sketch.getWeight(), dashArray: sketch._toDashArray(sketch.getLineStyle()) });
        circle._webgis = { geometryType: state.geometryType, lineStyle: sketch.getLineStyle() };
        return circle;
    };
    this.metaInfo = function (info) {
        info.radius = this.radius;
        if (state.vertices.length > 0) {
            info.center = { lat: state.vertices[0].y, lng: state.vertices[0].x };
        }
    };

    // circle as polygon (spherical)
    this.wktVertices = function (useWGS84) {
        var vertices = [];
        var frameworkElements = state.frameworkElements;
        var center = frameworkElements[0]._latlng;
        var radius = frameworkElements[0].getRadius();

        var alpha = radius / 6371000.0, lat = 90.0 - center.lat, lng = 90.0 - center.lng;
        var sin_a = Math.sin(alpha), cos_a = Math.cos(alpha);
        var sin_lat = Math.sin(lat * Math.PI / 180.0), cos_lat = Math.cos(lat * Math.PI / 180.0);
        var sin_lng = Math.sin(lng * Math.PI / 180.0), cos_lng = Math.cos(lng * Math.PI / 180.0);

        for (var t = 0; t < Math.PI * 2.0; t += .01) {  // ToDO: Strittweite: 0.01??
            // https://math.stackexchange.com/questions/643130/circle-on-sphere

            var x = (sin_a * cos_lat * cos_lng) * Math.cos(t) + (sin_a * sin_lng) * Math.sin(t) - (cos_a * sin_lat * cos_lng);
            var y = -(sin_a * cos_lat * sin_lng) * Math.cos(t) + (sin_a * cos_lng) * Math.sin(t) + (cos_a * sin_lat * sin_lng);
            var z = (sin_a * sin_lat) * Math.cos(t) + cos_a * cos_lat;

            var lat_ = Math.asin(z) * 180.0 / Math.PI;
            var lng_ = -Math.atan2(x, y) * 180.0 / Math.PI;

            if (useWGS84) {
                vertices.push({ x: lng_, y: lat_ });
            } else {
                var projected = webgis.fromWGS84(sketch.map.calcCrs(), lat_, lng_);
                vertices.push({ X: projected.x, Y: projected.y });
            }
        }

        return vertices;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'circle';
        this.radius = frameworkElement.getRadius();
    };
});
