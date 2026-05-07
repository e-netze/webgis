webgis.debugGeolocationApi = new function () {
    // Central Graz – used for getCurrentPosition
    var _center = { lat: 47.0707, lng: 15.4395 }; // Hauptplatz Graz

    // Route through central Graz for watchPosition (Hauptplatz → Stadtpark)
    var _route = [
        { lat: 47.0707, lng: 15.4395 }, // Hauptplatz
        { lat: 47.0697, lng: 15.4413 }, // Franziskanerplatz
        { lat: 47.0683, lng: 15.4437 }, // Herrengasse Süd
        { lat: 47.0670, lng: 15.4458 }, // Jakominiplatz
        { lat: 47.0657, lng: 15.4478 }, // Hans-Sachs-Gasse
        { lat: 47.0643, lng: 15.4501 }  // Stadtpark / Opernring
    ];

    var _stepsPerSegment = 10; // steps between two waypoints (1 step = 1 second)

    var _randomOffset = function (range) {
        return (Math.random() - 0.5) * 2 * range;
    };

    var _makePosition = function (lat, lng, accuracy, heading, speed) {
        return {
            coords: {
                latitude: lat,
                longitude: lng,
                accuracy: accuracy,
                heading: heading,
                speed: speed,
                altitude: null,
                altitudeAccuracy: null
            },
            timestamp: new Date().getTime()
        };
    };

    var _calcHeading = function (from, to) {
        var dLng = to.lng - from.lng;
        var dLat = to.lat - from.lat;
        return (Math.atan2(dLng, dLat) * 180 / Math.PI + 360) % 360;
    };

    this.isAvailable = function () {
        return true;
    };

    this.name = "Fake Geolocation (Debug)";

    this.getCurrentPosition = function (successCallback, errorCallback, options) {
        var lat = _center.lat + _randomOffset(0.002);
        var lng = _center.lng + _randomOffset(0.003);
        var accuracy = 5 + Math.random() * 20;
        setTimeout(function () {
            successCallback(_makePosition(lat, lng, accuracy, null, 0));
        }, 200 + Math.random() * 300);
    };

    this.watchPosition = function (successCallback, errorCallback, options) {
        var step = 0;
        var segmentIndex = 0;
        var forward = true;

        var intervalId = setInterval(function () {
            var fromIndex = segmentIndex;
            var toIndex = segmentIndex + (forward ? 1 : -1);
            var from = _route[fromIndex];
            var to = _route[toIndex];

            var t = step / _stepsPerSegment;
            var lat = from.lat + (to.lat - from.lat) * t + _randomOffset(0.00004);
            var lng = from.lng + (to.lng - from.lng) * t + _randomOffset(0.00006);
            var heading = _calcHeading(from, to);
            var speed = 1.2 + Math.random() * 0.8; // ~1.2–2.0 m/s (walking)
            var accuracy = 3 + Math.random() * 8;

            successCallback(_makePosition(lat, lng, accuracy, heading, speed));

            step++;
            if (step >= _stepsPerSegment) {
                step = 0;
                segmentIndex += forward ? 1 : -1;
                // reverse direction at the ends of the route
                if (segmentIndex >= _route.length - 1) {
                    segmentIndex = _route.length - 2;
                    forward = false;
                } else if (segmentIndex < 0) {
                    segmentIndex = 0;
                    forward = true;
                }
            }
        }, 1000);

        return intervalId;
    };

    this.clearWatch = function (watchId) {
        clearInterval(watchId);
    };
};

webgis.geolocationApis.add(webgis.debugGeolocationApi);
