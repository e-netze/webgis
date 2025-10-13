webgis.ui.definePlugin('webgis_sketchInfoContainer', {
    defaults: {
        map: null,
        readonly: false
    },
    init: function () {
        const $ = this.$;
        let options = this.options;

        if (!options && !options.map) {
            return;
        }

        this.$el
            .addClass('webgis-sketch-info-container-holder')
            .data('map', options.map);

        let $tabGeneral = $("<table><tr><th colspan=2>" + webgis.l10n.get("sketch") + "</th></tr></table>")
            .appendTo(this.$el);

        $("<tr><td>" + webgis.l10n.get("geometry-type") + ":</td><td class='sketch-info-item sketch-geometrytype'></td>")
            .appendTo($tabGeneral);

        let $tabSegment = $("<table><tr><th colspan=2>" + webgis.l10n.get("segment") + "</th></tr></table>")
            .appendTo(this.$el);

        $("<tr><td>" + webgis.l10n.get("segment-length") + ":</td><td class='sketch-info-item segment-length'></td>")
            .appendTo($tabSegment);
        $("<tr><td>" + webgis.l10n.get("segment-azimuth") + ":</td><td class='sketch-info-item segment-azimut-deg'></td>")
            .appendTo($tabSegment);
        $("<tr><td></td><td class='sketch-info-item segment-azimut-grad'></td>")
            .appendTo($tabSegment);

        let $divConstruction = $("<div>")
            .css('display', 'none')
            .appendTo(this.$el);

        let $tabSnapping = $("<table><tr><th colspan=2>Snapping</th></tr></table>")
            .appendTo(this.$el);

        $("<tr><td>" + webgis.l10n.get("snap-to") + ":</td><td class='sketch-info-item snap-result-type'></td>")
            .appendTo($tabSnapping);
        $("<tr><td>" + webgis.l10n.get("snap-to-layer") + ":</td><td class='sketch-info-item snap-result-name'></td>")
            .appendTo($tabSnapping);

        let $tabSections = $("<table>")
            .appendTo(this.$el);

        const sketch = options.sketch = options.map.toolSketch();
        if (sketch) {
            sketch.events.off('currentstate', this.constructor.onSketchCurrentStateChanged);
            sketch.events.on('currentstate', this.constructor.onSketchCurrentStateChanged,
            {
                sketch: sketch,
                elem: this.$el,
                tabSegment: $tabSegment,
                divConstruction: $divConstruction,
                tabSnapping: $tabSnapping,
                tabSections: $tabSections
            });
        }

        let $menu = $('<ul>')
            .addClass('webgis-contextmenu webgis-tool-contextmenu icons')
            .css('box-shadow', 'none')
            .css('border', 'none')
            .data('sketch', sketch)
            .appendTo(this.$el);

        $("<li>")
            .text(webgis.l10n.get("sketch-ortho-off"))
            .addClass('ortho-off')
            .css('background-image', 'url(' + webgis.css.imgResource('sketch_ortho_off-26.png', 'tools') + ')')
            .appendTo($menu)
            .click(function () {
                var sketch = $(this).closest('ul').data('sketch');

                if (sketch) {
                    sketch.stopOrthoMode();
                }
            });

        $("<li>")
            .text(webgis.l10n.get("sketch-trace-off"))
            .addClass('trace-off')
            .css('background-image', 'url(' + webgis.css.imgResource('sketch_trace_off-26.png', 'tools') + ')')
            .appendTo($menu)
            .on('click.webgis_sketch_info_container', function () {
                var sketch = $(this).closest('ul').data('sketch');

                if (sketch) {
                    sketch.stopTraceMode();
                }
            });

        $("<li>")
            .text(webgis.l10n.get("sketch-fan-off"))
            .addClass('fan-off')
            .css('background-image', 'url(' + webgis.css.imgResource('sketch_fan_off-26.png', 'tools') + ')')
            .appendTo($menu)
            .on('click.webgis_sketch_info_container', function () {
                var sketch = $(this).closest('ul').data('sketch');

                if (sketch) {
                    sketch.stopFanMode();
                }
            });

        $menu.children('li')
            .addClass('webgis-toolbox-tool-item')
            .css({ display: 'none', width: '100%', boxSizing: 'border-box', textAlign: 'left' });
    },
    destroy: function () {
        //console.log("destroy webgis_sketchInfoContainer");
        const $ = this.$;

        this.$el.off('.webgis_sketch_info_container');

        const sketch = this.options.sketch;
        if (sketch) {
            //console.log('off sketch event currentstate');
            sketch.events.off("currentstate", this.constructor.onSketchCurrentStateChanged);
        }
    },
    //methods: {

    //},
    staticMethods: {
        onSketchCurrentStateChanged: function (channel, info) {
            //console.log('onSketchCurrentStateChanged', info);
            const $ = webgis.$;

            const sketch = this.sketch;
            const $elem = this.elem;

            $elem.find('.sketch-info-item').text('');

            const $tabSegment = this.tabSegment;
            const $divConstruction = this.divConstruction;
            const $tabSnapping = this.tabSnapping;
            const $tabSections = this.tabSections;

            let constructionMode = webgis.sketch.construct ? webgis.sketch.construct.getConstructionMode(sketch) : null

            var geometryType = info.geometryType;
            $elem.find('.sketch-info-item.sketch-geometrytype').text(webgis.l10n.get(geometryType));

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
                    .css({ marginTop: '4px', paddingRight: '26px', minHeight: '30px', position: 'relative' })
                    .prependTo($divConstruction);

                $("<strong>")
                    .text("Konstruktionsmodus:")
                    .appendTo($header);
                $("<br>").appendTo($header);
                $("<strong>")
                    .text(webgis.l10n.get("construction-mode-" + constructionMode))
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
                    $elem.find('.sketch-info-item.snap-result-type').text(webgis.l10n.get(info.snapResult.type));
                    if (info.snapResult.meta) {
                        $elem.find('.sketch-info-item.snap-result-name').text(info.snapResult.meta.name);
                    }
                }
            } else {
                $tabSnapping.css('display', 'none');
            }

            var validParts = 0;
            $tabSections.children().remove();
            if (sketch.partCount() > 1) {
                $("<tr><th>" + webgis.l10n.get("sections") + ":</th><th></th></tr>").appendTo($tabSections);
                for (let p = sketch.partCount(); p > 0; p--) {
                    const vertices = sketch.partVertices(p - 1);

                    if (sketch.getGeometryType() === "polyline") {
                        const length = webgis.calc.length(vertices, "X", "Y");
                        if (length > 0) {
                            $("<tr><td>" + webgis.l10n.get("section") + " " + (p) + "</td><td>" + Math.round(length * 100) / 100 + " m</td></tr>").appendTo($tabSections);
                        }
                        validParts++;
                    } else if (sketch.getGeometryType() === "polygon") {
                        const area = webgis.calc.area(vertices, "X", "Y");
                        if (area > 0) {
                            $("<tr><td>" + webgis.l10n.get("section") + " " + (p) + "</td><td>" + Math.round(area * 100) / 100 + " m²</td></tr>").appendTo($tabSections);
                        }
                        validParts++;
                    }
                }
            }
        }
    }
});