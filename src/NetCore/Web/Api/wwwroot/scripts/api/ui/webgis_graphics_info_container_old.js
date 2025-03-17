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
    var defaults = {
        map: null,
        readonly: false
    };
    var methods = {
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
    var initUI = function (elem, options) {
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
    var refresh = function (elem, options) {
        var $elem = elem ? $(elem) : $(this);

        $elem.empty();
        var map = options.map || $elem.data('map');
        var elements = map.graphics.getElements();
        var readOnly = $elem.hasClass('readonly');

        var funcs = readOnly === true ? [elementAny] : [elementIsCurrent, elementHasLabel, elementHasNoLabel];
        var headings = readOnly === true ? [""] : [" ", "Bestehende Objekte bearbeiten", ""];

        if (readOnly === false && hasCurrentElement(elements) === false) {
            var tool = map.graphics.getTool();
            $("<h2><img style='margin-right:10px' src='" + webgis.css.imgResource('rest/toolresource/webgis-tools-redlining-redlining-' + tool) + "'></h2>")
                .text(headings[0] + translateType(tool) + ":")
                .appendTo($elem);
            $("<div>")
                .addClass('webgis-info')
                .text(map.graphics.getToolDescription(tool))
                .appendTo($elem);
        }

        for (var f in funcs) {
            var func = funcs[f];
            var first = true;
            for (var i in elements) {
                var element = elements[i];

                if (readOnly === true && element.type === 'text')
                    continue;

                if (func(element) === false)
                    continue;

                if (first === true) {
                    first = false;

                    if (headings[f]) {
                        if (f === '0') {
                            $("<h2><img style='margin-right:10px' src='" + webgis.css.imgResource('rest/toolresource/webgis-tools-redlining-redlining-' + element.type) + "'>" + headings[f] + translateType(element.type) + ":</h2>").appendTo($elem);
                        } else {
                            $("<h3>").text(headings[f]).appendTo($elem);
                        }
                    }

                    var $ul = $("<ul>")
                        .addClass('webgis-graphics-info-list')
                        .appendTo($elem);
                }

                var $li = $("<li></li>")

                    .addClass('webgis-graphics-info-listitem')
                    .appendTo($ul)
                    .data('element', element)
                    .mouseenter(function (e) {
                        var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                        var element = $(this).data('element');
                        if (element && element.originalElement && element.originalElement.frameworkElement) {
                            map.graphics.addPreviewFromJson(element.originalElement.frameworkElement.toGeoJSON().geometry, false, true);
                        }
                    })
                    .mouseleave(function (e) {
                        var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                        map.graphics.removePreview();
                    })
                    .click(function (e) {
                        var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                        map.graphics.removePreview();

                        var element = $(this).data('element');
                        var readOnly = $(this).closest('.webgis-graphics-info-container-holder').hasClass('readonly');
                        if (readOnly === false) {
                            if (!element.isCurrent) {
                                map.graphics.setTool('pointer');
                                map.graphics._onElementClick.apply(element.originalElement, [e]);
                            }
                        } else {
                            $(this).parent().children('.selected').removeClass('selected');
                            $(this).addClass('selected');

                            if (!map.isEnabled()) {
                                map.enable(true);
                            }
                            map.graphics.zoomTo(element);
                        }
                    });

                var txt = translateType(element.type), txtCls = 'unlabeled';
                if (element.metaText && element.metaText.length > 0) {
                    txt = element.metaText;
                    txtCls = '';
                }

                if (element.isCurrent === true && readOnly === false) {
                    $li.addClass('selected');
                    $("<span>").text(metaTextLabel(element.type)).appendTo($li);
                    var $textarea = $("<textarea rows='3'>")
                        .addClass('webgis-textarea')
                        .appendTo($li);

                    if (element.type === 'text') {
                        $textarea.keyup(function () {
                            var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                            map.graphics.setMetaText($(this).val());
                        });
                    } else {
                        $textarea.change(function () {
                            var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                            map.graphics.setMetaText($(this).val());
                        });
                    }
                    if (txtCls === 'unlabeled') {
                        $textarea.attr("placeholder", txt);
                    } else {
                        $textarea.val(txt);
                    }
                    //$textarea.focus();
                    $("<div>")
                        .addClass('button apply webgis-button')
                        .appendTo($li)
                        .text('Übernehmen / Weiters Zeichnen')
                        .click(function (e) {
                            e.stopPropagation();
                            var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                            map.graphics.assumeCurrentElement();
                        });
                } else {
                    $li.css({
                        backgroundImage: 'url(' + webgis.css.imgResource('rest/toolresource/webgis-tools-redlining-redlining-' + element.type) + ')'
                    });

                    $("<span>")
                        .text(txt)    // Don't use html() here -> Javascript Injekction!!!!
                        .appendTo($li);
                    $li.addClass(txtCls);
                    if (readOnly && element.readonly_selected) {
                        $li.addClass('selected');
                    }
                }

                if (readOnly === false) {
                    if (element.type === 'polygon' || element.type === 'line' ||
                        (element.type === 'point' && element.metaText)) {
                        $("<div>")
                            .addClass('button label')
                            .appendTo($li)
                            .click(function (e) {
                                e.stopPropagation();

                                var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                                var element = $(this).closest('.webgis-graphics-info-listitem').data('element');

                                if (element.isCurrent) {
                                    map.graphics.labelElement(null);
                                } else {
                                    map.graphics.labelElement(element.originalElement);
                                }
                            });
                    }
                    $("<div>")
                        .addClass('button delete')
                        .appendTo($li)
                        .click(function (e) {
                            e.stopPropagation();
                            var map = $(this).closest('.webgis-graphics-info-container-holder').data('map');
                            var element = $(this).closest('.webgis-graphics-info-listitem').data('element');

                            if (element.isCurrent && !element.originalElement) {
                                map.graphics.removeCurrentElement();
                            } else {
                                map.graphics.removeElement(element.originalElement);
                            }
                        });
                }
            }
        }
    };


    var hasCurrentElement = function (elements) {
        for (var e in elements) {
            if (elements[e].isCurrent === true) {
                return true;
            }
        }
        return false;
    };
    var elementIsCurrent = function (element) { return element.isCurrent === true; };
    var elementHasLabel = function (element) {
        if (elementIsCurrent(element) === false) {
            if (element.metaText && element.metaText.length > 0)
                return true;
        }
        return false;
    };
    var elementHasNoLabel = function (element) { return elementIsCurrent(element) === false && elementHasLabel(element) === false; };
    var elementAny = function (element) { return true; };

    var translateType = function (type) {
        return webgis.i18n.get("redlining-tool-" + type);
    };

    var metaTextLabel = function (type) {
        switch (type) {
            case "text":
                return 'Text:';
        }

        return "Beschreibung (optional):";
    };
})(webgis.$ || jQuery);
