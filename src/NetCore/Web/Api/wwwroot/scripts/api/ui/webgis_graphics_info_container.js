(function ($) {
    "use strict";
    $.fn.webgis_graphicsInfoContainer = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_graphicsInfoContainer');
        }
    };
    let defaults = {
        map: null,
        readonly: false
    };
    const methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        refresh: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new refresh(this, options);
            });
        },
        refreshAllContainers: function (options) {
            if (options && options.type) {
                $('.webgis-graphics-info-container-holder.' + options.type).webgis_graphicsInfoContainer('refresh');
            } else {
                $('.webgis-graphics-info-container-holder').webgis_graphicsInfoContainer('refresh');
            }
        },
        hasContainers: function (options) {
            if (options) {
                if (options.type) {
                    return $('.webgis-graphics-info-container-holder.' + options.type).length > 0;
                }
            }
            return $('.webgis-graphics-info-container-holder').length > 0;
        },
        destroyReadOnlyContainers: function (options) {
            $('.webgis-graphics-info-container-holder.readonly').each(function (i, container) {
                var $container = $(container);
                if ($.fn.webgis_dockPanel) {
                    var $dockPanel = $(container).closest('.webgis-dockpanel');
                    if ($dockPanel.length > 0) {
                        $dockPanel.webgis_dockPanel('remove');
                    } else {
                        $container.remove();
                    }
                } else {
                    $container.remove();
                }
            });
        }
    };
    const initUI = function (elem, options) {
        var $elem = $(elem).addClass('webgis-graphics-info-container-holder');
        if (options.readonly === true) {
            $elem.addClass('readonly');
        } else {
            $(null).webgis_graphicsInfoContainer('destroyReadOnlyContainers');
            $elem.addClass('editable');
        }
        $elem.data('map', options.map);
        refresh(elem, options);
    };
    const refresh = function (elem, options) {
        const $elem = elem ? $(elem) : $(this);

        $elem.empty();
        const map = options.map || $elem.data('map');
        const elements = map.graphics.getElements();
        const readOnly = $elem.hasClass('readonly');

        if ($elem.hasClass('order-descending')) elements.reverse();

        const hasMultiSelection = getSelectedCount(elements) > 1;

        if (!readOnly && !map.graphics.hasSelectedElements() && !map.graphics.hasStagedElement()) {
            var tool = map.graphics.getTool();
            $("<h3><img style='margin-right:10px' src='" + webgis.css.imgResource('rest/toolresource/webgis-tools-redlining-redlining-' + tool) + "'></h2>")
                .text(translateType(tool) + " erstellen:")
                .appendTo($elem);
            $("<div>")
                .addClass('webgis-info')
                .text(map.graphics.getToolDescription(tool))
                .appendTo($elem);
        }

        $('<div>')
            .addClass('webgis-graphics-info-list-menu')
            .appendTo($elem);
        buildMenu($elem);

        var usedHeight = 0;
        
        $elem.children().each(function (i, e) {
            usedHeight += $(e).outerHeight(true);  // true => include margin, ...
        });

        //console.log('usedHeight', usedHeight);

        const $ul = $("<ul>")
            .addClass('webgis-graphics-info-list')
            .css('height', 'calc(100vh - ' + (440 + usedHeight) + 'px')  // 440 empiric
            .appendTo($elem);

        const hasStagedElement = map.graphics.hasStagedElement();
        const hasUpdatingElement = map.graphics.hasUpdatingElement();
        const hasStagedOrUpdatingElements = hasStagedElement || hasUpdatingElement;
        const stagedElement = map.graphics.getStagedElement();

        for (let element of elements) {
            if (readOnly === true && element.type === 'text')
                continue;

            const $li = $("<li></li>")
                .addClass('webgis-graphics-info-listitem' + (element.originalElement && element.originalElement.selected === true ? ' selected' : ''))
                .appendTo($ul)
                .data('element', element)

            if (!hasUpdatingElement) {
                $li.mouseenter(function (e) {
                        const map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                        const element = $(this).data('element');
                        if (element && !element.updating && element.originalElement && element.originalElement.frameworkElement) {
                            map.graphics.addPreviewFromJson(element.originalElement.frameworkElement.toGeoJSON().geometry, false, true);
                        }
                    })
                    .mouseleave(function (e) {
                        var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                        map.graphics.removePreview();
                    });

                if (!hasStagedOrUpdatingElements) {
                    $li.addClass('can-select').click(function (e) {
                        const $li = $(this);
                        const map = $(this).closest('.webgis-graphics-info-container-holder').data('map');

                        const element = $(this).data('element');
                        const readOnly = $(this).closest('.webgis-graphics-info-container-holder').hasClass('readonly');

                        if (readOnly === false) {
                            if (e.shiftKey) {
                                selectElement($li, elements, element, 'shift');
                            } else if (e.ctrlKey) {
                                selectElement($li, elements, element, 'ctrl');
                            } else {
                                //map.graphics.commitCurrentElement(true);
                                selectElement($li, elements, element);
                                //map.graphics.setTool('pointer');
                                //map.graphics._onElementClick.apply(element.originalElement, [e]);  // element will selected there
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
                txt = hasLabel ? element.metaText : translateType(element.type);

            $li.css({
                borderLeft: '8px solid ' + (element.fill || element.stroke || element.color || '#fff'),
            });

            const $btnSymbol = $("<div>")
                .addClass('button symbol')
                .addClass(element.updating ? '' : 'inactive')
                .css({
                    backgroundImage: 'url(' + webgis.css.imgResource('rest/toolresource/webgis-tools-redlining-redlining-' + element.type) + ')',
                })
                .appendTo($li);

            if (element.updating) {
                $btnSymbol.click(function (e) {
                    e.stopPropagation();

                    const element = $(this).closest('.webgis-graphics-info-listitem').data('element');
                    //if (!element.updating) {
                    //    map.graphics.commitCurrentElement(true);
                    //    map.graphics.setTool('pointer');
                    //    map.graphics._onElementClick.apply(element.originalElement, [e]);
                    //}

                    webgis.tools.onButtonClick(map,
                        { command: 'show-symbol-selector', type: 'servertoolcommand', map: map },
                        this,
                        null,
                        { 'redlinig-symbol-type': element.type });
                });
            }

            if (element.updating) {
                const $labelInput = $("<textarea rows='1'>")
                    .addClass('webgis-textarea')
                    .attr('placeholder', 'Label (optional)...')
                    .appendTo($li)
                    .val(hasLabel ? txt : '');
                if (element.type === 'text') {
                    $labelInput.keyup(function () {
                        let map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                        map.graphics.setMetaText($(this).val());
                    });
                } else {
                    $labelInput.change(function () {
                        let map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                        map.graphics.setMetaText($(this).val());
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
                        .click(function (e) {
                            e.stopPropagation();
                            var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                            var element = $(this).closest('.webgis-graphics-info-listitem').data('element');

                            map.graphics.commitCurrentElement();
                        });
                } else if(!hasUpdatingElement) {
                    if (element.type === 'polygon' || element.type === 'line' ||
                        (element.type === 'point' && element.metaText)) {
                        $("<div>")
                            .addClass('button label')
                            .appendTo($li)
                            .click(function (e) {
                                e.stopPropagation();

                                var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                                var element = $(this).closest('.webgis-graphics-info-listitem').data('element');

                                map.graphics.labelElement(element.originalElement);
                            });
                    }
                    $("<div>")
                        .addClass('button edit')
                        .appendTo($li)
                        .click(function (e) {
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
                        .click(function (e) {
                            e.stopPropagation();
                            var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                            var element = $(this).closest('.webgis-graphics-info-listitem').data('element');

                            map.graphics.removeElement(element.originalElement);
                        });
                }
            }
        }
    };

    const buildMenu = function ($parent) {
        const map = $parent.data('map');
        if (!map)
            return;

        const selectedElements = map.graphics.selectedElements();
        const $menu = $parent.find('.webgis-graphics-info-list-menu').empty();

        if (map.graphics.hasStagedElement() === true) { // show tools for selected
            $('<div>')
                .addClass('button delete')
                .attr('title', 'Element entfernen')
                .appendTo($menu)
                .click(function (e) {
                    e.stopPropagation();

                    map.graphics.removeCurrentElement();
                });
        } 
        else if (selectedElements.length > 0) {  // show tools for selected
            // collect styles ids (stroke-color, fill-color, ...)
            const styleTypes = new Set();

            for (let type of [... new Set(selectedElements.map(e => e.type))]) { // Set => distict => only unique values
                const styleType = map.graphics.getStyleTypes(type);

                if (!styleType || !styleType.styles) continue;

                for (let style of styleType.styles) {
                    styleTypes.add(style);
                }
            }

            if (styleTypes.size > 0) {  
                var $textButton = $('<div>')
                    .addClass('text-button')
                    .data('styles', Array.from(styleTypes).toString())
                    .appendTo($menu)
                    .click(function (e) {
                        e.stopPropagation();

                        if (map.graphics.hasUpdatingElement()) {
                            $(this)
                                .closest('.webgis-graphics-info-container-holder')
                                .find('.webgis-graphics-info-listitem.selected .button.symbol')
                                .trigger('click');

                            return;
                        }

                        webgis.tools.onButtonClick(map,
                            { command: 'show-single-symbol-selector', type: 'servertoolcommand', map: map },
                            this,
                            null,
                            { 'redlinig-styles': $(this).data('styles') });
                    });

                $('<div>')
                    .addClass('button')
                    .attr('title', 'Symbolik anpassen')
                    .css({
                        backgroundImage: 'url(' + webgis.css.imgResource('content/api/img/graphics/ui/polygon-styles.png') + ')',
                    })
                    .appendTo($textButton)
                    
                $("<div>").addClass('text').text('Symbolik').appendTo($textButton);
            }

            if (map.graphics.hasUpdatingElement() !== true) {
                $('<div>')
                    .addClass('button remove-selection')
                    .attr('title', 'Auswahl entfernen')
                    .appendTo($menu)
                    .click(function (e) {
                        e.stopPropagation();

                        map.graphics.clearElementSelection();
                        $parent.find('.webgis-graphics-info-listitem').removeClass('selected');
                        $menu.empty();
                    });

                $('<div>')
                    .addClass('button delete')
                    .attr('title', 'Ausgewählte Elemente entfernen')
                    .appendTo($menu)
                    .click(function (e) {
                        e.stopPropagation();

                        map.graphics.removeElements(selectedElements);
                    });
            }
        } else if (map.graphics.countElements() > 0) {  // show swap ordering
            $('<div>')
                .addClass('button order')
                .attr('title', 'Reihenfolge umkehren')
                .appendTo($menu)
                .click(function (e) {
                    e.stopPropagation();

                    const $holder = $(this).closest('.webgis-graphics-info-container-holder');
                    refresh($holder.toggleClass('order-descending'), {});
                });
        }
    }; 

    const getSelectedElements = function (elements) {
        return elements.filter(el => el.originalElement.selected === true);
    };
    const getSelectedCount = function (elements, excludeElement) {
        return elements.filter(el =>  el != excludeElement && el.originalElement.selected === true).length;
    };
    const getElementIndex = (elements, element) => elements.indexOf(element);
    const getClosestSlected = function (elements, element) {
        let index = getElementIndex(elements, element);
        for (let i = index - 1; i >= 0; i--) {
            if (elements[i].originalElement.selected === true) return i;
        }
        return -1;
    };
    const getNextSelectedIndex = function (elements, element) {
        let index = getElementIndex(elements, element);
        for (let i = index + 1; i < elements.length; i++) {
            if (elements[i].originalElement.selected === true) return i;
        }
        return -1;
    };
    const selectElement = function ($li, elements, element, key) {
        if (key == 'ctrl') {
            element.originalElement.selected = element.originalElement.selected ? false : true;
        } else if (key == 'shift') {
            if (getSelectedCount(elements, element) == 0) {
                element.originalElement.selected = true;
            } else {
                let elementIndex = getElementIndex(elements, element);
                let closestIndex = getClosestSlected(elements, element);
                closestIndex = closestIndex >= 0 ? closestIndex : getNextSelectedIndex(elements, element);

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

        buildMenu($li.closest('.webgis-graphics-info-container-holder'));
    }
    const elementIsCurrent = function (element) { return element.isCurrentNew === true; };
    const elementHasLabel = function (element) {
        if (elementIsCurrent(element) === false) {
            if (element.metaText && element.metaText.length > 0)
                return true;
        }
        return false;
    };
    const elementHasNoLabel = function (element) { return elementIsCurrent(element) === false && elementHasLabel(element) === false; };
    const translateType = function (type) {
        return webgis.i18n.get("redlining-tool-" + type);
    };
})(webgis.$ || jQuery);
