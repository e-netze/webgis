// WebGIS UI Builder Visibility Filter Elements
// visfiltercombo, visfilterlist (plugins: webgis.ui.plugins.visfilter.js)

webgis.ui.builder["visfiltercombo.tagname"] = "select";
webgis.ui.builder["visfiltercombo"] = (map, $newElement, element) => {
    $newElement.addClass("webgis-select").webgis_visfilterCombo({ map: map, val: element.value });
};

webgis.ui.builder["visfilterlist"] = (map, $newElement, element) => {
    $newElement.webgis_visfilterList({ map: map });
};

webgis.ui.showRemoveVisFiltersDialog = function (map) {
    webgis.ui.showModal({
        title: webgis.l10n.get("remove-filters"),
        width: "600px",
        onload: function ($content) {

            //$("<button>")
            //    .addClass("webgis-button uibutton uibutton-danger")
            //    .text(webgis.l10n.get("remove-all-filters"))
            //    .appendTo(webgis.ui.createButtonGroup($content))
            //    .on("click", function () {
            //        map.unsetAllFilters();
            //        map.refresh();
            //        map.ui.refreshUIElements();
            //        webgis.ui.closeModal();
            //    });

            $("<div>").webgis_visfilterList({
                    map: map,
                    onRemoveAllFilters: function () { webgis.ui.closeModal(); }
                })
                .appendTo($content);
        }
    });
};
