webgis.currentPosition_classic = new function () {
    this.get = function (options) {
        options = options || { onSuccess: function () { } };
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(options.onSuccess, options.onError || this.onError, { timeout: 30000 });
        }
        else {
            webgis.alert('Ortung wird nicht unterstützt');
        }
    };
    this.onError = function (err) {
        webgis.alert('Fehler beim Ortungsversuch: ' + err.message);
    };
};

webgis.currentPosition_watch = new function () {
    this.maxWatch = 3;
    this.minAcc = 30;
    this.maxAcc = webgis.isTouchDevice() ? 10000 : 1000;
    this.bestAcc = Number.MAX_VALUE;
    this.maxAgeSeconds = Number.MAX_VALUE;
    this.watchIds = [];
    this.useWithSketchTool = false;
    this.useWithSketchToolTools = ["webgis.tools.editing.insertfeature", "webgis.tools.editing.updatefeature", "webgis.tools.measureline", "webgis.tools.measurearea"];
    this.useWithSketchToolGeometryTypes = ["point", "polyline", "polygon"];
    this.canUsedWithSketchTool = function (tool, sketch) {
        if (this.useWithSketchTool === false ||
            !tool ||
            !sketch ||
            !tool.tooltype ||
            tool.tooltype.indexOf('sketch') !== 0 ||
            !sketch.isEditable()) {
            return false;
        }

        if (this.useWithSketchToolTools && this.useWithSketchToolTools.length > 0) {
            if ($.inArray(tool.id, this.useWithSketchToolTools) < 0) {
                return false;
            }
        }

        if (this.useWithSketchToolGeometryTypes && this.useWithSketchToolGeometryTypes.length > 0) {
            if ($.inArray(sketch.getGeometryType(), this.useWithSketchToolGeometryTypes) < 0) {
                return false;
            }
        }

        return true;
    };
    this.get = function (options) {
        options = options || { onSuccess: function () { } };

        var watcher = function (options, max) {
            //var options = options;
            var index = 0, maxWatch = max;
            var bestPosition;
            var lastErr;

            this.getPosition = function (pos) {
                index++;
                console.log('getPosition', index, maxWatch);
                //console.log(pos);
                //console.log(pos.coords.accuracy);
                var ageSeconds = (new Date().getTime() - pos.timestamp) / 1000;
                //console.log(ageSeconds);
                if (!options.highAccuracy || ageSeconds < webgis.currentPosition_watch.maxAgeSeconds) {
                    if (!bestPosition)
                        bestPosition = pos;
                    else if (bestPosition.coords.accuracy > pos.coords.accuracy)
                        bestPosition = pos;
                }
                if (bestPosition)
                    webgis.currentPosition_watch.bestAcc = Math.min(webgis.currentPosition_watch.bestAcc, bestPosition.coords.accuracy);
                if (options.highAccuracy == true) {
                    if (bestPosition && bestPosition.coords.accuracy <= webgis.currentPosition_watch.minAcc)
                        webgis.currentPosition_watch.stopWatch(options, bestPosition);
                    if (index >= maxWatch)
                        webgis.currentPosition_watch.stopWatch(options, null, {
                            message: "Ortung mit gewünschter Genauigkeit von " + webgis.currentPosition_watch.minAcc + "m nicht möglich",
                            code: bestPosition == null ? 0 : 1
                        });
                }
                else {
                    if (index >= maxWatch ||
                        bestPosition.coords.accuracy < webgis.currentPosition_watch.minAcc ||
                        bestPosition.coords.accuracy < webgis.currentPosition_watch.bestAcc * 1.2 || // 20% von bester erreichter ist auch ok
                        bestPosition.coords.accuracy > webgis.currentPosition_watch.maxAcc) // MaxAcc -> Wenn Desktop Computer, wirds nicht besser -> Chrome braucht sonst 10min für ergebnis!!!
                        webgis.currentPosition_watch.stopWatch(options, bestPosition);
                }
            };

            this.onError = function (err) {
                lastErr = err;
                index++;
                console.log(err.message, err.code);
                //if (index >= maxWatch)
                webgis.currentPosition_watch.stopWatch(options, bestPosition, lastErr);
            };
        };

        var w = new watcher(options, options.maxWatch || 1);
        if (navigator.geolocation) {
            options.watchId = navigator.geolocation.watchPosition(w.getPosition, w.onError, { timeout: 5000, enableHighAccuracy: webgis.isTouchDevice() });
            //this.watchIds.push(options.watchId);
        }
        else {
            webgis.alert('Ortung wird nicht unterstützt');
        }
    };
    this.stopWatch = function (options, bestPosition, lastErr) {
        navigator.geolocation.clearWatch(options.watchId);
        if (!bestPosition) {
            if (options.onError)
                options.onError(lastErr);
            else
                webgis.alert('Ortung nicht möglich: ' + (lastErr ? lastErr.message + " (Error Code " + lastErr.code + ")" : ""));
        }
        else
            options.onSuccess(bestPosition);
    };
    //this.forceStopWatch = function () {
    //    for (var i in this.watchIds) {
    //        navigator.geolocation.clearWatch(options.watchId);
    //    }
    //}
};

