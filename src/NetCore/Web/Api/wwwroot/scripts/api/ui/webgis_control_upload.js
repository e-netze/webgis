(function ($) {
    "use strict";
    $.fn.webgis_control_upload = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_control_search');
        }
    };
    var defaults = {
        edit_service: 'service',
        edit_theme: '',
        field_name: 'File',
        hidden_class: '',
        onUpload: null
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
    };
    var initUI = function (elem, options) {
        var targetId = 'target-' + webgis.guid();
        var $uploadDivContainer = $('#__webgis-upload-div-container');
        if ($uploadDivContainer.length === 0)
            $uploadDivContainer = $("<div id='__webgis-upload-div-container' style='display:none'></div>").appendTo('body');
        $uploadDivContainer.find('#' + options.field_name + "-" + targetId + '-upload-div').remove();
        var $uploadDiv = $('<div></div>').attr('id', options.field_name + "-" + targetId + '-upload-div').appendTo($uploadDivContainer);
        var $elem = $(elem).addClass('webgis-control-upload');
        $("<iframe id='" + options.field_name + "-" + targetId + "-frame' name='" + options.field_name + "-" + targetId + "-frame' style='display:none'></iframe>").appendTo($uploadDiv);
        var $uploadForm = $("<form method='post' enctype='multipart/form-data' target='" + options.field_name + "-" + targetId + "-frame' action=''></form>").appendTo($uploadDiv);
        var callbackEventChannel = options.field_name + "-" + targetId + "-" + webgis.guid();
        $("<input id='" + callbackEventChannel + "' name='file' type='file' />").appendTo($uploadForm)
            .change(function () {
            var action = webgis.baseUrl + '/rest/services/' + $(this).attr('data-service') + '/edit/' + $(this).attr('data-theme') + "/upload?" + webgis.hmac.urlParameters({ responseformat_: 'framed', callbackchannel: $(this).attr('id'), field: $(this).attr('data-name') });
            var channel = $(this).attr('id');
            var $container = $(this).closest('.webgis-collector-container');
            var tid = $container.attr('data-targetid');
            var $preview = $('#' + $(this).attr('data-name') + "-" + tid + "-preview");
            $preview.css('height', '22px').find('img').remove();
            //$("<img src='" + webgis.baseUrl + "/content/api/img/hourglass/loader1.gif' style='height:30px;margin:-4px' />").appendTo($preview);
            for (var f in this.files) {
                var file = this.files[f];
                if (!file || !file.size)
                    continue;
                var formData = new FormData();
                var client = new XMLHttpRequest();
                var $progress = $("<progress id='progress' style='width:100%;height:20px;z-index:100' value='0' max='100'></progress>");
                $progress.appendTo($preview);
                var progress = $progress.get(0);
                formData.append(this.name, file);
                client.onerror = function (e) {
                    webgis.alert("Error",'error');
                };
                client.onload = function (e) {
                    progress.value = progress.max;
                    $progress.remove();
                    var response = $.eval(client.responseText);
                    webgis.events.fire(channel, response);
                };
                client.upload.onprogress = function (e) {
                    var p = Math.round(100 / e.total * e.loaded);
                    progress.value = p;
                };
                client.onabort = function (e) {
                    webgis.alert('Upload abgebrochen','info');
                };
                client.open("POST", action);
                client.send(formData);
            }
            return;
        }).attr('data-name', options.field_name).attr('data-service', options.edit_service).attr('data-theme', options.edit_theme);

        var $hidden = $("<input type='hidden' id='editfield_" + options.field_name + "' name='editfield_" + options.field_name + "' />").appendTo($elem);
        if (options.hidden_class)
            $hidden.addClass(options.hidden_class);

        var preview = $("<div id='" + options.field_name + "-" + targetId + "-preview' class='webgis-input' style='cursor:pointer;width:288px;position:relative;height:22px;display:block'><div style='position:absolute;right:2px;bottom:2px;width:26px;height:26px;background:url(" + webgis.baseUrl + '/content/api/img/upload-26.png' + ") no-repeat center right;' /></div></div>").appendTo($elem)
            .click(function () {
            $("#" + $(this).attr('data-fileinput-id')).trigger('click');
        }).attr('data-name', options.field_name).attr('data-fileinput-id', callbackEventChannel).data('onUpload', options.onUpload).get(0);
        webgis.events.on(callbackEventChannel, function (chanel, json) {
            $(this).parent().find("input[name='editfield_" + $(this).attr('data-name') + "']").val(json.url);
            $(this).css('height', 'auto').find('img').remove();
            $("<img src='" + webgis.baseUrl + json.url + "&size=256" + "' style='margin:-4px' />").appendTo($(this));
            if ($(this).data('onUpload'))
                $(this).data('onUpload')($(this).closest('.webgis-control-upload'), json);
            //if (json.position) {
            //    var map = $(this).data('map');
            //    map.sketch.addVertexCoords(json.position.lng, json.position.lat);
            //    map.setScale(1000, [json.position.lng, json.position.lat]);
            //}
            //if (json.dateString && $(this).attr('data-filedate-field')) {
            //    $(this).closest('.webgis-collector-fields-container').find("[name='editfield_" + $(this).attr('data-filedate-field') + "']").val(json.dateString);
            //}
        }, preview);
    };
})(webgis.$ || jQuery);

webgis.$.extend({
    eval: function (s) {
        if (typeof (s) === 'string') {
            if (window.JSON)
                s = JSON.parse(s);
            else
                s = eval("(" + s + ")");
        }
        if (!s)
            return {};
        return s;
    }
});
