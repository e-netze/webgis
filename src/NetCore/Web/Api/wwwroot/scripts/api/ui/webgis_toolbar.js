(function ($) {
    $.fn.webgis_toolbar = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_toolbar');
        }
    };
    var defaults = {
        map: null, containers: null
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        intialize: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initializeToolbar(this, options);
            });
        },
        selectCurrentToolTab: function (options) {
            var $this = $(this);
            var $selected = $this.find('.webgis-toolbox-tool-item.selected');
            if ($selected.length === 1) {
                var $tabContent = $selected.closest('.webgis-toolbar-tabcontent[data-index]');
                if ($tabContent.length === 1) {
                    var index = $tabContent.attr('data-index');
                    var $tab = $this.find(".webgis-toolbar-tab[data-index='" + index + "']");
                    if (!$tab.hasClass('selected'))
                        $tab.trigger('click');
                }
            }
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem).addClass('webgis-toolbar-holder').data('map', options.map);
        if (!options.containers)
            return;

        var $tab_ul = $("<ul>").addClass('webgis-toolbar-tabs').appendTo($elem), index = 0, hasFavorites = false;

        $("<li>&nbsp;</li>").addClass('webgis-toolbar-tab favorites').attr('data-index', index).appendTo($tab_ul);
        var $favContent = $("<ul>")
            .addClass('webgis-toolbar-tabcontent favorites')
            .attr('data-index', index)
            .css('display', 'none')
            .appendTo($elem);
        index++;

        var containers = options.map._mapToolsToServerContainers(options.containers) || [];

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
            if ((!container.tools || container.tools.length === 0) && !container.hasCustomTools)
                continue;

            var $tab = $("<li>" + container.name + "</li>")
                .addClass('webgis-toolbar-tab')
                .attr('data-index', index)
                .appendTo($tab_ul);

            var $tabContent = $("<ul>")
                .addClass('webgis-toolbar-tabcontent')
                .attr('data-index', index)
                .css('display', 'none')
                .appendTo($elem);

            for (var t in container.tools || []) {
                var tool = options.map._tools[container.tools[t]];
                if (!tool)
                    continue;

                if (tool.favorite_priority) {
                    addToolButton($favContent, options, tool, true);
                    hasFavorites = true;
                }
                var $li = addToolButton($tabContent, options, tool);
            }

            // CustomTools
            if (container.hasCustomTools && webgis.custom.tools) {
                var customTools = $.grep(webgis.custom.tools.toArray(), function (t) { return t.container === container.name });
                for (var t in customTools) {
                    var customTool = customTools[t];
                    if (options.map.getTool(customTool.id) == null)
                        options.map.addTool(customTool);

                    var $li = addToolButton($tabContent, options, customTool);
                }
            }
            index++;
        }

        if (webgis.help.url) {
            var $helpButton = $("<li>?</li>").addClass('webgis-toolbar-tab help').appendTo($tab_ul);

            if (options.map) {
                options.map.events.on('onchangeactivetool', function (e, m) {
                    var tool = m.getActiveTool();
                    if (m.isDefaultTool(tool)) {
                        $helpButton.css('background-image', '');
                        if (tool.help_urlpath_defaulttool) {
                            $helpButton.addClass('help-exists')
                        } else {
                            $helpButton.removeClass('help-exists')
                        }
                    } else {
                        if (tool && tool.help_urlpath) {
                            $helpButton.addClass('help-exists')
                                .css('background-image', 'url(' + webgis.css.imgResource(tool.image,'tools') + ')');
                        } else {
                            $helpButton.removeClass('help-exists')
                                .css('background-image', '');
                        }
                    }
                });
            }
        }

        if (hasFavorites === false) {
            $elem.find('.webgis-toolbar-tab.favorites').remove();
            $elem.find('.webgis-toolbar-tabcontent.favorites').remove();
        }

        $tab_ul.children('.webgis-toolbar-tab')
            .click(function () {
                var $tab = $(this),
                    $tabs = $tab.closest('.webgis-toolbar-tabs'),
                    $holder = $tabs.closest('.webgis-toolbar-holder');

                if ($tab.hasClass('help')) {
                    var map = $holder.data('map');
                    var activeTool = map.getActiveTool();
                    webgis.showHelp(activeTool ?
                        (map.isDefaultTool(activeTool) && activeTool.help_urlpath_defaulttool ? activeTool.help_urlpath_defaulttool : activeTool.help_urlpath) :
                        null,
                        map);
                } else {
                    $tabs.find('.webgis-toolbar-tab').removeClass('selected');
                    $holder.find('.webgis-toolbar-tabcontent').css('display', 'none');
                    $tab.addClass('selected');
                    $holder.find(".webgis-toolbar-tabcontent[data-index='" + $tab.attr('data-index') + "']").css('display', 'block');
                }
            });

        $tab_ul.find('.webgis-toolbar-tab').first().trigger('click');
        if (options.map) {
            //
            //  Wird schon in bei webgis_toolbar eingebunden und die wird immer im Hintergrund geladen, auch wenn das Desktoplayout geöffnet wird 
            //

            //options.map.events.on('onchangeactivetool', function (e, m) {
            //    var tool = m.getActiveTool();
            //    $('.webgis-toolbox-tool-item.selected').each(function () {
            //        if (!$(this).hasClass('webgis-toggle-button'))
            //            $(this).removeClass('selected');
            //    });
            //    var $item = $(".webgis-toolbox-tool-item[data-toolid='" + tool.id + "']");
            //    $item.addClass('selected');
            //});
        }

        initializeToolbar(elem, options);
    };

    var addToolButton = function ($tabContent, options, tool, useFavoritePriority) {
        if ($.inArray(tool.id, ["webgis.tools.addtoselection", "webgis.tools.removefromselection"]) >= 0) {   // do not show this in toolbar
            return;
        }

        var $li = $("<li class='webgis-toolbox-tool-item'>");

        var img = '';
        if (tool.image) {
            img = webgis.css.imgResource(tool.image, 'tools');
        }
        else {
            img = tool.hasui == true ? webgis.css.imgResource('enter-26.png','ui') : webgis.css.imgResource(tool.image, 'tools');
        }
        var $btn = $("<span class='webgis-toolbox-tool-item-span'><img src='" + img + "' /><span class='webgis-toolbox-tool-item-label'>&nbsp;" + tool.name + "</span></span>")
            .appendTo($li)
            .attr('alt', tool.name + ": " + tool.tooltip)
            .attr('title', tool.name + ": " + tool.tooltip);

        if (useFavoritePriority === true && tool.favorite_priority) {
            $li.attr('data-priority', tool.favorite_priority);
            var $insertBefore = null;
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

        $li.click(function () {
            var $holder = $(this).closest('.webgis-toolbar-holder');
            var map = $holder.data('map');
            var tool = $(this).data('tool');
            webgis.tools.onButtonClick(map, tool);
        });

        $li.data('tool', tool).attr('data-toolid', tool.id);

        //if (tool.dependencies && tool.dependencies.length > 0) {
        //    $li.addClass('webgis-dependencies');
        //    for (var d = 0; d < tool.dependencies.length; d++) {
        //        $li.addClass('webgis-dependency-' + tool.dependencies[d]);
        //    }
        //}

        return $li;
    };

    var initializeToolbar = function (elem, options) {
        if (options.map) {
            var $elem = $(elem);
            var map = options.map;

            $elem.find('.webgis-toolbox-tool-item[data-toolid]').each(function () {
                var $button = $(this);
                var tool = map.getTool($button.attr('data-toolid'));
                if (tool && tool.dependencies && tool.dependencies.length > 0) {
                    $button.addClass('webgis-dependencies');
                    for (var d = 0; d < tool.dependencies.length; d++) {
                        $button.addClass('webgis-dependency-' + tool.dependencies[d]);
                    }
                }
            });

            map.ui.refreshUIElements();
        }
    };
})(webgis.$ || jQuery);
