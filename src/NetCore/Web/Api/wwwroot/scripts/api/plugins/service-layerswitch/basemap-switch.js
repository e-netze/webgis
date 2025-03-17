(function ($) {
    $.fn.webgis_basemapswitch = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_basemapswitch');
        }
    };

    var defaults = {
        map: null,
        service: null
    };


    var methods = {
        init: function (options) {
            var options = $.extend({}, defaults, options);

            return this.each(function () {
                new initUI(this, options);
            });
        },
        setServiceId: function (options) {
            var serviceId = options.serviceId;

            var $this = $(this),
                map = $this.data('options').map;

            var $selected = null;
            $this.find('.webgis-plugin-service-layerswitch-presentation')
                .each(function (i, e) {
                    if ($(e).data('serviceId') === serviceId) {
                        $selected = $(e).addClass('selected');
                    } else {
                        $(e).removeClass('selected');
                    }
                });

            if ($selected) {
                $this
                    .children('.webgis-plugin-service-layerswitch-title')
                    .text($selected.text());

                //service.setServiceVisibility(presentation.layers);
            }

            if ($this.data('options').onChange)
                $this.data('options').onChange(this, serviceId);
        },
        hideServiceIds: function (options) {
            var $this = $(this);
            $this
                .find('.webgis-plugin-service-layerswitch-presentation')
                .removeClass('hidden');

            if (options.serviceIds) {
                $this
                    .find('.webgis-plugin-service-layerswitch-presentation')
                    .each(function (i, e) {
                        if ($.inArray($(e).data('serviceId'), options.serviceIds) >= 0) {
                            $(e).addClass('hidden');
                        }
                    });
            }
        }
    };

    var initUI = function (elem, options) {
        var $elem = $(elem)
                        .data('options', options)
                        .addClass('webgis-plugin-service-layerswitch');

        var map = options.map;

        $("<div>")
            .addClass('webgis-plugin-service-layerswitch-title')
            .appendTo($elem)
            .click(function (e) {
                e.stopPropagation();
                $(this)
                    .parent()
                    .children('.webgis-plugin-service-layerswitch-presentations')
                    .toggleClass('expanded');
            });

        var $ul = $("<ul>")
            .addClass('webgis-plugin-service-layerswitch-presentations')
            .appendTo($elem);

        $.each(map.serviceIds(), function (i, serviceId) {
            var service = map.getService(serviceId);
            if (service && service.isBasemap) {
                $("<li>")
                    .addClass('webgis-plugin-service-layerswitch-presentation')
                    .data('serviceId', serviceId)
                    .text(service.name)
                    .appendTo($ul)
                    .click(function (e) {
                        e.stopPropagation();
                        $(this)
                            .parent()
                            .removeClass('expanded')
                            .closest('.webgis-plugin-service-layerswitch')
                            .webgis_basemapswitch('setServiceId', { serviceId: $(this).data('serviceId') });
                    });
            }
        });
    }

})(webgis.$ || jQuery);