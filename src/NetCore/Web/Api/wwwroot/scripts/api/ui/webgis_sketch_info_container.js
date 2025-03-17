(function ($) {
    "use strict";
    $.fn.webgis_sketchInfoContainer = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_sketchInfoContainer');
        }
    };
    var defaults = {
        map: null,
        readonly: false
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
        if (!options && !options.map) {
            return;
        }

        var $elem = $(elem).addClass('webgis-sketch-info-container-holder');
        $elem.data('map', options.map);

        var $tabGeneral = $("<table><tr><th colspan=2>Sketch</th></tr></table>")
            .appendTo($elem);

        $("<tr><td>Geometrie-Typ:</td><td class='sketch-info-item sketch-geometrytype'></td>").appendTo($tabGeneral);

        var $tabSegment = $("<table><tr><th colspan=2>Segment</th></tr></table>")
            .appendTo($elem);

        $("<tr><td>Segment Länge:</td><td class='sketch-info-item segment-length'></td>").appendTo($tabSegment);
        $("<tr><td>Segment Azimut:</td><td class='sketch-info-item segment-azimut-deg'></td>").appendTo($tabSegment);
        $("<tr><td></td><td class='sketch-info-item segment-azimut-grad'></td>").appendTo($tabSegment);

        var $divConstruction = $("<div>")
            .css('display', 'none')
            .appendTo($elem);
        //$("<div>")
        //    .addClass('webgis-info')
        //    .css('margin-top', '4px')
        //    .text("Tipp: Drücke die rechte Maustaste zum beenden des Konstructionsmodus oder für mehr weitere Werkzeuge.")
        //    .appendTo($divConstruction);

        var $tabSnapping = $("<table><tr><th colspan=2>Snapping</th></tr></table>")
            .appendTo($elem);

        $("<tr><td>Snappen auf:</td><td class='sketch-info-item snap-result-type'></td>").appendTo($tabSnapping);
        $("<tr><td>Thema:</td><td class='sketch-info-item snap-result-name'></td>").appendTo($tabSnapping);

        var sketch = options.map.toolSketch();

        if (sketch) {
            sketch.events.off('currentstate', onSketchCurrentStateChanged);
            sketch.events.on('currentstate', onSketchCurrentStateChanged,
                {
                    sketch: sketch,
                    elem: $elem,
                    tabSegment: $tabSegment,
                    divConstruction: $divConstruction,
                    tabSnapping: $tabSnapping,
                });
        }

        var $menu = $('<ul>')
            .addClass('webgis-contextmenu webgis-tool-contextmenu icons')
            .css('box-shadow', 'none')
            .css('border', 'none')
            .data('sketch', sketch)
            .appendTo($elem);

        $("<li>Orthogonal-Modus beenden</li>")
            .addClass('ortho-off')
            .css('background-image', 'url(' + webgis.css.imgResource('sketch_ortho_off-26.png', 'tools') + ')')
            .appendTo($menu)
            .click(function () {
                var sketch = $(this).closest('ul').data('sketch');

                if (sketch) {
                    sketch.stopOrthoMode();
                }
            });

        $("<li>Trace-Modus beenden</li>")
            .addClass('trace-off')
            .css('background-image', 'url(' + webgis.css.imgResource('sketch_trace_off-26.png', 'tools') + ')')
            .appendTo($menu)
            .click(function () {
                var sketch = $(this).closest('ul').data('sketch');

                if (sketch) {
                    sketch.stopTraceMode();
                }
            });

        $("<li>Fan-Modus beenden</li>")
            .addClass('fan-off')
            .css('background-image', 'url(' + webgis.css.imgResource('sketch_fan_off-26.png', 'tools') + ')')
            .appendTo($menu)
            .click(function () {
                var sketch = $(this).closest('ul').data('sketch');

                if (sketch) {
                    sketch.stopFanMode();
                }
            });

        $menu.children('li')
            .addClass('webgis-toolbox-tool-item')
            .css({ display: 'none', width: '100%', boxSizing: 'border-box', textAlign: 'left' });
    };

    var onSketchCurrentStateChanged = function (channel, info) {
        var sketch = this.sketch;
        var $elem = this.elem;

        $elem.find('.sketch-info-item').text('');

        var $tabSegment = this.tabSegment;
        var $divConstruction = this.divConstruction;
        var $tabSnapping = this.tabSnapping;

        var constructionMode = webgis.sketch.construct ? webgis.sketch.construct.getConstructionMode(sketch) : null

        var geometryType = info.geometryType;
        $elem.find('.sketch-info-item.sketch-geometrytype').text(webgis.i18n.get(geometryType));

        if (!constructionMode && $.inArray(geometryType, ["polygon", "polyline"]) >= 0) {
            $tabSegment.css('display', 'block');
            $divConstruction.css('display', 'none');

            if (info.segmentLength >= 0) {
                $elem.find('.sketch-info-item.segment-length').text(Math.round(info.segmentLength * 100.0) / 100.0 + ' m');
            }
            if (info.segmentAzimut) {
                $elem.find('.sketch-info-item.segment-azimut-deg').text(Math.round(info.segmentAzimut * 180.0 / Math.PI * 1000.0) / 1000.0 + '°');
                $elem.find('.sketch-info-item.segment-azimut-grad').text(Math.round(info.segmentAzimut * 200.0 / Math.PI * 1000.0) / 1000.0 + ' gon');
            }
        } else {
            $tabSegment.css('display', 'none');
        }

        if (constructionMode) {
            $divConstruction.css('display', 'block')
                .children('.temp')
                .remove();

            var items = webgis.sketch.construct.getInfoItems(sketch);
            if (items) {
                var $itemContainer = $("<div>")
                    .addClass('temp')
                    .prependTo($divConstruction);

                for (var i in items) {
                    var item = items[i];

                    $("<div>")
                        .addClass('temp')
                        .addClass('webgis-' + (item.class || 'tip'))
                        .css('margin-top', '4px')
                        .text(item.text)
                        .appendTo($itemContainer);
                }
            }

            var $header = $("<div>")
                .addClass('temp')
                .css({ marginTop: '4px', paddingRight: '26px', minHeight:'30px', position: 'relative'})
                .prependTo($divConstruction);

            $("<strong>")
                .text("Konstruktionsmodus:")
                .appendTo($header);
            $("<br>").appendTo($header);
            $("<strong>")
                .text(webgis.i18n.get("construction-mode-" + constructionMode))
                .appendTo($header);

            $("<div>")
                .addClass('webgis-button-close-24')
                .appendTo($header)
                .click(function () {
                    webgis.sketch.construct.cancel(sketch);
                });
        }

        var $menu = $elem.find('ul.webgis-contextmenu');
        $menu.children('li.ortho-off').css('display', sketch.isInOrthoMode() ? 'block' : 'none');
        $menu.children('li.trace-off').css('display', sketch.isInTraceMode() ? 'block' : 'none');
        $menu.children('li.fan-off').css('display', sketch.isInFanMode() ? 'block' : 'none');

        if (sketch.map && sketch.map.construct.hasSnapping(sketch) === true) {
            $tabSnapping.css('display', 'block');
            if (info.snapResult) {
                $elem.find('.sketch-info-item.snap-result-type').text(webgis.i18n.get(info.snapResult.type));
                if (info.snapResult.meta) {
                    $elem.find('.sketch-info-item.snap-result-name').text(info.snapResult.meta.name);
                }
            }
        } else {
            $tabSnapping.css('display', 'none');
        }
    };
})(webgis.$ || jQuery);
