L.ImageOverlay.ImageServiceBase = L.ImageOverlay.extend({
    _imageBounds: null,
    _isZooming: false,
    _isLoading: true,
    _imageEventsAdded: false,
    _currentImageRequestId: '',
    _opacity: 1.0,
    _order: -1,

    onAdd: function (map) {
        this._url = '';
        this._bounds = map.getBounds();
        this._map = map;

        this._topLeft = map.getPixelBounds().min;

        var projectionKey = 'srs';
        this.params[projectionKey] = map.options.crs.code;
        this.params["mapname"] = map._webgis5Name;

        map.on("moveend", /*this._delayedRedrawSlow*/this._delayedRedrawFast, this);

        map.on("zoomstart", this._ImageServiceBase_eventhandler_on_map_zoomstart, this);
        map.on("zoomend", this._ImageServiceBase_eventhandler_on_map_zoomend, this);

        L.ImageOverlay.prototype.onAdd.call(this, map);

        if (this._imageEventsAdded === false) {
            this._imageEventsAdded = true;
            var me = this;
            webgis.$(this._image)/*.css('opacity',0)*/.addClass('webgis-service-image effects');
            webgis.$(this._image).on('load', function () {
                //me._onImageLoad();
                
                var $image = webgis.$(me._image);
                me._isLoading = false;

                if (me._service && me._currentImageRequestId !== me._service.currentRequestId()) {
                    if (!me._isEmptyImage()) {
                        me._setEmptyImage();
                    }
                }
                if (!me._isEmptyImage()) {
                    webgis.delayed(function () {
                        me.setOpacity(me._opacity);
                        if (me._order >= 0) {
                            me.setOrder(me._order);
                        }
                        me.setDisplay('');
                    }, 1);
                }
            });
        }

        this._onAdded();
    },

    onRemove: function (map) {
        map.off("moveend", /*this._delayedRedrawSlow*/this._delayedRedrawFast, this)
        map.off("zoomstart", this._ImageServiceBase_eventhandler_on_map_zoomstart, this);
        map.off("zoomend", this._ImageServiceBase_eventhandler_on_map_zoomend, this);

        L.ImageOverlay.prototype.onRemove.call(this, map);
    },

    _ImageServiceBase_eventhandler_on_map_zoomstart: function () {
        this._isZooming = true;
       
        if (this._imageBounds) {
            webgis.$(this._image).css('opacity', 0);
        } else {
            webgis.delayed(function (me) {
                //console.log('fadeout...', $(me._image).hasClass('effects'));
                webgis.$(this._image).css('opacity', 0);
            }, 500, this);
        }
    },
    _ImageServiceBase_eventhandler_on_map_zoomend: function () {
        //console.log('ImageOverlay.map.zoomend');

        this._isZooming = false;
    },

    _onAdded: function () {},
    _isEmptyImage: function () {
        var $image = webgis.$(this._image);
        return ($image.length === 0 || !$image.attr('src') || $image.attr('src').indexOf('/empty.gif') > 0);
    },
    _setEmptyImage: function () {
        webgis.$(this._image)
            .css('display', 'none')
            .attr('src', webgis.css.img('empty.gif'));
    },

    _reset: function () {       
        var image = this._image, bounds, size;
        if (!image) {
            return;
        }

        //console.log(this._isZooming);

        if (this._isWebMercator()) {
            L.ImageOverlay.prototype._reset.call(this);

            if (image && this._imageBounds) {
                bounds = this._imageBounds;
                if (bounds[0] < -20026376.39) {
                    //console.log('unset left');
                    
                    var diffLeft = -20026376.39 - bounds[0];
                    var resolution = this._map.options.crs._scales[this._map._zoom];
                    
                    var pos = L.DomUtil.getPosition(image);
                    pos.x -= diffLeft * resolution;

                    L.DomUtil.setPosition(image, pos);
                    image.style.width = '';
                } 
                else if (bounds[2] > 20026376.39) {
                    //console.log('unset right');
                    image.style.width = '';
                }
            }
        } else {
            //console.log(this._isZooming);
            if (this._isZooming) {
                L.ImageOverlay.prototype._reset.call(this);
                return;
            }
            
            if (this._imageBounds) {
                //console.log('reset with _imageBounds');
                bounds = this._imageBounds;
                var se = this._map.options.crs.unproject(L.point(bounds[2], bounds[1]));
                var nw = this._map.options.crs.unproject(L.point(bounds[0], bounds[3]));
                bounds = new L.Bounds(
                    this._map.latLngToLayerPoint(se),
                    this._map.latLngToLayerPoint(nw));
                size = this._calculateImageSize(); // this._map.getSize();

                //console.log(bounds.min, bounds.max);
                //console.log(this._calculateBounds().min, this._calculateBounds().max);

                L.DomUtil.setPosition(image, bounds.min);

                image.style.width = size.x + 'px';
                image.style.height = size.y + 'px';
            } else {
                //console.log('reset without _imageBounds');
                bounds = this._calculateBounds();
                size = this._calculateImageSize();

                L.DomUtil.setPosition(image, bounds.min);

                image.style.width = size.x + 'px';
                image.style.height = size.y + 'px';

                //L.ImageOverlay.prototype._reset.call(this);
            } 
        }
    },

    _isWebMercator: function () {
        return this._map.options.crs && this._map.options.crs.code.toLowerCase() === "epsg:3857";
    },
    _updateImagePosition: function () {
        
        // The original reset function really just sets the position and size, so rename it for clarity.
        //L.ImageOverlay.prototype._reset.call(this);
        //console.log(this._imageBounds);

        if (this._imageBounds) {
            var bounds = this._imageBounds;

            var sw = this._map.options.crs.unproject(L.point(bounds[0], bounds[1]));
            var ne = this._map.options.crs.unproject(L.point(bounds[2], bounds[3]));

            if (this._isWebMercator()) {
                if (bounds[0] < -20026376.39) {
                    sw.lng = -180.0;
                }
                if (bounds[2] > 20026376.39) {
                    ne.lng = +180.0;
                }
            }

            var latLngBounds = L.latLngBounds(sw, ne);

            this.setBounds(latLngBounds);
            //console.log(sw, ne);
        } else {
            var bounds = this._calculateLatLngBounds();
            this.setBounds(bounds);

            //L.ImageOverlay.prototype._reset.call(this);
        }
    },

    _checkMaxLatLng: function (latLng, isLeft, ignoreLat) {
        var minLng = -179.99, maxLng = -minLng;
        var minLat = -84.999999999999, maxLat = -minLat;

        var modified = false;

        if (latLng.lng < minLng) {
            latLng.lng = minLng;
            modified = true;
        }
        if (latLng.lng > maxLng) {
            latLng.lng = maxLng;
            modified = true;
        }

        if (!ignoreLat) {
            if (latLng.lat < minLat) {
                latLng.lat = minLat;
                modified = true;
            }
            if (latLng.lat > maxLat) {
                latLng.lat = maxLat;
                modified = true;
            }
        }

        if (this._isWebMercator()) {
            // nur mehr für globale Karten checken...

            var pixelBounds = this._map.getPixelBounds();
            if (isLeft) {
                if (pixelBounds.min.x < 0) {
                    latLng.lng = minLng;
                    modified = true;
                }
            }
            else {
                var boundWidth = pixelBounds.max.x - pixelBounds.min.x;
                //var imgWidth = this._map.getSize().x;

                var left = this._map.latLngToContainerPoint(L.latLng(0, minLng)).x;
                var right = this._map.latLngToContainerPoint(L.latLng(0, maxLng)).x;


                if (boundWidth > right) {
                    latLng.lng = maxLng;
                    modified = true;
                }
                //if (pixelBounds.min.x < 0) {
                //    latLng.lng = maxLng;
                //}
            }
        }

        return { latLng: latLng, modified: modified };
    },
    _getPixelBounds: function () {
        return this._map.getPixelBounds();
    },
    _calculateBbox: function () {
        var pixelBounds = this._getPixelBounds();

        var sw_ = this._checkMaxLatLng(this._map.unproject(pixelBounds.getBottomLeft()), true);
        var ne_ = this._checkMaxLatLng(this._map.unproject(pixelBounds.getTopRight()), false);

        var sw = sw_.latLng;
        var ne = ne_.latLng;

        //console.log('bbox', sw, ne);

        var neProjected = this._map.options.crs.project(ne);
        var swProjected = this._map.options.crs.project(sw);

        return [swProjected.x, swProjected.y, neProjected.x, neProjected.y];
    },
    _calculateBounds: function () {
        var pixelBounds = this._getPixelBounds();

        var sw_ = this._checkMaxLatLng(this._map.unproject(pixelBounds.getBottomLeft()), true);
        var ne_ = this._checkMaxLatLng(this._map.unproject(pixelBounds.getTopRight()), false);

        var sw = sw_.latLng;
        var ne = ne_.latLng;

        var bounds = new L.Bounds(
            this._map.latLngToLayerPoint(sw),
            this._map.latLngToLayerPoint(ne));

        return bounds;
    },
    _calculateLatLngBounds: function () {
        var pixelBounds = this._getPixelBounds();

        var sw_ = this._checkMaxLatLng(this._map.unproject(pixelBounds.getBottomLeft()), true);
        var ne_ = this._checkMaxLatLng(this._map.unproject(pixelBounds.getTopRight()), false);

        var sw = sw_.latLng;
        var ne = ne_.latLng;

        var bounds = new L.latLngBounds(sw, ne);

        return bounds;
    },
    _calculateImageSize: function () {
        var bounds = this._getPixelBounds();
        var size = this._map.getSize();

        // Checken, ob die Grenzen außerhalb der gültigen geographischen Grenzen liegt
        // => wichtig für "globale" Karten (Auslandssteierer)
        var sw_ = this._checkMaxLatLng(this._map.unproject(bounds.getBottomLeft()), true, this._isWebMercator());
        var ne_ = this._checkMaxLatLng(this._map.unproject(bounds.getTopRight()), false, this._isWebMercator());

        //console.log('bounds', bounds);
        //console.log('buttom-left', this._map.unproject(bounds.getBottomLeft()));
        //console.log('top-right', this._map.unproject(bounds.getTopRight()));

        var sw = sw_.latLng;
        var ne = ne_.latLng;

        // nur wenn außerhalb => Bildgröße neu berechnen!
        if (sw_.modified || ne_.modified) {
            console.log('elementSize', size);

            var right = this._map.latLngToLayerPoint(ne).x;
            var left = this._map.latLngToLayerPoint(sw).x;

            console.log('left/rignt', [left, right]);

            if (left > 0 || right < size.x) {
                size.x = Math.abs(right - left);
            }

            var top = this._map.latLngToLayerPoint(ne).y;
            var bottom = this._map.latLngToLayerPoint(sw).y;

            console.log('top/bottom', [top, bottom]);

            if (top > 0 || bottom < size.y) {
                size.y = Math.abs(bottom - top);
            }

            console.log('imageSize', size);
        }

        return size;
    },
    _calculateTimeEpoch: function () {
        const map = this._service.map;
        return map.getTimeEpoch();
    }

});

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
                                    .removeClass('effects')
                                    .css('opacity', 0)//.css('display', '')
                                    .addClass('effects');

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
                                    .removeClass('effects')
                                    .css('opacity', 0).css('display', '')
                                    .addClass('effects');
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

