// WebGIS UI Plugins (construct)
// webgis_snapping, webgis_dirDistControl, webgis_offsetControl, webgis_extendLineSketchControl,
// webgis_xyAbsoluteControl, webgis_sketchFromGeometry, webgis_uploadSketch, webgis_downloadSketch,
// select_sketch
// Snapping settings and sketch construction tools (direction/distance, offset, extend line,
// absolute coordinates, sketch from geometry, upload/download sketch, select named sketch).
// webgis_sketchFromGeometry and select_sketch need no element: use
// webgis.ui.showSketchFromGeometryDialog(options) / webgis.ui.showSelectSketchDialog(options).

webgis.ui.definePlugin('webgis_snapping', {
    defaults: {
        map: null
    },
    init: function () {
        const $ = this.$;
        const self = this;
        const map = this.options.map;
        const $parent = this.$el.addClass('snapping-holder').data('map', map);

        const $toleranceInput = $("<input type='number' min='0' step='2'>")
            .addClass('webgis-input')
            .css({ width: '50px', height: '10px', marginRight: '10px' })
            .val(map.construct.snapPixelTolerance)
            .on('change.webgis_snapping', function () {
                const tolerance = parseFloat($(this).val());
                if (isNaN(tolerance) || tolerance < 0) {
                    $(this).val(map.construct.snapPixelTolerance);
                } else {
                    map.construct.snapPixelTolerance = tolerance;
                }
            })
            .appendTo($("<label>")
                .text(webgis.l10n.get("snapping-tolerance-pixel") + ':')
                .appendTo($("<div>")
                    .css({ textAlign: 'right', paddingBottom: 4 })
                    .appendTo($parent)));

        const $table = $("<table>").css({
            width: '100%',
            borderSpacing: '0px'
        })
            .addClass('webgis-table')
            .appendTo($parent);
        $("<tr style='text-align:left'><th>"
            + webgis.l10n.get("scheme") +
            "</th><th style='text-align:center'>"
            + webgis.l10n.get("nodes") +
            "</th><th style='text-align:center'>"
            + webgis.l10n.get("edges") +
            "</th><th style='text-align:center'>"
            + webgis.l10n.get("endpoints") +
            "</th></tr>")
            .appendTo($table);

        const onSchemaChange = function () {
            $(this).closest('tr').find('.checkbox-snapping.detail').prop('checked', $(this).is(':checked'));
            self.refresh();
        };
        const onDetailChange = function () {
            self.refresh();
        };

        // Sketch
        let snappingTypes = map.getSnappingTypes(webgis.sketchSnappingSchemeId);
        const $tr = $("<tr>").attr('data-id', webgis.sketchSnappingSchemeId).appendTo($table);
        const $td = $("<td>").appendTo($tr);
        $("<input type='checkbox' id='" + webgis.sketchSnappingSchemeId + "'>").addClass('checkbox-snapping schema').prop('checked', snappingTypes && snappingTypes.length > 0)
            .css({
                position: 'relative', top: '7px'
            }).appendTo($td)
            .on('change.webgis_snapping', onSchemaChange);
        const $label = $("<label for='" + webgis.sketchSnappingSchemeId + "'>&nbsp;" + webgis.l10n.get("current-sketch") + "</label>")
            .appendTo($td);
        $("<div style='padding-left:25px;color:#aaa'>").text(webgis.l10n.get("all-scales")).appendTo($label);
        $("<input type='checkbox'>").addClass('checkbox-snapping detail').attr('data-type', 'nodes').appendTo($("<td style='text-align:center'>").appendTo($tr))
            .on('change.webgis_snapping', onDetailChange);
        $("<input type='checkbox'>").addClass('checkbox-snapping detail').attr('data-type', 'edges').appendTo($("<td style='text-align:center'>").appendTo($tr))
            .on('change.webgis_snapping', onDetailChange);
        $("<input type='checkbox'>").addClass('checkbox-snapping detail').attr('data-type', 'endpoints').appendTo($("<td style='text-align:center'>").appendTo($tr))
            .on('change.webgis_snapping', onDetailChange);
        $tr.find('input[data-type]').each(function (i, e) {
            $(e).prop('checked', $.inArray($(e).attr('data-type'), snappingTypes || []) >= 0);
        });

        // Services
        for (let i in map.services) {
            const service = map.services[i];
            for (let s in service.snapping) {
                const snapping = service.snapping[s];
                const snappingId = service.id + '~' + snapping.id;
                const snappingTypes = map.getSnappingTypes(snappingId);

                const $tr = $("<tr>").attr('data-id', snappingId).appendTo($table);
                const $td = $("<td>").appendTo($tr);
                $("<input type='checkbox' id='" + snapping.id + "'>").addClass('checkbox-snapping schema').prop('checked', snappingTypes && snappingTypes.length > 0)
                    .css({
                        position: 'relative', top: '7px'
                    }).appendTo($td)
                    .on('change.webgis_snapping', onSchemaChange);
                const $label = $("<label for='" + snapping.id + "'>&nbsp;" + snapping.name + "</label>").appendTo($td);
                $("<div style='padding-left:25px;color:#aaa'>").html("ab 1:" + snapping.min_scale).appendTo($label);
                $("<input type='checkbox'>").addClass('checkbox-snapping detail').attr('data-type', 'nodes').appendTo($("<td style='text-align:center'>").appendTo($tr))
                    .on('change.webgis_snapping', onDetailChange);
                $("<input type='checkbox'>").addClass('checkbox-snapping detail').attr('data-type', 'edges').appendTo($("<td style='text-align:center'>").appendTo($tr))
                    .on('change.webgis_snapping', onDetailChange);
                $("<input type='checkbox'>").addClass('checkbox-snapping detail').attr('data-type', 'endpoints').appendTo($("<td style='text-align:center'>").appendTo($tr))
                    .on('change.webgis_snapping', onDetailChange);
                $tr.find('input[data-type]').each(function (i, e) {
                    $(e).prop('checked', $.inArray($(e).attr('data-type'), snappingTypes || []) >= 0);
                });
            }
        }
    },
    destroy: function () {
        this.$el.find('*').addBack().off('.webgis_snapping');
    },
    methods: {
        refresh: function () {
            const $ = this.$;
            const $elem = this.$el;
            const map = $elem.data('map');
            $elem.find('tr[data-id]').each(function (i, tr) {
                const $tr = $(tr);
                let schemaChecked = false;
                const types = [];
                $tr.find('.checkbox-snapping.detail').each(function (j, check) {
                    const $check = $(check);
                    schemaChecked |= $check.is(':checked');
                    if ($check.is(':checked'))
                        types.push($check.attr('data-type'));
                });
                $tr.find('.checkbox-snapping.schema').prop('checked', schemaChecked);
                map.setSnapping($tr.attr('data-id'), types);
            });
        }
    }
});