webgis.currentPosition = webgis.isTouchDevice() ? webgis.currentPosition_watch : webgis.currentPosition_classic;

webgis.continuousPosition = new function () {
    this._map = null;
    this._watchId = null;
    this._watcher = null;
    this._marker = null;
    var _isWatching = false;
    webgis.implementEventController(this);
    this.start = function (map, options) {
        //console.log('start');
        if (webgis.continuousPosition._map == map)
            return;
        if (webgis.continuousPosition._map != null) {
            webgis.alert('Verfolgung läuft bereits!');
            return;
        }
        webgis.continuousPosition._map = map;
        map.removeMarkerGroup('currentPos');
        var defaults = {
            marker: 'watcher',
            minAcc: 500,
            maxAgeSeconds: Number.MAX_VALUE
        };
        options = $.extend({}, defaults, options);
        if (this.helmert2d)
            webgis.registerCRS(this.helmert2d.srs);

        var watcher = function () {
            this.helmert2d = null;
            this.isHelmert2dValid = function (pos) {
                if (webgis.continuousPosition.useTransformationService !== true)
                    return true;

                var helmert2d = webgis.continuousPosition._watcher.helmert2d;

                if (helmert2d == null)
                    return false;

                if (helmert2d.name === '_none')
                    return true;

                if (helmert2d._request_pos &&
                    helmert2d._request_pos._trans_spatial_validity
                    && pos) {
                    try {
                        var spatialDist = webgis.calc.SphericDistance(
                            helmert2d._request_pos.coords.longitude,
                            helmert2d._request_pos.coords.latitude,
                            pos.coords.longitude,
                            pos.coords.latitude
                        );
                        //console.log('spaitalDist', spatialDist);
                        if (spatialDist > helmert2d._request_pos._trans_spatial_validity) {
                            //console.log('spaitalDist > ' + helmert2d._request_pos._trans_spatial_validity);
                            return false;
                        }
                    } catch (e) {
                        console.trace(e);
                    }
                }
                return true;
            };
            this.newPosition = function (pos) {
                if (!webgis.continuousPosition._watcher.isHelmert2dValid(pos)) {

                    var ageSeconds = (new Date().getTime() - pos.timestamp) / 1000,
                        acc = pos.coords.accuracy;

                    var isOk = !(acc > Math.max(50, options.minAcc) ||      // für die Bestimmung der Transformation sollte man mindest 50m genau und nicht älter als 10Secunden sein
                        ageSeconds > Math.max(10, options.maxAgeSeconds));

                    if (isOk) {
                        $.get(webgis.baseUrl + '/rest/Helmert2dTransformation?lng=' + pos.coords.longitude + '&lat=' + pos.coords.latitude, function (result) {
                            if (result) {
                                //webgis.alert("Lokale Transformation wurde bestimmt: " + result.name, "Info");
                                webgis.toastMessage("GPS:", "Lokale Transformation: " + result.name, webgis.continuousPosition.showInfo);

                                result._request_pos = pos;
                                result._request_pos._trans_spatial_validity =
                                    webgis.continuousPosition.transformationSpatialValidity ?
                                        webgis.continuousPosition.transformationSpatialValidity :
                                        1000;
                                webgis.continuousPosition._watcher.helmert2d = result;
                            }
                            webgis.continuousPosition._watcher.processPosition(pos);
                        });
                    } else {
                        // Koordinate trotzdem schicken, sonst wird in der GPS Bubble nix angezeigt 
                        webgis.continuousPosition.current = {
                            status: '',  // not ok
                            lat: pos.coords.latitude,
                            lng: pos.coords.longitude,
                            acc: acc
                        };
                        webgis.continuousPosition.events.fire('watchposition', webgis.continuousPosition, pos);
                    }
                }
                else {
                    webgis.continuousPosition._watcher.processPosition(pos);
                }
            };
            this.processPosition = function (pos) {
                //console.log(pos);
                var lng = pos.coords.longitude,
                    lat = pos.coords.latitude,
                    ageSeconds = (new Date().getTime() - pos.timestamp) / 1000,
                    acc = pos.coords.accuracy; // / (webgis.calc.R) * 180.0 / Math.PI;

                if (webgis.continuousPosition._marker)
                    webgis.continuousPosition._map.removeMarker(webgis.continuousPosition._marker);
                if (this.helmert2d && this.helmert2d.name != '_none') {
                    webgis.registerCRS(this.helmert2d.srs);
                    var xy = webgis.project(this.helmert2d.srs, [lng, lat]);
                    //console.log(xy);
                    var xy_ = webgis.calc.helmert2d(xy[0], xy[1], this.helmert2d.Cx, this.helmert2d.Cy, this.helmert2d.scale, this.helmert2d.r, this.helmert2d.Rx, this.helmert2d.Ry);
                    //console.log(xy_);
                    var lnglat_ = webgis.unproject(this.helmert2d.srs, xy_);
                    //console.log([lng, lat]);
                    //console.log(lnglat_);
                    lng = lnglat_[0];
                    lat = lnglat_[1];
                }
                webgis.continuousPosition.events.fire('watchposition', webgis.continuousPosition, pos);
                var isOk = !(acc > options.minAcc ||
                    ageSeconds > options.maxAgeSeconds);
                webgis.continuousPosition._marker = webgis.continuousPosition._map.addMarker({
                    lat: lat, lng: lng,
                    icon: isOk ? (options.marker_ok || options.marker) : options.marker,
                    angle: pos.coords.heading
                });
                webgis.continuousPosition.current = {
                    status: isOk ? 'ok' : '',
                    lat: lat,
                    lng: lng,
                    acc: acc
                };
                var speed = pos.coords.speed && !isNaN(pos.coords.speed) ? pos.coords.speed : 0;
                $('.webgis-continous-position-speed').val(Math.round(speed * 3.6) + " km/h");
                var bounds = webgis.continuousPosition._map.getExtent(), boundsW = Math.abs(bounds[2] - bounds[0]) * .4, boundsH = Math.abs(bounds[3] - bounds[1]) * .4;
                var center = webgis.continuousPosition._map.getCenter();
                bounds = [center[0] - boundsW, center[1] - boundsH, center[0] + boundsW, center[1] + boundsW];
                if (lng < bounds[0] || lng > bounds[2] || lat < bounds[1] || lat > bounds[3])
                    webgis.continuousPosition._map.setCenter([lng, lat]);
            };
        };
        this._watcher = new watcher();
        if (webgis.continuousPosition.helmert2d) {
            this._watcher.helmert2d = webgis.continuousPosition.helmert2d;
        }

        if (navigator.geolocation) {
            this.events.fire('startwatching', this);
            _isWatching = true;
            webgis.continuousPosition._watchId = navigator.geolocation.watchPosition(this._watcher.newPosition, function () { }, { timeout: 5000, enableHighAccuracy: webgis.isTouchDevice() });
        }
        else {
            webgis.alert('Ortung wird nicht unterstützt');
        }
    };
    this.stop = function () {
        this.events.fire('stopwatching', this);
        _isWatching = false;
        if (webgis.continuousPosition._map && webgis.continuousPosition._watchId >= 0) {
            navigator.geolocation.clearWatch(webgis.continuousPosition._watchId);
            if (webgis.continuousPosition._marker != null)
                webgis.continuousPosition._map.removeMarker(webgis.continuousPosition._marker);
        }
        webgis.continuousPosition._watchId = null;
        webgis.continuousPosition._map = null;
        webgis.continuousPosition._marker = null;
        webgis.continuousPosition._watcher = null;
    };
    this.current = null;
    this.isWatching = function () { return _isWatching; };
    this.showInfo = function () {
        $('body').webgis_modal({
            title: 'GPS Messinfo',
            onload: function ($content) {
                if (webgis.currentPosition.useWithSketchTool) {
                    $("<h3>Genauigkeit</h3>").appendTo($content);
                    $("<table class='webgis-result-table'>" +
                        "<tr><td class='webgis-result-table-header'>Min Acc</td><td>" + webgis.currentPosition.minAcc + "m</td></tr>" +
                        "<tr><td class='webgis-result-table-header'>Max Age</td><td>" + webgis.currentPosition.maxAgeSeconds + "s</td></tr></table>")
                        .appendTo($content);
                }
                if (webgis.continuousPosition._watcher &&
                    webgis.continuousPosition._watcher.helmert2d &&
                    webgis.continuousPosition._watcher.helmert2d.name !== '_none') {
                    var helmert2d = webgis.continuousPosition._watcher.helmert2d;
                    $("<h3>Trafo: Lokale Helmert Transform.</h3>").appendTo($content);
                    $("<table class='webgis-result-table'>" +
                        "<tr><td class='webgis-result-table-header'>Name</td><td>" + helmert2d.name + "</td></tr>" +
                        "<tr><td class='webgis-result-table-header'>SRef [EPSG]</td><td>" + helmert2d.srs + "</td></tr>" +
                        "<tr><td class='webgis-result-table-header'>TransX [m]</td><td>" + Math.round(helmert2d.Cx * 1e3) / 1e3 + "</td></tr>" +
                        "<tr><td class='webgis-result-table-header'>TransY [m]</td><td>" + Math.round(helmert2d.Cy * 1e3) / 1e3 + "</td></tr>" +
                        "<tr><td class='webgis-result-table-header'>Rx [m]</td><td>" + Math.round(helmert2d.Rx * 1e3) / 1e3 + "</td></tr>" +
                        "<tr><td class='webgis-result-table-header'>Ry [m]</td><td>" + Math.round(helmert2d.Ry * 1e3) / 1e3 + "</td></tr>" +
                        "<tr><td class='webgis-result-table-header'>Rotation [°]</td><td>" + Math.round(helmert2d.r * 180.0 / Math.PI * 1e4) / 1e4 + "</td></tr>" +
                        "<tr><td class='webgis-result-table-header'>Scale [ppm]</td><td>" + Math.round((helmert2d.scale - 1) * 1e6 * 1e3) / 1e3 + "</td></tr>" +
                        "</table>")
                        .appendTo($content);
                    if (webgis.continuousPosition._watcher.helmert2d._request_pos) {
                        $("<h3>Trafo: Bestimmungsort/Qualität</h3>").appendTo($content);
                        $("<table class='webgis-result-table'>" +
                            "<tr><td class='webgis-result-table-header'>Beitengrad</td><td>" + webgis.continuousPosition._watcher.helmert2d._request_pos.coords.latitude + "</td></tr>" +
                            "<tr><td class='webgis-result-table-header'>Längengrad</td><td>" + webgis.continuousPosition._watcher.helmert2d._request_pos.coords.longitude + "</td></tr>" +
                            "<tr><td class='webgis-result-table-header'>Genauigkeit [m]</td><td>" + webgis.continuousPosition._watcher.helmert2d._request_pos.coords.accuracy + "</td></tr>" +
                            "<tr><td class='webgis-result-table-header'>Alter [sec]</td><td>" + ((new Date().getTime() - webgis.continuousPosition._watcher.helmert2d._request_pos.timestamp) / 1000) + "</td></tr>" +
                            "<tr><td class='webgis-result-table-header'>Räuml. Gültigkeit[m]</td><td>" + webgis.continuousPosition._watcher.helmert2d._request_pos._trans_spatial_validity + "</td></tr>" +
                            "</table>")
                            .appendTo($content);
                    }
                    $("<br/><button>Transformation temporär verwerfen</button>")
                        .addClass('webgis-button')
                        .appendTo($content)
                        .click(function () {
                            webgis.continuousPosition._watcher.helmert2d = { name: '_none' };
                            webgis.continuousPosition.showInfo();

                        });
                } else {
                    $("<p>Für die aktuelle Messung wird keine lokale Transformation verwendet!</p>").appendTo($content);
                }
            },
            width: '330px', height: '690px'
        });
    };
    this.isOk = function () {
        return (webgis.continuousPosition.current && webgis.continuousPosition.current.status === 'ok')
    };
};