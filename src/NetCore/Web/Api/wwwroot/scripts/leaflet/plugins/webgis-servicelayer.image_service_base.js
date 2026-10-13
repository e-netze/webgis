L.ImageOverlay.ImageServiceBase = L.ImageOverlay.extend({
    _imageBounds: null,
    _isZooming: false,
    _isLoading: true,
    _imageEventsAdded: false,
    _currentImageRequestId: '',
    _opacity: 1.0,
    _order: -1,
    _effects_class: "effects",   // show zoom fadeout effects. Can cause problems on iPhones (browser-tab shuts down!)

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

            if (webgis.isSafari() || webgis.is_iOS) {
                me._effects_class = "no-effects";
            }

            webgis.$(this._image).addClass('webgis-service-image ' + me._effects_class);
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
            //console.log('overlay has image bounds', this._service?.name, this._imageBounds);
            webgis.$(this._image).css('opacity', 0);
        } else {
            webgis.delayed(function (me) {
                //console.log('fadeout...', me._service?.name, $(me._image).hasClass('effects'));
                webgis.$(me._image).css('opacity', 0);
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
        return this._service.getTimeEpoch();
    }

});
