//
// 'angle'
//
webgis.sketch.construct.register(new function () {
    var _angleMoverLine = null;

    this.modes = ['angle', 'angle_geographic'];

    this.onCancel = function (sketch) {
        if (_angleMoverLine) {
            sketch.map.frameworkElement.removeLayer(_angleMoverLine);
            _angleMoverLine = null;
        }
    };

    this.onMapMouseMove = function (sketch, latlng) {
        if (sketch.vertexCount() > 0) {
            var vertices = sketch._getRawVertices();
            var latLng1 = L.latLng(vertices[0].y, vertices[0].x);
            var latLng2 = latlng;

            if (_angleMoverLine == null) {
                _angleMoverLine = L.angleGraphics([latLng1, latLng2], { color: 'gray', weight: 1 });
                sketch._addToMap(_angleMoverLine);
            }
            else {
                _angleMoverLine.setLatLngs([latLng1, latLng2]);
            }
        }

        return false;
    };
    this.onAddVertex = function (sketch, vertex) {
        var mode = webgis.sketch.construct.getConstructionMode(sketch);

        if (_angleMoverLine != null) {
            var latLngs = _angleMoverLine.getLatLngs();
            if (latLngs.length == 2) {
                var angle = webgis.calc.angle_deg([{ x: latLngs[0].lng, y: latLngs[0].lat }, { x: latLngs[1].lng, y: latLngs[1].lat }]);

                switch (mode) {
                    case 'angle_geographic':
                        angle = 90 - angle;
                        break;
                }

                if (sketch._constructionModeTargetSelector) {
                    angle = parseInt(360.0 + angle) % 360;
                    //console.log(angle);
                    $(sketch._constructionModeTargetSelector).val(angle);
                }
            }
            sketch.map.frameworkElement.removeLayer(_angleMoverLine);
            _angleMoverLine = null;
        }

        webgis.sketch.construct.setConstructionMode(sketch, null);

        return true;
    };
}());

//
// add-vertex
//
webgis.sketch.construct.register(new function () {
    this.modes = ['add-vertex'];

    this.onMapKeyDown = function (sketch, e) {
        if (e.key === 'a' || e.key === 'A') {
            webgis.sketch.construct.setConstructionMode(sketch, this.modes[0]);
            sketch.map.setCursor('pen_add_vertex.cur');
            return true;
        }
        return false;
    };
    this.onMapKeyUp = function (sketch, e) {
        webgis.sketch.construct.setConstructionMode(sketch, null);

        return true;
    }

    this.onMapMouseMove = function (sketch, latlng) {
        return true;
    };

    this.onAddVertex = function (sketch, vertex) {
        var layerPoint = sketch.map.frameworkElement.latLngToLayerPoint({ lng: vertex.x, lat: vertex.y });
        var closestEdge = sketch._closestEdge(sketch.map, layerPoint);
        if (closestEdge != null && closestEdge.dist < 10) {
            sketch.addPostVertex(closestEdge.index, closestEdge.t);
        }

        return true;
    };

    this.getInfoItems = function (sketch) {
        return [{
            text: webgis.i18n.get("construction-info-add-vertex")
        }];
    };
});

//
// remove-vertex
//
webgis.sketch.construct.register(new function () {
    this.modes = ['remove-vertex'];

    this.onMapKeyDown = function (sketch, e) {
        if (e.key === 'd' || e.key === 'D') {
            webgis.sketch.construct.setConstructionMode(sketch, this.modes[0]);
            sketch.map.setCursor('pen_remove_vertex.cur');
            return true;
        }
        return false;
    };
    this.onMapKeyUp = function (sketch, e) {
        webgis.sketch.construct.setConstructionMode(sketch, null);

        return true;
    };

    this.onMarkerClick = function (sketch, marker) {
        var latlng = marker.getLatLng();
        sketch.addVertexCoords(latlng.lng, latlng.lat, true);  // perform "remove-vertex" there

        return true;
    };

    this.onMapMouseMove = function (sketch, latlng) {
        return true;
    };

    this.onAddVertex = function (sketch, vertex) {
        var layerPoint = sketch.map.frameworkElement.latLngToLayerPoint({ lng: vertex.x, lat: vertex.y });
        var closestVertex = sketch._closestVertex(sketch.map, layerPoint);
        if (closestVertex != null && closestVertex.dist < 5) {
            sketch.removeVertex(closestVertex.index);
        }

        return true;
    };

    this.getInfoItems = function (sketch) {
        return [{
            text: webgis.i18n.get("construction-info-remove-vertex")
        }];
    };
});

//
// select-vertex
//
webgis.sketch.construct.register(new function () {
    this.modes = ['select-vertex'];

    this.onMapKeyDown = function (sketch, e) {
        if (webgis.tools.allowSketchVertexSelection(sketch.map, sketch.map.getActiveTool())) {
            if (e.ctrlKey === true) {
                webgis.sketch.construct.setConstructionMode(sketch, this.modes[0]);
                sketch.map.setCursor('pen_select_vertices.cur');
                return true;
            }
            return false;
        }
    };
    this.onMapKeyUp = function (sketch, e) {
        if (e.key === 'Control') {
            webgis.sketch.construct.setConstructionMode(sketch, null);

            return true;
        }

        return false;
    }

    this.onMapMouseMove = function (sketch, latlng) {
        return true;
    };

    this.onAddVertex = function (sketch, vertex) {
        return true;
    };

    this.getInfoItems = function (sketch) {
        return [{
            text: webgis.i18n.get("construction-info-select-vertex")
        }];
    };
});

