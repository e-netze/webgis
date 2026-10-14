// WebGIS UI Plugins (toolbox)
// webgis_toolbox
// Tool list grouped in collapsable containers (tools tab); loads the tool infos from the API once
// per element (elem.tools) and adds the tools to the map.
// The toolbox element is also the uiElement of its tools: after a tool is released the toolbox is
// rebuilt by calling webgis_toolbox({ map }) again on the same element (reinit).

webgis.ui.definePlugin('webgis_toolbox', {
    reinit: true,
    defaults: {
        map: null,
        fireEvent: false,
        containers: null   // null => default containers (staticMethods.defaultContainers)
    },
    init: function () {
        const $ = this.$, me = this, o = this.options, $elem = this.$el, elem = $elem.get(0);
        if ($elem.hasClass('no-toolbox'))
            return;

        elem._map = o.map;
        elem._containers = elem._containers && elem._containers.length > 0 ? elem._containers : (o.containers || this.constructor.defaultContainers());
        $elem.addClass('webgis-toolbox-holder');

        if (!elem.tools) {
            $elem.html("<img src='" + webgis.css.imgResource('loader1.gif', 'hourglass') + "' />...");
            webgis.toolInfos(o.tools, null, function (tools) {
                elem.tools = tools;
                if (o.usertools) {
                    const userContainer = { name: o.usertools_containername || "Benutzerdef. Werkzeuge", tools: [] };
                    for (const t in o.usertools) {
                        userContainer.tools.push(o.usertools[t].id);
                        elem.tools.push(o.usertools[t]);
                    }
                    elem._containers.push(userContainer);
                }
                // find Container Doubles
                for (let i = 0; i < elem._containers.length + 1; i++) {
                    for (let j = i + 1; j < elem._containers.length; j++) {
                        if (elem._containers[i].name === elem._containers[j].name) {
                            for (const t in elem._containers[j].tools) {
                                if ($.inArray(elem._containers[j].tools[t], elem._containers[i].tools, 0) < 0)
                                    elem._containers[i].tools.push(elem._containers[j].tools[t]);
                            }
                            elem._containers[j].tools = null;
                        }
                    }
                }

                for (const t in tools) {
                    if (me.constructor.containsTool(elem._containers, tools[t]))
                        o.map.addTool(tools[t]);
                }

                elem._containers = o.map._mapToolsToServerContainers(elem._containers);

                me._buildList();
                if ($elem.hasClass('no-toolbox'))
                    $elem.empty();

                if (o.fireEvent) {
                    elem._map.events.fire('toolboxloaded', elem._map);
                }

                $.fn.webgis_toolbox._toolsLoaded = true;
            });
        }
        else {
            this._buildList();
        }

        if (o.map) {
            webgis.delayed(function () {
                o.map.ui.refreshUIElements();
            }, 100);

            this._onChangeActiveTool = function (e, m) {
                const tool = m.getActiveTool();
                $('.webgis-toolbox-tool-item.selected').each(function () {
                    if (!$(this).hasClass('webgis-toggle-button'))
                        $(this).removeClass('selected');
                });
                if (tool) {
                    $(".webgis-toolbox-tool-item[data-toolid='" + tool.id + "']").addClass('selected');

                    // Wenn toolclick getriggert wurde, hier den richtigen Tab sichtbar schalten.
                    const $holder = $('.webgis-toolbar-holder');
                    if ($holder.length === 1 && $.fn.webgis_toolbar) {
                        $holder.webgis_toolbar('selectCurrentToolTab');
                    }
                }
            };
            o.map.events.on('onchangeactivetool', this._onChangeActiveTool);
        }
    },
    destroy: function () {
        if (this.options.map && this._onChangeActiveTool) {
            this.options.map.events.off('onchangeactivetool', this._onChangeActiveTool);
        }
    },
    methods: {
        _buildList: function () {
            const $ = this.$, me = this, $elem = this.$el, elem = $elem.get(0);
            $elem.empty();
            const tools = elem.tools;

            const containers = elem._containers;
            const $ul = $("<ul id='webgis-toolbox-list'></ul>").appendTo($elem);
            let hasFavorites = false;

            const $favorite_ul = $("<ul>").appendTo(this._addToolContainer($ul, "Favoriten"));

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
                if ((!container.tools || !container.options) && !container.hasCustomTools)
                    if (!container.tools && !container.options)
                        continue;
                if (!container.tools && container.options) {
                    try {
                        const tool = elem._map.getTool(webgis.tools.getToolId(container.name));
                        if (tool)
                            tool.options = container.options;
                    }
                    catch (e) { }
                    continue;
                }

                const $tools_ul = $("<ul>").css('display', 'none').appendTo(this._addToolContainer($ul, container.name));

                for (const t in container.tools || []) {
                    const containerToolId = container.tools[t];
                    for (let i = 0, to = tools.length; i < to; i++) {
                        const tool = tools[i];
                        if (tool.id === containerToolId) {
                            tool.uiElement = elem;
                            tool.map = elem._map;
                            tool.onRelease = this.constructor.onToolRelease;

                            if (tool.favorite_priority) {
                                this._addToolButton($favorite_ul, tool, true);
                                hasFavorites = true;
                            }
                            this._addToolButton($tools_ul, tool);
                        }
                    }
                }

                // CustomTools
                if (container.hasCustomTools && webgis.custom.tools) {
                    const customTools = $.grep(webgis.custom.tools.toArray(), function (t) { return t.container === container.name });
                    for (const t in customTools) {
                        const customTool = customTools[t];

                        if (elem._map.getTool(customTool.id) == null)
                            elem._map.addTool(customTool);

                        this._addToolButton($tools_ul, customTool);
                    }
                }
            }

            if (hasFavorites === false) {
                $favorite_ul.parent().remove();
            }

            $ul.find('.webgis-toolbox-container-title:first').trigger('click');
        },
        _addToolContainer: function ($parent, containerName) {
            const $ = this.$;
            const $container_li = $("<li class='webgis-toolbox-tool-item-group webgis-toolbox-collapsable'>")
                .attr('data-name', containerName)
                .appendTo($parent);

            $("<div class='webgis-toolbox-container-title'><span style='position:absolute' class='webgis-api-icon webgis-api-icon-triangle-1-s'></span><span style='margin-left:14px'>&nbsp;" + containerName + "</span></div>")
                .appendTo($container_li)
                .on('click.webgis_toolbox', function () {
                    const $this = $(this);

                    if ($this.children('.webgis-api-icon-triangle-1-e').length > 0) {
                        $this.children('.webgis-api-icon-triangle-1-e')
                            .removeClass('webgis-api-icon-triangle-1-e')
                            .addClass('webgis-api-icon-triangle-1-s');
                        $this.parent()
                            .removeClass('selected')
                            .children('ul').slideUp();
                    }
                    else {
                        $this.parent()
                            .parent()
                            .find('.webgis-toolbox-container-title .webgis-api-icon-triangle-1-e')
                            .each(function (i, e) {
                                $(e).removeClass('webgis-api-icon-triangle-1-e').addClass('webgis-api-icon-triangle-1-s');
                                $(e).parent().parent().children('ul').slideUp();
                            });

                        $this.parent()
                            .parent()
                            .find('.selected')
                            .removeClass('selected');

                        $this.children('.webgis-api-icon-triangle-1-s').removeClass('webgis-api-icon-triangle-1-s').addClass('webgis-api-icon-triangle-1-e');
                        $this.parent()
                            .addClass('selected')
                            .children('ul').slideDown();
                    }
                });

            return $container_li;
        },
        _addToolButton: function ($tools_ul, tool, useFavoritePriority) {
            const $ = this.$;

            if ($.inArray(tool.id, ["webgis.tools.addtoselection", "webgis.tools.removefromselection"]) >= 0) {   // do not show this in toolbar
                return;
            }

            const $li = $("<li class='webgis-toolbox-tool-item'>")
                .attr('data-toolid', tool.id);

            if (useFavoritePriority && tool.favorite_priority) {
                $li.attr('data-priority', tool.favorite_priority);
                let $insertBefore = null;
                $tools_ul.children().each(function (i, e) {
                    if ($insertBefore == null && $(e).attr('data-priority') && parseInt($(e).attr('data-priority')) > tool.favorite_priority)
                        $insertBefore = $(e);
                });
                if ($insertBefore !== null)
                    $li.insertBefore($insertBefore);
                else
                    $li.appendTo($tools_ul);
            } else {
                $li.appendTo($tools_ul);
            }

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
            $li.get(0).tool = tool;
            if (tool.dependencies && tool.dependencies.length > 0) {
                $li.addClass('webgis-dependencies');
                for (let d = 0; d < tool.dependencies.length; d++) {
                    $li.addClass('webgis-dependency-' + tool.dependencies[d]);
                }
            }

            const map = this.options.map;
            $li.on('click.webgis_toolbox', function () {
                webgis.tools.onButtonClick(map, this.tool);
            });

            return $li;
        }
    },
    staticMethods: {
        defaultContainers: function () {
            return [
                {
                    name: "Navigation",
                    tools: [webgis.tools.navigation.fullExtent,
                    webgis.tools.navigation.currentPos,
                    webgis.tools.navigation.zoomBack,
                    webgis.tools.navigation.refreshMap,
                    webgis.tools.navigation.saveMap,
                    webgis.tools.navigation.loadMap]
                },
                {
                    name: "Abfragen",
                    tools: [webgis.tools.info.identify,
                    webgis.tools.info.coordinates,
                    webgis.tools.info.queryResults]
                },
                {
                    name: "Werkeuge",
                    tools: [webgis.tools.advanced.measureDistance,
                    webgis.tools.advanced.measureArea,
                    webgis.tools.advanced.profile,
                    webgis.tools.advanced.mapmarkup,
                    webgis.tools.advanced.edit]
                }
            ];
        },
        // tool.onRelease: back to the parent tool or rebuild the toolbox (tool.uiElement)
        onToolRelease: function (t) {
            if (t.parentTool) {
                webgis.tools.onButtonClick(t.map, t.parentTool);
            }
            else {
                if (t.type == 'clientbutton' || t.type == 'serverbutton') {
                    // do nothing
                } else {
                    t.map.setActiveTool(null);
                    (webgis.$ || jQuery)(t.uiElement).webgis_uibuilder('empty', { map: t.map }).webgis_toolbox({ map: t.map });
                }
            }
        },
        containsTool: function (containers, tool) {
            const $ = webgis.$ || jQuery;

            for (const c in containers) {
                const container = containers[c];
                if (container.tools) {
                    for (const t in container.tools) {
                        if (container.tools[t] === tool.id) {
                            return true;
                        }
                    }
                }
            }

            if (webgis.usability.appendMapTools && $.inArray(tool.id, webgis.usability.appendMapTools) >= 0) {
                const toolContainer = $.grep(containers, function (g) {
                    return g.name === tool.container;
                });
                if (toolContainer.length === 1) {
                    toolContainer[0].tools = toolContainer[0].tools || [];
                    toolContainer[0].tools.push(tool.id);

                    return true;
                }
                else if (toolContainer.length === 0) {
                    containers.push({
                        name: tool.container,
                        tools: [tool.id]
                    });

                    return true;
                }
            }

            return false;
        }
    }
});
