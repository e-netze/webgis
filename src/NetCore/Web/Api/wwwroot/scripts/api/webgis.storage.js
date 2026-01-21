webgis.localStorage = new function () {
    this.get = function (key) {
        console.log('storage default', key, webgis.defaults[key]);
        let defaulValue = webgis.defaults[key];
        if (defaulValue && defaulValue.indexOf("!") === 0) { // force this value!!!
            console.log('storage forced', key, defaulValue.substring(1));
            return defaulValue.substring(1);
        }

        if (this.usable) {
            return localStorage.getItem(key) || defaulValue || '';
        }
        return defaulValue || '';
    };
    this.set = function (key, val) {
        if (this.usable) {
            localStorage.setItem(key, val);
        }
    };
    this.remove = function (key) {
        if (this.usable) {
            localStorage.removeItem(key);
        }
    };
    this.usable = function () {
        return typeof (Storage) !== 'undefined' && localStorage !== null;
    };

    this.getAnonymousUserId = function () {
        return this.get('_anonymousUserId');
    };
    this.setAnonyousUserId = function (id) {
        this.set('_anonymousUserId', id);
    };
};