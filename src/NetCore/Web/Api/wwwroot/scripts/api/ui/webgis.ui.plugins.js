webgis.ui = webgis.ui || {};

webgis.ui.__getPluginRegistry = function ($el) {
    var list = $el.data('__plugins__');
    if (!list) {
        list = [];
        $el.data('__plugins__', list);
    }
    return list;
};
webgis.ui.__registerPlugin = function ($el, instance) {
    //console.log('Register plugin', instance._pluginName);
    var list = webgis.ui.__getPluginRegistry($el);
    list.push(instance);
};
webgis.ui.__unregisterPlugin = function ($el, instance) {
    //console.log('Unregister plugin', instance._pluginName);
    var list = webgis.ui.__getPluginRegistry($el);
    var i = list.indexOf(instance);
    if (i > -1) list.splice(i, 1);
    if (list.length === 0) $el.removeData('__plugins__');
};
webgis.ui.destroyPluginsDeep = function ($root) {
    $root = $root instanceof $ ? $root : $($root);
    // Post-order: child first, then parent/root
    var nodes = $root.find('*').addBack().get().reverse();

    nodes.forEach(function (node) {
        var $node = $(node);
        var list = $node.data('__plugins__');
        if (!list || !list.length) return;

        // Copy list, in case destroy() unregisters itself or modifies the list
        var toDestroy = list.slice();
        toDestroy.forEach(function (inst) {
            if (inst && typeof inst.destroy === 'function' && !inst._destroyed) {
                try { inst.destroy(); } catch (e) { /* optional: logging */ }
            }
        });
    });
};
webgis.ui.definePlugin = function (name, spec) {
    let defaults = spec.defaults || {};

    function Ctor(el, options) {
        this.$el = $(el);
        this.options = $.extend(true, {}, defaults, options);

        this._pluginName = name;
        this._destroyed = false;
        webgis.ui.__registerPlugin(this.$el, this);      // << register plugin for deep destroy

        spec.init && spec.init.call(this);
    }

    Ctor.prototype.option = function (key, value) {
        if (arguments.length === 1) return this.options[key];

        this.options[key] = value;
        spec.update && spec.update.call(this, key, value);
    };

    Ctor.prototype.destroy = function () {
        if (this._destroyed) return;
        this._destroyed = true;
        // cleanup specific plugin
        spec.destroy && spec.destroy.call(this);
        // unregister (for cleanup) and free jQuery data
        webgis.ui.__unregisterPlugin(this.$el, this);
        this.$el.removeData(this._pluginName);
    };

    // add additional methods from spec.methods to the instance prototype
    if (spec.methods) {
        Object.keys(spec.methods).forEach(function (k) {
            Ctor.prototype[k] = spec.methods[k];
        });
    }

    // add static methods
    if (spec.staticMethods) {
        Object.keys(spec.staticMethods).forEach(function (k) {
            Ctor[k] = spec.staticMethods[k];
        });
    }


    (webgis.$ || jQuery).fn[name] = function (arg) {
        var args = Array.prototype.slice.call(arguments, 1);

        // Chainability: if no explicit return value from method, return this
        var chainable = true, result;

        this.each(function () {
            var $el = $(this);
            var inst = $el.data(name);

            if (!inst) {
                if (typeof arg === 'string') {
                    $.error('Method ' + arg + ' does not exist on ' + name);
                    return;
                }
                inst = new Ctor(this, arg);
                $el.data(name, inst);
            } else if (typeof arg === 'string') {
                var fn = inst[arg];
                if (typeof fn === 'function') {
                    var r = fn.apply(inst, args);
                    if (r !== undefined) { chainable = false; result = r; }
                } else {
                    $.error('Method ' + arg + ' does not exist on ' + name);
                }
            }
        });

        return chainable ? this : result;
    };
};