webgis.hmacController = function (keys) {
    var _keys = keys;
    var _favtaskname = null;
    var _currentBranch = null;

    if (_keys && _keys.ticks) {
        _keys.ticks_diff = _keys.ticks - new Date().getTime();
    }

    this._createHash = function (dataString, password) {
        if (CryptoJS.HmacSHA512) {
            return (webgis.cryptoJS || CryptoJS).HmacSHA512(dataString, password).toString((webgis.cryptoJS || CryptoJS).enc.Base64);
        } else if (CryptoJS.HmacSHA256) {
            return (webgis.cryptoJS || CryptoJS).HmacSHA256(dataString, password).toString((webgis.cryptoJS || CryptoJS).enc.Base64);
        } else if (CryptoJS.HmacSHA1) {
            return (webgis.cryptoJS || CryptoJS).HmacSHA1(dataString, password).toString((webgis.cryptoJS || CryptoJS).enc.Base64);
        }
    }

    this._featureGeoHashCode = function (feature) {
        if (!feature && !feature.oid) {
            return null;
        }

        var acc = 10000000;
        var hashString = feature.oid.toString();

        if (feature.bounds && feature.bounds.length === 4) {
            var minx = Math.round(feature.bounds[0] * acc),
                miny = Math.round(feature.bounds[1] * acc),
                maxx = Math.round(feature.bounds[2] * acc),
                maxy = Math.round(feature.bounds[3] * acc);

            hashString += minx.toString() + miny.toString() + maxx.toString() + maxy.toString();
        }

        return (webgis.cryptoJS || CryptoJS)
            .HmacSHA1(hashString, feature.oid.toString())
            .toString((webgis.cryptoJS || CryptoJS).enc.Base64);
    };

    this.appendHMACData = function (data) {
        data = data || {};
        if (!_keys || !_keys.privateKey) {
            data.hmac = false;
            return data;
        }
        data.hmac = true;
        data.hmac_pubk = _keys.publicKey;
        data.hmac_ts = new Date().getTime() + _keys.ticks_diff;
        data.hmac_data = Math.random().toString(36).slice(2);
        data.hmac_hash = this._createHash(data.hmac_ts + data.hmac_data, _keys.privateKey);

        if (webgis.globals && webgis.globals.urlParameters) {
            data._original_url_parameters = JSON.stringify(webgis.globals.urlParameters);
        }

        data._apiv = webgis.api_version;

        if (_favtaskname)
            data.hmac_ft = _favtaskname;

        if (_currentBranch)
            data.hmac_br = _currentBranch;

        // default Parameter -> sollten immer mitgeschickt werden!!
        if (!data.__gdi)
            data.__gdi = webgis.gdiScheme;

        // user language
        data._ul = webgis.l10n.language;

        return data;
    };
    this.urlParameters = function (data) {
        var data = this.appendHMACData(data);
        var url = '';
        for (var p in data) {
            url += '&' + p + "=" + encodeURIComponent(data[p]);
        }
        return url;
    };
    this.appendHMACDataToUrl = function (url, data) {
        var params = this.urlParameters(data || {});
        return url + (url.indexOf('?') < 0 ? '?' : '') + params;
    };
    this.userName = function () {
        return _keys ? _keys.username : 'unknown';
    };
    this.userDisplayName = function () {
        var username = this.userName();

        if (username.indexOf('::') > 0) {
            username = username.substr(username.indexOf('::') + 2);
        }
        if (username.indexOf('@@') > 0) {
            username = username.substr(0, username.indexOf('@@'));
        }

        return username;
    };
    this.isAnonymous = function () {
        return (_keys && _keys.username) ? false : true;
    };
    this.favoritesProgramAvailable = function () { return _keys.favstatus >= 0; };
    this.favoritesProgramActive = function () { return _favtaskname != null; }
    this.checkFavoritesStatus = function (msg, taskname, callback, onActivate, onDeactivate, forceDialog) {
        taskname = (webgis.cryptoJS || CryptoJS).HmacSHA1(webgis.url.encodeString(taskname.toLowerCase()), "taskname").toString((webgis.cryptoJS || CryptoJS).enc.Base64);
        //console.log('fav-taskname', taskname);

        if (_keys.favstatus === 0 || forceDialog) {
            var showConfirm = function () {
                webgis.confirm({
                    title: 'Favoriten-Programm',
                    height: '470px',
                    message: msg,
                    iconUrl: webgis.css.imgResource('fav-100.png'),
                    okText: _keys.favstatus === 1 ? 'Weiterhin teilnehmen' : 'Ja, Teilnehmen!',
                    cancelText: _keys.favstatus === 1 ? 'Verlassen' : 'Nein, Danke!',
                    onOk: function () {
                        _favtaskname = taskname.toLowerCase();
                        _keys.favstatus = 1;
                        if (onActivate)
                            onActivate();
                        callback();
                    },
                    onCancel: function () {
                        keys.favstatus = 2;
                        _favtaskname = null;
                        if (onDeactivate)
                            onDeactivate();
                        callback();
                    }
                });
            };

            if (msg.indexOf("https://") == 0 || msg.indexOf("http://") == 0) {
                webgis.$.get(msg, function (data) {
                    msg = data;
                    showConfirm();
                });
            } else {
                showConfirm();
            }
        } else {
            if (_keys.favstatus === 1)
                _favtaskname = taskname.toLowerCase();
            callback();
        }
    };
    this.setCurrentBranch = function (brach) { _currentBranch = brach };
};

webgis.security = {
    allowEmbeddingMessages: false,
    _embeddingMessagesTarget: null
};