(function (webgis) {
    "use strict"

    webgis.addPlugin(new function(){

        this.onInit = function () {

        };

        this.onMapCreated = function (map, container) {

        }

    });

    

})(webgis);

(function ($) {
    $.fn.webgis_collector = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_collector');
        }
    };

    var defaults = {
        map_options: null,
        edit_service: '',
        edit_themeids: '',
        quick_tools:'webgis.tools.navigation.currentPos1',
        onsave: null
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
        $elem.addClass('webgis-collector-container webgis-container-styles').css('max-width', '640px');

        if (options.onsave)
            $elem.data('onsave', options.onsave);

        webgis.ajax({
            url: webgis.baseUrl + '/rest/services/' + options.edit_service + '/edit/' + options.edit_themeids + "/capabilities",
            data: webgis.hmac.appendHMACData({}),
            type: 'get',
            success: function (result) {

                if (result.success == false) {
                    if (result.exception)
                        $("<span>" + result.exception + "</span>").appendTo($elem);
                    return;
                }

                var targetId = 'target-' + webgis.guid();
                $elem.attr('data-targetid', targetId);

                $("<iframe id='" + targetId + "' name='" + targetId + "' style='display:none'></iframe>").appendTo($elem);
                var $form = $("<form method='post' enctype='multipart/form-data' target='" + targetId + "' action=''></form>").appendTo($elem);

                $("<div class='webgis-collector-upload' style='display:none'></div>").appendTo($elem);

                var $clsSwitcher = $("<select name='_editfield_themeid' class='webgis-input webgis-collector-class-selector' style='margin-bottom:1px;font-weight:bold'></select").appendTo($form).data('classes', result.classes);
                for (var c in result.classes) {
                    var cls = result.classes[c];

                    $("<option value='" + cls.themeid + "'>" + cls.name + "</option>").appendTo($clsSwitcher);
                }

                var $mapContainer = $("<div class='webgis-container' style='position:relative;float:left;margin-right:10px'></div>").appendTo($form);
                var $map = $("<div style='width:300px;height:300px;margin-bottom:10px'></div>").appendTo($mapContainer);
                $("<div class='webgis-tool-button-bar shadow' data-tools='"+options.quick_tools+"' style='position:absolute;left:9px;top:109px'></div>").appendTo($mapContainer);
                $("<input type='hidden' name='_sketch' />").appendTo($form);
                
                $("<div class='webgis-collector-fields-container' style='width:300px;display:inline-block;text-align:left'></div>").appendTo($form);

                var map = webgis.createMap($map, options.map_options);

                $("<input type='hidden' name='_sketchSrs' value='" + map.crsId() + "' />").appendTo($form);

                $clsSwitcher.change(function () {
                    var themeId = $(this).val();
                    var classes = $(this).data('classes');
                    var map = $(this).data('map');
                    var $uploadDiv = $(this).closest('.webgis-collector-container').find('.webgis-collector-upload').empty();

                    var cls = null;
                    for (var c in result.classes) {
                        if (result.classes[c].themeid == themeId) {
                            cls = result.classes[c];
                            break;
                        }
                    }

                    var $fieldsContainer = $(this).closest('form').find('.webgis-collector-fields-container').empty();
                    if (cls == null)
                        return;

                    $("<br/><br/>").appendTo($fieldsContainer);
                    for (var f in cls.fields) {
                        var field = cls.fields[f];

                        if (field.visible == false) {
                            $("<input type='hidden' name='editfield_" + field.name + "' />").appendTo($fieldsContainer); // Hat wenig Sinn, wenn man nicht auch gleiche ein Value mitschickt!!??
                        } else if (field.type == 'domain' && field.domainvalues != null) {

                            $("<span class='webgis-input-label'>" + field.prompt + "</span>").appendTo($fieldsContainer);
                            $("<br/>").appendTo($fieldsContainer);

                            var $select = $("<select class='webgis-input' name='editfield_" + field.name + "' type='text' " + (field.readonly ? "readonly='readonly'" : "") + " ></select>").appendTo($fieldsContainer);
                            for (var d in field.domainvalues) {
                                $("<option value='" + field.domainvalues[d].value + "'>" + field.domainvalues[d].label + "</option>").appendTo($select);
                            }
                            $("<br/><br/>").appendTo($fieldsContainer);

                        } else if (field.type == 'file') {

                            $("<span class='webgis-input-label'>" + field.prompt + "</span>").appendTo($fieldsContainer);
                            $("<br/>").appendTo($fieldsContainer);
                            $("<div></div>").appendTo($fieldsContainer).webgis_control_upload({
                                edit_service: options.edit_service,
                                edit_theme: themeId,
                                field_name: field.name,
                                onUpload: function (sender, result) {
                                    if (result.position) {
                                        var map = $(sender).data('map');
                                        map.sketch.addVertexCoords(result.position.lng, result.position.lat);
                                        map.setScale(1000, [result.position.lng, result.position.lat]);
                                    }
                                    if (result.dateString && $(sender).attr('data-filedate-field')) {
                                        $(sender).closest('.webgis-collector-fields-container').find("[name='editfield_" + $(sender).attr('data-filedate-field') + "']").val(result.dateString);
                                    }
                                }
                            }).data('map', map).attr('data-filedate-field', field.filedatefield);

                        } else {

                            $("<span class='webgis-input-label'>" + field.prompt + "</span>").appendTo($fieldsContainer);
                            $("<br/>").appendTo($fieldsContainer);

                            $("<input class='webgis-input' name='editfield_" + field.name + "' type='text' " + (field.readonly ? "readonly='readonly'" : "") + " style='width:288px' />").appendTo($fieldsContainer)
                            $("<br/><br/>").appendTo($fieldsContainer);

                        }
                    }

                    $("<div class='webgis-button' style='width:285px;display:inline-block' id='"+targetId+"-save'>Speichern</div>").appendTo($fieldsContainer)
                    .click(function () {
                        var $form = $(this).closest("form");
                        $form.attr('action', webgis.baseUrl + '/rest/services/' + options.edit_service + '/edit/' + cls.themeid + "/insert?" + webgis.hmac.urlParameters({ responseformat: 'framed', callbackchannel: $(this).attr('id') }));
                        $form.submit();
                        $form.attr('action', '');

                        map.sketch.remove();
                    });

                    var tooltype = 'sketch0d';
                    switch (cls.geometrytype) {
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

                        collectorElement: $elem,

                        //onmapclick: function (sender, e) {
                        //    alert(map.sketch);
                        //    $(sender.collectorElement).find("input[name='_geometry']").val(JSON.stringify(e));
                        //}
                    };

                    map.sketch.remove();
                    $(this).closest('form').find("input[name='_sketch']").val('');
                    map.setActiveTool(tool);

                }).data('map', map);

                map.sketch.events.on('onchanged', function (channel, sender) {
                    $elem.find("input[name='_sketch']").val(sender.toWKT());
                });

                webgis.events.on(targetId + '-save', function (chanel, json) {
                    var map = $(this).find('.webgis-collector-class-selector').data('map');
                    if (json.success) {
                        if ($(this).data('onsave')) {
                            $(this).data('onsave')({
                                extent: map.getExtent()
                            });
                        } else {
                            map.refreshServices(json.refreshservices);
                            $(this).find('.webgis-collector-class-selector').trigger('change');
                        }
                    } else {
                        alert('Fehler: ' + json.message || json.exception);
                    }
                }, $elem.get(0));

                $clsSwitcher.trigger('change');

                webgis.currentPosition.get({
                    maxWatch: 1,
                    onSuccess: function (pos) {

                        var lng = pos.coords.longitude, lat = pos.coords.latitude, acc = pos.coords.accuracy / (webgis.calc.R) * 180.0 / Math.PI;

                        map.zoomTo([lng - acc, lat - acc, lng + acc, lat + acc]);
                        //map.showDraggableClickMarker(lat, lng);
                    },
                    onError: function () {
                        alert('Ortung fehlgeschlagen!');
                    }
                });
            }
        });

    };

    $.extend({
        eval: function (s) {
            if (typeof (s) == 'string') {
                if (window.JSON)
                    s = JSON.parse(s);
                else
                    s = eval("(" + s + ")");
            }
            if (!s) return {};
            return s;
        }
    })

})(webgis.$ || jQuery);

