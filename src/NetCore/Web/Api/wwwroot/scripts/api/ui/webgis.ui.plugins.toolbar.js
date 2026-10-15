// WebGIS UI Plugins (toolbar)
// webgis_toolbar
// Tool tabs (Portal top toolbar) built from the tool containers of the map;
// the tools must already be added to the map (webgis_toolbox, 'toolboxloaded' event)

webgis.ui.definePlugin('webgis_toolbar', {
    defaults: {
        map: null,
        containers: null
    },
    init: function () {
        const $ = this.$, me = this, o = this.options, $elem = this.$el;

        $elem.addClass('webgis-toolbar-holder').data('map', o.map);
        if (!o.containers)
            return;

        const $tab_ul = $("<ul>").addClass('webgis-toolbar-tabs').appendTo($elem);
        let index = 0, hasFavorites = false;

        $("<li>&nbsp;</li>").addClass('webgis-toolbar-tab favorites').attr('data-index', index).appendTo($tab_ul);
        const $favContent = $("<ul>")
            .addClass('webgis-toolbar-tabcontent favorites')
            .attr('data-index', index)
            .css('display', 'none')
            .appendTo($elem);
        index++;

        const containers = o.map._mapToolsToServerContainers(o.containers) || [];

        if (webgis.custom.tools) {
            const customTools = webgis.custom.tools.toArray();
            for (const t in customTools) {
                const customTool = customTools[t];
                const containerWithName = $.grep(containers, function (c) { return c.name == customTool.container });
                let container = containerWithName.length === 0 ? null : containerWithName[0];

                if (container === null) {
                    containers.push(container = {
                        name: customTool.container,
                    });
                }

                container.hasCustomTools = true;
            }
        }

        for (const c in containers) {
            const container = containers[c];
            if ((!container.tools || container.tools.length === 0) && !container.hasCustomTools)
                continue;

            $("<li>" + container.name + "</li>")
                .addClass('webgis-toolbar-tab')
                .attr('data-index', index)
                .appendTo($tab_ul);

            const $tabContent = $("<ul>")
                .addClass('webgis-toolbar-tabcontent')
                .attr('data-index', index)
                .attr('data-title', container.name)
                .css('display', 'none')
                .appendTo($elem);

            for (const t in container.tools || []) {
                const tool = o.map._tools[container.tools[t]];
                if (!tool)
                    continue;

                if (tool.favorite_priority) {
                    this._addToolButton($favContent, tool, true);
                    hasFavorites = true;
                }
                this._addToolButton($tabContent, tool);
            }

            // CustomTools
            if (container.hasCustomTools && webgis.custom.tools) {
                const customTools = $.grep(webgis.custom.tools.toArray(), function (t) { return t.container === container.name });
                for (const t in customTools) {
                    const customTool = customTools[t];
                    if (o.map.getTool(customTool.id) == null)
                        o.map.addTool(customTool);

                    this._addToolButton($tabContent, customTool);
                }
            }
            index++;
        }

        if (webgis.help.url) {
            const $helpButton = $("<li>?</li>").addClass('webgis-toolbar-tab help').appendTo($tab_ul);

            if (o.map) {
                this._onChangeActiveTool = function (e, m) {
                    const tool = m.getActiveTool();
                    if (m.isDefaultTool(tool)) {
                        $helpButton.css('background-image', '');
                        if (tool.help_urlpath_defaulttool) {
                            $helpButton.addClass('help-exists');
                        } else {
                            $helpButton.removeClass('help-exists');
                        }
                    } else {
                        if (tool && tool.help_urlpath) {
                            $helpButton.addClass('help-exists')
                                .css('background-image', 'url(' + webgis.css.imgResource(tool.image, 'tools') + ')');
                        } else {
                            $helpButton.removeClass('help-exists')
                                .css('background-image', '');
                        }
                    }
                };
                o.map.events.on('onchangeactivetool', this._onChangeActiveTool);
            }
        }

        if (hasFavorites === false) {
            $elem.find('.webgis-toolbar-tab.favorites').remove();
            $elem.find('.webgis-toolbar-tabcontent.favorites').remove();
        }

        $tab_ul.children('.webgis-toolbar-tab')
            .on('click.webgis_toolbar', function () {
                const $tab = $(this);

                if ($tab.hasClass('help')) {
                    const map = o.map;
                    const activeTool = map.getActiveTool();
                    webgis.showHelp(activeTool ?
                        (map.isDefaultTool(activeTool) && activeTool.help_urlpath_defaulttool ? activeTool.help_urlpath_defaulttool : activeTool.help_urlpath) :
                        null,
                        map);
                } else {
                    $tab_ul.find('.webgis-toolbar-tab').removeClass('selected');
                    $elem.find('.webgis-toolbar-tabcontent').css('display', 'none');
                    $tab.addClass('selected');
                    $elem.find(".webgis-toolbar-tabcontent[data-index='" + $tab.attr('data-index') + "']").css('display', 'block');
                }
            });

        $tab_ul.find('.webgis-toolbar-tab').first().trigger('click');

        this.constructor.applyToolDependencies($elem, o.map);
    },
    destroy: function () {
        if (this.options.map && this._onChangeActiveTool) {
            this.options.map.events.off('onchangeactivetool', this._onChangeActiveTool);
        }
    },
    methods: {
        selectCurrentToolTab: function () {
            const $selected = this.$el.find('.webgis-toolbox-tool-item.selected');
            if ($selected.length === 1) {
                const $tabContent = $selected.closest('.webgis-toolbar-tabcontent[data-index]');
                if ($tabContent.length === 1) {
                    const index = $tabContent.attr('data-index');
                    const $tab = this.$el.find(".webgis-toolbar-tab[data-index='" + index + "']");
                    if (!$tab.hasClass('selected'))
                        $tab.trigger('click');
                }
            }
        },
        _addToolButton: function ($tabContent, tool, useFavoritePriority) {
            const $ = this.$;

            if ($.inArray(tool.id, ["webgis.tools.addtoselection", "webgis.tools.removefromselection"]) >= 0) {   // do not show this in toolbar
                return;
            }

            const $li = $("<li class='webgis-toolbox-tool-item'>");

            let img = '';
            if (tool.image) {
                img = webgis.css.imgResource(tool.image, 'tools');
            }
            else {
                img = tool.hasui == true ? webgis.css.imgResource('enter-26.png', 'ui') : webgis.css.imgResource(tool.image, 'tools');
            }
            $("<span class='webgis-toolbox-tool-item-span'><img src='" + img + "' /><span class='webgis-toolbox-tool-item-label'>&nbsp;" + tool.name + "</span></span>")
                .webgis_ui_element({ tooltip: tool.name + ": " + tool.tooltip })
                .appendTo($li);

            if (useFavoritePriority === true && tool.favorite_priority) {
                $li.attr('data-priority', tool.favorite_priority);
                let $insertBefore = null;
                $tabContent.children().each(function (i, e) {
                    if ($insertBefore == null && $(e).attr('data-priority') && parseInt($(e).attr('data-priority')) > tool.favorite_priority)
                        $insertBefore = $(e);
                });
                if ($insertBefore !== null)
                    $li.insertBefore($insertBefore);
                else
                    $li.appendTo($tabContent);
            } else {
                $li.appendTo($tabContent);
            }

            const map = this.options.map;
            $li.on('click.webgis_toolbar', function () {
                webgis.tools.onButtonClick(map, $(this).data('tool'));
            });

            $li.data('tool', tool).attr('data-toolid', tool.id);

            return $li;
        }
    },
    staticMethods: {
        // adds the webgis-dependency-* classes of the tools to all tool buttons in $elem;
        // also used for other tool button lists (query results): webgis.ui.applyToolDependencies($elem, map)
        applyToolDependencies: function ($elem, map) {
            if (!map)
                return;

            const $ = webgis.$ || jQuery;

            $elem.find('.webgis-toolbox-tool-item[data-toolid]').each(function () {
                const $button = $(this);
                const tool = map.getTool($button.attr('data-toolid'));
                if (tool && tool.dependencies && tool.dependencies.length > 0) {
                    $button.addClass('webgis-dependencies');
                    for (let d = 0; d < tool.dependencies.length; d++) {
                        $button.addClass('webgis-dependency-' + tool.dependencies[d]);
                    }
                }
            });

            map.ui.refreshUIElements();
        }
    }
});

// static helper for callers outside this file (no plugin instance needed)
webgis.ui.applyToolDependencies = function ($elem, map) {
    (webgis.$ || jQuery).fn.webgis_toolbar.plugin.applyToolDependencies($elem, map);
};
