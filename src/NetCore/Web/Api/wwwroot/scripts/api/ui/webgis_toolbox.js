(function ($) {
    $.fn.webgis_toolbox = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_toolbox');
        }
    };
    var defaults = {
        map: null,
        fireEvent:false,
        containers: [
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
                webgis.tools.advanced.redlining,
                webgis.tools.advanced.edit]
            }
        ]
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        } /*,  // use init: {usertools:[...]} instead!!
        addtool: function (options) {
            if (!options.tool)
                return;

            if (!options.tool.uiElement)
                options.tool.uiElement = this;
            alert($(this).html());
           
            alert($(this).html());
            $('<button>' + options.tool.name + "</button>").
                click(function () {
                    webgis.tools.onButtonClick(options.map, options.tool, options.tool.ui);
                }).appendTo($(this));
        }*/
    };

    var initUI = function (elem, options) {
        //console.log('webgis_toolbox.initUI', options);
        var $elem = $(elem);
        if ($elem.hasClass('no-toolbox'))
            return;

        elem._map = options.map;
        elem._containers = elem._containers && elem._containers.length > 0 ? elem._containers : options.containers;
        $elem.addClass('webgis-toolbox-holder'); //.css('display', 'none');

        if (!elem.tools) {
            $elem.html("<img src='" + webgis.css.imgResource('loader1.gif', 'hourglass') + "' />...");
            webgis.toolInfos(options.tools, null, function (tools) {
                elem.tools = tools;
                if (options.usertools) {
                    var userContainer = { name: options.usertools_containername || "Benutzerdef. Werkzeuge", tools: [] };
                    for (var t in options.usertools) {
                        userContainer.tools.push(options.usertools[t].id);
                        elem.tools.push(options.usertools[t]);
                    }
                    elem._containers.push(userContainer);
                }
                // find Container Doubles
                for (var i = 0; i < elem._containers.length + 1; i++) {
                    for (var j = i + 1; j < elem._containers.length; j++) {
                        if (elem._containers[i].name === elem._containers[j].name) {
                            for (var t in elem._containers[j].tools) {
                                if ($.inArray(elem._containers[j].tools[t], elem._containers[i].tools, 0) < 0)
                                    elem._containers[i].tools.push(elem._containers[j].tools[t]);
                            }
                            elem._containers[j].tools = null;
                        }
                    }
                }

                for (var t in tools) {
                    if (containsTool(elem._containers, tools[t]))
                        options.map.addTool(tools[t]);
                }

                elem._containers = options.map._mapToolsToServerContainers(elem._containers);

                buildList(elem);
                if ($elem.hasClass('no-toolbox'))
                    $elem.empty();

                if (options.fireEvent) {
                    elem._map.events.fire('toolboxloaded', elem._map);
                }

                $.fn.webgis_toolbox._toolsLoaded = true;
            });
        }
        else {
            buildList(elem);
        }
        if (options.map) {
            webgis.delayed(function () {
                options.map.ui.refreshUIElements();
            }, 100);

            options.map.events.on('onchangeactivetool', function (e, m) {
                var tool = m.getActiveTool();
                $('.webgis-toolbox-tool-item.selected').each(function () {
                    if (!$(this).hasClass('webgis-toggle-button'))
                        $(this).removeClass('selected');
                });
                if (tool) {
                    var $item = $(".webgis-toolbox-tool-item[data-toolid='" + tool.id + "']");
                    $item.addClass('selected');

                    // Wenn toolclick getriggert wurde, hier den richtigen Tab sichtbar schalten.
                    var $holder = $('.webgis-toolbar-holder');
                    if ($holder.length === 1 && $.fn.webgis_toolbar) {
                        $holder.webgis_toolbar('selectCurrentToolTab');
                    }
                }
            });
        }
    };

    var buildList = function (elem) {
        $elem = $(elem);
        $elem.empty();
        var tools = elem.tools;

        var containers = elem._containers;
        var $ul = $("<ul id='webgis-toolbox-list'></ul>").appendTo($elem), hasFavorites = false;

        var $favorite_ul = $("<ul>").appendTo(addToolContainer($ul, "Favoriten"));

        if (webgis.custom.tools) {
            var customTools = webgis.custom.tools.toArray();
            for (var t in customTools) {
                var customTool = customTools[t];
                var containerWithName = $.grep(containers, function (c) { return c.name == customTool.container });
                var container = containerWithName.length === 0 ? null : containerWithName[0];

                if (container === null) {
                    containers.push(container = {
                        name: customTool.container,
                    });
                }

                container.hasCustomTools = true;
            }
        }

        //console.log('tool containers', containers);

        for (var c in containers) {
            var container = containers[c];
            if ((!container.tools || !container.options) && !container.hasCustomTools)
            if (!container.tools && !container.options)
                continue;
            if (!container.tools && container.options) {
                try {
                    var tool = elem._map.getTool(webgis.tools.getToolId(container.name));
                    if (tool)
                        tool.options = container.options;
                }
                catch (e) { }
                continue;
            }

            var $tools_ul = $("<ul>").css('display','none').appendTo(addToolContainer($ul, container.name));

            for (var t in container.tools || []) {
                var containerToolId = container.tools[t];
                for (var i = 0, to = tools.length; i < to; i++) {
                    var tool = tools[i];
                    if (tool.id === containerToolId) {
                        tool.uiElement = elem;
                        tool.map = elem._map;
                        tool.onRelease = function (t) {
                            console.log('onRelease', t);
                            if (t.parentTool) {
                                webgis.tools.onButtonClick(t.map, t.parentTool);
                            }
                            else {
                                if (t.type == 'clientbutton' || t.type == 'serverbutton') {
                                    // do nothing
                                } else {
                                    t.map.setActiveTool(null);
                                    $(t.uiElement).webgis_uibuilder('empty', { map: t.map }).webgis_toolbox({ map: t.map });
                                }
                            }
                        };

                        if (tool.favorite_priority) {
                            addToolButton($favorite_ul, tool, true);
                            hasFavorites = true;
                        }
                        addToolButton($tools_ul, tool);
                    }
                    // passiert schon im oben beim abfragen der API Tools 
                    //elem._map.addTool(tool);
                }
            }

            // CustomTools
            if (container.hasCustomTools && webgis.custom.tools) {
                var customTools = $.grep(webgis.custom.tools.toArray(), function (t) { return t.container === container.name });
                for (var t in customTools) {
                    var customTool = customTools[t];

                    if (elem._map.getTool(customTool.id) == null)
                        elem._map.addTool(customTool);

                    var $li = addToolButton($tools_ul, customTool);
                }
            }
        }

        if (hasFavorites === false) {
            $favorite_ul.parent().remove();
        } else {
            //$ul.find('.webgis-toolbox-container-title').each(function (i, e) {
            //    if ($(e).parent().attr('data-name') !== 'Favoriten')
            //        $(e).trigger('click');
            //});
        }

        $ul.find('.webgis-toolbox-container-title:first').trigger('click');
    };

    var addToolContainer = function ($parent, containerName) {
        var $container_li = $("<li class='webgis-toolbox-tool-item-group webgis-toolbox-collapsable'>")
            .attr('data-name', containerName)
            .appendTo($parent);
        var $container_li_content = $("<div class='webgis-toolbox-container-title'><span style='position:absolute' class='webgis-api-icon webgis-api-icon-triangle-1-s'></span><span style='margin-left:14px'>&nbsp;" + containerName + "</span></div>").appendTo($container_li)
            .click(function () {
                var $this = $(this);
                var me = this;

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
        //$("<img src='" + webgis.css.imgResource('tool-details.png', 'tools') + "' style='width:20px;position:absolute;right:20px;opacity:.5' />").appendTo($container_li_content)
        //    .click(function (event) {
        //        var $group = $(this).closest('.webgis-toolbox-tool-item-group');
        //        $(this).attr('src', $group.hasClass('webgis-toolbox-tool-item-group-details') ? webgis.css.imgResource('tool-details.png', 'tools') : webgis.css.imgResource('tool-symbols.png', 'tools'));
        //        $group.toggleClass('webgis-toolbox-tool-item-group-details');
        //        if ($group.find('.webgis-api-icon-triangle-1-e').length > 0)
        //            return false;
        //    });
        return $container_li;
    };

    var addToolButton = function ($tools_ul, tool, useFavoritePriority) {
        if ($.inArray(tool.id, ["webgis.tools.addtoselection", "webgis.tools.removefromselection"]) >= 0) {   // do not show this in toolbar
            return;
        }

        $li = $("<li class='webgis-toolbox-tool-item'>")
            .attr('data-toolid', tool.id);

        if (useFavoritePriority && tool.favorite_priority) {
            $li.attr('data-priority', tool.favorite_priority);
            var $insertBefore = null;
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

        var img = '';
        if (tool.image) {
            img = webgis.css.imgResource(tool.image, 'tools');
        }
        else {
            img = tool.hasui == true ? webgis.css.imgResource('enter-26.png','ui') : webgis.css.imgResource(tool.image, 'tools');
        }
        $("<span class='webgis-toolbox-tool-item-span'><img src='" + img + "' /><span class='webgis-toolbox-tool-item-label'>&nbsp;" + tool.name + "</span></span>").appendTo($li)
            .attr('alt', tool.name + ": " + tool.tooltip).attr('title', tool.name + ": " + tool.tooltip);
        $li.get(0).tool = tool;
        if (tool.dependencies && tool.dependencies.length > 0) {
            $li.addClass('webgis-dependencies');
            for (var d = 0; d < tool.dependencies.length; d++) {
                $li.addClass('webgis-dependency-' + tool.dependencies[d]);
            }
        }

        $li.click(function () {
            var $holder = $(this).closest('.webgis-toolbox-holder');
            var map = $holder.get(0)._map;
            var tool = this.tool;
            webgis.tools.onButtonClick(map, tool);
        })

        return $li;
    };

    var containsTool = function (containers, tool) {
        for (var c in containers) {
            var container = containers[c];
            if (container.tools) {
                for (var t in container.tools) {
                    if (container.tools[t] === tool.id) {
                        return true;
                    }
                }
            }
        }

        if (webgis.usability.appendMapTools && $.inArray(tool.id, webgis.usability.appendMapTools) >= 0) {
            var toolContainer = $.grep(containers, function (g) {
                return g.name === tool.container;
            });
            //console.log(toolContainer);
            //console.log(tool);
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
    };
})(webgis.$ || jQuery);
