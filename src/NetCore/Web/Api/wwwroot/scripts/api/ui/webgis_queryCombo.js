﻿(function ($) {
    "use strict";
    $.fn.webgis_queryCombo = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_queryCombo');
        }
    };
    var defaults = {
        map: null,
        type: 'query',
        customitems: [],
        onchange: null,
        valuefunction: null
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        selectfirst: function (options) {
            if ($(this).find('option[value]:first').length === 1) {
                var firstValue = $(this).find('option[value]:first').attr('value');
                if (firstValue) {
                    $(this).val(firstValue).trigger('change');
                }
            }
        },
        refresh: function (options) { 
            var $this = $(this);
            if ($this.hasClass('refreshing')) return;

            $this.addClass('refreshing');
            webgis.delayed(function () {
                addVisibleQueries({}, this.get(0)._map, this);
                this.removeClass('refreshing');
            }.bind(this), 300, $this);
        }
    };

    var initUI = function (elem, options) {
        var $elem = $(elem).addClass('webgis-query-combo');
        elem._map = options.map;
        elem._opt = options;
        if (options.onchange) {
            $elem.change(options.onchange);
        }

        var isInitialized = $elem.hasClass('initialized');

        if (options.customitems) {
            //console.log('querycombo.cutomitems', options.customitems);

            for (var i in options.customitems) {
                var item = options.customitems[i];
                var $parent = $elem;
                if (item.category) {
                    $parent = $elem.find("optgroup[label='" + item.category + "']");
                    if ($parent.length === 0)
                        $parent = $("<optgroup label='" + item.category + "' />").addClass('webgis-custom-option').appendTo($elem);
                }
                var $opt = $("<option value='" + item.value + "'>" + item.name + "</option>").addClass('webgis-custom-option').appendTo($parent);
                if (webgis.usability?.listVisibleQueriesInQueryCombo === true
                    && item.value === '#') {
                    $opt.css('font-weight', 'bold');
                    $elem.addClass('require-ui-refresh');
                }
                if (!isInitialized && (item.value === webgis.initialParameters.querythemeid)) {
                    $elem.addClass('initialized').val(item.value).trigger('change');
                    isInitialized = true;
                }
            }
        }

        if (options.map !== null) {
            for (let serviceId in options.map.services) {
                const service = options.map.services[serviceId];
                addService({}, service, elem);
            }
        }
        elem._map.events.on('onaddservice', addService, elem);
        elem._map.events.on('onremoveservice', removeService, elem);
    };

    var addService = function (e, service, elem) {
        elem = elem || this;
        if (!service || !service.queries || service.queries.length === 0)
            return;

        var $elem = $(elem);
        var count = $elem.find('option').length;
        var options = $elem.get(0)._opt;
        var type = options.type;
        var isInitialized = $elem.hasClass('initialized');

        var $group = $("<optgroup label='" + service.name + "' />");
        var customOptions = $elem.children('.webgis-custom-option');
        if (customOptions.length === 0) {
            $group.prependTo($elem);
        } else {
            $group.insertAfter(customOptions[customOptions.length - 1]);
        }


        $group.get(0).service = service;
        for (var i = 0, to = service.queries.length; i < to; i++) {
            var query = service.queries[i];
            if (query.visible === false)
                continue;
            if (type === 'query' && (query.items == null || query.items.length === 0))
                continue;
            var val = service.id + ':' + query.id;
            if (options.valuefunction) {
                val = options.valuefunction(service, query);
            }
            var $opt = $("<option value='" + val + "'>" + query.name + "</option>").appendTo($group);
            $opt.get(0).query = query;
            
            if (!isInitialized && (val === webgis.initialParameters.querythemeid || query.id === webgis.initialParameters.querythemeid)) {
                $elem.addClass('initialized').val(val).trigger('change');
                isInitialized = true;
            }
        }
        if ($group.find('option').length === 0)
            $group.remove();
        if (count === 0 && !$elem.hasClass('initialized'))
            $elem.trigger('change');

        $elem.webgis_catCombo();
    };
    var removeService = function (e, service, elem) {
        elem = elem || this;
        var $elem = $(elem);
        $elem.find('optgroup').each(function (i, e) {
            if (e.service && e.service.id === service.id)
                $(e).remove();
        });
        $elem.trigger('change');
    };
    var addVisibleQueries = function (e, map, elem) {
        //console.log('webgis_queryCombo.addVisibleQueries', e, map, elem);
        if (webgis.usability?.listVisibleQueriesInQueryCombo !== true) return;

        const $elem = $(elem || this);

        $elem.children('.scale-dependent').addClass('check-visibility');
        const visiableQueriesOption = $elem.children("[value='#']")
        if (!map || visiableQueriesOption.length !== 1) return;
       
        for (let serviceId in map.services) {
            const service = map.services[serviceId];

            for (const query of service.queries) {
                if (service.layerVisibleAndInScale(query.layerid)) {
                    const $existingOption = $elem.find("option.scale-dependent[value='" + service.id + ':' + query.id + "']");

                    if ($existingOption.length > 0) {
                        $existingOption.removeClass('check-visibility');
                        continue;
                    }

                    $("<option>")
                        .addClass('scale-dependent')
                        .css('fontStyle', 'italic')
                        .attr('value', service.id + ':' + query.id)
                        .text('☑ ' + query.name)
                        .insertAfter(visiableQueriesOption);
                }
            }
        }

        $elem.children('.scale-dependent.check-visibility').remove();
    }
})(webgis.$ || jQuery);

