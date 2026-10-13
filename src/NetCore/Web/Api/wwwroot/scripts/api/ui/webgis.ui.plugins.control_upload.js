// WebGIS UI Plugins (control_upload)
// webgis_control_upload: file upload field for edit forms (hidden iframe form, preview, progress)
// Element builder: webgis.ui.builder.upload.js ('upload')

webgis.ui.definePlugin('webgis_control_upload', {
    defaults: {
        edit_service: 'service',
        edit_theme: '',
        field_name: 'File',
        hidden_class: '',
        onUpload: null
    },
    init: function () {
        const $ = this.$, o = this.options;

        const targetId = 'target-' + webgis.guid();
        let $uploadDivContainer = $('#__webgis-upload-div-container');
        if ($uploadDivContainer.length === 0)
            $uploadDivContainer = $("<div id='__webgis-upload-div-container' style='display:none'></div>").appendTo('body');
        $uploadDivContainer.find('#' + o.field_name + "-" + targetId + '-upload-div').remove();
        const $uploadDiv = this._$uploadDiv = $('<div></div>').attr('id', o.field_name + "-" + targetId + '-upload-div').appendTo($uploadDivContainer);
        const $elem = this.$el.addClass('webgis-control-upload');
        $("<iframe id='" + o.field_name + "-" + targetId + "-frame' name='" + o.field_name + "-" + targetId + "-frame' style='display:none'></iframe>").appendTo($uploadDiv);
        const $uploadForm = $("<form method='post' enctype='multipart/form-data' target='" + o.field_name + "-" + targetId + "-frame' action=''></form>").appendTo($uploadDiv);
        const callbackEventChannel = this._callbackEventChannel = o.field_name + "-" + targetId + "-" + webgis.guid();
        $("<input id='" + callbackEventChannel + "' name='file' type='file' />").appendTo($uploadForm)
            .on('change.webgis_control_upload', function () {
                const action = webgis.baseUrl + '/rest/services/' + $(this).attr('data-service') + '/edit/' + $(this).attr('data-theme') + "/upload?" + webgis.hmac.urlParameters({ responseformat_: 'framed', callbackchannel: $(this).attr('id'), field: $(this).attr('data-name') });
                const channel = $(this).attr('id');
                const $container = $(this).closest('.webgis-collector-container');
                const tid = $container.attr('data-targetid');
                const $preview = $('#' + $(this).attr('data-name') + "-" + tid + "-preview");
                $preview.css('height', '22px').find('img').remove();
                for (let f in this.files) {
                    const file = this.files[f];
                    if (!file || !file.size)
                        continue;
                    const formData = new FormData();
                    const client = new XMLHttpRequest();
                    const $progress = $("<progress id='progress' style='width:100%;height:20px;z-index:100' value='0' max='100'></progress>");
                    $progress.appendTo($preview);
                    const progress = $progress.get(0);
                    formData.append(this.name, file);
                    client.onerror = function (e) {
                        webgis.alert("Error", 'error');
                    };
                    client.onload = function (e) {
                        progress.value = progress.max;
                        $progress.remove();
                        const response = $.eval(client.responseText);
                        webgis.events.fire(channel, response);
                    };
                    client.upload.onprogress = function (e) {
                        const p = Math.round(100 / e.total * e.loaded);
                        progress.value = p;
                    };
                    client.onabort = function (e) {
                        webgis.alert('Upload abgebrochen', 'info');
                    };
                    client.open("POST", action);
                    client.send(formData);
                }
                return;
            }).attr('data-name', o.field_name).attr('data-service', o.edit_service).attr('data-theme', o.edit_theme);

        const $hidden = $("<input type='hidden' id='editfield_" + o.field_name + "' name='editfield_" + o.field_name + "' />").appendTo($elem);
        if (o.hidden_class)
            $hidden.addClass(o.hidden_class);

        const preview = $("<div id='" + o.field_name + "-" + targetId + "-preview' class='webgis-input' style='cursor:pointer;width:288px;position:relative;height:22px;display:block'><div style='position:absolute;right:2px;bottom:2px;width:26px;height:26px;background:url(" + webgis.baseUrl + '/content/api/img/upload-26.png' + ") no-repeat center right;' /></div></div>").appendTo($elem)
            .on('click.webgis_control_upload', function () {
                $("#" + $(this).attr('data-fileinput-id')).trigger('click');
            }).attr('data-name', o.field_name).attr('data-fileinput-id', callbackEventChannel).data('onUpload', o.onUpload).get(0);

        this._onUploaded = function (chanel, json) {
            $(this).parent().find("input[name='editfield_" + $(this).attr('data-name') + "']").val(json.url);
            $(this).css('height', 'auto').find('img').remove();
            $("<img src='" + webgis.baseUrl + json.url + "&size=256" + "' style='margin:-4px' />").appendTo($(this));
            if ($(this).data('onUpload'))
                $(this).data('onUpload')($(this).closest('.webgis-control-upload'), json);
        };
        webgis.events.on(callbackEventChannel, this._onUploaded, preview);
    },
    destroy: function () {
        webgis.events.off(this._callbackEventChannel, this._onUploaded);
        this.$el.find('*').off('.webgis_control_upload');
        if (this._$uploadDiv) {
            this._$uploadDiv.find('*').off('.webgis_control_upload');
            this._$uploadDiv.remove();
        }
    }
});

// $.eval(s): parses a JSON string (used for the framed upload response)
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
