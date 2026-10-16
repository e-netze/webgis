// WebGIS UI Builder Label Editor Element
// label-editor (plugin: webgis.ui.plugins.labeleditor.js, server: UILabelEditor)

webgis.ui.builder['label-editor'] = (map, $newElement, element, options) => {
    $newElement.webgis_labelEditor({
        map: map,
        service_id: element.service_id,
        layer_id: element.layer_id,
        field_defs: element.field_defs,
        max_expression_length: element.max_expression_length,
        max_expression_lines: element.max_expression_lines
    });
};