(function ($) {
    "use strict";
    $.fn.webgis_chainageCombo = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_chainageCombo');
        }
    };
    var defaults = {
        map: null,
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
        if (options.map !== null) {
            for (var serviceId in options.map.services) {
                var service = options.map.services[serviceId];
                addService({}, service, elem);
            }
        }
        elem._map.events.on('onaddservice', addService, elem);
        elem._map.events.on('onremoveservice', removeService, elem);
    };
    var addService = function (e, service, elem) {
        var elem = elem || this;
        if (service == null || service.chainagethemes == null || service.chainagethemes.length === 0)
            return;
        var $elem = $(elem);
        var count = $elem.find('option').length;
        var options = $elem.get(0)._opt;
        var type = options.type;
        var $group = $("<optgroup label='" + service.name + "' />").appendTo($elem);
        $group.get(0).service = service;
        for (var i = 0, to = service.chainagethemes.length; i < to; i++) {
            var chanainage = service.chainagethemes[i];
            var val = chanainage.id;
            var $opt = $("<option value='" + val + "'>" + chanainage.name + "</option>").appendTo($group);
        }
        if ($group.find('option').length === 0)
            $group.remove();
    };
    var removeService = function (e, service, elem) {
        var elem = elem || this;
        var $elem = $(elem);
        $elem.find('optgroup').each(function (i, e) {
            if (e.service && e.service.id === service.id)
                $(e).remove();
        });
    };
})(webgis.$ || jQuery);

(function ($) {
    "use strict";
    $.fn.webgis_visfilterCombo = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_visfilterCombo');
        }
    };
    var defaults = {
        map: null
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem);
        elem._map = options.map;
        elem._opt = options;
        $("<option value='#'>Alle Filter</option>").appendTo($elem);
        if (options.map !== null) {
            for (var serviceId in options.map.services) {
                var service = options.map.services[serviceId];
                addService({}, service, elem);
            }
        }
        elem._map.events.on('onaddservice', addService, elem);
        elem._map.events.on('onremoveservice', removeService, elem);
        if (options.val)
            $elem.val(options.val);
    };
    var addService = function (e, service, elem) {
        elem = elem || this;
        if (!service || !service.filters || service.filters.length === 0)
            return;

        var $elem = $(elem);
        var count = $elem.find('option').length;
        var options = $elem.get(0)._opt;
        var type = options.type;
        var $group = $("<optgroup label='" + service.name + "' />").appendTo($elem);
        $group.get(0).service = service;
        for (var i = 0, to = service.filters.length; i < to; i++) {
            var filter = service.filters[i];
            if (filter.visible === false)
                continue;

            var val = service.id + "~" + filter.id;
            var $opt = $("<option value='" + val + "'>&nbsp;" + filter.name + "</option>").appendTo($group);
            if (service.map.hasFilter(val))
                $opt.css({
                    backgroundImage: "url('" + webgis.css.img('check2-24.png') + "')",
                    backgroundPosition: "0px -2px",
                    backgroundRepeat: "no-repeat",
                    height: 24
                });
        }
        if ($group.find('option').length === 0)
            $group.remove();
    };
    var removeService = function (e, service, elem) {
        elem = elem || this;
        var $elem = $(elem);
        $elem.find('optgroup').each(function (i, e) {
            if (e.service && e.service.id === service.id)
                $(e).remove();
        });
    };
})(webgis.$ || jQuery);