webgis.ui.definePlugin('webgis_dirDistControl', {
    defaults: {
        azimut: 0,
        azimut_unit: 'gon',
        azimut_readonly: false,
        distance: 0,
        distance_unit: 'm',
        distance_readonly: false,
        _3d: false,
        z_angle_type: 'h',
        z_angle: 0,
        z_angle_unit: webgis.usability.measureZAngleDefaultUnit,
        on_apply: null
    },
    init: function () {
        const $ = this.$;
        const self = this;
        const options = this.options;
        const $parent = this.$el;

        var azimut = Math.round(options.azimut * 1e+5) / 1e+5;
        var distance = Math.round(options.distance * 1e+2) / 1e+2;

        var $holder = $("<div>").css('margin', '10px').addClass('webgis-dir-dist-holder').data('options', options).appendTo($parent);
        $("<div>").text(webgis.l10n.get('direction-azimuth')).addClass('webgis-input-label')
            .appendTo($holder);

        var $inputAzimut = $("<input name='azimut'>")
            .addClass('webgis-input').css('width', '50%')
            .val(azimut).
            appendTo($holder);

        var $inputAzimutUnit = $("<select name='azimut_unit'><option value='gon'>GON</option><option value='deg'>Grad</option></select>")
            .addClass('webgis-input')
            .css({ width: '40%', height: '17px' })
            .appendTo($holder)
            .val(options.azimut_unit);

        $("<div>").text(webgis.l10n.get('distance'))
            .addClass('webgis-input-label')
            .appendTo($holder);

        var $inputDistance = $("<input name='dist'>")
            .addClass('webgis-input')
            .css('width', '50%')
            .val(distance)
            .appendTo($holder);

        var $inputDistanceUnit = $("<select name='dist_unit'><option value='m'>Meter</option><option value='km'>Kilometer</option></select>")
            .addClass('webgis-input')
            .css({ width: '40%', height: '17px' })
            .appendTo($holder).val(options.distance_unit);

        $("<br/><br/>")
            .appendTo($holder);

        $("<input type='checkbox' id='webigs_dir_dist_3d' name='use_3d' />").prop('checked', options._3d).appendTo($holder)
            .on('change.webgis_dirDistControl', function () {
                $(this).parent().find('.3dpane').slideToggle();
            });

        $("<label for='webigs_dir_dist_3d'>3D</label>")
            .appendTo($holder);

        var $_3dpane = $("<div class='3dpane'>").css('display', 'none')
            .appendTo($holder);

        $("<select name='z_angle_type'><option value='h'>Horizontalwinkel</option><option value='z'>Zenitdistanz</option></select>")
            .addClass('webgis-input')
            .css('width', '99%')
            .appendTo($_3dpane)
            .val(options.z_angle_type);

        $("<input name='z_angle'>")
            .addClass('webgis-input')
            .css('width', '50%')
            .val(options.z_angle)
            .appendTo($_3dpane);

        $("<select name='z_angle_unit'><option value='percent'>% (Prozent)</option><option value='gon'>GON</option><option value='deg'>Grad</option></select>")
            .addClass('webgis-input')
            .css({ width: '40%', height: '17px' })
            .appendTo($_3dpane)
            .val(options.z_angle_unit);

        $("<br/><br/>")
            .appendTo($holder);

        $("<button>").text(webgis.l10n.get('take-over'))
            .addClass('webgis-button')
            .appendTo($holder)
            .on('click.webgis_dirDistControl', function () {
                var toFloat = self.constructor.toFloat;
                var $holder = $(this).closest('.webgis-dir-dist-holder');
                var azimut = toFloat($holder.find("input[name='azimut']").val());
                switch ($holder.find("select[name='azimut_unit']").val()) {
                    case 'gon':
                        azimut *= 0.9;
                        break;
                    case 'deg':
                        break;
                }
                var dist = toFloat($holder.find("input[name='dist']").val());
                switch ($holder.find("select[name='dist_unit']").val()) {
                    case 'm':
                        break;
                    case 'km':
                        dist *= 1000;
                        break;
                }
                var z_angle = toFloat($holder.find("input[name='z_angle']").val());
                switch ($holder.find("select[name='z_angle_unit']").val()) {
                    case 'gon':
                        z_angle *= 0.9;
                        break;
                    case 'percent':
                        z_angle = Math.atan(z_angle / 100.0) * 180.0 / Math.PI;
                        break;
                    case 'deg':
                        break;
                }
                switch ($holder.find("select[name='z_angle_type']").val()) {
                    case 'h':
                        break;
                    case 'z':
                        z_angle = 90 - z_angle;
                        break;
                }

                if (!$holder.find("input[name='use_3d']").is(':checked'))
                    z_angle = 0;
                var options = $holder.data('options');
                if (options.on_apply)
                    options.on_apply({
                        azimut_deg: azimut,
                        distance_m: dist,
                        z_angle_deg: z_angle
                    });
            });

        if (options.azimut_readonly === true) {
            $inputAzimut.attr('readonly', 'readonly');
            $inputAzimutUnit.attr('readonly', 'readonly');
        }
        if (options.distance_readonly === true) {
            $inputDistance.attr('readonly', 'readonly');
            $inputDistanceUnit.attr('readonly', 'readonly');
        }
    },
    destroy: function () {
        this.$el.find('*').addBack().off('.webgis_dirDistControl');
    },
    staticMethods: {
        toFloat: function (val) {
            val = val.replace(',', '.');
            return parseFloat(val);
        }
    }
});

