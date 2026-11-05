// WebGIS UI Plugins (graphicsInfoContainer)
// webgis_graphicsInfoContainer
// List of the map graphics (mapmarkup) elements with select/edit/label/delete/symbol buttons (editable or readonly)
// Element builders: webgis.ui.builder.misc.js (graphics-info-container)

webgis.ui.definePlugin('webgis_graphicsInfoContainer', {
    defaults: {
        map: null,
        readonly: false
    },
    init: function () {
        const $ = this.$;
        const options = this.options;

        const $elem = this.$el.addClass('webgis-graphics-info-container-holder');
        if (options.readonly === true) {
            $elem.addClass('readonly');
        } else {
            this.constructor.destroyReadOnlyContainers();
            $elem.addClass('editable');
        }
        $elem.data('map', options.map);
        this.constructor.renderContainer(this.$el, options);
    },
    destroy: function () {
        this.$el.find('*').addBack().off('.webgis_graphics_info_container');
    },
    methods: {
        refresh: function (options) {
            const $ = this.$;
            this.constructor.renderContainer(this.$el, $.extend({}, { map: null, readonly: false }, options));
        }
    },
    staticMethods: {
        // usage from outside (no instance needed): webgis.ui.refreshGraphicsInfoContainers({ type: 'editable' })
        refreshAllContainers: function (options) {
            const $ = webgis.$ || jQuery;
            const self = $.fn.webgis_graphicsInfoContainer.plugin;

            const $containers = options && options.type
                ? $('.webgis-graphics-info-container-holder.' + options.type)
                : $('.webgis-graphics-info-container-holder');

            $containers.each(function () {
                self.renderContainer($(this), { map: null, readonly: false });
            });
        },
        hasContainers: function (options) {
            const $ = webgis.$ || jQuery;

            if (options) {
                if (options.type) {
                    return $('.webgis-graphics-info-container-holder.' + options.type).length > 0;
                }
            }
            return $('.webgis-graphics-info-container-holder').length > 0;
        },
        destroyReadOnlyContainers: function () {
            const $ = webgis.$ || jQuery;

            $('.webgis-graphics-info-container-holder.readonly').each(function (i, container) {
                var $container = $(container);
                if ($.fn.webgis_dockPanel) {
                    var $dockPanel = $(container).closest('.webgis-dockpanel');
                    if ($dockPanel.length > 0) {
                        webgis.ui.removeDockPanel($dockPanel);
                    } else {
                        $container.remove();
                    }
                } else {
                    $container.remove();
                }
            });
        },
        renderContainer: function ($elem, options) {
            const $ = webgis.$ || jQuery;
            const self = $.fn.webgis_graphicsInfoContainer.plugin;

            $elem.empty();
            const map = options.map || $elem.data('map');
            const elements = map.graphics.getElements();
            const readOnly = $elem.hasClass('readonly');

            // number per type in drawing order (independent of list order), e.g. "Point 3"
            const typeNumbers = new Map(), typeCounters = {};
            for (let element of elements) {
                typeCounters[element.type] = (typeCounters[element.type] || 0) + 1;
                typeNumbers.set(element, typeCounters[element.type]);
            }

            if ($elem.hasClass('order-descending')) elements.reverse();

            const hasMultiSelection = self.getSelectedCount(elements) > 1;

            if (!readOnly && !map.graphics.hasSelectedElements() && !map.graphics.hasStagedElement() && !map.graphics.showsToolDescriptionInPanel()) {
                var tool = map.graphics.getTool();
                var $header = $("<h3><img style='margin-right:10px' src='" + webgis.css.imgResource('rest/toolresource/webgis-tools-mapmarkup-mapmarkup-' + tool) + "'></h2>")
                    .text(self.translateType(tool))
                    .appendTo($elem);
                if (self.canShowProperties(map)) {
                    $header
                        .addClass('webgis-graphics-info-header-properties')
                        .css('cursor', 'pointer')
                        .attr('title', webgis.l10n.get('mapmarkup-show-properties'))
                        .on('click.webgis_graphics_info_container', function (e) {
                            e.stopPropagation();
                            map.graphics.showPropertiesPanel();
                        });
                }
                $("<div>")
                    .addClass('webgis-info')
                    .text(map.graphics.getToolDescription(tool))
                    .appendTo($elem);
            }

            $('<div>')
                .addClass('webgis-graphics-info-list-menu')
                .appendTo($elem);
            self.buildMenu($elem);

            if (!readOnly) {
                self.buildGroupBar($elem, map);
            }

            var usedHeight = 0;

            $elem.children().each(function (i, e) {
                usedHeight += $(e).outerHeight(true);  // true => include margin, ...
            });

            const $ul = $("<ul>")
                .addClass('webgis-graphics-info-list')
                .css('height', 'calc(100vh - ' + (440 + usedHeight) + 'px')  // 440 empiric (fallback, if the available height can't be calculated)
                .appendTo($elem);

            self.fitListHeight($ul);
            webgis.delayed(function () { self.fitListHeight($ul); }, 1);  // container maybe not visible/attached yet

            const hasStagedElement = map.graphics.hasStagedElement();
            const hasUpdatingElement = map.graphics.hasUpdatingElement();
            const hasStagedOrUpdatingElements = hasStagedElement || hasUpdatingElement;

            // groups: filter by the selected group or (all) sort by group order with headings
            const groupFilter = readOnly ? '*' : map.graphics.getGroupFilter();
            const groupIds = map.graphics.getGroups().map(g => g.id);
            const groupIndex = function (element) {
                return element.updating ? -1 : groupIds.indexOf(map.graphics.getElementGroupId(element.originalElement));
            };
            const listElements = elements
                .filter(e => e.updating || groupFilter === '*' || map.graphics.getElementGroupId(e.originalElement) === groupFilter)
                .map((e, i) => ({ e: e, i: i }))
                .sort((a, b) => (groupIndex(a.e) - groupIndex(b.e)) || (a.i - b.i))
                .map(x => x.e);
            const showGroupHeadings = groupFilter === '*' &&
                new Set(listElements.filter(e => !e.updating).map(e => groupIndex(e))).size > 1;
            let lastGroupIndex = null;

            for (let element of listElements) {
                if (readOnly === true && element.type === 'text')
                    continue;

                if (showGroupHeadings && !element.updating && groupIndex(element) !== lastGroupIndex) {
                    lastGroupIndex = groupIndex(element);
                    const headerGroupId = map.graphics.getElementGroupId(element.originalElement);
                    const $header = $("<li>")
                        .addClass('webgis-graphics-info-groupheader')
                        .appendTo($ul);
                    self.appendGroupVisibilityButton($header, map, headerGroupId);
                    $("<span>")
                        .text(map.graphics.getGroupName(headerGroupId))
                        .appendTo($header);
                }

                const $li = $("<li></li>")
                    .addClass('webgis-graphics-info-listitem' + (element.originalElement && element.originalElement.selected === true ? ' selected' : ''))
                    .toggleClass('group-hidden', !element.updating && !map.graphics.isElementVisible(element.originalElement))
                    .appendTo($ul)
                    .data('element', element)

                if (!hasUpdatingElement) {
                    $li.on('mouseenter.webgis_graphics_info_container', function (e) {
                            const map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                            const element = $(this).data('element');
                            if (element && !element.updating && element.originalElement && element.originalElement.frameworkElement) {
                                const frameworkElement = element.originalElement.frameworkElement;
                                const geoJson = frameworkElement.toPreviewGeoJSON ? frameworkElement.toPreviewGeoJSON() : frameworkElement.toGeoJSON();
                                map.graphics.addPreviewFromJson(geoJson.geometry, false, true);
                            }
                        })
                        .on('mouseleave.webgis_graphics_info_container', function (e) {
                            var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                            map.graphics.removePreview();
                        });

                    if (!hasStagedOrUpdatingElements) {
                        $li.addClass('can-select').on('click.webgis_graphics_info_container', function (e) {
                            const $li = $(this);
                            const map = $(this).closest('.webgis-graphics-info-container-holder').data('map');

                            const element = $(this).data('element');
                            const readOnly = $(this).closest('.webgis-graphics-info-container-holder').hasClass('readonly');

                            if (readOnly === false) {
                                if (e.shiftKey) {
                                    self.selectElement($li, listElements, element, 'shift');
                                } else if (e.ctrlKey) {
                                    self.selectElement($li, elements, element, 'ctrl');
                                } else {
                                    self.selectElement($li, elements, element);
                                }
                            } else {
                                $(this).parent().children('.selected').removeClass('selected');
                                $(this).addClass('selected');

                                if (!map.isEnabled()) {
                                    map.enable(true);
                                }
                                map.graphics.zoomTo(element);
                            }

                            webgis.delayed(function (map) {  // remove preview delayed... mouseleave, mouseenter can fired ... 
                                map.graphics.removePreview();
                            }, 100, map);
                        });
                    }
                } else if (!element.updating) {
                    $li.addClass('unselectable');
                }

                let hasLabel = element.metaText && element.metaText.length > 0,
                    txtCls = hasLabel ? '' : 'unlabeled',
                    txt = hasLabel ? element.metaText : self.translateType(element.type) + ' ' + typeNumbers.get(element);

                $li.css({
                    borderLeft: self.itemBorder(element),
                });

                const symbolIconUrl = self.symbolIconUrl(element);
                const $btnSymbol = $("<div>")
                    .addClass('button symbol')
                    .addClass(element.updating ? '' : 'inactive')
                    .addClass(symbolIconUrl ? 'marker-icon' : '')
                    .css({
                        backgroundImage: self.itemIcon(element, symbolIconUrl),
                    })
                    .appendTo($li);

                if (element.updating) {
                    $btnSymbol.on('click.webgis_graphics_info_container', function (e) {
                        e.stopPropagation();

                        const element = $(this).closest('.webgis-graphics-info-listitem').data('element');

                        webgis.tools.onButtonClick(map,
                            { command: 'show-symbol-selector', type: 'servertoolcommand', map: map },
                            this,
                            null,
                            { 'mapmarkup-symbol-type': element.type });
                    });
                }

                if (element.updating) {
                    const $labelInput = $("<textarea rows='1'>")
                        .addClass('webgis-textarea')
                        .attr('placeholder', webgis.l10n.get('label-optional-placeholder'))
                        .appendTo($li)
                        .val(hasLabel ? txt : '');
                    if (element.type === 'text') {
                        $labelInput.on('keyup.webgis_graphics_info_container', function () {
                            const $this = $(this);
                            const map = $this.closest('.webgis-graphics-info-container-holder').data('map');
                            map.graphics.setMetaText($this.val());
                        });
                    } else {
                        $labelInput.on('change.webgis_graphics_info_container', function () {
                            const $this = $(this);
                            const map = $this.closest('.webgis-graphics-info-container-holder').data('map');
                            map.graphics.setMetaText($this.val());
                        });
                    }

                } else {
                    $("<div>")
                        .addClass('label-text')
                        .text(txt)    // Don't use html() here -> Javascript Injekction!!!!
                        .appendTo($li);
                }

                $li.addClass(txtCls);
                if (readOnly && element.readonly_selected) {
                    $li.addClass('selected');
                }

                if (readOnly === false) {
                    if (element.updating === true) {
                        $("<div>")
                            .addClass('button commit')
                            .appendTo($li)
                            .on('click.webgis_graphics_info_container', function (e) {
                                e.stopPropagation();
                                var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');

                                map.graphics.commitCurrentElement();
                            });
                    } else if (!hasUpdatingElement) {
                        if (element.type === 'polygon' || element.type === 'line' ||
                            (element.type === 'point' && element.metaText)) {
                            $("<div>")
                                .addClass('button label')
                                .appendTo($li)
                                .on('click.webgis_graphics_info_container', function (e) {
                                    e.stopPropagation();

                                    var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                                    var element = $(this).closest('.webgis-graphics-info-listitem').data('element');

                                    map.graphics.labelElement(element.originalElement);
                                });
                        }
                        $("<div>")
                            .addClass('button zoom')
                            .attr('title', webgis.l10n.get('zoom-to-feature'))
                            .appendTo($li)
                            .on('click.webgis_graphics_info_container', function (e) {
                                e.stopPropagation();
                                var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                                var element = $(this).closest('.webgis-graphics-info-listitem').data('element');

                                map.graphics.zoomTo(element);
                            });
                        $("<div>")
                            .addClass('button edit')
                            .appendTo($li)
                            .on('click.webgis_graphics_info_container', function (e) {
                                e.stopPropagation();
                                var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                                var element = $(this).closest('.webgis-graphics-info-listitem').data('element');

                                map.graphics.commitCurrentElement(true);

                                map.graphics.setTool('pointer');
                                map.graphics._onElementClick.apply(element.originalElement, [e]);  // element will selected there

                                webgis.delayed(function () {
                                    map.graphics.removePreview();
                                }, 300);

                            });
                        $("<div>")
                            .addClass('button delete')
                            .appendTo($li)
                            .on('click.webgis_graphics_info_container', function (e) {
                                e.stopPropagation();
                                var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                                var element = $(this).closest('.webgis-graphics-info-listitem').data('element');

                                map.graphics.removeElement(element.originalElement);
                            });
                    }
                }
            }
        },
        buildMenu: function ($parent) {
            const $ = webgis.$ || jQuery;
            const self = $.fn.webgis_graphicsInfoContainer.plugin;

            const map = $parent.data('map');
            if (!map)
                return;

            const selectedElements = map.graphics.selectedElements();
            const $menu = $parent.find('.webgis-graphics-info-list-menu').empty();

            if (selectedElements.length === 0 && !$parent.hasClass('readonly')) {
                map.graphics.onSelectionCleared();  // closes a panel showing the symbology of the (now empty) selection
            }

            if (!$parent.hasClass('readonly')) {
                map.graphics.refreshSelectionHighlight();
            }

            if (map.graphics.hasStagedElement() === true) { // show tools for selected
                $('<div>')
                    .addClass('button delete')
                    .attr('title', webgis.l10n.get('remove-element'))
                    .appendTo($menu)
                    .on('click.webgis_graphics_info_container', function (e) {
                        e.stopPropagation();

                        map.graphics.removeCurrentElement();
                    });

                self.appendPropertiesButton($parent, $menu, map);
            }
            else if (selectedElements.length > 0) {  // show tools for selected
                const styles = self.selectedStyles(map, selectedElements);

                if (styles) {
                    var $textButton = $('<div>')
                        .addClass('text-button')
                        .data('styles', styles)
                        .appendTo($menu)
                        .on('click.webgis_graphics_info_container', function (e) {
                            e.stopPropagation();

                            if (map.graphics.hasUpdatingElement()) {
                                $(this)
                                    .closest('.webgis-graphics-info-container-holder')
                                    .find('.webgis-graphics-info-listitem.selected .button.symbol')
                                    .trigger('click');

                                return;
                            }

                            map.graphics.showSelectionPropertiesPanel($(this).data('styles'), this);
                        });

                    $('<div>')
                        .addClass('button')
                        .attr('title', webgis.l10n.get('adjust-symbology'))
                        .css({
                            backgroundImage: 'url(' + webgis.css.imgResource('content/api/img/graphics/ui/polygon-styles.png') + ')',
                        })
                        .appendTo($textButton)

                    $("<div>").addClass('text').text(webgis.l10n.get('mapmarkup-symbology-selection')).appendTo($textButton);
                }

                if (map.graphics.hasUpdatingElement() !== true) {
                    $('<div>')
                        .addClass('button remove-selection')
                        .attr('title', webgis.l10n.get('remove-selection'))
                        .appendTo($menu)
                        .on('click.webgis_graphics_info_container', function (e) {
                            e.stopPropagation();

                            map.graphics.clearElementSelection();
                            $parent.find('.webgis-graphics-info-listitem').removeClass('selected');
                            self.buildMenu($parent);  // closes the selection symbology panel, shows the default menu
                        });

                    $('<div>')
                        .addClass('button delete')
                        .attr('title', webgis.l10n.get('remove-selected-elements'))
                        .appendTo($menu)
                        .on('click.webgis_graphics_info_container', function (e) {
                            e.stopPropagation();

                            map.graphics.removeElements(selectedElements);
                        });

                    const $move = $('<select>')
                        .addClass('webgis-input webgis-graphics-info-move-to-group')
                        .appendTo($menu)
                        .on('click.webgis_graphics_info_container', function (e) {
                            e.stopPropagation();
                        })
                        .on('change.webgis_graphics_info_container', function (e) {
                            const id = $(this).val();
                            if (id) {
                                map.graphics.moveElementsToGroup(selectedElements, id);
                            }
                        });
                    $('<option>').val('').text(webgis.l10n.get('mapmarkup-group-move-to')).appendTo($move);
                    for (let group of map.graphics.getGroups()) {
                        $('<option>').val(group.id).text(map.graphics.getGroupName(group.id)).appendTo($move);
                    }
                }
            } else {
                if (map.graphics.countElements() > 0) {  // show swap ordering
                    $('<div>')
                        .addClass('button order')
                        .attr('title', webgis.l10n.get('reverse-order'))
                        .appendTo($menu)
                        .on('click.webgis_graphics_info_container', function (e) {
                            e.stopPropagation();

                            const $holder = $(this).closest('.webgis-graphics-info-container-holder');
                            self.renderContainer($holder.toggleClass('order-descending'), {});
                        });
                }
                self.appendPropertiesButton($parent, $menu, map);
            }
        },
        buildGroupBar: function ($parent, map) {
            const $ = webgis.$ || jQuery;
            const self = $.fn.webgis_graphicsInfoContainer.plugin;

            const $bar = $('<div>')
                .addClass('webgis-graphics-info-groups')
                .appendTo($parent);

            const filter = map.graphics.getGroupFilter();

            const showNameInput = function (initialValue, onCommit) {
                $bar.children().css('display', 'none');
                const $edit = $('<div>').addClass('webgis-graphics-info-groups-edit').appendTo($bar);
                const $input = $('<input type="text">')
                    .addClass('webgis-input')
                    .attr('placeholder', webgis.l10n.get('mapmarkup-group-name'))
                    .val(initialValue || '')
                    .appendTo($edit);
                const commit = function () {
                    if (onCommit($input.val()) === false) {
                        $input.addClass('error').trigger('focus');
                        return;
                    }
                    map.graphics._refreshInfoContainers();
                };
                const cancel = function () {
                    map.graphics._refreshInfoContainers();
                };
                $input.on('keydown.webgis_graphics_info_container', function (e) {
                    if (e.key === 'Enter') { e.preventDefault(); commit(); }
                    else if (e.key === 'Escape') { e.preventDefault(); cancel(); }
                });
                $('<div>').addClass('button commit').attr('title', webgis.l10n.get('apply')).appendTo($edit)
                    .on('click.webgis_graphics_info_container', function (e) { e.stopPropagation(); commit(); });
                $('<div>').addClass('button cancel').attr('title', webgis.l10n.get('cancel')).appendTo($edit)
                    .on('click.webgis_graphics_info_container', function (e) { e.stopPropagation(); cancel(); });
                $input.trigger('focus');
            };

            const $select = $('<select>')
                .addClass('webgis-input')
                .appendTo($bar)
                .on('change.webgis_graphics_info_container', function () {
                    map.graphics.setGroupFilter($(this).val());
                });
            $('<option>').val('*').text(webgis.l10n.get('mapmarkup-group-all')).appendTo($select);
            for (let group of map.graphics.getGroups()) {
                $('<option>')
                    .val(group.id)
                    .text(map.graphics.getGroupName(group.id) + ' (' + map.graphics.countGroupElements(group.id) + ')' +
                        (map.graphics.isGroupVisible(group.id) ? '' : ' – ' + webgis.l10n.get('mapmarkup-group-hidden')))
                    .appendTo($select);
            }
            $select.val(filter);

            if (filter !== '*') {
                self.appendGroupVisibilityButton($bar, map, filter);
            }

            $('<div>')
                .addClass('button add')
                .attr('title', webgis.l10n.get('mapmarkup-group-add'))
                .appendTo($bar)
                .on('click.webgis_graphics_info_container', function (e) {
                    e.stopPropagation();
                    showNameInput('', function (name) {
                        return map.graphics.addGroup(name) !== null;
                    });
                });

            if (filter !== '*') {
                $('<div>')
                    .addClass('button edit')
                    .attr('title', webgis.l10n.get('mapmarkup-group-rename'))
                    .appendTo($bar)
                    .on('click.webgis_graphics_info_container', function (e) {
                        e.stopPropagation();
                        showNameInput(map.graphics.getGroupName(filter), function (name) {
                            return map.graphics.renameGroup(filter, name);
                        });
                    });
            }

            if (filter !== '*' && filter !== 'default') {
                $('<div>')
                    .addClass('button delete')
                    .attr('title', webgis.l10n.get('mapmarkup-group-remove'))
                    .appendTo($bar)
                    .on('click.webgis_graphics_info_container', function (e) {
                        e.stopPropagation();
                        const count = map.graphics.countGroupElements(filter);
                        if (count === 0) {
                            map.graphics.removeGroup(filter, false);
                            return;
                        }
                        webgis.confirm({
                            title: webgis.l10n.get('mapmarkup-group-remove'),
                            message: webgis.l10n.get('mapmarkup-group-remove-question')
                                .replace('{0}', map.graphics.getGroupName(filter))
                                .replace('{1}', count)
                                .replace('{2}', map.graphics.getGroupName('default')),
                            okText: webgis.l10n.get('mapmarkup-group-remove-move-elements'),
                            maybeText: webgis.l10n.get('mapmarkup-group-remove-with-elements'),
                            cancelText: webgis.l10n.get('cancel'),
                            onOk: function () { map.graphics.removeGroup(filter, false); },
                            onMaybe: function () { map.graphics.removeGroup(filter, true); }
                        });
                    });
            }

            if (filter === '*' && map.graphics.getGroups().length > 1) {
                $('<div>')
                    .addClass('webgis-graphics-info-groups-hint')
                    .text(webgis.l10n.get('mapmarkup-group-new-elements-to')
                        .replace('{0}', map.graphics.getGroupName(map.graphics.getActiveGroupId())))
                    .appendTo($bar);
            }
        },
        appendGroupVisibilityButton: function ($parent, map, groupId) {
            const $ = webgis.$ || jQuery;
            const visible = map.graphics.isGroupVisible(groupId);

            return $('<div>')
                .addClass('button group-visibility')
                .addClass(visible ? 'checked' : '')
                .attr('title', webgis.l10n.get(visible ? 'mapmarkup-group-hide' : 'mapmarkup-group-show'))
                .appendTo($parent)
                .on('click.webgis_graphics_info_container', function (e) {
                    e.stopPropagation();
                    map.graphics.setGroupVisible(groupId, !visible);
                });
        },
        appendPropertiesButton: function ($parent, $menu, map) {
            // (re)open the properties panel of the current drawing tool
            const $ = webgis.$ || jQuery;
            const self = $.fn.webgis_graphicsInfoContainer.plugin;

            if ($parent.hasClass('readonly') || !self.canShowProperties(map)) return;

            var $propertiesButton = $('<div>')
                .addClass('text-button')
                .attr('title', webgis.l10n.get('mapmarkup-show-properties'))
                .appendTo($menu)
                .on('click.webgis_graphics_info_container', function (e) {
                    e.stopPropagation();
                    map.graphics.showPropertiesPanel();
                });

            $('<div>')
                .addClass('button')
                .css({
                    backgroundImage: 'url(' + webgis.css.imgResource('content/api/img/graphics/ui/polygon-styles.png') + ')',
                })
                .appendTo($propertiesButton);

            $("<div>").addClass('text')
                .text(webgis.l10n.get('mapmarkup-symbology-tool').replace('{0}', self.translateType(map.graphics.getTool())))
                .appendTo($propertiesButton);
        },
        canShowProperties: function (map) {
            const tool = map.graphics.getTool();
            return !!tool && tool !== 'pointer';
        },
        itemBorder: function (element) {
            return '8px solid ' + (element.fill || element.stroke || element.color || '#fff');
        },
        itemIcon: function (element, symbolIconUrl) {
            return 'url(' + (symbolIconUrl || webgis.css.imgResource('rest/toolresource/webgis-tools-mapmarkup-mapmarkup-' + element.type)) + ')';
        },
        updateAllContainerItems: function () {
            // refresh color border & symbol of the existing list items in-place (eg. after changing the symbology of the selection)
            // => no rerendering: scroll position, selection, ... stays untouched
            const $ = webgis.$ || jQuery;
            const self = $.fn.webgis_graphicsInfoContainer.plugin;

            $('.webgis-graphics-info-container-holder').each(function () {
                const map = $(this).data('map');
                if (!map) return;

                const elements = map.graphics.getElements();

                $(this).find('.webgis-graphics-info-listitem').each(function () {
                    const $li = $(this);
                    const current = $li.data('element');
                    if (!current || !current.originalElement) return;

                    const element = elements.find(e => e.originalElement === current.originalElement);
                    if (!element) return;

                    element.updating = current.updating;
                    $li.data('element', element)
                        .css('borderLeft', self.itemBorder(element));

                    const symbolIconUrl = self.symbolIconUrl(element);
                    $li.children('.button.symbol')
                        .toggleClass('marker-icon', !!symbolIconUrl)
                        .css('backgroundImage', self.itemIcon(element, symbolIconUrl));
                });
            });
        },
        symbolIconUrl: function (element) {
            // type 'symbol': show the marker icon itself instead of the generic tool icon
            if (!element || element.type !== 'symbol' || !element.originalElement) return null;

            const frameworkElement = element.originalElement.frameworkElement;
            const icon = frameworkElement && frameworkElement.options ? frameworkElement.options.icon : null;
            if (icon && icon.options && icon.options.iconUrl) return icon.options.iconUrl;

            const symbol = element.originalElement.symbol;
            return symbol && typeof symbol.icon === 'string' ? symbol.icon : null;
        },
        fitListHeight: function ($ul) {
            // use the available space down to the bottom of the (scrollable) sidebar container
            const $ = webgis.$ || jQuery;

            if (!$ul.is(':visible')) return;

            let $container = null;
            $ul.parents().each(function (i, p) {
                const overflowY = $(p).css('overflow-y');
                if (overflowY === 'auto' || overflowY === 'scroll') {
                    $container = $(p);
                    return false;
                }
            });
            if (!$container) return;

            const containerBottom = $container.offset().top + $container[0].clientHeight;
            const available = Math.floor(containerBottom - $ul.offset().top - 10);

            if (available > 150) {
                $ul.css('height', available + 'px');
            }
        },
        selectedStyles: function (map, selectedElements) {
            // collect styles ids (stroke-color, fill-color, ...) => comma separated or null
            const styleTypes = new Set();

            for (let type of [... new Set(selectedElements.map(e => e.type))]) { // Set => distict => only unique values
                const styleType = map.graphics.getStyleTypes(type);

                if (!styleType || !styleType.styles) continue;

                for (let style of styleType.styles) {
                    styleTypes.add(style);
                }
            }

            return styleTypes.size > 0 ? Array.from(styleTypes).toString() : null;
        },
        selectElement: function ($li, elements, element, key) {
            const $ = webgis.$ || jQuery;
            const self = $.fn.webgis_graphicsInfoContainer.plugin;

            if (key == 'ctrl') {
                element.originalElement.selected = element.originalElement.selected ? false : true;
            } else if (key == 'shift') {
                if (self.getSelectedCount(elements, element) == 0) {
                    element.originalElement.selected = true;
                } else {
                    let elementIndex = self.getElementIndex(elements, element);
                    let closestIndex = self.getClosestSelected(elements, element);
                    closestIndex = closestIndex >= 0 ? closestIndex : self.getNextSelectedIndex(elements, element);

                    if (closestIndex < 0) {
                        element.originalElement.selected = true;
                    } else {
                        let from = Math.min(elementIndex, closestIndex), to = Math.max(elementIndex, closestIndex);

                        for (let i = from; i <= to; i++) {
                            elements[i].originalElement.selected = true;
                        }
                    }
                }
            } else {
                const selectedValue = element.originalElement.selected ? false : true;
                elements.forEach((el) => el.originalElement.selected = false);
                element.originalElement.selected = selectedValue;
            }

            $li.closest('.webgis-graphics-info-list').children().each(function (i, li) {
                var el = $(li).data('element');
                if (el) {
                    if (el.originalElement.selected)
                        $(li).addClass('selected');
                    else
                        $(li).removeClass('selected');
                }
            });

            self.buildMenu($li.closest('.webgis-graphics-info-container-holder'));
            self.fitListHeight($li.closest('.webgis-graphics-info-list'));

            // desktop: show the symbology of the selected elements in the (right docked) properties panel
            const map = $li.closest('.webgis-graphics-info-container-holder').data('map');
            if (map && webgis.usability.mapMarkupAutoOpenProperties !== false && !webgis.isMobileDevice()) {
                const selectedElements = map.graphics.selectedElements();
                const styles = selectedElements.length > 0 ? self.selectedStyles(map, selectedElements) : null;
                if (styles) {
                    map.graphics.showSelectionPropertiesPanel(styles);
                } else {
                    map.graphics.onSelectionCleared();
                }
            }
        },
        getSelectedCount: function (elements, excludeElement) {
            return elements.filter(el => el != excludeElement && el.originalElement.selected === true).length;
        },
        getElementIndex: function (elements, element) {
            return elements.indexOf(element);
        },
        getClosestSelected: function (elements, element) {
            let index = elements.indexOf(element);
            for (let i = index - 1; i >= 0; i--) {
                if (elements[i].originalElement.selected === true) return i;
            }
            return -1;
        },
        getNextSelectedIndex: function (elements, element) {
            let index = elements.indexOf(element);
            for (let i = index + 1; i < elements.length; i++) {
                if (elements[i].originalElement.selected === true) return i;
            }
            return -1;
        },
        translateType: function (type) {
            return webgis.l10n.get("mapmarkup-tool-" + type);
        }
    }
});

// static helpers for callers outside this file (no plugin instance needed)
webgis.ui.hasGraphicsInfoContainers = function (options) {
    return (webgis.$ || jQuery).fn.webgis_graphicsInfoContainer.plugin.hasContainers(options);
};
webgis.ui.refreshGraphicsInfoContainers = function (options) {
    (webgis.$ || jQuery).fn.webgis_graphicsInfoContainer.plugin.refreshAllContainers(options);
};
webgis.ui.updateGraphicsInfoContainerItems = function () {
    (webgis.$ || jQuery).fn.webgis_graphicsInfoContainer.plugin.updateAllContainerItems();
};
