// Geometry type 'azimuth' (Richtungswinkel: polyline/traverse, azimuth against north for each segment)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('azimuth', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.unit = 'deg';      // deg | gon | both
    this.labelLength = true;
    this.lengthUnit = 'm';  // m | km
    this.elementOptions = { unit: ['getUnit', 'setUnit'], labelLength: ['getLabelLength', 'setLabelLength'], lengthUnit: ['getLengthUnit', 'setLengthUnit'] };

    this.createFrameworkElement = function (latLngs) {
        var azimuth = L.azimuth(latLngs || [], {
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            fontColor: sketch.getTextColor(),
            fontSize: sketch.getTextSize(),
            unit: sketch.getAzimuthUnit(),
            labelLength: sketch.getAzimuthLabelLength(),
            lengthUnit: sketch.getAzimuthLengthUnit()
        });
        azimuth._webgis = { geometryType: state.geometryType };
        return azimuth;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'azimuth';
        this.unit = frameworkElement.getUnit();
        this.labelLength = frameworkElement.getLabelLength();
        this.lengthUnit = frameworkElement.getLengthUnit();
    };
});
