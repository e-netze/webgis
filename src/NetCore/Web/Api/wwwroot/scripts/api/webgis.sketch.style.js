// Sketch component: style values (color, fill, weight, line style, text style)
// Each value is an array with one entry per style index (geometry type styleIndex: 0 => default, 1 => tools like dimline, hectoline, ...).
// The sketch facade delegates the public get<Value>/set<Value> methods to this component
// and applies the values to the framework elements (sketch.setStyle).
// See docs/architecture/sketch-geometry-types.md

webgis.sketch.style = function (ctx) {
    "use strict";

    var _values = {
        color: ['#ff0000', '#000000'],
        fillColor: ['#ffff00', '#aaaaaa'],
        opacity: [0.8, 1.0],
        fillOpacity: [0.2, 0.2],
        weight: [2, 2],
        lineStyle: [null, null],
        textColor: ['#000', '#000'],
        textStyle: ['', ''],
        textSize: [12, 14]
    };

    var _styleIndex = function (geomType) {
        return geomType ? ctx.typeOf(geomType).styleIndex : ctx.type().styleIndex;
    };

    // geomType: optional, default => current geometry type
    this.get = function (name, geomType) {
        return _values[name][_styleIndex(geomType)];
    };
    // returns the value => this.setStyle({ color: _style.set('color', geomType, col) })
    this.set = function (name, geomType, value) {
        return _values[name][_styleIndex(geomType)] = value;
    };

    // line style '10,5' (relative to weight 10) <=> leaflet dashArray [2, 1] (weight 2)
    this.toDashArray = function (arrayString, geomType) {
        if (arrayString == null)
            return null;
        var ret = [], values = arrayString.split(',');
        for (var v in values) {
            var d = parseFloat(values[v]) * ctx.sketch.getWeight(geomType) / 10.0;
            ret.push(d);
        }
        return ret;
    };
    this.fromDashArray = function (array, geomType) {
        var ret = '';
        for (var i = 0; i < array.length; i++) {
            if (ret != '')
                ret += ',';
            ret += array[i] * 10.0 / ctx.sketch.getWeight(geomType);
        }
        return ret;
    };
};