(function ($) {
    "use strict";
    $.fn.webgis_staticOverlayControl = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_staticOverlayControl');
        }
    };
    var defaults = {
        map: null,
        add_remove: true,
        command_buttons: []
    };
    var methods = {
        init: function (options) {
            var settings = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, settings);
            });
        },
        val: function (options) {
            var $this = $(this);

            console.log('webgis_staticOverlayControl.val', options);

            if (typeof options === 'string') {
                $this.data('webgis-value-element').val(options);

                var $serviceItem = null;
                $this.find('.webgis-static-overlay-control-item').each(function (i, item) {
                    if ($(item).data('service').id === options) {
                        $serviceItem = $(item);
                    }
                });

                if ($serviceItem && !$serviceItem.hasClass('selected')) {
                    $serviceItem.trigger('click');
                }
            }

            return $this.data('webgis-value-element').val();
        }
    };

    var initUI = function (elem, options) {
        var $elem = $(elem).addClass('webgis-static-overlay-control'), map = options.map;

        var $valueElement = $("<input type='hidden' class='webgis-selected-id' />")
            .appendTo($elem);

        $elem.data('webgis-value-element', $valueElement);

        if (!map)
            return;

        var $list = $("<div>")
            .appendTo($elem);

        var serviceIds = map.serviceIds(), overlayServices = [];

        // collect overlay services
        $.each(serviceIds, function (i, serviceId) {
            var service = map.getService(serviceId);
            if (service.type === "static_overlay") {
                overlayServices.push(service)
            }
        });

        // order by zIndex
        overlayServices.sort(function (a, b) {
            if (a.getOrder() == b.getOrder())
                return 0;

            return (a.getOrder() < b.getOrder()) ? -1 : 1;
        }).reverse();

        // create service items
        $.each(overlayServices, function (i, service) {
            addService($list, service, options);
        })

        webgis.require('sortable', function () {
            Sortable.create($list.get(0),
                {
                    animation: 150,
                    ghostClass: 'webgis-sorting',
                    onSort: function (e) {
                        var $items = $list.find('.webgis-static-overlay-control-item');
                        var zIndex = map.maxBackgroundCoveringServiceOrder() + $items.length + 1;
                        $items.each(function (i, item) {
                            var service = $(item).data('service');
                            console.log(service.id);
                            service.setOrder(zIndex--);
                            //service.setOrder(serviceIds.length * 10 - i * 10);
                        });

                    }
                });
        });
    };

    var addService = function ($parent, service, options) {
        console.log('addService', service);

        var $item = $("<div>")
            .addClass('webgis-static-overlay-control-item')
            .data('service', service)
            .appendTo($parent)
            .click(function (e) {
                e.stopPropagation();

                var $this = $(this);

                var $valueControl = $this
                    .closest('.webgis-static-overlay-control')
                    .find('input.webgis-selected-id');

                if ($this.hasClass('selected')) {
                    $this.removeClass('selected');
                    $this.data('service').hidePassPoints();
                    $valueControl.val('');
                } else {
                    var selectedService = $this.parent().children('.selected').data('service');
                    $this.parent().children('.selected').removeClass('selected');
                    if (selectedService) {
                        selectedService.hidePassPoints();
                    }

                    $this.addClass('selected');
                    
                    $valueControl.val($this.data('service').id);
                    $this.data('service').showPassPoints();
                }
            });

        var $tools = $("<div>")
            .addClass('item-tools')
            .appendTo($item);

        $("<div>")
            .addClass('item-tool remove')
            .appendTo($tools)
            .click(function (e) {
                e.stopPropagation();

                var $item = $(this).closest('.webgis-static-overlay-control-item')
                var service = $item.data('service');
                service.map.removeServices([service.id]);
                $item.remove();
            });

        $("<div>")
            .addClass('item-tool topmost' + (service.isTopMost() === true ? ' selected' : ''))
            .appendTo($tools)
            .click(function (e) {
                e.stopPropagation();

                var $this = $(this);
                var $item = $this.closest('.webgis-static-overlay-control-item')
                var service = $item.data('service');

                if ($this.hasClass('selected')) {
                    service.resetOrder();
                    $this.removeClass('selected');
                } else {
                    $item.closest('.webgis-static-overlay-control')
                        .find('.webgis-static-overlay-control-item > .item-tools > .item-tool.topmost')
                        .removeClass('selected');

                    service.bringToTop();
                    $this.addClass('selected');
                }
            });

        $.each(options.command_buttons, function (i, button) {
            $("<div>")
                .addClass('item-tool')
                .data('button', button)
                .css('background-image', 'url(' + webgis.css.imgResource(button.icon) + ')')
                .appendTo($tools)
                .click(function (e) {
                    e.stopPropagation();

                    var $item = $(this).closest('.webgis-static-overlay-control-item')
                    var service = $item.data('service');

                    var button = $(this).data('button');

                    var tool = service.map.getActiveTool();
                    var type = service.map.isServerButton(tool.id) ? 'servertoolcommand_ext' : 'servertoolcommand';
                    webgis.tools.onButtonClick(service.map, {
                        command: button.command, type: type, id: tool.id, map: service.map, argument: service.id
                    });
                })
        });

        $("<div>")
            .addClass('item-title')
            .text(service.name)
            .appendTo($item);

        $("<div>")
            .addClass('service-opacity-control')
            .appendTo($item)
            .webgis_opacity_control({ service: service });
    };
})(webgis.$ || jQuery);