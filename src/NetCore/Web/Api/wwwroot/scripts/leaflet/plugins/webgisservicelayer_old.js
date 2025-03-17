L.ImageOverlay.webgis_service = L.ImageOverlay.extend({

    defaultParams: {
        f: 'json',
        layers: ''
    },
    _me: null,
    _service: null,
    _removed: false,
    _opacity: 1.0,
    _currentImageRequestId: '',
    _isLoading: true,
    _imageEventsAdded: false,
    initialize: function (url, options) {

        if (webgis)
            webgis.implementEventController(this);

        this._baseUrl = url;
        this._me = this;

        var params = L.Util.extend({}, this.defaultParams);

        if (params.detectRetina && L.Browser.retina) {
            params.width = params.height = this.options.tileSize * 2;
        } else {
            params.width = params.height = this.options.tileSize;
        }

        for (var i in options) {
            // all keys that are not ImageOverlay options go to WMS params
            if (!this.options.hasOwnProperty(i)) {
                params[i] = options[i];
            }
        }

        this.params = params;
        L.Util.setOptions(this, options);
        this.on('load', function () {
            this._updateImagePosition();
        }, this);
    },


    onAdd: function (map) {
        this._bounds = map.getBounds();
        this._map = map;

        var projectionKey = 'srs';
        this.params[projectionKey] = map.options.crs.code;
        this.params["mapname"] = map._webgis5Name;

        map.on("moveend", this._delayedRedrawSlow, this);

        map.on("zoomstart", function () {
            webgis.delayed(function (me) {
                //console.log('fadeout...', $(me._image).hasClass('effects'));
                $(me._image).css('opacity', 0);
            }, 500, this);

        }, this);

        L.ImageOverlay.prototype.onAdd.call(this, map);

        if (this._imageEventsAdded === false) {
            this._imageEventsAdded = true;
            var me = this;
            $(this._image)/*.css('opacity',0)*/.addClass('webgis-service-image effects');
            $(this._image).on('load', function () {
                //me._onImageLoad();

                var $image = $(me._image);
                me._isLoading = false;

                if (me._service && me._currentImageRequestId !== me._service.map.currentRequestId()) {
                    L.Util.extend(me._image, {
                        src: webgis.css.img('empty.gif')
                    });
                }
                if ($image.attr('src').indexOf('/empty.gif') < 0) {
                    webgis.delayed(function () {
                        me.setOpacity(me._opacity);
                    }, 1);
                }
            });
        }
    },

    setOrder: function (order) {
        if (this._image)
            this._image.style.zIndex = order;
    },
    setOpacity: function (opacity) {
        this._opacity = opacity;
        try {
            if (this._image) {
                this._image.style.opacity = opacity;
            }
        } catch (e) { }
    },

    _setService: function (service) {
        this._service = service;
        this._service.events.on('onchangevisibility', this._delayedRedrawFast, this);
        this._service.events.on('refresh', this._delayedRedrawFast, this);
        this._service.events.on('remove', function (e) {
            this._map.removeLayer(this);
            this._removed = true;
        }, this);

        this._service.map.events.on('requestidchanged', function (channel, sender) {
            if (this._currentImageRequestId !== sender.currentRequestId() && this._isLoading === true && this._image) {
                L.Util.extend(this._image, {
                    src: webgis.css.img('empty.gif')
                });
            }
        }, this);
    },
    _updateUrl: function () {
        if (this._service && this._service.isDestroyed() === true)
            return;

        var map = this._map,
            bounds = this._bounds,
            zoom = map.getZoom(),
            crs = map.options.crs,

            topLeft = map.latLngToLayerPoint(bounds.getNorthWest()),
            mapSize = map.latLngToLayerPoint(bounds.getSouthEast()).subtract(topLeft),

            nw = crs.project(bounds.getNorthWest()),
            se = crs.project(bounds.getSouthEast()),

            bbox = [nw.x, se.y, se.x, nw.y].join(',');

        var vislayers = [];
        if (this._service != null) {
            for (var i = 0; i < this._service.layers.length; i++) {
                var layer = this._service.layers[i];
                if (layer.visible === true) {
                    vislayers.push(layer.id);
                }
            }
        }
        this.params.layers = vislayers.join(',');

        var crsId = 0;
        if (this._service && this._service.map && this._service.map.crs)
            crsId = this._service.map.crs.id;

        urlParams = { width: mapSize.x, height: mapSize.y, bbox: bbox, crs: crsId };
        if (this._service && this._service.map) {
            this._service.map.appendRequestParameters(urlParams, this._service.id);
        }
        webgis.hmac.appendHMACData(urlParams);

        url = this._baseUrl + L.Util.getParamString(L.Util.extend({}, this.params, urlParams));
        url += '&requestid=' + this._service.map.currentRequestId();

        this._url = url;
    },

    //_reset: function () {
    //    console.log('reset overlay');
    //},

    _updateImagePosition: function () {
        // The original reset function really just sets the position and size, so rename it for clarity.
        //console.log('updatepos');
        L.ImageOverlay.prototype._reset.call(this);
    },

    _timer: null,

    _delayedRedrawSlow: function () {
        if (this._removed === true)
            return;
        this._delayedRedraw(900);
    },
    _delayedRedrawFast: function (e) {
        if (this._removed === true)
            return;
        this._delayedRedraw(100 /*300*/);
    },
    _delayedRedraw: function (ms) {
        if (this._removed === true)
            return;
        var me = this;
        //$(me._image).fadeOut(ms);

        window.clearTimeout(this._timer);
        this._timer = window.setTimeout(function () {
            me._redraw(me);
        }, ms);
    },

    _redraw: function () {
        if (this._removed === true || (this._service && this._service.isDestroyed() === true))
            return;

        var me = this;
        this._bounds = this._map.getBounds();

        this._updateUrl();

        if (me.events)
            me.events.fire('beginredraw');

        webgis.ajax({
            url: this._url,
            type: 'post',
            success: function (result) {
                if (result.requestid && result.requestid !== me._service.map.currentRequestId()) {
                    //console.log('request id abgelaufen...' + result.requestid);
                    return;
                }

                if (me._service && me._service.isDestroyed() === false) {
                    if (me.events)
                        me.events.fire('endredraw');

                    if (!result.url || result.url.indexOf('/empty.gif') !== -1) {
                        result.url = webgis.css.img('empty.gif');
                    }

                    me._currentImageRequestId = result.requestid;
                    me._isLoading = true;
                    if (me._imageEventsAdded) {
                        //console.log('hide image before loading...');
                        var $image = $(me._image);
                        if ($image.css('opacity') > 0 && $image.attr('src').indexOf('/emtpy.gif') < 0) {
                            $image
                                .removeClass('effects')
                                .css('opacity', 0).css('display', '')
                                .addClass('effects');
                        }
                    }
                    //L.Util.extend(me._image, {
                    //    src: result.url
                    //});
                    $image.attr('src', result.url);

                    //me.setOpacity(me._opacity);
                }
            },
            error: function () {
                if (me.events)
                    me.events.fire('endredraw');
            }
        });
    },

    // Not exists since Leaflet 1.0.0 -> this.on('load',...) on "initialize" does the same
    //_onImageLoad: function () {
    //    this.fire('load');

    //    // Only update the image position after the image has loaded.
    //    // This the old image from visibly shifting before the new image loads.


    //    this._updateImagePosition();
    //}
});

