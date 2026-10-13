// WebGIS UI Builder Input Elements
// input-text, input-textarea, input-date, input-number, hidden, input-autocomplete, input-bbox, input-size
// (jQuery plugins: webgis_bbox_input.js, webgis_size_input.js)
//
// Inputs historically get no data-tool-id attribute (".toolid" = false), even if the UI is built for a tool
// (except input-bbox and input-size).

webgis.ui.builder["input-text.tagname"] = "input";
webgis.ui.builder["input-text.attributes"] = "type='text'";
webgis.ui.builder["input-text.toolid"] = false;
webgis.ui.builder["input-text.formelement"] = true;
webgis.ui.builder["input-text"] = (map, $newElement, element) => {
    if (element.label)
        $newElement.val(element.label);
    if ($newElement.hasClass('webgis-print-textelement') && !$newElement.val()) {
        $newElement.val(map.getPersistentToolParameter('webgis.tools.print', "LAYOUT_TEXT_" + $newElement.attr('id')));
    }
};

webgis.ui.builder["input-textarea.tagname"] = "textarea";
webgis.ui.builder["input-textarea.attributes"] = "rows='5'";
webgis.ui.builder["input-textarea.toolid"] = false;
webgis.ui.builder["input-textarea.formelement"] = true;
webgis.ui.builder["input-textarea"] = (map, $newElement, element) => {
    if (element.value)
        $newElement.text(element.value);
};

webgis.ui.builder["input-date.tagname"] = "input";
webgis.ui.builder["input-date.attributes"] = "type='text'";
webgis.ui.builder["input-date.toolid"] = false;
webgis.ui.builder["input-date.formelement"] = true;
webgis.ui.builder["input-date"] = (map, $newElement, element) => {
    if (element.label)
        $newElement.val(element.label);

    webgis.require('flatpickr', function ($newElement) {
        $newElement.flatpickr({
            weekNumbers: true,
            locale: 'de',
            dateFormat: element.date_only === true ? "d.m.Y" : "d.m.Y H:i",
            enableTime: element.date_only !== true,
            time_24hr: true
        });
    }, $newElement);
};

webgis.ui.builder["input-number.tagname"] = "input";
webgis.ui.builder["input-number.attributes"] = (element) =>
    "type='number' min='" + element.minValue + "' max='" + element.maxValue + "' step='" + element.stepWidth + "'";
webgis.ui.builder["input-number.toolid"] = false;
webgis.ui.builder["input-number.formelement"] = true;
webgis.ui.builder["input-number"] = (map, $newElement, element) => {
    if (element.value)
        $newElement.val(element.value);
};

webgis.ui.builder["hidden.tagname"] = "input";
webgis.ui.builder["hidden.attributes"] = "type='hidden'";
webgis.ui.builder["hidden.toolid"] = false;
webgis.ui.builder["hidden"] = (map, $newElement, element) => {
    $newElement.val(element.value);
    if ($newElement.hasClass("webgis-map-anonymous-user-id")) {
        var currentId = webgis.localStorage.getAnonymousUserId();
        if (!currentId) {
            webgis.localStorage.setAnonyousUserId($newElement.val());
        } else {
            $newElement.val(currentId);
        }
    }
};

webgis.ui.builder["input-autocomplete.tagname"] = "input";
webgis.ui.builder["input-autocomplete.attributes"] = "type='text'";
webgis.ui.builder["input-autocomplete.toolid"] = false;
webgis.ui.builder["input-autocomplete"] = (map, $newElement, element) => {
    $newElement.addClass('webgis-autocomplete').attr('data-source', element.source);
    if (element.label)
        $newElement.val(element.label);
};

// bounding box (4 coordinates) and size (width/height) inputs, used by print and 3D tools
webgis.ui.builder["input-bbox"] = (map, $newElement, element) => {
    $newElement.webgis_bbox_input({ readonly: element.readonly });
};

webgis.ui.builder["input-size"] = (map, $newElement, element) => {
    $newElement.webgis_size_input({ readonly: element.readonly });
};
