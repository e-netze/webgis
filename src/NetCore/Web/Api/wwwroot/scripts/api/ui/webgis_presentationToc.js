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
    let defaults = {
        items: null,
        map: null,
        gdi_button: false,
        basemap_rows: 1
    };
    let methods = {
        init: function (options) {
            let $this = $(this);
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
                let title = webgis.globals && webgis.globals.portal ? webgis.globals.portal.mapName : null;
                $.presentationToc.showLegend(
                    options.sender,
                    options.map,
                    options.map.services,
                    title || "Darstellung, ©, ...");
            }
        },
        get_expanded_names: function (options) {
            let collapsed = [];

            let findCollapsedGroupsRecursive = function ($parent, prefix) {
                $parent.children('.webgis-presentation_toc-item-group.webgis-presentation_toc-collapsable')
                    .each(function (g, group) {
                        let $group = $(group);
                        let $ul = $group.children('ul');
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
                    let $container = $(container);
                    if ($container.hasClass('webgis-expanded')) {
                        collapsed.push($container.attr("data-container"));
                    }
                    findCollapsedGroupsRecursive($container.children('.webgis-presentation_toc-content').children('ul'), $container.attr("data-container") + '|');
                });
            
            return collapsed;
        },
        expand: function (options) {
            let $this = $(this);

            if (options.exclusive === true) {
                _collapseAll(this, options);
            }

            webgis.delayed(function () {
                let getGroupRecurive = function ($parent, names) {
                    try {
                        let $group = $parent.children(".webgis-presentation_toc-item-group.webgis-presentation_toc-collapsable[data-ui-groupname='" + names[0] + "']")
                        if (names.length === 1)
                            return $group;

                        return getGroupRecurive($group.children('ul'), names.slice(1));
                    } catch (e) {
                        return null;
                    }
                }

                if (options.names) {
                    let containerNames = $.grep(options.names, function (n) { return n.split('|').length === 1 });
                    let groupNames = $.grep(options.names, function (n) { return n.split('|').length > 1 });

                    // first, expand groups
                    for (let n in groupNames) {
                        let nameParts = groupNames[n].split('|');

                        let $container = $this.find('.webgis-presentation_toc-title.webgis-presentation_toc-collapsable[data-container="' + webgis.encodeXPathString(nameParts[0]) + '"]');

                        let $group = getGroupRecurive(
                            $container.children('.webgis-presentation_toc-content').children('ul'),
                            nameParts.slice(1));

                        if ($group && $group.length === 1) {
                            let $ul = $group.children('ul');
                            if ($ul.length === 1) {
                                if ($ul.css('display') === 'none') {
                                    $group.children('.webgis-text-ellipsis-pro').trigger('click');
                                }
                            }
                        }
                    }

                    // then, expand contianers
                    for (let n in containerNames) {
                        let $container = $this.find('.webgis-presentation_toc-title.webgis-presentation_toc-collapsable[data-container="' + webgis.encodeXPathString(containerNames[n]) + '"]');
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
        },
        reorder: function (options) {
            sortToc($(this));
        }
    };
    $.presentationToc = {
        sequence: 0,
        process: function (sender, ids) {
            let $elem = sender ? $(sender).closest('.webgis-presentation_toc-holder') : $('.webgis-presentation_toc-holder');
            if ($elem.length === 0)
                return;

            let $search = $elem.find('.webgis-presentation_toc-search');
            if ($search.length > 0) {
                $search.webgis_contentsearch('reset');
            }

            let map = $elem.get(0)._map;
            if (map == null)
                return;
            for (let i = 0; i < ids.length; i++) {
                let p = $.presentationToc.findItem(map, ids[i]);
                if (p && p.presentation && p.item) {
                    let layerids = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                    if (p.item.style === 'button') {
                        p.presentation.service.setServiceVisibility(null); // alle unsichtbar schalten!!! und unten die einzelnen Layer einschalten. In Gruppen können mehrer Darstellungsvarianten aus dem gleichen Dienst vorkommen. Darum zuerst Sichtbarkeit aus und dann die einzellnen wieder einschalten.
                    }
                }
            }
            for (let i = 0; i < ids.length; i++) {
                let p = $.presentationToc.findItem(map, ids[i]);
                if (p && p.presentation && p.item) {
                    let layerids = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                    if (p.item.style === 'button') {
                        //p.presentation.service.setServiceVisibility(layerids);
                        p.presentation.service.setLayerVisibility(layerids, true); // Hier auch nur die einzelnen wieder einschalten, weil das Wegschalten schon oben erfolgt ist (siehe oben)
                    }
                    else if (p.item.style === 'checkbox') {
                        let status = p.presentation.service.checkLayerVisibility(layerids);
                        p.presentation.service.setLayerVisibilityDelayed(layerids, status != 1);
                    }
                    else if (p.item.style === 'optionbox') {
                        let status = p.presentation.service.checkLayerVisibility(layerids);
                        p.presentation.service.setLayerVisibilityDelayed(layerids, status != 1);

                        if (p.item.groupstyle === 'dropdown') {  // Wenn optionbox in einer Dropdown Gruppe ist -> alle anderen OptionBox in der Gruppe ausschalten
                            for (let s in map.services) {
                                let service = map.services[s];
                                if (!service || !service.presentations)
                                    continue;


                                for (let sp in service.presentations) {
                                    let presentation = service.presentations[sp];
                                    if (!presentation) continue;

                                    // don't uncheck the current presentation
                                    if (presentation.id === p.presentation.id) {                        // if presentation id is ident
                                        if (presentation.service && p.presentation.service) {           //    if service is implemented with presenteion
                                            if (presentation.service.id === p.presentation.service.id)  //       than continue only if its the same service
                                                continue;
                                        } else { continue; }                                            //    otherwise continue               
                                    }

                                    // uncheck if its an optionbox in the the same group
                                    for (let it in presentation.items) {
                                        let item = presentation.items[it];

                                        if (item.style === 'optionbox' &&
                                            item.groupstyle === 'dropdown' &&
                                            item.container === p.item.container &&
                                            item.name === p.item.name &&
                                            ((!item.ui_groupname && !p.item.ui_groupname) || (item.ui_groupname === p.item.ui_groupname))) {
                                            let optionLayerIds = service.getLayerIdsFromNames(presentation.layers);
                                            service.setLayerVisibilityDelayed(optionLayerIds, false);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (p.item.style === 'info') {
                        // open link in new tab
                        let url = p.item.metadata;
                        if (url && url.length > 0) {
                            if (url.indexOf('http') !== 0) {
                                url = webgis.globals.portal.baseUrl + url;
                            }
                            if (p.item.metadata_target === "dialog") {
                                webgis.iFrameDialog(url, p.item.metadata_title || p.item.name || "Metadaten")
                            } else {
                                window.open(url, '_blank');
                            }
                        }
                    }
                }
            }

            $.presentationToc.checkItemVisibility();
            $.presentationToc.checkContainerVisibility();
        },
        findItem: function (map, id) {
            for (let serviceId in map.services) {
                
                let service = map.services[serviceId];
                if (!service || !service.presentations)
                    continue;

                for (let p = 0; p < service.presentations.length; p++) {
                    let presentation = service.presentations[p];
                    if (!presentation.items)
                        continue;
                    for (let i = 0; i < presentation.items.length; i++) {
                        let item = presentation.items[i];
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
            let $elem = $(parent);
            if ($elem.length == 0)
                return;

            let map = $elem.get(0)._map;
            if (map == null)
                return;

            let serviceIds = map.serviceIds();
            // Find all items and check visibility
            $elem.find('.webgis-presentation_toc-item').each(function (i, e) {
                let $e = $(e);

                if ($e.hasClass('webgis-presentation_toc-basemap-item') ||
                    $e.hasClass('webgis-presentation_toc-dyncontent-item') ||
                    $e.hasClass('webgis-presentation_tol-custom-item')) {
                    $e.removeClass('webgis-display-none').addClass('webgis-display-block');
                    return;
                }

                let display = 'none';
                if ($e.hasClass('link-button')) {
                    if ($e.prev().hasClass('webgis-display-block')) {
                        display = 'block';
                    }
                } else {
                    if (e.ownerIds) {
                        for (let i = 0; i < e.ownerIds.length; i++) {
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

                let display = 'none';
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
                    for (let i = 0; i < e.presIds.length; i++) {
                        let p = $.presentationToc.findItem(map, e.presIds[i]);
                        if (p && p.presentation) {
                            let layerIds = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                            let status = p.presentation.service.checkLayerVisibility(layerIds);
                            let checktype = $(e).hasClass('optionbox') ? 'option' : 'check';
                            let img = webgis.css.imgResource(checktype + '0.png', 'toc');
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
            let $elem = $(parent);
            if ($elem.length === 0)
                return;

            let scale = /*map.scale()*/ map.crsScale();
            $elem.find('.webgis-scaledependent')
                .removeClass('webgis-outofscale').removeClass('webgis-inscale')
                .each(function (i, e) {

                    let $e = $(e);
                    
                    //if ($e.hasClass('webgis-presentation_toc-dyncontent-item')) {
                    //    // todo: query scale?
                    //    $e.removeClass('webgis-outofscale');
                    //} else {
                    if (e.presIds && e.presIds.length > 0) {
                        for (let i = 0, to = e.presIds.length; i < to; i++) {
                            let p = $.presentationToc.findItem(map, e.presIds[i]);
                            if (p && p.presentation) {

                                if ($e.hasClass('webgis-inscale')) {
                                    //console.log('already in scale', e);
                                    return; // already tested and in scale
                                }

                                let layerids = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                                let inScale = layerids.length == 0; // leere Darstellungsvarianten immer anzeigen -> sind meistens die "Dings Ausschalten" Buttons
                                for (let l in layerids) {
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
                    let $group = $(e);
                    let countChildren = $group.find('.webgis-presentation_toc-item').length;
                    let countOutOfScaleChildren = $group.find('.webgis-presentation_toc-item.webgis-outofscale').length;

                    //console.log($group.text(), countChildren, countOutOfScaleChildren);

                    if (countChildren > 0 && countChildren == countOutOfScaleChildren) {
                        $group.addClass('webgis-outofscale');
                    } else {
                        $group.removeClass('webgis-outofscale');
                    }
                });
        },
        checkContainerVisibility: function (parent) {
            let $parent = $(parent || $('.webgis-presentation_toc-holder'));
            if ($parent.length === 0 || $parent.get(0)._map == null)
                return;

            let map = $parent.get(0)._map;

            $parent.find('.webgis-presentation_toc-collapsable').each(function (i, collapsable) {
                let $collapsable = $(collapsable), foundVisibleLayers = false;

                if ($collapsable.hasClass('webgis-basemap-toc-container')) {
                    foundVisibleLayers = map.currentBasemapServiceId() || map.currentBasemapOverlayServiceIds().length > 0 ? true : false;
                } else if ($collapsable.hasClass('webgis-dynamiccontent-toc-container')) {
                    foundVisibleLayers = map.hasCurrentDynamicContent() && map.isCurrentDynamicContentFromTocTheme() === false;
                } else if ($collapsable.hasClass('webgis-presentation_toc-custom-container')) {
                    foundVisibleLayers = true;
                } else {
                    let containsOnlyCheckboxes = true, checkedItems = 0, uncheckedItems = 0;

                    $collapsable.find('.webgis-presentation_toc-item').each(function (j, e) {
                        let $e = $(e);

                        if (!$e.hasClass('checkbox') && !$e.hasClass('link-button')) { // link buttons are not realy an items
                            containsOnlyCheckboxes = false;
                        }

                        if (map.hasCurrentDynamicContent() && $e.hasClass('webgis-presentation_toc-dyncontent-item')) {
                            let dynamicContent = $e.data('dynamic-content');
                            if (dynamicContent && map.getCurrentDynamicContentId() == dynamicContent.id) {
                                foundVisibleLayers = true;
                            }
                        } else if (e.presIds) {
                            for (let i = 0; i < e.presIds.length; i++) {
                                let p = $.presentationToc.findItem(map, e.presIds[i]);
                                if (p && p.presentation && p.item && p.item.visible_with_service === true) {
                                    let layerIds = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                                    let status = p.presentation.service.checkLayerVisibility(layerIds);
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
                        let $checkboxIcon = $collapsable.children('div').children('.webgis-presentation_toc-item-group-checkbox');
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
            let serviceIds = [], reversedServices = [], hasLegend = false;
            for (let s in services) {
                if (services[s].isWatermark()) {
                    continue;
                }
                serviceIds.push(s);
            }
            serviceIds = serviceIds.reverse();
            for (let i in serviceIds) {
                reversedServices[serviceIds[i]] = services[serviceIds[i]];
                hasLegend |= services[serviceIds[i]].hasLegend();
            }
            services = reversedServices;

            webgis.delayed(function () {
                let onload = function ($content) {
                    $content.addClass('webgis-legend-pro-content');
                    // Tabs
                    let $tabsTable = $("<table>").css('width', '100%').addClass('webgis-modal-content-fill').appendTo($content);
                    let $tabsTableTr = $("<tr>").css('white-space', 'nowrap').appendTo($tabsTable);
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
                    let $mapProperties = $("<td></td>")
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
                        let $legendTabContent = $("<div class='webgis-tab-content webgis-legend-tab-content'>")
                            .css('display', 'none')
                            .attr('data-tab', 0)
                            .data('services', services)
                            .appendTo($content);

                        $legendTabContent.data('refresh', function ($tabContent) {
                            let services = $tabContent.empty().data('services'), count = 0;
                            for (let s in services) {
                                let service = services[s];
                                if (!service ||
                                    service.isBasemap === true ||
                                    service.hasLegend() === false ||
                                    service.hasVisibleLegendInScale() === false)
                                    continue;

                                let guid = webgis.guid();
                                count++;

                                $("<h2></h2>").text(service.name).appendTo($tabContent);

                                let $legendHolder = $("<div id='" + guid + "'></div>")
                                    .css('display', 'none')
                                    .appendTo($tabContent);

                                webgis.ajax({
                                    url: service.getLegendUrl(true) + "&requestid=" + guid,
                                    type: 'post',
                                    data: service.getLegendPostData(),
                                    success: function (result) {

                                        if (result && result.requestid && result.url) {
                                            let $legend = $('#' + result.requestid);
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
                    let $themesTabContent = $("<div class='webgis-tab-content webgis-themes-tab-content'>")
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
                                            let $ul = $(e);
                                            if ($ul.find('.webgis-search-content.active').length > 0) {
                                                let hasVisible = false;
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
                        let $elem = $(parent);
                        $elem.find('.webgis-services_toc-item').each(function (i, li) {
                            if (!li.layers || li.layers.length === 0)
                                return;

                            if (li.layers[0].visible) {
                                $(li).find('.webgis-service_toc-checkable-icon').attr('src', webgis.css.imgResource('check1.png', 'toc')).addClass('webgis-checked');
                            }
                            else {
                                $(li).find('.webgis-service_toc-checkable-icon').attr('src', webgis.css.imgResource('check0.png', 'toc')).removeClass('webgis-checked');
                            }

                            let inScale = false;
                            for (let l in li.layers) {
                                let scale = service.map.scale();
                                let layer = li.layers[l];
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
                                let service = toc.service;
                                $tabContent.data('checkItemVisibility')(service, $(toc));
                            });
                        });
                    });

                    for (let s in services) {
                        let service = services[s];
                        if (service.isBasemap === true || service.showInToc === false)
                            continue;

                        let $serviceContainer = $("<div>")
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
                        let $menu_container = $("<li class='webgis-presentation_toc-basemap-opacity'></li>")
                            .css({/* maxWidth: 320,*/ margin: '0px', display: showContextMenues === true ? 'block' : 'none' })
                            .appendTo($serviceContainer);

                        $("<div>")
                            .addClass('webgis-presentation_toc-menu-item')
                            .text(webgis.l10n.get("remove-service"))
                            .css('background-image', 'url(' + webgis.css.imgResource('remove.png', 'tools') + ')')
                            .data('serviceId', service.id)
                            .appendTo($menu_container)
                            .click(function () {
                                let $this = $(this);
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
                                let $this = $(this);
                                webgis.modalDialog(webgis.l10n.get("service-order"),
                                    function ($context) {
                                        $context.webgis_serviceOrder({ map: map, selected: $this.data('serviceId') });
                                    });
                            });

                        let $focusItem = $("<div>")
                            .addClass('webgis-presentation_toc-menu-item noicon nohover')
                            .addClass('webgis-presentation_toc-basemap-opacity-title')
                            .text(webgis.l10n.get("focus-service"))
                            .appendTo($menu_container);

                        $("<div>").appendTo($focusItem).webgis_focusservice_control({ service: service });

                        let $opacityItem = $("<div>")
                            .addClass('webgis-presentation_toc-menu-item noicon nohover')
                            .addClass('webgis-presentation_toc-basemap-opacity-title')
                            .text(webgis.l10n.get("opacity"))
                            .appendTo($menu_container);

                        $("<div>").appendTo($opacityItem).webgis_opacity_control({ service: service });

                        // Themes
                        let $toc_ul = $('<ul>')
                            .addClass('none')
                            .addClass('toc')
                            .appendTo($serviceContainer);
                        $toc_ul.get(0).service = service;

                        let scale = service.map.scale();
                        for (let l = 0, to = service.layers.length; l < to; l++) {
                            let layer = service.layers[l];
                            if (layer.locked || layer.tochidden)
                                continue;

                            let paths = (layer.undropdownable_groupname || layer.tocname || layer.name).split('\\');
                            let name = paths[paths.length - 1];

                            let $ul = $toc_ul;
                            if (paths.length > 1) {
                                for (let p = 0; p < paths.length - 1; p++) {
                                    let path = paths[p];
                                    let $path_ul = $ul.children("li[data-path='" + path + "']").children('ul');
                                    if ($path_ul.length === 0) {
                                        let $li = $("<li data-path='" + path + "'><i>" + path + "</i></li>")
                                            .addClass('webgis-services_toc-item group collapsed')
                                            .appendTo($ul)
                                            .click(function (e) {
                                                e.stopPropagation();
                                                let $this = $(this);
                                                $this.toggleClass('collapsed');
                                                $this.children('ul').css('display', $this.hasClass('collapsed') ? 'none' : 'block');
                                            });
                                        $ul = $("<ul>").addClass('none').css({ 'paddingLeft': 0, 'display':'none' }).appendTo($li);
                                    } else {
                                        $ul = $path_ul;
                                    }
                                }
                            }

                            let $toc_li = null;
                            // Es kann mehrere Themen mit gleichen Namen im TOC geben
                            // zB Wenn sich die Layer in einer nicht aufklappbaren Gruppe befinden (layer.undropdownable_groupname)
                            // => dann wird nur die Gruppe als Checkbox dargestellt und alle Layer darunter geschalten
                            $ul.children('li.webgis-services_toc-item.webgis-services_toc-checkable').each(function (i, li) {
                                let $li = $(li);
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
                                    let $ul = $(this).closest('.toc');

                                    let service = $ul.get(0).service;
                                    let layers = this.layers;

                                    if (service && layers) {
                                        let layerIds = $.map(layers, function (layer) { return layer.id });
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
                    let $descriptionTabContent = $("<div class='webgis-tab-content'>")
                        .css('display', 'none')
                        .attr('data-tab', 2)
                        .appendTo($content);

                    let descriptionCount = 0;

                    let mapDescription = map.getMapDescription(true);
                    if (mapDescription) {
                        descriptionCount++;

                        $("<p>")
                            .html(webgis.simpleMarkdown.render(mapDescription))
                            .appendTo($descriptionTabContent);
                    }

                    for (let s in services) {
                        let service = services[s];

                        if (service.metadata_link || service.servicedescription || service.copyrighttext || service.copyrightId) {
                            descriptionCount++;
                            $("<h2></h2>")
                                .text(service.name)
                                .appendTo($descriptionTabContent);

                            if (service.metadata_link) {
                                let $metadata = $("<div>")
                                    .addClass('webgis-service-metadata-link')
                                    .appendTo($descriptionTabContent);
                                $("<a>")
                                    .attr('href', service.metadata_link)
                                    .attr('target', '_blank')
                                    .text(webgis.l10n.get("metadata-link"))
                                    .appendTo($metadata);
                            }

                            if (service.servicedescription) {
                                let $descr = $("<div></div>")
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
                                let $cp = $("<div></div>")
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
                                let copyright = map.getCopyright(service.copyrightId);
                                if (copyright) {
                                    let $cp = $("<div></div>")
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

                            let $tabHeader = $(this);
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
            let $content = $(".webgis-legend-pro-content");
            if ($content.length > 0) {
                let $legendTabContent = $content.children(".webgis-tab-content.webgis-legend-tab-content");
                if ($legendTabContent.length > 0) {
                    //console.log('refresh legend');
                    $legendTabContent.data('refresh')($legendTabContent);
                }

                let $themesTabContent = $content.children('.webgis-tab-content.webgis-themes-tab-content');
                if ($themesTabContent.length > 0) {
                    $themesTabContent.data('refresh')($themesTabContent);
                }
            }
        },
        _calcDataOrderByServiceOrder: function(service) {
            return service
                ? 999999 - service.getOrder()
                : 0;
        }
    };

    let initUI = function (elem, options) {
        let $elem = $(elem);
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
        let $ul = $("<ul id='webgis-presentation_toc-list'></ul>");
        $ul.appendTo($elem);
        if (options.map != null) {
            if (options.map.dynamicContent && options.map.dynamicContent.length > 0) {
                for (let c in options.map.dynamicContent) {
                    let dynamicContent = options.map.dynamicContent[c];
                    addDynamicContent({}, dynamicContent, elem);
                }
            }
            for (let serviceId in options.map.services) {
                let service = options.map.services[serviceId];
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
                    let map = this.parentNode._map, $button = $(this);
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
                let $container = $(container);
                if (!$container.hasClass('webgis-toc-invisible-container')) {
                    let serviceGuids = $container.data('serviceGuids');

                    let hasLoadingsServices = false;
                    if (serviceGuids && serviceGuids.length > 0) {
                        for (let g in guids) {
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
                let $container = $(container);
                let serviceGuids = $container.data('serviceGuids');
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

    let addService = function (e, service, parent) {
        console.log('addService', service.name);
        parent = parent || this;
        if (service == null || service.map == null)
            return;

        if (service.isBasemap) {
            return addBasemap(e, service, parent);
        }
        if (service.presentations == null || service.presentations.length == 0)
            return;

        const $elem = $(parent); // $('.webgis-presentation_toc-holder');
        const $ul = $elem.children('#webgis-presentation_toc-list');

        for (let p = 0; p < service.presentations.length; p++) {
            let id = $.presentationToc.sequence++;
            let presentation = service.presentations[p];

            // zum Debuggen für bestimmte Darstellungsvariante
            //if (presentation.name == "Gewässermorphologie") {  
            //    debugger;
            //}

            // To find this presentation after click...
            presentation.sequenceId = id;
            for (let i = 0; i < presentation.items.length; i++) {
                //console.log(presentation.items[i]);
                let prop = presentation.items[i];
                let pid = $.presentationToc.sequence++;
                prop.id = pid;
                let $li = null, $item_ul = null;

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

                    if (webgis.usability.orderPresentationTocContainsByServiceOrder === true) {
                        console.log('data-order', service.name, service.getOrder())
                        $li.attr('data-order', $.presentationToc._calcDataOrderByServiceOrder(service));
                    }
                    else if (prop.container_order) {
                        $li.attr('data-order', prop.container_order);
                    } // otherwise dont set data-order => containers will ordered aphabetic by name

                    $li.data('serviceGuids', []).appendTo($ul);

                    const li = $li.get(0);
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

                    const collectServices = function ($this) {
                        let $holder = $this.closest('.webgis-presentation_toc-holder');
                        if ($holder.length == 0)
                            return null;
                        let map = $holder.get(0)._map;
                        // Collect Services
                        let services = null;
                        $this.closest('.webgis-presentation_toc-title').find('.webgis-presentation_toc-item').each(function (i, e) {
                            if (e.presIds) {
                                for (let p in e.presIds) {
                                    let item = $.presentationToc.findItem(map, e.presIds[p]);
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
                            let serviceCollection = collectServices($(this));
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
                            let serviceCollection = collectServices($(this));
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

                    const $div = $("<div class='webgis-presentation_toc-content' style='display:none' id='div_'></div>");

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

                let $parentUl = null, $item_li = null, item_li, itemname = prop.name || presentation.name, $group_li = null;
                let isDropdownGroup = (prop.groupstyle == 'dropdown' && prop.name);
                if (isDropdownGroup) {
                    $item_ul.find("li.webgis-presentation_toc-item-group.webgis-presentation_toc-collapsable").each(function (i, li) {  // .webgis-presentation_toc-item-group.webgis-presentation_toc-collapsable => nur dropdown gruppen suchen, sonst kann es zu problemen kommen, wenn es Darstellungsvarianten mit dem gleichen Theme gibt
                        if (li.groupname && li.groupname == prop.name) {
                            $group_li = $(li);
                            $parentUl = $group_li.children("ul");
                        }
                    });

                    let clickGroupFunction = function () {
                        let $this = $(this);

                        if (isInContentSearchMode(this, true))
                            $this.children('.webgis-api-icon-triangle-1-e').removeClass('webgis-api-icon-triangle-1-e').addClass('webgis-api-icon-triangle-1-s');

                        if ($this.children('.webgis-api-icon-triangle-1-e').length > 0) {
                            $this.children('.webgis-api-icon-triangle-1-e').removeClass('webgis-api-icon-triangle-1-e').addClass('webgis-api-icon-triangle-1-s');
                            $this.parent().children('ul').slideUp();
                        }
                        else {
                            $this.children('.webgis-api-icon-triangle-1-s').removeClass('webgis-api-icon-triangle-1-s').addClass('webgis-api-icon-triangle-1-e');
                            $this.parent().children('ul').slideDown();

                            let $container = $this.closest('.webgis-search-content-container');
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
                        let $title = $("<div class='webgis-text-ellipsis-pro check-for-title'></div>").appendTo($group_li)
                            .click(clickGroupFunction);
                        $("<span style='position:absolute' class='webgis-api-icon webgis-api-icon-triangle-1-s'></span>").appendTo($title);
                        $("<span class='webgis-presentation_toc-item-group-checkbox'><span>").appendTo($title).click(function (e) { _groupCheckBoxClicked(this, e); });
                        $("<span style='margin-left:14px' class='webgis-search-content'>&nbsp;" + webgis.encodeHtmlString(prop.name) + "</span>").appendTo($title);

                        $parentUl = $("<ul style='display:none'></ul>").appendTo($group_li);
                    }
                    itemname = presentation.name;

                    if (prop.ui_groupname) {
                        let ui_groupnameParts = prop.ui_groupname.replaceAll("\\/", "&sol;").split('/'); // slash (&sol;) can be encoded in CMS als \/
                        for (let gn in ui_groupnameParts) {
                            let ui_groupname = ui_groupnameParts[gn].replaceAll("&sol;", "/");  // encode back => &sol; => /

                            //console.log(ui_groupname);

                            let $ui_group = $parentUl.children("li[data-ui-groupname='" + ui_groupname + "']");
                            if ($ui_group.length === 0) {
                                $ui_group = $("<li></li>")
                                    .addClass('webgis-presentation_toc-item-group webgis-presentation_toc-collapsable')
                                    .data('serviceGuids', [])
                                    .attr('data-order', prop.item_order)
                                    .attr('data-ui-groupname', ui_groupname)
                                    .appendTo($parentUl);

                                let $ui_title = $("<div class='webgis-text-ellipsis-pro check-for-title'></div>")
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

                let hasMetadata_iButton =
                    prop.style !== 'info' &&
                    (prop.group_metadata || prop.metadata) &&
                    prop.metadata_button_style == 'i_button';

                if (prop.group_metadata && $group_li != null
                    && $group_li.find('.webgis-api-icon.webgis-api-icon-info').length === 0) {
                    $("<span style='position:absolute;left:5px;' class='webgis-api-icon webgis-api-icon-info'></span>")
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
                    let isGroup = false; // Experimental: Richtige id für parametrierten Aufruf finden. Gruppen haben das Kürzel dvg_ -> sonst presentation.id...
                    if (presentation.items) {
                        for (let pitem in presentation.items) {
                            if (presentation.items[pitem].name) {
                                isGroup = true;
                                break;
                            }
                        }
                    }
                    //console.log(presentation.id, isGroup);
                    let dvid = $parentUl.closest('.webgis-presentation_toc-item-group').attr('data-dvid');
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
                        let chk = "<img class='webgis-presentation_toc-checkable-icon' src=" + webgis.css.imgResource("check0.png", "toc") + ">";
                        $("<span class='webgis-search-content'>" + chk + "&nbsp;<span class='webgis-text-span nowrap'>" + webgis.encodeHtmlString(itemname) + "</span></span>").appendTo($item_li);
                        $item_li.click(function () {
                            $.presentationToc.process(this, this.presIds, true);
                            //sendCmd('button', '5D3A7A26-2953-45b6-A1CD-40B75B669BFA|' + (this.groupid ? this.groupid : this.pid) + '|' + ($D('_img' + this.pid).src.indexOf('1.png') != -1 ? 'false' : 'true'), 'get');
                        });
                    }
                    else if (prop.style === 'optionbox') {
                        //console.log('add option box');
                        $item_li.addClass('webgis-presentation_toc-checkable optionbox');
                        let opt = "<img class='webgis-presentation_toc-checkable-icon' src=" + webgis.css.imgResource("option0.png", "toc") + ">";
                        $("<span class='webgis-search-content'>" + opt + "&nbsp;<span class='webgis-text-span nowrap'>" + webgis.encodeHtmlString(itemname) + "<span></span>").appendTo($item_li);
                        $item_li.click(function () {
                            $.presentationToc.process(this, this.presIds, true);
                        });
                    }
                    else if (prop.style === 'dynamiccontentmarker') {
                        let query = service.getLayerIdsFromNames(presentation.layers).length === 1 ? service.getQueryByLayerId(service.getLayerIdsFromNames(presentation.layers)[0]) : null;
                        if (query) {
                            let contentId = webgis.guid();
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
                                    let map = $(this).data('map');
                                    let dynamicContent = $(this).data('dynamic-content');
                                    if ($(this).hasClass('webgis-presentation_toc-dyncontent-item-selected')) {
                                        let hasHistory = $.webgis_search_result_histories[map] != null && $.webgis_search_result_histories[map].count() > 0
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
                        let btn = "<img class='toc-icon' src=" + webgis.css.imgResource(prop.style === "info" ? "info-26.png" :  "layers-16.png", "toc") + ">";
                        $("<span class='webgis-search-content'>" + btn + "&nbsp;<span class='webgis-text-span nowrap'>" + webgis.encodeHtmlString(itemname) + "<span></span>").appendTo($item_li);
                        $item_li.click(function () {
                            let c = $.presentationToc.process(this, this.presIds);
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
                    for (let vis_i in prop.visible_with_one_of_services) {
                        item_li.ownerIds.push(prop.visible_with_one_of_services[vis_i]);
                    }
                }

                let metadata_prefix = prop.groupstyle && prop.groupstyle !== 'dropdown'
                    ? 'group_'
                    : '';
                
                if (prop.style !== 'info'
                    && prop[metadata_prefix + 'metadata']
                    && $item_li.find('.webgis-api-icon.webgis-api-icon-info').length === 0
                    && $item_li.next('.webgis-presentation_toc-item.link-button').length === 0) {
                    let $metadataButton = $("<span></span>")
                        .data('metadata', prop[metadata_prefix + 'metadata'])
                        .data('metadata_target', prop[metadata_prefix + 'metadata_target'])
                        .data('metadata_title', prop[metadata_prefix + 'metadata_title']);
                        
                        
                    switch (prop[metadata_prefix + 'metadata_button_style']) {
                        case 'link_button':
                            let $li = $("<li>")
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
                                .css({ position: 'absolute', left: '26px', 'marginTop': '2px' })
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

        service.events.on(['onchangevisibility', 'onchangevisibility-delayed'],
            function () {
                $.presentationToc.checkItemVisibility();  // no parameter here
                $.presentationToc.checkContainerVisibility();
            },
            parent);
        sortToc(parent);

        webgis.addTitleToEllipsis();
    };
    let addDynamicContent = function (e, content, parent) {
        //console.log('addDynamicContent');
        //console.log(content);
        parent = parent || this;
        if (content == null || content.map == null)
            return;

        if ($(content.map._webgisContainer).find("#" + content.id + ".webgis-presentation_toc-dyncontent-item").length > 0)  // already exists (eg anywhere in TOC...)
            return;

        let $elem = $(parent);
        let $ul = $elem.children('#webgis-presentation_toc-list'), $li = null, $item_ul = null, $item_li = null;
        let containerName = webgis.l10n.get("dynamic-content");
        $ul.find('.webgis-presentation_toc-title-text').each(function (i, obj) {
            if ($(obj).text() == containerName) {
                $li = $(obj.parentNode);
            }
        });
        if ($li == null) {
            let id = $.presentationToc.sequence++;
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
                                let resetPreview = $(e).data('resetPreview');
                                if (resetPreview) {
                                    resetPreview.apply($(e));
                                }
                            });
                        $li.children('.webgis-presentation_toc-plus')
                            .removeClass('webgis-api-icon-triangle-1-s')
                            .addClass('webgis-api-icon-triangle-1-e');
                    }
                })
                .appendTo($li);

            let $div = $("<div class='webgis-presentation_toc-content' style='display:none;white-space:normal;overflow:hidden' id='div_'></div>");
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
                let c = $(this).data('dynamic-content');
                if ($(this).hasClass('webgis-presentation_toc-dyncontent-item-selected')) {

                    let $holder = $(c.map.ui.webgisContainer()).find('.webgis-search-result-list').closest('.webgis-search-result-holder'),
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


        let img = content.map.ui.dynamicContentIcon(content);

        $("<span class='webgis-search-content'><img class='toc-icon' src=" + img + ">&nbsp;" + webgis.encodeHtmlString(content.name) + "</span>").appendTo($item_li);
    };
    let addBasemap = function (e, service, parent) {
        let $elem = $(parent);
        let $ul = $elem.children('#webgis-presentation_toc-list'), $li = null, $item_ul = null, $item_li = null;
        let containerName = webgis.l10n.get("basemaps");
        $ul.find('.webgis-presentation_toc-title-text').each(function (i, obj) {
            if ($(obj).text() == containerName) {
                $li = $(obj.parentNode);
            }
        });
        if ($li == null) {
            let id = $.presentationToc.sequence++, map = service.map;

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
                                let resetPreview = $(e).data('resetPreview');
                                if (resetPreview) {
                                    resetPreview.apply($(e));
                                }
                            });
                        $li.children('.webgis-presentation_toc-plus').removeClass('webgis-api-icon-triangle-1-s').addClass('webgis-api-icon-triangle-1-e');
                    }
                })
                .appendTo($li);

            let $div = $("<div class='webgis-presentation_toc-content' style='display:none;white-space:normal;overflow:hidden;margin-top:5px' id='div_'></div>");
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
                .text(webgis.l10n.get("no-basemap"))
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
            let $collapse_container = $("<li class='webgis-presentation_toc-basemap-collapse'></li>")
                .appendTo($item_ul)
                .click(function () {
                    let $this = $(this);
                    $this.toggleClass('expanded');

                    $ul.find('li.webgis-presentation_toc-basemap-item').each(function (i, e) {
                        if (i >= 3 && !$this.hasClass('expanded')) {
                            $(e).css('display', 'none');
                        } else {
                            $(e).css('display', '');
                        }
                    });
                });

            let $opacity_container = $("<li>")
                .addClass('webgis-presentation_toc-basemap-opacity')
                .appendTo($item_ul);
            $("<div>")
                .addClass('label')
                .text(webgis.l10n.get("opacity")+":")
                .appendTo($opacity_container);
            $("<div>")
                .addClass('buttons')
                .appendTo($opacity_container).webgis_opacity_control({
                    service: function () {  // should be a function, because it changes alway after a basemap is selected
                        return map.getService(map.currentBasemapServiceId()) ||
                            (
                                map.currentBasemapOverlayServiceIds().length > 0
                                    ? map.getService(map.currentBasemapOverlayServiceIds()[0])
                                    : null
                            )
                    }
                });

            service.map.events.on('basemap_changed', function (e, sender, basemapService) {
                $(this).webgis_opacity_control('refresh');
                sender.ui.refreshUIElements();
            }, $opacity_container);
            service.map.events.on('basemap_changed', function (e, sender, basemapService) {
                $(this)
                    .find('.webgis-presentation_toc-basemap-item')
                    .each(function (i, e) {
                        if (basemapService && basemapService.id === $(e).attr('id')) {
                            $(e).children('.webgis-presentation_toc-basemap-item-img').addClass('selected');
                        } else {
                            $(e).children('.webgis-presentation_toc-basemap-item-img').removeClass('selected');
                        }
                    });

                $(this)
                    .find('.webgis-presentation_toc-basemap-overlay')
                    .each(function (i, e) {
                        $(e).find('img')
                            .attr('src', webgis.css.imgResource(sender.currentBasemapOverlayServiceIds().includes($(e).attr('id'))
                                ? "check1.png"
                                : "check0.png", "toc"));
                    });

                $.presentationToc.checkContainerVisibility();
            }, $li);
        }
        else {
            $item_ul = $li.find('.webgis-presentation_toc-content').children('ul');
        }
        if (service.basemapType === 'overlay') {
            $item_li = $("<li style='width:100%;height:20px;display:none'></li>")
                .attr("id", service.id)
                .data("service", service)
                .addClass("webgis-presentation_toc-item webgis-presentation_toc-basemap-item webgis-presentation_toc-basemap-overlay")
                .insertBefore($item_ul.find('.webgis-presentation_toc-basemap-collapse'));

            let isChecked = service.map.currentBasemapOverlayServiceIds().includes(service.id);
            let chk = "<img class='toc-icon' src=" + webgis.css.imgResource(isChecked ? "check1.png" : "check0.png", "toc") + ">";
            $("<span>" + chk + "&nbsp;" + webgis.encodeHtmlString(service.name) + "</span>").appendTo($item_li);

            $item_li.click(function () {
                const service = $(this).data("service");
                service.map.setBasemap(service.id, true);
            });
        }
        else {
            $item_li = $("<li>")
                .attr("id", service.id)
                .data("service", service)
                .addClass("webgis-presentation_toc-item webgis-presentation_toc-basemap-item webgis-presentation_toc-basemap-item-block webgis-presentation_toc-hidden")
                .insertBefore($item_ul.find('.webgis-no-basemap'));

            if ($item_ul.closest('.webgis-presentation_toc-collapsable').hasClass('webgis-expanded')) {
                $item_li.removeClass('webgis-presentation_toc-hidden');
            }

            let $imgDiv = $("<div class='webgis-presentation_toc-basemap-item-img" + (service.map.currentBasemapServiceId() == service.id ? " selected" : "") + "'></div>").appendTo($item_li);
            $("<div class='webgis-presentation_toc-basemap-item-label'></div>")
                .text(service.name)
                .appendTo($imgDiv);

            $item_li.click(function () {
                const service = $(this).data("service");
                service.map.setBasemap(service.id);
                $(this).parent().find('.webgis-presentation_toc-basemap-item-img').removeClass('selected');
                $(this).find('.webgis-presentation_toc-basemap-item-img').addClass('selected');
                if (webgis.useMobileCurrent()) {
                    service.map.events.fire('hidepresentations');
                }

                let $collapse = $(this).parent().find('.webgis-presentation_toc-basemap-collapse');
                if ($collapse.hasClass('expanded'))
                    $collapse.trigger('click');
            });
            $item_li.data("resetPreview", function () {
                this.data("resetPreviewTimer").Start();
            });
            $item_li.data("resetPreviewTimer", new webgis.timer(function ($item) {
                let service = $item.data("service");
                if (!$item.hasClass('webgis-presentation_toc-hidden')) {
                    $item.find('.webgis-presentation_toc-basemap-item-img').each(function (i, e) {
                        let previewUrl = service.getPreviewUrl({ width: $(e).width(), height: $(e).height() });
                        $(e).css('background', 'url(' + previewUrl + ')');
                    });
                }
            }, 300, $item_li));

            service.map.events.on('refresh', function (e) {
                let service = this.data("service");
                let scale = Math.round(service.map.scale());
                if (this.data('last-refresh-scale') === scale)
                    return;

                $(this).data('last-refresh-scale', scale);
                this.data("resetPreview").apply(this);
            }, $item_li);

            if (!$item_li.hasClass('webgis-presentation_toc-hidden')) {
                webgis.delayed(function ($item) {
                    $item.data("resetPreview").apply($item);
                }, 100, $item_li);
            }
        }

        $ul.find('li.webgis-presentation_toc-basemap-item.webgis-presentation_toc-basemap-item-block').each(function (i, e) {
            if (i >= 3) {
                $(e).css('display','none');
            }
        });
        $ul.find('.webgis-presentation_toc-basemap-collapse').removeClass('expanded');
    };
    let removeService = function (e, service, parent) {
        if (service.isBasemap) {
            parent = parent || $('.webgis-presentation_toc-holder');
            let $elem = $(parent);
            $elem.find('.webgis-presentation_toc-basemap-item').each(function (i, e) {
                if (e.service && e.service.id == service.id) {
                    $(e).remove();
                }
            });
        }
        $.presentationToc.checkItemVisibility(null, service, parent);
    };

    let sortToc = function (parent) {
        let $parent = $(parent);
        $parent.find('ul').each(function (i, ul) {
            let $ul = $(ul);
            let items = $ul.children('li').get();
            items.sort(function (a, b) {
                let order1 = $(a).attr('data-order'), order2 = $(b).attr('data-order');
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
    let isInContentSearchMode = function (sender, reset) {
        let $elem = sender ? $(sender).closest('.webgis-presentation_toc-holder') : $('.webgis-presentation_toc-holder');

        if ($elem.hasClass('webgis-content-search-area active')) {
            if (reset === true) {
                $elem.find('.webgis-presentation_toc-search').webgis_contentsearch('reset');
            }
            return true;
        }

        return false;
    }

    let addCustomContainer = function (parent, options) {
        let $elem = $(parent);
        let $ul = $elem.children('#webgis-presentation_toc-list'), $li = null, $item_ul = null, $item_li = null;
        let containerName = options.containerName || 'Custom Container';
        $ul.find('.webgis-presentation_toc-title-text').each(function (i, obj) {
            if ($(obj).text() == containerName) {
                $li = $(obj.parentNode);
            }
        });
        if ($li == null) {
            let id = $.presentationToc.sequence++;
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
                                let resetPreview = $(e).data('resetPreview');
                                if (resetPreview) {
                                    resetPreview.apply($(e));
                                }
                            });
                        $li.children('.webgis-presentation_toc-plus').removeClass('webgis-api-icon-triangle-1-s').addClass('webgis-api-icon-triangle-1-e');
                    }
                })
                .appendTo($li);
            let $div = $("<div class='webgis-presentation_toc-content' style='display:none;white-space:normal;overflow:hidden' id='div_'></div>");
            $div.appendTo($li);
            $item_ul = $("<ul style='margin:8px 0px 0px 0px;'></ul>");
            $item_ul.appendTo($div);
        }
        else {
            $item_ul = $li.find('.webgis-presentation_toc-content').children('ul');
        }

        return $item_ul;
    };

    let _showMetadataLink = function ($sender) {
        let map = $sender.closest('.webgis-presentation_toc-holder').get(0)._map,
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

    let _groupCheckBoxClicked = function (sender, e) {
        e.stopPropagation();

        let $icon = $(sender), $collapsable = $icon.closest('.webgis-presentation_toc-item-group');
        let $parent = $collapsable.closest('.webgis-presentation_toc-holder');
        let state = $collapsable.data('checkbox-state');

        let map = $parent.length === 1 ? $parent.get(0)._map : null;

        console.log(state);

        $collapsable.find('.webgis-presentation_toc-item').each(function (j, e) {
            let $e = $(e);

            if (map && $e.hasClass('checkbox') && e.presIds) {
                for (let i = 0; i < e.presIds.length; i++) {
                    let p = $.presentationToc.findItem(map, e.presIds[i]);
                    if (p && p.presentation && p.item && p.item.visible_with_service === true) {
                        let layerIds = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);

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

    let _collapseAll = function (parent, options) {
        // Groups
        $(parent).find('.webgis-presentation_toc-item-group.webgis-presentation_toc-collapsable')
            .each(function (g, group) {
                let $group = $(group);
                let $ul = $group.children('ul');

                if ($ul.length === 1 && $ul.css('display') !== 'none') {
                    $group.children('.webgis-text-ellipsis-pro').trigger('click');
                }
            });

        // Containers
        $(parent).find('.webgis-presentation_toc-title.webgis-presentation_toc-collapsable') // containers
            .each(function (c, container) {
                let $container = $(container);
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
    let defaults = {
        map: null,
        opacityValues: [0.1, 0.2, 0.25, 0.3, 0.4, 0.5, 0.6, 0.7, 0.75, 0.8, 0.9, 1]
    };
    let methods = {
        init: function (options) {
            let $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };

    let initUI = function (parent, options) {
        let $parent = $(parent);
        let map = options.map;
        let serviceIds = map.serviceIds();

        $("<div>")
            .addClass("webgis-info")
            .text(webgis.l10n.get("info-service-order"))
            .appendTo($parent);

        let $list = $("<ul>")
            .addClass("webgis-service-order-list")
            .appendTo($parent);

        // Get sortable Services
        let services = [];
        for (let s in serviceIds) {
            let service = map.getService(serviceIds[s]);

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

        for (let s in services) {
            let service = services[s];

            let $li = $("<li>")
                .addClass('webgis-service-order-item')
                .data('service', service)
                .addClass(service.type)
                .text(service.name)
                .appendTo($list);

            if (service.id === options.selected)
                $li.addClass('selected');

            let $opacityContainer = $("<div><span>" + webgis.l10n.get("opacity") + ":&nbsp;</span></div>")
                .addClass('opacity-container')
                .appendTo($li);
            let $opacity = $("<select></select>")
                .appendTo($opacityContainer)
                .change(function () {
                    let service = $(this).closest('.webgis-service-order-item').data('service');
                    service.setOpacity(parseFloat($(this).val()));
                });

            for (let o in options.opacityValues) {
                $("<option value='" + options.opacityValues[o].toString() + "'>" + Math.round(options.opacityValues[o] * 100) + "%</option>")
                    .appendTo($opacity)
            }

            let opacityValue = (Math.round(service.opacity * 100)/100.0).toString();
            $opacity.val(opacityValue);
        }

        webgis.require('sortable', function () {
            let editableList = Sortable.create($list.get(0),
                {
                    animation: 150,
                    ghostClass: 'webgis-sorting',
                    onSort: function (e) {
                        const $toc = $(".webgis-presentation_toc-holder");

                        $list.find('.webgis-service-order-item')
                            .each(function (i, item) {
                                let service = $(item).data('service');
                                service.setOrder(serviceIds.length * 10 - i * 10);

                                if (webgis.usability.orderPresentationTocContainsByServiceOrder === true) {
                                    $toc.find('.webgis-presentation_toc-title').each(function (i, container) {
                                        let $container = $(container);

                                        console.log($container.attr("data-container"), $container.data("serviceGuids"));
                                        const containerServiceGuids = $container.data("serviceGuids")
                                        if (containerServiceGuids && containerServiceGuids.includes(service.guid)) {

                                            //console.log('reset container order', $container.attr("data-container"), $.presentationToc._calcDataOrderByServiceOrder(service));
                                            $container.attr("data-order", $.presentationToc._calcDataOrderByServiceOrder(service));
                                        }
                                    });
                                }
                            });

                        if (webgis.usability.orderPresentationTocContainsByServiceOrder === true) {
                            $toc.webgis_presentationToc('reorder');
                        }
                    }
                });
        });
    }; 
})(webgis.$ || jQuery);