L.imageOverlay.webgis_service = function (url, service, options) {
    var l = new L.ImageOverlay.webgis_service(url, options);
    l._setService(service);
    return l;
};

L.ImageOverlay.webgis_collection = L.ImageOverlay.extend({

    defaultParams: {
        f: 'json',
        layers: ''
    },
    _me: null,
    _service: null,
    _removed: false,
    initialize: function (url, options) {

        if (webgis)
            webgis.implementEventController(this);

        this._baseUrl = url;
        this._me = this;

        var params = L.Util.extend({}, this.defaultParams);

        if (params.detectRetina && L.Browser.retina) {
            params.width = params.height = this.options.tileSize * 2;
        } else {
            params.width = params.height = this.options.tileSize;
        }

        for (var i in options) {
            // all keys that are not ImageOverlay options go to WMS params
            if (!this.options.hasOwnProperty(i)) {
                params[i] = options[i];
            }
        }

        this.params = params;
        L.Util.setOptions(this, options);

        this.on('load', function () {
            this._updateImagePosition();
        }, this);
    },


    onAdd: function (map) {
        this._bounds = map.getBounds();
        this._map = map;

        var projectionKey = 'srs';
        this.params[projectionKey] = map.options.crs.code;
        map.on("moveend", this._delayedRedrawSlow, this);

        map.on("zoomstart", function () {
            $(this._image).fadeOut(900);
        }, this);

        L.ImageOverlay.prototype.onAdd.call(this, map);

        this._services = this._service.map.addServices(this._service.serviceInfo.services);
    },

    setOrder: function (order) {
        if (this._image)
            this._image.style.zIndex = order;
    },
    setOpacity: function (opacity) {
        if (this._image)
            this._image.style.opacity = opacity;
    },

    _setService: function (service) {
        this._service = service;
        this._service.events.on('onchangevisibility', this._delayedRedrawFast, this);
        this._service.events.on('refresh', this._delayedRedrawFast, this);
        this._service.events.on('remove', function (e) {
            this._map.removeLayer(this);
            this._removed = true;
        }, this);
    },
    _updateUrl: function () {
        if (this._service && this._service.isDestroyed() === true)
            return;

        var map = this._map,
            bounds = this._bounds,
            zoom = map.getZoom(),
            crs = map.options.crs,

            topLeft = map.latLngToLayerPoint(bounds.getNorthWest()),
            mapSize = map.latLngToLayerPoint(bounds.getSouthEast()).subtract(topLeft),

            nw = crs.project(bounds.getNorthWest()),
            se = crs.project(bounds.getSouthEast()),

            bbox = [nw.x, se.y, se.x, nw.y].join(',');
    },

    _updateImagePosition: function () {
        // The original reset function really just sets the position and size, so rename it for clarity.
        L.ImageOverlay.prototype._reset.call(this);
    },

    _timer: null,

    _delayedRedrawSlow: function () {
        if (this._removed === true)
            return;
        this._delayedRedraw(900);
    },
    _delayedRedrawFast: function () {
        if (this._removed === true)
            return;
        this._delayedRedraw(300);
    },
    _delayedRedraw: function (ms) {
        if (this._removed === true)
            return;
        var me = this;
        //$(me._image).fadeOut(ms);

        window.clearTimeout(this._timer);
        this._timer = window.setTimeout(function () {
            me._redraw(me);
        }, ms);
    },

    _redraw: function () {
        if (this._removed === true || (this._service && this._service.isDestroyed() === true))
            return;

        var me = this;

        this._bounds = this._map.getBounds();

        this._updateUrl();

        if (me.events)
            me.events.fire('beginredraw');

        /*
        $.ajax({
            url: this._url,
            type: 'get',
            success: function (result) {
                if (result.requestid && result.requestid != me._service.map.currentRequestId()) {
                    //alert('request id abgelaufen...');
                    return;
                }

                if (me.events)
                    me.events.fire('endredraw');

                if (result.url.indexOf('/empty.gif') != -1)
                    result.url = webgis.css.img('empty.gif');
                L.Util.extend(me._image, {
                    src: result.url
                });
            },
            error: function () {
                if (me.events)
                    me.events.fire('endredraw');
            }
        });

        $(this._image).on('load', function () {
            $(this).css('display', 'none').fadeIn(500, function () {

            })
        });
        */
    },

    // Not exists since Leaflet 1.0.0 -> this.on('load',...) on "initialize" does the same
    //_onImageLoad: function () {
    //    this.fire('load');

    //    // Only update the image position after the image has loaded.
    //    // This the old image from visibly shifting before the new image loads.


    //    this._updateImagePosition();
    //}
});

