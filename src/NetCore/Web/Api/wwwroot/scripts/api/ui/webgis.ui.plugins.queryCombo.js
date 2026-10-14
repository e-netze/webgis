// WebGIS UI Plugins (queryCombo)
// webgis_queryCombo, webgis_chainageCombo, webgis_labelingCombo, webgis_catCombo, webgis_extCombo, webgis_tagsCombo
// Selects filled from the map services (queries, chainage themes, labeling; updated on add/remove service),
// category/filter combo for grouped selects, extendable select and tag filter combo
// Element builder: webgis.ui.builder.combos.js (querycombo, chainagethemecombo, labelingcombo, select)

webgis.ui.definePlugin('webgis_queryCombo', {
    defaults: {
        map: null,
        type: 'query',
        customitems: [],
        onchange: null,
        valuefunction: null
    },
    init: function () {
        const $ = this.$, o = this.options;
        const $elem = this.$el.addClass('webgis-query-combo');
        const elem = $elem[0];
        elem._map = o.map;
        elem._opt = o;
        if (o.onchange) {
            $elem.on('change.webgis_querycombo', o.onchange);
        }

        let isInitialized = $elem.hasClass('initialized');

        if (o.customitems) {
            for (let i in o.customitems) {
                const item = o.customitems[i];
                let $parent = $elem;
                if (item.category) {
                    $parent = $elem.find("optgroup[label='" + item.category + "']");
                    if ($parent.length === 0)
                        $parent = $("<optgroup label='" + item.category + "' />").addClass('webgis-custom-option').appendTo($elem);
                }
                const $opt = $("<option value='" + item.value + "'>" + item.name + "</option>").addClass('webgis-custom-option').appendTo($parent);
                if (webgis.usability
                    && webgis.usability.listVisibleQueriesInQueryCombo === true
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

        if (o.map) {
            for (let serviceId in o.map.services) {
                this.addService(o.map.services[serviceId]);
            }

            this._onAddService = (e, service) => this.addService(service);
            this._onRemoveService = (e, service) => this.removeService(service);
            o.map.events.on('onaddservice', this._onAddService, elem);
            o.map.events.on('onremoveservice', this._onRemoveService, elem);
        }
    },
    destroy: function () {
        const map = this.options.map;
        if (map && map.events) {
            map.events.off('onaddservice', this._onAddService);
            map.events.off('onremoveservice', this._onRemoveService);
        }
        this.$el.off('.webgis_querycombo');
    },
    methods: {
        selectfirst: function () {
            const $elem = this.$el;
            if ($elem.find('option[value]:first').length === 1) {
                const firstValue = $elem.find('option[value]:first').attr('value');
                if (firstValue) {
                    $elem.val(firstValue).trigger('change');
                }
            }
        },
        refresh: function () {
            const $elem = this.$el;
            if ($elem.hasClass('refreshing')) return;

            $elem.addClass('refreshing');
            webgis.delayed(function () {
                if (this._destroyed) return;
                this._addVisibleQueries();
                $elem.removeClass('refreshing');
            }.bind(this), 300, $elem);
        },
        addService: function (service) {
            const $ = this.$;
            if (!service || !service.queries || service.queries.length === 0)
                return;

            const $elem = this.$el;
            const count = $elem.find('option').length;
            const options = this.options;
            const type = options.type;
            let isInitialized = $elem.hasClass('initialized');

            const $group = $("<optgroup label='" + service.name + "' />");
            const customOptions = $elem.children('.webgis-custom-option');
            if (customOptions.length === 0) {
                $group.prependTo($elem);
            } else {
                $group.insertAfter(customOptions[customOptions.length - 1]);
            }

            $group.get(0).service = service;
            for (let i = 0, to = service.queries.length; i < to; i++) {
                const query = service.queries[i];
                if (query.visible === false)
                    continue;
                if (type === 'query' && (!query.items || !query.items.find((item) => item.visible !== false)))
                    continue;
                let val = service.id + ':' + query.id;
                if (options.valuefunction) {
                    val = options.valuefunction(service, query);
                }
                const $opt = $("<option value='" + val + "'>" + query.name + "</option>").appendTo($group);
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
        },
        removeService: function (service) {
            const $ = this.$, $elem = this.$el;
            $elem.find('optgroup').each(function (i, e) {
                if (e.service && e.service.id === service.id)
                    $(e).remove();
            });
            $elem.trigger('change');
        },
        _addVisibleQueries: function () {
            const $ = this.$, map = this.options.map;
            if (webgis.usability && webgis.usability.listVisibleQueriesInQueryCombo !== true) return;

            const $elem = this.$el;

            $elem.children('.scale-dependent').addClass('check-visibility');
            const visiableQueriesOption = $elem.children("[value='#']");
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
    }
});

webgis.ui.definePlugin('webgis_chainageCombo', {
    defaults: {
        map: null
    },
    init: function () {
        const o = this.options, elem = this.$el[0];
        elem._map = o.map;
        elem._opt = o;
        if (o.map) {
            for (let serviceId in o.map.services) {
                this.addService(o.map.services[serviceId]);
            }

            this._onAddService = (e, service) => this.addService(service);
            this._onRemoveService = (e, service) => this.removeService(service);
            o.map.events.on('onaddservice', this._onAddService, elem);
            o.map.events.on('onremoveservice', this._onRemoveService, elem);
        }
    },
    destroy: function () {
        const map = this.options.map;
        if (map && map.events) {
            map.events.off('onaddservice', this._onAddService);
            map.events.off('onremoveservice', this._onRemoveService);
        }
    },
    methods: {
        addService: function (service) {
            const $ = this.$;
            if (service == null || service.chainagethemes == null || service.chainagethemes.length === 0)
                return;
            const $elem = this.$el;
            const $group = $("<optgroup label='" + service.name + "' />").appendTo($elem);
            $group.get(0).service = service;
            for (let i = 0, to = service.chainagethemes.length; i < to; i++) {
                const chainage = service.chainagethemes[i];
                $("<option value='" + chainage.id + "'>" + chainage.name + "</option>").appendTo($group);
            }
            if ($group.find('option').length === 0)
                $group.remove();
        },
        removeService: function (service) {
            const $ = this.$;
            this.$el.find('optgroup').each(function (i, e) {
                if (e.service && e.service.id === service.id)
                    $(e).remove();
            });
        }
    }
});

webgis.ui.definePlugin('webgis_labelingCombo', {
    defaults: {
        map: null
    },
    init: function () {
        const o = this.options, $elem = this.$el, elem = $elem[0];
        elem._map = o.map;
        elem._opt = o;
        if (o.map) {
            for (let serviceId in o.map.services) {
                this.addService(o.map.services[serviceId]);
            }

            this._onAddService = (e, service) => this.addService(service);
            this._onRemoveService = (e, service) => this.removeService(service);
            o.map.events.on('onaddservice', this._onAddService, elem);
            o.map.events.on('onremoveservice', this._onRemoveService, elem);
        }
        if (o.val)
            $elem.val(o.val);
    },
    destroy: function () {
        const map = this.options.map;
        if (map && map.events) {
            map.events.off('onaddservice', this._onAddService);
            map.events.off('onremoveservice', this._onRemoveService);
        }
    },
    methods: {
        addService: function (service) {
            const $ = this.$;
            if (service == null || service.labeling == null || service.labeling.length == 0)
                return;
            const $elem = this.$el;
            const $group = $("<optgroup label='" + service.name + "' />").appendTo($elem);
            $group.get(0).service = service;
            for (let i = 0, to = service.labeling.length; i < to; i++) {
                const labeling = service.labeling[i];
                const val = service.id + "~" + labeling.id;
                const $opt = $("<option value='" + val + "'>&nbsp;" + labeling.name + "</option>").appendTo($group);
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
        },
        removeService: function (service) {
            const $ = this.$;
            this.$el.find('optgroup').each(function (i, e) {
                if (e.service && e.service.id == service.id)
                    $(e).remove();
            });
        }
    }
});

// Called again (with or without options) on the same select after every added service
// to rebuild the category list => reinit
webgis.ui.definePlugin('webgis_catCombo', {
    reinit: true,
    defaults: {
        classFilters: null
    },
    init: function () {
        if (webgis && webgis.usability && webgis.usability.useCatCombos === false)
            return;

        const $ = this.$, options = this.options;
        const cache = this.constructor.cache;
        const $combo = this.$el;

        if (!$combo.parent().hasClass('webgis-cat-combo-holder')) {
            const $holder = $("<div>")
                .addClass('webgis-cat-combo-holder')
                .insertAfter($combo);

            $combo.appendTo($holder);
        }

        let $catCombo = $combo.prevAll('.webgis-cat-combo');

        if ($catCombo.length === 0) {
            $catCombo = $("<select>")
                .addClass('webgis-input webgis-cat-combo')
                .data('combo', $combo)
                .insertBefore($combo);
        }
        this._$catCombo = $catCombo;

        if (!webgis.isMobileDevice() && $.fn.typeahead) {
            if (!$combo.hasClass('allow-filter')) {
                // filter input and its handlers are created only once per combo (class 'allow-filter'),
                // so they are not removed on reinit (namespace .webgis_catcombo_filter)
                const $filterInput = $("<input type='text'>")
                    .attr('placeholder', webgis.l10n.get("find-layers"))
                    .addClass('webgis-input webgis-cat-combo-filter')
                    .insertBefore($combo);

                // Add Toggle Buttons
                const tog = function (v) { return v ? 'addClass' : 'removeClass'; };
                $combo.addClass('allow-filter')
                    .on('mousemove.webgis_catcombo_filter', function (e) {
                        $(this)[tog(this.offsetWidth - 46 < e.clientX - this.getBoundingClientRect().left &&
                            this.offsetWidth - 24 > e.clientX - this.getBoundingClientRect().left)]('onX');
                    }).on('touchstart.webgis_catcombo_filter click.webgis_catcombo_filter', function (ev) {
                        if ($(this).hasClass('onX')) {
                            const $input = $(this)
                                .removeClass('onX')
                                .closest('.webgis-cat-combo-holder')
                                .addClass('show-filter')
                                .find('.webgis-cat-combo-filter');

                            $input.typeahead('val', '');

                            const category = $(this).closest('.webgis-cat-combo-holder').children('.webgis-cat-combo').val();
                            $input.attr('placeholder', category ? webgis.l10n.get('find-layers-in-category', { category: category }) : webgis.l10n.get('find-layers'));
                        }
                    });

                $filterInput
                    .on('mousemove.webgis_catcombo_filter', function (e) {
                        $(this)[tog(this.offsetWidth - 28 < e.clientX - this.getBoundingClientRect().left)]('onX');
                    }).on('touchstart.webgis_catcombo_filter click.webgis_catcombo_filter', function (ev) {
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
                        const $holder = $(this)
                            .closest('.webgis-cat-combo-holder');
                        const $combo = $holder
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
                            const $holder = this.$el.closest('.webgis-cat-combo-holder');
                            const $combo = $holder.find('.webgis-cat-combo-target');

                            const items = [];
                            $combo.find('option').each(function (i, option) {
                                const $option = $(option),
                                    $optGroup = $option.parent().is('optgroup') ? $option.parent() : null;

                                if (!$optGroup ||
                                    $option.css('display') === 'none' ||
                                    $optGroup.css('display') === 'none') {
                                    return;
                                }

                                const text = $option.text().toLowerCase();

                                let found = true;
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
                                '<div class="tt-content">' + webgis.l10n.get('no-results-found') + '</div>',
                                '</div>'
                            ].join('\n'),
                            suggestion: function (item) {
                                const html = [
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
            for (let cf in options.classFilters) {
                const classFilter = options.classFilters[cf];
                $("<option>")
                    .attr('value', 'class-filter:' + cf)
                    .text(classFilter.label)
                    .appendTo($catCombo);
            }
        }

        const categories = [];
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

        $catCombo.on('change.webgis_catcombo', function () {
            const $catCombo = $(this), $combo = $catCombo.nextAll('.webgis-cat-combo-target');
            const val = $catCombo.val();

            $catCombo.closest('.webgis-cat-combo-holder').removeClass('show-filter');

            if (!val) {
                $combo.find('option').css('display', '');
                $combo.children('optgroup').css('display', '');
            } else if (val.indexOf('class-filter:') === 0) {
                $combo.children('optgroup').css('display', 'none');

                const className = val.substr('class-filter:'.length);
                let comboValue = $combo.val(), firstVisibleValue = null, setFirstVisibleValue = false;

                $combo.find('option').each(function (i, option) {
                    const $option = $(option);

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
                let $firstGroupOption = null;
                const $selectedOption = $combo.find("option[value='" + $combo.val() + "']");

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
                        const $selectedParent = $selectedOption.parent();
                        if ($selectedParent.css('display') === 'none' || !$selectedParent.is('optgroup')) {
                            $combo.val($firstGroupOption.attr('value')).trigger('change');
                        }
                    } else {
                        $combo.val($firstGroupOption.attr('value')).trigger('change');
                    }
                }
            }

            if ($combo.attr('id')) {
                cache[$combo.attr('id') + ':category'] = val;
            }

            //
            // Wenn Anwender alle auswählt automatisch auf erstes nicht gruppiertes element
            // => in der Regel "alle sichtbaren Themen"
            //
            if ($catCombo.hasClass('triggered')) {
                $catCombo.removeClass('triggered');
            } else if ($catCombo.val() === '') {
                const $firstOption = $combo.children('option').first();
                if ($firstOption.length === 1) {
                    $combo.val($firstOption.attr('value')).trigger('change');
                }
            }
        });

        if ($combo.attr('id')) {
            const catVal = cache[$combo.attr('id') + ':category'];
            const comboVal = cache[$combo.attr('id')];

            if (catVal &&
                $catCombo.val() !== catVal &&
                $catCombo.find("option[value='" + catVal + "']").length === 1) {

                $catCombo.addClass('triggered').val(catVal).trigger('change');
            }

            if (comboVal && $combo.val() !== comboVal) {
                const $comboOption = $combo.find("option[value='" + comboVal + "']");
                if ($comboOption.length === 1 &&
                    $comboOption.parent().css('display') !== 'none') {

                    $combo.val(comboVal).trigger('change');
                }
            }

            $combo.on('change.webgis_catcombo', function () {
                cache[$(this).attr('id')] = $(this).val();
            });
        }
    },
    destroy: function () {
        this.$el.off('.webgis_catcombo');
        if (this._$catCombo) {
            this._$catCombo.off('.webgis_catcombo');
        }
    },
    staticMethods: {
        // last selected category/value per combo id (restored when a combo with the same id is created again)
        cache: {}
    }
});

webgis.ui.definePlugin('webgis_extCombo', {
    defaults: {
        inputType: 'text'
    },
    init: function () {
        const $ = this.$;
        if (!webgis.ui.showModal)
            return;

        const options = this.options;
        const $combo = this.$el;
        $combo
            .addClass('webgis-extendable-combo')
            .on('mousemove.webgis_extcombo', function (e) {
                const width = $(this).outerWidth();
                const offset = $(this).offset();

                const pos = [-(offset.left + width - e.pageX), e.pageY - offset.top];
                if (pos[0] <= -26 && pos[0] >= -46) {
                    $(this).addClass('focus-extend');
                } else {
                    $(this).removeClass('focus-extend');
                }
            })
            .on('click.webgis_extcombo', function (e) {
                const $this = $(this);
                if ($this.hasClass('focus-extend')) {
                    $this.removeClass('focus-extend');
                    e.stopPropagation();

                    webgis.ui.showModal({
                        id: 'webgis-extendable-combo-modal',
                        title: webgis.l10n.get('expand-list'),
                        width: '340px',
                        height: '150px',
                        onload: function ($content) {
                            const $input = $("<input>")
                                .attr("type", options.inputType)
                                .addClass("webgis-input")
                                .css({ width: "calc(100% - 12px)", marginBottom: "5px" })
                                .val($combo.val())
                                .appendTo($content);

                            const $button = $("<button>")
                                .addClass("webgis-button")
                                .css({ width: "150px", float: "right" })
                                .text("Ok")
                                .appendTo($content)
                                .click(function () {
                                    const newValue = $input.val();

                                    $("<option>")
                                        .attr('value', newValue)
                                        .text(newValue)
                                        .appendTo($combo);

                                    $combo.val(newValue);
                                    $combo.trigger('change');

                                    webgis.ui.closeModal('webgis-extendable-combo-modal');
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
    },
    destroy: function () {
        this.$el.off('.webgis_extcombo');
    },
    methods: {
        values: function () {
            const $ = this.$;
            const values = [];
            this.$el.children('option').each(function (i, o) {
                values.push($(o).attr('value'));
            });
            return values;
        }
    }
});

webgis.ui.definePlugin('webgis_tagsCombo', {
    defaults: {
        itemSelector: '.webgis-tag-item',
        itemTagsAttr: 'tags',
        tags: []
    },
    init: function () {
        const $ = this.$, options = this.options;
        if (!options || !options.itemContainer)
            return;

        const $combo = this.$el;
        $combo
            .data('item-container', $(options.itemContainer))
            .data('item-selector', options.itemSelector)
            .data('item-tags-attr', options.itemTagsAttr)
            .on('mousemove.webgis_tagscombo', function (e) {
                const width = $(this).outerWidth();
                const offset = $(this).offset();

                const pos = [-(offset.left + width - e.pageX), e.pageY - offset.top];
                if (pos[0] <= -26 && pos[0] >= -46) {
                    $(this).addClass('focus-extend');
                } else {
                    $(this).removeClass('focus-extend');
                }
            })
            .on('click.webgis_tagscombo', function (e) {
                const $this = $(this);
                const $popup = $this.next('.webgis-tags-combo-popup');

                if ($this.hasClass('focus-extend')) {
                    $this.removeClass('focus-extend');

                    e.stopPropagation();
                    $this.blur();
                    $popup.css('top', $this.offset());
                    $popup.css('width', $this.outerWidth());
                    $popup.toggle();
                } else {
                    $popup.css('display', 'none');
                }
            });

        const $popup = $("<div>")
            .addClass('webgis-tags-combo-popup')
            //.addClass('webgis-hide-on-click-outside')
            .css({
                "display": "none",
                "position": "absolute",
            })
            .insertAfter($combo)
            .on('mouseleave.webgis_tagscombo', function () {
                const $this = $(this);

                $this.addClass('disappearing');
                webgis.delayed(function ($this) {
                    if ($this.hasClass('disappearing')) {
                        $this.css('display', 'none').removeClass('disappearing');
                    }
                }, 1000, $this);
            })
            .on('mouseenter.webgis_tagscombo', function () {
                $(this).removeClass('disappearing');
            });
        this._$popup = $popup;

        const $valueElement = $("<input>")
            .addClass('webgis-tags-combo-value')
            .attr('type', 'hidden')
            .insertAfter($popup);

        if (options.id) {
            $valueElement.attr('id', options.id);
        }
        if (options.addClasses) {
            $valueElement.addClass(options.addClasses);
        }

        this._setTags(options.tags);
    },
    destroy: function () {
        this.$el.off('.webgis_tagscombo');
        if (this._$popup) {
            this._$popup.find('*').addBack().off('.webgis_tagscombo');
        }
    },
    methods: {
        set_tags: function (options) {
            this._setTags(this.$.extend({ tags: [] }, options).tags);
        },
        restore: function () {
            this._loadTags();
        },
        _setTags: function (tags) {
            const $ = this.$, me = this;
            const $combo = this.$el;

            $combo.data('tags', tags);
            if (tags.length > 0) {
                $combo.addClass('webgis-tags-combo');
                $("<button>")
                    .addClass("webgis-tag-button all")
                    .text(webgis.l10n.get("show-all"))
                    .appendTo($combo.next('.webgis-tags-combo-popup'))
                    .on('click.webgis_tagscombo', function (e) {
                        e.stopPropagation();

                        const $this = $(this);

                        $this.parent().children().removeClass('active');
                        me._refreshItemVisibility(true);
                    });

                for (const tag of tags) {
                    $("<button>")
                        .addClass("webgis-tag-button")
                        .attr('tag', tag.toLowerCase())
                        .text(tag)
                        .appendTo($combo.next('.webgis-tags-combo-popup'))
                        .on('click.webgis_tagscombo', function (e) {
                            e.stopPropagation();

                            $(this).toggleClass('active');
                            me._refreshItemVisibility(true);
                        });
                }
            } else {
                $combo.removeClass('webgis-tags-combo').next('.webgis-tags-combo-popup').empty();
            }
        },
        _storeTags: function (tags) {
            const $combo = this.$el;
            const key = "webgis-tags." + $combo.attr('id') + '.' + $combo.val();
            webgis.localStorage.set(key, tags.join(','));
        },
        _loadTags: function () {
            const $ = this.$, $combo = this.$el;
            const key = "webgis-tags." + $combo.attr('id') + '.' + $combo.val();
            const tags = webgis.localStorage.get(key);
            const $popup = $combo.next('.webgis-tags-combo-popup');

            $popup.removeClass("has-tags");
            $combo.removeClass("has-tags");

            if (tags) {
                const activeTags = tags.split(',').map(t => t.trim()).filter(t => t.length > 0);

                if (activeTags.length > 0) {
                    $popup.addClass("has-tags");
                    $combo.addClass("has-tags");
                    $popup.children('.webgis-tag-button').removeClass('active').each(function (i, e) {
                        const $e = $(e);
                        if (activeTags.includes($e.attr("tag"))) {
                            $e.addClass('active');
                        } else {
                            $e.removeClass('active');
                        }
                    });
                }
            }

            this._refreshItemVisibility(false);
        },
        _refreshItemVisibility: function (store) {
            const $ = this.$, $combo = this.$el;
            const $container = $combo.data("item-container");
            const itemSelector = $combo.data("item-selector");
            const itemTagsAttr = $combo.data("item-tags-attr");

            const activeTags = Array.from($combo.next('.webgis-tags-combo-popup').find('button.active').map((i, e) => $(e).attr('tag')));

            $container.find(itemSelector).removeClass('webgis-tag-item-hidden');
            if (activeTags.length === 0) {
                $combo.removeClass("has-tags").next('.webgis-tags-combo-popup').removeClass("has-tags");
            }
            else {
                $container.find(itemSelector).each(function (i, e) {
                    $combo.addClass("has-tags").next('.webgis-tags-combo-popup').addClass("has-tags");

                    const $e = $(e);
                    const tags = $e.attr(itemTagsAttr).toLowerCase().split(',');

                    let found = false;
                    for (const activeTag of activeTags) {
                        if (tags.includes(activeTag)) {
                            found = true;
                            break;
                        }
                    }

                    if (!found) {
                        $e.addClass('webgis-tag-item-hidden');
                    }
                });
            }

            if (store) {
                this._storeTags(activeTags);
            }

            $combo
                //.next('.webgis-tags-combo-popup')
                .nextAll('.webgis-tags-combo-value')
                .val(activeTags.join(','));
        }
    }
});
