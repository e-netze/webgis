// WebGIS service controls (jQuery plugins webgis_opacity_control, webgis_focusservice_control)
// Opacity slider and focus option of a map service, used by the TOC, the static overlay control
// and the UI builder element opacity-control

(function ($) {
    $.fn.webgis_opacity_control = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_opacity_control');
        }
    };
    var defaults = {
        service: null
    };
    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        },
        refresh: function (options) {
            return this.each(function () {
                new refresh(this);
            });
        }
    };

    $.webgis_opacity_control_globals = [];

    var initUI = function (parent, options) {
        var $parent = $(parent);

        $parent
            .children('.webgis-menu-item-imagebutton-container.opacity')
            .remove();

        var service = getService(options.service);
        if (!service || !service.map)
            return;

        var map = service.map;

        if (!$.webgis_opacity_control_globals[map.guid]) {
            $.webgis_opacity_control_globals[map.guid] = map;
            map.events.on(['onfocusservices', 'onresestfocusservices'], function (e, sender) {
                checkMapFocusSettings();
            });
        }

        var $opacityItemButtonContainer = $("<div>")
            .data('service', options.service)
            .addClass('webgis-opacity-control-holder webgis-menu-item-imagebutton-container opacity')
            .appendTo($parent);

        $("<div>")
            .addClass('webgis-menu-item-imagebutton webgis-dependencies webgis-dependency-focused-service-exists')
            .css('background-image', 'url(' + webgis.css.imgResource('focus_remove-26.png', 'tools') + ')')
            .data('map', map)
            .appendTo($opacityItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                var map = $(this).data('map');
                map.resetFocusServices();
            });

        $("<div>")
            .addClass('webgis-menu-item-imagebutton 25')
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_25-26.png', 'tools') + ')')
            .appendTo($opacityItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                setServiceOpacity(this, 0.25);
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton 50')
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_50-26.png', 'tools') + ')')
            .appendTo($opacityItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                setServiceOpacity(this, 0.50);
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton 75')
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_75-26.png', 'tools') + ')')
            .appendTo($opacityItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                setServiceOpacity(this, 0.75);
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton 100')
            .css('background-image', 'url(' + webgis.css.imgResource('opacity_100-26.png', 'tools') + ')')
            .appendTo($opacityItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                setServiceOpacity(this, 1);
            });

        $opacityItemButtonContainer
            .find('.webgis-menu-item-imagebutton.' + Math.round(service.getOpacity() * 100))
            .addClass('selected');

        checkMapFocusSettings();
        map.ui.refreshUIElements();
    };

    var setServiceOpacity = function (button, opacity) {
        const $button = $(button), $holder = $button.closest('.webgis-opacity-control-holder');
        if ($button.hasClass('disabled'))
            return;

        const service = getService($holder.data('service'));

        applyServiceOpacity(service, opacity);

        $button.parent().children().removeClass('selected');
        $button.addClass('selected');
    };

    var applyServiceOpacity = function (service, opacity) {
        if (service) {
            service.setOpacity(opacity);
            if (service.map && service.isBasemap === true) {
                for (let overlayId of service.map.currentBasemapOverlayServiceIds()) {
                    const overlayService = service.map.getService(overlayId);
                    if (overlayService) {
                        overlayService.setOpacity(opacity);
                    }
                }
            }
        }
    };

    var getService = function (service) {
        if (typeof service === "function") {
            service = service();
        };

        return service;
    };

    var checkMapFocusSettings = function () {
        refresh(null);
    };

    var refresh = function (elem) {
        var $elem = $(elem || 'body');
        if ($elem.hasClass('webgis-opacity-control-holder') === false) {
            $elem = $elem.find('.webgis-opacity-control-holder');
        }

        $elem.each(function (i, e) {
            var $holder = $(e);
            var service = getService($holder.data('service'));

            if (service && service.map) {
                var focusedServices = service.map.focusedServices();

                if (focusedServices) {
                    $holder.css('opacity', .8);

                    var serviceOpacity = focusedServices.ids && $.inArray(service.id, focusedServices.ids) >= 0 ? 1.0 : focusedServices.opacity;

                    $holder.children('.webgis-menu-item-imagebutton')
                        .css('display', 'none')
                        .removeClass('selected');
                    $holder.children('.webgis-menu-item-imagebutton.' + Math.round(serviceOpacity * 100))
                        .addClass('disabled')
                        .css('display', '')
                } else {
                    $holder.removeClass('disabled').css('opacity', '');

                    $holder.children('.webgis-menu-item-imagebutton')
                        .css('display', '')
                        .removeClass('disabled')
                        .removeClass('selected');
                    $holder.children('.webgis-menu-item-imagebutton.' + service.getOpacity() * 100)
                        .addClass('selected');

                    if (service.isBasemap) {
                        applyServiceOpacity(service, service.getOpacity());  // set all overlay basemap to the same opacity
                    }
                }
            }
        });
    }
})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_focusservice_control = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_focusservice_control');
        }
    };
    var defaults = {
        service: null
    };

    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        }
    };

    var initUI = function (parent, options) {
        var $parent = $(parent);
        var service = options.service;
        if (!service)
            return;

        $parent
            .addClass('webgis-menu-imagebutton-container-holder')
            .children('.webgis-menu-item-imagebutton-container.focus')
            .remove();

        var $focusItemButtonContainer = $("<div>")
            .addClass('webgis-menu-item-imagebutton-container focus')
            .appendTo($parent);

        $("<div>")
            .addClass('webgis-menu-item-imagebutton webgis-dependencies webgis-dependency-focused-service-exists')
            .css('background-image', 'url(' + webgis.css.imgResource('focus_remove-26.png', 'tools') + ')')
            .data('service', service)
            .appendTo($focusItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                var service = $(this).data('service');
                service.map.resetFocusServices();
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton')
            .data('service', service)
            .css('background-image', 'url(' + webgis.css.imgResource('focus_75-26.png', 'tools') + ')')
            .appendTo($focusItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                var service = $(this).data('service');
                service.map.focusServices(service.id, 0.75);
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton')
            .data('service', service)
            .css('background-image', 'url(' + webgis.css.imgResource('focus_50-26.png', 'tools') + ')')
            .appendTo($focusItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                var service = $(this).data('service');
                service.map.focusServices(service.id, 0.5);
            });
        $("<div>")
            .addClass('webgis-menu-item-imagebutton')
            .data('service', service)
            .css('background-image', 'url(' + webgis.css.imgResource('focus_25-26.png', 'tools') + ')')
            .appendTo($focusItemButtonContainer)
            .click(function (e) {
                e.stopPropagation();
                var service = $(this).data('service');
                service.map.focusServices(service.id, 0.25);
            });
        
    };
})(webgis.$ || jQuery);