L.imageOverlay.webgis_collection = function (url, service, options) {
    var l = new L.ImageOverlay.webgis_collection(url, options);
    l._setService(service);
    return l;
};

L.ImageOverlay.webgis_selection = L.ImageOverlay.extend({
    defaultParams: {
        f: 'json',
        layer: '',
        fids: ''
    },
    _me: null,
    _service: null,
    _name: '',
    _removed: false,
    _currentImageRequestId: '',
    _isLoading: true,
    _imageEventsAdded: false,

    initialize: function (url, options) {
        if (webgis)
            webgis.implementEventController(this);

        this._baseUrl = url;
        this._me = this;

        var params = L.Util.extend({}, this.defaultParams);

        if (params.detectRetina && L.Browser.retina) {
            params.width = params.height = this.options.tileSize * 2;
        } else {
            params.width = params.height = this.options.tileSize;
        }

        for (var i in options) {
            // all keys that are not ImageOverlay options go to WMS params
            if (!this.options.hasOwnProperty(i)) {
                params[i] = options[i];
            }
        }

        this.params = params;
        L.Util.setOptions(this, options);

        this.on('load', function () {
            this._updateImagePosition();
        }, this);
    },

    onAdd: function (map) {
        this._bounds = map.getBounds();
        this._map = map;

        var projectionKey = 'srs';
        this.params[projectionKey] = map.options.crs.code;
        this.params['mapname'] = map._webgis5Name;

        map.on("moveend", this._delayedRedrawSlow, this);

        map.on("zoomstart", function () {
            $(this._image).attr('src', webgis.css.img('empty.gif'));
        }, this);

        L.ImageOverlay.prototype.onAdd.call(this, map);

        if (this._imageEventsAdded === false) {
            this._imageEventsAdded = true;
            var me = this;
            $(this._image)/*.css('opacity', 0)*/.addClass('webgis-service-image effects');
            $(this._image).on('load', function () {
                //me._onImageLoad();
                var $image = $(me._image);
                me._isLoading = false;

                if (me._service && me._currentImageRequestId !== me._service.map.currentRequestId()) {
                    $(me._image).attr('src', webgis.css.img('empty.gif'));
                }

                if ($image.attr('src').indexOf('/empty.gif') < 0) {
                    webgis.delayed(function () {
                        //$image.css('opacity', 1.0);
                        me._image.style.opacity = 1.0;
                    }, 1);
                }
            });
        }
    },

    setTargetLayer: function (service, layerid, fids) {
        this._service = service;
        this.params.layer = layerid;
        this.params.fids = fids;
        this._removed = false;
    },
    setTargetQuery: function (service, queryId, fids) {
        this._service = service;
        this.params.query = queryId;
        this.params.fids = fids;
        this._removed = false;
    },

    _setSelection: function (selection) {
        this._name = selection.name;
        //this._service = selection.service;
        //if (this._service) {
        selection.events.on('refresh', this._delayedRedrawFast, this);
        selection.events.on('remove', function (e) {
            //console.log('remove');
            //this._map.removeLayer(this);
            this._image.src = '';
            this._removed = true;
        }, this);
        //}
    },
    _updateUrl: function () {
        if (!this._service)
            return;

        if (this._service && this._service.isDestroyed() === true)
            return;

        var map = this._map,
            bounds = this._bounds,
            zoom = map.getZoom(),
            crs = map.options.crs,

            topLeft = map.latLngToLayerPoint(bounds.getNorthWest()),
            mapSize = map.latLngToLayerPoint(bounds.getSouthEast()).subtract(topLeft),

            nw = crs.project(bounds.getNorthWest()),
            se = crs.project(bounds.getSouthEast()),

            bbox = [nw.x, se.y, se.x, nw.y].join(',');

        var vislayers = [];
        if (this._service != null && this._service.layers) {
            for (var i = 0; i < this._service.layers.length; i++) {
                var layer = this._service.layers[i];
                if (layer.visible === true) {
                    vislayers.push(layer.id);
                }
            }
        }
        this.params.layers = vislayers.join(',');

        var crsId = 0;
        if (this._service && this._service.map && this._service.map.crs)
            crsId = this._service.map.crs.id;

        urlParams = { width: mapSize.x, height: mapSize.y, bbox: bbox, crs: crsId, selection: this._name };
        if (this._service && this._service.map) {
            this._service.map.appendRequestParameters(urlParams, this._service.id);
        }
        webgis.hmac.appendHMACData(urlParams);

        url = this._baseUrl + this._service.id + '/getselection' + L.Util.getParamString(/*L.Util.extend({}, this.params, urlParams)*/urlParams);
        url += '&requestid=' + this._service.map.currentRequestId();

        this._url = url;
    },

    _updateImagePosition: function () {
        // The original reset function really just sets the position and size, so rename it for clarity.
        L.ImageOverlay.prototype._reset.call(this);
    },

    _delayedRedrawSlow: function () {
        if (this._removed === true)
            return;
        this._delayedRedraw(900);
    },
    _delayedRedrawFast: function () {
        if (this._removed === true)
            return;
        this._delayedRedraw(300);
    },
    _delayedRedraw: function (ms) {
        if (this._removed === true)
            return;
        var me = this;
        //$(me._image).fadeOut(ms);

        window.clearTimeout(this._timer);
        this._timer = window.setTimeout(function () {
            me._redraw(me);
        }, ms);
    },

    _redraw: function () {
        if (this._removed === true || (this._service && this._service.isDestroyed() === true))
            return;

        var me = this;
        this._bounds = this._map.getBounds();

        this._updateUrl();
        if (me.events)
            me.events.fire('beginredraw');

        webgis.ajax({
            url: this._url,
            type: 'post',
            data: this.params,
            success: function (result) {
                if (result.requestid && result.requestid != me._service.map.currentRequestId()) {
                    //alert('request id abgelaufen...');
                    return;
                }

                if (me._service && me._service.isDestroyed() === false) {
                    if (me.events)
                        me.events.fire('endredraw');

                    if (!result.url || result.url.indexOf('/empty.gif') !== -1) {
                        result.url = webgis.css.img('empty.gif');
                    }

                    me._currentImageRequestId = result.requestid;
                    me._isLoading = true;

                    var $image = $(me._image);

                    if (me._imageEventsAdded) {
                        //console.log('hide image before loading...');
                        if ($image.css('opacity') > 0 /*&& $image.attr('src').indexOf('/emtpy.gif') < 0*/) {
                            $image
                                .removeClass('effects')
                                .css('opacity', 0).css('display', '')
                                .addClass('effects');
                        }
                    }

                    $image.attr('src', result.url);

                }
            },
            error: function () {
                if (me.events)
                    me.events.fire('endredraw');
            }
        });

        this._image.style.zIndex = 999;
    },

    // Not exists since Leaflet 1.0.0 -> this.on('load',...) on "initialize" does the same
    //_onImageLoad: function () {
    //    this.fire('load');

    //    // Only update the image position after the image has loaded.
    //    // This the old image from visibly shifting before the new image loads.


    //    this._updateImagePosition();
    //}
});

