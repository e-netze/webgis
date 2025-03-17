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
    $.fn.webgis_collector_pro = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_collector_pro');
        }
    };

    var defaults = {
        map_options: null,
        edit_service: '',
        edit_themeid: '',
        on_save: null,
        on_init: null,
        quick_tools: 'webgis.tools.navigation.currentPos',
        tabs: ["Attributieren", "Verorten", "Speichern"],
        saveButtonText: "Erfolgreich gespeichert<br/>Neue Erfassung starten?",
        allow_multipart: false
    };


    var ua = window.navigator.userAgent;


    var methods = {
        init: function (options) {
            var $this = $(this);
            var options = $.extend({}, defaults, options);
            options.saveButtonText = options.saveButtonText.replace("&lt;", "<").replace("&gt;", ">").replace("&sol;", "/");
            if (options.saveButtonText.length === 0)
                options.saveButtonText = defaults.saveButtonText;

            return this.each(function () {
                var eventHandlers = {};
                webgis.implementEventController(eventHandlers);
                $(this).data('eventHandlers', eventHandlers);
                $(this).data('eventHandlers').events.on('change', function (channel, args) {
                    // Wenn bei Pflichtfeld Text eingeben wurde => Hervorhebung entfernen
                    var $elem = $(".collectorpro-input[name='editfield_" + args.field + "']");
                    if ($elem.attr("required") == "required") {
                        $elem.addClass("required");
                        $elem.siblings(".select2").find(".select2-selection").addClass("required"); // Wenn Auswahlbox mit select2
                        $elem.parent(".wrapperRadioCheck").addClass("required");
                        $elem.parent(".wrapperRadioCheck").siblings(".collectorpro-input-label").addClass("required");
                        $elem.siblings(".collectorpro-input-label").addClass("required");
                        if (($elem.attr("type") == "checkbox" || $elem.attr("type") == "radio") && $elem.is(":checked")) {
                            $elem.parent(".wrapperRadioCheck").removeClass("required");
                            $elem.parent(".wrapperRadioCheck").siblings(".collectorpro-input-label").removeClass("required");
                        } else if (args.value != null && args.value.length > 0) {
                            $elem.removeClass("required");
                            $elem.siblings(".collectorpro-input-label").removeClass("required");
                            $elem.siblings(".select2").find(".select2-selection").removeClass("required");  // Wenn Auswahlbox mit select2
                            // Nur für Fileupload
                            if ($elem.data("type") == "file") {
                                $elem.siblings("div").removeClass("required");
                                $elem.closest("div").siblings(".collectorpro-input-label").removeClass("required");
                            }
                        }
                    }
                });

                new initUI(this, options);
            });
        },
        getValue: function (fieldname) {
            //var $this = $(this);
            return $(".collectorpro-input[name='editfield_" + fieldname + "']").val();
        },
        setValue: function (obj) {
            //var $this = $(this);
            $(".collectorpro-input[name='editfield_" + obj.fieldname + "']").val(obj.value).change();
        },
        hideElement: function (fieldname) {
            $(".collectorpro-input[name='editfield_" + fieldname + "']").closest(".wrapper-input").hide();
        },
        showElement: function (fieldname) {
            $(".collectorpro-input[name='editfield_" + fieldname + "']").closest(".wrapper-input").show();
        },
        getQuickSearchService: function () {
            return ($(".collectorpro-input[name='searchAdress']").length == 1) ? $(".collectorpro-input[name='searchAdress']").data("service") : "";
        },
        refreshSketch: function () {
            refreshSketch(getMap($(this).find("#map")));
        },
        getMap: function () {
            return getMap($(this).find("#map"))
        },
    };

    var initUI = function (elem, options) {
        var $elem = $(elem);
        $elem.addClass('webgis-plugin-collectorpro-container webgis-container-styles');

        // 3 Schritte -> Attributieren, Verorten, Speichern
        createTabs($elem, options);

        // Karte laden, danach auf ersten Tab schalten
        $(".tabHeaderPart").first().addClass("selected");
        $elem.find('.tab2').show();
        createMap(options, $elem.find('.tab2').children('.content'));
        $elem.find('.tab2').hide();
        $(".tab").first().show();

        $(window).resize(function () {
            $('.webgis-container').each(function (i, e) {
                $(e).css({
                    height: parseInt($(e).parent().height() - 58)
                });
            });
            if ($("#map").length > 0)
                getMap($("#map")).invalidateSize();  // Karte an aktuelle DIV Größe anpassen (für jeden Browser)

        });
        $(window).trigger('resize');

        // Editmaske befüllen
        var editThemeCapabilities;
        getEditThemeCapabilities(options, function (result) {
            if (result.success == false) {
                if (typeof (result.exception) !== "undefined")
                    $("<span style='color:orange;'>Fehler: " + result.exception + "</span>").prependTo($elem.find(".tabs .tab").first());
                return;
            }
            $elem.data('editThemeCapabilities', result.classes[0]);
            createEditmask(result.classes[0], $elem.find('.tab1').children('.content'), options);
        });

        // Buttons zum Weiter / Zurück schalten
        $(".button").click(function () {
            if ($(this).hasClass("close")) {
                var s = decodeURIComponent(window.location.search);
                window.location.search = "?app=" + getParameterByName("app", s) + "&category=" + getParameterByName("category", s);
                return;
            }

            if ($(this).hasClass("save")) {
                $(this).removeClass("save");
                if ($("form.collectorpro-form-input")[0].checkValidity() == true && getMap($("#map")).sketch.toWKT() != "") {
                    var records = serializeTable($elem.find('.tab3').children('.content'));
                    //console.log(records);

                    var $button = $(this);
                    var oldText = $(this).html();
                    var oldWidth = $(this).outerWidth();
                    $(this).html("<div class='loaderimage'></div>").outerWidth(oldWidth);

                    webgis.ajax({
                        url: webgis.baseUrl + '/rest/services/' + options.edit_service + '/edit/' + options.edit_themeid + '/insert',
                        data: $.extend({}, webgis.hmac.appendHMACData({ responseformat: 'framed', callbackchannel: $(this).attr('id') }), records),
                        type: 'post',
                        success: function (result) {
                            var myResult;
                            if (result.success == true) {
                                myResult = records;
                                $button.html(options.saveButtonText).addClass("close").removeAttr("style");
                                $button.closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('save', { success: true, result: myResult });
                            }
                            else {
                                alert("Fehler: " + result.exception);
                            }
                            //getMap($("#map")).sketch.remove();
                        }
                    });
                } else {
                    $(this).addClass("save");
                    alert("Bitte füllen Sie die markierten Felder aus.");
                }
                return;
            }

            var index = $(".tabHeaderPart.selected").index();
            var step = 1;
            if ($(this).hasClass("back"))
                step = -1;
            $(".tabHeaderPart:eq(" + eval(index + step) + ")").click();
        });
    }

    var createTabs = function ($elem, options) {
        var $tabs = $("<div class='tabs'></div>");
        var $tabHeader = $("<div class='tabHeader'></div>");
        for (var tabIndex in options.tabs) {
            var tabName = options.tabs[tabIndex];
            var tabClass = 'tab' + (parseInt(tabIndex) + 1);
            var $tab = $("<div class='tab " + tabClass + "'> <div class='content'></div> </div>").appendTo($tabs);
            // Floating einschränken
            var floatStyle = "style='float:left;'";
            var $tabHeaderPart = $("<div class='tabHeaderPart' " + floatStyle + "'></div>").data('tab', tabClass);
            var $tabHeaderIcon = $("<div class='tabHeaderIcon icon" + tabClass + "'></div>").appendTo($tabHeaderPart);
            $("<span>" + tabName + "</span>").appendTo($tabHeaderPart);
            $tabHeaderPart.appendTo($tabHeader);

            // Weiter / Zurück Schalftflächen
            if (tabIndex == 0)
                var $buttonBackForward = $("<div class='button forward'>Weiter »</div>").appendTo($tab);
            else if (tabIndex >= options.tabs.length - 1)
                var $buttonBackForward = $("<div class='button back'>« Zurück</div><div class='button forward save'>✓ Speichern</div>").appendTo($tab);
            else
                var $buttonBackForward = $("<div class='button back'>« Zurück</div><div class='button forward'>Weiter »</div>").appendTo($tab);
        }
        $("<div class='tabHeaderPart stretch'></div>").appendTo($tabHeader); // Element für restliche Breite
        $tabHeader.appendTo($elem);
        $tabs.appendTo($elem);

        $(".tabHeaderPart").click(function () {
            var $collector = $(this).closest('.webgis-plugin-collectorpro-container');
            $(".tabHeaderPart").removeClass("selected");
            $(this).addClass("selected");

            $collector.find(".tab").hide();
            var clickedTab = $(this).data("tab");
            $collector.find('.' + clickedTab).show();

            switch (clickedTab) {
                case 'tab2':
                    if ($collector.find("#map").length > 0) {
                        var myMap = getMap($collector.find("#map"));
                        myMap.invalidateSize();  // Karte an aktuelle DIV Größe anpassen (für jeden Browser)
                        myMap.refresh();
                        myMap.sketch.zoomTo();
                        if (myMap.scale() < 1000)
                            myMap.setScale(1000, myMap.getCenter());
                    }
                    break;
                case 'tab3':
                    updateSummary($collector.data('editThemeCapabilities'), $collector.find('.tab3').children('.content'));
                    break;
            }
        });
    }

    var createEditmask = function (editThemeCapabilities, $target, options) {
        var map = getMap($target);
        var $formInput = $("<form class='collectorpro-form-input' method='post' enctype='multipart/form-data' action=''></form>").appendTo($target);

        var ieHelp = setCategories(editThemeCapabilities);
        var categories = ieHelp[0], defaultCatIndex = ieHelp[1], helpCategories = ieHelp[2];

        var $categories = [];
        var searchAdressIndex = -1;
        for (var i in categories) {
            var isExpanded = (typeof categories[i].collapsed != "undefined" && categories[i].collapsed == true) ? false : true;
            $categories.push($("<li class='collectorpro-presentation_toc-title collectorpro-presentation_toc-collapsable " + (isExpanded ? "webgis-expanded" : "") + "' style='display:block;'>" +
                "<span class='collectorpro-presentation_toc-plus webgis-api-icon webgis-api-icon-triangle-1-" + (isExpanded ? "e" : "s") + "' style='position:absolute'></span>" +
                "<div class='collectorpro-presentation_toc-title-text'>" + categories[i].name + "</div>" +
                "<div id='div_' class='collectorpro-presentation_toc-content' style='white-space: normal; overflow:hidden; display: " + (isExpanded ? "block" : "none") + ";'></div>" +
                "<div class='category-description'>" + ((typeof categories[i].description != "undefined") ? categories[i].description : "") + "</div>" +
                "</li>"));

            if (typeof (editThemeCapabilities.categories) != "undefined" && typeof (editThemeCapabilities.categories[i].quick_search_service) != "undefined") {
                searchAdressIndex = i;
            }
        }
        $formInput.append($categories);

        for (var f in editThemeCapabilities.fields) {
            var field = editThemeCapabilities.fields[f];

            var catIndex = helpCategories.indexOf(field.category);
            if (catIndex == -1) {
                catIndex = defaultCatIndex;
            }

            var $catContent = $categories[catIndex].find(".collectorpro-presentation_toc-content");
            var $wrapperInput = $("<div class='wrapper-input'></div>").appendTo($catContent);

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

            if (field.visible == false) {
                $("<input class='collectorpro-input' type='hidden' name='editfield_" + field.name + "' data-field='" + field.name + "' value='" + defaultValue + "' />")
                    .appendTo($wrapperInput)
                    .change(function () {
                        $(this).closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('change', { field: $(this).attr('data-field'), value: $(this).val() });
                    });
            } else {
                $("<div class='collectorpro-input-label" + (field.required ? " required " : " ") + "'>" + field.prompt + "</div>").appendTo($wrapperInput);

                if (field.type == 'domain' && field.domainvalues != null) {
                    var $select = $("<select class='collectorpro-input" + (field.required ? " required " : " ") + "' name='editfield_" + field.name + "' data-field='" + field.name + "'" + (field.readonly ? "readonly='readonly'" : "") + (field.required ? "required " : " ") + " ></select>")
                        .appendTo($wrapperInput)
                        .change(function () {
                            $(this).closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('change', { field: $(this).attr('data-field'), value: $(this).val() });
                        });
                    for (var d in field.domainvalues) {
                        $("<option value='" + (field.domainvalues[d].value == null ? "" : field.domainvalues[d].value) + "'>" + field.domainvalues[d].label + "</option>").appendTo($select);
                    }
                    if ($.fn.select2 && field.domainvalues.length > 20) {
                        $select.select2({ dropdownAutoWidth: true });
                        $select.siblings(".select2-container").css("width", "");
                    }
                    if (defaultValue != "") {
                        $select.val(defaultValue).change();
                        $select.closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('change', { field: $select.attr('data-field'), value: defaultValue });
                    }
                    if (defaultValue == "" && field.required) {
                        $select.val(defaultValue).change();
                    }

                } else if (field.type == 'radio' && field.domainvalues != null) {
                    var $wrapperRadio = $("<div class='wrapperRadioCheck" + (field.required ? " required " : " ") + "'></div>").appendTo($wrapperInput);
                    for (var d in field.domainvalues) {
                        if (field.domainvalues[d].value != "") {
                            $("<input type='radio' class='collectorpro-input collectorpro-radio' name='editfield_" + field.name + "' value='" + field.domainvalues[d].value + "' data-field='" + field.name + "' type='text' " + (field.readonly ? "readonly='readonly'" : "") + (field.required ? "required " : " ") + "/><span class='alias'> " + field.domainvalues[d].label + "</span>&#x200B;")
                                .appendTo($wrapperRadio)
                                .change(function () {
                                    $(this).closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('change', { field: $(this).attr('data-field'), value: $(this).val() });
                                });
                        }
                    }
                    if (defaultValue != "")
                        $wrapperRadio.find("input[type=radio]").val([defaultValue]).triggerHandler('change');

                } else if (field.type == 'file') {
                    var $fileupload = $("<div class='webgis-fileupload' style='float:left;'></div>").appendTo($wrapperInput);
                    $fileupload.webgis_control_upload({
                        edit_service: options.edit_service,
                        edit_theme: options.edit_themeid,
                        field_name: field.name,
                        onUpload: function (sender, result) {
                            if (result.position) {
                                var map = $(sender).data('map');
                                map.sketch.addVertexCoords(result.position.lng, result.position.lat);
                                map.setScale(1000, [result.position.lng, result.position.lat]);
                            }
                            if (result.dateString && $(sender).attr('data-filedate-field')) {
                                $(sender).closest('.webgis-plugin-collectorpro-container').find("[name='editfield_" + $(sender).attr('data-filedate-field') + "']").val(result.dateString);
                            }

                            $(sender).closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('change', { field: $fileupload.find("input").data("field"), value: $fileupload.find("input").val() });
                            $(sender).find("img").css({ height: "23px", marginLeft: "0px" });
                        }
                    }).data('map', map).attr('data-filedate-field', field.filedatefield);
                    $fileupload.find("input").addClass("collectorpro-input input-fileupload").data("field", field.name).data("type", "file");

                    if (field.required) {
                        $fileupload.children("div .collectorpro-input").addClass("required");
                        $fileupload.children("input.collectorpro-input").attr("required", "required");
                    }
                    $fileupload.children("div .collectorpro-input").css('width', '100%').css("height", "18px");    // Box hat Element-Stil => weg damit, damit in Layout passt
                    $fileupload.children("div .webgis-input").css("width", "auto");

                } else if (field.type == 'checkbox') {
                    var $wrapperRadio = $("<div class='wrapperRadioCheck" + (field.required ? " required " : " ") + "'></div>").appendTo($wrapperInput);
                    var $inputfield = $("<input class='collectorpro-input' type='checkbox' name='editfield_" + field.name + "' data-field='" + field.name + "' value='" + defaultValue + "'" + (field.readonly ? "readonly='readonly'" : "") + (field.required ? "required " : " ") + " /><span class='alias'> " + field.label + "</span>")
                        .appendTo($wrapperRadio)
                        .change(function () {
                            $(this).closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('change', { field: $(this).attr('data-field'), value: $(this).val() });
                        });
                    //if (field.required && defaultValue != "")
                    //$inputfield.change();

                } else if (field.type == 'info') {
                    // gerade erstelltes collectorpro-input-label DIV wieder entfernen
                    $wrapperInput.children(".collectorpro-input-label").remove();
                    $("<div class='infotext'>" + field.label + "</div>").appendTo($wrapperInput);

                } else if (field.type == 'textarea') {
                    var $inputfield = $("<textarea  class='collectorpro-input" + (field.required ? " required " : " ") + "' name='editfield_" + field.name + "' data-field='" + field.name + "' type='text' " + (field.readonly ? "readonly " : " ") + (field.required ? "required " : " ") + " >" + defaultValue + "</textarea>")
                        .appendTo($wrapperInput)
                        .change(function () {
                            $(this).closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('change', { field: $(this).attr('data-field'), value: $(this).val() });
                        });
                    if (defaultValue != "")
                        $inputfield.change();

                } else {
                    var $inputfield = $("<input class='collectorpro-input" + (field.required ? " required " : " ") + "' name='editfield_" + field.name + "' data-field='" + field.name + "' type='" + (field.password ? "password " : "text") + "' " + (field.readonly ? "readonly " : " ") + (field.required ? "required " : " ") + " value='" + defaultValue + "' />")
                        .appendTo($wrapperInput)
                        .change(function () {
                            $(this).closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('change', { field: $(this).attr('data-field'), value: $(this).val() });
                        });

                    if (field.type == 'date' && field.readonly == false) {
                        var enableTime = false;
                        var dateFormat = "d.m.Y";
                        // Falls Uhrzeit dabei (Leerzeichen zwischen Datum und Zeit)
                        if (defaultValue != "" && defaultValue.indexOf(" ") > -1) {
                            enableTime = true;
                            dateFormat = "d.m.Y H:i";
                        }

                        $inputfield.addClass("flatpickr").attr("placeholder", "Datum auswählen...");
                        $inputfield.flatpickr({
                            locale: "de",
                            weekNumbers: true, // show week numbers
                            enableTime: enableTime,
                            dateFormat: dateFormat,
                            time_24hr: true
                        });
                    }
                    // Wenn defaultWert gesetzt wurde (bspw. Zeit) und Feld required => Hervorhebung weg
                    if (defaultValue != "")
                        $inputfield.change();
                }
            }
        }
        $_sketch = $("<input type='hidden' name='_sketch' data-field='_sketch' />")
            .appendTo($formInput);

        if (searchAdressIndex > -1) {
            $adrContent = $categories[searchAdressIndex].find(".collectorpro-presentation_toc-content");
            var $wrapperSearch = $("<div class='wrapper-input'></div>").prependTo($adrContent);
            $("<div class='collectorpro-input-label'>Suche</div>").appendTo($wrapperSearch);
            $searchAdress = $("<input class='collectorpro-input' name='searchAdress' type='text' placeholder='" + categories[searchAdressIndex].quick_search_placeholder + "'/><br/>").appendTo($wrapperSearch);
            $multiGeomWrapper = $("<div class='wrapper-input'></div>").appendTo($adrContent);
            $multiGeomTable = $("<table class='multi-geom-table'>" +
                "<tbody></tbody>" +
                "</table>").appendTo($multiGeomWrapper).hide();
            var $myFeatureRecord;

            $searchAdress.webgis_control_search({
                css_display: 'inline',
                search_service: categories[searchAdressIndex].quick_search_service,
                search_categories: categories[searchAdressIndex].quick_search_category,
                on_select_get_original: true,
                on_select_get_original_raw: true,
                on_select_get_original_fullgeometry: true,
                on_select: function (sender, feature, original) {
                    //console.log(sender);
                    //console.log(feature);
                    //console.log(original);

                    if (typeof (feature.features) != "undefined" && feature.features == 0) {
                        // Zuerst alle Felder leeren
                        for (var m in categories[searchAdressIndex].quick_search_mapping) {
                            var mapping = categories[searchAdressIndex].quick_search_mapping[m];
                            $(".collectorpro-input[name='editfield_" + mapping.target + "']").val("");
                        }
                    }

                    if (typeof (feature.features) != "undefined" && feature.features.length > 0) {
                        var myFeature = feature.features[0];
                        var deleteClass = (options.allow_multipart == true) ? " deleteRow" : "";
                        $myFeatureRecord = $("<tr class='row'>" +
                            "<td class='symbol'><div class='recordsymbol thumbnail'></div></td>" +
                            "<td class='info'></td>" +
                            "<td><div class='recordsymbol" + deleteClass + "'></div></td>" +
                            "</tr>");
                        $myFeatureRecord.data("feature", myFeature);
                        // Wenn kein Multipart-Feature erlaubt ist => Tabelle vorher leeren
                        if (options.allow_multipart != true)
                            $multiGeomTable.find("tbody").empty();
                        $myFeatureRecord.appendTo($multiGeomTable.find("tbody"));
                        $multiGeomTable.show();

                        // Falls gemappt werden soll (bspw. Adresse)
                        // Zuerst alle Felder leeren
                        for (var m in categories[searchAdressIndex].quick_search_mapping) {
                            var mapping = categories[searchAdressIndex].quick_search_mapping[m];
                            $(".collectorpro-input[name='editfield_" + mapping.target + "']").val("");
                        }
                        for (var m in categories[searchAdressIndex].quick_search_mapping) {
                            var mapping = categories[searchAdressIndex].quick_search_mapping[m];
                            if (typeof (myFeature.properties[mapping.source]) != "undefined")
                                $(".collectorpro-input[name='editfield_" + mapping.target + "']").val(myFeature.properties[mapping.source]).change();
                        }

                        if (categories[searchAdressIndex].quick_search_setgeometry == true) {
                            $_sketch.val(myFeature.geometry.coordinates);
                            $_sketch.closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('change', { field: $_sketch.attr('data-field'), value: myFeature });
                            // Am Schluss jeder Suche: Alle Features aus Tabelle auslesen und neu in Karte zeichnen
                            refreshSketch(map);
                        }
                    }

                    if (typeof ($myFeatureRecord) != "undefined" && feature.thumbnail && feature.label) {
                        $myFeatureRecord.find(".thumbnail").css('background-image', 'url(' + feature.thumbnail + ')');
                        $myFeatureRecord.find(".info").text(feature.label);
                    }
                }
            });
        }

        $(".multi-geom-table").on("click", ".deleteRow", function () {
            // Sketch neu zeichnen (mit noch vorhandenen Elementen)
            $(this).closest("tr").remove();
            refreshSketch(map);
        });

        // Map tools
        var tooltype = 'sketch0d';
        switch (editThemeCapabilities.geometrytype) {
            case 'line':
                tooltype = 'sketch1d';
                break;
            case 'polygon':
                tooltype = 'sketch2d';
                break;
        }

        var tool = {
            name: 'pos',
            type: 'clienttool',
            tooltype: tooltype,
            id: 'collector-tool-' + tooltype,
            //collectorElement: $elem,
        };
        map.setActiveTool(tool);
        map.sketch.events.on('onchanged', function (channel, sender) {
            $_sketch.val(sender.toWKT());
            $_sketch.closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('change', { field: $_sketch.attr('data-field'), value: $_sketch.val(), });
        });

        $formInput.find(".collectorpro-presentation_toc-title-text").click(function () {
            $(this).siblings(".collectorpro-presentation_toc-content").slideToggle();
            $(this).parent().toggleClass("webgis-expanded");
            $(this).siblings(".collectorpro-presentation_toc-plus").toggleClass("webgis-api-icon-triangle-1-s");
            $(this).siblings(".collectorpro-presentation_toc-plus").toggleClass("webgis-api-icon-triangle-1-e");
            $(this).siblings(".category-description").toggle();
        });

        // Wenn alles fertig ist:
        if (options.on_init)
            options.on_init({
                map: map,
                webgisContainer: $target.closest('.webgis-plugin-collectorpro-container').find('.webgis-container')
            });
        // Events (zum Scrolling) registrieren, damit die Schnellsuche Vorschläge beim Scrollen nicht verschwinden, sondern "mitwandern"
        webgis.registerUIEvents($target.closest('.webgis-plugin-collectorpro-container'));
    }

    var refreshSketch = function (map) {
        //var cancelTracker = new webgis.cancelTracker();
        webgis.showProgress('Geometrie wird in Karte übertragen...', null/*, cancelTracker*/);

        new webgis.timer(function (map) {
            map.sketch.remove();
            var appendToSketch = false;

            $(".multi-geom-table tbody tr").each(function (i, e) {
                var feature = $(this).data("feature");
                map.sketch.fromJson(feature.geometry, appendToSketch, true);
                appendToSketch = true;      // Ab dem 2. Mal dranhängen
            });
            if (appendToSketch == true)
                map.sketch.appendPart();  // neuen Part zum weiterzeichnen für den User beginnen

            webgis.hideProgress('Geometrie wird in Karte übertragen...');
        }, 200, map).Start();
    }

    var updateSummary = function (editThemeCapabilities, $target) {
        var ieHelp = setCategories(editThemeCapabilities);
        var categories = ieHelp[0], defaultCatIndex = ieHelp[1], helpCategories = ieHelp[2];

        var $categories = [];
        for (var i in categories) {
            $categories.push($("<div class='summary-category'>" + categories[i].name + "</div><div class='summary-category-content'></div>"));
        }

        for (var f in editThemeCapabilities.fields) {
            var field = editThemeCapabilities.fields[f];
            if (field.name == null)
                continue;
            var value = $(".collectorpro-input[name='editfield_" + field.name + "']").val();
            var value_label = value;


            if (field.type == "checkbox" || field.type == "radio") {
                value = $(".collectorpro-input[name='editfield_" + field.name + "']:checked").val();
                value_label = $(".collectorpro-input[name='editfield_" + field.name + "']:checked").next("span.alias").html();
            } else if (field.type == "domain") {
                value = $(".collectorpro-input[name='editfield_" + field.name + "'] option:selected").val();
                value_label = $(".collectorpro-input[name='editfield_" + field.name + "'] option:selected").text();
            } else if (field.type == "file" && value != "") {
                value_label = "erfolgreich hochgeladen";
            }

            var catIndex = helpCategories.indexOf(field.category);
            if (catIndex == -1) {
                catIndex = defaultCatIndex;
            }
            var $catContent = $categories[catIndex];

            $("<div class='summary-label' data-fieldname='" + field.name + "'>" + field.prompt + "</div>").appendTo($catContent[1]);
            $("<div class='summary-value" + (field.required && (value == "" || typeof (value) == 'undefined') ? " missing" : "") + "' data-value='" + ((value == "" || typeof (value) == 'undefined') ? "" : value.replace(/'/g, "&apos;").replace(/"/g, "&quot;")) + "'>" + (value_label == "" || typeof (value) == 'undefined' ? "&nbsp;" : value_label) + "</div>").appendTo($catContent[1]);
        }

        $target.html($categories);
        $target.closest('.webgis-plugin-collectorpro-container').data('eventHandlers').events.fire('summary', { target: $target });


        if (getMap($("#map")).sketch.toWKT() == "")
            $("<div class='summary-category missing'>Verortung fehlt. Bitte verorten Sie Ihre Eingabe über die Karte oder die Suchauswahl</div>").appendTo($target);
    }

    var createMap = function (options, $target) {
        $target.addClass('webgis-container').css({ position: 'relative' });
        var $map = $("<div id='map' style='position:absolute;left:0px;top:0px;right:0px;bottom:0px'></div>").appendTo($target);
        $("<div class='webgis-tool-button-bar shadow' data-tools='" + options.quick_tools + "' style='position:absolute;left:9px;top:109px'></div>").appendTo($target);


        var map = webgis.createMap($map, options.map_options);
        $target.closest('.webgis-plugin-collectorpro-container').data('map', map);
    }

    var getMap = function ($elem) {
        return $elem.closest('.webgis-plugin-collectorpro-container').data('map');
    }

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

    var setCategories = function (editThemeCapabilities) {
        var defaultCatIndex = -1;
        var helpCategories = [];
        var objCategories = editThemeCapabilities.categories;

        if (typeof (objCategories) != "undefined") {
            for (var c in objCategories) {
                if (editThemeCapabilities.categories[c].is_default == true) {
                    defaultCatIndex = c;
                }
                helpCategories.push(editThemeCapabilities.categories[c].id);
            }

            // Nochmal schauen, ob eventuell eine Kategorie "Allgemein" heißt => falls ja, diese ist dann Default       
            for (var c in objCategories) {
                if (editThemeCapabilities.categories[c].id == "allgemein")
                    defaultCatIndex = c;
            }
        } else
            objCategories = [];

        // Wenn keine Kategorien ODER falls keine Standard-Kategorie vorhanden => diese selbst setzen
        if (objCategories.length == 0 || defaultCatIndex == -1) {
            defaultCatIndex = objCategories.push({ name: "Allgemein", id: "allgemein", is_default: true }) - 1;
        }

        return [objCategories, defaultCatIndex, helpCategories];
    }

    /*
    // Metainfos (Service, etc.) holen
    var getMeta = function (id, options, callback) {
        webgis.ajax({
            url: webgis.baseUrl + '/rest/search/' + options.search_service + "?c=item_meta&f=json&id=" + id,
            data: webgis.hmac.appendHMACData({}),
            type: 'get',
            success: function (result) {
                if (typeof callback == "function") {
                    callback(result);
                }
            },
            error: function (result) {
                alert(result);
            }
        });
    }
    */

    var getParameterByName = function (name, url) {
        if (!url) url = window.location.href;
        name = name.replace(/[\[\]]/g, "\\$&");
        var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
            results = regex.exec(url);
        if (!results) return "";
        if (!results[2]) return "";
        return decodeURIComponent(results[2].replace(/\+/g, " "));
    }

    var serializeTable = function ($target) {
        var data = new Object();
        var paramname = [];
        ($target.find(".summary-label")).each(function () {
            paramname.push("editfield_" + $(this).data("fieldname"));
        });

        $target.find(".summary-value").each(function (i, e) {
            var myValue = $(this).data("value");
            data[paramname[i]] = myValue == "&nbsp;" ? "" : myValue;
        });

        data["_sketch"] = getMap($("#map")).sketch.toWKT(true);
        data["_sketchSrs"] = 4326;
        //console.log(data);
        return data;
    }

    if (webgis) {
        window.alert = webgis.alert || window.alert;
    }
})(webgis.$ || jQuery);