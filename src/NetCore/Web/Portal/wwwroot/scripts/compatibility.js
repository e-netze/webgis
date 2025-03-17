(function ($) {
    "use strict"
    $.fn.webgis_comp_maplinks = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on jQuery.webgis_addServicesToc');
        }
    };

    var defaults = {
    };

    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);

            return this.each(function () {
                new initUI(this, options);
            });
        }
    };

    var initUI = function (elem, options) {
        var $elem = $(elem);
        $elem.empty();

        if (!options.collection)
            return;

        for (var c in options.collection) {
            var collection = options.collection[c];
            if (!collection.links || collection.links.length == 0)
                continue;

            var $div = $("<div style='margin-top:10px'>").appendTo($elem);
            $("<h2 class='webgis-link-collection-header' style='cursor:pointer'><div class='webgis-link-collection-nav webgis-link-collection-down'>" + collection.name + " [" + collection.links.length + "]</div></h2>").appendTo($div)
                .click(function () {
                    $(this.parentNode).find('.webgis-link-container').slideToggle();
                    $(this.parentNode).find('.webgis-link-collection-nav').toggleClass('webgis-link-collection-down').toggleClass('webgis-link-collection-up');
                });

            var $container = $("<div class='webgis-link-container' style='margin:10px 0px;display:none'>").appendTo($div);
            for (var l in collection.links) {
                var link = collection.links[l];
                var thumbnail = link.thumbnail;

                $("<a href='" + link.href + "' target='_blank'><div class='webgis-link-tile'><div class='webgis-link-tile-image' style=\"" + (thumbnail ? "background-image:url('" + thumbnail + "')" : "") + "\"></div>" +
                    "<div class='webgis-link-tile-text'><div style='height:50px'>" + link.name + "</div><div style='font-size:0.85em'>" + (link.description ? link.description : "") + "</div></div>" +
                    "</div></a>").appendTo($container);
            }
        }

        var $info = $("<div style='color:#aaa;background:#eee;border-radius:10px;margin-top:220px;padding:20px'></div>").appendTo($elem);
        if (options.source) {
            $("<div>Source: " + options.source.host + "</div>").appendTo($info);
        }
        if (options.user) {
            $("<div>Username: " + options.user.username + "</div>").appendTo($info);
        }

        $(elem).find('.webgis-link-collection-header').each(function (i, e) {
            if (i < 1) {
                $(e).trigger('click');
            }
        });
    };
})(jQuery);

(function ($) {
    "use strict"
    $.fn.webgis_comp_hourglass = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on jQuery.webgis_hourglass');
        }
    };

    var defaults = {
        text: 'wird geladen...'
    };

    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);

            return this.each(function () {
                new initUI(this, options);
            });
        }
    };

    var initUI = function (elem, options) {
        var $elem = $(elem);
        elem._map = options.map;

        $elem.empty();
        $("<table><tr><td><div class='webgis-hourglass-image'></div></td><td nowrap class='webgis-hourglass-text'>" + options.text + "</td></tr></table>").appendTo($elem);
    };

})(jQuery);