L.imageOverlay.webgis_selection = function (url, selection, options) {
    var l = new L.ImageOverlay.webgis_selection(url, options);
    l._setSelection(selection);
    return l;
};

/*
L.ImageOverlay.webgis_maptips = L.ImageOverlay.extend({

    defaultParams: {
        f: 'json',
        layers: ''
    },
    _me: null,
    _service: null,
    _removed: false,

    initialize: function (url, options) {

        if (webgis)
            webgis.implementEventController(this);

        this._baseUrl = url;
        this._me = this;

        var params = L.Util.extend({}, this.defaultParams);

        if (params.detectRetina && L.Browser.retina) {
            params.width = params.height = this.options.tileSize * 2;
        } else {
            params.width = params.height = this.options.tileSize;
        }

        for (var i in options) {
            // all keys that are not ImageOverlay options go to WMS params
            if (!this.options.hasOwnProperty(i)) {
                params[i] = options[i];
            }
        }

        this.params = params;
        L.Util.setOptions(this, options);
    },

    onAdd: function (map) {
        this._bounds = map.getBounds();
        this._map = map;

        var projectionKey = 'srs';
        this.params[projectionKey] = map.options.crs.code;
        map.on("moveend", this._delayedRedrawSlow, this);

        map.on("zoomstart", function () {
            //$(this._image).fadeOut(900);
        }, this);

        L.ImageOverlay.prototype.onAdd.call(this, map);
    },
});
*/
