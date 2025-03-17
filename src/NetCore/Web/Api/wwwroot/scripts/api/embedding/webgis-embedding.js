var webgis_embedding = function (elementId, options) {

    var frame = document.getElementById(elementId);
 
    window.addEventListener('message', function(event) {
        //console.log('@parent: receive-message:', event);

        if (event.data.event === 'webgis-ready') {
            frame.contentWindow.postMessage({ event: 'register-map-events' }, '*');
        }
        else if (event.data.event === 'map-refresh') {
            if (options.onChangeExtent) {
                options.onChangeExtent(event.data);
            }
        }
        else if (event.data.event === 'current-map-image') {
            if (options.onReceiveCurrentMapImage) {
                options.onReceiveCurrentMapImage(event.data);
            }
        }
    });

    var requestCurrentMapImage = function(mapId, format, result_format) {
        frame.contentWindow.postMessage(
            {
                event: 'get-current-map-image',
                mapId: mapId,
                format: format || 'png',
                result_format: result_format || ''
            }, '*');
    }

    return {
        requestCurrentMapImage: requestCurrentMapImage
    }
};