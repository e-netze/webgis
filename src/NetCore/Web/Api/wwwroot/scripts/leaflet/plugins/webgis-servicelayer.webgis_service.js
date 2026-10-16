L.ImageOverlay.webgis_service = L.ImageOverlay.ImageServiceBase.extend({

    defaultParams: {
        f: 'json',
        layers:''
    },
    _me: null,
    _service: null,
    _removed: false,

    initialize: function (url, options) {

        if (webgis)
            webgis.implementEventController(this);

        this._baseUrl = url;
        this._me=this;

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
            //console.log('load and update position', webgis.$(this._image).attr('src'));
            this._updateImagePosition();
        }, this);
    },

    onRemove: function (map) {
        console.log('webgis_service.onRemove');
        L.ImageOverlay.ImageServiceBase.prototype.onRemove.call(this, map);
    },

    setOrder: function (order) {
        this._order = order;
        if (this._image) {
            //console.log('setOrder', this.id + ": " + order, this.frameworkElement.setOrder);
            this._image.style.zIndex = order;
        }
    },
    setOpacity: function (opacity) {
        this._opacity = opacity;
        try {
            if (this._image) {
                this._image.style.opacity = opacity;
            }
        } catch(e){}
    },
    setDisplay: function (display) {
        try {
            if (this._image) {
                if (this._image.style.display !== display) {
                    console.log('webgis_service: setDisplay from ' + this._image.style.display + ' to ' + display);
                }
                this._image.style.display = display;
            }
        } catch (e) {}
    },

    _setService:function(service){
        this._service = service;
        if (service.serviceInfo.type === "static_overlay") {
            this._baseUrl = service.serviceInfo.overlay_url;
        }
        this._service.events.on('onchangevisibility', this._delayedRedrawFast, this);
        this._service.events.on('onchangevisibility-delayed', this._delayedRedrawSlow, this);
        this._service.events.on('refresh', this._delayedRedrawFast, this);
        this._service.events.on('remove', function (e) {
            this._map.removeLayer(this);
            this._removed = true;
        }, this);

        // ToDo: braucht man das noch?
        this._service.events.on('requestidchanged', function (channel, sender) {
            if (this._currentImageRequestId !== sender.currentRequestId() && this._isLoading === true && this._image) {
                //console.log("set empty gif...");
                L.Util.extend(this._image, {
                    src: webgis.css.img('empty.gif')
                });
            }
        }, this);
    },
    _postParameters: {},
    _updateUrl: function () {
        if (this._service && this._service.isDestroyed() === true)
            return;

        var map = this._map,
            bounds = this._bounds,
            zoom = map.getZoom(),
            crs = map.options.crs;

        //mapSize = { x: webgis.$(map.elem).width(), y: webgis.$(map.elem).height() },
        //nw = map.layerPointToLatLng(L.point(0, 0)),
        //se = map.layerPointToLatLng(L.point(mapSize.x, mapSize.y),

        //topLeft = map.latLngToLayerPoint(bounds.getNorthWest()),
        //mapSize = map.latLngToLayerPoint(bounds.getSouthEast()).subtract(topLeft),

        //nw = crs.project(bounds.getNorthWest()),
        //se = crs.project(bounds.getSouthEast()),

        //bbox = [nw.x, se.y, se.x, nw.y].join(',');

        var bbox = this._calculateBbox();
        var mapSize = this._calculateImageSize();
        var timeEpoch = this._calculateTimeEpoch();

        //var relBbox = (bbox[2] - bbox[0]) / (bbox[3] - bbox[1]);
        //var relMapSize = mapSize.x / mapSize.y;

        var vislayers = [];
        if (this._service != null) {
            for (var i = 0; i < this._service.layers.length; i++) {
                var layer = this._service.layers[i];
                if (layer.visible === true) {
                    vislayers.push(layer.id);
                }
            }

            this._service.addCustomRequestParameters(this.params);
        }

        
        if (vislayers.length === 0 && !this._service.serviceInfo.has_visible_locked_layers) {
            this._url = webgis.css.img('empty.gif');
           
            if (this.events) {
                //console.log("vislayers.length", vislayers.length, this._service.name);
                this.events.fire('endredraw');
            }
            return;
        }

        this.params.layers = vislayers.join(',');
        var crsId = 0;
        if (this._service && this._service.map && this._service.map.crs)
            crsId = this._service.map.crs.id;

        urlParams = { width: mapSize.x, height: mapSize.y, bbox: bbox, crs: crsId, time_epoch: timeEpoch };
        if (this._service && this._service.map) {
            this._service.map.appendRequestParameters(urlParams, this._service.id);
        }
        webgis.hmac.appendHMACData(urlParams);

        this._postParameters = L.Util.extend({}, {}, this.params);
        if (urlParams.renderers) {  // user renderers can get long => POST
            this._postParameters.renderers = urlParams.renderers;
            delete urlParams.renderers;
        }
        url = this._baseUrl + L.Util.getParamString(L.Util.extend({}, /*this.params*/{}, urlParams));
        url += '&requestid=' + this._service.currentRequestId();

        this._url = url;
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

        if (me._service.hasInitialError() === true) {
            console.log('hasInitialError');

            me.setDisplay('none');
            webgis.$(me._image)
                .attr('src', webgis.css.img('empty.gif'));

            me._service.events.fire('onerror', me._service, {
                requestid: me._service.currentRequestId(),
                requestcommand: "GetMap",
                service: me._service,
                success: false,
                exception: me._service.getInitialError()
            });

            return;
        }

        if (this._url === webgis.css.img('empty.gif')) {
            me.setDisplay('none');
            webgis.$(me._image)
                .attr('src', this._url);
        } else {
            if (me.events)
                me.events.fire('beginredraw');

            //console.log('postParameters', this._postParameters);

            webgis.ajax({
                url: this._url,
                type: 'post',
                data: this._postParameters,
                success: function (result) {
                    if (result.requestid && result.requestid !== me._service.currentRequestId()) {
                        //console.log('success: ' + ((result.success === false) ? false : true) + ' request id abgelaufen...' + result.requestid);
                        return;
                    }

                    //console.log('setimage', result.requestid);

                    if (result.exception) {
                        result.requestid = result.requestid || me._service.currentRequestId();
                        result.requestcommand = "GetMap";
                        me._service.events.fire('onerror', me._service, result);

                        if (result.success === false) {  
                            // there can be error messages with successfull request, eg. ImageSize too large => a smaller image will received, in a lower quality
                            // than the exception is only a warning
                            // if not successfull => set empty image
                            result.url = webgis.css.img('empty.gif');
                        }
                    }

                    if (me._service && me._service.isDestroyed() === false) {
                        if (me.events)
                            me.events.fire('endredraw');

                        var display = '';

                        if (!result.url || result.url.indexOf('/empty.gif') !== -1) {
                            result.url = webgis.css.img('empty.gif');
                            display = 'none';
                        }

                        me._currentImageRequestId = result.requestid;
                        me._isLoading = true;
                        if (me._imageEventsAdded) {
                            //console.log('hide image before loading...');
                            var $image = webgis.$(me._image);
                            if ($image.css('opacity') > 0 && $image.attr('src').indexOf('/emtpy.gif') < 0) {
                                $image
                                    .removeClass(me._effects_class)
                                    .css('opacity', 0)//.css('display', '')
                                    .addClass(me._effects_class);

                                display = '';
                            }
                        }

                        me._imageBounds = result.extent;

                        //console.log(result);

                        me.setDisplay(display);
                        $image.attr('src', result.url);
                    }
                },
                error: function () {
                    if (me.events)
                        me.events.fire('endredraw');
                }
            });
        }
    },

    getContainer: function () {
        return webgis.$(this._image).parent().get(0);
    }
});

L.imageOverlay.webgis_service = function (url, service, options) {
    var l = new L.ImageOverlay.webgis_service(url, options);
    l._setService(service);
    return l;
};
