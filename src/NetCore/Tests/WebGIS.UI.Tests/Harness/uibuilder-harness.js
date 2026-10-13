// UI builder test harness. Loaded after jQuery and all WebGIS api/api-ui scripts.
// Provides:
//   uiHarness.map               recording fake map (Proxy): every call is logged with its path and described args
//   uiHarness.returns(p, v)     configure the return value of a fake call path, e.g. 'map.getActiveTool'
//   uiHarness.returnsFn(p, fn)  configure a function computing the return value
//   uiHarness.setProperty(p, v) configure a plain property value, e.g. 'map.toolParameters'
//   uiHarness.spy(p, impl)      replace a global function (e.g. 'webgis.tools.onButtonClick') by a recorder
//   uiHarness.build(elements, options)   $('#host').webgis_uibuilder({...})
//   uiHarness.snapshot()        normalized, deterministic description of the DOM + recorded calls
window.uiHarness = (function ($) {
    'use strict';

    var GUID = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;
    var JQUERY_EXPANDO = /^jQuery\d+/;
    // keys a fake must not answer (promise detection, JSON, DOM/jQuery duck typing, ...)
    var IGNORED_KEYS = {
        then: true, toJSON: true, nodeType: true, nodeName: true, jquery: true, length: true,
        constructor: true, prototype: true, __proto__: true, $$typeof: true, window: true
    };

    var calls = [];
    var callReturns = {};
    var propValues = {};
    var fakeCache = {};
    var fakePaths = new WeakMap();

    var hasOwn = function (o, k) { return Object.prototype.hasOwnProperty.call(o, k); };

    var normalizeString = function (s) {
        return String(s).replace(GUID, '{guid}');
    };

    var elementLabel = function (el) {
        var label = el.tagName ? el.tagName.toLowerCase() : el.nodeName;
        if (el.id) label += '#' + el.id;
        if (el.classList && el.classList.length) {
            label += '.' + Array.prototype.slice.call(el.classList).sort().join('.');
        }
        return normalizeString(label);
    };

    var describe = function (v, depth) {
        depth = depth || 0;
        if (v === null) return null;
        if (v === undefined) return '[undefined]';

        var t = typeof v;
        if (t === 'string') return normalizeString(v);
        if (t === 'boolean') return v;
        if (t === 'number') return isFinite(v) ? v : '[number:' + String(v) + ']';
        if (t === 'symbol' || t === 'bigint') return '[' + t + ']';
        if (fakePaths.has(v)) return '[fake:' + fakePaths.get(v) + ']';
        if (t === 'function') return '[function]';
        if (v === window) return '[window]';
        if (v === document) return '[document]';
        if (v instanceof Element) return '[element:' + elementLabel(v) + ']';
        if (v instanceof Node) return '[node:' + v.nodeName + ']';
        if (v instanceof Event) return '[event:' + v.type + ']';
        if (v instanceof $) {
            return '[jQuery(' + v.length + ')' +
                (v.length ? ':' + v.toArray().slice(0, 3).map(elementLabel).join(',') : '') + ']';
        }
        if (depth >= 4) return '[max-depth]';
        if (Array.isArray(v)) {
            return v.map(function (x) { return describe(x, depth + 1); });
        }

        var proto = Object.getPrototypeOf(v);
        if (proto === Object.prototype || proto === null) {
            var result = {};
            Object.keys(v).sort().forEach(function (k) {
                result[normalizeString(k)] = describe(v[k], depth + 1);
            });
            return result;
        }

        var name = proto && proto.constructor && proto.constructor.name ? proto.constructor.name : 'Object';
        return '[object ' + name + ']';
    };

    var describeArgs = function (args) {
        return Array.prototype.map.call(args, function (a) { return describe(a, 1); });
    };

    var makeFake = function (path) {
        if (hasOwn(fakeCache, path)) return fakeCache[path];

        var fake = new Proxy(function () { }, {
            get: function (target, key) {
                if (typeof key === 'symbol') return undefined;
                if (key === 'toString' || key === 'valueOf') {
                    return function () { return '[fake:' + path + ']'; };
                }
                if (hasOwn(IGNORED_KEYS, key)) return undefined;

                var childPath = path + '.' + key;
                if (hasOwn(propValues, childPath)) return propValues[childPath];
                return makeFake(childPath);
            },
            set: function (target, key, value) {
                propValues[path + '.' + String(key)] = value;
                return true;
            },
            has: function (target, key) {
                // proxy invariant: own non-configurable props of the target (e.g. 'prototype') must be reported
                return (key in target) || (typeof key === 'string' && hasOwn(propValues, path + '.' + key));
            },
            apply: function (target, thisArg, args) {
                calls.push({ call: path, args: describeArgs(args) });
                if (hasOwn(callReturns, path)) {
                    var r = callReturns[path];
                    return r && r.__computed === true ? r.fn.apply(thisArg, args) : r;
                }
                return undefined;
            }
        });

        fakePaths.set(fake, path);
        fakeCache[path] = fake;
        return fake;
    };

    var returns = function (path, value) {
        callReturns[path] = value;
    };
    var returnsFn = function (path, fn) {
        callReturns[path] = { __computed: true, fn: fn };
    };
    var setProperty = function (path, value) {
        propValues[path] = value;
    };

    var spy = function (path, impl) {
        var parts = path.split('.'), owner = window;
        for (var i = 0; i < parts.length - 1; i++) {
            owner = owner[parts[i]];
            if (!owner) throw new Error('uiHarness.spy: path not found: ' + path);
        }
        var name = parts[parts.length - 1];
        owner[name] = function () {
            calls.push({ call: path, args: describeArgs(arguments) });
            return typeof impl === 'function' ? impl.apply(this, arguments) : undefined;
        };
    };

    var applyDefaults = function () {
        returnsFn('map.ui.webgisContainer', function () { return $('#webgis-container'); });
        returnsFn('map.ui.mapContainer', function () { return $('#map-container'); });
        returns('map.ui.advancedToolBehaviour', false);
        returns('map.getActiveTool', null);
        returns('map.isDefaultTool', false);

        spy('webgis.tools.onButtonClick');

        // set by webgis.init from the server in the viewer; without keys => deterministic "hmac=false"
        if (!webgis.hmac && webgis.hmacController) {
            webgis.hmac = new webgis.hmacController();
        }
    };

    // ---------- DOM serialization ----------

    var serializeProps = function (el) {
        var tag = el.tagName.toLowerCase(), props = {};
        if (tag === 'input') {
            props.value = normalizeString(el.value);
            if (el.type === 'checkbox' || el.type === 'radio') props.checked = el.checked;
        } else if (tag === 'textarea') {
            props.value = normalizeString(el.value);
        } else if (tag === 'select') {
            props.value = normalizeString(el.value);
            props.selectedIndex = el.selectedIndex;
        } else if (tag === 'option') {
            props.selected = el.selected;
        }
        return props;
    };

    var serializeJQueryData = function (el) {
        if (!$.hasData(el)) return null;
        var data = $.data(el), result = {}, any = false;
        Object.keys(data).sort().forEach(function (k) {
            result[normalizeString(k)] = describe(data[k], 1);
            any = true;
        });
        return any ? result : null;
    };

    var serializeJQueryEvents = function (el) {
        var events = $._data(el, 'events');
        if (!events) return null;
        var result = {}, any = false;
        Object.keys(events).sort().forEach(function (type) {
            events[type].forEach(function (h) {
                var key = type + (h.namespace ? '.' + h.namespace : '') + (h.selector ? ' ' + h.selector : '');
                result[key] = (result[key] || 0) + 1;
                any = true;
            });
        });
        return any ? sortKeys(result) : null;
    };

    var serializeNativeListeners = function (el) {
        var entry = window.__uiHarnessNativeListeners && window.__uiHarnessNativeListeners.get(el);
        if (!entry) return null;
        var keys = Object.keys(entry).filter(function (k) { return entry[k] > 0; });
        if (!keys.length) return null;
        var result = {};
        keys.sort().forEach(function (k) { result[k] = entry[k]; });
        return result;
    };

    var serializeHandlerProperties = function (el) {
        var result = [];
        for (var k in el) {
            if (k.indexOf('on') === 0 && typeof el[k] === 'function') {
                result.push(k);
            }
        }
        return result.length ? result.sort() : null;
    };

    var serializeExpandos = function (el) {
        var result = {}, any = false;
        Object.keys(el).sort().forEach(function (k) {
            if (JQUERY_EXPANDO.test(k)) return;
            result[normalizeString(k)] = describe(el[k], 1);
            any = true;
        });
        return any ? result : null;
    };

    var sortKeys = function (o) {
        var r = {};
        Object.keys(o).sort().forEach(function (k) { r[k] = o[k]; });
        return r;
    };

    var serializeNode = function (node) {
        if (!node) return null;
        if (node.nodeType === 3) {
            // whitespace runs => single space, but keep leading/trailing space (matters between inline elements)
            var text = String(node.nodeValue).replace(/\s+/g, ' ');
            return text.trim() ? normalizeString(text) : null;
        }
        if (node.nodeType !== 1) return null;

        var o = { tag: node.tagName.toLowerCase() };

        if (node.attributes.length) {
            var attrs = {};
            Array.prototype.slice.call(node.attributes)
                .map(function (a) { return a.name; })
                .sort()
                .forEach(function (name) { attrs[name] = normalizeString(node.getAttribute(name)); });
            o.attrs = attrs;
        }

        var props = serializeProps(node);
        if (Object.keys(props).length) o.props = props;

        var data = serializeJQueryData(node);
        if (data) o.data = data;

        var events = serializeJQueryEvents(node);
        if (events) o.events = events;

        var listeners = serializeNativeListeners(node);
        if (listeners) o.listeners = listeners;

        var handlers = serializeHandlerProperties(node);
        if (handlers) o.handlers = handlers;

        var expandos = serializeExpandos(node);
        if (expandos) o.expandos = expandos;

        var children = [];
        Array.prototype.forEach.call(node.childNodes, function (c) {
            var s = serializeNode(c);
            if (s !== null) children.push(s);
        });
        if (children.length) o.children = children;

        return o;
    };

    var snapshot = function () {
        var host = document.getElementById('host');
        var container = document.getElementById('webgis-container');
        var others = [];
        Array.prototype.forEach.call(document.body.children, function (c) {
            if (c === host || c === container || c.tagName === 'SCRIPT') return;
            others.push(serializeNode(c));
        });

        return {
            host: serializeNode(host),
            container: serializeNode(container),
            body: others,
            calls: calls.slice()
        };
    };

    var build = function (elements, options) {
        options = $.extend({
            map: fakeMap,
            tool: null,
            title: '',
            closebutton: false
        }, options || {});
        options.elements = elements;

        $('#host').webgis_uibuilder(options);
    };

    var fakeMap = makeFake('map');
    applyDefaults();

    return {
        map: fakeMap,
        fake: makeFake,
        returns: returns,
        returnsFn: returnsFn,
        setProperty: setProperty,
        spy: spy,
        build: build,
        snapshot: snapshot,
        describe: describe,
        calls: function () { return calls.slice(); },
        clearCalls: function () { calls.length = 0; }
    };
})(window.jQuery);
