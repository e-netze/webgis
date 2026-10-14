// WebGIS UI Builder Time Filter Elements
// timefiltercontrol, timefilterlist (plugins: webgis.ui.plugins.timefilter.js)

webgis.ui.builder['timefiltercontrol'] = (map, $newElement, element) => {
    $newElement.webgis_timeFilterControl({ map: map });
};

webgis.ui.builder['timefilterlist'] = (map, $newElement, element) => {
    $newElement.webgis_timeFilterList({ map: map });
};

webgis.ui.showRemoveTimeFiltersDialog = function (map) {
    webgis.ui.showModal({
        title: webgis.l10n.get("remove-filters"),
        width: "600px",
        onload: function ($content) {
            $("<div>").webgis_timeFilterList({
                map: map,
                onRemoveAllFilters: function () { webgis.ui.closeModal(); }
            })
            .appendTo($content);
        }
    });
};
