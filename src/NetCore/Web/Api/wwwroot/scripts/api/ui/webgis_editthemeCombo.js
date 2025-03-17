(function ($) {
    "use strict";
    $.fn.webgis_editthemeCombo = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_editthemeCombo');
        }
    };
    var defaults = {
        map: null,
        customitems: [],
        onchange: null,
        dbrights: null
    };
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };

    var initUI = function (elem, options) {
        var $elem = $(elem);
        elem._map = options.map;
        elem._opt = options;
        if (options.onchange)
            $elem.change(options.onchange);

        if (options.customitems) {
            for (var i in options.customitems) {
                var item = options.customitems[i];
                var $parent = $elem;
                if (item.category) {
                    var $parent = $elem.find("optgroup[label='" + item.category + "']");
                    if ($parent.length == 0)
                        $parent = $("<optgroup label='" + item.category + "' />").addClass('webgis-custom-option').appendTo($elem);
                }
                var $opt = $("<option value='" + item.value + "'>" + item.name + "</option>").addClass('webgis-custom-option').appendTo($parent);
            }
        }
        if (options.map !== null) {
            for (var serviceId in options.map.services) {
                var service = options.map.services[serviceId];
                addService({}, service, elem);
            }
        }

        //$("<optgroup label='Geometrie' />").attr('data-name', 'geometry').appendTo($(elem));

        elem._map.events.on('onaddservice', addService, elem);
        elem._map.events.on('onremoveservice', removeService, elem);
    };

    var addService = function (e, service, elem) {
        var elem = elem || this;
        if (service == null || service.editthemes == null || service.editthemes.length === 0)
            return;

        var typePrefix = []; 
        typePrefix["point"] = "☆ ";
        typePrefix["line"] = "— ";//"⋋ ";
        typePrefix["polygon"] = "▭ ";

        var $elem = $(elem);
        var count = $elem.find('option').length;
        var options = $elem.get(0)._opt;
        var isInitialized = $elem.hasClass('initialized');
        var $group = $("<optgroup label='" + service.name + "' />");
        var customOptions = $elem.children('.webgis-custom-option');
        if (customOptions.length === 0) {
            $group.prependTo($elem);
        } else {
            $group.insertAfter(customOptions[customOptions.length - 1]);
        }

        $group.get(0).service = service;
        for (var i = 0, to = service.editthemes.length; i < to; i++) {
            var edittheme = service.editthemes[i];
      
            if (options.dbrights && edittheme.dbrights) {
                if (edittheme.dbrights.indexOf(options.dbrights) < 0) {
                    continue;
                }
            }

            var layer = service.getLayer(edittheme.layerid);
            var prefix = layer && typePrefix[layer.type] ? typePrefix[layer.type] : '';

            var val = service.id + ',' + edittheme.layerid + ',' + edittheme.themeid;
            var $opt = $("<option value='" + val + "'>" + prefix + edittheme.name + "</option>")
                .addClass('filter-' + ((layer && layer.type) ? layer.type : 'unknown'))
                .appendTo($group);

            $opt.get(0).edittheme = edittheme;
            if (!isInitialized && (val === webgis.initialParameters.query || edittheme.themeid === webgis.initialParameters.editthemeid)) {
                $elem.addClass('initialized').val(val).trigger('change');
                isInitialized = true;
            }
        }
        if (count === 0 && !$elem.hasClass('initialized'))
            $elem.trigger('change');

        if ($.fn.webgis_catCombo)
            $elem.webgis_catCombo({
                classFilters: {
                    "filter-point": { label: typePrefix['point']+ 'Punkte/Symbole' },
                    "filter-line": { label: typePrefix['line'] +'Linien' },
                    "filter-polygon": { label: typePrefix['polygon'] +'Flächen/Polygone' }
                }
            });
    };

    var removeService = function (e, service, elem) {
        var elem = elem || this;
        var $elem = $(elem);
        $elem.find('optgroup').each(function (i, e) {
            if (e.service && e.service.id === service.id)
                $(e).remove();
        });
        $elem.trigger('change');
    };
})(webgis.$ || jQuery);
