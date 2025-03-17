const CACHE_NAME = 'webgis-v{{version}}';

var urlsToCache = [
    
];

function _isFileType(url, type) {
    url = url.toLowerCase();
    type = type.toLowerCase();

    if (url.substr(url.length - type.length - 1) === "." + type ||
        url.indexOf("." + type + "?") > 0)
        return true;
}
function _isJs(url) { return _isFileType(url, "js"); }
function _isCss(url) { return _isFileType(url, "css"); }
function _isPortalImage(url) {
    return false;
}
function _isHttp(url) {
    return url.toLowerCase().indexOf('http://') === 0;
}

self.addEventListener('install', function (event) {
    self.skipWaiting();

    // Perform install steps
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(function (cache) {
                console.log('Opened cache');
                return cache.addAll(urlsToCache);
            })
    );
});

self.addEventListener('activate', event => {
    // delete any caches that aren't in expectedCaches
    // which will get rid of static-v1
    event.waitUntil(
        caches.keys().then(keys => Promise.all(
            keys.map(key => {
                if (key !== CACHE_NAME) {
                    console.log('delete cache: ' + key);
                    return caches.delete(key);
                }
            })
        )).then(() => {
            console.log(CACHE_NAME + ' now ready to handle fetches!');
        })
    );
});

self.addEventListener('fetch', function (event) {
    return event;

    //// DoTo: Return default, if http://  -> mixed content error!!!
    //if (_isHttp(event.request.url))
    //    return event;

    //event.respondWith(
    //    caches.match(event.request)
    //        .then(function (response) {
    //            // Cache hit - return response
    //            if (response) {
    //                return response;
    //            }

    //            console.log('fetch ' + event.request.url);
    //            if (_isJs(event.request.url) || _isCss(event.request.url) || _isPortalImage(event.request.url)) {
    //                // IMPORTANT: Clone the request. A request is a stream and
    //                // can only be consumed once. Since we are consuming this
    //                // once by cache and once by the browser for fetch, we need
    //                // to clone the response.
    //                var fetchRequest = event.request.clone();

    //                return fetch(fetchRequest).then(
    //                    function (response) {
    //                        // Check if we received a valid response
    //                        if (!response || response.status !== 200 /*|| response.type !== 'basic'*/) {
    //                            return response;
    //                        }

    //                        // IMPORTANT: Clone the response. A response is a stream
    //                        // and because we want the browser to consume the response
    //                        // as well as the cache consuming the response, we need
    //                        // to clone it so we have two streams.
    //                        var responseToCache = response.clone();

    //                        caches.open(CACHE_NAME)
    //                            .then(function (cache) {
    //                                cache.put(event.request, responseToCache);
    //                            });

    //                        return response;
    //                    }
    //                );
    //            }


    //            //console.log("fetch: " + event.request.url);
    //            return fetch(event.request);
    //        })
    //);
});