webgis.ui.definePlugin('webgis_offsetControl', {
    defaults: {
        offset: 0,
        offset_unit: 'm',
        on_apply: null
    },
    init: function () {
        const $ = this.$;
        const self = this;
        const options = this.options;
        const $parent = this.$el;

        var offset = Math.round(options.offset * 1e+2) / 1e+2;

        var $holder = $("<div>")
            .css('margin', '10px')
            .addClass('webgis-dir-dist-holder')
            .data('options', options)
            .appendTo($parent);

        $("<div>")
            .text(webgis.l10n.get('offset'))
            .addClass('webgis-input-label')
            .appendTo($holder);

        $("<select name='offset_direction'><option value='-1'>Links</option><option value='1'>Rechts</option></select>")
            .css('width', '50%')
            .addClass('webgis-input')
            .appendTo($holder).val(Math.sign(options.offset));

        $("<input name='offset'>")
            .addClass('webgis-input')
            .css('width', '50%')
            .val(Math.abs(offset))
            .appendTo($holder);

        $("<select name='offset_unit'><option value='m'>Meter</option><option value='km'>Kilometer</option></select>")
            .addClass('webgis-input')
            .css({ width: '40%', height: '17px' })
            .appendTo($holder).val(options.offset_unit);

        $("<br/><br/>").appendTo($holder);

        $("<button>").text(webgis.l10n.get('take-over'))
            .addClass('webgis-button')
            .appendTo($holder)
            .on('click.webgis_offsetControl', function () {
                var toFloat = self.constructor.toFloat;
                var $holder = $(this).closest('.webgis-dir-dist-holder');
                var offset =
                    toFloat($holder.find("input[name='offset']").val()) *
                    parseInt($holder.find("select[name='offset_direction']").val());

                webgis.log('offset', offset, toFloat($holder.find("input[name='offset']").val()), parseInt($holder.find("select[name='offset_direction']").val()));
                switch ($holder.find("select[name='offset_unit']").val()) {
                    case 'm':
                        break;
                    case 'km':
                        offset *= 1000;
                        break;
                }
                var options = $holder.data('options');
                if (options.on_apply)
                    options.on_apply({
                        offset_m: offset,
                    });
            });
    },
    destroy: function () {
        this.$el.find('*').addBack().off('.webgis_offsetControl');
    },
    staticMethods: {
        toFloat: function (val) {
            val = val.replace(',', '.');
            return parseFloat(val);
        }
    }
});

