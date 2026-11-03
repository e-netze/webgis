// Geometry type 'dimline' (dimension line, labels the segment lengths)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('dimline', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.lengthUnit = 'm';
    this.labelTotalLength = false;
    this.angleUnit = 'none';
    this.angleType = 'inner';
    this.elementOptions = {
        lengthUnit: ['getLengthUnit', 'setLengthUnit'], labelTotalLength: ['getLabelTotalLength', 'setLabelTotalLength'],
        angleUnit: ['getAngleUnit', 'setAngleUnit'], angleType: ['getAngleType', 'setAngleType']
    };

    this.createFrameworkElement = function (latLngs) {
        var dimLine = L.dimLine(latLngs || [], {
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            fontSize: sketch.getTextSize(),
            fontColor: sketch.getTextColor(),
            lengthUnit: sketch.getDimLineLengthUnit(),
            labelTotalLength: sketch.getDimLineLabelTotalLength(),
            angleUnit: sketch.getDimLineAngleUnit(),
            angleType: sketch.getDimLineAngleType()
        });
        dimLine._webgis = { geometryType: state.geometryType };
        return dimLine;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'dimline';
        this.lengthUnit = frameworkElement.getLengthUnit();
        this.labelTotalLength = frameworkElement.getLabelTotalLength();
        this.angleUnit = frameworkElement.getAngleUnit();
        this.angleType = frameworkElement.getAngleType();
    };
});
