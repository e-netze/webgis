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
