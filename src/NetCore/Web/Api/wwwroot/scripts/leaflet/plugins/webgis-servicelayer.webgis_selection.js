L.ImageOverlay.webgis_selection = L.ImageOverlay.ImageServiceBase.extend({
    defaultParams: {
        f: 'json',
        layer: '',
        fids: ''
    },
    _me: null,
    _service: null,
    _name:'',
    _removed: false,
    _currentImageRequestId: '',
    _isLoading: true,
    _imageEventsAdded: false,
    _zIndex: 999,

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

    setTargetLayer:function(service,layerid,fids) {
        this._service = service;
        this.params.layer = layerid;
        this.params.fids = fids;
        this._removed = false;
    },
    setTargetQuery:function(service,queryId,fids) {
        this._service = service;
        this.params.query = queryId;
        this.params.fids = fids;
        this._removed = false;
    },
    setTargetCustomId: function (service, customId) {
        this._service = service;
        this.params.customid = customId;
        this._removed = false;

        //console.log('setTargetCustomId', this.params, this._service);
    },
    setDisplay: function (display) {
        try {
            if (this._image) {
                if (this._image.style.display !== display) {
                    console.log('webgis_selection: setDisplay from ' + this._image.style.display + ' to ' + display);
                }
                this._image.style.display = display;
            }
        } catch (e) { }
    },
    _setSelection: function (selection) {
        this._name = selection.name;

        var order = 0;
        switch (this._name) {
            case 'query':
                order = 1;
                break;
            default:
                order = 0;
                break;
        }

        this._zIndex += order;

        //this._service = selection.service;
        //if (this._service) {
            selection.events.on('refresh', this._delayedRedrawFast, this);
            selection.events.on('remove', function (e) {
                //console.log('remove');
                //this._map.removeLayer(this);
                this._image.src = webgis.css.img('empty.gif');
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
            crs = map.options.crs;

        //topLeft = map.latLngToLayerPoint(bounds.getNorthWest()),
        //mapSize = map.latLngToLayerPoint(bounds.getSouthEast()).subtract(topLeft),

        //nw = crs.project(bounds.getNorthWest()),
        //se = crs.project(bounds.getSouthEast()),

        //bbox = [nw.x, se.y, se.x, nw.y].join(',');

        var bbox = this._calculateBbox();
        var mapSize = this._calculateImageSize();
        var timeEpoch = this._calculateTimeEpoch();

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

        urlParams = { width: mapSize.x, height: mapSize.y, bbox: bbox, crs: crsId, time_epoch: timeEpoch, selection: this._name };
        if (this._service && this._service.map) {
            this._service.map.appendRequestParameters(urlParams, this._service.id);
        }
        webgis.hmac.appendHMACData(urlParams);

        url = this._baseUrl + this._service.id + '/getselection' + L.Util.getParamString(/*L.Util.extend({}, this.params, urlParams)*/urlParams);
        url += '&requestid=' + this._service.currentRequestId();

        this._url = url;
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
        
        if (this._url === webgis.css.img('empty.gif')) {
            $image.attr('src', this._url);
        } else {
            if (me.events)
                me.events.fire('beginredraw');

            webgis.ajax({
                url: this._url,
                type: 'post',
                data: this.params,
                success: function (result) {
                    if (result.requestid && result.requestid != me._service.currentRequestId()) {
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

                        var $image = webgis.$(me._image);

                        if (me._imageEventsAdded) {
                            //console.log('hide image before loading...');
                            if ($image.css('opacity') > 0 /*&& $image.attr('src').indexOf('/emtpy.gif') < 0*/) {
                                $image
                                    .removeClass(me._effects_class)
                                    .css('opacity', 0).css('display', '')
                                    .addClass(me._effects_class);
                            }
                        }

                        me._imageBounds = result.extent;
                        //console.log('selection extent', me._imageBounds);

                        $image.attr('src', result.url);

                    }
                },
                error: function () {
                    if (me.events)
                        me.events.fire('endredraw');
                }
            });
        }

        this._image.style.zIndex = this._zIndex;
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
