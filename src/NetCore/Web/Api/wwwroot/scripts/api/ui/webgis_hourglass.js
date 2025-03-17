(function ($) {
    "use strict";
    $.fn.webgis_hourglass = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_hourglass');
        }
    };
    var defaults = {
        map: null,
        img: null
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
        var $elem = $(elem);
        elem._map = options.map;

        if (!options.img) {
            // nicht in "defaults" setzen, weil zum Zeitpunkt des Ladens des Scripts webgis.baseUrl eventuell noch nicht gesetzt ist => LR => eingebettete Karten
            options.img = webgis.css.imgResource('loader1.gif', 'hourglass');
        }

        $elem.addClass('webgis-hourglass-holder');
        $("<table class='webgis-hourglass-service-loader'><tr><td><img src='" + options.img + "' /></td><td nowrap class='webgis-hourglass-text'></td></tr></table>").css('display', 'none').appendTo($elem);
        $("<div class='webgis-hourglass-scale'></div>")
            .css({ display: 'none' })
            .appendTo($elem)
            .click(function (e) {
                e.stopPropagation();

                var map = $(this).closest('.webgis-hourglass-holder')[0]._map;
                map.ui.scaleDialog();
            });
        var scaleText = function (x) {
            x = Math.round(x / 10) * 10;
            return "M 1:" + x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, "."); // Tausender Punkte...
        };
        var scaleBar = function ($target, map) {
            try {
                var res = map.calcCrsResolution();
                var scale = map.calcCrsScale();

                //console.log('scalebar', res, scale);

                var testRulerLength = [1000000, 500000, 250000, 100000, 50000, 25000, 10000, 5000, 2500, 1000, 500, 250, 100, 50, 25, 10, 5, 4, 3, 2, 1];
                var rulerLength, ruler_pix, found = false;
                for (var i = 0; i < testRulerLength.length; i++) {
                    rulerLength = testRulerLength[i];
                    ruler_pix = rulerLength / res;

                    if (ruler_pix > 50 && ruler_pix < 150) {
                        found = true;
                        break;
                    }
                }
                var $ruler = $("<div></div>").css({ display: 'inline-block' }).appendTo($target);
                var $table = $("<table></table>").appendTo($ruler);
                var $tr = $("<tr></tr>").appendTo($table);
                if (found) {
                    $("<td></td>")
                        .addClass('webgis-hourglass-rulertext')
                        .appendTo($tr)
                        .html(rulerLength >= 1000 ? (rulerLength / 1000) + "km" : rulerLength + "m");

                    var $rulerCell = $("<td></td>").appendTo($tr);
                    $("<div></div>")
                        .addClass('webgis-hourglass-ruler')
                        .css({
                            width: ruler_pix,
                            height: 5
                        }).appendTo($rulerCell);
                }
                $("<td></td>")
                    .addClass('webgis-hourglass-scaletext')
                    .html(scaleText(scale))
                    .appendTo($tr);

            } catch (e) { $target.empty(); console.log('scalebar', e) }
        };
        elem._map.events.on('showhourglass', function (e, sender, names) {
            var text = names && names.length > 0 ? names.join(',') : null;
            if (text) {
                $(this).find('.webgis-hourglass-text').text(text);
                $(this).find('.webgis-hourglass-scale').css('display', 'none');
                $(this).find('.webgis-hourglass-service-loader').css('display', '');
            } else {
                $(this).find('.webgis-hourglass-service-loader').css('display', 'none');
                var $scale = $(this).find('.webgis-hourglass-scale').css('display', '').empty();
                scaleBar($scale, this._map);
            }
        }, elem);
        elem._map.events.on('hidehourglass', function (e, sender) {
            $(this).find('.webgis-hourglass-service-loader').css('display', 'none');
            var $scale = $(this).find('.webgis-hourglass-scale').css('display', '').empty();
            scaleBar($scale, this._map);
        }, elem);
    };
})(webgis.$ || jQuery);
