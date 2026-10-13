// Injected via page.AddInitScriptAsync BEFORE any page script runs.
// - deterministic Math.random (mulberry32, fixed seed)
// - tracks native addEventListener/removeEventListener per element (jQuery's own handler is excluded,
//   jQuery events are serialized separately via $._data(el, 'events'))
(function () {
    'use strict';

    var seed = 0x5eed1234;
    Math.random = function () {
        seed |= 0;
        seed = (seed + 0x6D2B79F5) | 0;
        var t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
        t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };

    var registry = new WeakMap();
    var isJQueryHandle = function (target, fn) {
        var jq = window.jQuery;
        return !!(jq && jq.hasData && jq.hasData(target) && jq._data(target).handle === fn);
    };
    var originalAdd = EventTarget.prototype.addEventListener;
    var originalRemove = EventTarget.prototype.removeEventListener;

    EventTarget.prototype.addEventListener = function (type, fn, opts) {
        if (this instanceof Element && fn && !isJQueryHandle(this, fn)) {
            var entry = registry.get(this);
            if (!entry) {
                entry = {};
                registry.set(this, entry);
            }
            entry[type] = (entry[type] || 0) + 1;
        }
        return originalAdd.call(this, type, fn, opts);
    };
    EventTarget.prototype.removeEventListener = function (type, fn, opts) {
        if (this instanceof Element && fn && !isJQueryHandle(this, fn)) {
            var entry = registry.get(this);
            if (entry && entry[type]) {
                entry[type]--;
                if (entry[type] === 0) {
                    delete entry[type];
                }
            }
        }
        return originalRemove.call(this, type, fn, opts);
    };

    window.__uiHarnessNativeListeners = registry;
})();