(function ($) {
    "use strict";
    $.fn.webgis_labelingCombo = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_labelingCombo');
        }
    };
    var defaults = {
        map: null,
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
        if (options.map !== null) {
            for (var serviceId in options.map.services) {
                var service = options.map.services[serviceId];
                addService({}, service, elem);
            }
        }
        elem._map.events.on('onaddservice', addService, elem);
        elem._map.events.on('onremoveservice', removeService, elem);
        if (options.val)
            $elem.val(options.val);
    };
    var addService = function (e, service, elem) {
        var elem = elem || this;
        if (service == null || service.labeling == null || service.labeling.length == 0)
            return;
        var $elem = $(elem);
        var count = $elem.find('option').length;
        var options = $elem.get(0)._opt;
        var type = options.type;
        var $group = $("<optgroup label='" + service.name + "' />").appendTo($elem);
        $group.get(0).service = service;
        for (var i = 0, to = service.labeling.length; i < to; i++) {
            var labeling = service.labeling[i];
            var val = service.id + "~" + labeling.id;
            var $opt = $("<option value='" + val + "'>&nbsp;" + labeling.name + "</option>").appendTo($group);
            if (service.map.hasLabeling(val))
                $opt.css({
                    backgroundImage: "url('" + webgis.css.img('check2-24.png') + "')",
                    backgroundPosition: "0px -2px",
                    backgroundRepeat: "no-repeat",
                    height: 24
                });
        }
        if ($group.find('option').length == 0)
            $group.remove();
    };
    var removeService = function (e, service, elem) {
        var elem = elem || this;
        var $elem = $(elem);
        $elem.find('optgroup').each(function (i, e) {
            if (e.service && e.service.id == service.id)
                $(e).remove();
        });
    };
})(webgis.$ || jQuery);

