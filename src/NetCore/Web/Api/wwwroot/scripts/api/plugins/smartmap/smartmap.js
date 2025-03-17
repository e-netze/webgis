// Plugin zum schnellen Suchen und hinspringen in der Karte. Das Suchfeld befindet sich außerhalb der Karte.
// Außerdem kann (falls eingestellt) ein Sketch basierend auf einem Editthema gezeichnet werden.
// Soll bspw. für Störungsübersicht (für Externe) eingesetzt werden: Zoom auf Standort => befinden sich Störungen in der Nähe

(function (webgis) {
    "use strict"

    webgis.addPlugin(new function () {

        this.onInit = function () {

        };

        this.onMapCreated = function (map, container) {

        }
    });
})(webgis);


(function ($) {
    $.fn.webgis_smartmap = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_smartmap');
        }
    };

    var defaults = {
        map_options: null,
        edit_service: '',
        edit_themeid: '',
        query_themeid: '',
        on_save: null,
        on_init: null,
        quick_tools: 'webgis.tools.navigation.currentPos',
        quick_search_category: '',
        quick_search_placeholder: 'Geben Sie einen Suchbegriff ein',
        quick_search_map_scale: 10000
    };

    var ua = window.navigator.userAgent;

    var methods = {
        init: function (options) {
            var $this = $(this);

            options = $.extend({}, defaults, options);
            if (options.quick_search_placeholder.length === 0)
                options.quick_search_placeholder = defaults.quick_search_placeholder;
            if (options.quick_search_map_scale == '')
                options.quick_search_map_scale = defaults.quick_search_map_scale;
            if (options.query_themeid == '')
                options.query_themeid = defaults.query_themeid;

            return this.each(function () {
                var eventHandlers = {};
                webgis.implementEventController(eventHandlers);
                $(this).data('eventHandlers', eventHandlers);
                new initUI(this, options);
            });
        },
        getMap: function () {
            return getMap($(this).find("#map"))
        },
        dataenriched: function (obj) {
            var data = obj.enrichedData;
            saveFeature($(this), data);
        },
    };

    var initUI = function (elem, options) {
        var $elem = $(elem);
        $elem.addClass('webgis-plugin-smartmap-container webgis-container-styles');

        createSearchBar(options, $elem);

        var $mapContent = $("<div class='content'></div>").appendTo($elem);
        createMap(options, $mapContent);

        var isEditable = false;
        if (options.edit_service.length > 0 && options.edit_themeid.length > 0)
            isEditable = true;

        $(window).resize(function () {
            $('.webgis-container').each(function (i, e) {
                $(e).css({
                    height: parseInt($(e).parent().height() - (isEditable ? 80 : 50))
                });
            });
            if ($("#map").length > 0)
                getMap($("#map")).invalidateSize();  // Karte an aktuelle DIV Größe anpassen (für jeden Browser)

        });
        $(window).trigger('resize');

        // Editmaske befüllen
        if (isEditable) {
            getEditThemeCapabilities(options, function (result) {
                if (result.success == false) {
                    if (result.exception)
                        $("<span>" + result.exception + "</span>").prependTo($elem);
                    return;
                }
                $elem.data('editThemeCapabilities', result.classes[0]);
                createEditmask(result.classes[0], $elem, options);
            });
        } else {
            console.log("Info: Kein Edithema gewählt");
        }

        // Wenn alles fertig ist:
        if (options.on_init)
            options.on_init({
                map: getMap($elem),
                webgisContainer: $mapContent.closest('.webgis-plugin-smartmap-container').find('.webgis-container')
            });
        // Events (zum Scrolling) registrieren, damit die Schnellsuche Vorschläge beim Scrollen nicht verschwinden, sondern "mitwandern"
        webgis.registerUIEvents($mapContent.closest('.webgis-plugin-smartmap-container'));
    }

    var createSearchBar = function (options, $target) {
        var $searchBar = $("<input class='smartmap-searchbar ' name='searchAdress' type='text' placeholder='" + options.quick_search_placeholder + "'/>").appendTo($target);

        $searchBar.webgis_control_search({
            css_display: 'inline',
            search_service: options.quick_search_service,
            search_categories: options.quick_search_category,
            on_select_get_original: false,
            on_select_get_original_raw: false,
            on_select_get_original_fullgeometry: false,
            on_select: function (sender, feature, original) {
                //console.log(sender);
                //console.log(feature);
                ///console.log(original);

                //if (typeof (feature.features) != "undefined" && feature.features.length > 0) {
                if (original == false && typeof(feature) != "undefined") {
                    var map = getMap($target);
                    var lat = feature.coords[1], lon = feature.coords[0];

                    var marker = map.addMarker({
                        lat: lat,
                        lng: lon,
                        icon: 'blue',
                        //text: p.STR + " " + p.HNR + ", &nbsp;<br/>" + p.PLZ + " " + p.ORT,
                        text: feature.label.replace(",", "<br/>"),
                        openPopup: true
                        /*
                        ,buttons: [{
                            label: 'Marker entfernen',
                            onclick: function (map) {
                                map.removeMarker(marker);
                            }
                        }]
                        */
                    });
                    
                    if (options.quick_search_map_scale != null) {
                        map.zoomTo([lon, lat, lon, lat]);
                        if (map.scale() < parseInt(options.quick_search_map_scale))
                            map.setScale(parseInt(options.quick_search_map_scale), map.getCenter());
                    }

                    $searchBar.closest('.webgis-plugin-smartmap-container')
                        .data('eventHandlers')
                        .events
                        .fire('onfeaturefound',{ feature: feature, marker: marker });
                }
            }
        });
        var $typeahead = $searchBar.closest(".twitter-typeahead").css("z-index", 500);
        $typeahead.find(".tt-menu.tt-open").css("z-index", 500);
    }

    var createEditmask = function (editThemeCapabilities, $target, options) {
        var map = getMap($target);
        var $editPanel = $("<div class='smartmap-editpanel'></div>").insertBefore($target.find(".content"));       // .find(".content")
        var $formInput = $("<form class='smartmap-form-input' method='post' enctype='multipart/form-data' action=''></form>").appendTo($target);

        // Geometrietyp
        var tooltype = 'sketch0d';
        var geomtype = "Punkt";
        switch (editThemeCapabilities.geometrytype) {
            case 'line':
                tooltype = 'sketch1d';
                geomtype = "Linien";
                break;
            case 'polygon':
                tooltype = 'sketch2d';
                geomtype = "Flächen";
                break;
        }

        var $editButton = $("<div class='smartmap-editbutton'><div class='smartmap-editsymbol edit'></div><span>Zeichnen</span></div>")
            .on("click", function () {
                $('.smartmap-editbutton.selected').removeClass('selected');
                $(this).addClass('selected');
                map.setActiveTool(map.getTool('smartmap-edit'));
            })
            .appendTo($editPanel);

        if (options.query_themeid != "") {
            var $deleteButton = $("<div class='smartmap-editbutton'><div class='smartmap-editsymbol delete'></div><span>Löschen</span></div>")
                .on("click", function () {
                    map.sketch.remove();
                    $('.smartmap-editbutton.selected').removeClass('selected');
                    $(this).addClass('selected');
                    map.setActiveTool(map.getTool('smartmap-delete'));
                })
                .appendTo($editPanel);
        }

        var $saveButton = $("<div class='smartmap-editbutton inactive'><div class='smartmap-editsymbol save'  ></div><span>Speichern</span></div>")
            .on("click", function () {
                if (map.sketch.isValid())
                    saveFeature(editThemeCapabilities, $formInput);
                else
                    alert("Zeichnen Sie bitte eine " + geomtype + "-Geometrie mit Klick in die Karte ein.");
            })
            .appendTo($editPanel);


        for (var f in editThemeCapabilities.fields) {
            var field = editThemeCapabilities.fields[f];

            // Wert aus RequestString ODER Standardwert aus XML
            var defaultValue = (typeof field.default_value != "undefined" ? field.default_value : "");
            if (typeof editThemeCapabilities.url_param_mapping != "undefined") {
                $.grep(editThemeCapabilities.url_param_mapping, function (e) {
                    if (e.target == field.name) {
                        var urlValue = getParameterByName(e.source, window.location.search);
                        defaultValue = urlValue != "" ? urlValue : defaultValue;
                    }
                });
            }

            $("<input class='smartmap-input' type='hidden'" + (field.required ? " required " : " ") + "name='editfield_" + field.name + "' type='" + (field.password ? "password " : "text") + "' " + (field.readonly ? "readonly " : " ") + (field.required ? "required " : " ") + " value='" + defaultValue + "' />")
            .appendTo($formInput)
            .change(function () {
                $(this).closest('.webgis-plugin-smartmap-container').data('eventHandlers').events.fire('change', { field: $(this).attr('data-field'), value: $(this).val() });
            });
        }

        // Map tools
        map.addTool({
            name: 'edit',
            type: 'clienttool',
            tooltype: tooltype,
            id: 'smartmap-edit',
        });
        map.addTool({
            name: 'delete',
            type: 'clienttool',
            tooltype: 'click',
            id: 'smartmap-delete',
            onmapclick: function (sender, click) {
                deleteFeature($deleteButton, click.world);
            }
        });

        $editButton.trigger('click');
        map.sketch.events.on('onchanged', function (channel, sender) {
            if (sender.isValid()) {
                $saveButton.removeClass("inactive");
                $editPanel.closest('.webgis-plugin-smartmap-container').data('eventHandlers').events.fire('onsketchchanged', { sketch: sender });
            }
            else
                $saveButton.addClass("inactive");
        });
    }

    var createMap = function (options, $target) {
        $target.addClass('webgis-container').css({ position: 'relative' });

        let $map = $("<div id='map' style='position:absolute;left:0px;top:0px;right:0px;bottom:0px'></div>").appendTo($target);
        $("<div data-tools='" + options.quick_tools + "'></div>")
            .addClass('webgis-tool-button-bar shadow')
            .css({
                position: 'absolute',
                left: '9px',
                top: '109px'
            })
            .appendTo($target);

        var map = webgis.createMap($map, options.map_options);
        map.refresh();

        let basemapServices = [];
        for (var serviceId in map.services) {
            let service = map.services[serviceId];
            if (service.isBasemap && service.basemapType === 'normal') {
                basemapServices.push(service);
            }
        }

        if (basemapServices.length > 1) {
            let $basemaps = $("<div>")
                .addClass('webgis-basemaps-bar')
                .appendTo($target);
            $("<button>")
                .addClass('webgis-button uibutton uibutton-cancel')
                .text('Hintergrund')
                .appendTo($basemaps)
                .click(function () {
                    $(this).parent().toggleClass('expanded');
                });
            let $basemapButtons = $("<div>").addClass('basemap-buttons').appendTo($basemaps);

            for (var basemapService of basemapServices) {
                $("<button>")
                    .addClass('webgis-button uibutton uibutton-cancel')
                    .data('serviceId', basemapService.id)
                    .text(basemapService.name)
                    .appendTo($basemapButtons)
                    .click(function (e) {
                        e.stopPropagation();

                        let $this = $(this);

                        $this.parent().children('button').addClass('uibutton-cancel');  // unselect all
                        map.setBasemap($this.removeClass('uibutton-cancel').data('serviceId'));  // set basemap and select button
                        $this.closest('.webgis-basemaps-bar').removeClass('expanded');  // close/collapse button bar
                        
                    });
            }
            $basemapButtons.children(':first').removeClass('uibutton-cancel');
        }

        $target.closest('.webgis-plugin-smartmap-container').data('map', map);
        $target.closest('.webgis-plugin-smartmap-container').data('options', options);
    }

    var getMap = function ($elem) {
        return $elem.closest('.webgis-plugin-smartmap-container').data('map');
    }

    var saveFeature = function (editThemeCapabilities, $form) {
        var map = getMap($form);
        var data = new Object();

        for (var f in editThemeCapabilities.fields) {
            var field = editThemeCapabilities.fields[f];
            if (field.name == null)
                continue;
            data["editfield_" + field.name] = $(".smartmap-input[name='editfield_" + field.name + "']").val();
        }

        data["_sketch"] = map.sketch.toWKT(true);
        map.sketch.remove();
        data["_sketchSrs"] = 4326;

        var eventData = {};
        $form.closest('.webgis-plugin-smartmap-container').data('eventHandlers').events.fire('onfeatureprepared', { data: eventData });

        for (var attr in eventData) {
            data["editfield_" + attr] = eventData[attr];
        }

        var options = $form.closest('.webgis-plugin-smartmap-container').data("options");

        webgis.ajax({
            url: webgis.baseUrl + '/rest/services/' + options.edit_service + '/edit/' + options.edit_themeid + '/insert',
            data: $.extend({}, webgis.hmac.appendHMACData({ responseformat: 'framed', callbackchannel: $(this).attr('id') }), data),
            type: 'post',
            success: function (result) {
                if (result.success == true) {
                    //$button.html("Erfolgreich gespeichert<br/>Neuen Störfall erfassen?").addClass("close").removeAttr("style");
                    $form.closest('.webgis-plugin-smartmap-container').data('eventHandlers').events.fire('save', { success: true, result: data });
                }
                else {
                    alert("Fehler: " + result.exception);
                }
                map.refresh();
            }
        });
    };

    var deleteFeature = function (sender, coords) {
        
        var options = $(sender).closest('.webgis-plugin-smartmap-container').data("options");
        var map = getMap($(sender).closest('.webgis-plugin-smartmap-container'));

        // Abfragen -> Abfragethema muss parametriert werden
        if (options.query_themeid.length == 0)
            return false;

        var data = {
            c: "identify",
            shape: "MULTIPOINT((" + coords.lng + " " + coords.lat + ")",
            shape_srs: 4326,
            f: "json",
            renderfields: false,
            geometry: "none"
        };
        data = map.appendRequestParameters(data, options.edit_service);

        console.log(coords);

        webgis.ajax({
            url: webgis.baseUrl + '/rest/services/' + options.edit_service + '/queries/' + options.query_themeid,
            type: 'get',
            data: webgis.hmac.appendHMACData(data),
            dataType: 'json',
            success: function (result) {
                if (result.features.length > 0) {
                    var oid = result.features[0].oid;       // "edit_test@ccgis_default:hilfsflaeche:13"
                    oid = oid.substring(oid.lastIndexOf(":") + 1);

                    data = {};
                    data = map.appendRequestParameters(data, options.edit_service);
                    data["oid"] = oid;
                    
                    webgis.ajax({
                        url: webgis.baseUrl + '/rest/services/' + options.edit_service + '/edit/' + options.edit_themeid + '/delete',
                        data: $.extend({}, webgis.hmac.appendHMACData({
                            responseformat: 'framed',
                            callbackchannel: $(this).attr('id')
                        }), data),
                        type: 'post',
                        success: function (result) {
                            if (result.success == true) {
                                map.refresh();
                            }
                            else {
                                alert("Fehler: " + result.exception);
                            }
                        }
                    });

                } else {
                    console.log("Kein Feature gefunden.");
                }

            }
        });
    };

    var getEditThemeCapabilities = function (options, callback) {
        webgis.ajax({
            url: webgis.baseUrl + '/rest/services/' + options.edit_service + '/edit/' + options.edit_themeid + '/capabilities',
            data: webgis.hmac.appendHMACData({}),
            type: 'get',
            success: function (result) {
                callback(result);
            }
        });
    };

    var getParameterByName = function (name, url) {
        if (!url) url = window.location.href;
        name = name.replace(/[\[\]]/g, "\\$&");
        var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
            results = regex.exec(url);
        if (!results) return "";
        if (!results[2]) return "";
        return decodeURIComponent(results[2].replace(/\+/g, " "));
    }
})(webgis.$ || jQuery);