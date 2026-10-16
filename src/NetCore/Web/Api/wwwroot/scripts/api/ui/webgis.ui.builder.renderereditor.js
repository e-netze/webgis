// WebGIS UI Builder Renderer Editor Element
// renderer-editor (plugin: webgis.ui.plugins.renderereditor.js, server: UIRendererEditor)

webgis.ui.builder['renderer-editor'] = (map, $newElement, element, options) => {
    $newElement.webgis_rendererEditor({
        map: map,
        service_id: element.service_id,
        layer_id: element.layer_id,
        geometry_type: element.geometry_type,
        field_defs: element.field_defs,
        max_classes: element.max_classes,
        callback_tool_id: element.callback_tool_id
    });
};
