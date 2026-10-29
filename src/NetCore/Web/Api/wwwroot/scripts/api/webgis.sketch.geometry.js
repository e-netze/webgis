// Registry for the geometry types of webgis.sketch (webgis.sketch.js).
//
// Each geometry type (point, polyline, polygon, ...) is defined in its own file
// webgis.sketch.geometry.<type>.js and contains the type specific behavior
// (framework element, validation, json/wkt, vertex handling, ...).
// The common defaults and the families (_line, _polygon, _radius) live in
// webgis.sketch.geometry.base.js.
//
// webgis.sketch keeps its public API and delegates the type specific parts to
// an instance of the current geometry type (one instance per sketch and type).
//
// define(name, baseName, ctor):  ctor is called with (ctx, parent), "this" is the new
//                                instance (Object.create(parent)).
//                                ctx = { sketch: <webgis.sketch>, state: <closure state of the sketch> }
// alias(alias, name):            an other name for an existing type (eg. linestring => polyline)
// create(name, ctx):             creates an instance. Unknown names fall back to '_base'

webgis.sketch.geometryTypes = new function () {
    "use strict";

    var _definitions = {}, _aliases = {};

    this.define = function (name, baseName, ctor) {
        _definitions[name] = { baseName: baseName || null, ctor: ctor };
    };

    this.alias = function (alias, name) {
        _aliases[alias] = name;
    };

    this.resolve = function (name) {
        if (_aliases.hasOwnProperty(name)) {
            name = _aliases[name];
        }
        return _definitions.hasOwnProperty(name) ? name : '_base';
    };

    this.create = function (name, ctx) {
        name = this.resolve(name);

        var definition = _definitions[name];
        var parent = definition.baseName ? this.create(definition.baseName, ctx) : {};
        var instance = Object.create(parent);

        instance.name = name;
        definition.ctor.call(instance, ctx, parent);

        return instance;
    };
};
