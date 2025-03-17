window.addEventListener('message', function (event) {
    //console.log('@webgis: receive-message:', event);

    if (!event || !event.data)
        return;

    if (webgis.security.allowEmbeddingMessages !== true) {
        console.log('Security Warning: webgis.security.allowEmbeddingMessages === false => embedding messages are forbidden. Change this value to \'true\' to allow embedding messages!');
    }

    // ToDo: Check origin

    if (!webgis.security._embeddingMessagesTarget) {
        webgis.security._embeddingMessagesTarget = window.parent;
    }

    var sendMapRefresh = function (sender) {
        if (webgis.security._embeddingMessagesTarget) {
            webgis.security._embeddingMessagesTarget.postMessage({
                event: 'map-refresh',
                mapId: $(sender.elem).attr('id'),
                scale: sender.scale(),
                center: sender.getCenter(),
                bounds: sender.getExtent()
            },'*');
        }
    };

    var sendMapImage = function (sender, result) {
        if (webgis.security._embeddingMessagesTarget) {

            result.event = 'current-map-image';
            result.mapId = $(sender.elem).attr('id');

            webgis.security._embeddingMessagesTarget.postMessage(result, '*');
        }
    };

    if (event.data.event === 'register-map-events') {
        for (var m in webgis.maps) {
            var map = webgis.maps[m];

            map.events.on('refresh', function (channel, sender, event) {
                sendMapRefresh(sender);
            });

            sendMapRefresh(map);
        }
    }
    if (event.data.event === 'get-current-map-image') {
        var map = webgis.maps[event.data.mapId];
        console.log('map', map);
        if (map) {
            map.downloadCurrentImage({
                format: event.data.format,
                result_format: event.data.result_format
            }, function (result) {
                sendMapImage(map, result);
            });
        }
    }
}); 