// WebGIS UI Builder Combo Elements
// select, querycombo, editthemecombo, editthemetree, chainagethemecombo, labelingcombo,
// circleradiuscombo, print-query-labelfield-combo, print-coordinates-labelfield-combo, sketchselect
// (plugins: webgis.ui.plugins.multi_select.js, webgis.ui.plugins.editthemeCombo.js)
//
// The combo plugins (webgis_queryCombo, webgis_editthemeCombo, ...) live in their own files,
// because they are also used outside of the UI builder.

(function ($) {
    "use strict";

    webgis.ui.builder["querycombo.tagname"] = "select";
    webgis.ui.builder["querycombo"] = (map, $newElement, element, options) => {
        $newElement.webgis_queryCombo({ map: options.map, 'type': element.combotype, customitems: element.customitems });
    };

    webgis.ui.builder["editthemecombo.tagname"] = "select";
    webgis.ui.builder["editthemecombo"] = (map, $newElement, element, options) => {
        $newElement.webgis_editthemeCombo({ map: options.map, customitems: element.customitems, dbrights: element.db_rights });
    };

    webgis.ui.builder["editthemetree"] = (map, $newElement, element, options) => {
        $newElement.webgis_editthemeTree({ id: element.id, map: options.map, customitems: element.customitems, dbrights: element.db_rights });
    };

    webgis.ui.builder["chainagethemecombo.tagname"] = "select";
    webgis.ui.builder["chainagethemecombo"] = (map, $newElement, element, options) => {
        $newElement.webgis_chainageCombo({ map: options.map });
    };

    webgis.ui.builder["labelingcombo.tagname"] = "select";
    webgis.ui.builder["labelingcombo"] = (map, $newElement, element, options) => {
        $newElement.webgis_labelingCombo({ map: options.map, val: element.value });
    };

    webgis.ui.builder["circleradiuscombo.tagname"] = "select";
    webgis.ui.builder["circleradiuscombo"] = (map, $newElement, element) => {
        $newElement
            .data('map', map)
            .change(function () {
                var map = $(this).data('map');
                map.showCircleMarker(null, parseInt($(this).val()));
            });
        var radii = element.radii || webgis.const.circleMarkerRadii;
        for (var r in radii) {
            var radius = radii[r];
            $("<option value='" + radius + "' " + (radius === map._circleMarkerOptions.radius ? "selected='selected'" : "") + ">" + radius.toString().numberWithCommas() + " m</option>").appendTo($newElement);
        }
    };

    webgis.ui.builder["print-query-labelfield-combo.tagname"] = "select";
    webgis.ui.builder["print-query-labelfield-combo"] = (map, $newElement) => {
        $("<option value=''>--- Beschriften (optional) ---</option>").appendTo($newElement);
        var feature = map.queryResultFeatures.first();
        if (feature && feature.properties) {
            var featureProperties = Array.isArray(feature.properties) ?
                (feature.properties.length > 0 ? feature.properties[0] : {}) :
                feature.properties;

            for (var property in featureProperties) {
                if (isPrintLabelProperty(property, featureProperties[property])) {
                    $("<option value='" + property + "'>" + printLabelPropertyDisplayname(property) + "</option>").appendTo($newElement);
                }
            }
        }
        $newElement.webgis_multi_select({ separator: ';', map: map });
    };

    webgis.ui.builder["print-coordinates-labelfield-combo.tagname"] = "select";
    webgis.ui.builder["print-coordinates-labelfield-combo"] = (map, $newElement, element) => {
        if (!element.showCoordinatePairsOnly) {
            $("<option value=''>--- Beschriftung (optional) ---</option>").appendTo($newElement);
            $newElement.webgis_multi_select({ separator: ';', map: map });
        }
        var feature = map.queryResultFeatures.firstToolResult('webgis.tools.coordinates');
        if (feature && feature.properties) {

            for (var property in feature.properties) {
                var propertyValue = feature.properties[property];
                if (isPrintLabelProperty(property, propertyValue)) {
                    let isPair = true;
                    if (propertyValue instanceof Array) {
                        // Koordinatenpaar => genau 2 Werte müssen angegeben sein.
                        var count = 0;
                        $.each(propertyValue, function (i, val) { if (val) count++; });
                        if (count !== 2) {
                            isPair = false;
                        }
                    } else {
                        isPair = false;
                    }
                    if (element.showCoordinatePairsOnly === true && isPair == false) {
                        continue;
                    }
                    $("<option value='" + property + "'>" + printLabelPropertyDisplayname(property) + "</option>").appendTo($newElement);
                }
            }
        }
    };

    webgis.ui.builder["sketchselect.tagname"] = "select";
    webgis.ui.builder["sketchselect"] = (map, $newElement) => {
        var currentToolSketches = map.currentToolSketches();

        $newElement
            .data('current-tool-sketches', currentToolSketches)
            .data('map', map);

        $("<option value=''>").text('--- ' + webgis.l10n.get('do-not-show') + ' ---').appendTo($newElement);

        for (var c in currentToolSketches) {
            var currentToolSketch = currentToolSketches[c];
            $("<option value='" + currentToolSketch.id + "'>" + currentToolSketch.name + "</option>").appendTo($newElement);
        }

        $newElement.change(function () {
            var val = $(this).val(), map = $(this).data('map');
            var currentToolSketch = $.grep($(this).data('current-tool-sketches'), function (n, i) { return n.id === val });
            if (currentToolSketch.length === 1) {
                map.sketch.load(currentToolSketch[0].storedSketch, map.getActiveTool() ? map.getActiveTool().id : null);
            } else {
                map.sketch.remove();
            }
        });
    };

    webgis.ui.builder["select.tagname"] = "select";
    webgis.ui.builder["select.formelement"] = true;
    webgis.ui.builder["select"] = (map, $newElement, element, options) => {
        if (element.allow_addvalues === true) {
            $newElement.webgis_extCombo({ inputType: 'number' });
        }
        if (element.options && element.options.length > 0) {
            for (var o = 0; o < element.options.length; o++) {
                $("<option value='" + element.options[o].value + "'" + (element.defaultvalue && element.options[o].value == element.defaultvalue ? "selected='selected'" : "") + ">" + element.options[o].label + "</option>").appendTo($newElement);
            }
        }
        else if ($newElement.hasClass('webgis-map-scales-select')) {
            var scale = map.scale();
            var scales = map.scales(), selectedScale = -1;
            for (var s in scales) {
                var scaleValue = Math.round(scales[s] / 10) * 10;
                $("<option value='" + scaleValue + "'>1:" + scaleValue.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ".") + "</option>").appendTo($newElement); // mit tausender Punkten

                if (selectedScale < 0 || scale < scaleValue) {
                    selectedScale = scaleValue;
                }
            }
            $newElement.val(selectedScale);
        }
        var select = $newElement.get(0);
        if (element.changecommand && element.changecommand !== 'unknown') {
            select.changetype = element.changetype;
            select.toolId = element.changetool || select.id;
            select.changecommand = element.changecommand;
            select.map = options.map;
            $newElement.change(function () {
                webgis.tools.onButtonClick(this.map, { command: this.changecommand, type: this.changetype, id: this.toolId, map: this.map, value: $(this).val() }, this);
            });
        }
        if (element.dependency_field_ids && element.dependency_field_ids.length > 0) {
            $newElement
                .addClass('webgis-ui-added')
                .addClass('webgis-select-has-dependency-field-ids')
                .data('dependency-field-ids', element.dependency_field_ids)
                .data('dependency_field_ids_callback_toolid', element.dependency_field_ids_callback_toolid);
        }
        if ($newElement.hasClass('webgis-print-show-query-markers-select')) {
            $newElement.change(function () {
                map.queryResultFeatures.setMarkerVisibility($(this).val() === 'show' ? true : false);
            });
        }
        if ($newElement.hasClass('webgis-print-show-coordinate-markers-select')) {
            $newElement.change(function () {
                map.queryResultFeatures.setToolMarkerVisibility('webgis.tools.coordinates', $(this).val() === 'show' ? true : false);
            });
        }
        if ($newElement.hasClass('webgis-print-show-chainage-markers-select')) {
            $newElement.change(function () {
                map.queryResultFeatures.setToolMarkerVisibility('webgis.tools.chainage', $(this).val() === 'show' ? true : false);
            });
        }

        if (element.allow_pro_behaviour === true && webgis.usability.select_pro_behaviour === "select2") {
            webgis.require('select2', function ($select) {
                $select.select2({ width: '100%' });
            }, $newElement);
        }
    };

    // print label combos: only "real" attributes and some special properties can be used as label
    var isPrintLabelProperty = function (property, propertyValue) {
        if (property.indexOf('_') === 0) {
            switch (property) {
                case '_fulltext':
                case '_label':
                    break;
                default:
                    return false;
            }
        }

        if (typeof propertyValue === 'string') {
            propertyValue = propertyValue.trim();
            if (propertyValue.length > 0 && propertyValue[0] === "<" && propertyValue[propertyValue.length - 1] === ">") {
                return false;
            }
        }

        return true;
    };
    var printLabelPropertyDisplayname = function (property) {
        switch (property) {
            case '_fulltext':
                return '(Kurz) Zusammenfassung';
            case '_label':
                return 'Beschriftung';
            default:
                return property;
        }
    };
})(webgis.$ || jQuery);
