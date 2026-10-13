L.ImageOverlay.webgis_collection = L.ImageOverlay.ImageServiceBase.extend({

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


    setOrder: function (order) {
        if (this._image) {
            this._image.style.zIndex = order;
        }
    },
    setOpacity: function (opacity) {
        if (this._image)
            this._image.style.opacity = opacity;
    },
    setDisplay: function (display) {
        try {
            if (this._image) {
                if (this._image.style.display !== display) {
                    console.log('webgis_collection: setDisplay from ' + this._image.style.display + ' to ' + display);
                }
                this._image.style.display = display;
            }
        } catch (e) { }
    },
    _setService: function (service) {
        this._service = service;
        this._service.events.on('onchangevisibility', this._delayedRedrawFast, this);
        this._service.events.on('onchangevisibility-delayed', this._delayedRedrawSlow, this);
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
        //webgis.$(me._image).fadeOut(ms);

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
                if (result.requestid && result.requestid != me._service.currentRequestId()) {
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

        webgis.$(this._image).on('load', function () {
            webgis.$(this).css('display', 'none').fadeIn(500, function () {

            })
        });
        */
    },
});

L.imageOverlay.webgis_collection = function (url, service, options) {
    var l = new L.ImageOverlay.webgis_collection(url, options);
    l._setService(service);
    return l;
};
