// WebGIS UI Builder Time Filter Elements
// timefiltercontrol, timefilterlist (plugins: webgis.ui.plugins.timefilter.js)

webgis.ui.builder['timefiltercontrol'] = (map, $newElement, element) => {
    $newElement.webgis_timeFilterControl({ map: map });
};

webgis.ui.builder['timefilterlist'] = (map, $newElement, element) => {
    $newElement.webgis_timeFilterList({ map: map });
};

webgis.ui.showRemoveTimeFiltersDialog = function (map) {
    webgis.$("body").webgis_modal({
        title: webgis.l10n.get("remove-filters"),
        width: "600px",
        onload: function ($content) {
            $("<div>").webgis_timeFilterList({
                map: map,
                onRemoveAllFilters: function () { webgis.$("body").webgis_modal("close"); }
            })
            .appendTo($content);
        }
    });
};