webgis.ui.definePlugin('webgis_extendLineSketchControl', {
    defaults: {
        extend: 1,
        extend_unit: 'm',
        on_apply: null
    },
    init: function () {
        const $ = this.$;
        const self = this;
        const options = this.options;
        const $parent = this.$el;

        var extend = Math.round(options.extend * 1e+2) / 1e+2;

        var $holder = $("<div>")
            .css('margin', '10px')
            .addClass('webgis-extend-line-holder')
            .data('options', options)
            .appendTo($parent);

        $("<select name='extend_ends'><option value='-1'>" + webgis.l10n.get('extend-at-start') + "</option><option value='1'>" + webgis.l10n.get('extend-at-end') + "</option><option value='0'>" + webgis.l10n.get('extend-at-start-and-end') + "</option></select>")
            .css('width', '55%')
            .addClass('webgis-input')
            .appendTo($holder).val(0);

        $("<input name='extend'>")
            .addClass('webgis-input')
            .css('width', '55%')
            .val(Math.abs(extend))
            .appendTo($holder);

        $("<select name='extend_unit'><option value='m'>Meter</option><option value='km'>Kilometer</option></select>")
            .addClass('webgis-input')
            .css({ width: '35%', height: '17px' })
            .appendTo($holder).val(options.extend_unit);

        $("<br/><br/>").appendTo($holder);

        $("<button>").text(webgis.l10n.get('extend'))
            .addClass('webgis-button')
            .appendTo($holder)
            .on('click.webgis_extendLineSketchControl', function () {
                var toFloat = self.constructor.toFloat;
                var $holder = $(this).closest('.webgis-extend-line-holder');
                var extend = toFloat($holder.find("input[name='extend']").val());
                var ends = parseInt($holder.find("select[name='extend_ends']").val());

                switch ($holder.find("select[name='extend_unit']").val()) {
                    case 'm':
                        break;
                    case 'km':
                        extend *= 1000;
                        break;
                }

                var options = $holder.data('options');
                if (options.on_apply)
                    options.on_apply({
                        extend_m: extend,
                        ends: ends
                    });
            });
    },
    destroy: function () {
        this.$el.find('*').addBack().off('.webgis_extendLineSketchControl');
    },
    staticMethods: {
        toFloat: function (val) {
            val = val.replace(',', '.');
            return parseFloat(val);
        }
    }
});

