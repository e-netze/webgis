// WebGIS UI Plugins (presentation toc)
// webgis_presentationToc, webgis_serviceOrder
// Presentation TOC of the viewer (containers, groups and presentation items of the map services,
// basemaps, dynamic content, custom containers, legend/copyright dialog) and the service order list
// (sort services and set their opacity, used in a modal dialog).
// The TOC helpers are static methods ($.fn.webgis_presentationToc.plugin.<name>, inside this file only;
// from outside: webgis.ui.showServicesLegend); the legacy global
// $.presentationToc (process, findItem, checkItemVisibility, showLegend, ..., sequence) is kept for
// existing callers and custom code. $.presentationToc.sequence is the global item/container id counter.

webgis.ui.definePlugin('webgis_presentationToc', {
    defaults: {
        items: null,
        map: null,
        gdi_button: false,
        basemap_rows: 1
    },
    init: function () {
        const $ = this.$, Ctor = this.constructor, options = this.options;
        const elem = this.$el.get(0), $elem = this.$el;

        // webgis events (map, services) subscribed by this instance, released in destroy
        this._subscriptions = [];

        elem._map = options.map;
        $elem.addClass('webgis-presentation_toc-holder');
        // no browser context menu in the whole toc (items and groups show their own menu)
        $elem.on('contextmenu.webgis_presentation_toc', function (e) {
            if ($(e.target).closest('input, textarea').length === 0) {
                e.preventDefault();
            }
        });
        if (webgis.usability.presentationTocSearch === true && $.fn.webgis_contentsearch) {
            $("<div></div>")
                .addClass('webgis-presentation_toc-search')
                .appendTo($elem)
                .webgis_contentsearch({
                    placeholder: webgis.l10n.get('filter-contents'),
                    container_selectors: ["li", "ul", ".webgis-presentation_toc-content", ".webgis-search-content-container"]
                });
        }
        let $ul = $("<ul id='webgis-presentation_toc-list'></ul>");
        $ul.appendTo($elem);
        if (options.map != null) {
            if (options.map.dynamicContent && options.map.dynamicContent.length > 0) {
                for (let c in options.map.dynamicContent) {
                    let dynamicContent = options.map.dynamicContent[c];
                    Ctor.addDynamicContent({}, dynamicContent, elem);
                }
            }
            for (let serviceId in options.map.services) {
                let service = options.map.services[serviceId];
                Ctor.addService({}, service, elem);
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
                .on('click.webgis_presentation_toc', function () {
                    let map = this.parentNode._map, $button = $(this);
                    webgis.ui.showModal({
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

        if (!elem._map) {
            return;
        }

        // per-instance handlers (this = elem, as before): events.off(channel, fn) must not
        // unsubscribe other TOC instances
        const mapEvents = elem._map.events;
        this._subscribe(mapEvents, 'onaddservice', function () { return Ctor.addService.apply(this, arguments); }, elem);
        this._subscribe(mapEvents, 'onremoveservice', function () { return Ctor.removeService.apply(this, arguments); }, elem);
        this._subscribe(mapEvents, 'onadddynamiccontent', function () { return Ctor.addDynamicContent.apply(this, arguments); }, elem);
        this._subscribe(mapEvents, 'refresh', function () { return Ctor.checkItemScaleVisibility.apply(this, arguments); }, elem);
        this._subscribe(mapEvents, 'refresh', function () { return Ctor.refreshLegend.apply(this, arguments); }, elem);
        this._subscribe(mapEvents, 'onbuildtoolui', function () { return Ctor.hideLegend.apply(this, arguments); }, elem);
        this._subscribe(mapEvents, 'showhourglass_guids', function (e, sender, guids) {
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
        this._subscribe(mapEvents, 'hidehourglass_guids', function (e, sender) {
            $elem.find('.webgis-presentation_toc-title, .webgis-presentation_toc-item-group').each(function (i, container) {
                let $container = $(container);
                let serviceGuids = $container.data('serviceGuids');
                if (serviceGuids) {
                    $container.removeClass('loading');
                }
            });
        });
        this._subscribe(mapEvents, 'refresh-toc-checkboxes', function (channel, sender, args) {
            //console.log('refresh-toc-checkboxes', channel, sender, args, this);
            Ctor.checkItemVisibility(channel, sender, args);
            Ctor.checkContainerVisibility(this);
        }, elem);
        const onBadgesChanged = function () {
            Ctor.updateItemBadges(elem, elem._map);
        };
        this._subscribe(mapEvents, 'service-layerrenderer-changed', onBadgesChanged, elem);
        this._subscribe(mapEvents, 'visfilters-changed', onBadgesChanged, elem);
    },
    destroy: function () {
        const subscriptions = this._subscriptions || [];
        for (const s of subscriptions) {
            try { s.events.off(s.channel, s.fn); } catch (e) { }
        }
        this._subscriptions = null;

        this.$el.find('*').addBack().off('.webgis_presentation_toc');
        this.$el.empty().removeClass('webgis-presentation_toc-holder');

        const elem = this.$el.get(0);
        if (elem) {
            elem._map = null;
        }
    },
    methods: {
        _subscribe: function (events, channel, fn, context) {
            events.on(channel, fn, context);
            if (this._subscriptions) {
                this._subscriptions.push({ events: events, channel: channel, fn: fn });
            }
        },
        show_first: function (options) {
            if (this.$el.find('.webgis-presentation_toc-collapsable.webgis-expanded').length == 0) {
                this.$el.find('.webgis-presentation_toc-collapsable:first').find('.webgis-presentation_toc-title-text').trigger('click');
            }
        },
        show_services_legend: function (options) {
            this.constructor.showServicesLegend(options);
        },
        get_expanded_names: function (options) {
            const $ = this.$;
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

            this.$el.find('.webgis-presentation_toc-title.webgis-presentation_toc-collapsable') // containers
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
            const $ = this.$;
            let $this = this.$el;

            if (options.exclusive === true) {
                this.constructor.collapseAll($this, options);
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
        },
        collapse_all: function (options) {
            this.constructor.collapseAll(this.$el, options);
        },
        createCustomContainer: function (options) {
            return this.constructor.addCustomContainer(this.$el, options);
        },
        reorder: function (options) {
            this.constructor.sortToc(this.$el);
        }
    },
    staticMethods: {
        // called without a TOC instance (from outside: webgis.ui.showServicesLegend, end of the plugin)
        showServicesLegend: function (options) {
            const Ctor = (webgis.$ || jQuery).fn.webgis_presentationToc.plugin;
            if (options && options.map) {
                let title = webgis.globals && webgis.globals.portal ? webgis.globals.portal.mapName : null;
                Ctor.showLegend(
                    options.sender,
                    options.map,
                    options.map.services,
                    title || "Darstellung, ©, ...");
            }
        },
        // registers a webgis event subscription (made by the static TOC builders) at the TOC instance
        // of parent, so the instance can release it in destroy
        trackSubscription: function (parent, events, channel, fn) {
            const $ = webgis.$ || jQuery;
            const inst = parent ? $(parent).data('webgis_presentationToc') : null;
            if (inst && inst._subscriptions) {
                inst._subscriptions.push({ events: events, channel: channel, fn: fn });
            }
        },
        // static methods don't rely on "this" being the plugin: they are also called via the legacy
        // global $.presentationToc and as event handlers (this = context element)
        process: function (sender, ids) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
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
                let p = Ctor.findItem(map, ids[i]);
                if (p && p.presentation && p.item) {
                    let layerids = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                    if (p.item.style === 'button') {
                        p.presentation.service.setServiceVisibility(null); // alle unsichtbar schalten!!! und unten die einzelnen Layer einschalten. In Gruppen können mehrer Darstellungsvarianten aus dem gleichen Dienst vorkommen. Darum zuerst Sichtbarkeit aus und dann die einzellnen wieder einschalten.
                    }
                }
            }
            for (let i = 0; i < ids.length; i++) {
                let p = Ctor.findItem(map, ids[i]);
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

            Ctor.checkItemVisibility();
            Ctor.checkContainerVisibility();
        },
        collectLayers: function (sender, ids) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            let $elem = sender ? $(sender).closest('.webgis-presentation_toc-holder') : $('.webgis-presentation_toc-holder');
            if ($elem.length === 0)
                return;

            let map = $elem.get(0)._map;
            if (map == null)
                return;

            var result = {};
            for (let i = 0; i < ids.length; i++) {
                let p = Ctor.findItem(map, ids[i]);
                if (p && p.presentation && p.item) {
                    const service = p.presentation.service;
                    const layerids = p.presentation.service.getLayerIdsFromNames(p.presentation.layers);
                    result[service.id] = result[service.id] || [];
                    for (let l of layerids) {
                        result[service.id].push(l);
                    }
                }
            }

            return result;
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
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
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
                        let p = Ctor.findItem(map, e.presIds[i]);
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
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
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
                            let p = Ctor.findItem(map, e.presIds[i]);
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
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
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
                                let p = Ctor.findItem(map, e.presIds[i]);
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
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
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
                        .on('click.webgis_presentation_toc', function (e) {
                            e.stopPropagation();

                            $('body').webgis_mapProperties({
                                map: map
                            });
                        });

                    $("<img>")
                        .attr('src', webgis.css.imgResource("admin-26.png", "ui"))
                        .css('width','26px')
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
                            .on('click.webgis_presentation_toc', function () {
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
                            .on('click.webgis_presentation_toc', function () {
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
                            .on('click.webgis_presentation_toc', function () {
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
                                            .on('click.webgis_presentation_toc', function (e) {
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
                                $toc_li.on('click.webgis_presentation_toc', function (e) {
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
                        .data('map', map)
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

                            // metadata
                            let $metadata = $("<div>")
                                .addClass('webgis-service-metadata-link');

                            if (service.metadata_link) {
                                $("<a>")
                                    .attr('href', service.metadata_link)
                                    .attr('target', '_blank')
                                    .text(webgis.l10n.get("metadata-link"))
                                    .appendTo($metadata);
                            }

                            if (webgis.usability.show_presentation_metadata_in_copyright) {
                                let $metadataList = $("<ul>")
                                    .css({ listStyle: "none", margin: 0, padding: "5px 0px" })
                                    .appendTo($metadata)
                                let usedGroupMetaContainerNames = [];
                                for (let p = 0; p < service.presentations.length; p++) {
                                    let presentation = service.presentations[p];
                                    //console.log(presentation);
                                    if (!presentation.items)
                                        continue;
                                    for (let i = 0; i < presentation.items.length; i++) {
                                        //console.log(presentation, presentation.items[i]);
                                        let prop = presentation.items[i];
                                        if (prop.group_metadata && usedGroupMetaContainerNames[prop.container] !== true) {
                                            usedGroupMetaContainerNames[prop.container] = true;
                                            let $group_metadata_item = $("<li>")
                                                .css({ padding: "4px 4px 4px 26px", cursor: "pointer", position: "relative", fontWeight:600 })
                                                .appendTo($metadataList)
                                                .data('metadata', prop.group_metadata)
                                                .data('metadata_target', prop.group_metadata_target)
                                                .data('metadata_dialog_width', prop.group_metadata_dialog_width)
                                                .data('metadata_dialog_height', prop.group_metadata_dialog_height)
                                                .attr('title', prop.group_metadata_title)
                                                .text(prop.container)
                                                .on('click.webgis_presentation_toc', function (event) {
                                                    event.stopPropagation();
                                                    Ctor.showMetadataLink($(this));
                                                });
                                            $("<span style='position:absolute;left:5px' class='webgis-api-icon webgis-api-icon-info'></span>")
                                                .prependTo($group_metadata_item);
                                        }
                                        if (prop.metadata) {
                                            let $metadata_item = $("<li>")
                                                .css({ padding: "4px 4px 4px 26px", cursor: "pointer", position: "relative" })
                                                .appendTo($metadataList)
                                                .data('metadata', prop.metadata)
                                                .data('metadata_target', prop.metadata_target)
                                                .data('metadata_dialog_width', prop.metadata_dialog_width)
                                                .data('metadata_dialog_height', prop.metadata_dialog_height)
                                                .attr('title', prop.metadata_title)
                                                .text(presentation.name)
                                                .on('click.webgis_presentation_toc', function (event) {
                                                    event.stopPropagation();
                                                    Ctor.showMetadataLink($(this));
                                                });
                                            $("<span style='position:absolute;left:5px;' class='webgis-api-icon webgis-api-icon-info'></span>")
                                                .prependTo($metadata_item);
                                        }
                                    }
                                }

                                if ($metadataList.children().length > 0) {
                                    $metadataList.appendTo($metadata);
                                }
                            }
                            if ($metadata.children().length > 0) {
                                $metadata.appendTo($descriptionTabContent)
                            }

                            Ctor.appendServiceDescription($descriptionTabContent, map, service);
                        }
                    }

                    if (descriptionCount === 0) {
                        $("<div>").text(webgis.l10n.get('no-copyright-info-available'))
                            .addClass('webgis-info')
                            .appendTo($descriptionTabContent);
                    }

                    $content.find('.webgis-tab-header[data-tab]')
                        .on('click.webgis_presentation_toc', function (e) {
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
                webgis.ui.showModal({
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
            const $ = webgis.$ || jQuery;
            webgis.ui.closeModal('webgis-legend');
        },
        refreshLegend: function (event, map, parent) {
            const $ = webgis.$ || jQuery;
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
        },
        addService: function (e, service, parent) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            webgis.log('addService', service.name);
            parent = parent || this;
            if (service == null || service.map == null)
                return;

            if (service.isBasemap) {
                return Ctor.addBasemap(e, service, parent);
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
                            webgis.log('data-order', service.name, service.getOrder())
                            $li.attr('data-order', Ctor._calcDataOrderByServiceOrder(service));
                        }
                        else if (prop.container_order) {
                            $li.attr('data-order', prop.container_order);
                        } // otherwise dont set data-order => containers will ordered aphabetic by name

                        $li.data('serviceGuids', []).appendTo($ul);

                        const li = $li.get(0);
                        $("<span style='position:absolute' class='webgis-presentation_toc-plus webgis-api-icon webgis-api-icon-triangle-1-s'></span>")
                            .appendTo($li)
                            .on('click.webgis_presentation_toc', function (event) {
                                event.stopPropagation();
                                $(this).parent().find('.webgis-presentation_toc-title-text').first().trigger('click');
                            });
                        //
                        $("<div class='webgis-presentation_toc-title-text webgis-search-content webgis-text-ellipsis-pro check-for-title'></div>")
                            .text(prop.container)
                            .on('click.webgis_presentation_toc', function (event) {
                                $li = $(this.parentNode);
                                event.stopPropagation();
                                if (Ctor.isInContentSearchMode(this, true))
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
                                        let item = Ctor.findItem(map, e.presIds[p]);
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
                            .on('click.webgis_presentation_toc', function () {
                                let serviceCollection = collectServices($(this));
                                if (serviceCollection && serviceCollection.services != null) {
                                    Ctor.showLegend(
                                        this,
                                        serviceCollection.map,
                                        serviceCollection.services,
                                        $(this).closest('.webgis-presentation_toc-title').find('.webgis-presentation_toc-title-text').html());
                                } else {
                                    webgis.alert(webgis.l10n.get('msg-no-legend-available'), 'info');
                                }
                            })
                            .on('contextmenu.webgis_presentation_toc', function () {
                                let serviceCollection = collectServices($(this));
                                if (serviceCollection && serviceCollection.services != null) {
                                    Ctor.showLegend(
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

                            if (Ctor.isInContentSearchMode(this, true))
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
                                .on('click.webgis_presentation_toc', clickGroupFunction);
                            $("<span style='position:absolute' class='webgis-api-icon webgis-api-icon-triangle-1-s'></span>").appendTo($title);
                            $("<span class='webgis-presentation_toc-item-group-checkbox'><span>").appendTo($title).on('click.webgis_presentation_toc', function (e) { Ctor.groupCheckBoxClicked(this, e); });
                            $("<span style='margin-left:14px' class='webgis-search-content'>&nbsp;" + webgis.encodeHtmlString(prop.name) + "</span>").appendTo($title);
                            Ctor.addItemMenu($group_li, $title);

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
                                        .on('click.webgis_presentation_toc', clickGroupFunction);

                                    $("<span style='position:absolute' class='webgis-api-icon webgis-api-icon-triangle-1-s'></span>").appendTo($ui_title);
                                    $("<span class='webgis-presentation_toc-item-group-checkbox'><span>").appendTo($ui_title).on('click.webgis_presentation_toc', function (e) { Ctor.groupCheckBoxClicked(this, e); });
                                    $("<span style='margin-left:14px' class='webgis-search-content'>&nbsp;" + webgis.encodeHtmlString(ui_groupname) + "</span>").appendTo($ui_title);
                                    Ctor.addItemMenu($ui_group, $ui_title);

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

                    if (prop.group_metadata && $group_li != null && !$group_li.data('toc-item-metadata')) {
                        $group_li.data('toc-item-metadata', {
                            metadata: prop.group_metadata,
                            metadata_target: prop.group_metadata_target,
                            metadata_title: prop.group_metadata_title,
                            metadata_dialog_width: prop.group_metadata_dialog_width,
                            metadata_dialog_height: prop.group_metadata_dialog_height
                        });
                    }
                    if (prop.group_metadata && $group_li != null && webgis.usability.show_metadata_i_button_toc
                        && $group_li.find('.webgis-api-icon.webgis-api-icon-info').length === 0) {
                        $("<span style='position:absolute;left:5px;' class='webgis-api-icon webgis-api-icon-info'></span>")
                            .prependTo($group_li.children('div'))
                            .data('metadata', prop.group_metadata)
                            .data('metadata_target', prop.group_metadata_target)
                            .attr('title', prop.group_metadata_title)
                            .on('click.webgis_presentation_toc', function (event) {
                                event.stopPropagation();
                                Ctor.showMetadataLink($(this));
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
                            $item_li.on('click.webgis_presentation_toc', function () {
                                Ctor.process(this, this.presIds, true);
                                //sendCmd('button', '5D3A7A26-2953-45b6-A1CD-40B75B669BFA|' + (this.groupid ? this.groupid : this.pid) + '|' + ($D('_img' + this.pid).src.indexOf('1.png') != -1 ? 'false' : 'true'), 'get');
                            });
                        }
                        else if (prop.style === 'optionbox') {
                            //console.log('add option box');
                            $item_li.addClass('webgis-presentation_toc-checkable optionbox');
                            let opt = "<img class='webgis-presentation_toc-checkable-icon' src=" + webgis.css.imgResource("option0.png", "toc") + ">";
                            $("<span class='webgis-search-content'>" + opt + "&nbsp;<span class='webgis-text-span nowrap'>" + webgis.encodeHtmlString(itemname) + "<span></span>").appendTo($item_li);
                            $item_li.on('click.webgis_presentation_toc', function () {
                                Ctor.process(this, this.presIds, true);
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
                                    .on('click.webgis_presentation_toc', function (e) {
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
                            $item_li.on('click.webgis_presentation_toc', function () {
                                let c = Ctor.process(this, this.presIds);
                                //sendCmd('button', '5D3A7A26-2953-45b6-A1CD-40B75B669BFA|' + (this.groupid ? this.groupid : this.pid), 'get');
                            });
                        }

                        $item_li.data('toc-item-map', service.map);
                        Ctor.addItemMenu($item_li, $item_li);
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
                            .data('metadata_title', prop[metadata_prefix + 'metadata_title'])
                            .data('metadata_dialog_width', prop[metadata_prefix + 'metadata_dialog_width'])
                            .data('metadata_dialog_height', prop[metadata_prefix + 'metadata_dialog_height']);
                        
                        
                        switch (prop[metadata_prefix + 'metadata_button_style']) {
                            case 'link_button':
                                if (webgis.usability.show_link_button_in_toc) {
                                    let $li = $("<li>")
                                        .addClass('webgis-presentation_toc-item link-button')
                                        .attr('data-order', $item_li.attr('data-order'))
                                        .appendTo($item_li.parent());

                                    $metadataButton
                                        .addClass('webgis-button')
                                        .text(prop[metadata_prefix + 'metadata_title'])
                                        .appendTo($li);
                                }
                                break;
                            default:
                                if (webgis.usability.show_metadata_i_button_toc) {
                                    // shown as badge behind the item name (updateItemBadges), click => item menu => metadata
                                    item_li.hasMetadataBadge = true;
                                }
                                break;
                        }

                        $metadataButton.on('click.webgis_presentation_toc', function (event) {
                            event.stopPropagation();
                            Ctor.showMetadataLink($(this));
                        });
                    }

                    if (!$item_li.data('toc-item-presentation')) {
                        $item_li.data('toc-item-presentation', presentation);
                    }
                    if (presentation && presentation.layers && presentation.layers.length > 0) {
                        item_li.hasTocLayers = true;
                    }
                    if (prop.style !== 'info'
                        && prop[metadata_prefix + 'metadata']
                        && !$item_li.data('toc-item-metadata')) {
                        $item_li.data('toc-item-metadata', {
                            metadata: prop[metadata_prefix + 'metadata'],
                            metadata_target: prop[metadata_prefix + 'metadata_target'],
                            metadata_title: prop[metadata_prefix + 'metadata_title'],
                            metadata_dialog_width: prop[metadata_prefix + 'metadata_dialog_width'],
                            metadata_dialog_height: prop[metadata_prefix + 'metadata_dialog_height']
                        });
                    }

                    if (prop.visible === false) {
                        $item_li.addClass('webgis-visibility-never');
                    }

                    if (prop.client_visibility) {
                        $item_li.addClass('webgis-visibility-client-' + prop.client_visibility);
                    }
                }
            }
            Ctor.checkItemVisibility(null, service, parent);
            Ctor.checkItemScaleVisibility(null, service.map, parent);
            Ctor.checkContainerVisibility(parent);

            const onServiceVisibilityChanged = function () {
                Ctor.checkItemVisibility();  // no parameter here
                Ctor.checkContainerVisibility();
            };
            service.events.on(['onchangevisibility', 'onchangevisibility-delayed'], onServiceVisibilityChanged, parent);
            Ctor.trackSubscription(parent, service.events, ['onchangevisibility', 'onchangevisibility-delayed'], onServiceVisibilityChanged);
            Ctor.sortToc(parent);

            // badges: user-defined renderers, toc layer filters (e.g. map loaded from a saved state) and metadata
            Ctor.updateItemBadges(parent, service.map);

            webgis.addTitleToEllipsis();
        },
        addDynamicContent: function (e, content, parent) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
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
                    .on('click.webgis_presentation_toc', function (event) {
                        event.stopPropagation();
                        $(this).parent().find('.webgis-presentation_toc-title-text').first().trigger('click');
                    });

                $("<div class='webgis-presentation_toc-title-text'></div>")
                    .text(containerName)
                    .on('click.webgis_presentation_toc', function (event) {
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

                const onLoadDynamicContent = function (e, sender) {
                    Ctor.checkContainerVisibility(this);
                };
                content.map.events.on('onloaddynamiccontent', onLoadDynamicContent, parent);
                Ctor.trackSubscription(parent, content.map.events, 'onloaddynamiccontent', onLoadDynamicContent);
            }
            else {
                $item_ul = $li.find('.webgis-presentation_toc-content').children('ul');
            }

            $item_li = $("<li id='" + content.id + "' class='webgis-presentation_toc-item webgis-presentation_toc-dyncontent-item'></li>")
                .appendTo($item_ul)
                .data('dynamic-content', content)
                .on('click.webgis_presentation_toc', function () {
                    let c = $(this).data('dynamic-content');
                    if ($(this).hasClass('webgis-presentation_toc-dyncontent-item-selected')) {

                        let $holder = $(c.map.ui.webgisContainer()).find('.webgis-search-result-list').closest('.webgis-search-result-holder'),
                            hasHistory = $.webgis_search_result_histories[c.map] != null && $.webgis_search_result_histories[c.map].count() > 0;

                        c.map.queryResultFeatures.clear(hasHistory);
                        c.map.getSelection('selection').remove();
                        webgis.ui.emptyQueryResultsList($holder.find('.webgis-search-result-list'), { map: c.map });
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
        },
        addBasemap: function (e, service, parent) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
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
                    .on('click.webgis_presentation_toc', function (event) {
                        event.stopPropagation();
                        $(this).parent().find('.webgis-presentation_toc-title-text').first().trigger('click');
                    });

                $("<div class='webgis-presentation_toc-title-text webgis-text-ellipisis-pro check-for-title'></div>")
                    .text(containerName)
                    .on('click.webgis_presentation_toc', function (event) {
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
                $item_li.on('click.webgis_presentation_toc', function () {
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
                    .on('click.webgis_presentation_toc', function () {
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

                const onBasemapChangedOpacity = function (e, sender, basemapService) {
                    webgis.ui.refreshOpacityControls(this);
                    sender.ui.refreshUIElements();
                };
                service.map.events.on('basemap_changed', onBasemapChangedOpacity, $opacity_container);
                Ctor.trackSubscription(parent, service.map.events, 'basemap_changed', onBasemapChangedOpacity);

                const onBasemapChanged = function (e, sender, basemapService) {
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

                    Ctor.checkContainerVisibility();
                };
                service.map.events.on('basemap_changed', onBasemapChanged, $li);
                Ctor.trackSubscription(parent, service.map.events, 'basemap_changed', onBasemapChanged);
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

                $item_li.on('click.webgis_presentation_toc', function () {
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

                $item_li.on('click.webgis_presentation_toc', function () {
                    const service = $(this).data("service");
                    service.map.setBasemap(service.id);
                    $(this).parent().find('.webgis-presentation_toc-basemap-item-img').removeClass('selected');
                    $(this).find('.webgis-presentation_toc-basemap-item-img').addClass('selected');
                    if (webgis.useMobileCurrent()) {
                        service.map.events.fire('hidepresentations');
                    }

                    // led it be expanded... or as it is ;)
                    // let $collapse = $(this).parent().find('.webgis-presentation_toc-basemap-collapse');
                    // if ($collapse.hasClass('expanded'))
                    //     $collapse.trigger('click');
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

                const onRefreshPreview = function (e) {
                    let service = this.data("service");
                    if (!service || !service.map) {
                        return;
                    }
                    let scale = Math.round(service.map.scale());
                    if (this.data('last-refresh-scale') === scale)
                        return;

                    $(this).data('last-refresh-scale', scale);
                    this.data("resetPreview").apply(this);
                };
                service.map.events.on('refresh', onRefreshPreview, $item_li);
                Ctor.trackSubscription(parent, service.map.events, 'refresh', onRefreshPreview);

                if (!$item_li.hasClass('webgis-presentation_toc-hidden')) {
                    webgis.delayed(function ($item) {
                        $item.data("resetPreview").apply($item);
                    }, 100, $item_li);
                }
            }

            $ul.find('.webgis-presentation_toc-basemap-collapse')
                .toggleClass('expanded', !webgis.usability.expandBasemapsOnAddServices || false)
                .trigger('click');
        },
        removeService: function (e, service, parent) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            if (service.isBasemap) {
                parent = parent || $('.webgis-presentation_toc-holder');
                let $elem = $(parent);
                $elem.find('.webgis-presentation_toc-basemap-item').each(function (i, e) {
                    const itemService = $(e).data("service");
                    if (itemService && itemService.id == service.id) {
                        $(e).remove();
                    }
                });
            }
            Ctor.checkItemVisibility(null, service, parent);
        },
        sortToc: function (parent) {
            const $ = webgis.$ || jQuery;
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
        },
        isInContentSearchMode: function (sender, reset) {
            const $ = webgis.$ || jQuery;
            let $elem = sender ? $(sender).closest('.webgis-presentation_toc-holder') : $('.webgis-presentation_toc-holder');

            if ($elem.hasClass('webgis-content-search-area active')) {
                if (reset === true) {
                    $elem.find('.webgis-presentation_toc-search').webgis_contentsearch('reset');
                }
                return true;
            }

            return false;
        },
        addCustomContainer: function (parent, options) {
            const $ = webgis.$ || jQuery;
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
                    .on('click.webgis_presentation_toc', function (event) {
                        event.stopPropagation();
                        $(this).parent().find('.webgis-presentation_toc-title-text').first().trigger('click');
                    });

                $("<div class='webgis-presentation_toc-title-text'></div>")
                    .text(containerName)
                    .on('click.webgis_presentation_toc', function (event) {
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
        },
        showMetadataLink: function ($sender, metadata) {
            const $ = webgis.$ || jQuery;
            // metadata: optional { metadata, metadata_target, metadata_title, metadata_dialog_width, metadata_dialog_height },
            // otherwise read from the data of $sender (metadata button)
            const meta = metadata || {
                metadata: $sender.data('metadata'),
                metadata_target: $sender.data('metadata_target'),
                metadata_title: $sender.data('metadata_title'),
                metadata_dialog_width: $sender.data('metadata_dialog_width'),
                metadata_dialog_height: $sender.data('metadata_dialog_height')
            };
            let map = $sender.closest('.webgis-tab-content').data("map")
                || $sender.closest('.webgis-presentation_toc-holder').get(0)._map,
                url = webgis.tools.replaceCustomToolUrl(map, meta.metadata),
                target = meta.metadata_target;

            switch (target) {
                case 'dialog':
                    webgis.iFrameDialog(
                        url,
                        meta.metadata_title || "Metadaten",
                        meta.metadata_dialog_width,
                        meta.metadata_dialog_height);
                    break;
                default:
                    window.open(url);
                    break;
            }
        },
        // context menu of a toc item (burger button / right click): every provider adds its entries for the item
        // provider: { hasItems($li) => bool (cheap, while building the toc), getItems($li, map) => [contextMenu items] }
        // item.data.action is called, when the entry is clicked
        tocItemMenuProviders: [
            {   // metadata: always for the whole presentation (no sub entries)
                hasItems: function ($li) {
                    return !!$li.data('toc-item-metadata');
                },
                getItems: function ($li, map) {
                    const Ctor = (webgis.$ || jQuery).fn.webgis_presentationToc.plugin;
                    const metadata = $li.data('toc-item-metadata');
                    if (!metadata) return [];

                    return [{
                        text: webgis.l10n.get('metadata'),
                        icon: webgis.css.imgResource('info-26.png', 'toc'),
                        data: { action: function () { Ctor.showMetadataLink($li, metadata); } }
                    }];
                }
            },
            {   // query builder (toc vis filter): all layers (common fields) or a single layer
                hasItems: function ($li) {
                    return webgis.usability.allowTocVisFilters === true && $li.get(0).hasTocLayers === true;
                },
                getItems: function ($li, map) {
                    const Ctor = (webgis.$ || jQuery).fn.webgis_presentationToc.plugin;
                    if (webgis.usability.allowTocVisFilters !== true || !map)
                        return [];

                    const layers = Ctor.collectItemLayers($li, map);
                    if (layers.length === 0)
                        return [];

                    const presentation = $li.data('toc-item-presentation') || {};
                    const openQueryBuilder = function (serviceLayers) {
                        webgis.tools.onButtonClick(map,
                            {
                                command: 'set_toc_layer_filter',
                                type: 'servertoolcommand_ext',
                                id: 'webgis.tools.presentation.visfilter',
                                map: map,
                                suppressToolDialog: true,
                                argument: JSON.stringify({
                                    id: presentation.id,
                                    name: presentation.name,
                                    serviceLayers: serviceLayers
                                })
                            }, $li.get(0), null, null);
                    };
                    const toServiceLayers = function (layers) {
                        const serviceLayers = {};
                        for (const layer of layers) {
                            serviceLayers[layer.service.id] = serviceLayers[layer.service.id] || [];
                            serviceLayers[layer.service.id].push(layer.layerId);
                        }
                        return serviceLayers;
                    };

                    const text = webgis.l10n.get('toc-item-menu-querybuilder'),
                        icon = webgis.css.imgResource('filter-26.png', 'tools'),
                        withFilter = layers.filter(function (l) { return l.hasFilter; });

                    let items;
                    if (layers.length === 1) {
                        items = [{
                            text: text,
                            icon: icon,
                            checked: layers[0].hasFilter,
                            data: { action: function () { openQueryBuilder(toServiceLayers(layers)); } }
                        }];
                    } else {
                        const subItems = [{
                            text: webgis.l10n.get('toc-item-menu-all-layers'),
                            checked: layers.every(function (l) { return l.hasFilter; }),
                            data: { action: function () { openQueryBuilder(toServiceLayers(layers)); } }
                        }, { separator: true }];

                        for (const layer of layers) {
                            subItems.push({
                                text: layer.title,
                                checked: layer.hasFilter,
                                data: { action: function () { openQueryBuilder(toServiceLayers([layer])); } }
                            });
                        }

                        items = [{ text: text, icon: icon, items: subItems }];
                    }

                    if (withFilter.length > 0) {
                        items.push({
                            text: webgis.l10n.get('toc-item-menu-filter-reset'),
                            data: { action: function () { Ctor.removeTocLayerFilters(map, withFilter); } }
                        });
                    }

                    return items;
                }
            },
            {   // user renderer ("Darstellung"): only single layers with allow_user_renderer (AGS DynamicLayers)
                hasItems: function ($li) {
                    return $li.get(0).hasTocLayers === true;
                },
                getItems: function ($li, map) {
                    const Ctor = (webgis.$ || jQuery).fn.webgis_presentationToc.plugin;
                    if (!map)
                        return [];

                    const layers = Ctor.collectItemLayers($li, map).filter(function (l) {
                        return l.layer && l.layer.allow_user_renderer === true;
                    });
                    if (layers.length === 0)
                        return [];

                    const openRendererEditor = function (layer) {
                        webgis.tools.onButtonClick(map,
                            {
                                command: 'set_toc_layer_renderer',
                                type: 'servertoolcommand_ext',
                                id: webgis.tools.presenation.userRenderer,
                                map: map,
                                suppressToolDialog: true,
                                argument: JSON.stringify({
                                    serviceId: layer.service.id,
                                    layerId: layer.layerId
                                })
                            }, $li.get(0), null, null);
                    };

                    const text = webgis.l10n.get('toc-item-menu-userrenderer'),
                        icon = webgis.css.imgResource('colorschema-26.png', 'tools'),
                        withRenderer = layers.filter(function (l) { return l.service.hasLayerRenderer(l.layerId); });

                    let items;
                    if (layers.length === 1) {
                        items = [{
                            text: text,
                            icon: icon,
                            checked: withRenderer.length > 0,
                            data: { action: function () { openRendererEditor(layers[0]); } }
                        }];
                    } else {
                        items = [{
                            text: text,
                            icon: icon,
                            items: layers.map(function (layer) {
                                return {
                                    text: layer.title,
                                    checked: layer.service.hasLayerRenderer(layer.layerId),
                                    data: { action: function () { openRendererEditor(layer); } }
                                };
                            })
                        }];
                    }

                    if (withRenderer.length > 0) {
                        items.push({
                            text: webgis.l10n.get('toc-item-menu-userrenderer-reset'),
                            data: {
                                action: function () {
                                    for (const layer of withRenderer) {
                                        layer.service.setLayerRenderer(layer.layerId, null);
                                    }
                                }
                            }
                        });
                    }

                    return items;
                }
            },
            {   // properties: always the last entry (separator before)
                hasItems: function ($li) {
                    return true;
                },
                getItems: function ($li, map) {
                    const Ctor = (webgis.$ || jQuery).fn.webgis_presentationToc.plugin;
                    return [{ separator: true }, {
                        text: webgis.l10n.get('toc-item-menu-properties'),
                        icon: webgis.css.imgResource('admin-26.png', 'ui'),
                        data: { action: function () { Ctor.showItemProperties($li, map); } }
                    }];
                }
            }
        ],
        // distinct layers of all presentations of a toc item or a group (all items of the group):
        // [{ service, layerId, layer, query, title, hasFilter }]
        collectItemLayers: function ($li, map) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            const result = [], keys = {};
            if (!map || !$li || $li.length === 0)
                return result;

            let presIds = [];
            if ($li.hasClass('webgis-presentation_toc-item-group')) {
                $li.find('li.webgis-presentation_toc-item').each(function (i, e) {
                    presIds = presIds.concat(e.presIds || []);
                });
            } else {
                presIds = $li.get(0).presIds || [];
            }

            for (const id of presIds) {
                const p = Ctor.findItem(map, id);
                if (!p || !p.presentation || !p.item || !p.presentation.service)
                    continue;

                const service = p.presentation.service;
                for (const layerId of service.getLayerIdsFromNames(p.presentation.layers)) {
                    const key = service.id + '~' + layerId;
                    if (keys[key]) continue;
                    keys[key] = true;

                    const query = service.getQueryByLayerId(layerId),
                        layer = service.getLayer(layerId);
                    const title = (query && query.name)
                        || (layer && layer.name ? layer.name.split('\\').pop() : layerId);

                    result.push({
                        service: service,
                        layerId: layerId,
                        layer: layer,
                        query: query || null,
                        title: title,
                        hasFilter: Ctor.hasTocLayerFilter(map, service.id, layerId)
                    });
                }
            }

            // same title in different services => append the service name
            for (const layer of result) {
                if (result.some(function (l) { return l !== layer && l.title === layer.title && l.service !== layer.service; })) {
                    layer.title += ' (' + (layer.service.name || layer.service.id) + ')';
                }
            }

            return result;
        },
        // marks toc items with a user-defined renderer ("Darstellung"), a toc layer filter on one of their layers
        // or metadata (instead of an i-button). Badges are placed behind the item name, a click opens the item menu
        updateItemBadges: function (parent, map) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            const $parent = $(parent);
            map = map || (parent && parent._map);

            const anyRenderer = !!map && !!map.services && Object.keys(map.services).some(function (id) {
                const service = map.services[id];
                return service && typeof service.hasLayerRenderers === 'function' && service.hasLayerRenderers();
            });
            const anyFilter = !!map && (map._visfilters || []).some(function (f) { return f.id && f.id.indexOf('#TOC#~') === 0; });

            $parent.find('li.webgis-presentation_toc-item').each(function (i, li) {
                const $li = $(li);
                const layers = anyRenderer || anyFilter ? Ctor.collectItemLayers($li, map) : [];
                const hasRenderer = anyRenderer && layers.some(function (l) {
                    return typeof l.service.hasLayerRenderer === 'function' && l.service.hasLayerRenderer(l.layerId);
                });
                const hasFilter = anyFilter && layers.some(function (l) { return l.hasFilter; });
                const metadata = li.hasMetadataBadge === true ? $li.data('toc-item-metadata') : null;

                $li.find('.webgis-presentation_toc-item-badges').remove();
                if (!hasRenderer && !hasFilter && !metadata) {
                    $li.removeClass('has-badges');
                    return;
                }

                const $text = $li.find('.webgis-text-span').first();
                if ($text.length !== 1) {
                    return;
                }
                $li.addClass('has-badges');

                const titles = [];
                const $badges = $("<span>").addClass('webgis-presentation_toc-item-badges').insertAfter($text);
                const addBadge = function (img, folder, title) {
                    $("<img>")
                        .addClass('webgis-presentation_toc-item-badge')
                        .attr('src', webgis.css.imgResource(img, folder))
                        .appendTo($badges);
                    titles.push(title);
                };
                if (hasRenderer) {
                    addBadge('colorschema-26.png', 'tools', webgis.l10n.get('toc-item-userrenderer-active'));
                }
                if (hasFilter) {
                    addBadge('filter-26.png', 'tools', webgis.l10n.get('toc-item-filter-active'));
                }
                if (metadata) {
                    addBadge('info-26.png', 'toc', metadata.metadata_title || webgis.l10n.get('metadata'));
                }
                $badges.attr('title', titles.join('\n'))
                    .on('click.webgis_presentation_toc', function (e) {
                        // don't toggle the visibility of the item, open the item menu instead
                        e.stopPropagation();
                        e.preventDefault();
                        Ctor.showItemContextMenu($li, e);
                    });
            });
        },
        hasTocLayerFilter: function (map, serviceId, layerId) {
            const filterId = '#TOC#~' + serviceId + '~' + layerId;
            return (map._visfilters || []).some(function (f) { return f.id === filterId; });
        },
        // burger button (on hover) + right click => context menu of a toc item or group
        // $li: li of the item/group, $host: element for the button and the right click (item: $li, group: title div)
        addItemMenu: function ($li, $host) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            const $overlayButtons = $("<div>")
                .addClass('webgis-presentation_toc-item-overlay-buttons');
            if ($li.hasClass('webgis-presentation_toc-item-group')) {
                $overlayButtons.prependTo($host);  // float right in the group title
            } else {
                $overlayButtons.appendTo($host);
            }
            $("<div>")
                .addClass('webgis-presentation_toc-item-overlay-button menu')
                .attr('title', webgis.l10n.get('toc-item-menu'))
                .appendTo($overlayButtons)
                .on('click.webgis_presentation_toc', function (e) {
                    e.stopPropagation();
                    Ctor.showItemContextMenu($li, e);
                });
            $host.on('contextmenu.webgis_presentation_toc', function (e) {
                e.stopPropagation();
                Ctor.showItemContextMenu($li, e);
                return false;
            });
        },
        getItemMap: function ($li) {
            const $holder = $li.closest('.webgis-presentation_toc-holder');
            return $li.data('toc-item-map') || ($holder.length > 0 ? $holder.get(0)._map : null);
        },
        showItemContextMenu: function ($li, e) {
            const Ctor = (webgis.$ || jQuery).fn.webgis_presentationToc.plugin;
            if (!$li || $li.length === 0 || !webgis.ui.showContextMenu)
                return false;

            const map = Ctor.getItemMap($li);
            let items = [];
            for (const provider of Ctor.tocItemMenuProviders) {
                if (!provider.hasItems($li)) continue;
                items = items.concat(provider.getItems($li, map) || []);
            }
            // no leading, trailing or double separators
            items = items.filter(function (item, i, arr) {
                return !item.separator || (i > 0 && i < arr.length - 1 && !arr[i - 1].separator);
            });
            if (items.length === 0)
                return false;

            webgis.ui.showContextMenu({
                clickEvent: e,
                items: items,
                callback: function (data) {
                    if (data && typeof data.action === 'function') {
                        data.action();
                    }
                }
            });

            return true;
        },
        // service description and copyright (legend dialog, properties dialog)
        appendServiceDescription: function ($target, map, service) {
            const $ = webgis.$ || jQuery;
            if (service.servicedescription) {
                let $descr = $("<div></div>")
                    .addClass('webgis-service-description')
                    .appendTo($target);

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
                    .appendTo($target);

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
                        .appendTo($target);

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
        },
        formatScale: function (scale) {
            return Math.round(scale).toLocaleString(webgis.l10n.language || undefined);
        },
        // "from 1:max to 1:min" (visible, if min <= scale <= max)
        formatScaleRange: function (layer) {
            const Ctor = (webgis.$ || jQuery).fn.webgis_presentationToc.plugin;
            const min = layer && layer.minscale > 1 ? layer.minscale : 0,
                max = layer && layer.maxscale > 1 ? layer.maxscale : 0;
            if (min && max)
                return webgis.l10n.get('toc-props-scale-range', { min: Ctor.formatScale(min), max: Ctor.formatScale(max) });
            if (max)
                return webgis.l10n.get('toc-props-scale-from', { max: Ctor.formatScale(max) });
            if (min)
                return webgis.l10n.get('toc-props-scale-to', { min: Ctor.formatScale(min) });
            return webgis.l10n.get('toc-props-scale-always');
        },
        // zoom (around the current center) to the nearest scale, where the layer is visible
        zoomToLayerScale: function (map, layer) {
            const min = layer && layer.minscale > 1 ? layer.minscale : 0,
                max = layer && layer.maxscale > 1 ? layer.maxscale : 0;
            const crsScale = map.crsScale(), scale = map.scale();
            if (!crsScale || !scale || (!min && !max))
                return;

            // layer scales are compared with the crs scale (see checkItemScaleVisibility), setScale uses the map scale
            const factor = scale / crsScale;
            const inRange = function (s) { return (!min || s >= min) && (!max || s <= max); };
            if (inRange(crsScale))
                return;

            let target = (min && crsScale < min) ? min * 1.02 : max * 0.98;
            if (!inRange(target))
                target = (min + max) / 2;

            // prefer a zoom level of the map inside the range (setScale snaps to the levels)
            let best = null;
            if (map.crs && map.crs.resolutions) {
                const dpm = 96.0 / 0.0254;
                for (const res of map.crs.resolutions) {
                    const s = Math.round(res * dpm);
                    if (!inRange(s / factor))
                        continue;
                    if (best === null || Math.abs(Math.log(s / factor / target)) < Math.abs(Math.log(best / factor / target)))
                        best = s;
                }
            }

            map.setScale(best !== null ? best : target * factor, map.getCenter());
        },
        removeTocLayerFilters: function (map, layers) {
            const Ctor = (webgis.$ || jQuery).fn.webgis_presentationToc.plugin;
            const services = {};
            for (const layer of layers) {
                if (!Ctor.hasTocLayerFilter(map, layer.service.id, layer.layerId)) continue;
                map.unsetFilter('#TOC#~' + layer.service.id + '~' + layer.layerId);
                services[layer.service.id] = layer.service;
            }
            for (const id in services) {
                services[id].refresh();
            }
            if (map.ui && map.ui.refreshUIElements) {
                map.ui.refreshUIElements();
            }
        },
        // properties dialog of a toc item (presentation) or a group: tabs general, legend, info & copyright, technical
        showItemProperties: function ($li, map) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            map = map || Ctor.getItemMap($li);
            if (!map || !$li || $li.length === 0)
                return;

            const isGroup = $li.hasClass('webgis-presentation_toc-item-group');
            const name = isGroup ? ($li.attr('data-ui-groupname') || $li.get(0).groupname) : $li.get(0).groupname;
            const isMapAuthor = !!(webgis.globals && webgis.globals.portal && webgis.globals.portal.isMapAuthor === true);

            const tabs = [
                { title: webgis.l10n.get('toc-props-general'), render: isGroup ? Ctor._renderGroupProperties : Ctor._renderItemProperties },
                { title: webgis.l10n.get('legend'), render: Ctor._renderItemLegend }
            ];
            if (!isGroup) {
                tabs.push({ title: webgis.l10n.get('info-and-copyright'), render: Ctor._renderItemInfo });
                if (isMapAuthor) {
                    tabs.push({ title: webgis.l10n.get('toc-props-technical'), render: Ctor._renderItemTechnical });
                }
            }

            webgis.ui.showModal({
                id: 'webgis-toc-item-properties',
                title: name,
                width: '640px',
                height: '70%',
                onload: function ($content) {
                    $content.addClass('webgis-toc-properties');
                    const $tr = $("<tr>").css('white-space', 'nowrap')
                        .appendTo($("<table>").css('width', '100%').addClass('webgis-modal-content-fill').appendTo($content));

                    tabs.forEach(function (tab, i) {
                        $("<td>")
                            .text(tab.title)
                            .addClass('webgis-tab-header')
                            .attr('data-tab', i)
                            .appendTo($tr);

                        const $tab = $("<div>")
                            .addClass('webgis-tab-content')
                            .attr('data-tab', i)
                            .css('display', 'none')
                            .appendTo($content);
                        // render lazy, when the tab is shown first (legend requests)
                        $tab.data('render', function () {
                            $tab.empty().data('rendered', true);
                            tab.render($tab, $li, map);
                        });
                    });

                    $content.find('.webgis-tab-header[data-tab]').on('click', function (e) {
                        e.stopPropagation();
                        const $header = $(this);
                        $header.parent().children('.webgis-tab-header').removeClass('selected');
                        $header.addClass('selected');
                        $content.children('.webgis-tab-content').css('display', 'none');
                        const $tab = $content.children('.webgis-tab-content[data-tab=' + $header.attr('data-tab') + ']').css('display', 'block');
                        if ($tab.data('rendered') !== true) {
                            $tab.data('render')();
                        }
                    });
                    $content.find('.webgis-tab-header:first').trigger('click');
                }
            });
        },
        _appendPropertyRow: function ($table, label, value, copyable) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            if (value === undefined || value === null || value === '')
                return;
            const $tr = $("<tr>").appendTo($table);
            $("<th>").text(label).appendTo($tr);
            const $td = $("<td>").text(value).appendTo($tr);
            if (copyable) Ctor._appendCopyButton($td, value);
        },
        // subtle copy button (visible on row hover)
        _appendCopyButton: function ($td, value) {
            const $ = webgis.$ || jQuery;
            if (value === undefined || value === null || value === '')
                return $td;
            $("<span>")
                .addClass('webgis-toc-properties-copy')
                .attr('title', webgis.l10n.get('copy-to-clipboard'))
                .css('background-image', 'url(' + webgis.css.imgResource('copy-26.png', 'sharebuttons') + ')')
                .appendTo($td.addClass('copyable'))
                .on('click', function (e) {
                    e.stopPropagation();
                    webgis.copyString(String(value));
                    const $btn = $(this).addClass('copied');
                    setTimeout(function () { $btn.removeClass('copied'); }, 1000);
                });
            return $td;
        },
        _flag: function (value) {
            if (value === true) return '\u2713';
            if (value === false) return '\u2013';
            return value;
        },
        _renderItemProperties: function ($tab, $li, map) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            const presentation = $li.data('toc-item-presentation') || {};
            const layers = Ctor.collectItemLayers($li, map);
            const services = [];
            for (const layer of layers) {
                if (services.indexOf(layer.service) < 0) services.push(layer.service);
            }

            const $props = $("<table>").addClass('webgis-toc-properties-table key-value').appendTo($tab);
            Ctor._appendPropertyRow($props, webgis.l10n.get('toc-props-name'), $li.get(0).groupname);
            Ctor._appendPropertyRow($props, webgis.l10n.get('toc-props-description'), presentation.description);
            Ctor._appendPropertyRow($props, webgis.l10n.get('services'), services.map(function (s) { return s.name || s.id; }).join(', '));

            if (layers.length === 0)
                return;

            $("<h3>").text(webgis.l10n.get('toc-props-layers')).appendTo($tab);
            const $table = $("<table>").addClass('webgis-toc-properties-table').appendTo($tab);
            const $head = $("<tr>").appendTo($table);
            [webgis.l10n.get('toc-props-name'), webgis.l10n.get('toc-props-visible'), webgis.l10n.get('toc-props-scale'),
            webgis.l10n.get('toc-props-queryable'), webgis.l10n.get('toc-props-filter')].forEach(function (text) {
                $("<th>").text(text).appendTo($head);
            });

            const scale = map.crsScale(), rerender = function () { $tab.data('render')(); };
            for (const layer of layers) {
                const inScale = layer.service.layerInScale(layer.layerId, scale);
                const $tr = $("<tr>").toggleClass('out-of-scale', !inScale).appendTo($table);
                Ctor._appendCopyButton($("<td>").text(layer.title).appendTo($tr), layer.title);
                $("<td>").text(Ctor._flag(!!(layer.layer && layer.layer.visible))).appendTo($tr);

                const $scale = $("<td>").text(Ctor.formatScaleRange(layer.layer)).appendTo($tr);
                if (!inScale) {
                    $scale.attr('title', webgis.l10n.get('toc-props-out-of-scale'));
                    $("<div>")
                        .addClass('webgis-toc-properties-action')
                        .text(webgis.l10n.get('toc-props-zoom-to-scale'))
                        .appendTo($scale)
                        .on('click', function () {
                            const onRefresh = function () {
                                map.events.off('refresh', onRefresh);
                                if ($.contains(document, $tab.get(0))) rerender();
                            };
                            map.events.on('refresh', onRefresh);
                            Ctor.zoomToLayerScale(map, layer.layer);
                        });
                }

                $("<td>").text(Ctor._flag(!!layer.query)).appendTo($tr);

                const $filter = $("<td>").text(Ctor._flag(layer.hasFilter)).appendTo($tr);
                if (layer.hasFilter) {
                    $("<div>")
                        .addClass('webgis-toc-properties-action')
                        .text(webgis.l10n.get('toc-props-remove-filter'))
                        .appendTo($filter)
                        .on('click', function () {
                            Ctor.removeTocLayerFilters(map, [layer]);
                            rerender();
                        });
                }
            }

            if (layers.filter(function (l) { return l.hasFilter; }).length > 1) {
                $("<button>")
                    .addClass('webgis-button')
                    .text(webgis.l10n.get('toc-props-remove-all-filters'))
                    .appendTo($("<div>").css('margin-top', '10px').appendTo($tab))
                    .on('click', function () {
                        Ctor.removeTocLayerFilters(map, layers);
                        rerender();
                    });
            }
        },
        _renderGroupProperties: function ($tab, $li, map) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;

            const $props = $("<table>").addClass('webgis-toc-properties-table key-value').appendTo($tab);
            Ctor._appendPropertyRow($props, webgis.l10n.get('toc-props-name'), $li.attr('data-ui-groupname') || $li.get(0).groupname);

            const $items = $li.find('li.webgis-presentation_toc-item')
                .not('.link-button')
                .not('.webgis-display-none');
            if ($items.length === 0)
                return;

            $("<h3>").text(webgis.l10n.get('toc-props-presentations')).appendTo($tab);
            const $table = $("<table>").addClass('webgis-toc-properties-table').appendTo($tab);
            const $head = $("<tr>").appendTo($table);
            [webgis.l10n.get('toc-props-name'), webgis.l10n.get('toc-props-visible'), webgis.l10n.get('toc-props-scale'),
            webgis.l10n.get('toc-props-filter')].forEach(function (text) {
                $("<th>").text(text).appendTo($head);
            });

            $items.each(function (i, e) {
                const $e = $(e), layers = Ctor.collectItemLayers($e, map);
                const visibleCount = layers.filter(function (l) { return l.layer && l.layer.visible; }).length;
                const inScale = !$e.hasClass('webgis-outofscale');

                const $tr = $("<tr>").toggleClass('out-of-scale', !inScale).appendTo($table);
                $("<td>").text(e.groupname || '').appendTo($tr);
                $("<td>").text(layers.length === 0 ? '' :
                    (visibleCount === layers.length ? Ctor._flag(true) : (visibleCount === 0 ? Ctor._flag(false) : webgis.l10n.get('toc-props-partly'))))
                    .appendTo($tr);
                $("<td>").text(Ctor._flag(inScale)).appendTo($tr);
                $("<td>").text(Ctor._flag(layers.some(function (l) { return l.hasFilter; }))).appendTo($tr);
            });
        },
        // legend of all layers of the item/group (also the invisible layers)
        _renderItemLegend: function ($tab, $li, map) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            const serviceLayers = [];
            for (const layer of Ctor.collectItemLayers($li, map)) {
                if (!layer.layer || layer.layer.legend !== true || !layer.service.canLegend() || layer.service.serviceInfo.has_legend === false)
                    continue;
                let entry = serviceLayers.find(function (s) { return s.service === layer.service; });
                if (!entry) serviceLayers.push(entry = { service: layer.service, layerIds: [] });
                entry.layerIds.push(layer.layerId);
            }

            const noLegend = function ($target) {
                $("<div>").text(webgis.l10n.get('toc-props-no-legend')).addClass('webgis-info').appendTo($target);
            };
            if (serviceLayers.length === 0) {
                noLegend($tab);
                return;
            }

            for (const entry of serviceLayers) {
                if (serviceLayers.length > 1) {
                    $("<h3>").text(entry.service.name || entry.service.id).appendTo($tab);
                }
                const $holder = $("<div>").addClass('webgis-loading').appendTo($tab);
                webgis.ajax({
                    url: entry.service.getLegendUrl(true),
                    type: 'post',
                    data: $.extend(entry.service.getLegendPostData(), { layers: entry.layerIds.join(',') }),
                    success: function (result) {
                        $holder.removeClass('webgis-loading');
                        if (result && result.url) {
                            $("<img>").attr('src', result.url).appendTo($holder);
                        } else {
                            noLegend($holder);
                        }
                    },
                    error: function () {
                        $holder.removeClass('webgis-loading');
                        noLegend($holder);
                    }
                });
            }
        },
        _renderItemInfo: function ($tab, $li, map) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            const services = [];
            for (const layer of Ctor.collectItemLayers($li, map)) {
                if (services.indexOf(layer.service) < 0) services.push(layer.service);
            }

            for (const service of services) {
                const $section = $("<div>");
                if (service.metadata_link) {
                    $("<a>")
                        .attr('href', service.metadata_link)
                        .attr('target', '_blank')
                        .text(webgis.l10n.get('metadata-link'))
                        .appendTo($("<div>").addClass('webgis-service-metadata-link').appendTo($section));
                }
                Ctor.appendServiceDescription($section, map, service);

                if ($section.children().length > 0) {
                    $("<h2>").text(service.name || service.id).appendTo($tab);
                    $section.appendTo($tab);
                }
            }

            if ($tab.children().length === 0) {
                $("<div>").text(webgis.l10n.get('no-copyright-info-available')).addClass('webgis-info').appendTo($tab);
            }
        },
        // only for map authors (Portal: webgis.globals.portal.isMapAuthor)
        _renderItemTechnical: function ($tab, $li, map) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            const layers = Ctor.collectItemLayers($li, map);

            const $props = $("<table>").addClass('webgis-toc-properties-table key-value').appendTo($tab);
            Ctor._appendPropertyRow($props, webgis.l10n.get('toc-props-dvid'), $li.attr('data-dvid'), true);

            const services = [];
            for (const layer of layers) {
                if (services.indexOf(layer.service) < 0) services.push(layer.service);
            }

            for (const service of services) {
                $("<h3>").text(service.name || service.id).appendTo($tab);
                const $serviceProps = $("<table>").addClass('webgis-toc-properties-table key-value').appendTo($tab);
                Ctor._appendPropertyRow($serviceProps, webgis.l10n.get('toc-props-id'), service.id, true);
                Ctor._appendPropertyRow($serviceProps, webgis.l10n.get('toc-props-type'), service.type);

                const $table = $("<table>").addClass('webgis-toc-properties-table').appendTo($tab);
                const $head = $("<tr>").appendTo($table);
                [webgis.l10n.get('toc-props-id'), webgis.l10n.get('toc-props-fullname'), webgis.l10n.get('toc-props-minscale'),
                webgis.l10n.get('toc-props-maxscale'), webgis.l10n.get('toc-props-query')].forEach(function (text) {
                    $("<th>").text(text).appendTo($head);
                });

                for (const layer of layers.filter(function (l) { return l.service === service; })) {
                    const $tr = $("<tr>").appendTo($table);
                    Ctor._appendCopyButton($("<td>").text(layer.layerId).appendTo($tr), layer.layerId);
                    Ctor._appendCopyButton($("<td>").text(layer.layer ? layer.layer.name : '').appendTo($tr), layer.layer ? layer.layer.name : '');
                    $("<td>").text(layer.layer && layer.layer.minscale > 1 ? '1:' + Ctor.formatScale(layer.layer.minscale) : '').appendTo($tr);
                    $("<td>").text(layer.layer && layer.layer.maxscale > 1 ? '1:' + Ctor.formatScale(layer.layer.maxscale) : '').appendTo($tr);
                    Ctor._appendCopyButton($("<td>").text(layer.query ? layer.query.id : '').appendTo($tr), layer.query ? layer.query.id : '');
                }
            }
        },
        groupCheckBoxClicked: function (sender, e) {
            const $ = webgis.$ || jQuery, Ctor = $.fn.webgis_presentationToc.plugin;
            e.stopPropagation();

            let $icon = $(sender), $collapsable = $icon.closest('.webgis-presentation_toc-item-group');
            let $parent = $collapsable.closest('.webgis-presentation_toc-holder');
            let state = $collapsable.data('checkbox-state');

            let map = $parent.length === 1 ? $parent.get(0)._map : null;

            webgis.log(state);

            $collapsable.find('.webgis-presentation_toc-item').each(function (j, e) {
                let $e = $(e);

                if (map && $e.hasClass('checkbox') && e.presIds) {
                    for (let i = 0; i < e.presIds.length; i++) {
                        let p = Ctor.findItem(map, e.presIds[i]);
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

            Ctor.checkContainerVisibility($parent);
        },
        collapseAll: function (parent, options) {
            const $ = webgis.$ || jQuery;
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
        },
    }
});

// static helper for callers outside this file (no TOC instance needed),
// e.g. the legend button of the tabs header: webgis.ui.showServicesLegend({ map: map, sender: this })
webgis.ui.showServicesLegend = function (options) {
    (webgis.$ || jQuery).fn.webgis_presentationToc.plugin.showServicesLegend(options);
};

// legacy global: used from webgis.tools.js, the map context menu, the Portal (webgis-map.js)
// and possibly from custom code
(function ($) {
    "use strict";
    const plugin = $.fn.webgis_presentationToc.plugin;

    $.presentationToc = {
        sequence: 0,
        process: function (sender, ids) { return plugin.process.apply(this, arguments); },
        collectLayers: function (sender, ids) { return plugin.collectLayers.apply(this, arguments); },
        findItem: function (map, id) { return plugin.findItem.apply(this, arguments); },
        checkItemVisibility: function (event, service, parent) { return plugin.checkItemVisibility.apply(this, arguments); },
        checkItemScaleVisibility: function (event, map, parent) { return plugin.checkItemScaleVisibility.apply(this, arguments); },
        checkContainerVisibility: function (parent) { return plugin.checkContainerVisibility.apply(this, arguments); },
        showLegend: function (sender, map, services, title, selectedTab, showContextMenues, onLoaded) { return plugin.showLegend.apply(this, arguments); },
        hideLegend: function () { return plugin.hideLegend.apply(this, arguments); },
        refreshLegend: function (event, map, parent) { return plugin.refreshLegend.apply(this, arguments); },
        _calcDataOrderByServiceOrder: function (service) { return plugin._calcDataOrderByServiceOrder.apply(this, arguments); }
    };
})(webgis.$ || jQuery);

webgis.ui.definePlugin('webgis_serviceOrder', {
    defaults: {
        map: null,
        selected: null,
        opacityValues: null   // null => [0.1, 0.2, 0.25, 0.3, 0.4, 0.5, 0.6, 0.7, 0.75, 0.8, 0.9, 1] (arrays must not be deep merged)
    },
    init: function () {
        const $ = this.$, me = this, options = this.options;
        let $parent = this.$el;
        let map = options.map;
        let serviceIds = map.serviceIds();
        let opacityValues = options.opacityValues || [0.1, 0.2, 0.25, 0.3, 0.4, 0.5, 0.6, 0.7, 0.75, 0.8, 0.9, 1];

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
                .on('change.webgis_service_order', function () {
                    let service = $(this).closest('.webgis-service-order-item').data('service');
                    service.setOpacity(parseFloat($(this).val()));
                });

            for (let o in opacityValues) {
                $("<option value='" + opacityValues[o].toString() + "'>" + Math.round(opacityValues[o] * 100) + "%</option>")
                    .appendTo($opacity)
            }

            let opacityValue = (Math.round(service.opacity * 100)/100.0).toString();
            $opacity.val(opacityValue);
        }

        webgis.require('sortable', function () {
            if (me._destroyed)
                return;

            me._sortable = Sortable.create($list.get(0),
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

                                        webgis.log($container.attr("data-container"), $container.data("serviceGuids"));
                                        const containerServiceGuids = $container.data("serviceGuids")
                                        if (containerServiceGuids && containerServiceGuids.includes(service.guid)) {

                                            //console.log('reset container order', $container.attr("data-container"), $.fn.webgis_presentationToc.plugin._calcDataOrderByServiceOrder(service));
                                            $container.attr("data-order", $.fn.webgis_presentationToc.plugin._calcDataOrderByServiceOrder(service));
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
    },
    destroy: function () {
        if (this._sortable) {
            try { this._sortable.destroy(); } catch (e) { }
            this._sortable = null;
        }
        this.$el.find('*').addBack().off('.webgis_service_order');
        this.$el.empty();
    }
});