//
// distance-distance
//
webgis.sketch.construct.register(new function () {
    var _distanceElement1, _distanceElement2;
    var _resultMarkers;

    this.modes = ['distance-distance'];

    this.onCancel = function (sketch) {
        if (_distanceElement1) {
            sketch.map.frameworkElement.removeLayer(_distanceElement1);
            _distanceElement1 = null;
        }

        if (_distanceElement2) {
            sketch.map.frameworkElement.removeLayer(_distanceElement2);
            _distanceElement2 = null;
        }

        if (_resultMarkers) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers);
            _resultMarkers = null;
        }
    };

    this.onMapMouseMove = function (sketch, latlng) {
        //if (sketch.isClosed()) {
        //    return true;
        //}

        if (_distanceElement1 && !_distanceElement1._closed) {
            _distanceElement1.setCirclePoint(latlng);
        }
        else if (_distanceElement2 && !_distanceElement2._closed) {
            _distanceElement2.setCirclePoint(latlng);

            _calc(sketch);
        }

        return true;
    };

    this.onAddVertex = function (sketch, vertex) {

        if (!_distanceElement1) {
            _distanceElement1 = L.circlePro([L.latLng(vertex.y, vertex.x)], { color: 'green', weight: 1, fillColor: 'none', calcCrs: sketch.map.calcCrs() });
            sketch._addToMap(_distanceElement1);
        }
        else if (_distanceElement1 && !_distanceElement1._closed) {
            _distanceElement1.setCirclePoint(L.latLng(vertex.y, vertex.x));
            _distanceElement1._closed = true;
        }
        else if (!_distanceElement2) {
            _distanceElement2 = L.circlePro([L.latLng(vertex.y, vertex.x)], { color: 'green', weight: 1, fillColor: 'none', calcCrs: sketch.map.calcCrs() });
            sketch._addToMap(_distanceElement2);
        }
        else if (_distanceElement2 && !_distanceElement2._closed) {
            _distanceElement2.setCirclePoint(L.latLng(vertex.y, vertex.x));
            _distanceElement2._closed = true;

            _calc(sketch);
        }

        return true;
    };

    this.createContextMenuUI = function (sketch, $menu) {
        if (_distanceElement1 && !_distanceElement1._closed) {
            webgis.sketch.construct._ui.distanceContextUI(_distanceElement1, sketch, $menu, _calc);
        }
        else if (_distanceElement2 && !_distanceElement2._closed) {
            webgis.sketch.construct._ui.distanceContextUI(_distanceElement2, sketch, $menu, _calc);
        }

        if (!_distanceElement2 || !_distanceElement2._closed) {
            webgis.sketch.construct._ui.addContainer(sketch, $menu, {
                appendSnapping: true,
                appendXYAbsolute: true
            });
        }

        return true;
    };

    this.contextMenuItem = function (sketch) {
        return {
            mode: this.modes[0],
            icon: webgis.css.imgResource('sketch-construct-distance-distance_26.png', 'tools')
        };
    };

    this.getInfoItems = function (sketch) {
        if (!_distanceElement1) {
            return [{
                text: webgis.i18n.get("construction-info-distance1"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (!_distanceElement1._closed) {
            return [{
                text: webgis.i18n.get("construction-info-distance1-close")
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (!_distanceElement2) {
            return [{
                text: webgis.i18n.get("construction-info-distance2"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (!_distanceElement2._closed) {
            return [{
                text: webgis.i18n.get("construction-info-distance2-close"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else {
            return [webgis.sketch.construct._ui._infoItemClickConstructionPoint(), webgis.sketch.construct._ui._infoItemRightMouseButton() ];
        }
    };

    var _calc = function (sketch) {
        if (_resultMarkers) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers);
            _resultMarkers = null;
        }

        if (_distanceElement1 && _distanceElement1._closed && _distanceElement2) {

            var circle1LatLngs = _distanceElement1.getLatLngs(),
                circle2LatLngs = _distanceElement2.getLatLngs();

            vertices = [
                { x: circle1LatLngs[0].lng, y: circle1LatLngs[0].lat },
                { x: circle1LatLngs[1].lng, y: circle1LatLngs[1].lat },
                { x: circle2LatLngs[0].lng, y: circle2LatLngs[0].lat },
                { x: circle2LatLngs[1].lng, y: circle2LatLngs[1].lat },
            ];

            webgis.complementProjected(sketch.map.calcCrs(), vertices);

            // https://mathworld.wolfram.com/Circle-CircleIntersection.html
            var d = Math.sqrt(Math.pow(vertices[2].X - vertices[0].X, 2) + Math.pow(vertices[2].Y - vertices[0].Y, 2)),
                R = Math.sqrt(Math.pow(vertices[1].X - vertices[0].X, 2) + Math.pow(vertices[1].Y - vertices[0].Y, 2)),
                r = Math.sqrt(Math.pow(vertices[3].X - vertices[2].X, 2) + Math.pow(vertices[3].Y - vertices[2].Y, 2));

            if (d <= 0.0) {
                return;
            }

            var a_ = (-d + r - R) * (-d - r + R) * (-d + r + R) * (d + r + R);
            if (a_ <= 0.0) {
                return;
            }

            var a = 1 / d * Math.sqrt(a_);
            var x = (d * d - r * r + R * R) / (2.0 * d);
            //console.log('a=' + a, 'x=' + d);

            // To Real World Coordinates
            var v1 = { X: (vertices[2].X - vertices[0].X) / d, Y: (vertices[2].Y - vertices[0].Y) / d };
            var v2 = { X: -v1.Y, Y: v1.X };  // orthogonal

            var results = [
                { X: vertices[0].X + v1.X * x + v2.X * a / 2.0, Y: vertices[0].Y + v1.Y * x + v2.Y * a / 2.0 },
                { X: vertices[0].X + v1.X * x - v2.X * a / 2.0, Y: vertices[0].Y + v1.Y * x - v2.Y * a / 2.0 }
            ];
            //console.log('results', results, v1, v2);

            webgis.complementWGS84(sketch.map.calcCrs(), results);

            _resultMarkers = L.sketchConstructionResults({ sketch: sketch, vertices: results, addClick: _distanceElement2._closed === true });
            sketch._addToMap(_resultMarkers);
        }
    };
});

//
// direction-distance
//
webgis.sketch.construct.register(new function () {
    var _directionElement, _distanceElement;
    var _resultMarkers;

    this.modes = ['direction-distance'];

    this.onCancel = function (sketch) {
        if (_directionElement) {
            sketch.map.frameworkElement.removeLayer(_directionElement);
            _directionElement = null;
        }

        if (_distanceElement) {
            sketch.map.frameworkElement.removeLayer(_distanceElement);
            _distanceElement = null;
        }

        if (_resultMarkers) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers);
            _resultMarkers = null;
        }
    };

    this.onMapMouseMove = function (sketch, latlng) {
        //if (sketch.isClosed()) {
        //    return true;
        //}

        if (_directionElement && !_directionElement._closed) {
            _directionElement.setDirectionVertex({ x: latlng.lng, y: latlng.lat });
        }
        else if (_distanceElement && !_distanceElement._closed) {
            _distanceElement.setCirclePoint(latlng);

            _calc(sketch);
        }

        return true;
    };

    this.onAddVertex = function (sketch, vertex) {

        if (!_directionElement) {
            _directionElement = L.directionLine({ sketch: sketch, vertices: [vertex, vertex], color: 'green', weight: 1 });

            sketch._addToMap(_directionElement);
        }
        else if (!_directionElement._closed) {
            _directionElement.setDirectionVertex(vertex);
            _directionElement._closed = true;
        }
        else if (!_distanceElement) {
            _distanceElement = L.circlePro([L.latLng(vertex.y, vertex.x)], { color: 'green', weight: 1, fillColor: 'none', calcCrs: sketch.map.calcCrs() });
            sketch._addToMap(_distanceElement);
        }
        else if (!_distanceElement._closed) {
            _distanceElement.setCirclePoint({ lng: vertex.x, lat: vertex.y });
            _distanceElement._closed = true;

            _calc(sketch);
        }

        return true;
    };

    this.contextMenuItem = function (sketch) {
        return {
            mode: this.modes[0],
            icon: webgis.css.imgResource('sketch-construct-direction-distance_26.png', 'tools')
        };
    };

    this.createContextMenuUI = function (sketch, $menu) {
        if (_directionElement && !_directionElement._closed) {
            webgis.sketch.construct._ui.directionContextUI(_directionElement, sketch, $menu, _calc);
        }
        else if (_distanceElement && !_distanceElement._closed) {
            webgis.sketch.construct._ui.distanceContextUI(_distanceElement, sketch, $menu, _calc);
        }

        if (!_distanceElement || !_distanceElement._closed) {
            webgis.sketch.construct._ui.addContainer(sketch, $menu, {
                appendSnapping: true,
                appendXYAbsolute: true,
                fixDirection: {
                    append: _directionElement && !_directionElement._closed,
                    directionElement: _directionElement,
                    onSucceeded: _calc
                }
            });
        }

        return true;
    };

    this.getInfoItems = function (sketch) {
        if (!_directionElement) {
            return [{
                text: webgis.i18n.get("construction-info-direction"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (!_directionElement._closed) {
            return [{
                text: webgis.i18n.get("construction-info-direction-close"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (!_distanceElement) {
            return [{
                text: webgis.i18n.get("construction-info-distance"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (!_distanceElement._closed) {
            return [{
                text: webgis.i18n.get("construction-info-distance-close"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else {
            return [webgis.sketch.construct._ui._infoItemClickConstructionPoint(), webgis.sketch.construct._ui._infoItemRightMouseButton()];
        }
    };

    var _calc = function (sketch) {
        if (_resultMarkers) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers);
            _resultMarkers = null;
        }

        if (_directionElement && _directionElement._closed && _distanceElement) {
            var directionLatLngs = _directionElement.getLatLngs(),
                circleLatLngs = _distanceElement.getLatLngs();

            vertices = [
                { x: directionLatLngs[0].lng, y: directionLatLngs[0].lat },
                { x: directionLatLngs[1].lng, y: directionLatLngs[1].lat },
                { x: circleLatLngs[0].lng, y: circleLatLngs[0].lat },
                { x: circleLatLngs[1].lng, y: circleLatLngs[1].lat },
            ];

            webgis.complementProjected(sketch.map.calcCrs(), vertices);

            // https://mathworld.wolfram.com/Circle-LineIntersection.html
            var r = Math.sqrt(Math.pow(vertices[3].X - vertices[2].X, 2) + Math.pow(vertices[3].Y - vertices[2].Y, 2));
            var dx = vertices[1].X - vertices[0].X,
                dy = vertices[1].Y - vertices[0].Y;

            var dr = Math.sqrt(dx * dx + dy * dy);
            var D = (vertices[0].X - vertices[2].X) * (vertices[1].Y - vertices[2].Y) - (vertices[1].X - vertices[2].X) * (vertices[0].Y - vertices[2].Y);

            var sign_dy = dy < 0 ? -1 : 1;

            var descrimiant = r * r * dr * dr - D * D;
            if (descrimiant < 0) {
                return;
            }

            var x1 = (D * dy + sign_dy * dx * Math.sqrt(descrimiant)) / (dr * dr);
            var y1 = (-D * dx + Math.abs(dy) * Math.sqrt(descrimiant)) / (dr * dr);

            var x2 = (D * dy - sign_dy * dx * Math.sqrt(descrimiant)) / (dr * dr);
            var y2 = (-D * dx - Math.abs(dy) * Math.sqrt(descrimiant)) / (dr * dr); 

            // translate circle centerpoint
            var results = [
                { X: x1 + vertices[2].X, Y: y1 + vertices[2].Y },
                { X: x2 + vertices[2].X, Y: y2 + vertices[2].Y }
            ];

            webgis.complementWGS84(sketch.map.calcCrs(), results);

            _resultMarkers = L.sketchConstructionResults({ sketch: sketch, vertices: results, addClick: _distanceElement._closed === true });
            sketch._addToMap(_resultMarkers);
        }
    };
});

//
// direction-direction
//
webgis.sketch.construct.register(new function () {
    var _directionElement1, _directionElement2;
    var _resultMarkers;

    this.modes = ['direction-direction'];

    this.onCancel = function (sketch) {
        if (_directionElement1) {
            sketch.map.frameworkElement.removeLayer(_directionElement1);
            _directionElement1 = null;
        }

        if (_directionElement2) {
            sketch.map.frameworkElement.removeLayer(_directionElement2);
            _directionElement2 = null;
        }

        if (_resultMarkers) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers);
            _resultMarkers = null;
        }
    };

    this.onMapMouseMove = function (sketch, latlng) {
        //if (sketch.isClosed()) {
        //    return true;
        //}

        if (_directionElement1 && !_directionElement1._closed) {
            _directionElement1.setDirectionVertex({ x: latlng.lng, y: latlng.lat });
        }
        else if (_directionElement2 && !_directionElement2._closed) {
            _directionElement2.setDirectionVertex({ x: latlng.lng, y: latlng.lat });

            _calc(sketch);
        }

        return true;
    };

    this.onAddVertex = function (sketch, vertex) {

        if (!_directionElement1) {
            _directionElement1 = L.directionLine({ sketch: sketch, vertices: [vertex, vertex], color: 'green', weight: 1 });

            sketch._addToMap(_directionElement1);
        }
        else if (!_directionElement1._closed) {
            _directionElement1.setDirectionVertex(vertex);
            _directionElement1._closed = true;
        }
        else if (!_directionElement2) {
            _directionElement2 = L.directionLine({ sketch: sketch, vertices: [vertex, vertex], color: 'green', weight: 1 });
            sketch._addToMap(_directionElement2);
        }
        else if (!_directionElement2._closed) {
            _directionElement2.setDirectionVertex(vertex);
            _directionElement2._closed = true;

            _calc(sketch);
        }

        return true;
    };

    this.contextMenuItem = function (sketch) {
        return {
            mode: this.modes[0],
            icon: webgis.css.imgResource('sketch-construct-direction-direction_26.png', 'tools')
        };
    };

    this.createContextMenuUI = function (sketch, $menu) {
        if (_directionElement1 && !_directionElement1._closed) {
            webgis.sketch.construct._ui.directionContextUI(_directionElement1, sketch, $menu, _calc);
        }
        else if (_directionElement2 && !_directionElement2._closed) {
            webgis.sketch.construct._ui.directionContextUI(_directionElement2, sketch, $menu, _calc);
        }

        var currentDirectionElement = _directionElement2 || _directionElement1;
        if (!currentDirectionElement || !currentDirectionElement._closed) {
            webgis.sketch.construct._ui.addContainer(sketch, $menu, {
                appendSnapping: true,
                appendXYAbsolute: true,
                fixDirection: {
                    append: (currentDirectionElement && !currentDirectionElement._closed),
                    directionElement: currentDirectionElement,
                    onSucceeded: _calc
                }
            });
        }
                
        return true
    };

    this.getInfoItems = function (sketch) {
        if (!_directionElement1) {
            return [{
                text: webgis.i18n.get("construction-info-direction1"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (!_directionElement1._closed) {
            return [{
                text: webgis.i18n.get("construction-info-direction1-close"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (!_directionElement2) {
            return [{
                text: webgis.i18n.get("construction-info-direction2"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (!_directionElement2._closed) {
            return [{
                text: webgis.i18n.get("construction-info-direction2-close"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else {
            return [webgis.sketch.construct._ui._infoItemClickConstructionPoint(), webgis.sketch.construct._ui._infoItemRightMouseButton()];
        }
    };

    var _calc = function (sketch) {
        if (_resultMarkers) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers);
            _resultMarkers = null;
        }

        if (_directionElement1 && _directionElement1._closed && _directionElement2) {
            var direction1LatLngs = _directionElement1.getLatLngs(),
                direction2LatLngs = _directionElement2.getLatLngs();

            var result = webgis.calc.intersectionPoint(sketch.map.calcCrs(),
                [
                    { x: direction1LatLngs[0].lng, y: direction1LatLngs[0].lat },
                    { x: direction1LatLngs[1].lng, y: direction1LatLngs[1].lat }
                ],
                [
                    { x: direction2LatLngs[0].lng, y: direction2LatLngs[0].lat },
                    { x: direction2LatLngs[1].lng, y: direction2LatLngs[1].lat }
                ]
            );

            _resultMarkers = L.sketchConstructionResults({ sketch: sketch, vertices: [result], addClick: _directionElement2._closed === true });
            sketch._addToMap(_resultMarkers);
        }
    };
});

//
// midpoint
// 
webgis.sketch.construct.register(new function () {
    var _lineElement;
    var _resultMarkers;

    this.modes = ['midpoint'];

    this.onCancel = function (sketch) {
        if (_lineElement) {
            sketch.map.frameworkElement.removeLayer(_lineElement);
            _lineElement = null;
        }

        if (_resultMarkers) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers);
            _resultMarkers = null;
        }
    };

    this.onMapMouseMove = function (sketch, latlng) {
        //if (sketch.isClosed()) {
        //    return true;
        //}

        if (_lineElement && !_lineElement._closed) {
            var latLngs = _lineElement.getLatLngs();
            latLngs[1] = latlng;
            _lineElement.setLatLngs(latLngs);

            _calc(sketch);
        }

        return true;
    };

    this.onAddVertex = function (sketch, vertex) {

        if (!_lineElement) {
            _lineElement = L.polyline([{ lng: vertex.x, lat: vertex.y }, { lng: vertex.x, lat: vertex.y }], { color: 'green', weight: 1 });

            sketch._addToMap(_lineElement);
        }
        else if (!_lineElement._closed) {
            var latLngs = _lineElement.getLatLngs();
            latLngs[1] = { lng: vertex.x, lat: vertex.y };
            _lineElement.setLatLngs(latLngs);
            _lineElement._closed = true;

            _calc(sketch);
        }

        return true;
    };

    this.contextMenuItem = function (sketch) {
        return {
            mode: this.modes[0],
            icon: webgis.css.imgResource('sketch-construct-midpoint_26.png', 'tools')
        };
    };

    this.createContextMenuUI = function (sketch, $menu) {
        if (!_lineElement || !_lineElement._closed) {
            webgis.sketch.construct._ui.addContainer(sketch, $menu, {
                appendSnapping: true,
                appendXYAbsolute: true
            });
        }
    };

    this.getInfoItems = function (sketch) {
        if (!_lineElement) {
            return [{
                text: webgis.i18n.get("construction-info-midpoint"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (!_lineElement._closed) {
            return [{
                text: webgis.i18n.get("construction-info-midpoint-close"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else {
            return [webgis.sketch.construct._ui._infoItemClickConstructionPoint(), webgis.sketch.construct._ui._infoItemRightMouseButton()];
        }
    };

    var _calc = function (sketch) {
        if (_resultMarkers) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers);
            _resultMarkers = null;
        }

        if (_lineElement) {
            var latLngs = _lineElement.getLatLngs();

            var vertices = [
                { x: latLngs[0].lng, y: latLngs[0].lat },
                { x: latLngs[1].lng, y: latLngs[1].lat }
            ];

            webgis.complementProjected(sketch.map.calcCrs(), vertices);

            var results = [
                { X: (vertices[0].X + vertices[1].X) / 2.0, Y: (vertices[0].Y + vertices[1].Y) / 2.0 }
            ];

            webgis.complementWGS84(sketch.map.calcCrs(), results);

            _resultMarkers = L.sketchConstructionResults({ sketch: sketch, vertices: results, addClick: _lineElement._closed === true });
            sketch._addToMap(_resultMarkers);
        }
    };
});

//
// vertex-perpendicular
//
webgis.sketch.construct.register(new function () {
    var _resultMarkers;
    var _lines = [];

    this.modes = ['vertex-perpendicular'];

    this.onCancel = function (sketch) {
        _removeConstructionPoints(sketch);
    };

    this.contextMenuItem = function (sketch) {
        if (sketch._contextVertexIndex == null) {
            return null;
        }

        var neighborVertices = sketch.getNeighborVertices(sketch._contextVertexIndex, true);
        //console.log(neighborVertices);

        if (neighborVertices.length !== 3) {
            return null;
        }

        return {
            mode: this.modes[0],
            icon: webgis.css.imgResource('sketch-construct-vertex-perpendicular_26.png', 'tools')
        };
    };

    this.init = function (sketch) {
        _removeConstructionPoints(sketch);

        if (!_calc(sketch)) {
            webgis.sketch.construct.cancel(sketch);
        }
    };

    this.getInfoItems = function (sketch) {
        if (!_resultMarkers)  {
            return [webgis.sketch.construct._ui._infoItemClickConstructionPoint(), webgis.sketch.construct._ui._infoItemRightMouseButton()];
        }
    };

    var _removeConstructionPoints = function (sketch) {
        if (_lines.length > 0) {
            for (var l in _lines) {
                sketch.map.frameworkElement.removeLayer(_lines[l]);
            }
            _lines = [];
        }
        if (_resultMarkers) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers);
            _resultMarkers = null;
        }
    };

    var _calc = function (sketch) {
        if (sketch._contextVertexIndex == null) {
            return false;
        }

        var neighborVertices = sketch.getNeighborVertices(sketch._contextVertexIndex, true);

        if (neighborVertices.length !== 3) {
            return false;
        }

        var vertices = [
            { x: neighborVertices[0].x, y: neighborVertices[0].y },
            { x: neighborVertices[1].x, y: neighborVertices[1].y },
            { x: neighborVertices[2].x, y: neighborVertices[2].y }
        ];

        webgis.complementProjected(sketch.map.calcCrs(), vertices);

        var linePoints1, linePoints2, dX, dY, intersection, results = [];

        for (var i = 0; i <= 1; i++) {
            var index1 = 0, index2 = 2;
            if (i === 1) {
                index1 = 2, index2 = 0;
            }
            var linePoints1 = [
                { X: vertices[index1].X, Y: vertices[index1].Y },
                { X: vertices[1].X, Y: vertices[1].Y }
            ];
            dX = linePoints1[1].X - linePoints1[0].X;
            dY = linePoints1[1].Y - linePoints1[0].Y;
            var linePoints2 = [
                { X: vertices[index2].X, Y: vertices[index2].Y },
                { X: vertices[index2].X + dY, Y: vertices[index2].Y - dX }
            ];

            intersection = webgis.calc.intersectionPoint2(sketch.map.calcCrs(), linePoints1, linePoints2);
            if (intersection) {
                results.push(intersection);

                var line = L.polyline(
                    [
                        { lng: vertices[index1].x, lat: vertices[index1].y },
                        { lng: intersection.x, lat: intersection.y },
                        { lng: vertices[index2].x, lat: vertices[index2].y }
                    ],
                    { color: 'green', weight: 1 });

                sketch._addToMap(line);
                _lines.push(line);
            }
        }

        if (results.length > 0) {
            _resultMarkers = L.sketchConstructionResults({ sketch: sketch, vertices: results, addClick: true });
            sketch._addToMap(_resultMarkers);

            return true;
        }

        return false;
    };
});

//
//  Arc (3 Points)
//
webgis.sketch.construct.register(new function () {
    var _resultMarkers1, _resultMarkers2;
    var _latLngs = [];
    var _closed = false;

    this.modes = ['arc-3points'];

    this.init = function (sketch) {
        _removeConstructionPoints(sketch);

        var vertices = sketch._getRawVertices();
        if (vertices.length === 0)
            webgis.sketch.construct.cancel(sketch);

        _latLngs = [{
            lng: vertices[vertices.length - 1].x,
            lat: vertices[vertices.length - 1].y
        }, {
            lng: vertices[vertices.length - 1].x,
            lat: vertices[vertices.length - 1].y
        }
        ];

        _closed = false;
    };

    this.onCancel = function (sketch) {
        _removeConstructionPoints(sketch);
    };

    var _removeConstructionPoints = function (sketch) {
        console.log('cancel');
        if (_resultMarkers1) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers1);
            _resultMarkers1 = null;
        }
        if (_resultMarkers2) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers2);
            _resultMarkers2 = null;
        }

        _latLngs = [];
        _closed = false;

        console.log('closed', _closed);
    };

    this.onMapMouseMove = function (sketch, latlng) {

        if (!_closed) {
            if (_latLngs.length === 2) {
                _latLngs[1] = { lng: latlng.lng, lat: latlng.lat };
            } else if (_latLngs.length === 3) {
                _latLngs[2] = { lng: latlng.lng, lat: latlng.lat };
            }
        }

        _calc(sketch);

        return true;
    };

    this.onAddVertex = function (sketch, vertex) {
        if (!_closed) {
            if (_latLngs.length === 2) {
                _latLngs[1] = { lng: vertex.x, lat: vertex.y };
                _latLngs.push({ lng: vertex.x, lat: vertex.y });
            } else if (_latLngs.length === 3) {
                _latLngs[2] = { lng: vertex.x, lat: vertex.y };
                _closed = true;
            }

            _calc(sketch);
        }

        return true;
    };

    this.contextMenuItem = function (sketch) {
        if (!webgis.sketch.construct._isLineOrPolygon(sketch) ||
            sketch._contextVertexIndex != null ||
            sketch.getCurrentPartLength() < 1) {
            return null;
        }

        return {
            mode: this.modes[0],
            icon: webgis.css.imgResource('sketch-construct-arc-3points_26.png', 'tools')
        };
    };

    this.createContextMenuUI = function (sketch, $menu) {
        if (!_closed) {
            webgis.sketch.construct._ui.addContainer(sketch, $menu, {
                appendSnapping: true,
                appendXYAbsolute: true
            });
        }
    };

    this.suppressSnapping = function (sketch) {
        return _closed === true;
    };

    this.getInfoItems = function (sketch) {
        if (!_latLngs) {
            return null;
        }
        else if (_latLngs.length === 2) {
            return [{
                text: webgis.i18n.get("construction-info-arc-3points-point1"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (_latLngs.length === 3 && !_closed) {
            return [{
                text: webgis.i18n.get("construction-info-arc-3points-point2"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else {
            return [webgis.sketch.construct._ui._infoItemClickConstructionPoints(), webgis.sketch.construct._ui._infoItemRightMouseButton()];
        }
    };

    var _calc = function (sketch) {
        if (_resultMarkers1) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers1);
            _resultMarkers1 = null;
        }
        if (_resultMarkers2) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers2);
            _resultMarkers2 = null;
        }

        if (_latLngs.length < 3) {
            _resultMarkers1 = L.sketchConstructionResults({
                sketch: sketch,
                vertices: [{ x: _latLngs[0].lng, y: _latLngs[0].lat }, { x: _latLngs[1].lng, y: _latLngs[1].lat }]
            });
            sketch._addToMap(_resultMarkers1);

            return;
        }

        var vertices = [
            { x: _latLngs[0].lng, y: _latLngs[0].lat },
            { x: _latLngs[1].lng, y: _latLngs[1].lat },
            { x: _latLngs[2].lng, y: _latLngs[2].lat },
        ];

        webgis.complementProjected(sketch.map.calcCrs(), vertices);

        // https://www.arndt-bruenner.de/mathe/scripts/kreis3p.htm

        var eq33 = new webgis.calc.linearEquation3(
            -(Math.pow(vertices[0].X, 2.0) + Math.pow(vertices[0].Y, 2.0)),  // l0
            -(Math.pow(vertices[1].X, 2.0) + Math.pow(vertices[1].Y, 2.0)),  // l1
            -(Math.pow(vertices[2].X, 2.0) + Math.pow(vertices[2].Y, 2.0)),  // l2
            1, -vertices[0].X, -vertices[0].Y,
            1, -vertices[1].X, -vertices[1].Y,
            1, -vertices[2].X, -vertices[2].Y);

        if (!eq33.solve()) {
            console.log('detA==0.0');
            return;
        }

        var A = eq33.var1(), B = eq33.var2(), C = eq33.var3();
        var Mx = B / 2.0, My = C / 2.0, r2 = Math.pow(Mx, 2.0) + Math.pow(My, 2.0) - A;
        if (r2 <= 0) {
            console.log("r2<=0", r2);
            return;
        }
        var r = Math.sqrt(r2);
        //console.log("Arc: ", Mx, My, r);

        var dx1 = vertices[0].X - Mx, dy1 = vertices[0].Y - My;
        var dx2 = vertices[1].X - Mx, dy2 = vertices[1].Y - My;

        var angle1 = Math.atan2(dy1, dx1) * 180.0 / Math.PI,
            angle2 = Math.atan2(dy2, dx2) * 180.0 / Math.PI,
            step = 5.0;

        if (angle1 > angle2)
            angle1 -= 360.0;

        var arcPoints1 = [];
        for (var w = angle1; w < angle2; w += step) {
            arcPoints1.push({
                X: Mx + r * Math.cos(w * Math.PI / 180.0),
                Y: My + r * Math.sin(w * Math.PI / 180.0)
            });
        }
        arcPoints1.push({
            X: vertices[1].X, Y: vertices[1].Y
        });

        var arcPoints2 = [];
        for (var w = angle1 + 360.0; w > angle2; w -= step) {
            arcPoints2.push({
                X: Mx + r * Math.cos(w * Math.PI / 180.0),
                Y: My + r * Math.sin(w * Math.PI / 180.0)
            });
        }
        arcPoints2.push({
            X: vertices[1].X, Y: vertices[1].Y
        });

        webgis.complementWGS84(sketch.map.calcCrs(), arcPoints1);
        webgis.complementWGS84(sketch.map.calcCrs(), arcPoints2);

        var undoText = _closed ? webgis.i18n.get("construction-mode-arc-3points") : null;
        _resultMarkers1 = L.sketchConstructionResults({
            sketch: sketch,
            vertices: arcPoints1,
            addClick: _closed,
            color: 'red',
            display: 'polyline',
            undoText: undoText
        });
        sketch._addToMap(_resultMarkers1);

        _resultMarkers2 = L.sketchConstructionResults({
            sketch: sketch,
            vertices: arcPoints2,
            addClick: _closed,
            color: 'blue',
            display: 'polyline',
            undoText: undoText
        });
        sketch._addToMap(_resultMarkers2);
    };
});

//
//  Arc (2 Tangents)
//
webgis.sketch.construct.register(new function () {
    var _directionElement1, _directionElement2;
    var _resultMarkers1, _resultMarkers2;

    this.modes = ['arc-2tangents'];

    this.init = function (sketch) {
        _removeConstructionPoints(sketch);

        var vertices = sketch._getRawVertices();
        if (vertices.length < 2)
            webgis.sketch.construct.cancel(sketch);

        _directionElement1 = L.directionLine({ sketch: sketch, vertices: [vertices[vertices.length - 2], vertices[vertices.length - 1]], color: 'green', weight: 1 });
        _directionElement1._closed = true;
        sketch._addToMap(_directionElement1);

        _lineElement = L.polyline([], { color: 'green', weight: 1 });
        sketch._addToMap(_lineElement);
    };

    this.onCancel = function (sketch) {
        _removeConstructionPoints(sketch);
    };

    _removeConstructionPoints = function (sketch) {
        if (_directionElement1) {
            sketch.map.frameworkElement.removeLayer(_directionElement1);
            _directionElement1 = null;
        }
        if (_directionElement2) {
            sketch.map.frameworkElement.removeLayer(_directionElement2);
            _directionElement2 = null;
        }
        if (_resultMarkers1) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers1);
            _resultMarkers1 = null;
        }
        if (_resultMarkers2) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers2);
            _resultMarkers2 = null;
        }
    };

    this.onMapMouseMove = function (sketch, latlng) {
        //if (sketch.isClosed()) {
        //    return true;
        //}

        if (_directionElement2 && !_directionElement2._closed) {
            _directionElement2.setDirectionVertex({ x: latlng.lng, y: latlng.lat });

            _calc(sketch);
        }

        return true;
    };

    this.onAddVertex = function (sketch, vertex) {
        if (!_directionElement2) {
            _directionElement2 = L.directionLine({ sketch: sketch, vertices: [vertex, vertex], color: 'green', weight: 1 });
            sketch._addToMap(_directionElement2);
        } else if (!_directionElement2._closed) {
            _directionElement2.setDirectionVertex(vertex);
            _directionElement2._closed = true;

            _calc(sketch);
        }

        return true;
    };

    this.contextMenuItem = function (sketch) {
        if (!webgis.sketch.construct._isLineOrPolygon(sketch) ||
            sketch._contextVertexIndex != null ||
            sketch.getCurrentPartLength() < 2) {
            return null;
        }

        return {
            mode: this.modes[0],
            icon: webgis.css.imgResource('sketch-construct-arc-2tangents_26.png', 'tools')
        };
    };

    this.createContextMenuUI = function (sketch, $menu) {
        if (_directionElement2 && !_directionElement2._closed) {
            webgis.sketch.construct._ui.directionContextUI(_directionElement2, sketch, $menu, _calc);

            webgis.sketch.construct._ui.addContainer(sketch, $menu, {
                appendSnapping: true,
                appendXYAbsolute: true,
                fixDirection: {
                    append: true,
                    directionElement: _directionElement2,
                    onSucceeded: _calc
                }
            });
        }
    };

    this.suppressSnapping = function (sketch) {
        return _directionElement2 && _directionElement2._closed === true;
    };

    this.getInfoItems = function (sketch) {
        if (!_directionElement2) {
            return [{
                text: webgis.i18n.get("construction-info-arc-2tangents-point1"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        } else if (!_directionElement2._closed) {
            return [{
                text: webgis.i18n.get("construction-info-arc-2tangents-point2"),
            }, webgis.sketch.construct._ui._infoItemRightMouseButton()];
        }
        else {
            return [webgis.sketch.construct._ui._infoItemClickConstructionPoints(), webgis.sketch.construct._ui._infoItemRightMouseButton()];
        }

        return null;
    };

    var _calc = function (sketch) {

        if (_resultMarkers1) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers1);
            _resultMarkers1 = null;
        }
        if (_resultMarkers2) {
            sketch.map.frameworkElement.removeLayer(_resultMarkers2);
            _resultMarkers2 = null;
        }

        if (!_directionElement1 || !_directionElement2) {
            return;
        }

        var direction1LatLngs = _directionElement1.getLatLngs(),
            direction2LatLngs = _directionElement2.getLatLngs();

        var tangent1Points = [
            { x: direction1LatLngs[0].lng, y: direction1LatLngs[0].lat },
            { x: direction1LatLngs[1].lng, y: direction1LatLngs[1].lat }
        ];
        var tangent2Points = [
            { x: direction2LatLngs[0].lng, y: direction2LatLngs[0].lat },
            { x: direction2LatLngs[1].lng, y: direction2LatLngs[1].lat }
        ];

        var tangentIntersectionPoint = webgis.calc.intersectionPoint(sketch.map.calcCrs(),
            tangent1Points, tangent2Points
        );

        if (tangentIntersectionPoint === null) {
            return;
        };

        webgis.complementProjected(sketch.map.calcCrs(), tangent1Points);
        webgis.complementProjected(sketch.map.calcCrs(), tangent2Points);

        //console.log('tagentIntersectionPoint', tangentIntersectionPoint);

        var r1x = tangentIntersectionPoint.X - tangent1Points[1].X,
            r1y = tangentIntersectionPoint.Y - tangent1Points[1].Y,
            dist = Math.sqrt(r1x * r1x + r1y * r1y);
        r1x /= dist, r1y /= dist;

        var r2x = tangent2Points[1].X - tangent2Points[0].X,
            r2y = tangent2Points[1].Y - tangent2Points[0].Y,
            len = Math.sqrt(r2x * r2x + r2y * r2y);
        r2x /= len; r2y /= len;

        circlePoint1 = { X: tangent1Points[1].X, Y: tangent1Points[1].Y };
        circlePoint2 = { X: tangentIntersectionPoint.X + r2x * dist, Y: tangentIntersectionPoint.Y + r2y * dist };

        var rPoint1 = { X: circlePoint1.X - r1y, Y: circlePoint1.Y + r1x },
            rPoint2 = { X: circlePoint2.X - r2y, Y: circlePoint2.Y + r2x };

        webgis.complementWGS84(sketch.map.calcCrs(), [circlePoint1, circlePoint2, rPoint1, rPoint2]);

        var circleCenter = webgis.calc.intersectionPoint(sketch.map.calcCrs(),
            [
                { x: circlePoint1.x, y: circlePoint1.y },
                { x: rPoint1.x, y: rPoint1.y }
            ],
            [
                { x: circlePoint2.x, y: circlePoint2.y },
                { x: rPoint2.x, y: rPoint2.y }
            ]
        );

        var r = Math.sqrt(Math.pow(circleCenter.X - circlePoint1.X, 2.0) + Math.pow(circleCenter.Y - circlePoint1.Y, 2.0));

        var dx1 = circlePoint1.X - circleCenter.X, dy1 = circlePoint1.Y - circleCenter.Y;
        var dx2 = circlePoint2.X - circleCenter.X, dy2 = circlePoint2.Y - circleCenter.Y;

        var angle1 = Math.atan2(dy1, dx1) * 180.0 / Math.PI,
            angle2 = Math.atan2(dy2, dx2) * 180.0 / Math.PI,
            step = 5;

        if (angle1 > angle2)
            angle1 -= 360.0;

        var arcPoints1 = [];
        for (var w = angle1; w < angle2; w += step) {
            arcPoints1.push({
                X: circleCenter.X + r * Math.cos(w * Math.PI / 180.0),
                Y: circleCenter.Y + r * Math.sin(w * Math.PI / 180.0)
            });
        }
        var arcPoints2 = [];
        for (var w = angle1 + 360.0; w > angle2; w -= step) {
            arcPoints2.push({
                X: circleCenter.X + r * Math.cos(w * Math.PI / 180.0),
                Y: circleCenter.Y + r * Math.sin(w * Math.PI / 180.0)
            });
        }

        arcPoints1.push(circlePoint2);
        arcPoints1.push({ x: direction2LatLngs[1].lng, y: direction2LatLngs[1].lat });
        arcPoints2.push(circlePoint2);
        arcPoints2.push({ x: direction2LatLngs[1].lng, y: direction2LatLngs[1].lat });

        webgis.complementWGS84(sketch.map.calcCrs(), arcPoints1);
        webgis.complementWGS84(sketch.map.calcCrs(), arcPoints2);

        var undoText = _directionElement2._closed === true ? webgis.i18n.get("construction-mode-arc-2tangents") : null;
        _resultMarkers1 = L.sketchConstructionResults({
            sketch: sketch,
            vertices: arcPoints1,
            addClick: _directionElement2._closed === true,
            color: 'red',
            display: 'polyline',
            undoText: undoText
        });
        sketch._addToMap(_resultMarkers1);

        _resultMarkers2 = L.sketchConstructionResults({
            sketch: sketch,
            vertices: arcPoints2,
            addClick: _directionElement2._closed === true,
            color: 'blue',
            display: 'polyline',
            undoText: undoText
        });
        sketch._addToMap(_resultMarkers2);
    };
});

//
// circle
//
webgis.sketch.construct.register(new function () {
    var _moverLine = null;

    this.modes = ['circle'];

    this.onCancel = function (sketch) {
        if (_moverLine) {
            sketch.map.frameworkElement.removeLayer(_moverLine);
            _moverLine = null;
        }
    };

    this.onMapMouseMove = function (sketch, latlng) {
        //if (sketch.isClosed()) {
        //    return true;
        //}

        if (sketch.vertexCount() > 0) {
            var latLng1 = latlng, vertices = sketch._getRawVertices();
            var latLng2 = L.latLng(vertices[0].y, vertices[0].x);

            var r = latLng1.distanceTo(latLng2);
            var radius = latLng1.distanceTo(latLng2);

            if (_moverLine === null) {
                _moverLine = L.circlePro([L.latLng(vertices[0].y, vertices[0].x)], { color: 'green', weight: 1, fillColor: 'none', calcCrs: sketch.map.calcCrs() });
                _moverLine.setCirclePoint(latlng);

                sketch._addToMap(_moverLine);
            }
            else {
                _moverLine.setCirclePoint(latlng);
            }
        }

        return true;
    };

    this.onAddVertex = function (sketch, vertex) {
        if (webgis.sketch.construct._isPolygon(sketch)) {
            webgis.sketch.construct.setConstructionMode(sketch, null);

            if (sketch.vertexCount() === 1) {
                var center = sketch.getConstructionVerticesPro()[0];
                webgis.complementProjected(sketch.map.calcCrs(), [vertex]);

                var r = Math.sqrt((center.X - vertex.X) * (center.X - vertex.X) + (center.Y - vertex.Y) * (center.Y - vertex.Y));
                //console.log('radius', r);

                if (r > 0) {
                    var step = (0.5 / r) * 180 / Math.PI;

                    var circleVertices = [];

                    if (step > 10) {
                        step = 10;
                    }
                    else if (step < 2) {
                        step = 2;
                    }
                    for (var a = 0; a < 360.0; a += step) {
                        circleVertices.push(sketch.map.construct.distanceDirection(center, a, r));
                    }
                    sketch.map.frameworkElement.removeLayer(_moverLine);
                    _moverLine = null;

                    sketch.addUndo('Kreis konstruieren');
                    sketch.removeVertex(0, true);
                    sketch.addVertices(circleVertices, true);
                    sketch.appendPart();  // this.close();
                }
            }

            return true;
        }

        return false;
    };

    this.createContextMenuUI = function (sketch, $menu) {
        $("<li>")
            .appendTo($menu)
            .webgis_form({
                name: 'construct_circle',
                input: [
                    {
                        label: 'Radius [m]',
                        name: 'radius',
                        type: 'number',
                        required: true
                    }
                ],
                submitText: 'Kreis konstruieren',
                onSubmit: function (result) {
                    var sketch = webgis.sketch.construct._sketchFromContextMenuItem(this);
                    var radius = result.radius;

                    //console.log(radius);

                    if (radius) {
                        var center = sketch.getConstructionVerticesPro()[0];
                        //console.log(center);
                        var p = sketch.map.construct.distanceDirection(center, 0, radius, 0);

                        if (p) {
                            sketch._snappedVertex = p;
                            sketch.addVertexCoords(p.x, p.y, true); // x,y sind dummy -> es wird sowie der Snapped Vertex genommen...
                        }
                    }

                    webgis.sketch.construct._closeContextMenu(this);
                }
            });

        return true;
    };

    this.contextMenuItem = function (sketch) {
        var vertices = sketch._getRawVertices();

        if (vertices.length === 1 && webgis.sketch.construct._isPolygon(sketch)) {
            return {
                mode: this.modes[0],
                icon: webgis.css.imgResource('sketch-construct-circle-26.png', 'tools')
            };
        }

        return null;
    };
}());

//
//  rectangle
//
webgis.sketch.construct.register(new function () {
    var _moverLine = null;

    this.modes = ['rectangle'];

    this.onCancel = function (sketch) {
        if (_moverLine) {
            sketch.map.frameworkElement.removeLayer(_moverLine);
            _moverLine = null;
        }
    }

    this.onMapMouseMove = function (sketch, latlng) {
        //if (sketch.isClosed()) {
        //    return true;
        //}

        if (sketch.vertexCount() > 0) {
            var vertices = sketch._getRawVertices();
            var rectBounds = [[latlng.lat, latlng.lng], [vertices[0].y, vertices[0].x]];

            if (_moverLine === null) {
                _moverLine = L.rectangle(rectBounds, { color: 'green', weight: 1, fillColor: 'none' });
                sketch._addToMap(_moverLine);
            }
            else {
                _moverLine.setBounds(rectBounds);
            }
        }

        return true;
    };

    this.onAddVertex = function (sketch, vertex) {
        if (webgis.sketch.construct._isPolygon(sketch)) {
            webgis.sketch.construct.setConstructionMode(sketch, null);

            if (sketch.vertexCount() === 1) {
                var corner = sketch.getConstructionVerticesPro()[0];
                webgis.complementProjected(sketch.map.calcCrs(), [vertex]);

                var rectWidth = vertex.X - corner.X,
                    rectHeight = vertex.Y - corner.Y;

                //console.log('rect', rectWidth, rectHeight);

                var rectVertices = [];
                rectVertices.push(corner);
                rectVertices.push({ X: corner.X + rectWidth, Y: corner.Y });
                rectVertices.push(vertex);
                rectVertices.push({ X: corner.X, Y: corner.Y + rectHeight });

                webgis.complementWGS84(sketch.map.calcCrs(), rectVertices);
                //console.log('rectvertices', rectVertices);

                sketch.map.frameworkElement.removeLayer(_moverLine);
                _moverLine = null;

                sketch.addUndo('Rechteck konstruieren');
                sketch.removeVertex(0, true);
                sketch.addVertices(rectVertices, true);
                sketch.appendPart();  // this.close();
            }

            return true;
        }

        return false;
    };

    this.createContextMenuUI = function (sketch, $menu) {
        $("<li>")
            .appendTo($menu)
            .webgis_form({
                name: 'construct_rectangle',
                input: [
                    {
                        label: 'Breite [m]',
                        name: 'width',
                        type: 'number',
                        required: true
                    },
                    {
                        label: 'Höhe [m]',
                        name: 'height',
                        type: 'number',
                        required: true
                    }
                ],
                submitText: 'Rechteck konstruieren',
                onSubmit: function (result) {
                    var sketch = webgis.sketch.construct._sketchFromContextMenuItem(this);

                    var corner = sketch.getConstructionVerticesPro()[0];
                    var p = {
                        X: corner.X + result.width,
                        Y: corner.Y - result.height
                    };

                    webgis.complementWGS84(sketch.map.calcCrs(), [p]);

                    sketch._snappedVertex = p;
                    sketch.addVertexCoords(p.x, p.y, true); // x,y sind dummy -> es wird sowie der Snapped Vertex genommen...

                    webgis.sketch.construct._closeContextMenu(this);
                }
            });

        return true;
    };

    this.contextMenuItem = function (sketch) {
        var vertices = sketch._getRawVertices();

        if (vertices.length === 1 && webgis.sketch.construct._isPolygon(sketch)) {
            return {
                mode: this.modes[0],
                icon: webgis.css.imgResource('sketch-construct-rectangle-26.png', 'tools')
            };
        }

        return null;
    };
});