L.ImageOverlay.webgis_static_service = L.ImageOverlay.webgis_service.extend({

    //_affinePoints: [
    //    { lng: 13.842998, lat: 48.771905 },
    //    { lng: 16.760369, lat: 49.771905 },
    //    { lng: 12.842998, lat: 46.958502 }
    //],
    _resizeMarker: null,

    initialize: function (url, options) {
        L.ImageOverlay.webgis_service.prototype.initialize.call(this, url, options);
    },
    _onAdded: function () {
        this.on('click', this._webgis_static_service_eventhandler_on_click, this);
        this._map.on('zoomstart', this._webgis_static_service_eventhandler_on_map_zoomstart, this);
        this._map.on('zoomend resetview', this._webgis_static_service_eventhandler_on_map_zoomend, this);
        this._map.on('zoomend moveend', this._webgis_static_service_eventhandler_on_map_moveend, this);
    },

    onRemove: function (map) {
        //console.log('webgis_static_service.onRemove');

        this.hidePassPoints();

        this.off('click', this._webgis_static_service_eventhandler_on_click, this);
        this._map.off('zoomstart', this._webgis_static_service_eventhandler_on_map_zoomstart, this);
        this._map.off('zoomend resetview', this._webgis_static_service_eventhandler_on_map_zoomend, this);
        this._map.off('zoomend moveend', this._webgis_static_service_eventhandler_on_map_moveend, this);

        if (this._resizeMarker) {
            map.removeLayer(this._resizeMarker);
            this._resizeMarker = null;
        }

        L.ImageOverlay.webgis_service.prototype.onRemove.call(this, map);
    },

    _webgis_static_service_eventhandler_on_click: function (e) {
        if (this.options.passPointsVisible === true) {
            var vector = this._calcVector({ x: e.layerPoint.x, y: e.layerPoint.y });
            this._addPassPoint(vector, e.latlng, true);
        }
    },
    _webgis_static_service_eventhandler_on_map_zoomstart: function() {
        webgis.$(this._image).css('opacity', 0);
    },
    _webgis_static_service_eventhandler_on_map_zoomend: function () {
        this._updateImagePosition()
    },
    _webgis_static_service_eventhandler_on_map_moveend: function () {
        this._refreshPreviewPasspoints();
    },

    _refreshPreviewPasspoints: function () {
        if (this.options.affinePoints[0] == null) {
            for (var i in this.options.passPoints) {
                var passPoint = this.options.passPoints[i];
                var latLng = this._calcLatLngFromVector(passPoint.vector);
                passPoint.setLatLng(0, latLng);
                if (!passPoint.isActive()) {
                    passPoint.setLatLng(1, latLng);
                }
            }
        }
    },

    reposition: function (topleft, topright, bottomleft) {
        //console.log('reposition', topleft, topright, bottomleft);
        this.options.affinePoints = [topleft, topright, bottomleft];
        this._updateImagePosition();
        //this._reset();

        for (var i in this.options.passPoints) {
            var passPoint = this.options.passPoints[i];
            var latLng = this._calcLatLngFromVector(passPoint.vector);
            passPoint.setLatLng(0, latLng);
        }
    },

    _getAffinePoints: function () {
        if (this.options.affinePoints && this.options.affinePoints.length === 3) {
            var points = [this.options.affinePoints[0],
                          this.options.affinePoints[1],
                          this.options.affinePoints[2]];

            if (points[0] == null || points[1] == null || points[2] == null) {
                var pixelBounds = this._getPixelBounds();

                var sw_ = this._checkMaxLatLng(this._map.unproject(pixelBounds.getBottomLeft()), true);
                var ne_ = this._checkMaxLatLng(this._map.unproject(pixelBounds.getTopRight()), false);

                var sw = sw_.latLng;
                var ne = ne_.latLng;

                var width = ne.lng - sw.lng,
                    height = ne.lat - sw.lat;

                var center = this._map.getCenter(), ratio = this.options.widthHeightRatio;

                if (ratio > 0) {
                    if (ratio > 1.0) {
                        if (height < width / ratio) {
                            width = height * ratio;
                        } else {
                            height = width / ratio;
                        }
                    } else {
                        if (width < height * ratio) {
                            height = width / ratio;
                        } else {
                            width = height * ratio;
                        }
                    }
                }

                // Variante: FullScreen
                //if (points[0] == null) {
                //    points[0] = {
                //        lng: center.lng - (width * 0.45),
                //        lat: center.lat + (height * 0.45)
                //    };
                //}
                //if (points[1] == null) {
                //    points[1] = {
                //        lat: points[0].lat
                //    };
                //}
                //if (points[2] == null) {
                //    points[2] = {
                //        lng: points[0].lng,
                //        lat: center.lat - (height * 0.45)
                //    };
                //}

                // Variante: Buttom Right
                var bottomRight = {
                    lng: ne.lng,
                    lat: sw.lat
                };

                if (points[0] == null) {
                    points[0] = {
                        lng: bottomRight.lng - (width * (this.options.previewSize + 0.01)),
                        lat: center.lat + (height * this.options.previewSize / 2.0)
                    };
                }
                if (points[1] == null) {
                    points[1] = {
                        lng: bottomRight.lng - (width * 0.01),
                        lat: points[0].lat
                    };
                }
                if (points[2] == null) {
                    points[2] = {
                        lng: points[0].lng,
                        lat: center.lat - (height * this.options.previewSize / 2.0)
                    };
                }

                console.log('ratio', ratio, width, height);

                //console.log('points', points);
                //console.log('bottomRight', bottomRight);
                //console.log('width', width);
                //console.log('height', height);
                //console.log('ratio', this.options.widthHeightRatio);
            }

            return points;
        }
        return null;
    },
    _hasRegularAffinePoints: function () {
        if (this.options.affinePoints && this.options.affinePoints.length === 3) {
            var points = [this.options.affinePoints[0],
            this.options.affinePoints[1],
            this.options.affinePoints[2]];

            return (points[0] != null && points[1] != null && points[2] != null);
        }

        return false;
    },
    _calcTransformation: function () {
        var points = this._getAffinePoints();
        if (points && this._map) {
            //console.log('_calcTransformation', this._map, points);
            // Project control points to container-pixel coordinates
            var pxTopLeft = this._map.latLngToLayerPoint(points[0]);  // topLeft
            var pxTopRight = this._map.latLngToLayerPoint(points[1]);  // topRight
            var pxBottomLeft = this._map.latLngToLayerPoint(points[2]);  // bottomLeft

            // Infer coordinate of bottom right
            var pxBottomRight = pxTopRight.subtract(pxTopLeft).add(pxBottomLeft);

            // pxBounds is mostly for positioning the <div> container
            var pxBounds = L.bounds([pxTopLeft, pxTopRight, pxBottomLeft, pxBottomRight]);
            var size = pxBounds.getSize();

            this._image.style.width = size.x + 'px';
            this._image.style.height = size.y + 'px';

            var imgW = this._image.width;
            var imgH = this._image.height;
            if (!imgW || !imgH) {
                //console.log('Probably because the image hasnt loaded yet.');
                return null;
            }

            // Sides of the control-point box, in pixels
            // These are the main ingredient for the transformation matrix.
            var vectorX = pxTopRight.subtract(pxTopLeft);
            var vectorY = pxBottomLeft.subtract(pxTopLeft);

            return {
                matrix: [
                    [(vectorX.x / imgW), (vectorX.y / imgW)],
                    [(vectorY.x / imgH), (vectorY.y / imgH)]
                ],
                origin: pxTopLeft,
                size: size,
                bounds: pxBounds,
                points: points
            };
        }

        return null;
    },
    _calcVector: function (layerPoint) {
        var transformation = this._calcTransformation();
        if (transformation) {
            layerPoint.x -= transformation.origin.x;
            layerPoint.y -= transformation.origin.y;

            var Det = transformation.matrix[0][0] * transformation.matrix[1][1] - transformation.matrix[0][1] * transformation.matrix[1][0];
            var invMatrix = [
                [transformation.matrix[1][1] / Det, -transformation.matrix[0][1] / Det],
                [-transformation.matrix[1][0] / Det, transformation.matrix[0][0] / Det]
            ];

            var transformtedLayerPoint = {
                x: invMatrix[0][0] * layerPoint.x + invMatrix[1][0] * layerPoint.y,
                y: invMatrix[0][1] * layerPoint.x + invMatrix[1][1] * layerPoint.y,
            };
            layerPoint = transformtedLayerPoint;

            layerPoint.x /= transformation.size.x;
            layerPoint.y /= transformation.size.y;
        }

        return layerPoint;
    },
    _calcLatLngFromVector: function (vector) {
        var transformation = this._calcTransformation();
        if (transformation) {
            var affinePoints = this._getAffinePoints();
            var topLeft = this._map.options.crs.project(affinePoints[0]);
            var topRight = this._map.options.crs.project(affinePoints[1]);
            var bottomLeft = this._map.options.crs.project(affinePoints[2]);

            //console.log('topLeft', topLeft);
            //console.log('topRight', topRight);
            //console.log('bottomLeft', bottomLeft);

            var dx = [topRight.x - topLeft.x, topRight.y - topLeft.y];
            var dy = [bottomLeft.x - topLeft.x, bottomLeft.y - topLeft.y];

            var pos = {
                x: topLeft.x + dx[0] * vector.x + dy[0] * vector.y,
                y: topLeft.y + dx[1] * vector.x + dy[1] * vector.y
            };

            var latLng = this._map.options.crs.unproject(pos);

            return latLng;
        }

        return null;
    },
    _addPassPoint: function (vec, latLng, suppressEvent) {
        var transformation = this._calcTransformation();
        if (transformation) {

            var vecPixelPos = {
                x: transformation.origin.x + transformation.matrix[0][0] * transformation.size.x * vec.x + transformation.matrix[1][0] * transformation.size.y * vec.y,
                y: transformation.origin.y + transformation.matrix[0][1] * transformation.size.x * vec.x + transformation.matrix[1][1] * transformation.size.y * vec.y,
            };

            var vecLatLng = this._map.layerPointToLatLng(vecPixelPos);
            //console.log('vecPixelPos', vecPixelPos, vecLatLng);

            var passPoint = L.passPoint([vecLatLng, { lat: latLng.lat, lng: latLng.lng }])
                .on('marker1-changed', function (e) {
                    var passPoint = e.target;

                    var layerPoint = this._map.latLngToLayerPoint(passPoint.getLatLngs()[0]);
                    passPoint.vector = this._calcVector(layerPoint);

                    //console.log('marker1-changed vector', passPoint.vector);
                    passPoint.setActive(true);
                    this.fireEvent('passpoints-changed');
                }, this)
                .on('marker2-changed', function (e) {
                    var passPoint = e.target;

                    passPoint.setActive(true);
                    this.fireEvent('passpoints-changed');
                }, this);

            passPoint.service_layer = this;
            passPoint.vector = vec;

            if (this.options.passPointsVisible) {
                passPoint.addTo(this._map)
            }

            passPoint.on('passpoint-removed', function (e) {
                console.log('passpoint-removed', e);
                var passPoint = e.target;

                var remainPassPoints = [];
                for (var p in this.options.passPoints) {
                    var pp = this.options.passPoints[p];
                    if (pp != passPoint) {
                        remainPassPoints.push(pp);
                    }
                }
                this.options.passPoints = remainPassPoints;

                if (this.options.passPointsVisible) {
                    // redraw passpoints
                    this.hidePassPoints();
                    this.showPassPoints();
                }
                this.fireEvent('passpoints-changed');
            }, this);

            this.options.passPoints.push(passPoint);
            if (suppressEvent !== true) {
                this.fireEvent('passpoints-changed');
            }

            return passPoint;
        }

        return null;
    },

    showPassPoints: function () {
        if (this.options.passPointsVisible == true)
            return;

        this.options.passPointsVisible = true;
        for (var p in this.options.passPoints) {
            this.options.passPoints[p].addTo(this._map);
        }
    },
    hidePassPoints: function () {
        if (this.options.passPointsVisible === false)
            return;

        this.options.passPointsVisible = false;
        for (var p in this.options.passPoints) {
            this._map.removeLayer(this.options.passPoints[p]);
        }
    },
    setPassPoints: function (passPoints) {
        var passPointsVisible = this.options.passPointsVisible;

        if (passPointsVisible)
            this.hidePassPoints();

        this.options.passPoints = [];

        if (passPoints && passPoints.length > 0) {
            for (var p in passPoints) {
                var passPoint = passPoints[p];
                if (passPoint.vec && passPoint.latLng) {
                    var pp = this._addPassPoint(passPoint.vec, passPoint.latLng, true);
                    if (pp) {
                        pp.setActive(true);
                    }
                }
            }

            if (passPointsVisible) {
                this.showPassPoints();
            }
        }
    },
    getGeoRefDefintion: function () {
        var result = {};

        var passPoints = [];
        for (var i in this.options.passPoints) {
            var passPoint = this.options.passPoints[i];
            if (passPoint.isActive()) {
                passPoints.push({
                    vector: passPoint.vector,
                    pos: passPoint.getLatLngs()[1]
                });
            }
        }
        result.passPoints = passPoints;

        var points = this._getAffinePoints();
        if (points) {
            result.topLeft = points[0];
            result.topRight = points[1];
            result.bottomLeft = points[2];
        }

        result.size = [webgis.$(this._image).width(), webgis.$(this._image).height()];

        return result;
    },

    isReady: function () {
        return this._calcTransformation() !== null;
    },
    onLoaded: function (callback) {
       var count = 0, me = this;

        var func = function () {
            webgis.delayed(function () {
                count++;
                var ready = me.isReady();

                //console.log('isReady', ready);

                if (ready || count > 40) {  // max 40*250 = 10.000 ms
                    callback();
                } else {
                    func();
                }
            }, 250);
        }

        // hier immer func() aufrufen und nicht vorher abfragen, ob bereits ready
        // Sonst werden teilweise Passpunte verschoben dargestellt, weil Bild nich ganz an der richtigen Position ist...
        func();
    },

    _updateImagePosition: function () {

        // The original reset function really just sets the position and size, so rename it for clarity.
        //L.ImageOverlay.prototype._reset.call(this);
        if (!this._map)
            return;

        var transformation = this._calcTransformation();
        console.log('updateImagePosition', transformation);
        if (transformation) {
            // LatLngBounds are needed for (zoom) animations
            this._bounds = L.latLngBounds(
                this._map.layerPointToLatLng(transformation.bounds.min),
                this._map.layerPointToLatLng(transformation.bounds.max));

            //console.log('transformation', transformation);
            L.DomUtil.setPosition(this._image, transformation.bounds.min);

            this._image.style.transformOrigin = '0 0';

            this._image.style.transform = "matrix(" +
                transformation.matrix[0][0] + ', ' + transformation.matrix[0][1] + ', ' +
                transformation.matrix[1][0] + ', ' + transformation.matrix[1][1] + ', ' +
                transformation.origin.x + ', ' + transformation.origin.y + ')';

            if (!this._hasRegularAffinePoints()) {
                if (this._resizeMarker) {
                    this._resizeMarker.setLatLng(L.latLng(transformation.points[0].lat, transformation.points[0].lng));
                } else {
                    this._resizeMarker = L.marker(L.latLng(transformation.points[0].lat, transformation.points[0].lng), {
                        icon: L.icon({
                            iconUrl: webgis.css.imgResource('resize-all_26@1.png', 'markers'),
                            iconSize: [26, 26],
                            iconAnchor: [0, 0],
                            popupAnchor: [0, 0]
                        })
                    });
                    this._resizeMarker._static_service = this;
                    this._resizeMarker.addTo(this._map);
                    this._resizeMarker.on('click', function () {
                        var options = this._static_service.options;
                        if (options.previewSize > 0.25) {
                            options.previewSize = 0.25;
                        }
                        else {
                            options.previewSize = 0.75;
                        }

                        console.log(options);
                        this._static_service._updateImagePosition();
                        this._static_service._refreshPreviewPasspoints();
                    });
                }
            } else {
                if (this._resizeMarker) {
                    this._map.removeLayer(this._resizeMarker);
                    this._resizeMarker = null;
                }
            }

        } else {
            var pixelBounds = this._getPixelBounds();

            var sw_ = this._checkMaxLatLng(this._map.unproject(pixelBounds.getBottomLeft()), true);
            var ne_ = this._checkMaxLatLng(this._map.unproject(pixelBounds.getTopRight()), false);

            var sw = sw_.latLng;
            var ne = ne_.latLng;

            var ratio = this.options.widthHeightRatio;

            var width = ne.lat - sw.lat,
                height = ne.lng - sw.lng;

            var bounds = new L.latLngBounds(
                { lng: sw.lng + height * 0.05, lat: sw.lat + width * 0.05, },
                { lng: ne.lng - height * 0.05, lat: ne.lat - width * 0.05, });

            this.setBounds(bounds);
        }
    },

    _updateUrl: function () {
        if (this._service && this._service.isDestroyed() === true)
            return;

        //var map = this._map,
        //    bounds = this._bounds,
        //    zoom = map.getZoom(),
        //    crs = map.options.crs;

        //var bbox = this._calculateBbox();
        //var mapSize = this._calculateImageSize();

        if (this._service != null &&
            this._service.layers &&
            this._service.layers.length === 1 &&
            this._service.layers[0].visible !== true) {
            this._url = webgis.css.img('empty.gif');
            return;
        }

        url = this._baseUrl;

        this._url = url;
    },

    _redraw: function () {
        if (this._removed === true || (this._service && this._service.isDestroyed() === true))
            return;

        this._currentImageRequestId = this._service.currentRequestId();

        this._updateUrl();

        webgis.$(this._image).attr('src', this._url);
    }
});

L.imageOverlay.webgis_static_service = function (id, service, options) {
    options = options || {};
    options.interactive = true;
    options.overlay_url = service.serviceInfo.overlay_url;
    options.widthHeightRatio = service.serviceInfo.widthHeightRatio || 1.0;
    options.previewSize = 0.25;

    options.affinePoints = options.affinePoints;
    if (!options.affinePoints || options.affinePoints.length !== 3)
        options.affinePoints = [null, null, null];

    options.passPoints = options.passPoints || [];
    options.passPointsVisible = options.passPointsVisible ? true : false;

    //console.log(options);

    var l = new L.ImageOverlay.webgis_static_service(id, options);

    l._setService(service);

    
    return l;
}; 