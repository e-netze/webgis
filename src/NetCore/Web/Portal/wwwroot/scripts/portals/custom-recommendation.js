webgis.markerIcons["query_result"]["default"] = {
    url: function (i, f) {
        return webgis.baseUrl + '/rest/numbermarker/' + (i + 1);
    },
    size: function (i, f) { return [33, 41]; },
    anchor: function (i, f) { return [16, 42]; },
    popupAnchor: function (i, f) { return [0, -42]; },
    className: function (i, f) {
        return null; // "webgis-sprite-marker";
    }
};
webgis.markerIcons["dynamic_content"]["default"] = webgis.markerIcons["query_result"]["default"];
webgis.markerIcons["dynamic_content_extenddependent"]["default"] = {
    url: function (i, f) {
        return webgis.baseUrl + '/rest/numbermarker';
    },
    size: function (i, f) { return [33, 41]; },
    anchor: function (i, f) { return [16, 42]; },
    popupAnchor: function (i, f) { return [0, -42]; }
};

webgis.markerIcons["query_result"]["webgis.tools.rasteridentify"] = {
    url: function (i, f) { return webgis.css.imgResource('height_marker.png', 'markers'); },
    size: function (i, f) { return [25, 41]; },
    anchor: function (i, f) { return [12, 42]; },
    popupAnchor: function (i, f) { return [0, -42]; }
};
webgis.markerIcons["query_result"]["webgis.tools.chainage"] = {
    url: function (i, f) {
        return webgis.baseUrl + '/rest/numbermarker/' + i+ '?style=chainage';
        //return webgis.css.imgResource('xy_marker.png', 'markers');
    },
    size: function (i, f) { return [26, 37]; },
    anchor: function (i, f) { return [13, 24]; },
    popupAnchor: function (i, f) { return [0, -42]; }
};
webgis.markerIcons["query_result"]["webgis.tools.coordinates"] = {
    url: function (i, f) {
        return webgis.baseUrl + '/rest/numbermarker/' + i + '?style=coords';
        //return webgis.css.imgResource('xy_marker.png', 'markers');
    },
    size: function (i, f) { return [26, 37]; },
    anchor: function (i, f) { return [13, 24]; },
    popupAnchor: function (i, f) { return [0, -42]; }
};

webgis.usability.clickBubble = webgis.usability.contextMenuBubble = webgis.isTouchDevice();
webgis.usability.enableHistoryManagement = webgis.isMobileDevice();
webgis.usability.sketchMarkerPopup = false;
webgis.usability.useMarkerPopup = false;
webgis.usability.showSingleResultPopup = true;
webgis.usability.presentationTocSearch = !webgis.isMobileDevice();
webgis.usability.useGraphicsMarkerPopups = false;  // Info Container für Grafiken (MapMarkup) anzeigen
webgis.usability.toolSketchOnlyEditableIfToolTabIsActive = true;

webgis.usability.allowDarkmode = true;
webgis.usability.allowStyleClassSelection = true;
webgis.usability.allowLanguageSelection = true;

webgis.usability.allowAddCustomServices = false;
webgis.usability.allowMapContextMenu = true;
webgis.usability.makePresentationTocGroupCheckboxes = true;

webgis.usability.zoom.useFreeZooming();

webgis.usability.useCompassTool = webgis.isMobileDevice();

webgis.usability.allowSketchShortcuts = true;
webgis.usability.allowSelectSketchVertices = true;
webgis.usability.highlightFeatureOnMarkerClick = true;

webgis.usability.allowViewerLayoutTemplateSelection = true;
webgis.usability.quickSearch.displayMetadata.geocodes = true;

webgis.usability.useAdvancedKeyShortcutHandling = true;

if (webgis.isMobileDevice() === false) {
    webgis.usability.optionContainerDefault.push({
        id: 'webgis-identify-tool',
        value: 'rectangle'
    });

    if (webgis.useMobileCurrent() == false) {
        webgis.usability.appendMiniMap = true;
    }
}
