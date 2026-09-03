webgis.map._eventHandlers = function (map) {
    var $ = webgis.$;
    var _map = map;
    return {
        createMouseEvent: function (e) {
            var latLng = webgis.mapFramework == "leaflet" ? _map.frameworkElement.containerPointToLatLng(L.point([e.clientX, e.clientY])) : {};
            var layerPoint = webgis.mapFramework == "leaflet" ? _map.frameworkElement.latLngToLayerPoint(latLng) : { x: e.clientX, y: e.clientY };
            return {
                containerPoint: {
                    x: e.clientX, y: e.clientY
                },
                layerPoint: layerPoint,
                originalEvent: e,
                latlng: latLng
            };
        },
        click: function (e) {
            if (!e.force
                && (!_map.ifLastZoomEventMilliSec(1000) || !_map._ifLastBBoxEventMilliSec(500))) {
                return;
            }
            var clickMap = this;
            //console.log('click-handler');
            var clickFunc = function (clickMap, event) {
                var e = event || e;
                if (webgis.tools) {
                    webgis.tools.onMapClick(clickMap, e);
                }

                var activeTool = clickMap.getActiveTool();
                if (activeTool && activeTool.tooltype === 'click' && webgis.usability.clickBubble === false && webgis.isTouchDevice()) {
                    clickMap.showDraggableClickMarker(e.latlng.lat, e.latlng.lng);
                } else {
                    clickMap.hideDraggableClickMarker();
                }
                if (activeTool && clickMap.sketch && clickMap.isSketchTool(activeTool) && clickMap.sketch.isReadOnly() !== true) {
                    if (webgis.currentPosition.useWithSketchTool === true && webgis.continuousPosition.isWatching() === true) {
                        // Warning? 
                    }
                    else {
                        clickMap.sketch.addVertexCoords(e.latlng.lng, e.latlng.lat, true);
                    }
                }
                if (activeTool && activeTool.tooltype === 'circlemarker') {
                    clickMap.showCircleMarker([e.latlng.lng, e.latlng.lat], null);
                }
                if (activeTool && activeTool.tooltype === 'graphics') {
                    clickMap.graphics.onMapClick(e);
                }
            };
            var defaultClickFunc = function (event) {
                if (webgis.isTouchDevice() && !clickMap.ui.useClickBubble()) {
                    event.clickMap.ui.showQuickInfo({
                        pos: {
                            left: event.clickEvent.containerPoint.x,
                            top: event.clickEvent.containerPoint.y
                        },
                        content: clickMap.getActiveTool().name,
                        clickdata: event,
                        click: function (event) {
                            clickFunc(event.clickMap, event.clickEvent);
                        }
                    });
                }
                else {
                    clickFunc(event.clickMap, event.clickEvent);
                }
            };
            if (clickMap.getActiveTool() && clickMap.getActiveTool().id.indexOf(webgis._defaultToolPrefix) === 0) {
                // hier sollte der Doppelklick immer noch funktionieren, also erst verzögert starten
                webgis.delayedToggle('defaulttool-click', defaultClickFunc, 500, { clickMap: clickMap, clickEvent: e });
            }
            else {
                clickFunc(clickMap, e);
            }
        },
        move: function (e) {

            if (_map._currentCoordsContainer !== null && e.latlng) {
                var lat = e.latlng.lat, lng = e.latlng.lng;

                var coords = [0, 0], webgisCalcCrsId = webgis.calc.getCalcCrsId([{ x: lng, y: lat }]);
                if (webgisCalcCrsId > 0) {
                    $(_map._currentCoordsContainer).find('.epsg').html(webgisCalcCrsId);
                    var pCoord = webgis.calc.project(lng, lat);
                    coords = [Math.round(pCoord.X * 100) / 100, Math.round(pCoord.Y * 100) / 100];
                }
                else if (_map.crs) {
                    var pCoord = webgis.fromWGS84(_map.crs, lat, lng);
                    coords = [Math.round(pCoord.x * 100) / 100, Math.round(pCoord.y * 100) / 100];
                }
                else {
                    coords = [Math.round(lat * 100) / 100, Math.round(lng * 100) / 100];
                }
                $(_map._currentCoordsContainer).find('.x').html(coords[0]);
                $(_map._currentCoordsContainer).find('.y').html(coords[1]);
            }

            if (e.latlng) {
                _map._currentCursorPosition.lat = e.latlng.lat;
                _map._currentCursorPosition.lng = e.latlng.lng;
            }

            if (webgis.tools) {
                // Sketch/tool mousemove handling (snapping, tracing, sketch-info overlay, ...) can be
                // relatively expensive (eg. topology snapping on large networks). Native mousemove events
                // can fire faster than the browser can render/process them; if every single event is
                // handled synchronously, a burst of queued events can pile up and process one after another,
                // blocking the UI thread and causing the map/UI to flicker or become unresponsive until the
                // backlog is cleared. Coalesce bursts to at most one (the latest) update per animation frame.
                _map._pendingMouseMoveEvent = e;
                if (!_map._mouseMoveRafScheduled) {
                    _map._mouseMoveRafScheduled = true;
                    (window.requestAnimationFrame || window.setTimeout)(function () {
                        _map._mouseMoveRafScheduled = false;
                        var pendingEvent = _map._pendingMouseMoveEvent;
                        _map._pendingMouseMoveEvent = null;
                        if (pendingEvent) {
                            webgis.tools.onMapMouseMove(_map, pendingEvent);
                        }
                    });
                }
                // old method: can block UI
                // webgis.tools.onMapMouseMove(this, e);
            }

            var ctrlKeyBox = _map._toolBoxLayer != null &&
                _map._activeTool != null &&
                (_map._activeTool.allow_ctrlbbox || webgis.tools.allowSketchVertexSelection(_map, _map._activeTool)) &&
                e.originalEvent.ctrlKey;

            if (_map._activeTool != null && (_map._activeTool.tooltype === 'box' || ctrlKeyBox) && _map._toolBoxLayer != null) {
                _map._toolBoxLayer.setBounds(L.latLngBounds(_map._toolBoxLayer.touchDownLatLng, e.latlng));
            }
        },
        keydown: function (e) {
            if (webgis.tools) {
                webgis.tools.onMapKeyDown(this, e.originalEvent);
            }
        },
        keyup: function (e) {
            if (webgis.tools) {
                webgis.tools.onMapKeyUp(this, e.originalEvent);
            }
        }
    };
};