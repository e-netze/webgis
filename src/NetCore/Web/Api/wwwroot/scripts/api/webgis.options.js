webgis.markerClusterOptions = {
    maxClusterRadius: 20
};

webgis.queryResultOptions = {
    showMarker: true,
    showMenu: true,
    showHistory: true,
    showHeadingCount: true
};

webgis.advancedOptions = {
    quicksearch_custom_parameters: "",
    get_serviceinfo_purpose: ""
}

webgis.usability = {
    clickBubble: false,
    contextMenuBubble: false,
    constructionTools: true,
    sketchContextMenu: true,
    sketchMarkerPopup: webgis.isTouchDevice(),
    enableHistoryManagement: webgis.isMobileDevice(),
    useMarkerPopup: true,
    useGraphicsMarkerPopups: true,   // Popups or MarkerInfoPanel
    showMarkerInfoPanel: true,
    showSingleResultPopup: false,
    useCatCombos: true,
    singleResultButtons: [],
    dockPanelPadding: {
        top: 0,
        left: 0,
        right: 0,
        bottom: 0
    },
    optionContainerDefault: [],
    presentationTocSearch: false,
    allowDarkmode: false,
    allowStyleClassSelection: false,
    allowLanguageSelection: false,
    mapClickTolerance: 3,
    mapSketchClickTolerance: 80,
    toolSketchOnlyEditableIfToolTabIsActive: false,
    sketch: {
        checkForOverlappingPolygonSegments: false,
        checkForOverlappingPolylineSegments: true
    },
    cooperativeGestureHandling: false,
    socialShare: {
        allowWhatsApp: false,
        allowFacebook: false,
        allowFacebookMessenger: false,
        allowTwitter: false
    },
    appendMiniMap: false,
    miniMapOptions: {
        zoomLevelOffset: -5,
        position: 'bottomleft',
        toggleDisplay: true,
        minimized: true
    },
    appendMapTools: [], //[ "webgis.tools.serialization.sharemap", "webgis.tools.presentation.labeling" ],
    useCompassTool: false,
    appendContentSearchToSearchResults: false,
    showErrorsInTabs: true,
    allowUserRemoveErrors: true,
    showTips: false,
    zoom: {
        zoomSnap: 1,
        zoomDelta: 1,
        useFreeZooming: function () { webgis.usability.zoom.zoomSnap = 0; },
        useFreeZoomingWithDelta: function (delta) { webgis.usability.zoom.zoomSnap = 0; webgis.usability.zoom.zoomDelta = delta },
        useDefaultZooming: function () { webgis.usability.zoom.zoomSnap = 1; webgis.usability.zoom.zoomDelta = 1; },
        allowsFreeZooming: function () { return webgis.usability.zoom.zoomSnap <= 0.01 },
        useIterativeZoomingToFixScale: false,
        setMaxBounds: false,
        minFeatureZoom: 1000
    },
    allowAddCustomServices: false,
    allowMapContextMenu: false,

    makePresentationTocGroupCheckboxes: false,
    orderPresentationTocContainsByServiceOrder: false,

    allowSelectSketchVertices: false,
    allowSketchShortcuts: false,

    measureZAngleDefaultUnit: 'percent',  // deg, gon, percent

    highlightFeatureOnMarkerClick: false,

    tableToolsContainerExtended: true,

    tsp: {
        allowOrderFeatures: false,
        maxFeatures: 50
    },

    toolProperties: [],

    quickSearch: {
        displayMetadata: {
            default: true,
            geocodes: false
        }
    }
};

webgis.colorScheme = 'default';

webgis.defaults = {
    "__dummy": 0, // dummy => otherwise minimizer removes the whole object, if there are no properties
    /*
    'map.properties.language': 'en',
    'map.properties.template.1200': 'desktop',
    'map.properties.cssClass': '_space-saving',
    'map.properties.colorScheme': '_bg-light'
    */
}

webgis.leaflet = {
    tilePane: null,
    onePanePerService: false
};