webgis.ui.definePlugin('webgis_xyAbsoluteControl', {
    defaults: {
        map: null
    },
    init: function (rawOptions) {
        this.constructor.open(rawOptions, this.$el);
    },
    staticMethods: {
        // $parent: element the server UI is built into (may be empty, eg. $(null))
        open: function (options, $parent) {
            const $ = webgis.$ || jQuery;
            options = $.extend({}, { map: null }, options);
            $parent = $($parent);

            if (!options.map || !options.sketch)
                return;

            var tool = {
                id: 'webgis.tools.coordinates',
                type: 'servertoolcommand_ext',
                command: 'inputcoordinates-sketch-xyabsolute'
            };

            tool.handleToolResponseCustomHandler = function (response) {
                if (response.ui) {
                    response.ui.map = options.map;
                    response.ui.tool = tool;
                    response.ui.title = null;
                    response.ui.closebutton = false;
                    // This is a small, embedded construction-tool popup (opened from the coordinate
                    // display / sketch context menu while another tool, eg. MeasureLine/MapMarkup, is
                    // actually active) - not a real tool-dialog (re)build. Firing 'onbuildtoolui' with
                    // this tool's id would make webgis.map.js think the *active* tool changed and wipe
                    // the sketch-info overlay content, which then never gets repopulated since the
                    // actually active tool isn't rebuilding its UI. See sketch_info_container / map.js.
                    response.ui.suppressOnBuildToolUi = true;

                    $parent.webgis_uibuilder(response.ui);
                }
            };

            var customEvents = {}, map = options.map, sketch = options.sketch;

            if (options.map.calcCrs()) {
                var projectedCoords = null, wgs84Coords = null;

                if (sketch._snappedVertex) {
                    wgs84Coords = [];
                    projectedCoords = { x: sketch._snappedVertex.X, y: sketch._snappedVertex.Y };
                } else if (sketch._moverLine) {
                    var latLngs = sketch._moverLine.getLatLngs();
                    if (latLngs.length >= 2) {
                        wgs84Coords = [{ x: latLngs[1].lng, y: latLngs[1].lat }];
                        webgis.log('moverline', wgs84Coords);

                        projectedCoords = webgis.fromWGS84(
                            options.map.calcCrs(wgs84Coords),
                            latLngs[1].lat, latLngs[1].lng);
                    }
                } else if (map._currentCursorPosition && map._currentCursorPosition.lat && map._currentCursorPosition.lng) {
                    wgs84Coords = [{ x: map._currentCursorPosition.lng, y: map._currentCursorPosition.lat }]
                    webgis.log('curentCursor');
                    projectedCoords = webgis.fromWGS84(
                        options.map.calcCrs(wgs84Coords),
                        map._currentCursorPosition.lat, map._currentCursorPosition.lng);
                } else {
                    var center = options.map.getCenter();
                    webgis.log('center', center);
                    wgs84Coords = [{ x: center[0], y: center[1] }];
                    projectedCoords = webgis.fromWGS84(
                        options.map.calcCrs(wgs84Coords),
                        center[1], center[0]);
                }

                customEvents["coordinates-map-srs"] = options.map.calcCrs(wgs84Coords).id;

                if (projectedCoords) {
                    customEvents["coordinates-input-x-value"] = Math.round(projectedCoords.x * 100.0) / 100.0;
                    customEvents["coordinates-input-y-value"] = Math.round(projectedCoords.y * 100) / 100.0;
                    customEvents["coordinates-input-proj"] = options.map.calcCrs(wgs84Coords).id;
                }
            }

            webgis.tools.onButtonClick(options.map, tool, null, null, customEvents);
        }
    }
});

