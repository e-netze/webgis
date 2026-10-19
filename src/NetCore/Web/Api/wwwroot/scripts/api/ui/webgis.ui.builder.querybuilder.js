// WebGIS UI Builder Query Builder Element
// query-builder (plugin: webgis.ui.plugins.querybuilder.js)

webgis.ui.builder['query-builder'] = (map, $newElement, element, options) => {
    $newElement.webgis_queryBuilder({
        map: map,
        id: element.id,
        field_defs: element.field_defs,
        show_geometry_option: element.show_geometry_option,
        callback_tool_id: element.callback_tool_id,
        callback_argument: element.callback_argument,
        whereclause_parts: element.whereclause_parts,
        values_source: element.values_source,
        event: options.event,
        // opened without tool dialog (eg. from the toc context menu) => apply without tool dialog, too
        suppress_tool_dialog: !!(options.tool && options.tool.suppressToolDialog === true)
    });
};
