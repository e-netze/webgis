// WebGIS UI Plugins (hourglass)
// webgis_hourglass: map loading indicator (service names while loading, scale bar otherwise)
// Created by map.ui.createHourglass (webgis.map.ui.js)

webgis.ui.definePlugin('webgis_hourglass', {
    defaults: {
        map: null,
        img: null
    },
    init: function () {
        const $ = this.$, o = this.options, $elem = this.$el;
        const elem = $elem[0];
        elem._map = o.map;

        if (!o.img) {
            // nicht in "defaults" setzen, weil zum Zeitpunkt des Ladens des Scripts webgis.baseUrl eventuell noch nicht gesetzt ist => LR => eingebettete Karten
            o.img = webgis.css.imgResource('loader1.gif', 'hourglass');
        }

        $elem.addClass('webgis-hourglass-holder');
        $("<table class='webgis-hourglass-service-loader'><tr><td><img src='" + o.img + "' /></td><td nowrap class='webgis-hourglass-text'></td></tr></table>").css('display', 'none').appendTo($elem);
        $("<div class='webgis-hourglass-scale'></div>")
            .css({ display: 'none' })
            .appendTo($elem)
            .on('click.webgis_hourglass', function (e) {
                e.stopPropagation();

                const map = $(this).closest('.webgis-hourglass-holder')[0]._map;
                map.ui.scaleDialog();
            });

        const ctor = this.constructor;
        this._onShowHourglass = function (e, sender, names) {
            const text = names && names.length > 0 ? names.join(',') : null;
            if (text) {
                $elem.find('.webgis-hourglass-text').text(text);
                $elem.find('.webgis-hourglass-scale').css('display', 'none');
                $elem.find('.webgis-hourglass-service-loader').css('display', '');
            } else {
                $elem.find('.webgis-hourglass-service-loader').css('display', 'none');
                const $scale = $elem.find('.webgis-hourglass-scale').css('display', '').empty();
                ctor.scaleBar($, $scale, o.map);
            }
        };
        this._onHideHourglass = function (e, sender) {
            $elem.find('.webgis-hourglass-service-loader').css('display', 'none');
            const $scale = $elem.find('.webgis-hourglass-scale').css('display', '').empty();
            ctor.scaleBar($, $scale, o.map);
        };
        o.map.events.on('showhourglass', this._onShowHourglass, elem);
        o.map.events.on('hidehourglass', this._onHideHourglass, elem);
    },
    destroy: function () {
        const map = this.options.map;
        if (map && map.events) {
            map.events.off('showhourglass', this._onShowHourglass);
            map.events.off('hidehourglass', this._onHideHourglass);
        }
        this.$el.find('*').off('.webgis_hourglass');
    },
    staticMethods: {
        scaleText: function (x) {
            x = Math.round(x / 10) * 10;
            return "M 1:" + x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, "."); // Tausender Punkte...
        },
        scaleBar: function ($, $target, map) {
            try {
                const res = map.calcCrsResolution();
                const scale = map.calcCrsScale();

                const testRulerLength = [1000000, 500000, 250000, 100000, 50000, 25000, 10000, 5000, 2500, 1000, 500, 250, 100, 50, 25, 10, 5, 4, 3, 2, 1];
                let rulerLength, ruler_pix, found = false;
                for (let i = 0; i < testRulerLength.length; i++) {
                    rulerLength = testRulerLength[i];
                    ruler_pix = rulerLength / res;

                    if (ruler_pix > 50 && ruler_pix < 150) {
                        found = true;
                        break;
                    }
                }
                const $ruler = $("<div></div>").css({ display: 'inline-block' }).appendTo($target);
                const $table = $("<table></table>").appendTo($ruler);
                const $tr = $("<tr></tr>").appendTo($table);
                if (found) {
                    $("<td></td>")
                        .addClass('webgis-hourglass-rulertext')
                        .appendTo($tr)
                        .html(rulerLength >= 1000 ? (rulerLength / 1000) + "km" : rulerLength + "m");

                    const $rulerCell = $("<td></td>").appendTo($tr);
                    $("<div></div>")
                        .addClass('webgis-hourglass-ruler')
                        .css({
                            width: ruler_pix,
                            height: 5
                        }).appendTo($rulerCell);
                }
                $("<td></td>")
                    .addClass('webgis-hourglass-scaletext')
                    .html(this.scaleText(scale))
                    .appendTo($tr);

            } catch (e) { $target.empty(); webgis.log('scalebar', e) }
        }
    }
});