(function ($) {
    "use strict";
    $.fn.webgis_catCombo = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_catCombo');
        }
    };
    var defaults = {
        classFilters: null
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
        if (webgis && webgis.usability && webgis.usability.useCatCombos === false)
            return;

        var $combo = $(elem);

        if (!$combo.parent().hasClass('webgis-cat-combo-holder')) {
            var $holder = $("<div>")
                .addClass('webgis-cat-combo-holder')
                .insertAfter($combo);

            $combo.appendTo($holder);
        }

        var $catCombo = $combo.prevAll('.webgis-cat-combo');

        if ($catCombo.length === 0) {
            $catCombo = $("<select>")
                .addClass('webgis-input webgis-cat-combo')
                .data('combo', $combo)
                .insertBefore($combo);
        }

        if (!webgis.isMobileDevice() && $.fn.typeahead) {
            if (!$combo.hasClass('allow-filter')) {
                var $filterInput = $("<input type='text'>")
                    .attr('placeholder', webgis.l10n.get("find-layers"))
                    .addClass('webgis-input webgis-cat-combo-filter')
                    .insertBefore($combo);

                // Add Toggle Buttons
                function tog(v) { return v ? 'addClass' : 'removeClass'; };
                $combo.addClass('allow-filter')
                    .on('mousemove', function (e) {
                        $(this)[tog(this.offsetWidth - 46 < e.clientX - this.getBoundingClientRect().left &&
                            this.offsetWidth - 24 > e.clientX - this.getBoundingClientRect().left)]('onX');
                    }).on('touchstart click', function (ev) {
                        if ($(this).hasClass('onX')) {
                            var $input = $(this)
                                .removeClass('onX')
                                .closest('.webgis-cat-combo-holder')
                                .addClass('show-filter')
                                .find('.webgis-cat-combo-filter');

                            $input.typeahead('val', '');

                            var category = $(this).closest('.webgis-cat-combo-holder').children('.webgis-cat-combo').val();
                            $input.attr('placeholder', 'Themen ' + (category ? 'in ' + category + ' ' : '') + 'suchen...');
                        }
                    });

                $filterInput
                    .on('mousemove', function (e) {
                        $(this)[tog(this.offsetWidth - 28 < e.clientX - this.getBoundingClientRect().left)]('onX');
                    }).on('touchstart click', function (ev) {
                        if ($(this).hasClass('onX')) {
                            $(this)
                                .removeClass('onX')
                                .closest('.webgis-cat-combo-holder')
                                .removeClass('show-filter');
                        }
                    });

                // Initialize Autocomplete
                $filterInput.on({
                    'typeahead:select': function (e, item) {
                        var $holder = $(this)
                            .closest('.webgis-cat-combo-holder');
                        var $combo = $holder
                            .find('.webgis-cat-combo-target');

                        $(this).typeahead('val', '');
                        $combo.val(item.value).trigger('change');
                        $holder.removeClass('show-filter');
                    }
                })
                    .typeahead({
                        hint: false,
                        highlight: false,
                        minLength: 0
                    }, {
                            limit: Number.MAX_VALUE,
                            displayKey: 'label',
                            source: function (query, processSync, processAsync) {
                                var $holder = this.$el.closest('.webgis-cat-combo-holder');
                                var $combo = $holder.find('.webgis-cat-combo-target');

                                var items = [];
                                $combo.find('option').each(function (i, option) {
                                    var $option = $(option),
                                        $optGroup = $option.parent().is('optgroup') ? $option.parent() : null;

                                    if (!$optGroup ||
                                        $option.css('display') === 'none' ||
                                        $optGroup.css('display') === 'none') {
                                        return;
                                    }

                                    var text = $option.text().toLowerCase();

                                    var found = true;
                                    $.each(query.split(' '), function (i, term) {
                                        if (text && text.indexOf(term.toLowerCase()) < 0)
                                            found = false;
                                    });

                                    if (found) {
                                        items.push(
                                            {
                                                value: $option.val(),
                                                label: $option.text(),
                                                category: $optGroup.attr('label')
                                            }
                                        );
                                    }
                                });

                                processSync(items.length > 0 ? items.slice(0, 20) : null);
                            },
                            templates: {
                                empty: [
                                    '<div class="tt-suggestion">',
                                    '<div class="tt-content">Keine Ergebnisse gefunden</div>',
                                    '</div>'
                                ].join('\n'),
                                suggestion: function (item) {
                                    var html = [
                                        '<div class="tt-suggestion">',
                                        '<div class="tt-content">',
                                        '<div>' + item.label + '</div>',
                                        '<div style="color:#aaa">' + item.category + '</div>',
                                        '</div>',
                                        '</div>'
                                    ].join('\n');
                                    return $(html).data('item', item);
                                }
                            }
                    });
            }
        }

        $combo.addClass('webgis-cat-combo-target');

        $catCombo.empty();
        $("<option value=''>")
            .text(webgis.l10n.get("query-choose-category"))
            .appendTo($catCombo);
        $("<option value=''>")
            .text("--- " + webgis.l10n.get("query-all") + " ---")
            .appendTo($catCombo);

        if (options.classFilters) {
            for (var cf in options.classFilters) {
                var classFilter = options.classFilters[cf];
                $("<option>")
                    .attr('value', 'class-filter:' + cf)
                    .text(classFilter.label)
                    .appendTo($catCombo);
            }
        };

        var categories = [];
        $combo.find('optgroup').each(function (i, group) {
            categories.push($(group).attr('label'));
            
        });
        categories.sort();
        $.each(categories, function (i, category) {
            $("<option>")
                .attr('value', category)
                .text(category)
                .appendTo($catCombo);
        });

        $catCombo.change(function () {
            var $catCombo = $(this), $combo = $catCombo.nextAll('.webgis-cat-combo-target');
            var val = $catCombo.val();

            $catCombo.closest('.webgis-cat-combo-holder').removeClass('show-filter');

            if (!val) {
                $combo.find('option').css('display', '');
                $combo.children('optgroup').css('display', '');
            } else if (val.indexOf('class-filter:') === 0) {
                $combo.children('optgroup').css('display', 'none');

                var className = val.substr('class-filter:'.length);
                var comboValue = $combo.val(), firstVisibleValue = null, setFirstVisibleValue = false;

                $combo.find('option').each(function (i, option) {
                    var $option = $(option);
                    
                    if ($option.hasClass(className)) {
                        $option.css('display', '').closest('optgroup').css('display', '');
                        firstVisibleValue = firstVisibleValue || $option.attr('value');
                    } else {
                        $option.css('display', 'none');
                        if ($option.attr('value') === comboValue) { // Current selecteded is hidden
                            setFirstVisibleValue = true;
                        }
                    }
                });

                if (setFirstVisibleValue) {
                    $combo.val(firstVisibleValue).trigger('change');
                }
            } else {
                var $firstGroupOption = null,
                    $selectedOption = $combo.find("option[value='" + $combo.val() + "']");

                $combo.children('option').css('display', 'none');
                $combo.find('option').css('display', '');

                $combo.find('optgroup').each(function (i, group) {
                    if ($(group).attr('label') === val) {
                        $(group).css('display', '');
                        $firstGroupOption = $(group).children('option').first();
                    } else {
                        $(group).css('display', 'none');
                    }
                }); 

                if ($firstGroupOption) {
                    if ($selectedOption.length === 1) {
                        var $selectedParent = $selectedOption.parent();
                        if ($selectedParent.css('display') === 'none' || !$selectedParent.is('optgroup')) {
                            $combo.val($firstGroupOption.attr('value')).trigger('change');
                        }
                    } else {
                        $combo.val($firstGroupOption.attr('value')).trigger('change');
                    }
                }
            }

            if ($combo.attr('id')) {
                $.__webgis_catCombo_cache[$combo.attr('id') + ':category'] = val;
            }

            // 
            // Wenn Anwender alle auswählt automatisch auf erstes nicht gruppiertes element
            // => in der Regel "alle sichtbaren Themen"
            //
            if ($catCombo.hasClass('triggered')) {
                $catCombo.removeClass('triggered')
            } else if ($catCombo.val() === '') {
                var $firstOption = $combo.children('option').first();
                if ($firstOption.length === 1) {
                    $combo.val($firstOption.attr('value')).trigger('change');
                }
            }
        });

        if ($combo.attr('id')) {
            var catVal = $.__webgis_catCombo_cache[$combo.attr('id') + ':category'];
            var comboVal = $.__webgis_catCombo_cache[$combo.attr('id')];

            if (catVal &&
                $catCombo.val() !== catVal &&
                $catCombo.find("option[value='" + catVal + "']").length === 1) {

                $catCombo.addClass('triggered').val(catVal).trigger('change');
            }

            if (comboVal && $combo.val() !== comboVal) {
                var $comboOption = $combo.find("option[value='" + comboVal + "']");
                if ($comboOption.length === 1 &&
                    $comboOption.parent().css('display') !== 'none') {

                    $combo.val(comboVal).trigger('change');
                }
            }

            $combo.change(function () {
                $.__webgis_catCombo_cache[$(this).attr('id')] = $(this).val();
            });
        }
    }

    $.__webgis_catCombo_cache = [];
})(webgis.$ || jQuery);