webgis.ui.definePlugin('webgis_sketchFromGeometry', {
    defaults: {
        map: null
    },
    init: function (rawOptions) {
        this.constructor.open(rawOptions, this.$el);
    },
    staticMethods: {
        // no element needed, from outside: webgis.ui.showSketchFromGeometryDialog(options, $parent)
        // $parent (optional): element the server UI is built into, default: $(null)
        open: function (options, $parent) {
            const $ = webgis.$ || jQuery;
            options = $.extend({}, { map: null }, options);
            $parent = $($parent || null);

            if (!options.map || !options.sketch)
                return;

            var tool = {
                id: 'webgis.tools.identify',
                type: 'servertoolcommand_ext',
                command: 'sketchfromgeometry'
            };

            tool.handleToolResponseCustomHandler = function (response) {
                if (response.ui) {
                    response.ui.map = options.map;
                    response.ui.tool = tool;
                    response.ui.title = null;
                    response.ui.closebutton = false;
                    // Embedded construction-tool popup, not a real tool-dialog (re)build - see comment
                    // in webgis_xyAbsoluteControl for why this matters.
                    response.ui.suppressOnBuildToolUi = true;

                    $parent.webgis_uibuilder(response.ui);
                }
            };

            var customEvents = {}, map = options.map, sketch = options.sketch;

            if (options.map && options.map.crs && sketch) {
                var projectedCoords = null;

                if (map._currentCursorPosition && map._currentCursorPosition.lat && map._currentCursorPosition.lng) {
                    projectedCoords = webgis.fromWGS84(options.map.crs, map._currentCursorPosition.lat, map._currentCursorPosition.lng);
                }

                var $hidden = $("<input type='hidden'>")
                    .addClass('webgis-all-queries')
                    .appendTo($(map.elem));

                map.ui.refreshUIElements();

                customEvents["identify-all-queries"] = $hidden.val();
                customEvents["identify-map-scale"] = map.scale();
                customEvents["identify-srs"] = options.map.crs.id;
                customEvents["sketch-geometry"] = sketch.getGeometryType();
                customEvents["current-activetool-id"] = map.getActiveTool() ? map.getActiveTool().id : null;

                $hidden.remove();

                if (projectedCoords) {
                    customEvents["identify-x"] = projectedCoords.x;
                    customEvents["identify-y"] = projectedCoords.y;
                }

                webgis.tools.onButtonClick(options.map, tool, null, null, customEvents);
            }
        }
    }
});

// no element needed: webgis.ui.showSketchFromGeometryDialog({ map: map, sketch: sketch })
// $parent (optional): element the server UI is built into, default: $(null)
webgis.ui.showSketchFromGeometryDialog = function (options, $parent) {
    (webgis.$ || jQuery).fn.webgis_sketchFromGeometry.plugin.open(options, $parent);
};

