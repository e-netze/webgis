// WebGIS UI Plugins (tabs)
// webgis_tabs
// Tab bar (.webgis-tabs-holder) + tab content holder for presentations, query results, tools,
// settings, errors and custom tool tabs, appended to the plugin element (e.g. #webgis-container).
// The plugin element is reused when a map is re-created in the same container (map builder),
// so calling webgis_tabs({ ... }) again re-initializes it (reinit).
// show/hide/hideAll/resize are also static methods for callers that only have the
// .webgis-tabs-holder element: webgis.ui.hideTab($holder, 'tools'), webgis.ui.hideAllTabs, webgis.ui.resizeTabs.

webgis.ui.definePlugin('webgis_tabs', {
    reinit: true,
    defaults: {
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
        header_buttons: []
    },
    init: function () {
        const $ = this.$, me = this, options = this.options, $parent = this.$el, parent = this.$el.get(0);
        const addHeaderButtons = this.constructor.addHeaderButtons;

        options.options_presentations.map =
            options.options_settings.map =
            options.options_tools.map =
            options.options_queryResults.map = options.map;

        this._mapEvents = [];

        webgis.log('webgis_tabs.options', options)
        parent._map = options.map;
        var $div = this._$div = $("<div class='webgis-tabs-holder webgis-ui-trans'></div>")
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
        var $tab_content = this._$tabContent = $("<div class='webgis-tabs-tab-content-holder'></div>").appendTo($parent)
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
            // encoded branch name (CmsBranches.Encode) => display the original name
            var currentBranch = webgis.hmac && webgis.hmac.getCurrentBranchName ? webgis.hmac.getCurrentBranchName() : '';
            if (currentBranch) {
                try { currentBranch = decodeURIComponent(currentBranch.replace(/_([0-9a-f]{2})/g, '%$1')); } catch (e) { }
            }

            var $header = $("<div></div>")
                .addClass("webgis-tabs-tab-header")
                .css("display", "none")
                .css("position", "relative")
                .attr("id", "tab-presentations-header")
                .text(webgis.l10n.get("content") + (currentBranch ? ' [' + webgis.l10n.get('branch') + ': ' + currentBranch + ']' : ''))
                .appendTo($tab_content);

            addHeaderButtons($header, options, "presentations");

            if ($.fn.webgis_presentationToc) {
                $("<div></div>")
                    .css({
                        position: 'absolute', right: 3, top: 3, width: 32, height: 32, cursor: 'pointer', margin: -2,
                        backgroundRepeat: 'no-repeat', backgroundPosition: 'center',
                        backgroundImage: 'url(' + webgis.css.imgResource('legend-24.png', 'toc') + ')',
                        backgroundSize: 26
                    })
                    .appendTo($header)
                    .data('map', options.map)
                    .on('click.webgis_tabs', function (e) {
                        e.stopPropagation();
                        var map = $(this).data('map');
                        webgis.ui.showServicesLegend({ map: map, sender: this });
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

            $("<div id='tab-queryresults-content-results'></div>")
                .appendTo($contentDiv)
                .webgis_queryResultsHolder(options.options_queryResults);

            if (options.map) {
                this._onMapEvent('showqueryresults', function (e, args) {
                    if (!args.map.ui.getQueryResultTabControl()) {
                        $("#tab-queryresults-header").text("Karten Marker");

                        if (args && args.map && args.map.queryResultFeatures) {
                            switch (args.map.queryResultFeatures.queryTool()) {
                                case "identify":
                                case "pointidentify":
                                case "lineidentify":
                                case "polygonidentify":
                                    $("#tab-queryresults-header").text(webgis.l10n.get("query-results"));
                                    break;
                                case "search":
                                case "query":
                                    $("#tab-queryresults-header").text(webgis.l10n.get("search-results"));
                                    break;
                                case "buffer":
                                    $("#tab-queryresults-header").text(webgis.l10n.get("buffer-results"));
                                    break;
                            }
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
                });
                this._onMapEvent('clearqueryresults', function (e) {
                    $(this).webgis_tabs('hide', 'queryresults');
                    $(this).find('#tab-queryresults').css('display', 'none');
                    $(this).webgis_tabs('resize');
                });
                this._onMapEvent('hidequeryresults', function (e) {
                    $(this).webgis_tabs('hide', 'queryresults');
                });
                this._onMapEvent('hidepresentations', function (e) {
                    $(this).webgis_tabs('hide', 'presentations');
                });
                this._onMapEvent('queryresult_removefeature', function (e) {
                    // Refresh Selection
                    var $button = $(this).find('.webgis-selection-button.selected');
                    $button.removeClass('selected');
                    if ($button.length > 0) {
                        webgis.log('trigger first selection button');
                        $button.first().trigger('click');
                    }
                });

                // die handler waren vorher im webgis_topbar (Zeile 217) => hier besser aufgehoben, weil Topbar muss nicht in jeder Karte drin sein.
                this._onMapEvent('queryresult_removefeature', function (e, map, oid) {
                    var $li = $(".webgis-result[data-id='" + oid + "']");
                    if ($li.hasClass($.fn.webgis_queryResultsList.selectedItemClass)) {
                        webgis.log('remove', map.getSelection('query'))
                        map.getSelection('query').remove();
                    }
                    var $holder = $li.closest('.webgis-search-result-list');
                    $holder.find('.webgis-results-counter').html(map.queryResultFeatures.count());
                    $li.remove();
                    if ($holder.find('.webgis-result').length === 1) { // einzelnen Ergebnis kann man nicht mehr löschen -> ganze Abfrage löschen 
                        $holder.find('.webgis-result').find('.webgis-result-remove').remove();
                    }
                });
                this._onMapEvent('queryresult_replacefeature', function (e, map, feature) {
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
                });
            }
        }
        if (options.add_tools === true || options.add_tool_content === true) {
            var $tab = $("<div id='tab-tools' class='webgis-tabs-tab' style='position:relative'><img src='" + webgis.css.imgResource('tools.png', 'toolbar') + "' /><div style='display:none' class='webgis-tabs-tab-text' id='tab-tools-content-activetool'></div><div style='display:none' class='webgis-tabs-tab-close'>✖</div></div>").appendTo($div);
            var $header = $("<div id='tab-tools-header' class='webgis-tabs-tab-header' style='display:none'>" + webgis.l10n.get("current-tool") + ":<div class='webgis-tabs-close'></div></div>")
                .appendTo($tab_content);

            addHeaderButtons($header, options, "tools");

            options.options_tools.fireEvent = true;
            var $content = $("<div id='tab-tools-content' class='webgis-tabs-tab-content' style='display:none'></div>").appendTo($tab_content).webgis_toolbox(options.options_tools);
            if (options.add_tools !== true) {
                $content.addClass('no-toolbox');
                $content.empty();
                if (options.map) {
                    this._onMapEvent('onchangeactivetool', function (e, m) {
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
                    });
                    this._onMapEvent('requirebuttonui', function (e, m) {
                        $tab.css('display', '');
                        if (!$tab.hasClass('webgis-tabs-tab-selected'))
                            $tab.trigger('click');
                        $(this).webgis_tabs('resize');
                    });
                }
            }
            if (options.map) {
                this._onMapEvent('onchangeactivetool', function (e, m) {
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
                        var imageUrl = webgis.css.imgResource(tool.image, 'tools');

                        $img.attr('src', imageUrl).css({ padding: '3px', height: '26px' });
                        $close.css('display', webgis.isMobileDevice() ? 'none' : '');
                    } else {
                        $img.attr('src', webgis.css.imgResource('tools.png', 'toolbar')).css({ padding: '', height: '' });
                        $close.css('display', 'none');
                    }

                });

                $tab.find('.webgis-tabs-tab-close').on('click.webgis_tabs', function (e) {
                    e.stopPropagation();

                    var toolsContent = $($(this).closest('.webgis-tabs-tab').get(0).tab_content).find('#tab-tools-content');  // WTF
                    var toolDialogHeader = toolsContent.find('.webgis-tooldialog-header');
                    if (toolDialogHeader.length === 1) {
                        toolDialogHeader.trigger('click');
                    }
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
            var $errorTab = $("<div id='tab-errors' class='webgis-tabs-tab' style='position:relative;display:none'><img src='" + webgis.css.imgResource('error-32.png', 'toolbar') + "' /><div style='display:none' class='webgis-tabs-tab-text webgis-error-counter'></div></div>").appendTo($div);
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
                        .on('click.webgis_tabs', function () {
                            webgis.tools.onButtonClick($(this).data('map'), $(this).data('tool'));
                        });
                }
            }
        }

        $div.find('.webgis-tabs-tab').each(function (i, e) {
            e.tab_content = $tab_content.get(0);
            $(e).on('click.webgis_tabs', function () {
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
                    $(this.tab_content).find('#' + this.id + '-content').slideDown();
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
                $(e).on('click.webgis_tabs', function () {
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

        me.resize();
        if (options.selected !== null) {
            $div.find('#tab-' + options.selected).trigger('click');
        }
    },
    destroy: function () {
        const map = this.options.map;
        if (map && map.events && this._mapEvents) {
            this._mapEvents.forEach(function (sub) {
                map.events.off(sub.channel, sub.fn);
            });
        }
        this._mapEvents = [];

        // removes also the plugins inside (toolbox, presentationToc, servicesToc, errors, ...)
        if (this._$div) this._$div.remove();
        if (this._$tabContent) this._$tabContent.remove();
        this._$div = this._$tabContent = null;
    },
    methods: {
        show: function (options) {
            this.constructor.show(this.$el, options);
        },
        hide: function (options) {
            this.constructor.hide(this.$el, options);
        },
        hideAll: function (options) {
            this.constructor.hideAll(this.$el, options);
        },
        resize: function (options) {
            this.constructor.resize(this.$el, options);
        },
        // map event subscription with the plugin element as context (this), released in destroy
        _onMapEvent: function (channel, fn) {
            this.options.map.events.on(channel, fn, this.$el.get(0));
            this._mapEvents.push({ channel: channel, fn: fn });
        }
    },
    staticMethods: {
        // $target: plugin element or any element containing the .webgis-tabs-holder (or the holder itself)
        show: function ($target, options) {
            const $ = webgis.$ || jQuery;
            var tab = (typeof options === 'string') ? options : options.tab;
            $($target).find('#tab-' + tab).removeClass('webgis-tabs-tab-selected').trigger('click');
        },
        hide: function ($target, options) {
            const $ = webgis.$ || jQuery;
            var tab = (typeof options === 'string') ? options : options.tab;
            if ($($target).find('#tab-' + tab).hasClass('webgis-tabs-tab-selected')) {
                $($target).find('#tab-' + tab).trigger('click');
            }
        },
        hideAll: function ($target, options) {
            const $ = webgis.$ || jQuery;
            $($target).find('.webgis-tabs-tab-selected').trigger('click');
        },
        resize: function ($target, options) {
            const $ = webgis.$ || jQuery;
            var count = 0;
            $($target).find('.webgis-tabs-tab').each(function (i, e) {
                if ($(e).css('display') !== 'none')
                    count++;
            });
            var width = 99.99 / Math.max(1, count) + '%';
            $($target).find('.webgis-tabs-tab').css('width', width);
        },
        addHeaderButtons: function ($header, options, name) {
            const $ = webgis.$ || jQuery;
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
                        $button.on('click.webgis_tabs', function (e) {
                            e.stopPropagation();
                            button.click(e);
                        });

                    left += $button.width();
                }
                $header.css('padding-left', left + 4);
            }
        }
    }
});

// static helpers for callers outside this file (no plugin instance needed)
// $target: plugin element or any element containing the .webgis-tabs-holder (or the holder itself)
webgis.ui.hideTab = function ($target, options) {
    (webgis.$ || jQuery).fn.webgis_tabs.plugin.hide($target, options);
};
webgis.ui.hideAllTabs = function ($target) {
    (webgis.$ || jQuery).fn.webgis_tabs.plugin.hideAll($target);
};
webgis.ui.resizeTabs = function ($target) {
    (webgis.$ || jQuery).fn.webgis_tabs.plugin.resize($target);
};
