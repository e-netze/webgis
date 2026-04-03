webgis.navigatorGeolocationApi = new function () {
    this.isAvailable = function () {
        return ("geolocation" in navigator) == true; 
    };
    this.name = "Browser/Navigator (Defaut)";

    this.getCurrentPosition = function (successCallback, errorCallback, options) {
        navigator.geolocation.getCurrentPosition(successCallback, errorCallback, options);
    };
    this.watchPosition = function (successCallback, errorCallback, options) {
        return navigator.geolocation.watchPosition(successCallback, errorCallback, options);
    };
    this.clearWatch = function (watchId) {
        navigator.geolocation.clearWatch(watchId);
    };
};

webgis.geolocationApis.add(webgis.navigatorGeolocationApi);
