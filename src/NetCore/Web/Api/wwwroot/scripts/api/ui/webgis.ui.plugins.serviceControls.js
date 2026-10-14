// WebGIS UI Plugins (service controls)
// webgis_opacity_control, webgis_focusservice_control
// Opacity and focus buttons of a map service, used by the TOC, the map context menu,
// the static overlay control and the UI builder element opacity-control (webgis.ui.builder.misc.js)

webgis.ui.definePlugin('webgis_opacity_control', {
    reinit: true,   // calling it again on the same parent replaces the buttons
    defaults: {
        service: null   // service or function returning the service (eg. current basemap)
    },
    init: function () {
        const $ = this.$, me = this;
        const service = this.constructor.getService(this.options.service);
        if (!service || !service.map)
            return;

        const map = service.map;
        this.constructor.subscribeMap(map);

        const $holder = this._$holder = $("<div>")
            .data('service', this.options.service)
            .addClass('webgis-opacity-control-holder webgis-menu-item-imagebutton-container opacity')
            .appendTo(this.$el);

        $("<div>")
            .addClass('webgis-menu-item-imagebutton webgis-dependencies webgis-dependency-focused-service-exists')
            .css('background-image', 'url(' + webgis.css.imgResource('focus_remove-26.png', 'tools') + ')')
            .data('map', map)
            .appendTo($holder)
            .on('click.webgis_opacity_control', function (e) {
                e.stopPropagation();
                map.resetFocusServices();
            });

        [25, 50, 75, 100].forEach(function (percent) {
            $("<div>")
                .addClass('webgis-menu-item-imagebutton ' + percent)
                .css('background-image', 'url(' + webgis.css.imgResource('opacity_' + percent + '-26.png', 'tools') + ')')
                .appendTo($holder)
                .on('click.webgis_opacity_control', function (e) {
                    e.stopPropagation();
                    me._setServiceOpacity($(this), percent / 100);
                });
        });

        $holder
            .find('.webgis-menu-item-imagebutton.' + Math.round(service.getOpacity() * 100))
            .addClass('selected');

        this.constructor.refreshHolders(null);
        map.ui.refreshUIElements();
    },
    destroy: function () {
        if (this._$holder) {
            this._$holder.remove();
            this._$holder = null;
        }
    },
    methods: {
        refresh: function () {
            this.constructor.refreshHolders(this.$el);
        },
        _setServiceOpacity: function ($button, opacity) {
            if ($button.hasClass('disabled'))
                return;

            const service = this.constructor.getService(this.options.service);
            this.constructor.applyServiceOpacity(service, opacity);

            $button.parent().children().removeClass('selected');
            $button.addClass('selected');
        }
    },
    staticMethods: {
        _subscribedMaps: {},
        // one subscription per map (not per instance): focus changes refresh all opacity controls
        subscribeMap: function (map) {
            const subscribedMaps = this._subscribedMaps;
            if (subscribedMaps[map.guid])
                return;

            subscribedMaps[map.guid] = map;
            const Ctor = this;
            map.events.on(['onfocusservices', 'onresestfocusservices'], function () {
                Ctor.refreshHolders(null);
            });
        },
        getService: function (service) {
            return typeof service === 'function' ? service() : service;
        },
        applyServiceOpacity: function (service, opacity) {
            if (!service)
                return;

            service.setOpacity(opacity);
            if (service.map && service.isBasemap === true) {
                for (let overlayId of service.map.currentBasemapOverlayServiceIds()) {
                    const overlayService = service.map.getService(overlayId);
                    if (overlayService) {
                        overlayService.setOpacity(opacity);
                    }
                }
            }
        },
        // elem: holder, element containing holders or null (= all holders in the document)
        refreshHolders: function (elem) {
            const $ = webgis.$ || jQuery, Ctor = this;

            let $elem = $(elem || 'body');
            if ($elem.hasClass('webgis-opacity-control-holder') === false) {
                $elem = $elem.find('.webgis-opacity-control-holder');
            }

            $elem.each(function (i, e) {
                const $holder = $(e);
                const service = Ctor.getService($holder.data('service'));

                if (service && service.map) {
                    const focusedServices = service.map.focusedServices();

                    if (focusedServices) {
                        $holder.css('opacity', .8);

                        const serviceOpacity = focusedServices.ids && $.inArray(service.id, focusedServices.ids) >= 0 ? 1.0 : focusedServices.opacity;

                        $holder.children('.webgis-menu-item-imagebutton')
                            .css('display', 'none')
                            .removeClass('selected');
                        $holder.children('.webgis-menu-item-imagebutton.' + Math.round(serviceOpacity * 100))
                            .addClass('disabled')
                            .css('display', '');
                    } else {
                        $holder.removeClass('disabled').css('opacity', '');

                        $holder.children('.webgis-menu-item-imagebutton')
                            .css('display', '')
                            .removeClass('disabled')
                            .removeClass('selected');
                        $holder.children('.webgis-menu-item-imagebutton.' + service.getOpacity() * 100)
                            .addClass('selected');

                        if (service.isBasemap) {
                            Ctor.applyServiceOpacity(service, service.getOpacity());  // set all overlay basemap to the same opacity
                        }
                    }
                }
            });
        }
    }
});

webgis.ui.definePlugin('webgis_focusservice_control', {
    reinit: true,   // calling it again on the same parent replaces the buttons
    defaults: {
        service: null
    },
    init: function () {
        const $ = this.$;
        const service = this.options.service;
        if (!service)
            return;

        this.$el.addClass('webgis-menu-imagebutton-container-holder');

        const $container = this._$container = $("<div>")
            .addClass('webgis-menu-item-imagebutton-container focus')
            .appendTo(this.$el);

        $("<div>")
            .addClass('webgis-menu-item-imagebutton webgis-dependencies webgis-dependency-focused-service-exists')
            .css('background-image', 'url(' + webgis.css.imgResource('focus_remove-26.png', 'tools') + ')')
            .data('service', service)
            .appendTo($container)
            .on('click.webgis_focusservice_control', function (e) {
                e.stopPropagation();
                service.map.resetFocusServices();
            });

        [75, 50, 25].forEach(function (percent) {
            $("<div>")
                .addClass('webgis-menu-item-imagebutton')
                .data('service', service)
                .css('background-image', 'url(' + webgis.css.imgResource('focus_' + percent + '-26.png', 'tools') + ')')
                .appendTo($container)
                .on('click.webgis_focusservice_control', function (e) {
                    e.stopPropagation();
                    service.map.focusServices(service.id, percent / 100);
                });
        });
    },
    destroy: function () {
        if (this._$container) {
            this._$container.remove();
            this._$container = null;
        }
    }
});