(function ($) {
    "use strict";
    $.fn.webgis_extCombo = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_extCombo');
        }
    };
    var defaults = {
        inputType: 'text'
    };
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        values: function (options) {
            var values = [];
            $(this).children('option').each(function (i, o) {
                values.push($(o).attr('value'));
            });
            return values;
        }
    };

    var initUI = function (combo, options) {
        if (!$.fn.webgis_modal)
            return;

        var $combo = $(combo);
        $combo
            .addClass('webgis-extendable-combo')
            .mousemove(function (e) {
                var width = $(this).outerWidth();
                var offset = $(this).offset();

                var pos = [-(offset.left + width - e.pageX), e.pageY - offset.top];
                if (pos[0] <= -26 && pos[0] >= -46) {
                    $(this).addClass('focus-extend')
                } else {
                    $(this).removeClass('focus-extend')
                }
            })
            .click(function (e) {
                var $this = $(this);
                if ($this.hasClass('focus-extend')) {
                    $this.removeClass('focus-extend');
                    e.stopPropagation();

                    $('body').webgis_modal({
                        id: 'webgis-extendable-combo-modal',
                        title: 'Liste erweitern',
                        width: '340px',
                        height: '150px',
                        onload: function ($content) {
                            var $input = $("<input>")
                                .attr("type", options.inputType)
                                .addClass("webgis-input")
                                .css({ width: "calc(100% - 12px)", marginBottom: "5px" })
                                .val($combo.val())
                                .appendTo($content);
  
                            var $button = $("<button>")
                                .addClass("webgis-button")
                                .css({ width: "150px", float: "right" })
                                .text("Ok")
                                .appendTo($content)
                                .click(function () {
                                    var newValue = $input.val();

                                    $("<option>")
                                        .attr('value', newValue)
                                        .text(newValue)
                                        .appendTo($combo);

                                    $combo.val(newValue);
                                    $combo.trigger('change');

                                    $(null).webgis_modal('close', { id: 'webgis-extendable-combo-modal' });
                                });

                            $input.keypress(function (e) {
                                if (e.keyCode === 13) {
                                    $button.trigger("click");
                                }
                            }).focus();
                        } 
                    });
                }
            });
    }
})(webgis.$ || jQuery);
