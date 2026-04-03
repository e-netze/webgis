webgis.zenoConnectGeolocationApi = new function () {
    var _wsUrl = 'wss://zeno.geocloud.hexagon.com:44385/zenoconnect/NMEA';
    var _watchSockets = {};
    var _watchCounter = 0;

    // NMEA DDMM.MMMM → decimal degrees
    var _parseNmeaLat = function (str, direction) {
        if (!str || str.length < 4) return null;
        var deg = parseFloat(str.substr(0, 2));
        var min = parseFloat(str.substr(2));
        if (isNaN(deg) || isNaN(min)) return null;
        var decimal = deg + min / 60;
        return direction === 'S' ? -decimal : decimal;
    };

    // NMEA DDDMM.MMMM → decimal degrees
    var _parseNmeaLon = function (str, direction) {
        if (!str || str.length < 5) return null;
        var deg = parseFloat(str.substr(0, 3));
        var min = parseFloat(str.substr(3));
        if (isNaN(deg) || isNaN(min)) return null;
        var decimal = deg + min / 60;
        return direction === 'W' ? -decimal : decimal;
    };

    var _makePosition = function (lat, lng, accuracy, altitude) {
        return {
            coords: {
                latitude: lat,
                longitude: lng,
                accuracy: accuracy,
                heading: null,
                speed: null,
                altitude: altitude,
                altitudeAccuracy: null
            },
            timestamp: new Date().getTime()
        };
    };

    // Handles incoming NMEA messages.
    // pendingAccuracy is an object { value: null } shared between messages
    // so that a $GNGST sentence can pre-fill accuracy for the next $GPGGA.
    var _onMessage = function (e, pendingAccuracy, onPosition) {
        var table = e.data.split(',');
        var sentence = table[0];

        // $GNGST / $GPGST – accuracy statistics (sigma lat/lon in metres)
        if ((sentence === '$GNGST' || sentence === '$GPGST') && table.length >= 9) {
            var latAcc = parseFloat(table[6]);
            var lonAcc = parseFloat(table[7]);
            if (!isNaN(latAcc) && !isNaN(lonAcc)) {
                pendingAccuracy.value = Math.sqrt(latAcc * latAcc + lonAcc * lonAcc);
            }
            return;
        }

        // $GPGGA / $GNGGA – position fix
        if ((sentence === '$GPGGA' || sentence === '$GNGGA') && table.length >= 10) {
            var fixQuality = parseInt(table[6]);
            if (fixQuality === 0) return; // no GPS fix, skip

            var lat = _parseNmeaLat(table[2], table[3]);
            var lng = _parseNmeaLon(table[4], table[5]);
            if (lat === null || lng === null) return;

            var alt = parseFloat(table[9]);
            var accuracy = pendingAccuracy.value !== null ? pendingAccuracy.value : 10;
            onPosition(_makePosition(lat, lng, accuracy, isNaN(alt) ? null : alt));
        }
    };

    this.isAvailable = function () {
        return typeof WebSocket !== 'undefined';
    };

    this.name = "Zeno Connect (Leica)";

    // Single position: connect, wait for first valid fix, then close.
    this.getCurrentPosition = function (successCallback, errorCallback, options) {
        var done = false;
        var pendingAccuracy = { value: null };
        var ws;
        var timeoutHandle = null;

        var finish = function (pos, err) {
            if (done) return;
            done = true;
            if (timeoutHandle) clearTimeout(timeoutHandle);
            ws.close();
            if (pos) successCallback(pos);
            else if (errorCallback) errorCallback(err);
        };

        if (options && options.timeout) {
            timeoutHandle = setTimeout(function () {
                finish(null, { code: 3, message: 'Timeout' });
            }, options.timeout);
        }

        try {
            ws = new WebSocket(_wsUrl);
        } catch (ex) {
            if (errorCallback) errorCallback({ code: 2, message: ex.message });
            return;
        }

        ws.onmessage = function (e) {
            _onMessage(e, pendingAccuracy, function (pos) {
                finish(pos, null);
            });
        };

        ws.onerror = function () {
            finish(null, { code: 2, message: 'WebSocket connection failed. Is Zeno Connect App running?' });
        };
    };

    // Continuous watch: keep connection open and report every valid fix.
    this.watchPosition = function (successCallback, errorCallback, options) {
        var watchId = ++_watchCounter;
        var pendingAccuracy = { value: null };
        var ws;

        try {
            ws = new WebSocket(_wsUrl);
        } catch (ex) {
            if (errorCallback) errorCallback({ code: 2, message: ex.message });
            return watchId;
        }

        ws.onmessage = function (e) {
            _onMessage(e, pendingAccuracy, function (pos) {
                successCallback(pos);
            });
        };

        ws.onerror = function () {
            if (errorCallback) errorCallback({ code: 2, message: 'WebSocket connection failed. Is Zeno Connect App running?' });
        };

        ws.onclose = function () {
            delete _watchSockets[watchId];
        };

        _watchSockets[watchId] = ws;
        return watchId;
    };

    this.clearWatch = function (watchId) {
        if (_watchSockets[watchId]) {
            _watchSockets[watchId].close();
            delete _watchSockets[watchId];
        }
    };
};

webgis.geolocationApis.add(webgis.zenoConnectGeolocationApi);