webgis.ui.definePlugin('webgis_uploadSketch', {
    defaults: {
        map: null
    },
    init: function (rawOptions) {
        this.constructor.open(rawOptions, this.$el);
    },
    staticMethods: {
        // $parent: element the server UI is built into (may be empty, eg. $(null))
        open: function (options, $parent) {
            const $ = webgis.$ || jQuery;
            options = $.extend({}, { map: null }, options);
            $parent = $($parent);

            if (!options.map || !options.sketch)
                return;

            var tool = {
                id: 'webgis.tools.coordinates',
                type: 'servertoolcommand_ext',
                command: 'upload-sketch'
            };

            tool.handleToolResponseCustomHandler = function (response) {
                if (response.ui) {
                    response.ui.map = options.map;
                    response.ui.tool = tool;
                    response.ui.title = null;
                    response.ui.closebutton = false;
                    // Embedded construction-tool popup, not a real tool-dialog (re)build - see comment
                    // in webgis_xyAbsoluteControl for why this matters.
                    response.ui.suppressOnBuildToolUi = true;

                    $parent.webgis_uibuilder(response.ui);
                }
            };

            var customEvents = {}, map = options.map, sketch = options.sketch;

            if (sketch) {
                customEvents["sketch-geometry-type"] = sketch.getGeometryType();
            }

            webgis.tools.onButtonClick(map, tool, null, null, customEvents);
        }
    }
});

webgis.ui.definePlugin('webgis_downloadSketch', {
    defaults: {
        map: null
    },
    init: function (rawOptions) {
        this.constructor.open(rawOptions, this.$el);
    },
    staticMethods: {
        // $parent: element the server UI is built into (may be empty, eg. $(null))
        open: function (options, $parent) {
            const $ = webgis.$ || jQuery;
            options = $.extend({}, { map: null }, options);
            $parent = $($parent);

            if (!options.map || !options.sketch)
                return;

            var tool = {
                id: 'webgis.tools.coordinates',
                type: 'servertoolcommand_ext',
                command: 'download-sketch'
            };

            tool.handleToolResponseCustomHandler = function (response) {
                if (response.ui) {
                    response.ui.map = options.map;
                    response.ui.tool = tool;
                    response.ui.title = null;
                    response.ui.closebutton = false;
                    // Embedded construction-tool popup, not a real tool-dialog (re)build - see comment
                    // in webgis_xyAbsoluteControl for why this matters.
                    response.ui.suppressOnBuildToolUi = true;

                    $parent.webgis_uibuilder(response.ui);
                }
            };

            var customEvents = {}, map = options.map, sketch = options.sketch;

            if (sketch) {
                customEvents["sketch-geometry-type"] = sketch.getGeometryType();
            }

            webgis.tools.onButtonClick(map, tool, null, null, customEvents);
        }
    }
});

