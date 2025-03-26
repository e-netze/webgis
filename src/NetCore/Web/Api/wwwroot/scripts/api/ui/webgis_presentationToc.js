(function ($) {
    "use strict";
    $.fn.webgis_presentationToc = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_presentationToc');
        }
    };
    var defaults = {
        items: null,
        map: null,
        gdi_button: false,
        basemap_rows: 1
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        show_first: function (options) {
            if ($(this).find('.webgis-presentation_toc-collapsable.webgis-expanded').length == 0) {
                $(this).find('.webgis-presentation_toc-collapsable:first').find('.webgis-presentation_toc-title-text').trigger('click');
            }
        },

        show_services_legend: function (options) {
            if (options.map) {
                var title = webgis.globals && webgis.globals.portal ? webgis.globals.portal.mapName : null;
                $.presentationToc.showLegend(
                    options.sender,
                    options.map,
                    options.map.services,
                    title || "Darstellung, ©, ...");
            }
        },
        get_expanded_names: function (options) {
            var collapsed = [];

            var findCollapsedGroupsRecursive = function ($parent, prefix) {
                $parent.children('.webgis-presentation_toc-item-group.webgis-presentation_toc-collapsable')
                    .each(function (g, group) {
                        var $group = $(group);
                        var $ul = $group.children('ul');
                        if ($ul.length === 1) {
                            if ($ul.css('display') !== 'none') {
                                collapsed.push(prefix + $group.attr('data-ui-groupname'))
                            }
                            findCollapsedGroupsRecursive($ul, prefix + $group.attr('data-ui-groupname') + '|');
                        }
                    });
            }

            $(this).find('.webgis-presentation_toc-title.webgis-presentation_toc-collapsable') // containers
                .each(function (c, container) {
                    var $container = $(container);
                    if ($container.hasClass('webgis-expanded')) {
                        collapsed.push($container.attr("data-container"));
                    }
                    findCollapsedGroupsRecursive($container.children('.webgis-presentation_toc-content').children('ul'), $container.attr("data-container") + '|');
                });
            
            return collapsed;
        },
        expand: function (options) {
            var $this = $(this);

            if (options.exclusive === true) {
                _collapseAll(this, options);
            }

            webgis.delayed(function () {
                var getGroupRecurive = function ($parent, names) {
                    try {
                        var $group = $parent.children(".webgis-presentation_toc-item-group.webgis-presentation_toc-collapsable[data-ui-groupname='" + names[0] + "']")
                        if (names.length === 1)
                            return $group;

                        return getGroupRecurive($group.children('ul'), names.slice(1));
                    } catch (e) {
                        return null;
                    }
                }

                if (options.names) {
                    var containerNames = $.grep(options.names, function (n) { return n.split('|').length === 1 });
                    var groupNames = $.grep(options.names, function (n) { return n.split('|').length > 1 });

                    // first, expand groups
                    for (var n in groupNames) {
                        var nameParts = groupNames[n].split('|');

                        var $container = $this.find('.webgis-presentation_toc-title.webgis-presentation_toc-collapsable[data-container="' + webgis.encodeXPathString(nameParts[0]) + '"]');

                        var $group = getGroupRecurive(
                            $container.children('.webgis-presentation_toc-content').children('ul'),
                            nameParts.slice(1));

                        if ($group && $group.length === 1) {
                            var $ul = $group.children('ul');
                            if ($ul.length === 1) {
                                if ($ul.css('display') === 'none') {
                                    $group.children('.webgis-text-ellipsis-pro').trigger('click');
                                }
                            }
                        }
                    }

                    // then, expand contianers
                    for (var n in containerNames) {
                        var $container = $this.find('.webgis-presentation_toc-title.webgis-presentation_toc-collapsable[data-container="' + webgis.encodeXPathString(containerNames[n]) + '"]');
                        if (!$container.hasClass('webgis-expanded')) {
                            $container.children('.webgis-presentation_toc-title-text').trigger('click');
                        }
                    }
                }
            }, options.exclusive === true ? 500 : 1);

            return $this;
        },
        collapse_all: function (options) {
            _collapseAll(this, options);
        },
        createCustomContainer: function (options) {
            return addCustomContainer($(this), options);
        }
    };
    $.presentationToc = {
        sequence: 0,
        process: function (sender, ids) {
            var $elem = sender ? $(sender).closest('.webgis-presentation_toc-holder') : $('.webgis-presentation_toc-holder');
            if ($elem.length === 0)
                return;

            var $search = $elem.find('.webgis-presentation_toc-search');
            if ($search.length > 0) {
                $search.webgis_contentsearch('reset');
            }

            var map = $elem.get(0)._map;
            if (map == null)
                return;
            for (i = 0; i < ids.length; i++) {
                var p = $.presentationToc.findItem(map, ids[i]);
                if (p && p.presentation && p.item) {
                    var layerids = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                    if (p.item.style === 'button') {
                        p.presentation.service.setServiceVisibility(null); // alle unsichtbar schalten!!! und unten die einzelnen Layer einschalten. In Gruppen können mehrer Darstellungsvarianten aus dem gleichen Dienst vorkommen. Darum zuerst Sichtbarkeit aus und dann die einzellnen wieder einschalten.
                    }
                }
            }
            for (var i = 0; i < ids.length; i++) {
                var p = $.presentationToc.findItem(map, ids[i]);
                if (p && p.presentation && p.item) {
                    var layerids = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                    if (p.item.style === 'button') {
                        //p.presentation.service.setServiceVisibility(layerids);
                        p.presentation.service.setLayerVisibility(layerids, true); // Hier auch nur die einzelnen wieder einschalten, weil das Wegschalten schon oben erfolgt ist (siehe oben)
                    }
                    else if (p.item.style === 'checkbox') {
                        var status = p.presentation.service.checkLayerVisibility(layerids);
                        p.presentation.service.setLayerVisibilityDelayed(layerids, status != 1);
                    }
                    else if (p.item.style === 'optionbox') {
                        var status = p.presentation.service.checkLayerVisibility(layerids);
                        p.presentation.service.setLayerVisibilityDelayed(layerids, status != 1);

                        if (p.item.groupstyle === 'dropdown') {  // Wenn optionbox in einer Dropdown Gruppe ist -> alle anderen OptionBox in der Gruppe ausschalten
                            for (var s in map.services) {
                                var service = map.services[s];
                                if (!service || !service.presentations)
                                    continue;

                              
                                for (var sp in service.presentations) {
                                    var presentation = service.presentations[sp];
                                    if (!presentation) continue;

                                    // don't uncheck the current presentation
                                    if (presentation.id === p.presentation.id) {                        // if presentation id is ident
                                        if (presentation.service && p.presentation.service) {           //    if service is implemented with presenteion
                                            if (presentation.service.id === p.presentation.service.id)  //       than continue only if its the same service
                                                continue;
                                        } else { continue; }                                            //    otherwise continue               
                                    } 

                                    // uncheck if its an optionbox in the the same group
                                    for (var it in presentation.items) {
                                        var item = presentation.items[it];

                                        if (item.style === 'optionbox' &&
                                            item.groupstyle === 'dropdown' &&
                                            item.container === p.item.container &&
                                            item.name === p.item.name &&
                                            ((!item.ui_groupname && !p.item.ui_groupname) || (item.ui_groupname === p.item.ui_groupname))) {
                                            var optionLayerIds = service.getLayerIdsFromNames(presentation.layers);
                                            service.setLayerVisibilityDelayed(optionLayerIds, false);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            $.presentationToc.checkItemVisibility();
            $.presentationToc.checkContainerVisibility();
        },
        findItem: function (map, id) {
            for (var serviceId in map.services) {
                
                var service = map.services[serviceId];
                if (!service || !service.presentations)
                    continue;

                for (var p = 0; p < service.presentations.length; p++) {
                    var presentation = service.presentations[p];
                    if (!presentation.items)
                        continue;
                    for (var i = 0; i < presentation.items.length; i++) {
                        var item = presentation.items[i];
                        if (item.id == id) {
                            return { presentation: presentation, item: item };
                        }
                    }
                }
            }
            return null;
        },
        checkItemVisibility: function (event, service, parent) {
            parent = parent || $('.webgis-presentation_toc-holder');
            var $elem = $(parent);
            if ($elem.length == 0)
                return;

            var map = $elem.get(0)._map;
            if (map == null)
                return;

            var serviceIds = map.serviceIds();
            // Find all items and check visibility
            $elem.find('.webgis-presentation_toc-item').each(function (i, e) {
                var $e = $(e);

                if ($e.hasClass('webgis-presentation_toc-basemap-item') ||
                    $e.hasClass('webgis-presentation_toc-dyncontent-item') ||
                    $e.hasClass('webgis-presentation_tol-custom-item')) {
                    $e.removeClass('webgis-display-none').addClass('webgis-display-block');
                    return;
                }

                var display = 'none';
                if ($e.hasClass('link-button')) {
                    if ($e.prev().hasClass('webgis-display-block')) {
                        display = 'block';
                    }
                } else {
                    if (e.ownerIds) {
                        for (var i = 0; i < e.ownerIds.length; i++) {
                            if ($.inArray(e.ownerIds[i], serviceIds) >= 0) {
                                //alert(e.ownerIds);
                                display = 'block';
                            }
                        }
                    }
                }
                //$(e).css('display', display);
                if (display === 'block') {
                    $e.removeClass('webgis-display-none').addClass('webgis-display-block');
                } else {
                    $e.removeClass('webgis-display-block').addClass('webgis-display-none');
                }
            });

            // parse all collapsables and check Visibility
            $elem.find('.webgis-presentation_toc-collapsable').each(function (i, c) {
                if ($(c).hasClass('webgis-presentation_toc-custom-container'))
                    return;

                var display = 'none';
                $(c).find('.webgis-presentation_toc-item').each(function (j, e) {
                    //if ($(e).css('display') != 'none') {
                    if ($(e).hasClass('webgis-display-block')) {
                        display = 'block';
                    }
                });
                //$(c).css('display', display);

                if (display === 'block') {
                    $(c).removeClass('webgis-display-none').addClass('webgis-display-block');
                    $(c).find('.webgis-search-content').addClass('active');
                } else {
                    $(c).removeClass('webgis-display-block').addClass('webgis-display-none');
                    $(c).find('.webgis-search-content').removeClass('active');
                }
            });

            // parse all checkable items
            $elem.find('.webgis-presentation_toc-checkable').each(function (i, e) {
                if (e.presIds) {
                    for (var i = 0; i < e.presIds.length; i++) {
                        var p = $.presentationToc.findItem(map, e.presIds[i]);
                        if (p && p.presentation) {
                            var layerIds = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                            var status = p.presentation.service.checkLayerVisibility(layerIds);
                            var checktype = $(e).hasClass('optionbox') ? 'option' : 'check';
                            var img = webgis.css.imgResource(checktype + '0.png', 'toc');
                            switch (status) {
                                case 1:
                                    img = webgis.css.imgResource(checktype + '1.png', 'toc');
                                    break;
                                case -1:
                                    img = webgis.css.imgResource(checktype + '01.png', 'toc');
                                    break;
                            }
                            $(e).find('.webgis-presentation_toc-checkable-icon').attr('src', img);
                        }
                    }
                }
            });
        },
        checkItemScaleVisibility: function (event, map, parent) {
            parent = parent || $('.webgis-presentation_toc-holder');
            var $elem = $(parent);
            if ($elem.length === 0)
                return;

            var scale = /*map.scale()*/ map.crsScale();
            $elem.find('.webgis-scaledependent')
                .removeClass('webgis-outofscale').removeClass('webgis-inscale')
                .each(function (i, e) {

                    var $e = $(e);
                    
                    //if ($e.hasClass('webgis-presentation_toc-dyncontent-item')) {
                    //    // todo: query scale?
                    //    $e.removeClass('webgis-outofscale');
                    //} else {
                    if (e.presIds && e.presIds.length > 0) {
                        for (var i = 0, to = e.presIds.length; i < to; i++) {
                            var p = $.presentationToc.findItem(map, e.presIds[i]);
                            if (p && p.presentation) {

                                if ($e.hasClass('webgis-inscale')) {
                                    //console.log('already in scale', e);
                                    return; // already tested and in scale
                                }

                                var layerids = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                                var inScale = layerids.length == 0; // leere Darstellungsvarianten immer anzeigen -> sind meistens die "Dings Ausschalten" Buttons
                                for (var l in layerids) {
                                    inScale = inScale || p.presentation.service.layerInScale(layerids[l], scale);
                                    //console.log(layerids[l], p.presentation.service.layerInScale(layerids[l], scale), inScale);
                                }
                                if (inScale) {
                                    $e.removeClass('webgis-outofscale').addClass('webgis-inscale');
                                } else {
                                    $e.addClass('webgis-outofscale').removeClass('webgis-inscale');
                                }
                            }
                        }
                    }
                //}
            });

            // Set out of scale for groups
            $elem.find('.webgis-presentation_toc-item-group.webgis-presentation_toc-collapsable')
                .each(function (i, e) {
                    var $group = $(e);
                    var countChildren = $group.find('.webgis-presentation_toc-item').length;
                    var countOutOfScaleChildren = $group.find('.webgis-presentation_toc-item.webgis-outofscale').length;

                    //console.log($group.text(), countChildren, countOutOfScaleChildren);

                    if (countChildren > 0 && countChildren == countOutOfScaleChildren) {
                        $group.addClass('webgis-outofscale');
                    } else {
                        $group.removeClass('webgis-outofscale');
                    }
                });
        },
        checkContainerVisibility: function (parent) {
            var $parent = $(parent || $('.webgis-presentation_toc-holder'));
            if ($parent.length === 0 || $parent.get(0)._map == null)
                return;

            var map = $parent.get(0)._map;

            $parent.find('.webgis-presentation_toc-collapsable').each(function (i, collapsable) {
                var $collapsable = $(collapsable), foundVisibleLayers = false;

                if ($collapsable.hasClass('webgis-basemap-toc-container')) {
                    foundVisibleLayers = map.currentBasemapServiceId() || map.currentBasemapOverlayServiceId() ? true : false;
                } else if ($collapsable.hasClass('webgis-dynamiccontent-toc-container')) {
                    foundVisibleLayers = map.hasCurrentDynamicContent() && map.isCurrentDynamicContentFromTocTheme() === false;
                } else if ($collapsable.hasClass('webgis-presentation_toc-custom-container')) {
                    foundVisibleLayers = true;
                } else {
                    var containsOnlyCheckboxes = true, checkedItems = 0, uncheckedItems = 0;

                    $collapsable.find('.webgis-presentation_toc-item').each(function (j, e) {
                        var $e = $(e);

                        if (!$e.hasClass('checkbox') && !$e.hasClass('link-button')) { // link buttons are not realy an items
                            containsOnlyCheckboxes = false;
                        }

                        if (map.hasCurrentDynamicContent() && $e.hasClass('webgis-presentation_toc-dyncontent-item')) {
                            var dynamicContent = $e.data('dynamic-content');
                            if (dynamicContent && map.getCurrentDynamicContentId() == dynamicContent.id) {
                                foundVisibleLayers = true;
                            }
                        } else if (e.presIds) {
                            for (var i = 0; i < e.presIds.length; i++) {
                                var p = $.presentationToc.findItem(map, e.presIds[i]);
                                if (p && p.presentation && p.item && p.item.visible_with_service === true) {
                                    var layerIds = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                                    var status = p.presentation.service.checkLayerVisibility(layerIds);
                                    if (status) {
                                        foundVisibleLayers = true;
                                        checkedItems++;
                                    } else {
                                        uncheckedItems++;
                                    }
                                }
                            }
                        }
                    });

                    if (webgis.usability.makePresentationTocGroupCheckboxes === true && containsOnlyCheckboxes  === true) {
                        $collapsable.addClass('checkbox');
                        if (checkedItems > 0 && uncheckedItems === 0) {
                            $collapsable.data('checkbox-state', 'check1');
                        } else if (checkedItems > 0) {
                            $collapsable.data('checkbox-state', 'check01');
                        } else {
                            $collapsable.data('checkbox-state', 'check0');
                        }
                        var $checkboxIcon = $collapsable.children('div').children('.webgis-presentation_toc-item-group-checkbox');
                        if ($checkboxIcon.length === 1) {
                            $checkboxIcon.css('background-image', 'url(' + webgis.css.imgResource($collapsable.data('checkbox-state') + '.png', 'toc') + ')');
                        }
                    } else {
                        $collapsable.removeClass('checkbox');
                    }
                }

                if (foundVisibleLayers === false) {
                    $collapsable.removeClass('webgis-toc-visbile-container').removeClass('loading').addClass('webgis-toc-invisible-container');
                } else {
                    $collapsable.removeClass('webgis-toc-invisible-container').addClass('webgis-toc-visible-container');
                }
            });
        },
        showLegend: function (sender, map, services, title, selectedTab, showContextMenues, onLoaded) {
            if (sender) {
                $(sender).addClass('webgis-loading');
            }

            // reverse services
            var serviceIds = [], reversedServices = [], hasLegend = false;
            for (var s in services) {
                if (services[s].isWatermark()) {
                    continue;
                }
                serviceIds.push(s);
            }
            serviceIds = serviceIds.reverse();
            for (var i in serviceIds) {
                reversedServices[serviceIds[i]] = services[serviceIds[i]];
                hasLegend |= services[serviceIds[i]].hasLegend();
            }
            services = reversedServices;

            webgis.delayed(function () {
                var onload = function ($content) {
                    $content.addClass('webgis-legend-pro-content');
                    // Tabs
                    var $tabsTable = $("<table>").css('width', '100%').addClass('webgis-modal-content-fill').appendTo($content);
                    var $tabsTableTr = $("<tr>").css('white-space', 'nowrap').appendTo($tabsTable);
                    if (hasLegend) {
                        $("<td>")
                            .text(webgis.l10n.get("legend"))
                            .addClass('webgis-tab-header')
                            .attr('data-tab', '0')
                            .attr('data-name', 'legend')
                            .appendTo($tabsTableTr);
                    }
                    $("<td>")
                        .text(webgis.l10n.get("layers"))
                        .addClass('webgis-tab-header')
                        .attr('data-tab', '1')
                        .attr('data-name', 'themes')
                        .appendTo($tabsTableTr);
                    $("<td>")
                        .text(webgis.l10n.get("info-and-copyright"))
                        .addClass('webgis-tab-header')
                        .attr('data-tab', '2')
                        .attr('data-name', 'copyright')
                        .appendTo($tabsTableTr);
                    var $mapProperties = $("<td></td>")
                        .addClass('webgis-tab-header')
                        .appendTo($tabsTableTr)
                        .click(function (e) {
                            e.stopPropagation();

                            $('body').webgis_mapProperties({
                                map: map
                            });
                        });

                    $("<img>")
                        .attr('src', webgis.css.imgResource("admin-26.png", "ui"))
                        .appendTo($mapProperties);
                    
                    if (hasLegend) {
                        // Legend Content
                        var $legendTabContent = $("<div class='webgis-tab-content webgis-legend-tab-content'>")
                            .css('display', 'none')
                            .attr('data-tab', 0)
                            .data('services', services)
                            .appendTo($content);

                        $legendTabContent.data('refresh', function ($tabContent) {
                            var services = $tabContent.empty().data('services'), count = 0;
                            for (var s in services) {
                                var service = services[s];
                                if (!service ||
                                    service.isBasemap === true ||
                                    service.hasLegend() === false ||
                                    service.hasVisibleLegendInScale() === false)
                                    continue;

                                var guid = webgis.guid();
                                count++;

                                $("<h2></h2>").text(service.name).appendTo($tabContent);

                                var $legendHolder = $("<div id='" + guid + "'></div>")
                                    .css('display', 'none')
                                    .appendTo($tabContent);

                                webgis.ajax({
                                    url: service.getLegendUrl(true) + "&requestid=" + guid,
                                    type: 'post',
                                    data: service.getLegendPostData(),
                                    success: function (result) {

                                        if (result && result.requestid && result.url) {
                                            var $legend = $('#' + result.requestid);
                                            $("<img src='" + result.url + "' />").appendTo($legend);
                                            $legend.css('display', '');
                                        }

                                    }
                                });
                            }
                            if (count === 0) {
                                $("<div>")
                                    .text(webgis.l10n.get("info-no-legend"))
                                    .addClass('webgis-info')
                                    .appendTo($tabContent);
                            }
                        });
                        $legendTabContent.data('refresh')($legendTabContent);
                    }

                    // Themes Content
                    var $themesTabContent = $("<div class='webgis-tab-content webgis-themes-tab-content'>")
                        .css('display', 'none')
                        .attr('data-tab', 1)
                        .data('services', services)
                        .appendTo($content);

                    if ($.fn.webgis_contentsearch) {
                        $("<div></div>")
                            .appendTo($themesTabContent)
                            .webgis_contentsearch({
                                placeholder: webgis.l10n.get("find-layers"),
                                container_selectors: ["li", "ul", ".webgis-service-toc-container"],
                                onChanged: function (sender, val) {
                                    // Hide empty Lists (uls)
                                    
                                    $(sender)
                                        .find('ul.toc')
                                        .find('ul')
                                        .each(function (i, e) {
                                            var $ul = $(e);
                                            if ($ul.find('.webgis-search-content.active').length > 0) {
                                                var hasVisible = false;
                                                $ul.find('.webgis-search-content.active').each(function (j, l) {
                                                    hasVisible |= $(l).closest('li').css('display') !== 'none';
                                                });
                                                $ul.parent().css('display', hasVisible ? '' : 'none');
                                            }
                                        });
                                },
                                onReset: function (sender) {
                                    $(sender).find('li.webgis-services_toc-item.group')
                                        .css('display', 'block');
                                }
                            });
                    }

                    $themesTabContent.data('checkItemVisibility', function (service, parent) {
                        parent = parent || $('.webgis-services_toc-holder');
                        var $elem = $(parent);
                        $elem.find('.webgis-services_toc-item').each(function (i, li) {
                            if (!li.layers || li.layers.length === 0)
                                return;

                            if (li.layers[0].visible) {
                                $(li).find('.webgis-service_toc-checkable-icon').attr('src', webgis.css.imgResource('check1.png', 'toc')).addClass('webgis-checked');
                            }
                            else {
                                $(li).find('.webgis-service_toc-checkable-icon').attr('src', webgis.css.imgResource('check0.png', 'toc')).removeClass('webgis-checked');
                            }

                            var inScale = false;
                            for (var l in li.layers) {
                                var scale = service.map.scale();
                                var layer = li.layers[l];
                                if (layer.minscale > 0 || layer.maxscale > 0) {

                                    if ((layer.maxscale > 0 && scale > layer.maxscale) ||
                                        (layer.minscale > 0 && scale < layer.minscale)) {
                                        //$(li).addClass('webgis-outofscale');
                                    } else {
                                        inScale = true;
                                        //$(li).removeClass('webgis-outofscale');
                                    }
                                } else {
                                    inScale = true;
                                }
                            }
                            if (inScale) {
                                $(li).removeClass('webgis-outofscale');
                            } else {
                                $(li).addClass('webgis-outofscale');
                            }
                        });
                    });
                    $themesTabContent.data('refresh', function ($tabContent) {
                        $tabContent.children('.webgis-service-toc-container').each(function (i, service_container) {
                            $(service_container).children('ul.toc').each(function (t, toc) {
                                var service = toc.service;
                                $tabContent.data('checkItemVisibility')(service, $(toc));
                            });
                        });
                    });

                    for (var s in services) {
                        var service = services[s];
                        if (service.isBasemap === true || service.showInToc === false)
                            continue;

                        var $serviceContainer = $("<div>")
                            .addClass('webgis-service-toc-container')
                            .appendTo($themesTabContent)
                        $("<h2></h2>")
                            .text(service.name + "...")
                            .addClass('webgis-context-clickable')
                            .appendTo($serviceContainer)
                            .click(function () {
                                $(this).next().css('display', $(this).next().css('display') === 'none' ? '' : 'none');
                            });

                        // Opacity
                        var $menu_container = $("<li class='webgis-presentation_toc-basemap-opacity'></li>")
                            .css({/* maxWidth: 320,*/ margin: '0px', display: showContextMenues === true ? 'block' : 'none' })
                            .appendTo($serviceContainer);

                        $("<div>")
                            .addClass('webgis-presentation_toc-menu-item')
                            .text(webgis.l10n.get("remove-service"))
                            .css('background-image', 'url(' + webgis.css.imgResource('remove.png', 'tools') + ')')
                            .data('serviceId', service.id)
                            .appendTo($menu_container)
                            .click(function () {
                                var $this = $(this);
                                webgis.confirm({
                                    message: webgis.l10n.get('confirm-remove-service'),
                                    cancelText: webgis.l10n.get('confirm-remove-service-cancel'),
                                    okText: webgis.l10n.get('confirm-remove-service-ok'),
                                    onOk: function () {
                                        map.removeServices([$this.data('serviceId')]);
                                        $this.closest('.webgis-service-toc-container').remove();
                                    }
                                });
                            });

                        $("<div>")
                            .addClass('webgis-presentation_toc-menu-item')
                            .text(webgis.l10n.get("service-order")+"..")
                            .css('background-image', 'url(' + webgis.css.imgResource('rest/toolresource/webgis-tools-serviceorder-service_order', 'tools') + ')')
                            .data('serviceId', service.id)
                            .appendTo($menu_container)
                            .click(function () {
                                var $this = $(this);
                                webgis.modalDialog(webgis.l10n.get("services") + ": " + webgis.l10n.get("service-order"),
                                    function ($context) {
                                        $context.webgis_serviceOrder({ map: map, selected: $this.data('serviceId') });
                                    });
                            });

                        var $focusItem = $("<div>")
                            .addClass('webgis-presentation_toc-menu-item noicon nohover')
                            .addClass('webgis-presentation_toc-basemap-opacity-title')
                            .text(webgis.l10n.get("focus-service"))
                            .appendTo($menu_container);

                        $("<div>").appendTo($focusItem).webgis_focusservice_control({ service: service });

                        var $opacityItem = $("<div>")
                            .addClass('webgis-presentation_toc-menu-item noicon nohover')
                            .addClass('webgis-presentation_toc-basemap-opacity-title')
                            .text(webgis.l10n.get("opacity"))
                            .appendTo($menu_container);

                        $("<div>").appendTo($opacityItem).webgis_opacity_control({ service: service });

                        // Themes
                        var $toc_ul = $('<ul>')
                            .addClass('none')
                            .addClass('toc')
                            .appendTo($serviceContainer);
                        $toc_ul.get(0).service = service;

                        var scale = service.map.scale();
                        for (var l = 0, to = service.layers.length; l < to; l++) {
                            var layer = service.layers[l];
                            if (layer.locked || layer.tochidden)
                                continue;

                            var paths = (layer.undropdownable_groupname || layer.tocname || layer.name).split('\\');
                            var name = paths[paths.length - 1];

                            var $ul = $toc_ul;
                            if (paths.length > 1) {
                                for (var p = 0; p < paths.length - 1; p++) {
                                    var path = paths[p];
                                    var $path_ul = $ul.children("li[data-path='" + path + "']").children('ul');
                                    if ($path_ul.length === 0) {
                                        var $li = $("<li data-path='" + path + "'><i>" + path + "</i></li>")
                                            .addClass('webgis-services_toc-item group collapsed')
                                            .appendTo($ul)
                                            .click(function (e) {
                                                e.stopPropagation();
                                                var $this = $(this);
                                                $this.toggleClass('collapsed');
                                                $this.children('ul').css('display', $this.hasClass('collapsed') ? 'none' : 'block');
                                            });
                                        $ul = $("<ul>").addClass('none').css({ 'paddingLeft': 0, 'display':'none' }).appendTo($li);
                                    } else {
                                        $ul = $path_ul;
                                    }
                                }
                            }

                            var $toc_li = null;
                            // Es kann mehrere Themen mit gleichen Namen im TOC geben
                            // zB Wenn sich die Layer in einer nicht aufklappbaren Gruppe befinden (layer.undropdownable_groupname)
                            // => dann wird nur die Gruppe als Checkbox dargestellt und alle Layer darunter geschalten
                            $ul.children('li.webgis-services_toc-item.webgis-services_toc-checkable').each(function (i, li) {
                                var $li = $(li);
                                if ($li.data('toc-name') === name) {
                                    $toc_li = $li;
                                }
                            });
                            if ($toc_li == null) {
                                $toc_li = $("<li class='webgis-services_toc-item webgis-services_toc-checkable'></li>")
                                    .data('toc-name', name)
                                    .appendTo($ul);
                                $("<span class='webgis-search-content active'><img class='webgis-service_toc-checkable-icon' />&nbsp;" + webgis.encodeHtmlString(name) + "</span>").appendTo($toc_li);
                            }
                           
                            $toc_li.get(0).layers = $toc_li.get(0).layers || [];
                            $toc_li.get(0).layers.push(layer);
                            if ($toc_li.get(0).layers.length === 1) {
                                $toc_li.click(function (e) {
                                    e.stopPropagation();
                                    var $ul = $(this).closest('.toc');

                                    var service = $ul.get(0).service;
                                    var layers = this.layers;

                                    if (service && layers) {
                                        var layerIds = $.map(layers, function (layer) { return layer.id });
                                        service.setLayerVisibilityDelayed(layerIds, !$(this).find('.webgis-service_toc-checkable-icon').hasClass('webgis-checked'));

                                        $ul.closest('.webgis-themes-tab-content').data('checkItemVisibility')(service, $ul);
                                    }

                                });
                            }

                            //checkItemVisibility($toc_li, service, $toc_ul);
                        }

                        //$themesTabContent.data('checkItemVisibility')(service, $toc_ul);
                    }
                    $themesTabContent.data('refresh')($themesTabContent);

                    // Description Content
                    var $descriptionTabContent = $("<div class='webgis-tab-content'>")
                        .css('display', 'none')
                        .attr('data-tab', 2)
                        .appendTo($content);

                    var descriptionCount = 0;

                    var mapDescription = map.getMapDescription(true);
                    if (mapDescription) {
                        descriptionCount++;

                        $("<p>")
                            .html(webgis.simpleMarkdown.render(mapDescription))
                            .appendTo($descriptionTabContent);
                    }

                    for (var s in services) {
                        var service = services[s];

                        if (service.servicedescription || service.copyrighttext || service.copyrightId) {
                            descriptionCount++;
                            $("<h2></h2>")
                                .text(service.name)
                                .appendTo($descriptionTabContent);

                            // ToDo: Javascript injekction: sollte am Server geparsed werden...
                            if (service.servicedescription) {
                                var $descr = $("<div></div>")
                                    .addClass('webgis-service-description')
                                    .appendTo($descriptionTabContent);

                                if (service.servicedescription.indexOf('md:') === 0) {
                                    $("<p>")
                                        .html(webgis.asMarkdownOrText(service.servicedescription))
                                        .appendTo($descr);
                                } else {
                                    $.each(service.servicedescription.split('\n'), function (i, paragraph) {
                                        $("<p>").text(paragraph).appendTo($descr);
                                    });
                                    if ($.fn.webgis_collapsable_paragraph) {
                                        $descr.webgis_collapsable_paragraph();
                                    }
                                }
                            }

                            if (service.copyrighttext) {
                                var $cp = $("<div></div>")
                                    .addClass('webgis-service-copyrighttext')
                                    .appendTo($descriptionTabContent);

                                $.each(service.copyrighttext.split('\n'), function (i, paragraph) {
                                    $("<p>").text(paragraph).appendTo($cp);
                                });
                                if ($.fn.webgis_collapsable_paragraph) {
                                    $cp.webgis_collapsable_paragraph();
                                }
                            }
                            else if (service.copyrightId) { // CMS copyright
                                var copyright = map.getCopyright(service.copyrightId);
                                if (copyright) {
                                    var $cp = $("<div></div>")
                                        .text(service.copyrighttext)
                                        .addClass('webgis-service-copyrighttext')
                                        .appendTo($descriptionTabContent);

                                    if (copyright.logo) {
                                        $("<img src='" + copyright.logo + "' />")
                                            .css('width', copyright.logo_size && copyright.logo_size.length === 2 ? copyright.logo_size[0] : 'auto')
                                            .css('height', copyright.logo_size && copyright.logo_size.length === 2 ? copyright.logo_size[1] : 'auto');
                                    }
                                    $("<strong></strong>").text(copyright.copyright).appendTo($cp);
                                    $("<p></p>").text(copyright.advice).appendTo($cp);
                                    if (copyright.link) {
                                        $("<a href='" + copyright.link + "' target='_blank'>").text(copyright.link_text || copyright.text).appendTo($cp);
                                    }
                                    if ($.fn.webgis_collapsable_paragraph) {
                                        $cp.webgis_collapsable_paragraph();
                                    }
                                }
                            }
                        }
                    }

                    if (descriptionCount === 0) {
                        $("<div>Für die gewählten Dienste sind keine Copyright Information oder Beschreibungen vorhanden.</div>")
                            .addClass('webgis-info')
                            .appendTo($descriptionTabContent);
                    }

                    $content.find('.webgis-tab-header[data-tab]')
                        .click(function (e) {
                            e.stopPropagation();

                            var $tabHeader = $(this);
                            $tabHeader.parent().children('.webgis-tab-header').removeClass('selected');
                            $tabHeader.addClass('selected');
                            $tabHeader.closest('.webgis-modal-content').find('.webgis-tab-content').css('display', 'none');
                            $tabHeader.closest('.webgis-modal-content').find('.webgis-tab-content[data-tab=' + $tabHeader.attr('data-tab') + ']')
                                .css('display', 'block')
                                .find('.webgis-content-search-holder input')
                                .first()
                                .focus();


                        });

                    if (selectedTab) {
                        $content.find(".webgis-tab-header[data-tab=" + selectedTab + "]").trigger('click');
                    } else {
                        $content.find('.webgis-tab-header:first').trigger('click');
                    }

                    if (sender) {
                        $(sender).removeClass('webgis-loading');
                    }
                };
                $('body').webgis_modal({
                    id: 'webgis-legend',
                    title: title + ":",
                    onload: function ($content) {
                        onload($content);
                        if (onLoaded) {
                            onLoaded($content);
                        }
                    },
                    dock: 'left',
                    width: '400px', minWidth: '400px',
                    hasBlocker: false,
                    animate: false
                });
            }, 1);
        },
        hideLegend: function () {
            $(null).webgis_modal('close', { id: 'webgis-legend' });
        },
        refreshLegend: function (event, map, parent) {
            var $content = $(".webgis-legend-pro-content");
            if ($content.length > 0) {
                var $legendTabContent = $content.children(".webgis-tab-content.webgis-legend-tab-content");
                if ($legendTabContent.length > 0) {
                    //console.log('refresh legend');
                    $legendTabContent.data('refresh')($legendTabContent);
                }

                var $themesTabContent = $content.children('.webgis-tab-content.webgis-themes-tab-content');
                if ($themesTabContent.length > 0) {
                    $themesTabContent.data('refresh')($themesTabContent);
                }
            }
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem);
        elem._map = options.map;
        $elem.addClass('webgis-presentation_toc-holder');
        if (webgis.usability.presentationTocSearch === true && $.fn.webgis_contentsearch) {
            $("<div></div>")
                .addClass('webgis-presentation_toc-search')
                .appendTo($elem)
                .webgis_contentsearch({
                    placeholder: 'Inhalte filtern...',
                    container_selectors: ["li", "ul", ".webgis-presentation_toc-content", ".webgis-search-content-container"]
                });
        }
        var $ul = $("<ul id='webgis-presentation_toc-list'></ul>");
        $ul.appendTo($elem);
        if (options.map != null) {
            if (options.map.dynamicContent && options.map.dynamicContent.length > 0) {
                for (var c in options.map.dynamicContent) {
                    var dynamicContent = options.map.dynamicContent[c];
                    addDynamicContent({}, dynamicContent, elem);
                }
            }
            for (var serviceId in options.map.services) {
                var service = options.map.services[serviceId];
                addService({}, service, elem);
            }
        }
        if (options.gdi_button === true) {
            $("<button></button>")
                .text(webgis.l10n.get("add-services")+'...')
                .addClass('webgis-button light')
                .addClass('webgis-add-service-button')
                .addClass('webgis-search-content-alt-text')
                .attr('data-content-search-alt-text', webgis.l10n.get("search-in-services"))
                .attr('data-content-search-text', webgis.l10n.get("add-services"))
                .appendTo($elem)
                .click(function () {
                    var map = this.parentNode._map, $button = $(this);
                    $('body').webgis_modal({
                        title: webgis.l10n.get("add-services"),
                        onload: function ($content) {
                            $content.css('padding', '0px');
                            $content.webgis_addServicesToc({ map: map, searchTag: $button.attr('data-search-tag'), allowCustomServices: webgis.usability.allowAddCustomServices === true });
                        },
                        onclose: function () {
                            if ($.fn.webgis_contentsearch) {
                                $('.webgis-content-search-holder').webgis_contentsearch('trigger', {});
                            }
                        }
                    });
            });
        }
        elem._map.events.on('onaddservice', addService, elem);
        elem._map.events.on('onremoveservice', removeService, elem);
        elem._map.events.on('onadddynamiccontent', addDynamicContent, elem);
        elem._map.events.on('refresh', $.presentationToc.checkItemScaleVisibility, elem);
        elem._map.events.on('refresh', $.presentationToc.refreshLegend, elem);
        elem._map.events.on('onbuildtoolui', $.presentationToc.hideLegend, elem);
        elem._map.events.on('showhourglass_guids', function (e, sender, guids) {
            $elem.find('.webgis-presentation_toc-title, .webgis-presentation_toc-item-group').each(function (i, container) {
                var $container = $(container);
                if (!$container.hasClass('webgis-toc-invisible-container')) {
                    var serviceGuids = $container.data('serviceGuids');

                    var hasLoadingsServices = false;
                    if (serviceGuids && serviceGuids.length > 0) {
                        for (var g in guids) {
                            hasLoadingsServices |= $.inArray(guids[g], serviceGuids) >= 0;
                        }

                        if (hasLoadingsServices) {
                            $container.addClass('loading');
                        } else {
                            $container.removeClass('loading');
                        }
                    }
                }
            });
        });
        elem._map.events.on('hidehourglass_guids', function (e, sender) {
            $elem.find('.webgis-presentation_toc-title, .webgis-presentation_toc-item-group').each(function (i, container) {
                var $container = $(container);
                var serviceGuids = $container.data('serviceGuids');
                if (serviceGuids) {
                    $container.removeClass('loading');
                }
            });
        });
        elem._map.events.on('refresh-toc-checkboxes', function (channel, sender, args) {
            //console.log('refresh-toc-checkboxes', channel, sender, args, this);
            $.presentationToc.checkItemVisibility(channel, sender, args);
            $.presentationToc.checkContainerVisibility(this);
        }, elem);
    };

    var addService = function (e, service, parent) {
        //console.log('addService');
        parent = parent || this;
        if (service == null || service.map == null)
            return;

        if (service.isBasemap) {
            return addBasemap(e, service, parent);
        }
        if (service.presentations == null || service.presentations.length == 0)
            return;

        var $elem = $(parent); // $('.webgis-presentation_toc-holder');
        var $ul = $elem.children('#webgis-presentation_toc-list');
        for (var p = 0; p < service.presentations.length; p++) {
            var id = $.presentationToc.sequence++;
            var presentation = service.presentations[p];

            // zum Debuggen für bestimmte Darstellungsvariante
            //if (presentation.name == "Gewässermorphologie") {  
            //    debugger;
            //}

            // To find this presentation after click...
            presentation.sequenceId = id;
            for (var i = 0; i < presentation.items.length; i++) {
                //console.log(presentation.items[i]);
                var prop = presentation.items[i];
                var pid = $.presentationToc.sequence++;
                prop.id = pid;
                var $li = null, $item_ul = null;
                $ul.find('.webgis-presentation_toc-title-text').each(function (i, obj) {
                    if ($(obj).text() == prop.container) {
                        $li = $(obj.parentNode);
                    }
                });
                if ($li == null) {
                    $li = $("<li></li>")
                        .addClass("webgis-presentation_toc-title webgis-presentation_toc-collapsable webgis-search-content-container")
                        .attr('data-container', prop.container)
                        .attr('id', id);

                    if (prop.container_order) {
                        $li.attr('data-order', prop.container_order);
                    }
                    $li.data('serviceGuids', []).appendTo($ul);

                    var li = $li.get(0);
                    $("<span style='position:absolute' class='webgis-presentation_toc-plus webgis-api-icon webgis-api-icon-triangle-1-s'></span>")
                        .appendTo($li)
                        .click(function (event) {
                            event.stopPropagation();
                            $(this).parent().find('.webgis-presentation_toc-title-text').first().trigger('click');
                        });
                    //
                    $("<div class='webgis-presentation_toc-title-text webgis-search-content webgis-text-ellipsis-pro check-for-title'></div>")
                        .text(prop.container)
                        .click(function (event) {
                            $li = $(this.parentNode);
                            event.stopPropagation();
                            if (isInContentSearchMode(this, true))
                                $(this.parentNode).removeClass('webgis-expanded');

                            if ($(this.parentNode).hasClass('webgis-expanded')) {
                                $li.removeClass('webgis-expanded');
                                $li.children('.webgis-presentation_toc-content').slideUp()
                                    .find('.webgis-presentation_toc-item')
                                    .addClass('webgis-presentation_toc-hidden');
                                $li.children('.webgis-presentation_toc-plus')
                                    .removeClass('webgis-api-icon-triangle-1-e')
                                    .addClass('webgis-api-icon-triangle-1-s');
                            }
                            else {
                                $li.closest('ul')
                                    .find('.webgis-expanded')
                                    .removeClass('webgis-expanded')
                                    .find('.webgis-presentation_toc-content').slideUp()
                                    .find('.webgis-presentation_toc-item')
                                    .addClass('webgis-presentation_toc-hidden');

                                $li.closest('ul')
                                    .find('.webgis-presentation_toc-plus')
                                    .removeClass('webgis-api-icon-triangle-1-e')
                                    .addClass('webgis-api-icon-triangle-1-s');

                                $li.addClass('webgis-expanded');
                                $li.children('.webgis-presentation_toc-content').slideDown()
                                    .find('.webgis-presentation_toc-item')
                                    .removeClass('webgis-presentation_toc-hidden');
                                $li.children('.webgis-presentation_toc-plus')
                                    .removeClass('webgis-api-icon-triangle-1-s')
                                    .addClass('webgis-api-icon-triangle-1-e');
                            }
                        })
                        .appendTo($li);

                    var collectServices = function ($this) {
                        var $holder = $this.closest('.webgis-presentation_toc-holder');
                        if ($holder.length == 0)
                            return null;
                        var map = $holder.get(0)._map;
                        // Collect Services
                        var services = null;
                        $this.closest('.webgis-presentation_toc-title').find('.webgis-presentation_toc-item').each(function (i, e) {
                            if (e.presIds) {
                                for (var p in e.presIds) {
                                    var item = $.presentationToc.findItem(map, e.presIds[p]);
                                    if (item && item.item && item.item.visible_with_service === true &&
                                        item.presentation && item.presentation.layers && item.presentation.layers.length > 0
                                        && item.presentation.service) {
                                        if (item.presentation.service && item.presentation.service.canLegend()) {
                                            if (services == null)
                                                services = [];
                                            if (!services[item.presentation.service.id]) {
                                                services[item.presentation.service.id] = item.presentation.service;
                                            }
                                        }
                                    }
                                }
                            }
                        });

                        return { map: map, services: services };
                    };

                    $("<div class='webgis-presentation_toc-title-legend-icon'></div>")
                        .attr("title", webgis.l10n.get("legend"))
                        .attr("alt", webgis.l10n.get("legend"))
                        .appendTo($li) // Legend icon
                        .css('background-image', 'url(' + webgis.css.imgResource('legend-24.png', 'toc') + ')')
                        .click(function () {
                            var serviceCollection = collectServices($(this));
                            if (serviceCollection && serviceCollection.services != null) {
                                $.presentationToc.showLegend(
                                    this,
                                    serviceCollection.map,
                                    serviceCollection.services,
                                    $(this).closest('.webgis-presentation_toc-title').find('.webgis-presentation_toc-title-text').html());
                            } else {
                                webgis.alert(webgis.l10n.get('msg-no-legend-available'), 'info');
                            }
                        })
                        .contextmenu(function () {
                            var serviceCollection = collectServices($(this));
                            if (serviceCollection && serviceCollection.services != null) {
                                $.presentationToc.showLegend(
                                    this,
                                    serviceCollection.map,
                                    serviceCollection.services,
                                    $(this).closest('.webgis-presentation_toc-title').find('.webgis-presentation_toc-title-text').html(),
                                    "1", true);
                            } else {
                                webgis.alert(webgis.l10n.get('msg-no-services-available'), 'info');
                            }

                            return false;
                        });
                    var $div = $("<div class='webgis-presentation_toc-content' style='display:none' id='div_'></div>");
                    $div.appendTo($li);
                    $item_ul = $("<ul style='margin:8px 0px 0px 6px'></ul>");
                    $item_ul.appendTo($div);
                }
                else {
                    $item_ul = $li.find('.webgis-presentation_toc-content').children('ul');
                }

                if (prop.visible_with_service === true && $.inArray(service.guid, $li.data('serviceGuids')) < 0) {
                    $li.data('serviceGuids').push(service.guid);
                }

                var $parentUl = null, $item_li = null, item_li, itemname = prop.name || presentation.name, $group_li = null;
                var isDropdownGroup = (prop.groupstyle == 'dropdown' && prop.name);
                if (isDropdownGroup) {
                    $item_ul.find("li.webgis-presentation_toc-item-group.webgis-presentation_toc-collapsable").each(function (i, li) {  // .webgis-presentation_toc-item-group.webgis-presentation_toc-collapsable => nur dropdown gruppen suchen, sonst kann es zu problemen kommen, wenn es Darstellungsvarianten mit dem gleichen Theme gibt
                        if (li.groupname && li.groupname == prop.name) {
                            $group_li = $(li);
                            $parentUl = $group_li.children("ul");
                        }
                    });

                    var clickGroupFunction = function () {
                        var $this = $(this);

                        if (isInContentSearchMode(this, true))
                            $this.children('.webgis-api-icon-triangle-1-e').removeClass('webgis-api-icon-triangle-1-e').addClass('webgis-api-icon-triangle-1-s');

                        if ($this.children('.webgis-api-icon-triangle-1-e').length > 0) {
                            $this.children('.webgis-api-icon-triangle-1-e').removeClass('webgis-api-icon-triangle-1-e').addClass('webgis-api-icon-triangle-1-s');
                            $this.parent().children('ul').slideUp();
                        }
                        else {
                            $this.children('.webgis-api-icon-triangle-1-s').removeClass('webgis-api-icon-triangle-1-s').addClass('webgis-api-icon-triangle-1-e');
                            $this.parent().children('ul').slideDown();

                            var $container = $this.closest('.webgis-search-content-container');
                            if (!$container.hasClass('webgis-expanded')) {
                                $container.children('.webgis-presentation_toc-title-text').trigger('click');
                            }
                        }
                    };

                    if ($parentUl == null) {
                        $group_li = $("<li></li>")
                            .addClass('webgis-presentation_toc-item-group webgis-presentation_toc-collapsable')
                            .data('serviceGuids', [])
                            .attr('data-ui-groupname', prop.name)
                            .attr('data-dvid', 'dvg_' + prop.name.toLowerCase().replace(/ /g, '_')).appendTo($item_ul);

                        if (prop.group_order)
                            $group_li.attr('data-order', prop.group_order);

                        $group_li.get(0).groupname = prop.name;
                        var $title = $("<div class='webgis-text-ellipsis-pro check-for-title'></div>").appendTo($group_li)
                            .click(clickGroupFunction);
                        $("<span style='position:absolute' class='webgis-api-icon webgis-api-icon-triangle-1-s'></span>").appendTo($title);
                        $("<span class='webgis-presentation_toc-item-group-checkbox'><span>").appendTo($title).click(function (e) { _groupCheckBoxClicked(this, e); });
                        $("<span style='margin-left:14px' class='webgis-search-content'>&nbsp;" + webgis.encodeHtmlString(prop.name) + "</span>").appendTo($title);

                        $parentUl = $("<ul style='display:none'></ul>").appendTo($group_li);
                    }
                    itemname = presentation.name;

                    if (prop.ui_groupname) {
                        var ui_groupnameParts = prop.ui_groupname.replaceAll("\\/", "&sol;").split('/'); // slash (&sol;) can be encoded in CMS als \/
                        for (var gn in ui_groupnameParts) {
                            var ui_groupname = ui_groupnameParts[gn].replaceAll("&sol;", "/");  // encode back => &sol; => /

                            //console.log(ui_groupname);

                            var $ui_group = $parentUl.children("li[data-ui-groupname='" + ui_groupname + "']");
                            if ($ui_group.length === 0) {
                                $ui_group = $("<li></li>")
                                    .addClass('webgis-presentation_toc-item-group webgis-presentation_toc-collapsable')
                                    .data('serviceGuids', [])
                                    .attr('data-order', prop.item_order)
                                    .attr('data-ui-groupname', ui_groupname)
                                    .appendTo($parentUl);

                                var $ui_title = $("<div class='webgis-text-ellipsis-pro check-for-title'></div>")
                                    .appendTo($ui_group)
                                    .click(clickGroupFunction);

                                $("<span style='position:absolute' class='webgis-api-icon webgis-api-icon-triangle-1-s'></span>").appendTo($ui_title);
                                $("<span class='webgis-presentation_toc-item-group-checkbox'><span>").appendTo($ui_title).click(function (e) { _groupCheckBoxClicked(this, e); });
                                $("<span style='margin-left:14px' class='webgis-search-content'>&nbsp;" + webgis.encodeHtmlString(ui_groupname) + "</span>").appendTo($ui_title);

                                $parentUl = $("<ul style='display:none'></ul>").appendTo($ui_group);
                            } else {
                                $parentUl = $ui_group.children('ul');
                            }
                        }
                    }
                }
                else {
                    $parentUl = $item_ul;
                }

                if (prop.visible_with_service === true &&
                    $parentUl.parent().hasClass('webgis-presentation_toc-item-group') &&
                    $parentUl.parent().data('serviceGuids') &&
                    $.inArray(service.guid, $parentUl.parent().data('serviceGuids')) < 0) {
                    $parentUl.parent().data('serviceGuids').push(service.guid);
                }

                var hasMetadata_iButton = (prop.group_metadata || prop.metadata) && prop.metadata_button_style == 'i_button';

                if (prop.group_metadata && $group_li != null && $group_li.find('.webgis-api-icon.webgis-api-icon-info').length == 0) {
                    var $metadataButton = $("<span style='position:absolute;left:5px;' class='webgis-api-icon webgis-api-icon-info'></span>")
                        .prependTo($group_li.children('div'))
                        .data('metadata', prop.group_metadata)
                        .data('metadata_target', prop.group_metadata_target)
                        .attr('title', prop.group_metadata_title)
                        .click(function (event) {
                            event.stopPropagation();
                            _showMetadataLink($(this));
                        });
                }
                $parentUl.find('li').each(function (i, e) {
                    if (e.groupname == itemname) {
                        item_li = e;
                        $item_li = $(e);
                    }
                });
                if ($item_li == null) {
                    var isGroup = false; // Experimental: Richtige id für parametrierten Aufruf finden. Gruppen haben das Kürzel dvg_ -> sonst presentation.id...
                    if (presentation.items) {
                        for (var pitem in presentation.items) {
                            if (presentation.items[pitem].name) {
                                isGroup = true;
                                break;
                            }
                        }
                    }
                    //console.log(presentation.id, isGroup);
                    var dvid = $parentUl.closest('.webgis-presentation_toc-item-group').attr('data-dvid');
                    dvid = dvid ? dvid + '/' + presentation.id : (isGroup ? 'dvg_' + itemname.toLowerCase().replace(/ /g, '_') : presentation.id);
                    $item_li = $("<li class='webgis-presentation_toc-item webgis-scaledependent'></li>")
                        .addClass('webgis-text-ellipsis-pro check-for-title')
                        .attr('data-dvid', dvid);
                    if (isDropdownGroup && prop.item_order)
                        $item_li.attr('data-order', prop.item_order);
                    else if (prop.group_order)
                        $item_li.attr('data-order', prop.group_order);
                    $item_li.appendTo($parentUl);
                    item_li = $item_li.get(0);
                    item_li.groupname = itemname;
                    item_li.presIds = [], item_li.ownerIds = [];
                    if (prop.style === 'checkbox') {
                        $item_li.addClass('webgis-presentation_toc-checkable checkbox');
                        var chk = "<img class='webgis-presentation_toc-checkable-icon' src=" + webgis.css.imgResource("check0.png", "toc") + ">";
                        $("<span class='webgis-search-content'>" + chk + "&nbsp;<span class='webgis-text-span nowrap'>" + webgis.encodeHtmlString(itemname) + "</span></span>").appendTo($item_li);
                        $item_li.click(function () {
                            $.presentationToc.process(this, this.presIds, true);
                            //sendCmd('button', '5D3A7A26-2953-45b6-A1CD-40B75B669BFA|' + (this.groupid ? this.groupid : this.pid) + '|' + ($D('_img' + this.pid).src.indexOf('1.png') != -1 ? 'false' : 'true'), 'get');
                        });
                    }
                    else if (prop.style === 'optionbox') {
                        //console.log('add option box');
                        $item_li.addClass('webgis-presentation_toc-checkable optionbox');
                        var opt = "<img class='webgis-presentation_toc-checkable-icon' src=" + webgis.css.imgResource("option0.png", "toc") + ">";
                        $("<span class='webgis-search-content'>" + opt + "&nbsp;<span class='webgis-text-span nowrap'>" + webgis.encodeHtmlString(itemname) + "<span></span>").appendTo($item_li);
                        $item_li.click(function () {
                            $.presentationToc.process(this, this.presIds, true);
                        });
                    }
                    else if (prop.style === 'dynamiccontentmarker') {
                        var query = service.getLayerIdsFromNames(presentation.layers).length === 1 ? service.getQueryByLayerId(service.getLayerIdsFromNames(presentation.layers)[0]) : null;
                        if (query) {
                            var contentId = webgis.guid();
                            $item_li
                                .attr('id', contentId)
                                .addClass('webgis-presentation_toc-dyncontent-item')
                                .data('map', service.map)
                                .data('dynamic-content', {
                                    fromTocTheme: true,
                                    autoZoom: false,
                                    extentDependend: true,
                                    id: contentId,
                                    name: presentation.name,
                                    suppressAutoLoad: true,
                                    suppressShowResult: true,
                                    type: 'api-query',
                                    url: webgis.baseUrl + '/rest/services/' + service.id + '/queries/' + query.id + '/query?f=json&'
                                })
                                .click(function (e) {
                                    e.stopPropagation();
                                    var map = $(this).data('map');
                                    var dynamicContent = $(this).data('dynamic-content');
                                    if ($(this).hasClass('webgis-presentation_toc-dyncontent-item-selected')) {
                                        var hasHistory = $.webgis_search_result_histories[map] != null && $.webgis_search_result_histories[map].count() > 0
                                        map.queryResultFeatures.clear(hasHistory);
                                        map.getSelection('selection').remove();
                                        map.getSelection('query').remove();
                                        $(this).removeClass('webgis-presentation_toc-dyncontent-item-selected')
                                        map.unloadDynamicContent();
                                    } else {
                                        map.addDynamicContent([dynamicContent]);
                                        map.loadDynamicContent(map.getDynamicContent(dynamicContent.id));
                                    }
                                });
                            $("<span class='webgis-text-span nowrap'><span>")
                                .text(itemname)
                                .appendTo($item_li);
                            if (query.apply_zoom_limits !== true)
                                $item_li.removeClass('webgis-scaledependent');
                        } else {
                            $item_li.remove();
                        }
                    }
                    else {
                        var btn = "<img style='width:16px' src=" + webgis.css.imgResource("layers-16.png", "toc") + ">";
                        $("<span class='webgis-search-content'>" + btn + "&nbsp;<span class='webgis-text-span nowrap'>" + webgis.encodeHtmlString(itemname) + "<span></span>").appendTo($item_li);
                        $item_li.click(function () {
                            var c = $.presentationToc.process(this, this.presIds);
                            //sendCmd('button', '5D3A7A26-2953-45b6-A1CD-40B75B669BFA|' + (this.groupid ? this.groupid : this.pid), 'get');
                        });
                    }


                }

                item_li.presIds = item_li.presIds || [];
                item_li.ownerIds = item_li.ownerIds || [];

                item_li.presIds.push(pid);
                if (prop.visible_with_service == true)
                    item_li.ownerIds.push(service.id);
                if (prop.visible_with_one_of_services != null && prop.visible_with_one_of_services.length > 0) {
                    for (var vis_i in prop.visible_with_one_of_services) {
                        item_li.ownerIds.push(prop.visible_with_one_of_services[vis_i]);
                    }
                }

                var metadata_prefix = prop.groupstyle !== 'dropdown' ? 'group_' : '';

                if (prop[metadata_prefix + 'metadata']
                    && $item_li.find('.webgis-api-icon.webgis-api-icon-info').length === 0
                    && $item_li.next('.webgis-presentation_toc-item.link-button').length === 0) {
                    var $metadataButton = $("<span></span>")
                        .data('metadata', prop[metadata_prefix + 'metadata'])
                        .data('metadata_target', prop[metadata_prefix + 'metadata_target'])
                        .data('metadata_title', prop[metadata_prefix + 'metadata_title']);
                        
                        
                    switch (prop[metadata_prefix + 'metadata_button_style']) {
                        case 'link_button':
                            var $li = $("<li>")
                                .addClass('webgis-presentation_toc-item link-button')
                                .attr('data-order', $item_li.attr('data-order'))
                                .appendTo($item_li.parent());

                            $metadataButton
                                .addClass('webgis-button')
                                .text(prop[metadata_prefix + 'metadata_title'])
                                .appendTo($li);
                            break;
                        default:
                            hasMetadata_iButton = true;
                            $metadataButton
                                .css({ position: 'absolute', left: '26px' })
                                .addClass('webgis-api-icon webgis-api-icon-info').prependTo($item_li)
                                .attr('title', prop.metadata_title);
                            break;
                    }

                    $metadataButton.click(function (event) {
                        event.stopPropagation();
                        _showMetadataLink($(this));
                    });
                }

                if (hasMetadata_iButton === true) {
                    $item_li.children('.webgis-search-content').children('.webgis-text-span').css('paddingLeft', '16px');
                }

                if (prop.visible === false) {
                    $item_li.addClass('webgis-visibility-never');
                }

                if (prop.client_visibility) {
                    $item_li.addClass('webgis-visibility-client-' + prop.client_visibility);
                }
            }
        }
        $.presentationToc.checkItemVisibility(null, service, parent);
        $.presentationToc.checkItemScaleVisibility(null, service.map, parent);
        $.presentationToc.checkContainerVisibility(parent);

        service.events.on(['onchangevisibility','onchangevisibility-delayed'], $.presentationToc.checkItemVisibility, parent);
        sortToc(parent);

        webgis.addTitleToEllipsis();
    };
    var addDynamicContent = function (e, content, parent) {
        //console.log('addDynamicContent');
        //console.log(content);
        parent = parent || this;
        if (content == null || content.map == null)
            return;

        if ($(content.map._webgisContainer).find("#" + content.id + ".webgis-presentation_toc-dyncontent-item").length > 0)  // already exists (eg anywhere in TOC...)
            return;

        var $elem = $(parent);
        var $ul = $elem.children('#webgis-presentation_toc-list'), $li = null, $item_ul = null, $item_li = null;
        var containerName = webgis.l10n.get("dynamic-content");
        $ul.find('.webgis-presentation_toc-title-text').each(function (i, obj) {
            if ($(obj).text() == containerName) {
                $li = $(obj.parentNode);
            }
        });
        if ($li == null) {
            var id = $.presentationToc.sequence++;
            $li = $("<li></li>")
                .addClass("webgis-presentation_toc-title webgis-presentation_toc-collapsable webgis-dynamiccontent-toc-container")
                .attr('data-container', '__dynamic-content')
                .attr("id", id);

            $li.attr('data-order', 0);
            $li.prependTo($ul);
            $("<span style='position:absolute' class='webgis-presentation_toc-plus webgis-api-icon webgis-api-icon-triangle-1-s'></span>")
                .appendTo($li)
                .click(function (event) {
                    event.stopPropagation();
                    $(this).parent().find('.webgis-presentation_toc-title-text').first().trigger('click');
                });

            $("<div class='webgis-presentation_toc-title-text'></div>")
                .text(containerName)
                .click(function (event) {
                    $li = $(this.parentNode);
                    event.stopPropagation();
                    if ($(this.parentNode).hasClass('webgis-expanded')) {
                        $li.removeClass('webgis-expanded');
                        $li.children('.webgis-presentation_toc-content').slideUp()
                            .find('.webgis-presentation_toc-item')
                            .addClass('webgis-presentation_toc-hidden');
                        $li.children('.webgis-presentation_toc-plus')
                            .removeClass('webgis-api-icon-triangle-1-e')
                            .addClass('webgis-api-icon-triangle-1-s');
                    }
                    else {
                        $li.closest('ul')
                            .find('.webgis-expanded')
                            .removeClass('webgis-expanded')
                            .find('.webgis-presentation_toc-content').slideUp()
                            .find('.webgis-presentation_toc-item')
                            .addClass('webgis-presentation_toc-hidden');

                        $li.closest('ul')
                            .find('.webgis-presentation_toc-plus')
                            .removeClass('webgis-api-icon-triangle-1-e')
                            .addClass('webgis-api-icon-triangle-1-s');

                        $li.addClass('webgis-expanded');
                        $li.children('.webgis-presentation_toc-content').slideDown()
                            .find('.webgis-presentation_toc-item')
                            .removeClass('webgis-presentation_toc-hidden')
                            .each(function (i, e) {
                                if (e.resetPreview) {
                                    e.resetPreview.apply(e);
                                }
                            });
                        $li.children('.webgis-presentation_toc-plus')
                            .removeClass('webgis-api-icon-triangle-1-s')
                            .addClass('webgis-api-icon-triangle-1-e');
                    }
                })
                .appendTo($li);

            var $div = $("<div class='webgis-presentation_toc-content' style='display:none;white-space:normal;overflow:hidden' id='div_'></div>");
            $div.appendTo($li);
            $item_ul = $("<ul style='margin:8px 0px 0px 0px;'></ul>");
            $item_ul.appendTo($div);

            content.map.events.on('onloaddynamiccontent', function (e, sender) {
                $.presentationToc.checkContainerVisibility(this);
            }, parent);
        }
        else {
            $item_ul = $li.find('.webgis-presentation_toc-content').children('ul');
        }

        $item_li = $("<li id='" + content.id + "' class='webgis-presentation_toc-item webgis-presentation_toc-dyncontent-item'></li>")
            .appendTo($item_ul)
            .data('dynamic-content', content)
            .click(function () {
                var c = $(this).data('dynamic-content');
                if ($(this).hasClass('webgis-presentation_toc-dyncontent-item-selected')) {

                    var $holder = $(c.map.ui.webgisContainer()).find('.webgis-search-result-list').closest('.webgis-search-result-holder'),
                        hasHistory = $.webgis_search_result_histories[c.map] != null && $.webgis_search_result_histories[c.map].count() > 0;

                    c.map.queryResultFeatures.clear(hasHistory);
                    c.map.getSelection('selection').remove();
                    $holder.find('.webgis-search-result-list').webgis_queryResultsList('empty', { map: c.map });
                    $holder.get(0).search_term = '';
                    if (hasHistory) {
                        $.webgis_search_result_histories[c.map].render($holder.find('.webgis-search-result-list'));
                    }
                    else {
                        c.map.events.fire('hidequeryresults');
                    }
                    c.map.getSelection('query').remove();
                    c.map.loadDynamicContent(null);  // remove current (necessay, if extent depended)
                } else {

                    if (c) {
                        c.map.loadDynamicContent(c);
                    }

                }
                //$(this).toggleClass('webgis-presentation_toc-dyncontent-item-selected');
            });


        var img = content.map.ui.dynamicContentIcon(content);

        $("<span class='webgis-search-content'><img style='width:16px' src=" + img + ">&nbsp;" + webgis.encodeHtmlString(content.name) + "</span>").appendTo($item_li);
    };
    var addBasemap = function (e, service, parent) {
        var $elem = $(parent);
        var $ul = $elem.children('#webgis-presentation_toc-list'), $li = null, $item_ul = null, $item_li = null;
        var containerName = webgis.l10n.get("basemaps");
        $ul.find('.webgis-presentation_toc-title-text').each(function (i, obj) {
            if ($(obj).text() == containerName) {
                $li = $(obj.parentNode);
            }
        });
        if ($li == null) {
            var id = $.presentationToc.sequence++, map = service.map;

            $li = $("<li></li>")
                .addClass("webgis-presentation_toc-title webgis-presentation_toc-collapsable webgis-basemap-toc-container")
                .attr("data-container", "__basemaps")
                .attr("id", id);

            $li.attr('data-order', 0);
            $li.appendTo($ul);
            $("<span style='position:absolute' class='webgis-presentation_toc-plus webgis-api-icon webgis-api-icon-triangle-1-s'></span>")
                .appendTo($li)
                .click(function (event) {
                    event.stopPropagation();
                    $(this).parent().find('.webgis-presentation_toc-title-text').first().trigger('click');
                });

            $("<div class='webgis-presentation_toc-title-text webgis-text-ellipisis-pro check-for-title'></div>")
                .text(containerName)
                .click(function (event) {
                    $li = $(this.parentNode);
                    event.stopPropagation();
                    if ($(this.parentNode).hasClass('webgis-expanded')) {
                        $li.removeClass('webgis-expanded');
                        $li.children('.webgis-presentation_toc-content').slideUp()
                            .find('.webgis-presentation_toc-item')
                            .addClass('webgis-presentation_toc-hidden');
                        $li.children('.webgis-presentation_toc-plus')
                            .removeClass('webgis-api-icon-triangle-1-e')
                            .addClass('webgis-api-icon-triangle-1-s');
                    }
                    else {
                        $li.closest('ul')
                            .find('.webgis-expanded')
                            .removeClass('webgis-expanded')
                            .find('.webgis-presentation_toc-content').slideUp()
                            .find('.webgis-presentation_toc-item')
                            .addClass('webgis-presentation_toc-hidden');

                        $li.closest('ul')
                            .find('.webgis-presentation_toc-plus')
                            .removeClass('webgis-api-icon-triangle-1-e')
                            .addClass('webgis-api-icon-triangle-1-s');

                        $li.addClass('webgis-expanded');

                        $li.children('.webgis-presentation_toc-content').slideDown()
                            .find('.webgis-presentation_toc-item')
                            .removeClass('webgis-presentation_toc-hidden')
                            .each(function (i, e) {
                                if (e.resetPreview) {
                                    e.resetPreview.apply(e);
                                }
                            });
                        $li.children('.webgis-presentation_toc-plus').removeClass('webgis-api-icon-triangle-1-s').addClass('webgis-api-icon-triangle-1-e');
                    }
                })
                .appendTo($li);

            var $div = $("<div class='webgis-presentation_toc-content' style='display:none;white-space:normal;overflow:hidden;margin-top:5px' id='div_'></div>");
            $div.appendTo($li);
            $item_ul = $("<ul style='margin:8px 0px 0px 0px;'></ul>");
            $item_ul.appendTo($div);
            $item_li = $("<li>")
                .addClass('webgis-no-basemap')
                .addClass('webgis-presentation_toc-item webgis-presentation_toc-basemap-item-block webgis-presentation_toc-basemap-item');

            $item_li.appendTo($item_ul);
            $item_li.get(0).map = map;
            $("<div>")
                .addClass('webgis-presentation_toc-basemap-item-label')
                .text('Kein Hintergrund')
                .appendTo(
                    $("<div>")
                        .addClass('webgis-presentation_toc-basemap-item-img')
                        .appendTo($item_li)
                );
            $item_li.click(function () {
                this.map.setBasemap(null);
                $(this).parent().find('.webgis-presentation_toc-basemap-item-img').removeClass('selected');
                this.map.setBasemap(null, true);
                //console.log(webgis.useMobileCurrent());
                if (webgis.useMobileCurrent()) {
                    this.map.events.fire('hidepresentations');
                }
            });
            var $collapse_container = $("<li class='webgis-presentation_toc-basemap-collapse'></li>")
                .appendTo($item_ul)
                .click(function () {
                    var $this = $(this);
                    $this.toggleClass('expanded');

                    $ul.find('li.webgis-presentation_toc-basemap-item').each(function (i, e) {
                        if (i >= 3 && !$this.hasClass('expanded')) {
                            $(e).css('display', 'none');
                        } else {
                            $(e).css('display', '');
                        }
                    });
                });

            var $opacity_container = $("<li>")
                .addClass('webgis-presentation_toc-basemap-opacity')
                .appendTo($item_ul);
            $("<div>")
                .addClass('label')
                .text(webgis.l10n.get("opacity")+":")
                .appendTo($opacity_container);
            $("<div>")
                .addClass('buttons')
                .appendTo($opacity_container).webgis_opacity_control({
                service: function () { return map.getService(map.currentBasemapServiceId()) || map.getService(map.currentBasemapOverlayServiceId()); }
            });

            service.map.events.on('basemap_changed', function (e, sender, basemapService) {
                $(this).webgis_opacity_control('refresh');
                //$(this).css('display', sender.currentBasemapServiceId() || sender.currentBasemapOverlayServiceId() ? '' : 'none');
                sender.ui.refreshUIElements();
            }, $opacity_container);
            service.map.events.on('basemap_changed', function (e, sender, basemapService) {
                $(this).find('.webgis-presentation_toc-basemap-item').each(function (i, e) {
                    if (basemapService && basemapService.id === $(e).attr('id')) {
                        $(e).children('.webgis-presentation_toc-basemap-item-img').addClass('selected');
                    } else {
                        $(e).children('.webgis-presentation_toc-basemap-item-img').removeClass('selected');
                    }
                });

                $(this).find('.webgis-presentation_toc-basemap-overlay').each(function (i, e) {
                    $(e).find('img').attr('src', webgis.css.imgResource(sender.currentBasemapOverlayServiceId() === $(e).attr('id') ? "check1.png" : "check0.png", "toc"))
                });

                $.presentationToc.checkContainerVisibility();
            }, $li);
        }
        else {
            $item_ul = $li.find('.webgis-presentation_toc-content').children('ul');
        }
        if (service.basemapType === 'overlay') {
            $item_li = $("<li id='" + service.id + "'class='webgis-presentation_toc-item webgis-presentation_toc-basemap-item webgis-presentation_toc-basemap-overlay' style='width:100%;height:20px;display:none'></li>");
            $item_li.insertBefore($item_ul.find('.webgis-presentation_toc-basemap-collapse'));
            var item_li = $item_li.get(0);
            item_li.service = service;
            var isChecked = service.map.currentBasemapOverlayServiceId() === service.id;
            if (isChecked === true) {
                $item_li.addClass('checked');
            }
            var chk = "<img style='width:16px' src=" + webgis.css.imgResource(isChecked ? "check1.png" : "check0.png", "toc") + ">";
            $("<span>" + chk + "&nbsp;" + webgis.encodeHtmlString(service.name) + "</span>").appendTo($item_li);
            $item_li.click(function () {
                var service = this.service;
                var $li = $(this);
                $li.toggleClass('checked');
                var checked = $li.hasClass('checked');
                $li.parent().find('.webgis-presentation_toc-basemap-overlay').removeClass('checked').find('img').attr('src', webgis.css.imgResource("check0.png", "toc"));
                if (checked) {
                    $li.addClass('checked').find('img').attr('src', webgis.css.imgResource("check1.png", "toc"));
                    service.map.setBasemap(service.id, true);
                }
                else {
                    $li.find('img').attr('src', webgis.css.imgResource("check0.png", "toc"));
                    service.map.setBasemap(null, true);
                }
            });
        }
        else {
            $item_li = $("<li id='" + service.id + "' class='webgis-presentation_toc-item webgis-presentation_toc-basemap-item webgis-presentation_toc-basemap-item-block webgis-presentation_toc-hidden'></li>");
            if ($item_ul.closest('.webgis-presentation_toc-collapsable').hasClass('webgis-expanded')) {
                //console.log('remove hidden');
                $item_li.removeClass('webgis-presentation_toc-hidden');
            }
            $item_li.insertBefore($item_ul.find('.webgis-no-basemap')); //.appendTo($item_ul);
            var item_li = $item_li.get(0);
            item_li.service = service;
            var $imgDiv = $("<div class='webgis-presentation_toc-basemap-item-img" + (service.map.currentBasemapServiceId() == service.id ? " selected" : "") + "'></div>").appendTo($item_li);
            $("<div class='webgis-presentation_toc-basemap-item-label'></div>")
                .text(service.name)
                .appendTo($imgDiv);
            $item_li.click(function () {
                var service = this.service;
                service.map.setBasemap(service.id);
                $(this).parent().find('.webgis-presentation_toc-basemap-item-img').removeClass('selected');
                $(this).find('.webgis-presentation_toc-basemap-item-img').addClass('selected');
                if (webgis.useMobileCurrent()) {
                    service.map.events.fire('hidepresentations');
                }

                var $collapse = $(this).parent().find('.webgis-presentation_toc-basemap-collapse');
                if ($collapse.hasClass('expanded'))
                    $collapse.trigger('click');
            });
            item_li.resetPreview = function () {
                this.resetPreviewTimer.Start();
            };
            item_li.resetPreviewTimer = new webgis.timer(function (item) {
                var service = item.service;
                if (!$(item).hasClass('webgis-presentation_toc-hidden')) {
                    var img = $(item).find('.webgis-presentation_toc-basemap-item-img').each(function (i, e) {
                        var previewUrl = service.getPreviewUrl({ width: $(e).width(), height: $(e).height() });
                        $(e).css('background', 'url(' + previewUrl + ')');
                    });
                }
            }, 300, item_li);

            service.map.events.on('refresh', function (e) {
                var scale = Math.round(this.service.map.scale());
                if ($(this).data('last-refresh-scale') === scale)
                    return;

                $(this).data('last-refresh-scale', scale);
                this.resetPreview.apply(this);
            }, item_li);

            if (!$item_li.hasClass('webgis-presentation_toc-hidden')) {
                webgis.delayed(function () {
                    item_li.resetPreview.apply(item_li);
                }, 100, item_li);
            }
        }

        $ul.find('li.webgis-presentation_toc-basemap-item.webgis-presentation_toc-basemap-item-block').each(function (i, e) {
            if (i >= 3) {
                $(e).css('display','none');
            }
        });
        $ul.find('.webgis-presentation_toc-basemap-collapse').removeClass('expanded');
    };
    var removeService = function (e, service, parent) {
        if (service.isBasemap) {
            parent = parent || $('.webgis-presentation_toc-holder');
            var $elem = $(parent);
            $elem.find('.webgis-presentation_toc-basemap-item').each(function (i, e) {
                if (e.service && e.service.id == service.id) {
                    $(e).remove();
                }
            });
        }
        $.presentationToc.checkItemVisibility(service, parent);
    };

    var sortToc = function (parent) {
        var $parent = $(parent);
        $parent.find('ul').each(function (i, ul) {
            var $ul = $(ul);
            var items = $ul.children('li').get();
            items.sort(function (a, b) {
                var order1 = $(a).attr('data-order'), order2 = $(b).attr('data-order');
                if (!order1 && !order2) {
                    if (i == 0) // Top Level: Container
                        return $(a).text().toUpperCase().localeCompare($(b).text().toUpperCase());
                    return 0;
                }
                if (!order1)
                    return 1;
                if (!order2)
                    return -1;
                if (order1 == order2)
                    return 0;
                return parseInt(order1) < parseInt(order2) ? -1 : 1;
            });
            $.each(items, function (i, item) { $ul.append(item); });
        });
    };
    var isInContentSearchMode = function (sender, reset) {
        var $elem = sender ? $(sender).closest('.webgis-presentation_toc-holder') : $('.webgis-presentation_toc-holder');

        if ($elem.hasClass('webgis-content-search-area active')) {
            if (reset === true) {
                $elem.find('.webgis-presentation_toc-search').webgis_contentsearch('reset');
            }
            return true;
        }

        return false;
    }

    var addCustomContainer = function (parent, options) {
        var $elem = $(parent), $item_ul;
        var $ul = $elem.children('#webgis-presentation_toc-list'), $li = null, $item_ul = null, $item_li = null;
        var containerName = options.containerName || 'Custom Container';
        $ul.find('.webgis-presentation_toc-title-text').each(function (i, obj) {
            if ($(obj).text() == containerName) {
                $li = $(obj.parentNode);
            }
        });
        if ($li == null) {
            var id = $.presentationToc.sequence++;
            $li = $("<li></li>")
                .addClass("webgis-presentation_toc-title webgis-presentation_toc-collapsable webgis-presentation_toc-custom-container")
                .attr('data-container', '__custom_' + containerName)
                .attr("id", id);

            $li.attr('data-order', 0);
            $li.prependTo($ul);
            $("<span style='position:absolute' class='webgis-presentation_toc-plus webgis-api-icon webgis-api-icon-triangle-1-s'></span>")
                .appendTo($li)
                .click(function (event) {
                    event.stopPropagation();
                    $(this).parent().find('.webgis-presentation_toc-title-text').first().trigger('click');
                });

            $("<div class='webgis-presentation_toc-title-text'></div>")
                .text(containerName)
                .click(function (event) {
                    event.stopPropagation();

                    $li = $(this.parentNode);
                    
                    if ($(this.parentNode).hasClass('webgis-expanded')) {
                        $li.removeClass('webgis-expanded');

                        $li.children('.webgis-presentation_toc-content').slideUp()
                            .find('.webgis-presentation_toc-item')
                            .addClass('webgis-presentation_toc-hidden');

                        $li.children('.webgis-presentation_toc-plus')
                            .removeClass('webgis-api-icon-triangle-1-e')
                            .addClass('webgis-api-icon-triangle-1-s');
                    }
                    else {
                        $li.closest('ul')
                            .find('.webgis-expanded')
                            .removeClass('webgis-expanded')
                            .find('.webgis-presentation_toc-content').slideUp()
                            .find('.webgis-presentation_toc-item')
                            .addClass('webgis-presentation_toc-hidden');

                        $li.closest('ul')
                            .find('.webgis-presentation_toc-plus')
                            .removeClass('webgis-api-icon-triangle-1-e')
                            .addClass('webgis-api-icon-triangle-1-s');

                        $li.addClass('webgis-expanded');
                        $li.children('.webgis-presentation_toc-content').slideDown()
                            .find('.webgis-presentation_toc-item')
                            .removeClass('webgis-presentation_toc-hidden')
                            .each(function (i, e) {
                                if (e.resetPreview) {
                                    e.resetPreview.apply(e);
                                }
                            });
                        $li.children('.webgis-presentation_toc-plus').removeClass('webgis-api-icon-triangle-1-s').addClass('webgis-api-icon-triangle-1-e');
                    }
                })
                .appendTo($li);
            var $div = $("<div class='webgis-presentation_toc-content' style='display:none;white-space:normal;overflow:hidden' id='div_'></div>");
            $div.appendTo($li);
            $item_ul = $("<ul style='margin:8px 0px 0px 0px;'></ul>");
            $item_ul.appendTo($div);
        }
        else {
            $item_ul = $li.find('.webgis-presentation_toc-content').children('ul');
        }

        return $item_ul;
    };

    var _showMetadataLink = function ($sender) {
        var map = $sender.closest('.webgis-presentation_toc-holder').get(0)._map,
            url = webgis.tools.replaceCustomToolUrl(map, $sender.data('metadata')),
            target = $sender.data('metadata_target');

        switch (target) {
            case 'dialog':
                webgis.iFrameDialog(url, $sender.data('metadata_title') || "Metadaten");
                break;
            default:
                window.open(url);
                break;
        }
    }

    var _groupCheckBoxClicked = function (sender, e) {
        e.stopPropagation();

        var $icon = $(sender), $collapsable = $icon.closest('.webgis-presentation_toc-item-group');
        var $parent = $collapsable.closest('.webgis-presentation_toc-holder');
        var state = $collapsable.data('checkbox-state');

        var map = $parent.length === 1 ? $parent.get(0)._map : null;

        console.log(state);

        $collapsable.find('.webgis-presentation_toc-item').each(function (j, e) {
            var $e = $(e);

            if (map && $e.hasClass('checkbox') && e.presIds) {
                for (var i = 0; i < e.presIds.length; i++) {
                    var p = $.presentationToc.findItem(map, e.presIds[i]);
                    if (p && p.presentation && p.item && p.item.visible_with_service === true) {
                        var layerIds = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);

                        switch (state) {
                            case 'check0':
                            case 'check01':
                                p.presentation.service.setLayerVisibilityDelayed(layerIds, true);
                                break;
                            case 'check1':
                                p.presentation.service.setLayerVisibilityDelayed(layerIds, false);
                                break;
                        }
                    }
                }
            }
        });

        $.presentationToc.checkContainerVisibility($parent);
    };

    var _collapseAll = function (parent, options) {
        // Groups
        $(parent).find('.webgis-presentation_toc-item-group.webgis-presentation_toc-collapsable')
            .each(function (g, group) {
                var $group = $(group);
                var $ul = $group.children('ul');

                if ($ul.length === 1 && $ul.css('display') !== 'none') {
                    $group.children('.webgis-text-ellipsis-pro').trigger('click');
                }
            });

        // Containers
        $(parent).find('.webgis-presentation_toc-title.webgis-presentation_toc-collapsable') // containers
            .each(function (c, container) {
                var $container = $(container);
                if ($container.hasClass('webgis-expanded')) {
                    $container.children('.webgis-presentation_toc-title-text').trigger('click');
                }
            });
    };

})(webgis.$ || jQuery);

(function ($) {
    "use strict";
    $.fn.webgis_serviceOrder = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_serviceOrder');
        }
    };
    var defaults = {
        map: null,
        opacityValues: [0.1, 0.2, 0.25, 0.3, 0.4, 0.5, 0.6, 0.7, 0.75, 0.8, 0.9, 1]
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };

    var initUI = function (parent, options) {
        var $parent = $(parent), map = options.map, serviceIds = map.serviceIds();

        $("<div>")
            .addClass("webgis-info")
            .text(webgis.l10n.get("info-service-order"))
            .appendTo($parent);

        var $list = $("<ul>")
            .addClass("webgis-service-order-list")
            .appendTo($parent);

        // Get sortable Services
        var services = [];
        for (var s in serviceIds) {
            var service = map.getService(serviceIds[s]);

            if (!service)
                continue;

            if (webgis.leaflet.tilePane !== 'overlayPane' && service.type === 'tile')
                continue;

            if (service.isWatermark()) {
                continue;
            }

            services.push(service);
        }

        // Order services
        services.sort(function (s1, s2) {
            return s1.getOrder() - s2.getOrder();
        });
        services = services.reverse();

        for (var s in services) {
            var service = services[s];

            var $li = $("<li>")
                .addClass('webgis-service-order-item')
                .data('service', service)
                .addClass(service.type)
                .text(service.name)
                .appendTo($list);

            if (service.id === options.selected)
                $li.addClass('selected');

            var $opacityContainer = $("<div><span>" + webgis.l10n.get("opacity") + ":&nbsp;</span></div>")
                .addClass('opacity-container')
                .appendTo($li);
            var $opacity = $("<select></select>")
                .appendTo($opacityContainer)
                .change(function () {
                    var service = $(this).closest('.webgis-service-order-item').data('service');
                    service.setOpacity(parseFloat($(this).val()));
                });

            for (var o in options.opacityValues) {
                $("<option value='" + options.opacityValues[o].toString() + "'>" + Math.round(options.opacityValues[o] * 100) + "%</option>")
                    .appendTo($opacity)
            }

            var opacityValue = (Math.round(service.opacity * 100)/100.0).toString();
            $opacity.val(opacityValue);
        }

        webgis.require('sortable', function () {
            var editableList = Sortable.create($list.get(0),
                {
                    animation: 150,
                    ghostClass: 'webgis-sorting',
                    onSort: function (e) {
                        $list.find('.webgis-service-order-item')
                            .each(function (i, item) {
                                var service = $(item).data('service');
                                service.setOrder(serviceIds.length * 10 - i * 10);
                            });
                    }
                });
        });
    }; 
})(webgis.$ || jQuery);
