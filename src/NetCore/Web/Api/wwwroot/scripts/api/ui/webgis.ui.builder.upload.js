// WebGIS UI Builder Upload Elements
// upload-file, upload-file-control
//
// The upload control plugin (webgis_control_upload) lives in its own file (webgis.ui.plugins.control_upload.js),
// because it is also used outside of the UI builder (collector plugins).

(function ($) {
    "use strict";

    // button + hidden file input; the selected file is sent to the server command of the tool
    webgis.ui.builder["upload-file.tagname"] = "button";
    webgis.ui.builder["upload-file.properties"] = false;
    webgis.ui.builder["upload-file"] = (map, $newElement, element, options) => {
        $newElement.html(element.text ? element.text : webgis.l10n.get("file-upload")).addClass('webgis-button')
            .attr('id', element.id)
            .click(function (event) {
                event.stopPropagation();
                $('#' + $(this).attr('id') + "_file_input").trigger('click');
            });

        const $form = $("<div></div>").css('display', 'none').appendTo($newElement.parent());
        const $input = $("<input type='file' name='" + element.id + "' id='" + element.id + "_file_input' />").appendTo($form)
            .data('servercommand', element.servercommand)
            .data('toolid', element.toolid)
            .data('map', map);
        $input.change(function (e) {
            const map = $(this).data('map');
            for (var i in this.files) {
                const file = this.files[i];
                if (!file || !file.size)
                    continue;

                webgis.tools.uploadFile(map, $(this).data('toolid'), $(this).data('servercommand'), $(this).attr('name'), file);
            }
        });
    };

    webgis.ui.builder["upload-file-control.properties"] = false;
    webgis.ui.builder["upload-file-control"] = (map, $newElement, element, options) => {
        $newElement.webgis_control_upload({
            edit_service: element.edit_service,
            edit_theme: element.edit_theme,
            field_name: element.field_name,
            hidden_class: element.css,
            onload: function (sender, result) {
                webgis.log(result);
            }
        });
    };
})(webgis.$ || jQuery);