webgis.ui.definePlugin('select_sketch', {
    defaults: {
        map: null,
        named_sketches: null,
        close_sketch: false
    },
    init: function (rawOptions) {
        this.constructor.open(rawOptions);
    },
    staticMethods: {
        // no element needed, from outside: webgis.ui.showSelectSketchDialog(options)
        open: function (options) {
            "use strict"; // as in the legacy plugin: 'this' in applyCancel stays undefined (not window)
            const $ = webgis.$ || jQuery;
            options = $.extend({}, {
                map: null,
                named_sketches: null,
                close_sketch: false
            }, options);

            if (!options.map || !options.named_sketches)
                return;

            var activeTool = options.map.getActiveTool();
            var sketchInstance = activeTool && activeTool.tooltype === 'graphics' ?
                options.map.graphics._sketch :
                options.map.sketch;

            if (!sketchInstance)
                return;

            var $blocker = $("<div class='blocker' style='z-index:9999999;position:absolute;left:0px;right:0px;top:0px;bottom:0px;background:rgba(0,0,0,0.5)' oncontextmenu='return false;'></div>")
                .appendTo('body')
                .click(function () {
                    $(this).remove();
                });

            var currentSketch = sketchInstance.store();

            let set_sketch_any = false;
            let zoom_sketch_any = false;
            for (var i in options.named_sketches) {
                set_sketch_any |= options.named_sketches[i].set_sketch !== false;
                zoom_sketch_any |= options.named_sketches[i].zoom_on_preview === true;
            }

            var $menuHolder = $("<div class='webgis-contextmenu icons' style='position:absolute;background:#fff;display:inline-block' >")
                .appendTo($blocker)
                .on('mouseleave', function () {
                    sketchInstance.load(currentSketch);
                    if (zoom_sketch_any) {
                        sketchInstance.zoomTo();
                    }
                });

            var $menu = $("<ul>").addClass('webgis-toolbox-tool-item-group-details').css({
                padding: 0, margin: 0, minWidth: '200px'
            }).appendTo($menuHolder);

            var $title = $("<li>")
                .addClass('webgis-toolbox-tool-item')
                .appendTo($menu)
                .click(function (e) {
                    e.stopPropagation();
                })
                .on('mouseenter', function (e) {
                    sketchInstance.load(currentSketch);
                    if (zoom_sketch_any) {
                        sketchInstance.zoomTo();
                    }
                });

            $("<strong>")
                .text(set_sketch_any ? webgis.l10n.get('take-sketch-from') : webgis.l10n.get('take-values-from'))
                .appendTo($title);

            function applySketch(sender, isPreview) {
                var $sender = $(sender).css('background-image', 'url(' + webgis.css.imgResource('button-ok.png', '') + ')');

                sketchInstance.fromJson($sender.data('sketch'));

                if ($sender.data('zoom_on_preview')) {
                    sketchInstance.zoomTo();
                }
                if (options.close_sketch) {
                    sketchInstance.appendPart();
                }

                if (!isPreview) {
                    if (!$sender.data('set_sketch')) {
                        sketchInstance.load(currentSketch);
                    }

                    var setters = $sender.data('setters');
                    if (setters && setters.length > 0) {
                        for (var setter of setters) {
                            $("#" + setter.id).val(setter.val);
                        }
                    }
                }
            }
            function applyCancel(sender, isPreview) {
                var $sender = $(this).css('background-image', 'url(' + webgis.css.imgResource('button-cancel.png', '') + ')');

                if (set_sketch_any && isPreview) {
                    sketchInstance.load(currentSketch);
                    if (zoom_sketch_any) {
                        sketchInstance.zoomTo();
                    }
                } else {
                    sketchInstance.load(currentSketch);
                }
            };

            for (var i in options.named_sketches) {
                var named_sketch = options.named_sketches[i];

                options.map.graphics._resetGeoJsonType(named_sketch.sketch);

                $("<li></li>")
                    .text(named_sketch.name)
                    .addClass('webgis-toolbox-tool-item')
                    .data('sketch', named_sketch.sketch)
                    .data('set_sketch', named_sketch.set_sketch)
                    .data('setters', named_sketch.setters)
                    .data('zoom_on_preview', named_sketch.zoom_on_preview)
                    .append('<br>')
                    .append($("<span style='color:#aaa;font-size:.8em'>").text(named_sketch.subtext))
                    .appendTo($menu)
                    .on('click', function () {
                        applySketch(this)
                    })
                    .on('mouseenter', function () {
                        applySketch(this, true)
                    })
                    .on('mouseleave', function () {
                        $(this).css('background-image', '');
                    });
            }

            $("<li>").text(webgis.l10n.get('cancel'))
                .addClass('webgis-toolbox-tool-item')
                .appendTo($menu)
                .on('click', function () {
                    applyCancel(this);
                })
                .on('mouseenter', function () {
                    applyCancel(this, true);
                })
                .on('mouseleave', function () {
                    $(this).css('background-image', '');
                });

            var pos = [10, 10];
            if (options.map.elem) {
                var offset = $(options.map.elem).offset();
                pos[0] += offset.left;
                pos[1] += offset.top;
            }

            $menuHolder.css({
                'left': pos[0], 'top': pos[1]
            });
        }
    }
});

// no element needed: webgis.ui.showSelectSketchDialog({ map: map, named_sketches: [...], close_sketch: false })
webgis.ui.showSelectSketchDialog = function (options) {
    (webgis.$ || jQuery).fn.select_sketch.plugin.open(options);
};
