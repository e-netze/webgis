webgis.localStorage = new function () {
    this.get = function (key) {
        console.log('storage default', key, webgis.defaults[key])
        if (this.usable) {
            return localStorage.getItem(key) || webgis.defaults[key] || '';
        }
        return webgis.defaults[key] || '';
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