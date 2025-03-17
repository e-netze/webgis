(function ($) {
    "use strict";
    $.fn.webgis_tabs = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_tabs');
        }
    };
    var defaults = {
        top: null,
        right: 0,
        left: null,
        bottom: null,
        width: '100%',
        height: 40,
        content_size: 'normal',
        selected: null,
        add_presentations: true,
        add_settings: true,
        add_tools: true,
        add_queryResults: true,
        add_custom: null,
        options_presentations: {},
        options_settings: {},
        options_tools: {},
        options_queryResults: {},
        header_buttons:[]
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            options.options_presentations.map =
                options.options_settings.map =
                    options.options_tools.map =
                        options.options_queryResults.map = options.map;
            return this.each(function () {
                new initUI(this, options);
            });
        },
        show: function (options) { 
            var tab = (typeof options === 'string') ? options : options.tab;
            $(this).find('#tab-' + tab).removeClass('webgis-tabs-tab-selected').trigger('click');
        },
        hide: function (options) {
            var tab = (typeof options === 'string') ? options : options.tab;
            if ($(this).find('#tab-' + tab).hasClass('webgis-tabs-tab-selected')) {
                $(this).find('#tab-' + tab).trigger('click');
            }
        },
        hideAll: function (options) {
            $(this).find('.webgis-tabs-tab-selected').trigger('click');
        },
        resize: function (options) {
            var count = 0;
            $(this).find('.webgis-tabs-tab').each(function (i, e) {
                if ($(e).css('display') !== 'none')
                    count++;
            });
            var width = 99.99 / Math.max(1, count) + '%';
            $(this).find('.webgis-tabs-tab').css('width', width);
        }
    };
    var initUI = function (parent, options) {
        var $parent = $(parent);
        parent._map = options.map;
        var $div = $("<div class='webgis-tabs-holder webgis-ui-trans'></div>")
            .appendTo($parent)
            .css({
                position: "absolute",
                left: options.left,
                top: options.top,
                right: options.right,
                bottom: options.bottom,
                height: options.height,
                width: options.width
        });
        var top = null, bottom = null;
        try {
            top = parseInt(options.top);
            if (isNaN(top))
                top = null;
        }
        catch (e) { }
        try {
            bottom = parseInt(options.bottom);
            if (isNaN(bottom))
                bottom = null;
        }
        catch (e) { }
        var $tab_content = $("<div class='webgis-tabs-tab-content-holder'></div>").appendTo($parent)
            .css({
            position: "absolute",
            left: options.left,
            right: options.right,
            top: top !== null ? parseInt(top + options.height + 1) : null,
            bottom: bottom !== null ? parseInt(bottom + options.height) : null,
            width: options.width
        });
        if (top !== null)
            $tab_content.addClass('under');
        if (options.content_size == 'fill') {
            $tab_content.css({ maxWidth: '100%', maxHeight: '100%' });
            if (top !== null) {
                $tab_content.css({ left: 0, right: 0, bottom: 0 });
            }
            else if (bottom !== null) {
                $tab_content.css({ left: 0, right: 0, top: 0 });
            }
            else {
                $tab_content.css({ left: 0, right: 0, bottom: 0, top: 0 });
            }
        }
        if (options.add_presentations) {
            $("<div id='tab-presentations' class='webgis-tabs-tab'><img src='" + webgis.css.imgResource('presentations.png', 'toolbar') + "' /></div>").appendTo($div);
            //$("<div id='tab-presentations-header' class='webgis-tabs-tab-header' style='display:none'>Darstellung<div class='webgis-tabs-close'></div></div>").appendTo($tab_content);
            var currentBranch = webgis.localStorage.get('currentBranch');

            var $header = $("<div></div>")
                .addClass("webgis-tabs-tab-header")
                .css("display", "none")
                .attr("id", "tab-presentations-header")
                .text("Darstellung" + (currentBranch ? '[' + currentBranch + ']' : ''))
                .appendTo($tab_content);

            addHeaderButtons($header, options, "presentations");

            if ($.fn.webgis_presentationToc) {
                $("<div></div>")
                    .css({
                        float: 'right', width: 32, height: 32, cursor: 'pointer', margin: -2,
                        backgroundRepeat: 'no-repeat', backgroundPosition: 'center',
                        backgroundImage: 'url(' + webgis.css.imgResource('legend-24.png', 'toc') + ')'
                    })
                    .appendTo($header)
                    .data('map',options.map)
                    .click(function (e) {
                        e.stopPropagation();
                        var map = $(this).data('map');
                        $(null).webgis_presentationToc('show_services_legend', { map: map, sender:this });
                    });
            }
            $("<div id='tab-presentations-content' class='webgis-tabs-tab-content' style='display:none'></div>").appendTo($tab_content).webgis_presentationToc(options.options_presentations);
        }
        if (options.add_queryResults) {
            $("<div id='tab-queryresults' class='webgis-tabs-tab' style='position:relative;display:none'><img src='" + webgis.css.imgResource('markers.png', 'toolbar') + "' /><div style='display:none' class='webgis-tabs-tab-text' id='tab-queryresults-content-counter'></div></div>")
                .appendTo($div);

            var $header = $("<div id='tab-queryresults-header' class='webgis-tabs-tab-header' style='display:none'>Suchergebnisse<div class='webgis-tabs-close'></div></div>")
                .appendTo($tab_content);

            addHeaderButtons($header, options, "queryresults");

            var $contentDiv = $("<div id='tab-queryresults-content' class='webgis-tabs-tab-content' style='display:none'>")
                .appendTo($tab_content);

            //$("<div id='tab-queryresults-content-menu' class='webgis-tabs-tab-menu'></div>")
            //    .appendTo($contentDiv);
            $("<div id='tab-queryresults-content-results'></div>")
                .appendTo($contentDiv)
                .webgis_queryResultsHolder(options.options_queryResults);

            if (options.map) {
                options.map.events.on(['ui-builder-targetelement-changed', 'ui-builder-collapsable-expanded'], function (e, sender, args) {
                    var target = args.target;
                    if (!target && args.$element) {
                        target = '#' + args.$element.parent().attr('id');
                    }
                }, parent);

                options.map.events.on('showqueryresults', function (e, args) {
                    if (!args.map.ui.getQueryResultTabControl()) {
                        $("#tab-queryresults-header").text("Karten Marker");

                        if (args && args.map && args.map.queryResultFeatures) {
                            switch (args.map.queryResultFeatures.queryTool()) {
                                case "identify":
                                case "pointidentify":
                                case "lineidentify":
                                case "polygonidentify":
                                    $("#tab-queryresults-header").text("Abfrage-Ergebnisse");
                                    break;
                                case "search":
                                case "query":
                                    $("#tab-queryresults-header").text("Suchergebnisse");
                                    break;
                                case "buffer":
                                    $("#tab-queryresults-header").text("Nachbarschaftsberechnung");
                                    break;
                            }
                        } else {

                        }

                        if (!args || !args.suppressShowResult) {
                            $(this).webgis_tabs('show', 'queryresults');
                        }
                        $(this).find('#tab-queryresults').css('display', '');
                        $(this).webgis_tabs('resize');
                        if (args && args.buttons) {
                            $('#tab-queryresults-content-results').webgis_queryResultsHolder('showbuttons', args.buttons);
                        }
                    }
                }, parent);
                options.map.events.on('clearqueryresults', function (e) {
                    $(this).webgis_tabs('hide', 'queryresults');
                    $(this).find('#tab-queryresults').css('display', 'none');
                    $(this).webgis_tabs('resize');
                }, parent);
                options.map.events.on('hidequeryresults', function (e) {
                    $(this).webgis_tabs('hide', 'queryresults');
                }, parent);
                options.map.events.on('hidepresentations', function (e) {
                    $(this).webgis_tabs('hide', 'presentations');
                }, parent);
                options.map.events.on('queryresult_removefeature', function (e) {
                    // Refresh Selection
                    var $button = $(this).find('.webgis-selection-button.selected');
                    $button.removeClass('selected');
                    if ($button.length > 0) {
                        console.log('trigger first selection button');
                        $button.first().trigger('click');
                    }
                }, parent);

                // die handler waren vorher im webgis_topbar (Zeile 217) => hier besser aufgehoben, weil Topbar muss nicht in jeder Karte drin sein.
                options.map.events.on('queryresult_removefeature', function (e, map, oid) {
                    var $li = $(".webgis-result[data-id='" + oid + "']");
                    if ($li.hasClass($.fn.webgis_queryResultsList.selectedItemClass)) {
                        console.log('remove', map.getSelection('query'))
                        map.getSelection('query').remove();
                    }
                    var $holder = $li.closest('.webgis-search-result-list');
                    $holder.find('.webgis-results-counter').html(map.queryResultFeatures.count());
                    $li.remove();
                    if ($holder.find('.webgis-result').length === 1) { // einzelnen Ergebnis kann man nicht mehr löschen -> ganze Abfrage löschen 
                        $holder.find('.webgis-result').find('.webgis-result-remove').remove();
                    }
                }, parent);
                options.map.events.on('queryresult_replacefeature', function (e, map, feature) {
                    // Update Feature List
                    var $li = $(".webgis-geojuhu-result.webgis-result[data-id='" + feature.oid + "']");
                    $li
                        .children('.webgis-result-fulltext')
                        .text(feature.properties._fulltext);

                    // Update Feature Result Table (DockPanel)
                    var $panel = $(".webgis-dockpanel.webgis-result[data-id='" + feature.oid + "']");
                    if ($panel.length > 0 && !$panel.hasClass('webgis-result-table')) {
                        map.queryResultFeatures.showFeatureTable(feature);
                    }
                }, parent);
            }
        }
        if (options.add_tools === true || options.add_tool_content === true) {
            var $tab = $("<div id='tab-tools' class='webgis-tabs-tab' style='position:relative'><img src='" + webgis.css.imgResource('tools.png', 'toolbar') + "' /><div style='display:none' class='webgis-tabs-tab-text' id='tab-tools-content-activetool'></div><div style='display:none' class='webgis-tabs-tab-close'>✖</div></div>").appendTo($div);
            var $header = $("<div id='tab-tools-header' class='webgis-tabs-tab-header' style='display:none'>Werkzeuge<div class='webgis-tabs-close'></div></div>")
                .appendTo($tab_content);

            addHeaderButtons($header, options, "tools");

            options.options_tools.fireEvent = true;
            var $content = $("<div id='tab-tools-content' class='webgis-tabs-tab-content' style='display:none'></div>").appendTo($tab_content).webgis_toolbox(options.options_tools);
            if (options.add_tools !== true) {
                $content.addClass('no-toolbox'); //.css('display', 'none');
                $content.empty();
                if (options.map) {
                    options.map.events.on('onchangeactivetool', function (e, m) {
                        var tool = m.getActiveTool();
                        var $tab = $(this).find('#tab-tools');
                        if (tool == null || tool.id.indexOf(webgis._defaultToolPrefix) === 0 || tool.hasui === false) {
                            $tab.css('display', 'none');
                            if ($tab.hasClass('webgis-tabs-tab-selected'))
                                $(this).find('.webgis-tabs-tab').first().trigger('click');
                        }
                        else {
                            $tab.css('display', '');
                            if (!$tab.hasClass('webgis-tabs-tab-selected'))
                                $tab.trigger('click');
                        }
                        $(this).webgis_tabs('resize');
                    }, parent);
                    options.map.events.on('requirebuttonui', function (e, m) {
                        $tab.css('display', '');
                        if (!$tab.hasClass('webgis-tabs-tab-selected'))
                            $tab.trigger('click');
                        $(this).webgis_tabs('resize');
                    }, parent);
                }
            }
            if (options.map) {
                options.map.events.on('onchangeactivetool', function (e, m) {
                    var tool = m.getActiveTool();
                    if (tool != null && (!tool._isDefaultTool || tool.hasui != false)) {
                        $($(this).find('#tab-tools-content-activetool')).html(tool.name).css('display', '');
                    }
                    else {
                        $($(this).find('#tab-tools-content-activetool')).html('').css('display', 'none');
                    }

                    var $img = $(m._webgisContainer).find('#tab-tools.webgis-tabs-tab').find('img');
                    var $close = $(m._webgisContainer).find('#tab-tools.webgis-tabs-tab').find('.webgis-tabs-tab-close');

                    if (tool && tool._isDefaultTool !== true && tool.image) {
                        //var imageUrl = tool.image && (tool.image.toLowerCase().indexOf('https://') === 0 || tool.image.toLowerCase().indexOf('http://') === 0) ?
                        //    tool.image :
                        //    webgis.baseUrl + '/' + tool.image;

                        var imageUrl = webgis.css.imgResource(tool.image, 'tools');

                        $img.attr('src', imageUrl).css({ padding: '3px', height: '26px' });
                        $close.css('display', webgis.isMobileDevice() ? 'none' : '');
                    } else {
                        $img.attr('src', webgis.css.imgResource('tools.png', 'toolbar')).css({ padding: '', height: '' });
                        $close.css('display', 'none');
                    }

                }, parent);

                $tab.find('.webgis-tabs-tab-close').click(function (e) {
                    e.stopPropagation();

                    var toolsContent = $($(this).closest('.webgis-tabs-tab').get(0).tab_content).find('#tab-tools-content');  // WTF
                    var toolDialogHeader = toolsContent.find('.webgis-tooldialog-header');
                    if (toolDialogHeader.length === 1) {
                        toolDialogHeader.trigger('click');
                    }
                    //options.map.setActiveTool(null);
                });
            }
        }
        if (options.add_settings) {
            $("<div id='tab-settings' class='webgis-tabs-tab'><img src='" + webgis.css.imgResource('settings.png', 'toolbar') + "' /></div>").appendTo($div);
            var $header = $("<div id='tab-settings-header' class='webgis-tabs-tab-header' style='display:none'>Dienste<div class='webgis-tabs-close'></div></div>")
                .appendTo($tab_content);

            addHeaderButtons($header, options, "settings");

            $("<div id='tab-settings-content' class='webgis-tabs-tab-content' style='display:none'></div>").appendTo($tab_content).webgis_servicesToc(options.options_settings);
        }
        
        if (webgis.usability.showErrorsInTabs == true && options.map && $.fn.webgis_errors) {
            var $errorTab=$("<div id='tab-errors' class='webgis-tabs-tab' style='position:relative;display:none'><img src='" + webgis.css.imgResource('error-32.png', 'toolbar') + "' /><div style='display:none' class='webgis-tabs-tab-text webgis-error-counter'></div></div>").appendTo($div);
            var $header = $("<div id='tab-errors-header' class='webgis-tabs-tab-header' style='display:none'>Errors/Warnings<div class='webgis-tabs-close'></div></div>")
                .appendTo($tab_content);

            addHeaderButtons($header, options, "erros")

            $("<div id='tab-errors-content' class='webgis-tabs-tab-content' style='display:none'></div>").appendTo($tab_content).webgis_errors({
                map: options.map,
                tab_element_selector: $errorTab,
                allow_remove: webgis.usability.allowUserRemoveErrors
            });
        }
        if (options.add_custom) {
            if (options.add_custom.tools) {
                for (var t in options.add_custom.tools) {
                    var tool = options.add_custom.tools[t];
                    $("<div id='tab-settings' class='webgis-tabs-tab'><img style='width:32px;height:32px' src='" + webgis.css.imgResource(tool.image) + "' /></div>").appendTo($div)
                        .data('tool', tool).data('map', options.map)
                        .click(function () {
                            webgis.tools.onButtonClick($(this).data('map'), $(this).data('tool'));
                        });
                }
            }
        }

        $div.find('.webgis-tabs-tab').each(function (i, e) {
            e.tab_content = $tab_content.get(0);
            $(e).click(function () {
                // Bei 'fill' (Desktop Layout) sollte man Inhalt nicht wegklappen können
                if (options.content_size === 'fill') {
                    if ($(this).hasClass('webgis-tabs-tab-selected')) {
                        return;
                    }
                }
                var show = !$(this).hasClass('webgis-tabs-tab-selected');

                $(this.parentNode).find('.webgis-tabs-tab').removeClass('webgis-tabs-tab-selected');
                $(this.tab_content).find('.webgis-tabs-tab-content').css('display', 'none');
                $(this.tab_content).find('.webgis-tabs-tab-header').css('display', 'none');

                if (show) {
                    $(this.tab_content).find('#' + this.id + '-header').css('display', 'block');
                    $(this.tab_content).find('#' + this.id + '-content').slideDown(); //.css('display', 'block');
                    if (this.id === "tab-presentations")
                        $(this.tab_content).find('#' + this.id + '-content').webgis_presentationToc('show_first');
                    $(this).addClass('webgis-tabs-tab-selected');
                    $(this).closest('.webgis-tabs-holder').addClass('webgis-ui-trans-hover');

                    if (webgis.usability.toolSketchOnlyEditableIfToolTabIsActive === true) {
                        var $tabsHolder = $(this).closest('.webgis-tabs-holder');
                        if ($tabsHolder.children('#tab-tools').length === 1) {
                            var map = $tabsHolder.parent()[0]._map;
                            if (map && map.sketch) {
                                var activeTool = map.getActiveTool();
                                if (activeTool && !map.isDefaultTool(activeTool) && activeTool.sketch_only_editable_if_tool_tab_is_active === true) {
                                    map.sketch.setEditable(this.id === "tab-tools" && activeTool.tooltype && activeTool.tooltype.indexOf('sketch') === 0);
                                }
                            }
                        }
                    }
                }
                else {
                    $(this).closest('.webgis-tabs-holder').removeClass('webgis-ui-trans-hover');
                }
            });
        });

        $tab_content.find('.webgis-tabs-close').each(function (i, e) {
            // Bei 'fill' (Desktop Layout) sollte man in Inhalt nicht wegklappen können
            if (options.content_size === 'fill') {
                $(e).remove();
            } else {
                $(e).click(function () {
                    var $holder = $(this).closest('.webgis-tabs-tab-content-holder');
                    $holder.find('.webgis-tabs-tab-content').css('display', 'none');
                    $holder.find('.webgis-tabs-tab-header').css('display', 'none');
                    $holder.parent()
                        .find('.webgis-tabs-holder')
                        .removeClass('webgis-ui-trans-hover')
                        .find('.webgis-tabs-tab-selected')
                        .removeClass('webgis-tabs-tab-selected');
                });
            }
        });
        
        
        $parent.webgis_tabs('resize');
        if (options.selected !== null) {
            $div.find('#tab-' + options.selected).trigger('click');
        }
    };

    var addHeaderButtons = function ($header, options, name) {
        if (options && options.header_buttons && options.header_buttons[name] && Array.isArray(options.header_buttons[name])) {
            var left = 0;
            for (var b in options.header_buttons[name]) {
                var button = options.header_buttons[name][b];

                var $button = $("<div>")
                    .addClass("webgis-tab-header-button")
                    .css('left', left)
                    .css('background-image', 'url(' + button.img + ')')
                    .appendTo($header);
                if (button.click)
                    $button.click(function (e) {
                        e.stopPropagation();
                        button.click(e);
                    });

                left += $button.width();
            }
            $header.css('padding-left', left + 4);
        }
    }
})(webgis.$ || jQuery);
