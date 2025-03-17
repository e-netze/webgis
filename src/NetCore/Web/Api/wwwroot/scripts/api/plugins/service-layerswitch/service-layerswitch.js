(function ($) {
    $.fn.webgis_layerswitch = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_layerswitch');
        }
    };

    var defaults = {
        map: null,
        service: null,
        presentations: []
    };


    var methods = {
        init: function (options) {
            var options = $.extend({}, defaults, options);

            return this.each(function () {
                new initUI(this, options);
            });
        },
        setIndex: function (options) {
            var index = options.index;

            var $this = $(this),
                map = $this.data('options').map,
                service = $this.data('options').service;

            var $selected = null;
            $this.find('.webgis-plugin-service-layerswitch-presentation')
                .each(function (i, e) {
                    if ($(e).data('index') === index) {
                        $selected = $(e).addClass('selected');
                    } else {
                        $(e).removeClass('selected');
                    }
                });

            if ($selected) {
                var presentation = $selected.data('presentation');
                $this
                    .children('.webgis-plugin-service-layerswitch-title')
                    .text(presentation.name);

                service.setServiceVisibility(presentation.layers);
            }
        }
    };

    var initUI = function (elem, options) {
        var $elem = $(elem)
                        .data('options', options)
                        .addClass('webgis-plugin-service-layerswitch');

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

        $.each(options.presentations, function (i, presentation) {
            $("<li>")
                .addClass('webgis-plugin-service-layerswitch-presentation')
                .data('presentation', presentation)
                .data('index', i)
                .text(presentation.name)
                .appendTo($ul)
                .click(function (e) {
                    e.stopPropagation();
                    $(this)
                        .parent()
                        .removeClass('expanded')
                        .closest('.webgis-plugin-service-layerswitch')
                        .webgis_layerswitch('setIndex', { index: $(this).data('index') });
                });
        });

        options.map.events.on('showhourglass', function (e, sender, names) {
            if ($.inArray(options.service.name, names) >= 0) {
                $elem.addClass('loading');
            } else {
                $elem.removeClass('loading');
            }
        }, $elem);
        options.map.events.on('hidehourglass', function (e, sender) {
            $elem.removeClass('loading');
        }, $elem);
    }

})(webgis.$ || jQuery);