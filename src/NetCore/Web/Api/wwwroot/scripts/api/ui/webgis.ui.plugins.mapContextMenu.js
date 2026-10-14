// WebGIS UI Plugins (map context menu)
// webgis_map_contextmenu
// Context menu of the map (right click, created by webgis.map.js). The menu itself is one global
// singleton ($._webgisMapContextMenu) appended to the element the plugin is called on (body).

webgis.ui.definePlugin('webgis_map_contextmenu', {
    reinit: true,   // every call with options (re)builds and shows the singleton menu
    defaults: {
        map: null,
        clickX: 0,
        clickY: 0,
        force: false
    },
    init: function () {
        const $ = this.$, o = this.options, Ctor = this.constructor;
        if (!o.map)
            return;

        const map = o.map;

        let toolContextVisible = false;
        $('body').find('.webgis-contextmenu.webgis-tool-contextmenu').each(function (i, e) {
            if ($(e).css('display') !== 'none') {
                toolContextVisible = true;
            }
        });
        if (toolContextVisible == true) {
            if (o.force) {
                $('body').find('.webgis-contextmenu.webgis-tool-contextmenu').css('display', 'none');
            } else {
                return;
            }
        }

        const $contextMenu = Ctor._getOrCreateMenu(this.$el, map);

        $contextMenu.empty();
        const $menu = $("<ul>").addClass('webgis-toolbox-tool-item-group-details')
            .css({ padding: 0, margin: 0, minWidth: '200px' })
            .appendTo($contextMenu);

        const $header = $("<li style='font-weight:bold;padding: 16px 10px 10px 10px'>")
            .addClass('webgis-menu-item-title')
            .appendTo($menu);
        $("<i>").text(map.name).appendTo($header);
        $("<div>").addClass('webgis-close-button').appendTo($header);

        //  Remove All Visfilters
        $("<li>")
            .text('Alle Darstellungsfilter entfernen')
            .css('background-image', 'url(' + webgis.css.imgResource('filter-remove-26.png', 'tools') + ')')
            .addClass('webgis-tool-button webgis-dependencies webgis-dependency-hasfilters')
            .appendTo($menu)
            .on('click.webgis_map_contextmenu', function (e) {
                map.unsetAllFilters();
                map.refresh();
                map.ui.refreshUIElements();
            });

        //  Remove all queryresults
        $("<li>")
            .text('Alle (Such)ergebnisse aus der Karte entfernen')
            .css('background-image', 'url(' + webgis.css.imgResource('marker-remove-26.png', 'tools') + ')')
            .addClass('webgis-tool-button webgis-dependencies webgis-dependency-queryresultsexists')
            .appendTo($menu)
            .on('click.webgis_map_contextmenu', function (e) {
                const removeButton = $(map._webgisContainer).find('.webgis-toolbox-tool-item.remove-queryresults');
                if (removeButton.length > 0) {
                    removeButton.click();
                } else {
                    map.queryResultFeatures.clear(false);
                    map.getSelection('selection').remove();
                }
                map.unloadDynamicContent();
            });

        // reset (service) focus
        $("<li>")
            .text('Focus auf Dienst entfernen')
            .css('background-image', 'url(' + webgis.css.imgResource('focus_remove-26.png', 'tools') + ')')
            .addClass('webgis-tool-button webgis-dependencies webgis-dependency-focused-service-exists')
            .appendTo($menu)
            .on('click.webgis_map_contextmenu', function (e) {
                map.resetFocusServices();
            });

        if (map.services) {
            $("<li style='font-weight:bold'><i>" + webgis.l10n.get("map-services") + "</i></li>")
                .appendTo($menu);

            const sortedServices = map.sortedServices("name");

            // Background
            let $menuItem = $("<li>")
                .text(webgis.l10n.get("basemaps"))
                .addClass('basemap')
                .css('background-image', 'url(' + webgis.css.imgResource('tile-service.png') + ')')
                .appendTo($menu)
                .on('click.webgis_map_contextmenu', function (e) {
                    e.stopPropagation();
                    const $this = $(this);

                    if ($this.hasClass('subitems-collapsed')) {
                        $this.removeClass('subitems-collapsed');
                        $this.parent().children().css('display', 'block').removeClass('webgis-menu-item-hidden');
                        $this.parent().find('li.webgis-submenu-item').addClass('webgis-menu-item-hidden');
                        map.ui.refreshUIElements();
                    } else {
                        $this.addClass('subitems-collapsed');
                        $this.parent().children('li').addClass('webgis-menu-item-hidden');
                        $this.parent().children('.webgis-menu-item-title').removeClass('webgis-menu-item-hidden');
                        $this.parent().children('.basemap').removeClass('webgis-menu-item-hidden');
                    }
                });

            Ctor._addBasemapSubItems($menu, map);

            // Map Services
            for (let s in sortedServices) {
                const service = sortedServices[s];

                if (service.isBasemap === true) {
                    continue;
                }

                if (service.isWatermark()) {
                    continue;
                }

                $menuItem = $("<li>")
                    .addClass(service.guid)
                    .addClass('service-item')
                    .text(service.name)
                    .css('background-image', 'url(' + webgis.css.imgResource('image-service.png') + ')')
                    .data('service', service)
                    .appendTo($menu)
                    .on('click.webgis_map_contextmenu', function (e) {
                        e.stopPropagation();
                        const $this = $(this);
                        const service = $this.data('service');

                        if ($this.hasClass('subitems-collapsed')) {
                            $this.removeClass('subitems-collapsed');
                            $this.parent().children().css('display', 'block').removeClass('webgis-menu-item-hidden');
                            $this.parent().find('li.webgis-submenu-item').addClass('webgis-menu-item-hidden');
                            map.ui.refreshUIElements();
                        } else {
                            $this.addClass('subitems-collapsed');
                            $this.parent().children('li').addClass('webgis-menu-item-hidden');
                            $this.parent().children('.webgis-menu-item-title').removeClass('webgis-menu-item-hidden');
                            $this.parent().children('.' + service.guid).removeClass('webgis-menu-item-hidden');
                        }
                    })
                    .on('mouseenter.webgis_map_contextmenu', function () {
                        const service = $(this).data('service');
                        map.showServiceExclusive(service.id, 0.08);
                    })
                    .on('mouseleave.webgis_map_contextmenu', function () {
                        map.resetShowServiceExcusives();
                    });

                Ctor._addServiceSubItems($menu, service);

                if (!service.hasVisibleLayersInScale()) {
                    $menuItem.addClass('webgis-outofscale');
                }
            }
        }

        $menu.find('li').addClass('webgis-toolbox-tool-item');
        $menu.find('li.webgis-submenu-item').addClass('webgis-menu-item-hidden');

        $contextMenu.css('display', 'inline-block');

        const left = Math.max(0, Math.min($(window).width() - $contextMenu.width() - 10, o.clickX)),
            top = Math.max(0, Math.min($(window).height() - $contextMenu.height() - 10, o.clickY)),
            maxHeight = $(window).height() - top - 10;

        webgis.log('maxHeight', $(window).height() - top - 20);

        $contextMenu.css({
            left: left,
            top: top,
            maxHeight: maxHeight,
            overflow: 'auto'
        });

        Ctor.refreshItems();
        map.ui.refreshUIElements();
    },
    destroy: function () {
        // nothing to release on $el: the menu is a global singleton ($._webgisMapContextMenu),
        // its items are rebuilt (.empty()) by the next init
    },
    methods: {
        hide: function () {
            this.constructor.hide();
        }
    },
    staticMethods: {
        // no instance needed, from outside: webgis.ui.hideMapContextMenu()
        hide: function () {
            const $ = webgis.$ || jQuery;
            if ($._webgisMapContextMenu) {
                $._webgisMapContextMenu.css('display', 'none');
            }
        },
        // creates the singleton menu once (and subscribes the map events once, for the first map)
        _getOrCreateMenu: function ($parent, map) {
            const $ = webgis.$ || jQuery, Ctor = this;

            if ($._webgisMapContextMenu == null) {
                $._webgisMapContextMenu = $("<div class='webgis-contextmenu icons' style='z-index:9999999;display:none;position:absolute;background:#fff' oncontextmenu='return false;'>")
                    .appendTo($parent)
                    .on('click.webgis_map_contextmenu', function () {
                        map.resetShowServiceExcusives();
                        Ctor.hide();
                    });

                map.events.on(['refresh', 'zoomstart', 'movestart', 'mousedown'], function (e, sender) {
                    Ctor.hide();
                });
                map.events.on(['onfocusservices', 'onresestfocusservices'], function (e, sender) {
                    Ctor.refreshItems();
                });
            }

            return $._webgisMapContextMenu;
        },
        _addBasemapSubItems: function ($menu, map) {
            const $ = webgis.$ || jQuery, Ctor = this;
            const sortedServices = map.services; // map.sortedServices("name");

            // Basemaps
            let $basemapOpacityItem = null;
            let service;

            $("<li>")
                .addClass('webgis-submenu-item service basemap')
                .addClass(!map.currentBasemapServiceId() ? 'selected' : '')
                .text(webgis.l10n.get("no-basemap"))
                .css({
                    paddingLeft: '48px'
                })
                .appendTo($menu)
                .on('click.webgis_map_contextmenu', function (e) {
                    e.stopPropagation();
                    const $this = $(this);

                    map.setBasemap(null, false);
                    Ctor._addOpacityImageContainer($basemapOpacityItem, null);

                    $this.parent().children('.webgis-submenu-item.service.basemap.selected').removeClass('selected');

                    // Overlay
                    map.setBasemap(null, true);
                    $this.parent().children('.webgis-submenu-item.service.basemap.checked').removeClass('checked').addClass('unchecked');

                    $this.addClass('selected');
                });

            for (let s in sortedServices) {
                service = sortedServices[s];
                if (service.isBasemap === true && service.basemapType !== 'overlay') {
                    $("<li>")
                        .addClass('webgis-submenu-item service basemap')
                        .addClass(service.id === map.currentBasemapServiceId() ? 'selected' : '')
                        .text(service.name)
                        .css({
                            backgroundImage: 'url(' + service.getSampleTileUrl() + ')',
                            backgroundSize: '41px',
                            backgroundPosition: '0px 0px',
                            paddingLeft: '48px'
                        })
                        .data('service', service)
                        .appendTo($menu)
                        .on('click.webgis_map_contextmenu', function (e) {
                            e.stopPropagation();
                            const $this = $(this);
                            const service = $this.data('service');

                            map.setBasemap(service.id, false);

                            const basemapService = map.getService(map.currentBasemapServiceId());
                            if (basemapService) {
                                Ctor._addOpacityImageContainer($basemapOpacityItem, basemapService);
                            }

                            $this.parent().children('.webgis-submenu-item.service.basemap.selected').removeClass('selected');
                            $this.addClass('selected');
                        });
                }
            }

            // Overlay Basemaps
            for (let s in sortedServices) {
                service = sortedServices[s];
                if (service.isBasemap === true && service.basemapType === 'overlay') {
                    $("<li>")
                        .addClass('webgis-submenu-item service basemap')
                        .addClass(map.currentBasemapOverlayServiceIds().includes(service.id) ? 'checked' : 'unchecked')
                        .css({
                            backgroundPosition: '13px center',
                            paddingLeft: '48px'
                        })
                        .text(service.name)
                        .data('service', service)
                        .appendTo($menu)
                        .on('click.webgis_map_contextmenu', function (e) {
                            e.stopPropagation();
                            const $this = $(this);

                            if ($this.hasClass('checked')) {
                                map.setBasemap(null, true);
                                $this.removeClass('checked').addClass('unchecked');
                            } else {
                                const service = $this.data('service');
                                map.setBasemap(service.id, true);
                                $this.removeClass('unchecked').addClass('checked');
                            }
                        });
                }
            }

            // legacy: data 'service' is the last service of the loops above
            $basemapOpacityItem = $("<li>")
                .text(webgis.l10n.get("opacity"))
                .css('background-image', 'url(' + webgis.css.imgResource('opacity_0-26.png', 'tools') + ')')
                .addClass('webgis-submenu-item service basemap')
                .data('service', service)
                .appendTo($menu);

            Ctor._addOpacityImageContainer($basemapOpacityItem, map.getService(map.currentBasemapServiceId()));
        },
        _addServiceSubItems: function ($menu, service) {
            const $ = webgis.$ || jQuery, Ctor = this;
            if (!service || !service.map)
                return;

            $("<li>")
                .text(webgis.l10n.get("remove-service"))
                .css('background-image', 'url(' + webgis.css.imgResource('remove.png', 'tools') + ')')
                .addClass('webgis-submenu-item service')
                .addClass(service.guid)
                .data('service', service)
                .appendTo($menu)
                .on('click.webgis_map_contextmenu', function () {
                    const service = $(this).data('service');
                    webgis.confirm({
                        message: webgis.l10n.get('confirm-remove-service'),
                        cancelText: webgis.l10n.get('confirm-remove-service-cancel'),
                        okText: webgis.l10n.get('confirm-remove-service-ok'),
                        onOk: function () {
                            service.map.removeServices([service.id]);
                        }
                    });
                });

            $("<li>")
                .text(webgis.l10n.get("service-order"))
                .css('background-image', 'url(' + webgis.css.imgResource('rest/toolresource/webgis-tools-serviceorder-service_order', 'tools') + ')')
                .addClass('webgis-submenu-item service')
                .addClass(service.guid)
                .data('service', service)
                .appendTo($menu)
                .on('click.webgis_map_contextmenu', function () {
                    const service = $(this).data('service');
                    webgis.modalDialog(webgis.l10n.get("service-order"),
                        function ($context) {
                            $context.webgis_serviceOrder({ map: service.map, selected: service.id });
                        });
                });

            $("<li>")
                .text(webgis.l10n.get("legend"))
                .css('background-image', 'url(' + webgis.css.imgResource('legend.png', 'tools') + ')')
                .addClass('webgis-submenu-item service')
                .addClass(service.guid)
                .data('service', service)
                .appendTo($menu)
                .on('click.webgis_map_contextmenu', function () {
                    const service = $(this).data('service');
                    $.presentationToc.showLegend(this, service.map, [service], service.name);
                });

            const $focusItem = $("<li>")
                .text(webgis.l10n.get("focus-service"))
                .css('background-image', 'url(' + webgis.css.imgResource('focus-26.png', 'tools') + ')')
                .addClass('webgis-submenu-item service')
                .addClass(service.guid)
                .appendTo($menu);
            Ctor._addFocusImageContainer($focusItem, service);

            const $opacityItem = $("<li>")
                .text(webgis.l10n.get("opacity"))
                .css('background-image', 'url(' + webgis.css.imgResource('opacity_0-26.png', 'tools') + ')')
                .addClass('webgis-submenu-item service')
                .addClass(service.guid)
                .data('service', service)
                .appendTo($menu);
            Ctor._addOpacityImageContainer($opacityItem, service);
        },
        _addOpacityImageContainer: function ($parent, service) {
            if (!service) {
                $parent.css('display', 'none');
                return;
            }

            $parent
                .addClass('webgis-menu-imagebutton-container-holder')
                .css('display', '');

            $parent.webgis_opacity_control({ service: service });
        },
        _addFocusImageContainer: function ($parent, service) {
            $parent.webgis_focusservice_control({ service: service });
        },
        refreshItems: function () {
            const $ = webgis.$ || jQuery;
            if (!$._webgisMapContextMenu)
                return;

            $($._webgisMapContextMenu).find('.webgis-toolbox-tool-item.service-item').each(function (i, item) {
                const $item = $(item);

                const service = $item.data('service');
                if (!service || !service.map)
                    return;

                const focusedServices = service.map.focusedServices();
                let imgUrl = webgis.css.imgResource('image-service.png');
                if (focusedServices && focusedServices.ids && $.inArray(service.id, focusedServices.ids) >= 0) {
                    imgUrl = webgis.css.imgResource('focus-26.png', 'tools');
                }

                $item.css('background-image', 'url(' + imgUrl + ')');
            });
        }
    }
});

// static helper for callers outside this file (no plugin instance needed)
webgis.ui.hideMapContextMenu = function () {
    (webgis.$ || jQuery).fn.webgis_map_contextmenu.plugin.hide();
};

(webgis.$ || jQuery)._webgisMapContextMenu = null;
