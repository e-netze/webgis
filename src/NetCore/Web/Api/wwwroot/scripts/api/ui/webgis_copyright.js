(function ($) {
    "use strict";
    $.fn.webgis_copyright = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_copyright');
        }
    };
    var defaults = {
        map: null
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
    var initUI = function (elem, options) {
        var $elem = $(elem).addClass('webgis-copyright-container');
        $elem.data('map', options.map);
        options.map.events.on('onaddservice', addService, elem);
        options.map.events.on('onremoveservice', removeService, elem);
        for (var serviceId in options.map.services) {
            var service = options.map.services[serviceId];
            addService({}, service, elem);
        }
    };
    var addService = function (e, service, elem) {
        var $elem = elem ? $(elem) : $(this);
        if (!service.copyrightId)
            return;
        var map = $elem.data('map');
        if (map == null || map !== service.map)
            return;
        var copyrightIds = service.copyrightId.split(',');
        for (var c in copyrightIds) {
            var copyrightInfo = map.getCopyright(copyrightIds[c]);
            if (!copyrightInfo)
                return;
            var $div = $elem.find("[data-copyrightid='" + copyrightIds[c] + "']");
            if ($div.length === 0) {
                $div = $("<div></div>").attr("data-copyrightid", copyrightIds[c]).addClass('webgis-copyright-info').appendTo($elem);
                $("<div>").addClass('webgis-copyright-services').appendTo($div);
                if (copyrightInfo.logo) {
                    var $img = $("<img>").attr('src', copyrightInfo.logo);
                    if (copyrightInfo.logo_size && copyrightInfo.logo_size[0] > 0)
                        $img.css('width', copyrightInfo.logo_size[0]);
                    if (copyrightInfo.logo_size && copyrightInfo.logo_size[1] > 0)
                        $img.css('height', copyrightInfo.logo_size[1]);
                    $img.appendTo($div);
                }
                if (copyrightInfo.copyright)
                    $("<div>" + copyrightInfo.copyright + "</div>").appendTo($div);
                if (copyrightInfo.link)
                    $("<a href='" + copyrightInfo.link + "' target='_blank'>" + (copyrightInfo.link_text ? copyrightInfo.link_text : copyrightInfo.link) + "</a>").appendTo($div);
                if (copyrightInfo.advice)
                    $("<p>").html(copyrightInfo.advice).appendTo($div);
            }
            var $servicesDiv = $div.find('.webgis-copyright-services');
            $("<div>" + service.name + "</div>").attr("data-service", service.id).appendTo($servicesDiv);
        }
    };
    var removeService = function (e, service, elem) {
        var $elem = elem ? $(elem) : $(this);
        console.log('remove');
        if (!service.copyrightId)
            return;
        var map = $elem.data('map');
        if (map == null || map !== service.map)
            return;
        var copyrightIds = service.copyrightId.split(',');
        for (var c in copyrightIds) {
            var $div = $elem.find("[data-copyrightid='" + copyrightIds[c] + "']");
            console.log($div);
            var $servicesDiv = $div.find('.webgis-copyright-services');
            $servicesDiv.find("[data-service='" + service.id + "']").remove();
            if ($servicesDiv.find('div').length == 0)
                $div.remove();
        }
    };
})(webgis.$ || jQuery);
