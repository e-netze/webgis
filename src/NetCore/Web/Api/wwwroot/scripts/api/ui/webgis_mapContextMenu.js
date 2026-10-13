// WebGIS map context menu (jQuery plugin webgis_map_contextmenu)
// Context menu of the map, created by webgis.map.js

(function ($) {
    $.fn.webgis_map_contextmenu = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_map_contextmenu');
        }
    };
    var defaults = {
        map: null,
        clickX: 0,
        clickY: 0,
        force: false
    };
    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        },
        hide: function () {
            if ($._webgisMapContextMenu) {
                $._webgisMapContextMenu.css('display', 'none');
            }
        }
    };

    $._webgisMapContextMenu = null;

    var initUI = function (parent, options) {
        if (!options.map)
            return;

        var $parent = parent ? $(parent) : $('body');
        var map = options.map;

        var toolContextVisible = false;
        $('body').find('.webgis-contextmenu.webgis-tool-contextmenu').each(function (i, e) {
            if ($(e).css('display') !== 'none') {
                toolContextVisible = true;
            }
        });
        if (toolContextVisible == true) {
            if (options.force) {
                $('body').find('.webgis-contextmenu.webgis-tool-contextmenu').css('display', 'none');
            } else {
                return;
            }
        }

        if ($._webgisMapContextMenu == null) {
            $._webgisMapContextMenu = $("<div class='webgis-contextmenu icons' style='z-index:9999999;display:none;position:absolute;background:#fff' oncontextmenu='return false;'>")
                .appendTo($parent)
                .click(function () {
                    map.resetShowServiceExcusives();
                    $(null).webgis_map_contextmenu('hide')
                });

            map.events.on(['refresh', 'zoomstart', 'movestart','mousedown'], function (e, sender) {
                $(null).webgis_map_contextmenu('hide');
            });
            map.events.on(['onfocusservices', 'onresestfocusservices'], function (e, sender) {
                _refreshItems();
            });
        }

        $._webgisMapContextMenu.empty();
        var $menu = $("<ul>").addClass('webgis-toolbox-tool-item-group-details')
            .css({ padding: 0, margin: 0, minWidth: '200px' })
            .appendTo($._webgisMapContextMenu);

        var $header = $("<li style='font-weight:bold;padding: 16px 10px 10px 10px'>")
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
            .click(function (e) {
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
            .click(function (e) {
                var removeButton = $(map._webgisContainer).find('.webgis-toolbox-tool-item.remove-queryresults');
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
            .click(function (e) {
                map.resetFocusServices();
            });

        
        if (map.services) {
            $("<li style='font-weight:bold'><i>" + webgis.l10n.get("map-services") + "</i></li>")
                .appendTo($menu);

            var sortedServices = map.sortedServices("name");

            // Background
            var $menuItem = $("<li>")
                .text(webgis.l10n.get("basemaps"))
                .addClass('basemap')
                .css('background-image', 'url(' + webgis.css.imgResource('tile-service.png') + ')')
                .appendTo($menu)
                .click(function (e) {
                    e.stopPropagation();
                    var $this = $(this);

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

            addBasemapSubItems($menu, map);

            // Map Services
            for (var s in sortedServices) {
                var service = sortedServices[s];

                serviceClass = service.id.replace('')
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
                    .css('background-image', 'url(' + webgis.css.imgResource('image-service.png')+ ')')
                    .data('service', service)
                    .appendTo($menu)
                    .click(function (e) {
                        e.stopPropagation();
                        var $this = $(this);
                        var service = $(this).data('service');

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
                .mouseenter(function () {
                    var service = $(this).data('service');
                    map.showServiceExclusive(service.id, 0.08);
                })
                .mouseleave(function () {
                    map.resetShowServiceExcusives();
                });

                addServiceSubItems($menu, service);

                if (!service.hasVisibleLayersInScale()) {
                    $menuItem.addClass('webgis-outofscale');
                }
            }
        }

        $menu.find('li').addClass('webgis-toolbox-tool-item');
        $menu.find('li.webgis-submenu-item').addClass('webgis-menu-item-hidden');

        $._webgisMapContextMenu.css('display', 'inline-block');

        var left = Math.max(0, Math.min($(window).width() - $._webgisMapContextMenu.width() - 10, options.clickX)),
            top = Math.max(0, Math.min($(window).height() - $._webgisMapContextMenu.height() - 10, options.clickY)),
            maxHeight = $(window).height() - top - 10;

        console.log('maxHeight', $(window).height() - top - 20);

        $._webgisMapContextMenu.css({
            left: left,
            top: top,
            maxHeight: maxHeight,
            overflow: 'auto'
        });

        _refreshItems();
        map.ui.refreshUIElements();
    };

    var addBasemapSubItems = function ($menu, map) {
        var sortedServices = map.services; // map.sortedServices("name");

        // Basemaps
        var $basemapOpacityItem = null;

        $menuItem = $("<li>")
            .addClass('webgis-submenu-item service basemap')
            .addClass(!map.currentBasemapServiceId() ? 'selected' : '')
            .text(webgis.l10n.get("no-basemap"))
            //.css('background-image', 'url(' + webgis.css.imgResource('tile-service.png') + ')')
            .css({
                paddingLeft: '48px'
            })
            .appendTo($menu)
            .click(function (e) {
                e.stopPropagation();
                var $this = $(this);
              
                map.setBasemap(null, false);
                addOpacityImageContainer($basemapOpacityItem, null);

                $this.parent().children('.webgis-submenu-item.service.basemap.selected').removeClass('selected');

                // Overlay
                map.setBasemap(null, true);
                $this.parent().children('.webgis-submenu-item.service.basemap.checked').removeClass('checked').addClass('unchecked');

                $this.addClass('selected');
            });

        for (var s in sortedServices) {
            var service = sortedServices[s];
            if (service.isBasemap === true && service.basemapType !== 'overlay') {
                $menuItem = $("<li>")
                    .addClass('webgis-submenu-item service basemap')
                    .addClass(service.id === map.currentBasemapServiceId() ? 'selected' : '')
                    .text(service.name)
                    //.css('background-image', 'url(' + webgis.css.imgResource('tile-service.png') + ')')
                    .css({
                        backgroundImage: 'url(' + service.getSampleTileUrl() + ')',
                        backgroundSize: '41px',
                        backgroundPosition: '0px 0px',
                        paddingLeft: '48px'
                    })
                    .data('service', service)
                    .appendTo($menu)
                    .click(function (e) {
                        e.stopPropagation();
                        var $this = $(this);
                        var service = $this.data('service');

                        map.setBasemap(service.id, false);

                        var basemapService = map.getService(map.currentBasemapServiceId());
                        if (basemapService) {
                            addOpacityImageContainer($basemapOpacityItem, basemapService);
                        }

                        $this.parent().children('.webgis-submenu-item.service.basemap.selected').removeClass('selected');
                        $this.addClass('selected');
                    });
            }
        }

        // Overlay Basemaps
        for (var s in sortedServices) {
            var service = sortedServices[s];
            if (service.isBasemap === true && service.basemapType === 'overlay') {
                $menuItem = $("<li>")
                    .addClass('webgis-submenu-item service basemap')
                    .addClass(map.currentBasemapOverlayServiceIds().includes(service.id) ? 'checked' : 'unchecked')
                    .css({
                        backgroundPosition: '13px center',
                        paddingLeft: '48px'
                    })
                    .text(service.name)
                    .data('service', service)
                    .appendTo($menu)
                    .click(function (e) {
                        e.stopPropagation();
                        var $this = $(this);

                        if ($this.hasClass('checked')) {
                            map.setBasemap(null, true);
                            $this.removeClass('checked').addClass('unchecked');
                        } else {
                            var service = $this.data('service');
                            map.setBasemap(service.id, true);
                            $this.removeClass('unchecked').addClass('checked');
                        }
                    });
            }
        }

        $basemapOpacityItem = $("<li>")
            .text(webgis.l10n.get("opacity"))
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_0-26.png', 'tools') + ')')
            .addClass('webgis-submenu-item service basemap')
            .data('service', service)
            .appendTo($menu);

        addOpacityImageContainer($basemapOpacityItem, map.getService(map.currentBasemapServiceId()));
    };

    var addServiceSubItems = function ($menu, service) {
        if (!service || !service.map)
            return;

        $("<li>")
            .text(webgis.l10n.get("remove-service"))
            .css('background-image', 'url(' + webgis.css.imgResource('remove.png', 'tools') + ')')
            .addClass('webgis-submenu-item service')
            .addClass(service.guid)
            .data('service', service)
            .appendTo($menu)
            .click(function () {
                var service = $(this).data('service');
                webgis.confirm({
                    message: webgis.l10n.get('confirm-remove-service'),
                    cancelText: webgis.l10n.get('confirm-remove-service-cancel'),
                    okText: webgis.l10n.get('confirm-remove-service-ok'),
                    onOk: function () {
                        service.map.removeServices([service.id]);
                    }
                });
            })

        $("<li>")
            .text(webgis.l10n.get("service-order"))
            .css('background-image', 'url(' + webgis.css.imgResource('rest/toolresource/webgis-tools-serviceorder-service_order', 'tools') + ')')
            .addClass('webgis-submenu-item service')
            .addClass(service.guid)
            .data('service', service)
            .appendTo($menu)
            .click(function () {
                var service = $(this).data('service');
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
            .click(function () {
                var service = $(this).data('service');
                $.presentationToc.showLegend(this, service.map, [service], service.name);
            });

        var $focusItem = $("<li>")
            .text(webgis.l10n.get("focus-service"))
            .css('background-image', 'url(' + webgis.css.imgResource('focus-26.png', 'tools') + ')')
            .addClass('webgis-submenu-item service')
            .addClass(service.guid)
            .appendTo($menu)
        addFocusImageContainer($focusItem, service);

        var $opacityItem = $("<li>")
            .text(webgis.l10n.get("opacity"))
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_0-26.png', 'tools') + ')')
            .addClass('webgis-submenu-item service')
            .addClass(service.guid)
            .data('service', service)
            .appendTo($menu);
        addOpacityImageContainer($opacityItem, service);
    };

    var addOpacityImageContainer = function ($parent, service) {
        if (!service) {
            $parent.css('display', 'none');
            return;
        }

        $parent
            .addClass('webgis-menu-imagebutton-container-holder')
            .css('display', '');

        $parent.webgis_opacity_control({ service: service });
    };

    var addFocusImageContainer = function ($parent, service) {
        $parent.webgis_focusservice_control({ service: service });
    };

    var _refreshItems = function () {
        if (!$._webgisMapContextMenu)
            return;

        $($._webgisMapContextMenu).find('.webgis-toolbox-tool-item.service-item').each(function (i, item) {
            var $item = $(item);

            var service = $item.data('service');
            if (!service || !service.map)
                return;

            var focusedServices = service.map.focusedServices();
            var imgUrl = webgis.css.imgResource('image-service.png');
            if (focusedServices && focusedServices.ids && $.inArray(service.id, focusedServices.ids) >= 0) {
                imgUrl = webgis.css.imgResource('focus-26.png', 'tools');
            }

            $item.css('background-image', 'url(' + imgUrl + ')')
        });
    };
})(webgis.$ || jQuery);
