webgis.usability.clickBubble = webgis.usability.contextMenuBubble = webgis.isTouchDevice();
webgis.usability.enableHistoryManagement = true; // Back Button am Handy
webgis.usability.sketchMarkerPopup = false;
webgis.usability.useMarkerPopup = false;
//webgis.usability.useCompassTool = true;
webgis.usability.appendContentSearchToSearchResults = true;
//webgis.usability.cooperativeGestureHandling = true;
webgis.usability.useMyPresentations = true;
//webgis.usability.appendMapTools = ["webgis.tools.serialization.sharemap"];

webgis.usability.allowDarkmode = true;
webgis.usability.allowStyleClassSelection = true;
webgis.usability.showTips = true;

webgis.usability.allowAddCustomServices = true;
webgis.usability.makePresentationTocGroupCheckboxes = true;

//webgis.usability.zoom.useDefaultZooming();

calcCrs = 31256; // webgis.calc.crs_BestAustria_GK;

webgis.usability.tsp.allowOrderFeatures = true;
webgis.usability.tsp.maxFeatures = 50;

webgis.usability.singleResultButtons = webgis.usability.singleResultButtons || [];
webgis.usability.singleResultButtons.push(
    {
        name: 'Google Navigation',
        url: 'https://maps.google.com/maps?daddr={lat},{lon}&ll=',
        img: webgis.css.imgResource('google-maps-26.png', 'tools')
    });

webgis.usability.allowSelectSketchVertices = true;

webgis.hooks["query_result_feature"]["enetze_fotos"] = function (map, $parent, feature, base) {
    $(feature.properties.Vorschau).appendTo($parent);
};


if (/*mapUrlName === "Basiskarte und Kataster"*/ true) {
    webgis.currentPosition = webgis.currentPosition_watch;

    webgis.currentPosition.minAcc = 500000;   // [m]
    webgis.currentPosition.maxAgeSeconds = 0.1;  // [s]
    webgis.currentPosition.useWithSketchTool = true; 

    webgis.continuousPosition.useTransformationService = true;
    webgis.continuousPosition.transformationSpatialValidity = 20;

    //webgis.continuousPosition.helmert2d = {
    //    srs: 31256,
    //    Cx:  0.600,
    //    Cy: -0.234,
    //    Rx: -67946.151,
    //    Ry: 215079.498,
    //    r: 399.9992 * Math.PI / 200,
    //    scale: 1 + (-6.576 * 1e-6)
    //};
}

if (webgis.markerIcons["query_result"]["webgis.tools.coordinates"]) {
    webgis.markerIcons["query_result"]["webgis.tools.coordinates"].label = function (i, f) {
        var label = '';
        for (let property in f.properties) {
            console.log(property);
            if (property == "DGM 10m" || property == "ALS DOM") {
                if (f.properties[property].length > 0 && f.properties[property][0]) {
                    if (label) {
                        label += '\n';
                    }

                    label += `${property}: ${f.properties[property][0]}`;
                }
            }
        }
        return label;
    };
}



