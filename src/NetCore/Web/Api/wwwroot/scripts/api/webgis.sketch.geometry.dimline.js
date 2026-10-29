// Geometry type 'dimline' (dimension line, labels the segment lengths)
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('dimline', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.lengthUnit = 'm';
    this.labelTotalLength = false;
    this.elementOptions = { lengthUnit: ['getLengthUnit', 'setLengthUnit'], labelTotalLength: ['getLabelTotalLength', 'setLabelTotalLength'] };

    this.createFrameworkElement = function (latLngs) {
        var dimLine = L.dimLine(latLngs || [], {
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            fontSize: sketch.getTextSize(),
            fontColor: sketch.getTextColor(),
            lengthUnit: sketch.getDimLineLengthUnit(),
            labelTotalLength: sketch.getDimLineLabelTotalLength()
        });
        dimLine._webgis = { geometryType: state.geometryType };
        return dimLine;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'dimline';
        this.lengthUnit = frameworkElement.getLengthUnit();
        this.labelTotalLength = frameworkElement.getLabelTotalLength();
    };
});
