webgis.sketch = function (map) {
    "use strict"

    var $ = webgis.$;

    webgis.implementEventController(this);
    this.map = map;
    this.ui = new webgis.sketch._ui(this);

    var _vertices = [], _partIndex = [0], _undos = [];
    var _lastFreehandVertexIndex = -1;
    var _geometryType = '', _originalSrs = 0, _originalSrsP4Params = null;
    var _frameworkElements = [], _currentFrameworkIndex = 0;
    var _vertexFrameworkElements = [], _currentBubbleMoveVertextFrameworkElement = null;
    var _color = ['#ff0000', '#000000'], _fillColor = ['#ffff00', '#aaaaaa'], _opacity = [0.8, 1.0], _fillOpacity = [0.2, 0.2], _weight = [2, 2], _linesStyle = [null, null];
    var _txtColor = ['#000','#000'], _txtStyle = ['',''], _txtSize = [12, 14], _txtRefResolution = 0; 
    var _distance_circle_radius = 5000.0, _distance_circle_steps = 3, _compass_rose_radius=10.0, _compass_rose_steps = 36, _circle_radius = 0, _svg_text = null;
    var _hectoline_unit = 'm', _hectoline_interval = 100;
    var _dimPolygon_areaUnit = 'm²', _dimPolygon_labelEdges = true;
    var _isDraggingMarker = false;
    var _sketchProperties = null;
    this._lastChangeTime = new Date().getTime();
    var _sketchMovingVertexIndex = -1, _sketchRotatingVertexIndex = -1;

    var _ortho = false, _trace = false, _fan = false;
    this._isDirty = false;

    var _isLineOrPolygon = function () { return $.inArray(_geometryType, ["polyline", "polygon", "dimline", "dimpolygon", "hectoline"]) >= 0 };
    var _isLine = function () { return $.inArray(_geometryType, ["polyline", "dimline", "hectoline"]) >= 0 }
    var _isPolygon = function () { return $.inArray(_geometryType, ["polygon", "dimpolygon"]) >= 0 };
    var _isSimplePoint = function () { return _geometryType === 'point' };

    this._resetMarkerImage = function (frameworkElement) {
        if (webgis.mapFramework == "leaflet") {
            frameworkElement.setIcon(webgis.createMarkerIcon({ vertex: frameworkElement.vertex_index + 1, icon: 'sketch_vertex' }));
        }
    };
    this._addToMap = function (element) {
        element.addTo(this.map.frameworkElement);
    };
    this._createFrameworkElement = function (latLngs) {
        if (webgis.mapFramework === "leaflet") {
            switch (_geometryType) {
                case 'point':
                    var marker = webgis.createMarker({ draggable: true, lat: 0, lng: 0, icon: 'sketch_vertex', vertex: 1 });
                    marker.sketch = this;
                    marker.vertex_index = 0;
                    this._appendMarkerEvents(marker);
                    return marker;
                case 'polyline':
                case 'linestring':
                    _geometryType = 'polyline';
                    var polyline = L.polyline(latLngs || [], { color: this.getColor(), opacity: this.getOpacity(), weight: this.getWeight(), dashArray: this._toDashArray(this.getLineStyle()) });
                    polyline._webgis = { geometryType: _geometryType, lineStyle: this.getLineStyle() };
                    return polyline;
                case 'freehand':
                    var freeline = L.polyline(latLngs || [], { color: this.getColor(), opacity: this.getOpacity(), weight: this.getWeight(), dashArray: this._toDashArray(this.getLineStyle()) });
                    freeline._webgis = { geometryType: _geometryType, lineStyle: this.getLineStyle() };
                    return freeline;
                case 'polygon':
                    // Interactive = true -> Sonst lässt sich das Polygon beim Redling nicht mehr auswählen
                    var polygon = L.polygon(latLngs || [], { interactive: true, color: this.getColor(), fillColor: this.getFillColor(), opacity: this.getOpacity(), fillOpacity: this.getFillOpacity(), weight: this.getWeight(), dashArray: this._toDashArray(this.getLineStyle()) });
                    polygon._webgis = { geometryType: _geometryType, lineStyle: this.getLineStyle() };
                    // Kein Click Event -> Man kann sonst nicht mehr Punkte auf die Fläche setzen!!!
                    //polygon.on('click', function (e) {  
                    //    this.events.fire('click', this, e);
                    //}, this);
                    return polygon;
                case 'rectangle':
                    var rect = L.rectangle(latLngs || [[0, 0], [0, 0]], { interactive: true, color: this.getColor(), fillColor: this.getFillColor(), opacity: this.getOpacity(), fillOpacity: this.getFillOpacity(), weight: this.getWeight(), dashArray: this._toDashArray(this.getLineStyle()) });
                    rect._webgis = { geometryType: _geometryType, lineStyle: this.getLineStyle() };
                    return rect;
                case 'circle':
                    var circle = L.circle(latLngs || [0, 0], { radius: _circle_radius, interactive: true, color: this.getColor(), fillColor: this.getFillColor(), opacity: this.getOpacity(), fillOpacity: this.getFillOpacity(), weight: this.getWeight(), dashArray: this._toDashArray(this.getLineStyle()) });
                    circle._webgis = { geometryType: _geometryType, lineStyle: this.getLineStyle() };
                    return circle;
                case 'text':
                    var text = L.svgText(latLngs || [0, 0], {
                        text: _svg_text || 'Text...',
                        fontColor: this.getTextColor(),
                        fontSize: this.getTextSize(),
                        fontStyle: this.getTextStyle(),
                        refResolution: _txtRefResolution
                    });
                    text._webgis = { geometryType: _geometryType };
                    return text;
                case 'distance_circle':
                    var distanceCircle = L.distanceCircle(latLngs || [], {
                        radius: this.getDistanceCircleRadius(),
                        steps: this.getDistanceCircleSteps(),
                        color: this.getColor(),
                        fillColor: this.getFillColor(),
                        weight: this.getWeight()
                    });
                    distanceCircle._webgis = { geometryType: _geometryType };
                    return distanceCircle;
                case 'compass_rose':
                    var compassRose = L.compassRose(latLngs || [], {
                        radius: this.getCompassRoseRadius(),
                        steps: this.getCompassRoseSteps(),
                        color: this.getColor(),
                        weight: this.getWeight(),
                        calcCrs: this.map.calcCrs()
                    });
                    compassRose._webgis = { geometryType: _geometryType };
                    return compassRose;
                case 'dimline':
                    var dimLine = L.dimLine(latLngs || [], {
                        color: this.getColor(),
                        weight: this.getWeight(),
                        fontSize: this.getTextSize(),
                        fontColor: this.getTextColor()
                    });
                    dimLine._webgis = { geometryType: _geometryType };
                    return dimLine;
                case 'dimpolygon':
                    var dimPolygon = L.dimPolygon(latLngs || [], {
                        fillColor: this.getFillColor(),
                        fillOpacity: this.getFillOpacity(),
                        color: this.getColor(),
                        weight: this.getWeight(),
                        fontSize: this.getTextSize(),
                        fontColor: this.getTextColor(),
                        areaUnit: this.getDimPolygonAreaUnit(),
                        labelEdges: this.getDimPolygonLabelEdges()
                    });
                    dimPolygon._webgis = { geometryType: _geometryType };
                    return dimPolygon;
                case 'hectoline':
                    var hectoline = L.hectoLine(latLngs || [], {
                        color: this.getColor(),
                        weight: this.getWeight(),
                        fontColor: this.getTextColor(),
                        fontSize: this.getTextSize(),
                        interval: this.getHectolineInterval(),
                        unit: this.getHectolineUnit()
                    });
                    hectoline._webgis = { geometryType: _geometryType };
                    return hectoline;
            }
        }
    };
    this._isFirstPartVertex = function (index, partIndex) {
        return index > 0 && ($.inArray(index, partIndex || _partIndex) >= 0);
    };
    this._moverMarker = null;
    //this._moverMarkers = null;
    this._moverLine = null;
    var _traceLine = null;
    var _sketchMoveFrameworkElements = [];
    var _sketchRotateFrameworkElements = [];
    this._constructMover = null;
    this._isReadonly = false;
    this._isEditable = false;

    // War für 3D Messen (dynamisch) angedacht => erst einmal auf Eis gelegt
    //this._hasZ = false;
    //this._getZValuesCommand = '';

    var _cloneArray = function (arr) {
        //return arr.splice(0);
        var ret = [];
        if (arr && arr.length) {
            for (var i = 0; i < arr.length; i++) {
                ret.push(arr[i]);
            }
        }
        return ret;
    };

    this.store = function () {
        return {
            vertices: _vertices.slice(0),
            parts: _partIndex.slice(0),
            geometryType: _geometryType,
            closed: this.isClosed(),
            readOnly: this.isReadOnly(),
            originalSrs: _originalSrs,
            originalSrsP4Params: _originalSrsP4Params
        };
    };
    this.load = function (stored, loadedForTool) {
        if (!stored)
            return false;

        _geometryType = stored.geometryType;

        // first clone
        var storedVertices = stored.vertices.slice(0);
        var sotredParts = stored.parts.slice(0);
        // then remomve -> remove uses same _vertices refrernce and slices -> clone first
        this.remove(true);
        this.setReadOnly(stored.readOnly === true);

        this.events.suppress('onchanged');  // Performance!! dont calc area etc on every add (large polygons, ...)

        for (var i = 0; i < storedVertices.length; i++) {
            if (this._isFirstPartVertex(i, sotredParts))
                this.appendPart();
            this.addVertex(storedVertices[i]);
        }
        if (stored.closed) {
            this.appendPart();
        }
        //_partIndex = stored.parts;
        //this.redraw(_geometryType == 'polyline' || _geometryType == 'polygon');

        this.events.enable('onchanged');
        this.events.fire('onchanged', this);

        _originalSrs = stored.originalSrs || 0;
        _originalSrsP4Params = stored.originalSrsP4Params || null;

        this.loadedForTool = loadedForTool;
        this._isDirty = false;
        return true;
    };
    this.setReadOnly = function (readOnly) { this._isReadonly = (readOnly === true); };
    this.isReadOnly = function () { return this._isReadonly === true; };

    this._suppressFireSetEditableChanged = false;
    this.setEditable = function (editable) {
        editable = editable !== false;
        if (this._isEditable !== editable) {
            this._isEditable = editable;
            if (editable === false) {
                for (var i in _vertexFrameworkElements) {
                    this.map.frameworkElement.removeLayer(_vertexFrameworkElements[i]);
                }
            } else {
                this.redraw(true);
            }
        }

        if (!this._suppressFireSetEditableChanged)
            this.events.fire('oneditablechanged', this);
    };
    this.isEditable = function () { return this._isEditable === true; };

    this.destroy = function () {
        if (this.map) {
            this.map.events.off('rightclick', mapOnRightClick);
            this.map.events.off(['moveend', 'zoomend'], mapOnZoomEnd);
            this.remove(true);
            this.map = null;
        }
    };

    this.setConstructionMode = function (mode, targetSelector) {
        webgis.sketch.construct.setConstructionMode(this, mode, targetSelector);
    };

    this.setGeometryType = function (geom) {
        geom = geom.toLocaleLowerCase();
        if (geom === 'symbol')
            geom = 'point';
        if (geom === 'line' || geom === 'linestring' || geom === 'multilinestring')
            geom = 'polyline';
        if (geom.toLocaleLowerCase() != _geometryType) {
            _geometryType = geom.toLowerCase();
            this.remove(true);
            return;
        }
        else {
            this.hide();
        }

        _frameworkElements = [];
        _frameworkElements.push(this._createFrameworkElement());
        _currentFrameworkIndex = 0;

        webgis.sketch.construct.cancel(this);

        var frameworkElement = _frameworkElements[_frameworkElements.length - 1];
        if (webgis.mapFramework === "leaflet") {
            for (var i = 0; i < _vertices.length; i++) {
                if (i > 0 && this.isInFanMode()) {
                    _frameworkElements.push(this._createFrameworkElement());
                    frameworkElement = _frameworkElements[_frameworkElements.length - 1];
                    frameworkElement.addLatLng(L.latLng(_vertices[0].y, _vertices[0].x));
                }
                else if (i > 0 && $.inArray(i, _partIndex) >= 0) {
                    _frameworkElements.push(this._createFrameworkElement());
                    frameworkElement = _frameworkElements[_frameworkElements.length - 1];
                }

                var vertex = _vertices[i];
                if (frameworkElement.addLatLng) {
                    frameworkElement.addLatLng(L.latLng(vertex.y, vertex.x));
                }
                else {
                    frameworkElement.setLatLng(L.latLng(vertex.y, vertex.x));  
                }

                if (_geometryType === 'point') {  // Bei Punkt ist Frameworkelement auch der verschiebbare Marker und brauch den Vertex, damit beim Verschieben im "dragend" die Koordinaten richt übernommen werden.
                    frameworkElement.sketch_vertex_coords = vertex;
                }
            }
            if (_vertices.length > 0 && $.inArray(_vertices.length, _partIndex) >= 0) { // Closed Geometry -> start a new Part
                _frameworkElements.push(this._createFrameworkElement());
            }
        }

        this.setEditable(true);
        this.show();
        this.events.fire('onchangegeometrytype', this);
        this.events.fire('onchanged', this);
        this.refreshProperties();

        this.fireCurrentState();
    };
    this.getGeometryType = function () { return _geometryType; };
    this.remove = function (silent) {
        if (!silent)
            this.addUndo(webgis.l10n.get("sketch-remove-sketch"));
        if (webgis.mapFramework === "leaflet" && _frameworkElements) {
            for (var i in _frameworkElements) {
                this.map.frameworkElement.removeLayer(_frameworkElements[i]);
            }
            for (var i in _vertexFrameworkElements) {
                this.map.frameworkElement.removeLayer(_vertexFrameworkElements[i]);
            }
        }

        const isValidBeforeRemove = this.isValid();

        _vertexFrameworkElements = [];
        _currentFrameworkIndex = 0;
        //_vertices = [];
        //_partIndex = [0];
        _vertices.splice(0, _vertices.length);  // clear -> keep reference
        _partIndex.splice(0, _partIndex.length);  // clear -> keep reference
        _partIndex.push(0);

        this.stopFanMode();
        this.setGeometryType(_geometryType);

        this.events.fire('onremoved', this);
        this.events.fire('onchanged', this);
        this._isDirty = false;

        if (isValidBeforeRemove === true && this.isValid() === false) {
            this.events.fire('ongetsinvalid', this);
        }
    };
    this.show = function () {
        if (_frameworkElements === null || this.map === null)
            return;

        if (webgis.mapFramework === "leaflet") {
            for (var i in _frameworkElements)
                this._addToMap(_frameworkElements[i]);
            for (var i in _vertexFrameworkElements) {
                this._addToMap(_vertexFrameworkElements[i]);
            }
        }
    };
    this.hide = function () {
        if (_frameworkElements == null || this.map == null || this.map.frameworkElement == null)
            return;

        if (webgis.mapFramework == "leaflet" && this.map) {
            for (var i in _frameworkElements)
                this.map.frameworkElement.removeLayer(_frameworkElements[i]);
            for (var i in _vertexFrameworkElements) {
                this.map.frameworkElement.removeLayer(_vertexFrameworkElements[i]);
            }
            this.hideMoverLine();
            this.hideMoverMarker();
            this.hideTraceLine();
        }
    };
    this.redraw = function (resetVetices) {

        if (resetVetices) {
            if (webgis.mapFramework === "leaflet") {
                this.setGeometryType(_geometryType);
            }
        }
        for (var i in _frameworkElements) {
            if (_frameworkElements[i].redraw)
                _frameworkElements[i].redraw();
        }
    };
    this.redrawMarkers = function () {
        for (var i in _vertexFrameworkElements) {
            this.map.frameworkElement.removeLayer(_vertexFrameworkElements[i]);
        }
        var old = _vertexFrameworkElements;
        _vertexFrameworkElements = [];
        for (var v in _vertices) {
            var vertex = _vertices[v];

            var icon = "sketch_vertex";
            if (vertex.fixed === true) {
                icon = "sketch_vertex_fixed"
            }
            else if (vertex.selected === true) {
                icon = "sketch_vertex_selected"
            }

            _vertexFrameworkElements[v] = webgis.createMarker({
                lat: vertex.y, lng: vertex.x,
                draggable: true,
                icon: icon,
                vertex: parseInt(v) + 1
            });
            if (_isLineOrPolygon(_geometryType)) {
                this._addToMap(_vertexFrameworkElements[v]);
                _vertexFrameworkElements[v].sketch_vertex_coords = old[v].sketch_vertex_coords;
                _vertexFrameworkElements[v].sketch = this;
                _vertexFrameworkElements[v].vertex_index = v;
                this._appendMarkerEvents(_vertexFrameworkElements[v]);
            }
        }
    };
    this.resetMarkerDragging = function () {
        if (this._isMarkerDragging) {
            if (_currentBubbleMoveVertextFrameworkElement != null) {
                _currentBubbleMoveVertextFrameworkElement.setLatLng(_currentBubbleMoveVertextFrameworkElement._dragstartLatLng);
                _currentBubbleMoveVertextFrameworkElement = null;
            }
            this._isMarkerDragging = false;
        }
    };
    this._appendMarkerEvents = function (marker) {
        marker.on('dragstart', function () {
            this.sketch._isMarkerDragging = true;
            this._dragstartLatLng = this.getLatLng();
            this._forceRedrawMarkers = this.sketch_vertex_coords.fixed === true;
            this.sketch_vertex_coords.fixed = null;
            this.sketch.hideMoverLine();
            $(this._icon).addClass('dragging');
        }, marker);
        marker.on('dragend', function () {
            var geomType = this.sketch.getGeometryType();
            this.sketch._lastChangeTime = new Date().getTime();
            var newLatLng = this.getLatLng();
            if (!this.sketch.checkMaxBBox({ x: newLatLng.lng, y: newLatLng.lat })) {
                this.setLatLng(this._dragstartLatLng);
                this.sketch.redraw(geomType === 'polyline' || geomType === 'polygon');
                this.sketch._isMarkerDragging = false;
                return;
            }
            this.sketch.addUndo(webgis.l10n.get("sketch-move-vertex"));
            if (this.sketch._moverMarker && this.sketch._moverMarker.snapResult) {
                var result = this.sketch._moverMarker.snapResult.result;
                if (!this.sketch_vertex_coords)
                    this.sketch_vertex_coords = { x: result.x, y: result.y, X: result.X, Y: result.Y, projEvent: 'snapped' };
                else {
                    this.sketch_vertex_coords.x = result.x;
                    this.sketch_vertex_coords.y = result.y;
                    this.sketch_vertex_coords.X = result.X;
                    this.sketch_vertex_coords.Y = result.Y;
                    this.sketch_vertex_coords.projEvent = 'snapped';

                    // Wenn GPS Zeichentool möglich ist, M-Wert zurücksetzen. Das wird in diesem Fall für die GPS Genauigkeit verwendet.
                    // Wenn Punkt verschoben wird, wird die GPS Genauigkeit zurückgesetzt
                    if (this.sketch_vertex_coords.m &&
                        this.sketch && this.sketch.map &&
                        webgis.currentPosition.canUsedWithSketchTool && webgis.currentPosition.canUsedWithSketchTool(this.sketch.map.getActiveTool(), this.sketch)) {
                        //console.log('unset sketch vertex m-value');
                        this.sketch_vertex_coords.m = 0;
                    }
                }
                this.setLatLng({ lng: result.x, lat: result.y });
                this.sketch._moverMarker.snapResult = null;
            }
            else {
                var latlng = this.getLatLng();
                if (!this.sketch_vertex_coords) {
                    this.sketch_vertex_coords = { x: latlng.lng, y: latlng.lat };
                } else {
                    this.sketch_vertex_coords.x = latlng.lng;
                    this.sketch_vertex_coords.y = latlng.lat;
                    this.sketch_vertex_coords.projEvent = '';

                    // Wenn GPS Zeichentool möglich ist, M-Wert zurücksetzen. Das wird in diesem Fall für die GPS Genauigkeit verwendet.
                    // Wenn Punkt verschoben wird, wird die GPS Genauigkeit zurückgesetzt
                    if (this.sketch_vertex_coords.m &&
                        this.sketch && this.sketch.map &&
                        webgis.currentPosition.canUsedWithSketchTool && webgis.currentPosition.canUsedWithSketchTool(this.sketch.map.getActiveTool(), this.sketch)) {
                        //console.log('unset sketch vertex m-value');
                        this.sketch_vertex_coords.m = 0;
                    }
                }
            }

            if (geomType === 'rectangle' && _frameworkElements.length === 1) {
                var rectBounds = _frameworkElements[0].getBounds();
                //console.log('rectBounds', [[this.sketch_vertex_coords.x, this.sketch_vertex_coords.y], [rectBounds.getWest(), rectBounds.getSouth()]]);
                _vertices[0] = { x: this.sketch_vertex_coords.x, y: this.sketch_vertex_coords.y };
                _frameworkElements[0].setBounds([[this.sketch_vertex_coords.y, this.sketch_vertex_coords.x], [rectBounds.getSouth(), rectBounds.getEast()]]);
            }

            this.sketch.redraw($.inArray(geomType, ['polyline', 'polygon', 'distance_circle','compass_rose', 'circle', 'text', 'dimline', 'dimpolygon', 'hectoline']) >= 0);
            this.sketch.events.fire('onchangevertex', this.sketch, this.sketch_vertex_coords);
            this.sketch.events.fire('onchanged', this.sketch);
            this.sketch._isDirty = true;
            this.sketch._isMarkerDragging = false;

            if (this.sketch.snapFixVertex(this.sketch_vertex_coords) || this._forceRedrawMarkers === true) {
                this._forceRedrawMarkers = false;
                this.sketch.redrawMarkers();
            }

            try {
                if (geomType === 'polyline' || geomType === 'polygon') {
                    this.dragging._draggable.options.clickTolerance = webgis.usability.mapSketchClickTolerance;
                }
            } catch (ex) { }

            $(this._icon).removeClass('dragging');
        }, marker);
        marker.on('drag', function () {
            var latlng = this.getLatLng();

            this.sketch._showMoveMarker({ lng: latlng.lng, lat: latlng.lat });
            var vertices = this.sketch.getNeighborVertices(this.vertex_index, true);
            var latLngs = [];

            if (this.sketch._moverMarker && this.sketch._moverMarker.snapResult) {
                var result = this.sketch._moverMarker.snapResult.result;
                latlng = L.latLng(result.y, result.x);
            }
            for (var i in vertices) {
                var vertex = vertices[i];
                if (vertex.index == this.vertex_index) {
                    latLngs.push(latlng);
                } else {
                    latLngs.push({ lng: vertex.x, lat: vertex.y });
                }
            }
            this.sketch.showMoverLine(latLngs);

        }, marker);
        marker.on('click', function (e) {
            // stop propagation
            e.originalEvent.preventDefault();
            var sketch = this.sketch;

            if (webgis.sketch.construct.onMarkerClick(sketch, this) === true) {
                return;
            }

            if (new Date().getTime() - this._creationDate < 1000) {
                //this._map.doubleClickZoom.disable();
                sketch.close();
                webgis.delayed(function (marker) {
                    if (marker && marker._map)
                        marker._map.closePopup();
                    //marker._map.doubleClickZoom.enable();
                }, 10, this);
            } else {
                //console.log('marker-click', sketch.isClosed());
                if (!sketch.isClosed()) {
                    if (sketch.getGeometryType() === "polygon" || sketch.getGeometryType() === "polyline") {
                        if (sketch.getGeometryType() === "polygon" && this.vertex_index === sketch.firstVertexIndexInCurrentPart()) {
                            if (sketch.isInTraceMode() == true) {
                                var latlng = this.getLatLng();
                                sketch.addVertexCoords(latlng.lng, latlng.lat, true);
                            }
                            sketch.close();
                        } else {
                            var latlng = this.getLatLng();
                            sketch.addVertexCoords(latlng.lng, latlng.lat, true);
                        }
                    }
                }
            }
        }, marker);
        marker.on('mouseover', function (e) {
            try {
                var geomType = this.sketch.getGeometryType();
                if (geomType === 'polyline' || geomType === 'polygon') {
                    this.dragging._draggable.options.clickTolerance = webgis.usability.mapClickTolerance;
                }
            } catch (ex) { }
            _suppressMover = true;
        }, marker);
        marker.on('mouseout', function (e) {
            if (!this.sketch._isMarkerDragging) {
                try {
                    var geomType = this.sketch.getGeometryType();
                    if (geomType === 'polyline' || geomType === 'polygon') {
                        this.dragging._draggable.options.clickTolerance = webgis.usability.mapSketchClickTolerance;
                    }
                } catch (ex) { }
                _suppressMover = false;
            }
        }, marker);
        if (webgis.usability.sketchMarkerPopup === true && webgis.usability.contextMenuBubble === false) {  // Nur anzeigen, wenn keine rechte Maus möglich ist (TouchDevice oder custom.js...)
            webgis.bindMarkerPopup(marker, this._menu, function (marker) {
                var sketch = marker ? marker.sketch : null;
                if (!sketch)
                    return "Kein Sketch!";
                var index = marker.vertex_index;
                var $ul = $('#__sketch_context_menu').length > 0 ? $('__sketch_context_menu') : $("<ul id='__sketch_context_menu' style='list-style:none;padding:0px'></ul>").appendTo(document.body);
                var ul = $ul.addClass('webgis-toolbox-tool-item-group-details').css('padding-top', 20).get(0);
                ul.sketch = sketch;
                ul.vertex_index = index;
                $ul.empty();
                $("<li class='webgis-toolbox-tool-item'>Vertex löschen</li>").appendTo($ul)
                    .click(function () {
                        var sketch = this.parentNode.sketch;
                        sketch.removeVertex(this.parentNode.vertex_index);
                    });
                if (_geometryType == 'polyline' || _geometryType == 'polygon' || _geometryType === 'dimline' || _geometryType === 'dimpolygon' || _geometryType === 'hectoline') {
                    if (sketch.hasPrevVertex(index)) {
                        $("<li class='webgis-toolbox-tool-item'>Vorgänger einfügen</li>").appendTo($ul)
                            .click(function () {
                                var sketch = this.parentNode.sketch;
                                sketch.addPrevVertex(this.parentNode.vertex_index);
                            });
                    }
                    if (sketch.hasPostVertex(index)) {
                        $("<li class='webgis-toolbox-tool-item'>Nachfolger einfügen</li>").appendTo($ul)
                            .click(function () {
                                var sketch = this.parentNode.sketch;
                                sketch.addPostVertex(this.parentNode.vertex_index);
                            });
                    }
                    $("<li class='webgis-toolbox-tool-item'>Sketch löschen</li>").appendTo($ul)
                        .click(function () {
                            var sketch = this.parentNode.sketch;
                            sketch.remove();
                        });
                    $("<li class='webgis-toolbox-tool-item'>Vertex Reihenfolge umkehren</li>").appendTo($ul)
                        .click(function () {
                            var sketch = this.parentNode.sketch;
                            sketch.swapDirection();
                        });
                    $("<li class='webgis-toolbox-tool-item'>Neuen Abschnitt beginnen</li>").appendTo($ul)
                        .click(function () {
                            var sketch = this.parentNode.sketch;
                            sketch.appendPart();
                        });
                }
                var icons = ["sketch_vertex_bottom", "sketch_vertex_circle", "sketch_vertex_square"];
                var $markerIcons = $("<li class='webgis-toolbox-tool-item'></li>").appendTo($ul);
                for (var i in icons) {
                    $("<div><img src='" + webgis.markerIcons[icons[i]].url(1) + "'/></div>").css({ display: 'inline-block', width: 32, height: 32, marginRight: '6px ' }).data('marker', icons[i]).appendTo($markerIcons)
                        .click(function () {
                            webgis.markerIcons["sketch_vertex"] = webgis.markerIcons[$(this).data('marker')];
                            var sketch = this.parentNode.parentNode.sketch;
                            sketch.redrawMarkers();
                        });
                }
                return ul;
            });
        }
        marker._creationDate = new Date().getTime();
    };
    this.setMarkerTooltip = function (vertex, text) {
        if (webgis.mapFramework == "leaflet") {
            try {
                var elements = this.getGeometryType() == 'point' ? _frameworkElements : _vertexFrameworkElements;
                //console.log(vertex);
                if (vertex.x && vertex.y) {
                    for (var v in elements) {
                        var element = elements[v];
                        var latLng = element.getLatLng();
                        //console.log(Math.abs(latLng.lng - vertex.x) + " " + Math.abs(latLng.lat - vertex.y));
                        if (Math.abs(latLng.lng - vertex.x) < 1e-7 && Math.abs(latLng.lat - vertex.y) < 1e-7) {
                            element.bindTooltip(text).openTooltip();
                        }
                    }
                }
                else {
                    if (elements.length > vertex) {
                        var element = elements[vertex];
                        element.bindTooltip(text).openTooltip();
                        //webgis.bindMarkerPopup(element, popup);
                    }
                }
            }
            catch (e) { }
        }
    };
    this.addVertices = function (vertices, fireEvents, readOnly) {
        if (_frameworkElements == null ||
            this.map == null ||
            _frameworkElements.length < _currentFrameworkIndex ||
            !vertices ||
            this.isEditable() === false) {
            return;
        }

        var isValid = this.isValid();

        //console.log('addvertices', vertices);

        for (var v = 0, to = vertices.length; v < to; v++) {
            var vertex = vertices[v];
            if (!vertex || !vertex.x || !vertex.y)
                continue;
            if (!this.checkMaxBBox(vertex))
                break;
            if (_geometryType === 'point') {
                //_vertices = [];
                //_partIndex = [0];
                _vertices.splice(0, _vertices.length);  // clear
                _partIndex.splice(0, _partIndex.length);  // clear -> keep reference
                _partIndex.push(0);
                _currentFrameworkIndex = 0;
            }

            if ((_geometryType === 'distance_circle' || _geometryType === 'compass_rose' || _geometryType === 'circle') && _vertices.length > 0) {
                if (/*this.map.calcCrs() &&*/ _frameworkElements.length === 1) {
                    //var p1 = webgis.fromWGS84(this.map.calcCrs(), L.latLng(vertices[vertices.length - 1].y, vertices[vertices.length - 1].x));
                    //var p2 = webgis.fromWGS84(this.map.calcCrs(), L.latLng(_vertices[0].y, _vertices[0].x));

                    //var radius = Math.round(Math.sqrt((p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y)) * 10) / 10;

                    var vertex0 = _vertices[0], vertex1 = vertices[vertices.length - 1], radius = 0;
                    if (vertex0.hasOwnProperty('X') && vertex0.hasOwnProperty('Y') && vertex1.hasOwnProperty('X') && vertex1.hasOwnProperty('Y')) {
                        radius = Math.sqrt((vertex1.X - vertex0.X) * (vertex1.X - vertex0.X) + (vertex1.Y - vertex0.Y) * (vertex1.Y - vertex0.Y));
                    } else {
                        var latLng1 = L.latLng(vertices[vertices.length - 1].y, vertices[vertices.length - 1].x);
                        var latLng2 = L.latLng(_vertices[0].y, _vertices[0].x);

                        radius = latLng1.distanceTo(latLng2);
                    }

                    _frameworkElements[0].setRadius(radius);

                    if (_geometryType === 'distance_circle') {
                        _distance_circle_radius = radius === 0 ? _distance_circle_radius : radius;
                        // do not close sketch => sonst kann man nachträglich Radius und steps nicht mehr setzen. Wenn man den mover nicht mehr einblenden will ist es besser dafür einen eigene Eigenschaft zu machen (zB this.showMover...)
                    } else if (_geometryType === 'compass_rose') {
                        _compass_rose_radius = radius === 0 ? _compass_rose_radius : radius;
                    } else {
                        _circle_radius = radius === 0 ? _circle_radius : radius;
                        //this.close();
                        //this.hideMoverLine();
                    }
                    this.map.ui.refreshUIElements();
                }
            }
            else if (_geometryType === 'rectangle') {
                var rectBounds = [[0, 0], [0, 0]];
                if (_vertices.length === 0) {
                    _vertices.push(vertex);
                    rectBounds = [[_vertices[0].y, _vertices[0].x], [_vertices[0].y, _vertices[0].x]];

                    _vertexFrameworkElements[_vertices.length - 1] = webgis.createMarker({
                        lat: vertex.y, lng: vertex.x, draggable: true, icon: "sketch_vertex", vertex: _vertices.length
                    });
                    this._addToMap(_vertexFrameworkElements[_vertices.length - 1]);
                    _vertexFrameworkElements[_vertices.length - 1].sketch_vertex_coords = coords;
                    _vertexFrameworkElements[_vertices.length - 1].sketch = this;
                    _vertexFrameworkElements[_vertices.length - 1].vertex_index = _vertices.length - 1;
                    this._appendMarkerEvents(_vertexFrameworkElements[_vertices.length - 1]);
                }
                else if (_vertices.length === 1) {
                    _vertices.push(vertex);
                    rectBounds = [[_vertices[0].y, _vertices[0].x], [_vertices[1].y, _vertices[1].x]];
                }
                else if (_vertices.length > 1) {
                    _vertices[1].y = vertex.y, _vertices[1].x = vertex.x;
                    rectBounds = [[_vertices[0].y, _vertices[0].x], [_vertices[1].y, _vertices[1].x]];
                }
                _frameworkElements[0].setBounds(rectBounds);
            }
            else {
                var coords = vertex;
                if (fireEvents) {
                    this.events.fire('beforeaddvertex', this, coords);
                }

                if (_geometryType === 'text') {
                    _vertices = [coords];
                    if (_vertexFrameworkElements.length > 0) {
                        this.map.frameworkElement.removeLayer(_vertexFrameworkElements[0]);
                    }
                } else {
                    _vertices.push(coords);
                    this.snapFixVertex(coords);
                }
                var lastVertex = null;

                if (this.isInFanMode() && _vertices.length > 1) {
                    _frameworkElements.push(this._createFrameworkElement());
                    _currentFrameworkIndex = _frameworkElements.length - 1;
                    this.map.frameworkElement.addLayer(_frameworkElements[_currentFrameworkIndex]);
                    _frameworkElements[_currentFrameworkIndex].addLatLng(L.latLng(_vertices[0].y, _vertices[0].x));
                }

                if (webgis.mapFramework === "leaflet") {
                    if (_frameworkElements[_currentFrameworkIndex].addLatLng) {
                        _frameworkElements[_currentFrameworkIndex].addLatLng(L.latLng(vertex.y, vertex.x));
                    }
                    else {
                        _frameworkElements[_currentFrameworkIndex].setLatLng(L.latLng(vertex.y, vertex.x));
                        _frameworkElements[_currentFrameworkIndex].sketch_vertex_coords = coords;
                    }

                    if (!readOnly && !this.isReadOnly()) {    
                        if (_geometryType === 'polyline'
                            || _geometryType === 'polygon'
                            || (_geometryType === 'freehand' && this._addFreehandVertexMarker() === true)
                            || _geometryType === 'text'
                            || _geometryType === 'rectangle'
                            || _geometryType === 'dimline'
                            || _geometryType === 'dimpolygon'
                            || _geometryType === 'hectoline'
                            || ((_geometryType === 'distance_circle' || _geometryType === 'compass_rose' || _geometryType === 'circle') && _vertices.length === 1)
                        ) {
                            var icon = "sketch_vertex";
                            if (_geometryType === 'text') {
                                icon = "sketch_vertex_text";
                            }
                            else if (_vertices[_vertices.length - 1].fixed === true) {
                                icon = "sketch_vertex_fixed";
                            }
                            else if (_vertices[_vertices.length - 1].selected === true) {
                                icon = "sketch_vertex_selected";
                            }

                            _vertexFrameworkElements[_vertices.length - 1] = webgis.createMarker({
                                lat: vertex.y,
                                lng: vertex.x,
                                draggable: true,
                                icon: icon,
                                vertex: _vertices.length
                            });
                            this._addToMap(_vertexFrameworkElements[_vertices.length - 1]);

                            _vertexFrameworkElements[_vertices.length - 1].sketch_vertex_coords = coords;
                            _vertexFrameworkElements[_vertices.length - 1].sketch = this;
                            _vertexFrameworkElements[_vertices.length - 1].vertex_index = _vertices.length - 1;

                            this._appendMarkerEvents(_vertexFrameworkElements[_vertices.length - 1]);
                        }
                    }
                }
            }
            if (fireEvents) {
                this.events.fire('onvertexadded', this, coords);
            }


        }

        this.events.fire('onchanged', this);
        this._isDirty = true;

        if (isValid === false && this.isValid() === true) {
            this.events.fire('ongetsvalid', this);
        }

        if (_ortho && this.getCurrentPartLength() >= 2) {
            var vertices = this.getConstructionVerticesPro();
            var v1 = vertices[vertices.length - 2], v2 = vertices[vertices.length - 1];
            var dx = v2.X - v1.X, dy = v2.Y - v1.Y, len = Math.sqrt(dx * dx + dy * dy);
            if (len > 0) {

                this._fixedDirectionVector = { X: dy / len, Y: -dx / len };
                this._fixedDistance = null;
            }
        } else if (this.partCount() > 1) { 
            // keep current _fixedDirectionVector for new parts => do nothing
        } else {
            this._fixedDirectionVector = null;
        }
        //this._getZValues();
    };
    this._addFreehandVertexMarker = function () {
        if (_vertices.length === 1)
            return true;

        if (_vertices.length % 10 === 0)
            return true;

        if (_vertices.length>0 && _vertices.length === _lastFreehandVertexIndex + 1)
            return true;

        return false;
    };

    this.hideMoverLine = function () {
        if (this._moverLine) {
            this.map.frameworkElement.removeLayer(this._moverLine);
            this._moverLine = null;
        }
    };
    this.showMoverLine = function (latLngs) {
        if (this._moverLine == null) {
            this._moverLine = L.polyline(latLngs, { color: 'green', weight: 1 });
            this._addToMap(this._moverLine);
        }
        else {
            this._moverLine.setLatLngs(latLngs);
        }
    };
    this.hideMoverMarker = function () {
        if (this._moverMarker) {
            this.map.frameworkElement.removeLayer(this._moverMarker);
            this._moverMarker = null;
        }
    };
    this.hideTraceLine = function () {
        if (_traceLine != null) {
            this.map.frameworkElement.removeLayer(_traceLine);
            _traceLine = null;
        }
    };
    this.hideConstructMover = function () {
        if (this._constructMover) {
            this.map.frameworkElement.removeLayer(this._constructMover);
            this._constructMover = null;
        }
    };

    this.addVertex = function (vertex) {
        if (_currentBubbleMoveVertextFrameworkElement) {
            this.endBubbleMoveVertex();
            return;
        }

        if (webgis.sketch.construct.onAddVertex(this, vertex) === true) {
            return;
        }

        if (_frameworkElements == null || this.map == null || _frameworkElements.length < _currentFrameworkIndex) {
            return;
        }

        if (!vertex || !vertex.x || !vertex.y) {
            return;
        }

        // iPad Safari Bug: nach dem dragend, gibts an und zu auch ein Click event drauf...
        var time = new Date().getTime();

        if (Math.abs(time - this._lastChangeTime) < 100) {
            return;
        }

        var addAddVertexUndo = function (sketch, geometryType) {
            if (geometryType === 'polygon' || geometryType === 'polyline' || geometryType === 'dimline' || geometryType === 'dimpolygon' || geometryType === 'hectoline') {
                sketch.addUndo(webgis.l10n.get("sketch-add-vertex"));
            }
        }

        if ((_isPolygon() && webgis.usability.sketch.checkForOverlappingPolygonSegments === true) ||
            (_isLine() && webgis.usability.sketch.checkForOverlappingPolylineSegments === true)) {
            var currentVertex = this.lastVertexInCurrentPart(0);
            var prevVertex = this.lastVertexInCurrentPart(1);

            if (currentVertex && prevVertex) {

                if (webgis.calc.isParallel(prevVertex, currentVertex, currentVertex, vertex) == -1) {
                    console.log('overlays with prev segment');

                    var me = this;
                    webgis.confirm({
                        title: webgis.l10n.get('sketch-overlapping-segments'),
                        message: webgis.l10n.get('sketch-overlapping-segments-message'),
                        iconUrl: webgis.baseUrl + '/content/api/img/' + (_isPolygon() ? 'polygon' : 'polyline') + '-overlapping-segments.png',
                        okText: webgis.l10n.get('sketch-overlapping-segements-remove-prev'),
                        onOk: function () {
                            addAddVertexUndo(me, _geometryType);
                            me.removeVertex(_vertices.length - 1, true);
                            me.addVertices([vertex], true);
                        },
                        cancelText: webgis.l10n.get('cancel')
                    });

                    return;
                }
            }
            //console.log('addVertex', this.projectCoord(vertexCopy), _vertices);
        } 

        addAddVertexUndo(this, _geometryType);

        this.addVertices([vertex], true);
    };
    this.addVertexCoords = function (x, y, useSnappedVertex) {
        if (useSnappedVertex && this._snappedVertex) {
            var firstNode = true;
            if (_trace && _vertices.length > 0) {
                var lastVertex = _vertices[_vertices.length - 1];
                var lastNodeIndex = map.construct.getTopVertexIndex(lastVertex.X, lastVertex.Y);
                var currentNodeIndex = map.construct.getTopVertexIndex(this._snappedVertex.X, this._snappedVertex.Y);
                if (lastNodeIndex >= 0 && currentNodeIndex >= 0) {
                    var result = map.construct.traceNodes(lastNodeIndex, currentNodeIndex);
                    if (result) {
                        for (var i in result) {
                            if (result[i] == lastNodeIndex || result[i] == currentNodeIndex)
                                continue;
                            var vertex = map.construct.getTopoVertex(result[i]), vertex_wgs84 = map.construct.getTopoVertexWGS84(result[i]);
                            if (vertex && vertex_wgs84) {
                                if (firstNode) {
                                    firstNode = false;
                                    this.addUndo(webgis.l10n.get("sketch-add-trace-vertices"));
                                    this.startSuppressUndo();
                                }
                                this.addVertex({ x: vertex_wgs84[0], y: vertex_wgs84[1], X: vertex[0], Y: vertex[1], projEvent: 'snapped' });
                            }
                        }
                    }
                }
            }
            var result = this._snappedVertex;

            if (this.isSketchMoving() === true) {
                this.moveSketch({ lng: result.x, lat: result.y });
            }
            else if (this.isSketchRotating() === true) {
                this.rotateSketch({ lng: result.x, lat: result.y });
            }
            else {
                this.addVertex({ x: result.x, y: result.y, X: result.X, Y: result.Y, projEvent: 'snapped' });
            }
            if (!firstNode)
                this.stopSuppressUndo();

            if (!_ortho) {
                this._fixedDirectionVector = null;
            }
            this._fixedDistance = null;
            this._snappedVertex = null;
        }
        else {
            if (this.isSketchMoving() === true) {
                this.moveSketch({ lng: x, lat: y });
            }
            else if (this.isSketchRotating() === true) {
                this.rotateSketch({ lng: x, lat: y });
            }
            else {
                this.addVertex({ x: x, y: y });
            }
        }
    };
    this.addOrSetCurrentContextVertex = function (vertex, cancelConstruction) {
        var vertexIndex = this._contextVertexIndex !== null ? parseInt(this._contextVertexIndex) : null;
        this._contextVertexIndex = null;

        if (cancelConstruction === true) {
            webgis.sketch.construct.cancel(this);
        }

        if (vertexIndex !== null) {
            var vertices = this._getRawVertices();
            if (vertices.length > vertexIndex) {
                this.addUndo(webgis.l10n.get("sketch-move-vertex"));

                if (!vertex.X || !vertex.Y) {
                    // if X,Y not set, eg after changing an existing point with Absolute XY Tool
                    // fire this event. This will cause an projection from xy => XY
                    // otherwise "undefined" value will cause, that the feature can not be parsed
                    // and saved (editing)

                    //console.log('vertex', vertex);
                    this.events.fire('onchangevertex', this, vertex);
                    //console.log('vertex', vertex);
                }

                var existingVertex = vertices[vertexIndex];
                existingVertex.x = vertex.x;
                existingVertex.y = vertex.y;
                existingVertex.X = vertex.X;
                existingVertex.Y = vertex.Y;
                existingVertex.projEvent = vertex.projEvent;

                this.redraw(true);
                this.redrawMarkers();
            }

            return false;  // modified
        } else {
            this.addVertex(vertex);

            return true;   // added
        }
    };

    this.originalSrs = function () {
        return _originalSrs;
    };
    this.originalSrsP4Params = function () {
        return _originalSrsP4Params;
    };
    this.unsetOriginalSrs = function () {
        _originalSrs = 0;
        _originalSrsP4Params = null;
    };
    this.tryGetSketchSrs = function () {
        if (_vertices) {
            for (var v in _vertices) {
                if (_vertices[v].srs) {
                    return _vertices[v].srs
                }
            };
        }
        if (_originalSrs) {
            return _originalSrs;
        }
        return this.map.calcCrsId();
    };
    
    /***** Snapping, Construct, Contextmenu ********/
    this._isConstructContextMenuVisible = false;

    var mapOnRightClick = function (e, map, clickEvent) {
        if (map.toolSketch() !== this || webgis.usability.sketchContextMenu === false) {
            return;
        }

        this.ui.showContextMenu(clickEvent);
       
        this._isConstructContextMenuVisible = true;
    };
    var mapOnZoomEnd = function (e, map) {
        if (_currentBubbleMoveVertextFrameworkElement !== null) {
            _currentBubbleMoveVertextFrameworkElement = null;
            this._isMarkerDragging = false;
            map.ui.setClickBubbleLatLng(null); // Park
        }
    };
    this.map.events.on('rightclick', mapOnRightClick, this);
    this.map.events.on(['moveend', 'zoomend'], mapOnZoomEnd, this);
    this._mapMousePressed = false;

    this.onMapMousedown = function (map, e) {
        if (e && e.originalEvent && e.originalEvent.button === 0) {  // Left button
            //if (_geometryType === 'freehand')
            //    map._enableBBoxToolHandling();

            this._mapMousePressed = true;
        }
    };
    this.onMapMouseup = function (map, e) {
        if (e && e.originalEvent && e.originalEvent.button === 0) {  // Left button
            if (_geometryType === 'freehand') {
                //map._disableBBoxToolHandling();
                this.close();
            }
            this._mapMousePressed = false;
        }
    };

    var _suppressMover = false;
    this.onMapMouseMove = function (map, latlng) {
        var hasTraceResult = false;

        if (this._isMarkerDragging === true) {
            if (_currentBubbleMoveVertextFrameworkElement !== null && latlng != null) {
                _currentBubbleMoveVertextFrameworkElement.setLatLng(latlng);
                _currentBubbleMoveVertextFrameworkElement.fire('drag');
            }
            return;
        }

        if (this._isConstructContextMenuVisible === true) {
            return;
        }

        if (!webgis.sketch.construct.suppressSnapping(this)) {
            this._showMoveMarker(latlng);
        }

        if (!latlng || this.isEditable() === false || this.isReadOnly() === true) {
            this.hideMoverLine();
            this.hideMoverMarker();
            this.hideConstructMover();
            this.hideTraceLine();
            return;
        }

        if (webgis.sketch.construct.onMapMouseMove(this, latlng)) {
            return;
        }

        if (_geometryType === 'distance_circle' || _geometryType === 'compass_rose' || _geometryType === 'circle') {
            if (this.isClosed()) {
                return;
            }
            if (_vertices.length > 0) {
                var latLng1 = latlng;
                var latLng2 = L.latLng(_vertices[0].y, _vertices[0].x);

                var r = latLng1.distanceTo(latLng2);

                //console.log('radius', r);

                var radius = latLng1.distanceTo(latLng2);

                if (this._moverLine === null || !this._moverLine.setRadius) {
                    this.hideMoverLine();

                    this._moverLine = L.circlePro([L.latLng(_vertices[0].y, _vertices[0].x)], { color: 'green', weight: 1, fillColor: 'none' });
                    this._moverLine.setCirclePoint(latlng);

                    this._addToMap(this._moverLine);
                }
                else {
                    this._moverLine.setCirclePoint(latlng);
                }
            }
            return;
        }

        if (_geometryType === 'rectangle') {
            if (this.isClosed()) {
                return;
            }
            if (_vertices.length > 0) {
                var rectBounds = [[latlng.lat, latlng.lng], [_vertices[0].y, _vertices[0].x]];

                if (this._moverLine === null || !this._moverLine.setBounds) {
                    this.hideMoverLine();
                    this._moverLine = L.rectangle(rectBounds, { color: 'green', weight: 1, fillColor: 'none' });
                    this._addToMap(this._moverLine);
                }
                else {
                    this._moverLine.setBounds(rectBounds);
                }
            }
            return;
        }

        var constructionMode = webgis.sketch.construct.getConstructionMode(this);
        if (constructionMode != null && constructionMode != 'normal') {
            return;
        }
        

        if (this._mapMousePressed && this.isClosed() === false && this.isReadOnly() === false && _geometryType === 'freehand') {
            this.addVertex({ x: latlng.lng, y: latlng.lat });
            return;
        }

        var latLngs = [], ll = this._snappedVertex ? { lng: this._snappedVertex.x, lat: this._snappedVertex.y } : { lat: latlng.lat, lng: latlng.lng };
        var referenceVertex = this._referenceVertex();

        if (referenceVertex) {
            if (this.getCurrentPartLength() === 1 && _ortho === true) {
                var p1 = this._referenceVertex(), p0 = this._snappedVertex || this.map.construct.toProjectedVertex(this.map.calcCrs(), latlng);
                var dx = p1.x - p0.x, dy = p1.y - p0.y, len = Math.sqrt(dx * dx + dy * dy);
                dx /= len;
                dy /= len;

                var vec = this._fixedDirectionVector ?
                    { X: this._fixedDirectionVector.X, Y: this._fixedDirectionVector.Y } :  // current
                    { X: 1.0, Y: 0.0 };  // x-axis

                // cross product (mouse x vec)  
                var w1 = dx * vec.Y - dy * vec.X;
                // cross product (mouse x orthogonal(vec))
                var w2 = dx * (-vec.X) - dy * vec.Y;

                //console.log(dx, dy, w1, w2);

                // compare cross products (sin(alpah))
                if (Math.abs(w1) <= Math.abs(w2)) {
                    this._fixedDirectionVector = { X: vec.X, Y: vec.Y };  // take vector
                } else {
                    this._fixedDirectionVector = { X: -vec.Y, Y: vec.X };  // take ortogonal vector
                }
            }
            if (this._fixedDirectionVector) {
                var p1 = this._referenceVertex(), p0 = this._snappedVertex || this.map.construct.toProjectedVertex(this.map.calcCrs(), latlng);
                var p = this.map.construct.projectOrthogonal(p0, p1, this._fixedDirectionVector);
                if (p) {
                    ll = { lng: p.x, lat: p.y };
                    this._snappedVertex = p;
                }
            }
            if (this._fixedDistance > 0) {
                var p1 = this._referenceVertex(), p0 = this._snappedVertex || this.map.construct.toProjectedVertex(this.map.calcCrs(), latlng);
                var p = this.map.construct.projectLength(p0, p1, this._fixedDistance);
                if (p) {
                    ll = { lng: p.x, lat: p.y };
                    this._snappedVertex = p;
                }
            }

            latLngs.push([referenceVertex.y, referenceVertex.x]);
            latLngs.push([ll.lat, ll.lng]);
        }

        if (_geometryType === 'polyline' || _geometryType === 'linestring' || _geometryType === 'polygon' || _geometryType === 'dimline' || _geometryType === 'dimpolygon' || _geometryType === 'hectoline') {
            if (!this.isClosed()) {
                var traceLatLngs = [], lastNodeIndex = null, currentNodeIndex = null;

                if (_trace &&
                    _vertices.length > 0 &&
                    this._snappedVertex &&
                    this.isSketchMoving() === false &&
                    this.isSketchRotating() === false) {

                    var lastVertex = _vertices[_vertices.length - 1];
                    var lastNodeIndex = map.construct.getTopVertexIndex(lastVertex.X, lastVertex.Y);
                    var currentNodeIndex = map.construct.getTopVertexIndex(this._snappedVertex.X, this._snappedVertex.Y);
                    //console.log('last', lastVertex, lastNodeIndex)
                    //console.log('current', this._snappedVertex, currentNodeIndex);
                    if (lastNodeIndex >= 0 && currentNodeIndex >= 0) {
                        var result = map.construct.traceNodes(lastNodeIndex, currentNodeIndex);
                        //console.log('trace vertices', lastNodeIndex, currentNodeIndex);
                        if (result) {
                            //console.log('trace-result', result);
                            hasTraceResult = true;
                            for (var i in result) {
                                var vertex = map.construct.getTopoVertexWGS84(result[i]);
                                if (vertex)
                                    traceLatLngs.push([vertex[1], vertex[0]]);
                            }
                        }
                    }
                }

                if (traceLatLngs.length > 0) {
                    latLngs = traceLatLngs;
                }

                if (hasTraceResult) {
                    if (latLngs.length > 2) {
                        //this._moverMarkers = [];
                        //for (var i = 1; i < latLngs.length - 1; i++) {
                        //    this._moverMarkers[i] = webgis.createMarker({
                        //        lat: latLngs[i][0], lng: latLngs[i][1], icon: 'sketch_vertex', vertex: 0
                        //    });
                        //    this._addToMap(this._moverMarkers[i]);
                        //}
                    }

                    if (_traceLine == null) {
                        _traceLine = L.polyline(latLngs, { color: 'blue', weight: 3 });
                        this._addToMap(_traceLine);
                    }
                    else {
                        _traceLine.setLatLngs(latLngs);
                    }
                }
                else {
                    if (_traceLine != null) {
                        this.map.frameworkElement.removeLayer(_traceLine);
                        _traceLine = null;
                    }
                }

                if (_isPolygon() && _vertices.length >= 2 && !this.isSketchMoving() && !this.isSketchRotating()) {
                    latLngs.push([_vertices[_partIndex[_partIndex.length - 1]].y, _vertices[_partIndex[_partIndex.length - 1]].x]);
                }
            }

            if (!this.isClosed() || this.isSketchMoving() || this.isSketchRotating() && _suppressMover !== true) {
                this.showMoverLine(latLngs);

                if (_sketchProperties && _sketchProperties.maxBBox && !this.isSketchMoving() && !this.isSketchRotating()) {
                    var bbox = this.getBounds(true, latlng);
                    if (bbox.width > _sketchProperties.maxBBox.width ||
                        bbox.height > _sketchProperties.maxBBox.height) {
                        this._moverLine.setStyle({ color: 'red' });
                    }
                    else {
                        this._moverLine.setStyle({ color: 'green' });
                    }
                }
            }

            if (this.isSketchMoving() === true) {
                this.moveSketchPreview({ lat: latLngs[1][0], lng: latLngs[1][1] });
            }
            else if (this.isSketchRotating() === true) {
                this.rotateSketchPreview({ lat: latLngs[1][0], lng: latLngs[1][1] });
            }
        }
        else {
            this.hideMoverLine();
            this.hideTraceLine();
        }

        this.fireCurrentState();
    };
    this.onMapKeyDown = function (map, e) {
        if (!webgis.usability.allowSketchShortcuts) {
            return;
        }

        var $focus = $(":focus");
        if ($focus.hasClass('webgis-input') ||
            $focus.hasClass('webgis-textarea') ||
            $focus.hasClass('webgis-search-input') ||
            $focus.hasClass('webgis-autocomplete')) {
            return false;
        }

        var removeMoverLine = false;

        switch (e.key.toLocaleLowerCase()) {
            case 'z':
                if (e.ctrlKey === true) {  // Undo
                    this.undo();
                }
                break;
            case 'o':
                if (e.ctrlKey === false) {
                    this.toggleOrthoMode();
                }
                break;
            case 't':
                if (e.ctrlKey === false) {
                    this.toggleTraceMode();
                }
                break;
            case 'n':  // s is reserved for ctrl+s => save in editing
                if (e.ctrlKey === false) {
                    webgis.tools.onButtonClick(this.map, { type: 'clientbutton', command: 'snapping' });
                }
                break;
            case 'f':
                if (e.ctrlKey === false) {
                    if (!_isLineOrPolygon())
                        break;

                    if (!this.isLastPartClosed()) {
                        this.appendPart();
                        removeMoverLine = true;
                    }
                }
                break;
            default:
                removeMoverLine = webgis.sketch.construct.onMapKeyDown(this, e) === true;
                break;
        }

        if (removeMoverLine) {
            this.hideMoverLine();
        }
    };
    this.onMapKeyUp = function (map, e) {
        webgis.sketch.construct.onMapKeyUp(this, e);
    };

    this._showMoveMarker = function (latlng) {
        if (!latlng) {
            this.hideConstructMover();
            return;
        }
        if (!this.map.construct.hasSnapping(this) || this.map.toolSketch() != this) {
            if (this._moverMarker != null) {
                this.map.frameworkElement.removeLayer(this._moverMarker);
                this._moverMarker = null;
            }
            this._snappedVertex = null;
            return;
        }
        var snapResult = this.map.construct.performSnap(latlng.lng, latlng.lat, null, this);
        if (snapResult != null) {
            //console.log("snapResult", snapResult);
            latlng.lng = snapResult.result.x;
            latlng.lat = snapResult.result.y;
            this._snappedVertex = snapResult.result;
        }
        else {
            this._snappedVertex = null;
        }
        if (this._moverMarker == null) {
            this._moverMarker = webgis.createMarker({
                lat: latlng.lat, lng: latlng.lng, draggable: false, icon: "sketch_mover"
            });
            this._moverMarker.setZIndexOffset(-1000);
            this._addToMap(this._moverMarker);
        }
        else {
            this._moverMarker.setLatLng(latlng);
        }
        this._moverMarker.snapResult = snapResult;
        if (snapResult) {
            //this._moverMarker.bindTooltip(snapResult.type + ": " + snapResult.meta.name).openTooltip();
            if (snapResult.vertices && snapResult.vertices.length == 2) {
                var v1 = this.map.construct.getTopoVertexWGS84(snapResult.vertices[0]);
                var v2 = this.map.construct.getTopoVertexWGS84(snapResult.vertices[1]);
                if (v1 && v2) {
                    var latLngs = [[v1[1], v1[0]], [v2[1], v2[0]]];
                    if (this._constructMover == null) {
                        this._constructMover = L.polyline(latLngs, {
                            color: 'yellow', weight: 2
                        });
                        this._addToMap(this._constructMover);
                    }
                    else {
                        this._constructMover.setLatLngs(latLngs);
                    }
                }
            }
            else {
                this.hideConstructMover();
            }

            this.fireCurrentState();
        }
        else {
            //this._moverMarker.unbindTooltip();
            this.hideConstructMover();
        }
    };

    this._fixedDirectionVector = null;
    this._fixedDistance = null;
    this._snappedVertex = null;
    this._closestVertex = function (map, pos) {
        var index = -1, dist = 0;
        for (var v in _vertices) {
            var vertex = _vertices[v];
            var point = map.frameworkElement.latLngToLayerPoint(L.latLng(vertex.y, vertex.x));
            var d = (point.x - pos.x) * (point.x - pos.x) + (point.y - pos.y) * (point.y - pos.y);
            if (index < 0 || d < dist) {
                index = v;
                dist = d;
            }
        }
        if (index >= 0) {
            return {
                index: index,
                vertex: _vertices[index],
                dist: Math.sqrt(dist)
            };
        }
        return null;
    };
    this._closestEdge = function (map, pos) {
        if (_isLineOrPolygon(_geometryType) && _vertices.length >= 2) {
            var index = -1, dist = 0, t = .5, pIndex = 1, to = _vertices.length - 1, currentPartIndex = 0;
            if (_geometryType === 'polygon')
                to++;
            for (var i = 0; i < to; i++) {
                var v1 = _vertices[i], v2 = null;
                var nextPartIndex = (i + 1) >= _vertices.length ? currentPartIndex + 1 : this.vertexPartIndex(i + 1);
                if (nextPartIndex > currentPartIndex) {
                    if (_geometryType === 'polygon') {
                        //console.log('last partVertex: ('+currentPartIndex+') ' + _partIndex[currentPartIndex]);
                        v2 = _vertices[_partIndex[currentPartIndex]];
                    }
                    currentPartIndex = nextPartIndex;
                }
                else {
                    v2 = _vertices[i + 1];
                }
                if (!v2)
                    continue;
                var p1 = map.frameworkElement.latLngToLayerPoint(L.latLng(v1.y, v1.x)), p2 = map.frameworkElement.latLngToLayerPoint(L.latLng(v2.y, v2.x));
                var rx = p2.x - p1.x, ry = p2.y - p1.y;
                var lx = pos.x - p1.x, ly = pos.y - p1.y;
                var linEq = new webgis.calc.linearEquation2(lx, ly, rx, -ry, ry, rx);
                if (linEq.solve()) {
                    var t1 = linEq.var1();
                    if (t1 >= 0 && t1 <= 1.0) {
                        var p = {
                            x: p1.x + t1 * rx,
                            y: p1.y + t1 * ry
                        };
                        var d = (pos.x - p.x) * (pos.x - p.x) + (pos.y - p.y) * (pos.y - p.y);
                        if (index < 0 || d < dist) {
                            dist = d;
                            index = i;
                            t = t1;
                        }
                    }
                }
            }
            if (index >= 0) {
                return {
                    index: index,
                    t: t,
                    dist: Math.sqrt(dist)
                };
            }
        }
        return null;
    };

    this.canReverse = function () {
        return _isLine(_geometryType) && _vertices.length > 1 && _partIndex.length === 1;
    };
    this.reverse = function () {
        if (this.canReverse()) {
            this.addUndo(webgis.l10n.get("sketch-tool-reverse"));

            var vertices = _vertices;
            vertices.reverse();

            _vertices = [];
            this.remove(true);
            this.addVertices(vertices);
        }
    };

    this.canMergeParts = function () {
        if (this.map.construct && _geometryType == 'polyline' && _partIndex.length > 1) {
            return this.map.construct.mergePaths(this.getParts()) != null;
        }
        return false;
    };
    this.mergeParts = function () {
        if (this.canMergeParts()) {
            var vertices = this.map.construct.mergePaths(this.getParts());

            if (vertices != null) {
                //console.log('merged', vertices);
                this.addUndo(webgis.l10n.get("sketch-merge-sections"));

                _vertices = [];
                this.remove(true);
                this.addVertices(vertices);
            }
        }
    };

    this.infoTextLines = function () {
        var properties = this.calcProperties("X", "Y");

        var infoText = [];

        if (_partIndex.length > 1) {
            infoText.push("Multipart: " + _partIndex.length + " " + webgis.l10n.get("sections"));
        }

        if (_geometryType === 'polygon') {
            infoText.push(webgis.l10n.get("area") + ": " + (Math.round(properties.area * 100) / 100) + 'm²');
            infoText.push(webgis.l10n.get("circumference") + ": " + (Math.round(properties.circumference * 100) / 100) + 'm');
        }
        else if (_geometryType === 'polyline') {
            infoText.push(webgis.l10n.get("length") + ": " + (Math.round)(properties.length * 100) / 100 + 'm');
        }

        return infoText;
    };

    /******* Current State *************/
    this.fireCurrentState = function () {

        var args = {
            geometryType: _geometryType
        };

        if (this._moverLine !== null && this.map && this.map.calcCrs()) {
            var latLngs = this._moverLine.getLatLngs();
            if (latLngs.length >= 2) {
                //var p0 = webgis.fromWGS84(this.map.calcCrs(), latLngs[0].lat, latLngs[0].lng);
                //var p1 = webgis.fromWGS84(this.map.calcCrs(), latLngs[1].lat, latLngs[1].lng);
                //args.segmentLength = Math.sqrt((p1.x - p0.x) * (p1.x - p0.x) + (p1.y - p0.y) * (p1.y - p0.y));
                //args.segmentAzimut = Math.atan2(p1.x - p0.x, p1.y - p0.y);

                // immer mit webgis.calc berechnen, damit die projektion passt...
                var coords = [{ x: latLngs[0].lng, y: latLngs[0].lat }, { x: latLngs[1].lng, y: latLngs[1].lat }];
                args.segmentLength = webgis.calc.length(coords, "X", "Y");
                args.segmentAzimut = webgis.calc.azimut_rad(coords);
                
                if (args.segmentAzimut < 0) {
                    args.segmentAzimut += Math.PI * 2.0;
                }
            }
        }

        if (this._moverMarker && this._moverMarker.snapResult) {
            args.snapResult = this._moverMarker.snapResult;
        }

        //console.log(args);

        this.events.fire('currentstate', args);
    };

    /*********************************/
    /******** Undo ************/
    var _suppressUndo = false;
    this.addUndo = function (text) {
        //console.log('addUndo', text, _suppressUndo);
        if (_suppressUndo === true)
            return;
        var vertices = [];
        for (var i = 0, to = _vertices.length; i < to; i++) {
            vertices.push({
                x: _vertices[i].x, y: _vertices[i].y,
                X: _vertices[i].X, Y: _vertices[i].Y,
                projEvent: _vertices[i].projEvent,
                fixed: _vertices[i].fixed,
                selected: _vertices[i].selected,
                m: _vertices[i].m,
                z: _vertices[i].z
            });
        }
        var partIndex = new Array();
        for (var i = 0; i < _partIndex.length; i++)
            partIndex.push(_partIndex[i]);
        if (_undos.length >= 10) {
            var undos = new Array();
            for (var i = _undos.length - 10; i < _undos.length; i++)
                undos.push(_undos[i]);
            _undos = undos;
        }
        _undos.push({ vertices: vertices, partIndex: partIndex, text: text });
    };
    this.undo = function () {
        if (!this.canUndo())
            return;

        const isValidBeforeUndo = this.isValid();

        const undo = _undos[_undos.length - 1];
        const u = new Array();

        for (let i = 0; i < _undos.length - 1; i++)
            u.push(_undos[i]);

        _undos = u;
        this.remove(true);

        for (let i = 0; i < undo.partIndex.length; i++) {
            if (i > 0)
                this.appendPart(null, true);

            const vertices = [];

            for (let v = undo.partIndex[i], to = i == undo.partIndex.length - 1 ? undo.vertices.length : undo.partIndex[i + 1]; v < to; v++) {
                vertices.push({
                    x: undo.vertices[v].x, y: undo.vertices[v].y,
                    X: undo.vertices[v].X, Y: undo.vertices[v].Y,
                    projEvent: undo.vertices[v].projEvent,
                    fixed: undo.vertices[v].fixed,
                    selected: undo.vertices[v].selected,
                    m: undo.vertices[v].m,
                    z: undo.vertices[v].z
                });
            }
            if (vertices.length > 0)
                this.addVertices(vertices);  // here gets invalid is fired
        }

        // fire gets invalid event
        if (isValidBeforeUndo === true && this.isValid() === false) {
            this.events.fire('ongetsinvalid', this);
        }
    };
    this.canUndo = function () { return _undos.length > 0; };
    this.undoText = function () {
        if (this.canUndo() == false)
            return '';
        return _undos[_undos.length - 1].text;
    };
    this.startSuppressUndo = function () { _suppressUndo = true; };
    this.stopSuppressUndo = function () { _suppressUndo = false; };
    this.cleanUndos = function () { _undos = []; }
    /*********************************/
    this.calcProperties = function (xParam, yParam) {
        if (!xParam)
            xParam = 'x';
        if (!yParam)
            yParam = 'y';

        //console.log('_geometryType', _geometryType);
        //console.log('_vertices', _vertices);
        if (_isSimplePoint()) {
            if (_vertices.length === 1) {
                var pos = webgis.calc.project(_vertices[0].x, _vertices[0].y);
                //console.log(pos);
                return { posX: pos[xParam], posY: pos[yParam], srs: xParam === 'X' ? pos.srs : 4326 };
            }
        }
        if (_isLine()) {
            var len = 0;
            for (var i = 0; i < this.partCount(); i++) {
                len += webgis.calc.length(this.partVertices(i), xParam, yParam);
            }
            return {
                length: len,
                set_values: true
            };
        }
        if (_isPolygon()) {
            var circum = 0, area = 0;
            if (this.isValid()) {
                var partsVertices = [];
                for (var i = 0; i < this.partCount(); i++) {
                    var partVertices = this.partVertices(i);
                    partsVertices.push(partVertices);
                }

                //console.log('calc polygon area with ' + partsVertices.length + ' parts');

                // Check for self intersecting
                var selfIntersecting = webgis.calc.isSelfIntersecting(partsVertices, true);

                // Nach Größe sortieren... eigentlich nicht notwendig
                //partsVertices.sort(function (ring1, ring2) {
                //    var a1 = webgis.calc.area(ring1, xParam, yParam);
                //    var a2 = webgis.calc.area(ring2, xParam, yParam);
                //    if (a1 > a2) return 1;
                //    if (a1 < a2) return -1;
                //    return 0;
                //});
                //partsVertices.reverse();
                //for (var i = 0; i < this.partCount(); i++) {
                //    console.log(webgis.calc.area(partsVertices[i], xParam, yParam));
                //}

                for (var i = 0; i < partsVertices.length; i++) {
                    if (selfIntersecting == true) {
                        //console.log('selfintersecting polygon => area==NaN');
                        area = Math.NaN;
                    } else {
                        var otherRings = [];
                        // Jeden Ring mit allen anderen testen
                        for (var j = 0; j < partsVertices.length; j++) {
                            if (j != i) otherRings.push(partsVertices[j]);
                        }

                        if (webgis.calc.isRingsHole(otherRings, partsVertices[i])) {
                            //console.log(i, 'hole');
                            area -= webgis.calc.area(partsVertices[i], xParam, yParam);
                        } else {
                            //console.log(i, 'ring');
                            area += webgis.calc.area(partsVertices[i], xParam, yParam);
                        }
                    }
                    //console.log('area', area);
                    circum += webgis.calc.length(partsVertices[i], xParam, yParam, true);
                }
            }
            return {
                circumference: circum,
                area: area,
                set_values: true,
            };
        }
        return {};
    };
    this.getCoords = function () {
        var ret = [];
        for (var p = 0; p < this.partCount(); p++) {
            var pv = this.partVertices(p);
            for (var i = 0; i < pv.length; i++) {
                ret.push({ x: pv[i].x, y: pv[i].x });
            }
        }
        return ret;
    };

    this.getVertices = function (xParam, yParam) {
        if (!xParam)
            xParam = 'x';
        if (!yParam)
            yParam = 'y';
        var ret = [];
        for (var p = 0; p < this.partCount(); p++) {
            var pv = this.partVertices(p);
            for (var i = 0; i < pv.length; i++) {
                ret.push([pv[i][xParam], pv[i][yParam]]);
            }
        }
        return ret;
    };
    this._getRawVertices = function () { return _vertices; }
    this.getVerticesPro = function (xParam, yParam, part) {
        if (!xParam)
            xParam = 'x';
        if (!yParam)
            yParam = 'y';
        var ret = [];
        for (var p = 0; p < this.partCount(); p++) {
            var pv = this.partVertices(p);
            for (var i = 0; i < pv.length; i++) {
                var vertex = {};
                vertex[xParam] = pv[i][xParam];
                vertex[yParam] = pv[i][yParam];
                if (pv[i].fixed === true) {
                    vertex.fixed = true;
                }
                if (pv[i].selected === true) {
                    vertex.selected = true;
                }
                ret.push(vertex);
            }
        }
        return ret;
    };
    this.getNeighborVertices = function (index, includeVertex) {
        index = parseInt(index);
        var vertices = [];

        if (_isLineOrPolygon() && index < _vertices.length) {
            var partNumber = this.getVertexPartNumber(index);
            //console.log('partnumber', partNumber);

            var min = partNumber == 0 ? 0 : _partIndex[partNumber];
            var max = partNumber == _partIndex.length - 1 ? _vertices.length - 1 : _partIndex[partNumber + 1] - 1;

            if (_isPolygon() && index == min) {
                vertices.push({ x: _vertices[max].x, y: _vertices[max].y, index: i });
            }

            for (var i = Math.max(min, index - 1), to = Math.min(index + 1, max); i <= to; i++) {
                if (i !== index || includeVertex === true) {
                    vertices.push({ x: _vertices[i].x, y: _vertices[i].y, index: i });
                }
            } 

            if (_isPolygon() && index == max) {
                vertices.push({ x: _vertices[min].x, y: _vertices[min].y, index: i });
            }
        }
        return vertices;
    };
    this.getVerticesCount = function () { return _vertices ? _vertices.length : 0; };
    this.getVertexIndicesFromBounds = function (bounds) {
        var indices = [];

        if (bounds && bounds.minX !== undefined && bounds.minY !== undefined && bounds.maxX !== undefined && bounds.maxY !== undefined) {
            var minX = Math.min(bounds.minX, bounds.maxX),
                minY = Math.min(bounds.minY, bounds.maxY),
                maxX = Math.max(bounds.minX, bounds.maxX),
                maxY = Math.max(bounds.minY, bounds.maxY);

            var w = maxX - minX, h = maxY - minY;

            if (w > 0 || h > 0) {
                for (var i in _vertices) {
                    var vertex = _vertices[i];
                    if (vertex.x >= minX && vertex.x <= maxX &&
                        vertex.y >= minY && vertex.y <= maxY) {
                        indices.push(i);
                    }
                }
            }
            else {
                var layerPoint = this.map.frameworkElement.latLngToLayerPoint({ lng: minX, lat: minY });
                var closestVertex = this._closestVertex(this.map, layerPoint);
                if (closestVertex != null && closestVertex.dist < 7) {
                    indices.push(closestVertex.index);
                }
            }
        }

        return indices;
    };
    this.getConstructionVerticesPro = function() {
        // get actual calcCrs
        var crs = this.map.calcCrs(), reproject = false;

        if (this.originalSrs() > 0 && this.originalSrs() != crs.id) {
            reproject = true;
        } else {
            for (var v in _vertices) {
                if (_vertices[v].srs) {
                    reproject = true;
                    break;
                }
            }
        }

        if (reproject === true || this.map.hasDynamicCalcCrs() === true) {
            var vertices = this.getVerticesPro("x", "y");
            // Project: necassary if map has dynamicCrs
            // project x,y => X,Y
            webgis.complementProjected(crs, vertices);

            return vertices;
        }

        return this.getVerticesPro("X", "Y");
    };
    this.getReferenceVertexPro = function () {
        var vertex = this._referenceVertex();
        // get actual calcCrs
        var crs = this.map.calcCrs(), reproject = false;

        if (this.originalSrs() > 0 && this.originalSrs() != crs.id) {
            reproject = true;
        } else if (vertex.srs) {
            reproject = true;
        }

        if (reproject === true || this.map.hasDynamicCalcCrs() === true) {
            vertex = { x: vertex.x, y: vertex.y };
            webgis.complementProjected(crs, [ vertex ]);

            return vertex;
        }

        return { X: vertex.X, Y: vertex.Y };
    };
    this.projectedUnprojectedVertices = function (crs) {
        var vertices = [];

        crs = crs || this.map.calcCrs();

        for (var i in _vertices) {
            var vertex = _vertices[i];
            webgis.complementProjected(crs, [vertex]);
        }
        //console.log('projected coords complemented', _vertices);

        return vertices;
    };
    this.verticesHasProperty = function (propertyName) {
        for (var p = 0; p < this.partCount(); p++) {
            var pv = this.partVertices(p);
            for (var i = 0; i < pv.length; i++) {
                if (!pv[i].hasOwnProperty(propertyName)) {
                    return false;
                }
            }
        }
        return true;
    };
    this.partCount = function () { return _partIndex.length; };
    this.partVertices = function (partNumber) {
        if (partNumber < 0 || partNumber >= _partIndex.length)
            return [];
        var vertices = [];
        var to = partNumber < _partIndex.length - 1 ? _partIndex[partNumber + 1] : _vertices.length;
        for (var i = _partIndex[partNumber]; i < to; i++) {
            vertices.push(_vertices[i]);
        }
        return vertices;
    };
    this.getParts = function () {
        var parts = [];
        for (var p = 0, to = this.partCount(); p < to; p++) {
            parts.push(this.partVertices(p));
        }
        return parts;
    };
    this.getVertexPartNumber = function (index) {
        if (_partIndex) {
            for (var p = 0; p < _partIndex.length - 1; p++) {
                if (_partIndex[p] <= index && _partIndex[p + 1] > index) {
                    return p;
                }
            }

            return _partIndex.length - 1;
        }

        return 0;
    };
    this.getCurrentPartLength = function () {
        if (!_partIndex || _partIndex.length <= 1)
            return _vertices.length;

        var partLength = Math.max(0, _vertices.length - _partIndex[_partIndex.length - 1]);
        //console.log('currentPartLength', partLength);

        return partLength;
    };
    this.isValid = function () {
        switch (_geometryType) {
            case 'point':
                return _vertices.length > 0;
            case 'polyline':
            case 'linestring':
                _geometryType = 'polyline';
                // ToDo: Parts
                return _vertices.length > 1;
            case 'freehand':
                _geometryType = 'freehand';
                return _vertices.length > 1;
            case 'polygon':
                // ToDo: Parts
                return _vertices.length > 2;
            case 'circle':
            case 'distance_circle':
            case 'compass_rose':
                return _vertices.length > 0;
            case 'rectangle':
                return _vertices.length > 1;
            case 'text':
                return _vertices.length > 0;
            case 'hectoline':
            case 'dimline':
                return _vertices.length > 1;
            case 'dimpolygon':
                return _vertices.length > 2;
        }
        return false;
    };
    this.getBounds = function (projected, addLngLat) {
        var bounds = null;
        if (projected) {
            for (var i = 0, to = _vertices.length; i < to; i++) {
                if (i == 0)
                    bounds = {};
                bounds.minX = i == 0 ? _vertices[i].X : Math.min(_vertices[i].X, bounds.minX);
                bounds.maxX = i == 0 ? _vertices[i].X : Math.max(_vertices[i].X, bounds.maxX);
                bounds.minY = i == 0 ? _vertices[i].Y : Math.min(_vertices[i].Y, bounds.minY);
                bounds.maxY = i == 0 ? _vertices[i].Y : Math.max(_vertices[i].Y, bounds.maxY);
            }
            if (addLngLat) {
                var vertex = this.map.construct.toProjectedVertex(this.map.calcCrs(), addLngLat);
                if (bounds) {
                    bounds.minX = Math.min(vertex.X, bounds.minX);
                    bounds.maxX = Math.max(vertex.X, bounds.maxX);
                    bounds.minY = Math.min(vertex.Y, bounds.minY);
                    bounds.maxY = Math.max(vertex.Y, bounds.maxY);
                }
                else {
                    bounds = {};
                    bounds.minX = bounds.maxX = vertex.X;
                    bounds.minY = bounds.maxY = vertex.Y;
                }
            }
            if (bounds && bounds.minX && bounds.minY && bounds.maxX && bounds.maxY) {
                bounds.width = Math.abs(bounds.maxX - bounds.minX);
                bounds.height = Math.abs(bounds.maxY - bounds.minY);
            }
        }
        else {
            for (var i = 0, to = _vertices.length; i < to; i++) {
                if (i == 0)
                    bounds = {};
                bounds.minLng = i == 0 ? _vertices[i].x : Math.min(_vertices[i].x, bounds.minLng);
                bounds.maxLng = i == 0 ? _vertices[i].x : Math.max(_vertices[i].x, bounds.maxLng);
                bounds.minLat = i == 0 ? _vertices[i].y : Math.min(_vertices[i].y, bounds.minLat);
                bounds.maxLat = i == 0 ? _vertices[i].y : Math.max(_vertices[i].y, bounds.maxLat);
            }
            if (addLngLat) {
                if (bounds) {
                    bounds.minLng = Math.min(addLngLat.lng, bounds.minLng);
                    bounds.maxLng = Math.max(addLngLat.lng, bounds.maxLng);
                    bounds.minLat = Math.min(addLngLat.lat, bounds.minLat);
                    bounds.maxLat = Math.max(addLngLat.lat, bounds.maxLat);
                }
                else {
                    bounds = {};
                    bounds.minLng = bounds.maxLng = vertex.lng;
                    bounds.minLat = bounds.maxLat = vertex.lat;
                }
            }
            if (bounds && bounds.minLng && bounds.minLat && bounds.maxLng && bounds.maxLat) {
                bounds.width = Math.abs(bounds.maxLng - bounds.minLng);
                bounds.height = Math.abs(bounds.maxLat - bounds.minLat);
            }
        }
        return bounds;
    };
    this.refreshProperties = function () {
        _sketchProperties = webgis.getSketchProperties(this.map);
        if (_sketchProperties && _sketchProperties.style) {
            var style = _sketchProperties.style;
            if (style.color)
                this.setColor(style.color);
            if (style.fillColor)
                this.setFillColor(style.fillColor);
            if (style.opacity)
                this.setOpacity(style.opacity);
            if (style.fillOpacity)
                this.setFillOpacity(style.fillOpacity);
            if (style.weight)
                this.setWeight(style.weight);
        }
    };
    this.checkMaxBBox = function (newVertex) {
        if (_sketchProperties && _sketchProperties.maxBBox) {
            var bbox = this.getBounds(true, { lng: newVertex.x, lat: newVertex.y });
            if (bbox.width > _sketchProperties.maxBBox.width ||
                bbox.height > _sketchProperties.maxBBox.height) {
                webgis.alert('Die aktuelle Änderung konnte nicht übernommen werden. Die maximale Größe für den Sketch wurde überschritten.', 'info');
                return false;
            }
        }
        return true;
    };

    this.isInDefaultMode = function () {
        if (this.isInOrthoMode()) return false;
        if (this.isInTraceMode()) return false;
        if (this.isInFanMode()) return false;

        return true;
    };
    this.startDefaultMode = function () {
        this.stopOrthoMode();
        this.stopTraceMode();
        this.stopFanMode();
    };
    
    this.isInOrthoMode = function () { return _ortho === true; }
    this.startOrthoMode = function () {
        if (_ortho)
            return;

        this.stopTraceMode();
        this.stopFanMode();
        _ortho = true;

        var vertices = this.getConstructionVerticesPro();

        if (vertices.length >= 2) {
            var v1 = vertices[vertices.length - 2], v2 = vertices[vertices.length - 1];
            var dx = v2.X - v1.X, dy = v2.Y - v1.Y, len = Math.sqrt(dx * dx + dy * dy);
            if (len > 0) {
                this._fixedDirectionVector = { X: dy / len, Y: -dx / len };
                this._fixedDistance = null;
            }
        }

        this.fireCurrentState();
    };
    this.stopOrthoMode = function () {
        _ortho = false;

        this._fixedDirectionVector = null;

        this.fireCurrentState();
    };
    this.toggleOrthoMode = function () {
        if (this.isInOrthoMode()) {
            this.stopOrthoMode();
        } else {
            this.startOrthoMode();
        }
    };

    this.isInTraceMode = function() { return _trace === true; }
    this.startTraceMode = function () {
        this.stopOrthoMode();
        this.stopFanMode();

        _trace = true;

        this.fireCurrentState();
    };
    this.stopTraceMode = function () {
        _trace = false;

        this.fireCurrentState();
    };
    this.toggleTraceMode = function() {
        if (this.isInTraceMode()) {
            this.stopTraceMode();
        } else {
            this.startTraceMode();
        }
    };

    this.isInFanMode = function () { return _fan === true; }
    this.startFanMode = function () {
        this.stopOrthoMode();
        this.stopTraceMode();

        _fan = true;

        this.redraw(true);
        this.fireCurrentState();
    };
    this.stopFanMode = function () {
        if (_fan === true) {
            _fan = false;

            _currentFrameworkIndex = 0;
            this.redraw(true);
            this.fireCurrentState();
        }
    };
    this.toggleFanMode = function () {
        if (this.isInFanMode()) {
            this.stopFanMode();
        } else {
            this.startFanMode();
        }
    };

    this.setStyle = function (style) {
        if (webgis.mapFramework === "leaflet") {
            for (var f in _frameworkElements) {
                if (_frameworkElements[f]) {
                    try {
                        if (_frameworkElements[f].setStyle) {
                            _frameworkElements[f].setStyle(style);
                        }
                    } catch (ex) {
                        //console.log('sketch.setStyle', style, ex.message);
                    }
                }
            }
            //_frameworkElements[_currentFrameworkIndex].setStyle(style);
        }
    };
    this.setColor = function (col, geomType) { this.setStyle({ color: _color[_getStyleIndex(geomType)] = col }); };
    this.setOpacity = function (opacity, geomType) { this.setStyle({ opacity: _opacity[_getStyleIndex(geomType)] = opacity }); };
    this.setFillColor = function (col, geomType) { this.setStyle({ fillColor: _fillColor[_getStyleIndex(geomType)] = col }); };
    this.setFillOpacity = function (opacity, geomType) { this.setStyle({ fillOpacity: _fillOpacity[_getStyleIndex(geomType)] = opacity }); };
    this.setWeight = function (weight, geomType) {
        this.setStyle({ weight: _weight[_getStyleIndex(geomType)] = weight });
        this.setStyle({ dashArray: this._toDashArray(_linesStyle[_getStyleIndex(geomType)]) });
    };
    this.setLineStyle = function (style, geomType) {
        _linesStyle[_getStyleIndex(geomType)] = style;

        if (!_frameworkElements[_currentFrameworkIndex]._webgis) {
            _frameworkElements[_currentFrameworkIndex]._webgis = {};
        }

        _frameworkElements[_currentFrameworkIndex]._webgis.lineStyle = style;
        this.setStyle({ dashArray: this._toDashArray(style) });
    };
    this.setCircleRadius = function (radius) {
        if (_frameworkElements[_currentFrameworkIndex].setRadius)
            _frameworkElements[_currentFrameworkIndex].setRadius(radius);

        _circle_radius = radius;
    };
    this.setDistanceCircleRadius = function (radius) {
        if (_frameworkElements[_currentFrameworkIndex].setRadius)
            _frameworkElements[_currentFrameworkIndex].setRadius(radius);

        _distance_circle_radius = radius;
    };
    this.setDistanceCircleSteps = function (steps) {
        if (_frameworkElements[_currentFrameworkIndex].setSteps)
            _frameworkElements[_currentFrameworkIndex].setSteps(steps);

        _distance_circle_steps = steps;
    };
    this.setCompassRoseRadius = function (radius) {
        if (_frameworkElements[_currentFrameworkIndex].setRadius)
            _frameworkElements[_currentFrameworkIndex].setRadius(radius);

        _compass_rose_radius = radius;
    };
    this.setCompassRoseSteps = function (steps) {
        if (_frameworkElements[_currentFrameworkIndex].setSteps)
            _frameworkElements[_currentFrameworkIndex].setSteps(steps);

        _compass_rose_steps = steps;
    };
    this.setHectolineUnit = function (unit) {
        if (_frameworkElements[_currentFrameworkIndex].setUnit)
            _frameworkElements[_currentFrameworkIndex].setUnit(unit);

        _hectoline_unit = unit;
    };
    this.setHectolineInterval = function (interval) {
        if (_frameworkElements[_currentFrameworkIndex].setInterval)
            _frameworkElements[_currentFrameworkIndex].setInterval(interval);

        _hectoline_interval = interval;
    };
    this.setDimPolygonAreaUnit = function (unit) {
        if (_frameworkElements[_currentFrameworkIndex].setAreaUnit)
            _frameworkElements[_currentFrameworkIndex].setAreaUnit(unit);

        _dimPolygon_areaUnit = unit;
    };
    this.setDimPolygonLabelEdges = function (doLabel) {
        if (_frameworkElements[_currentFrameworkIndex].setLabelEdges)
            _frameworkElements[_currentFrameworkIndex].setLabelEdges(doLabel);

        _dimPolygon_labelEdges = doLabel;
    };
    this.setText = function (text) {
        if (_frameworkElements[_currentFrameworkIndex].setText)
            _frameworkElements[_currentFrameworkIndex].setText(text);

        _svg_text = text;
    };
    this.setTextColor = function (col, geomType) { this.setStyle({ fill: _txtColor[_getStyleIndex(geomType)] = col }); };
    this.setTextStyle = function (style, geomType) { this.setStyle({ "font-style": _txtStyle[_getStyleIndex(geomType)] = style }); };
    this.setTextSize = function (size, geomType) { this.setStyle({ "font-size": _txtSize[_getStyleIndex(geomType)] = size }); };

    this.getText = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getText)
            _frameworkElements[_currentFrameworkIndex].getText();

        return _svg_text;
    };
    this.getColor = function (geomType) { /*console.log('getcolor[' + _getStyleIndex() + ']=' + _color[_getStyleIndex()]);*/ return _color[_getStyleIndex(geomType)]; };
    this.getOpacity = function (geomType) { return _opacity[_getStyleIndex(geomType)]; };
    this.getFillColor = function (geomType) { return _fillColor[_getStyleIndex(geomType)]; };
    this.getFillOpacity = function (geomType) { return _fillOpacity[_getStyleIndex(geomType)]; };
    this.getWeight = function (geomType) { return _weight[_getStyleIndex(geomType)]; };
    this.getLineStyle = function (geomType) { return _linesStyle[_getStyleIndex(geomType)]; };
    this.getTextColor = function (geomType) { return _txtColor[_getStyleIndex(geomType)]; };
    this.getTextStyle = function (geomType) { return _txtStyle[_getStyleIndex(geomType)]; };
    this.getTextSize = function (geomType) { return _txtSize[_getStyleIndex(geomType)]; };
    this.getCircleRadius = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getRadius)
            _circle_radius = _frameworkElements[_currentFrameworkIndex].getRadius();

        return _circle_radius;
    };
    this.getDistanceCircleRadius = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getRadius)
            _distance_circle_radius = _frameworkElements[_currentFrameworkIndex].getRadius();

        return _distance_circle_radius;
    };
    this.getDistanceCircleSteps = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getSteps)
            _distance_circle_steps = _frameworkElements[_currentFrameworkIndex].getSteps();

        return _distance_circle_steps;
    };
    this.getCompassRoseRadius = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getRadius)
            _compass_rose_radius = _frameworkElements[_currentFrameworkIndex].getRadius();

        return _compass_rose_radius;
    };
    this.getCompassRoseSteps = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getSteps)
            _compass_rose_steps = _frameworkElements[_currentFrameworkIndex].getSteps();

        return _compass_rose_steps;
    };
    this.getHectolineUnit = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getUnit)
            _hectoline_unit = _frameworkElements[_currentFrameworkIndex].getUnit();

        return _hectoline_unit;
    };
    this.getHectolineInterval = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getInverval)
            _hectoline_interval = _frameworkElements[_currentFrameworkIndex].getInverval();

        return _hectoline_interval;
    };
    this.getDimPolygonAreaUnit = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getAreaUnit)
            _dimPolygon_areaUnit = _frameworkElements[_currentFrameworkIndex].getAreaUnit();

        return _dimPolygon_areaUnit;
    };
    this.getDimPolygonLabelEdges = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getLabelEdges)
            _dimPolygon_labelEdges = _frameworkElements[_currentFrameworkIndex].getLabelEdges();

        return _dimPolygon_labelEdges;
    }

    var _getStyleIndex = function (geomType) {
        switch (geomType || _geometryType) {
            case 'dimline':
            case 'dimpolygon': 
            case 'hectoline':
            case 'distance_circle':
            case 'compass_rose':
                return 1;
            default:
                return 0;
        }
    }

    this._toDashArray = function (arrayString, geomType) {
        if (arrayString == null)
            return null;
        var ret = [], values = arrayString.split(',');
        for (var v in values) {
            var d = parseFloat(values[v]) * this.getWeight(geomType) / 10.0;
            ret.push(d);
        }
        return ret;
    };
    this._fromDashArray = function (array, geomType) {
        var ret = '';
        for (var i = 0; i < array.length; i++) {
            if (ret != '')
            if (ret != '')
                ret += ',';
            ret += array[i] * 10.0 / this.getWeight(geomType);
        }
        return ret;
    };
    this.setGeometryType('polyline');
    this.vertexCount = function () {
        return _vertices.length;
    };
    this.hasPrevVertex = function (index) {
        return index > 0;
    };
    this.hasPostVertex = function (index) {
        // ToDo: Parts!!
        return index < _vertices.length - 1;
    };
    this.startBubbleMoveVertex = function (index) {
        if (_geometryType === 'point') {
            //console.log('_frameworkElements', _frameworkElements);
            if (_frameworkElements.length > 0) {
                _currentBubbleMoveVertextFrameworkElement = _frameworkElements[0];
            }
        } else {
            if (index < 0 || index >= _vertexFrameworkElements.length)
                return;

            this.hideMoverLine();
            this.hideMoverMarker();
            this.hideTraceLine();

            _currentBubbleMoveVertextFrameworkElement = _vertexFrameworkElements[index];
        }

        _currentBubbleMoveVertextFrameworkElement.fire('dragstart');
        this.map.ui.setClickBubbleLatLng(_currentBubbleMoveVertextFrameworkElement.getLatLng());
    };
    this.endBubbleMoveVertex = function () {
        if (_currentBubbleMoveVertextFrameworkElement !== null) {
            _currentBubbleMoveVertextFrameworkElement.fire('dragend');
            _currentBubbleMoveVertextFrameworkElement = null;
        }
    };

    /************* Move Sketch ***********/
    var _sketchMoveRefLatLng = null;
    this.startSketchMoving = function (index) {
        this.endSketchRotating();

        if (index >= 0 && index < _vertices.length) {
            this.endSketchMoving();

            this.hide();
            var vertex = _vertices[index];

            if (_vertices.length > 1) {
                var polyline = null;
                var partIndex = 0;
                for (var i = 0; i < _vertices.length; i++) {
                    if (i === 0 || _partIndex[partIndex] === i) {
                        if (polyline != null && _geometryType === 'polygon') {
                            var latLngs = polyline.getLatLngs();
                            if (latLngs.length > 2)
                                polyline.addLatLng(L.latLng(latLngs[0].lat, latLngs[0].lng));
                        }
                        polyline = L.polyline([], { color: 'gray', opacity: .8, weight: 4 });
                        _sketchMoveFrameworkElements.push(polyline);
                        partIndex++;
                    }

                    var vertex = _vertices[i];
                    polyline.addLatLng(L.latLng(vertex.y, vertex.x));
                }
                if (polyline != null && _geometryType === 'polygon') {
                    var latLngs = polyline.getLatLngs();
                    if (latLngs.length > 2)
                        polyline.addLatLng(L.latLng(latLngs[0].lat, latLngs[0].lng));
                }

                for (var p in _sketchMoveFrameworkElements) {
                    this._addToMap(_sketchMoveFrameworkElements[p]);
                }
            }

            _sketchMoveRefLatLng = { lat: _vertices[index].y, lng: _vertices[index].x };
            _sketchMovingVertexIndex = index;

            _ortho = false;
            this._fixedDirectionVector = null;
            this._fixedDistance = null;
        } else {
            this.endSketchMoving();
        }
    };
    this.isSketchMoving = function () {
        return _sketchMovingVertexIndex >= 0;
    };
    this.endSketchMoving = function () {
        if (this.isSketchMoving() === false)
            return;

        _sketchMovingVertexIndex = -1;
        for (var e in _sketchMoveFrameworkElements) {
            if (webgis.mapFramework === "leaflet") {
                this.map.frameworkElement.removeLayer(_sketchMoveFrameworkElements[e]);
            }
        }
        _sketchMoveFrameworkElements = [];
        this.show();
    };
    this.moveSketchPreview = function (latlng) {
        if (_sketchMovingVertexIndex >= 0 && _sketchMovingVertexIndex < _vertices.length) {
            
            var deltaLng = (this._snappedVertex != null ? this._snappedVertex.x : latlng.lng) - _sketchMoveRefLatLng.lng;
            var deltaLat = (this._snappedVertex != null ? this._snappedVertex.y : latlng.lat) - _sketchMoveRefLatLng.lat;
            _sketchMoveRefLatLng = this._snappedVertex != null ? L.latLng(this._snappedVertex.y, this._snappedVertex.x) : latlng;

            var hasSelectedVertices = this.hasSelectedVertices();

            var vIndex = 0;
            for (var p in _sketchMoveFrameworkElements) {
                var path = _sketchMoveFrameworkElements[p];
                var pLatLngs = path.getLatLngs();
                for (var l in pLatLngs) {
                    var vertex = _vertices[vIndex % _vertices.length], apply = !(vertex.fixed === true || (hasSelectedVertices && vertex.selected !== true));
            
                    if (apply) {
                        pLatLngs[l].lng += deltaLng;
                        pLatLngs[l].lat += deltaLat;
                    }
                    vIndex++;
                }
                path.setLatLngs(pLatLngs);
            }
        }
    };
    this.moveSketch = function (latlng) {
        if (_sketchMovingVertexIndex >= 0 && _sketchMovingVertexIndex < _vertices.length) {

            this.addUndo(webgis.l10n.get("sketch-tool-move"));

            var vertex = _vertices[_sketchMovingVertexIndex];

            var deltaX = (this._snappedVertex != null ? this._snappedVertex.x : latlng.lng) - vertex.x;
            var deltaY = (this._snappedVertex != null ? this._snappedVertex.y : latlng.lat) - vertex.y;
            _sketchMoveRefLatLng = null;

            var hasSelectedVertices = this.hasSelectedVertices();

            for (var i = 0; i < _vertices.length; i++) {
                var vertex = _vertices[i], apply = !(vertex.fixed === true || (hasSelectedVertices && vertex.selected !== true));

                if (apply) {
                    _vertices[i] = { x: _vertices[i].x + deltaX, y: _vertices[i].y + deltaY, selected: _vertices[i].selected };
                }
            }
        }

        this.startSuppressUndo();
        this.load(this.store());  // Force hard redaw
        this.stopSuppressUndo();

        this.endSketchMoving();
    };
    /************* Rotate Sketch ***********/
    var _sketchRotateRefAngle = 0.0, _sketchStartAngle = 0.0;
    this.startSketchRotating = function (index, startAngle) {
        this.endSketchMoving();

        if (index >= 0 && index < _vertices.length) {
            this.endSketchRotating();

            this.hide();
            var vertex = _vertices[index];

            if (_vertices.length > 1) {
                var polyline = null;
                var partIndex = 0;
                for (var i = 0; i < _vertices.length; i++) {
                    if (i === 0 || _partIndex[partIndex] === i) {
                        if (polyline != null && _geometryType === 'polygon') {
                            var latLngs = polyline.getLatLngs();
                            if (latLngs.length > 2)
                                polyline.addLatLng(L.latLng(latLngs[0].lat, latLngs[0].lng));
                        }
                        polyline = L.polyline([], { color: 'gray', opacity: .8, weight: 4 });
                        _sketchRotateFrameworkElements.push(polyline);
                        partIndex++;
                    }

                    var vertex = _vertices[i];
                    polyline.addLatLng(L.latLng(vertex.y, vertex.x));
                }
                if (polyline != null && _geometryType === 'polygon') {
                    var latLngs = polyline.getLatLngs();
                    if (latLngs.length > 2)
                        polyline.addLatLng(L.latLng(latLngs[0].lat, latLngs[0].lng));
                }

                for (var p in _sketchRotateFrameworkElements) {
                    this._addToMap(_sketchRotateFrameworkElements[p]);
                }
            }

            _sketchRotateRefAngle = 0.0;
            _sketchStartAngle = startAngle || 0.0;
            _sketchRotatingVertexIndex = index;

            _ortho = false;
            this._fixedDirectionVector = null;
            this._fixedDistance = null;
        } else {
            this.endSketchRotating();
        }
    };
    this.isSketchRotating = function () {
        return _sketchRotatingVertexIndex >= 0;
    };
    this.endSketchRotating = function () {
        if (this.isSketchRotating() === false)
            return;

        _sketchRotatingVertexIndex = -1;
        for (var e in _sketchRotateFrameworkElements) {
            if (webgis.mapFramework === "leaflet") {
                this.map.frameworkElement.removeLayer(_sketchRotateFrameworkElements[e]);
            }
        }
        _sketchRotateFrameworkElements = [];
        this.show();
    };
    this.rotateSketchPreview = function (latlng) {
        if (_sketchRotatingVertexIndex >= 0 && _sketchRotatingVertexIndex < _vertices.length) {
            var crs = this.map.calcCrs();

            var vertices = [
                { x: this._referenceVertex().x, y: this._referenceVertex().y },
                { x: (this._snappedVertex != null ? this._snappedVertex.x : latlng.lng), y: (this._snappedVertex != null ? this._snappedVertex.y : latlng.lat) }
            ];
            webgis.complementProjected(crs, vertices);

            var deltaX = vertices[1].X - vertices[0].X;
            var deltaY = vertices[1].Y - vertices[0].Y;

            var angle = Math.atan2(deltaY, deltaX) + _sketchStartAngle;
            var x = angle;
            angle -= _sketchRotateRefAngle;
            _sketchRotateRefAngle = x;

            var hasSelectedVertices = this.hasSelectedVertices();

            var vIndex = 0;
            for (var p in _sketchRotateFrameworkElements) {
                var path = _sketchRotateFrameworkElements[p];
                var pLatLngs = path.getLatLngs();
                for (var l in pLatLngs) {
                    var vertex = _vertices[vIndex % _vertices.length], apply = !(vertex.fixed === true || (hasSelectedVertices && vertex.selected !== true));

                    if (apply) {
                        vertex = { x: pLatLngs[l].lng, y: pLatLngs[l].lat };
                        webgis.complementProjected(crs, [vertex]);

                        var dX = vertex.X - vertices[0].X;
                        var dY = vertex.Y - vertices[0].Y;

                        var rVertex = {
                            X: (Math.cos(angle) * dX - Math.sin(angle) * dY) + vertices[0].X,
                            Y: (Math.sin(angle) * dX + Math.cos(angle) * dY) + vertices[0].Y
                        };

                        webgis.complementWGS84(crs, [rVertex]);

                        pLatLngs[l].lng = rVertex.x;
                        pLatLngs[l].lat = rVertex.y;
                    }
                    vIndex++;
                }
                path.setLatLngs(pLatLngs);
            }
        }
    };
    this.rotateSketch = function(latlng) {
        if (_sketchRotatingVertexIndex >= 0 && _sketchRotatingVertexIndex < _vertices.length) {
            this.addUndo(webgis.l10n.get("sketch-tool-rotate"));

            var crs = this.map.calcCrs();

            var vertices = [
                { x: this._referenceVertex().x, y: this._referenceVertex().y },
                { x: (this._snappedVertex != null ? this._snappedVertex.x : latlng.lng), y: (this._snappedVertex != null ? this._snappedVertex.y : latlng.lat) }
            ];
            webgis.complementProjected(crs, vertices);

            var deltaX = vertices[1].X - vertices[0].X;
            var deltaY = vertices[1].Y - vertices[0].Y;

            var angle = Math.atan2(deltaY, deltaX) + _sketchStartAngle;
            _sketchRotateRefAngle = 0.0;

            var cosA = Math.cos(angle), sinA = Math.sin(angle);

            var hasSelectedVertices = this.hasSelectedVertices();

            for (var i = 0; i < _vertices.length; i++) {
                var vertex = _vertices[i], apply = !(vertex.fixed === true || (hasSelectedVertices && vertex.selected !== true));

                if (apply) {
                    vertex = { x: _vertices[i].x, y: _vertices[i].y };
                    webgis.complementProjected(crs, [vertex]);

                    var dX = vertex.X - vertices[0].X;
                    var dY = vertex.Y - vertices[0].Y;

                    var rVertex = {
                        X: cosA * dX - sinA * dY + vertices[0].X,
                        Y: sinA * dX + cosA * dY + vertices[0].Y,
                        selected: _vertices[i].selected
                    };

                    webgis.complementWGS84(crs, [rVertex]);

                    _vertices[i] = rVertex;
                }
            }
        }

        this.startSuppressUndo();
        this.load(this.store());  // Force hard redaw
        this.stopSuppressUndo();

        this.endSketchRotating();
    };

    this.removeVertex = function (index, suppressUndo) {
        this.hide();
        if (suppressUndo !== true) {
            this.addUndo(webgis.l10n.get("sketch-remove-vertex"));
        }
        const isValidBeforeRemove = this.isValid();

        var vertexFrameworkElements = [];
        for (var i = 0; i < _vertices.length; i++) {
            if (i == index)
                continue;
            if (i > index) {
                _vertexFrameworkElements[i].vertex_index--;
                this._resetMarkerImage(_vertexFrameworkElements[i]);
            }
        }
        //_vertices = vertices;
        //_vertexFrameworkElements = vertexFrameworkElements;
        _vertices.splice(index, 1);
        _vertexFrameworkElements.splice(index, 1);

        for (var p = 0; p < _partIndex.length; p++) {
            if (_partIndex[p] > index)
                _partIndex[p] = _partIndex[p] - 1;
        }
        this.redraw(true);
        this.redrawMarkers();

        if (isValidBeforeRemove === true && this.isValid() === false) {
            this.events.fire('ongetsinvalid', this);
        }
    };
    /******** Fix Vertices **********************/
    this.fixVertex = function (index, fix) {
        if (index < 0 || index >= _vertices.length) {
            return;
        }
        if (fix === true) {
            _vertices[index].fixed = true;
        } else if (fix === false) {
            _vertices[index].fixed = false;
        } else {  // toogle
            _vertices[index].fixed = _vertices[index].fixed === true ? false : true;
        }

        this.redrawMarkers();
    };
    this.snapFixVertex = function (vertex) {
        //console.log('snapFixVertex', vertex);
        var editTheme = this.map.getCurrentEditTheme();
        if (editTheme == null || !editTheme.snapping) {
            return false;
        }

        for (var s in editTheme.snapping) {
            var snapping = editTheme.snapping[s];
            if (snapping.fixto) {
                //console.log('snapping with fixto', snapping);
                for (var f in snapping.fixto) {
                    var fixto = snapping.fixto[f];

                    var options = {
                        id: snapping.serviceid + "~" + snapping.id,
                        name: fixto.name === "*" ? null : fixto.name,
                        types: fixto.types,
                        tolerance: 1e-8,
                        //sketch: this
                    };

                    var snapResult = this.map.construct.performSnap(vertex.x, vertex.y, options);
                    //console.log('snapResult', snapResult);
                    if (snapResult) {
                        //console.log('fix this vertex');
                        vertex.fixed = true;
                        return true;
                    }
                }
            }
        }

        return false;
    };

    /******** Select Vertices ******************/
    var _selectVertex = function (index, select) {
        if (webgis.usability.allowSelectSketchVertices !== true) {
            return;
        }
        if (index < 0 || index >= _vertices.length) {
            return;
        }
        if (select === true) {
            _vertices[index].selected = true;
        } else if (select === false) {
            _vertices[index].selected = false;
        } else {  // toogle
            _vertices[index].selected = _vertices[index].selected === true ? false : true;
        }
    };
    this.selectVertex = function (index, select) {
        _selectVertex(index, select);

        this.redrawMarkers();
    };
    this.selectVertices = function (indices, select) {
        if (!indices || indices.length === 0)
            return;

        for (var i in indices) {
            _selectVertex(indices[i], select);
        }

        this.redrawMarkers();
    };
    this.unselectAllVertices = function () {
        for (var i in _vertices) {
            _selectVertex(i, false);
        }

        this.redrawMarkers();
    };
    this.toggleVertexSelection = function () {
        for (var i in _vertices) {
            _selectVertex(i, _vertices[i].selected !== true);
        }

        this.redrawMarkers();
    };
    this.removeSelectedVertices = function () {
        this.addUndo(webgis.l10n.get("sketch-remove-selected-vertices"));

        while (this.hasSelectedVertices()) {
            for (var i in _vertices) {
                if (_vertices[i].selected === true) {
                    this.removeVertex(i, true);
                    break;
                }
            }
        }
    };
    this.hasSelectedVertices = function () {
        if (webgis.usability.allowSelectSketchVertices !== true) {
            return false;
        }
        for (var i in _vertices) {
            if (_vertices[i].selected === true) {
                return true;
            }
        }

        return false;
    };

    /********************************************/
    this.addPrevVertex = function (index) {
        this.addUndo(webgis.l10n.get("sketch-add-vertex"));
        var vertices = _cloneArray(_vertices),
            partIndex = _cloneArray(_partIndex);
        this.remove(true);

        this.events.suppress('onchanged');  // Performance!! dont calc area etc on every add (large polygons, ...)

        for (var i = 0; i < vertices.length; i++) {
            if (this._isFirstPartVertex(i, partIndex))
                this.appendPart();
            if (i == index && i > 0) {
                this.addVertexCoords((vertices[i - 1].x + vertices[i].x) / 2.0, (vertices[i - 1].y + vertices[i].y) / 2.0);
            }
            this.addVertex(vertices[i]);
        }
        for (var p = 0; p < partIndex.length; p++) {
            if (partIndex[p] >= index)
                partIndex[p] = partIndex[p] + 1;
        }

        _partIndex = partIndex;

        this.events.enable('onchanged');
        this.events.fire('onchanged', this);
    };
    this.addPostVertex = function (index, t) {
        this.addUndo(webgis.l10n.get("sketch-add-vertex"));
        _suppressUndo = true;

        var vertices = _cloneArray(_vertices),    // clone
            partIndex = _cloneArray(_partIndex);  // clone
        var isClosed = this.isClosed();

        this.remove(true);

        this.events.suppress('onchanged');  // Performance!! dont calc area etc on every add (large polygons, ...)

        for (var i = 0; i < vertices.length; i++) {
            if (this._isFirstPartVertex(i, partIndex))
                this.appendPart();
            this.addVertex(vertices[i]);
            if (i == index) {
                if (!t)
                    t = .5;
                var pIndex1 = this.vertexPartIndex(i), pIndex2 = (i + 1) >= vertices.length ? pIndex1 + 1 : this.vertexPartIndex(i + 1, partIndex);
                var nVertex = null;
                if (pIndex2 > pIndex1) {
                    if (_geometryType == 'polygon') {
                        nVertex = vertices[_partIndex[pIndex1]];
                    }
                    else {
                        continue;
                    }
                }
                else {
                    nVertex = vertices[i + 1];
                }
                if (!nVertex)
                    continue;
                this.addVertexCoords(vertices[i].x + (nVertex.x - vertices[i].x) * t, vertices[i].y + (nVertex.y - vertices[i].y) * t);
            }
        }
        //console.log(partIndex);
        //for (var p = 0; p < partIndex.length; p++) {
        //    if (partIndex[p] >= index)
        //        partIndex[p] = partIndex[p] + 1;
        //}
        //console.log(partIndex);
        if (isClosed) {
            this.appendPart();
        }
        //_partIndex = partIndex;
        //this.redraw(true);

        this.events.enable('onchanged');
        this.events.fire('onchanged', this);

        _suppressUndo = false;
    };
    this.getEdgeAngle = function (index) {
        if (index >= _vertices.length)
            return 0;

        var crs = this.map.calcCrs();
        var vertices = [
            { x: _vertices[index].x, y: _vertices[index].y },
            { x: _vertices[(index + 1) % _vertices.length].x, y: _vertices[(index + 1) % _vertices.length].y }
        ];

        webgis.complementProjected(crs, vertices);
        return Math.atan2(
            vertices[1].Y - vertices[0].Y,
            vertices[1].X - vertices[0].X
        );
    },
    this.swapDirection = function () {
        var vertices = _cloneArray(_vertices),
            partIndex = _cloneArray(_partIndex);
        this.remove(true);
        if (partIndex.length > 0) {
            var last = vertices.length;
            for (var i = partIndex.length - 1; i > 0; i--) {
                _partIndex.push(_partIndex[_partIndex.length - 1] + (last - partIndex[i]));
                last = partIndex[i];
            }
        }
        for (var i = vertices.length - 1; i >= 0; i--) {
            this.addVertex(vertices[i]);
        }
        this.redraw(true);
        _currentFrameworkIndex = _partIndex.length - 1;
    };
    this.appendPart = function (latLngs, silent) {
        if (!silent) {
            this.addUndo(webgis.l10n.get("sketch-close-section"));
        }
        if (_vertices.length === 0) {
            _frameworkElements = [this._createFrameworkElement(latLngs)];
            _partIndex.splice(0, _partIndex.length);  // clear -> keep reference
            _partIndex.push(0);
        }
        if (_geometryType === "freehand") {
            // keine neues Frameworkelement erzeugen, weil man hier sowieso nicht weiterzeichnen kann
            // einfach PartIndex erhöhen, um Objekte abzuschließen
            if ($.inArray(_vertices.length, _partIndex) < 0)
                _partIndex.push(_vertices.length);
        }
        else {
            _frameworkElements.push(this._createFrameworkElement(latLngs));
            if ($.inArray(_vertices.length, _partIndex) < 0)
                _partIndex.push(_vertices.length);
        }
        _currentFrameworkIndex = _frameworkElements.length - 1;
        this._addToMap(_frameworkElements[_currentFrameworkIndex]);
    }; 
    this.isMultiPart = function () {
        return _partIndex == null || _partIndex.length > 1;
    }
    this.vertexPartIndex = function (vertexIndex, partIndex) {
        var partIndex = partIndex || _partIndex;
        if (partIndex == null || partIndex.length <= 1)
            return 0;
        for (var p = 1, to = partIndex.length; p < to; p++) {
            if (partIndex[p] > vertexIndex)
                return p - 1;
        }
        return partIndex.length - 1;
    };
    this.firstVertexIndexInCurrentPart = function () {
        if (_partIndex && _partIndex.length > 0) {
            return _partIndex[_partIndex.length - 1];
        }
        return 0;
    };
    this.lastVertexInCurrentPart = function (stepsBack) {
        stepsBack = stepsBack || 0;

        var firstInCurrentPartIndex = this.firstVertexIndexInCurrentPart;

        if (this.firstVertexIndexInCurrentPart() + stepsBack < _vertices.length) {
            return _vertices[_vertices.length - 1 - stepsBack];
        }

        return null;
    };
    this.isClosed = function () {
        //console.log(_partIndex[_partIndex.length-1] + '>' + _vertices.length);
        return (_partIndex != null && _partIndex.length > 1 && _partIndex[_partIndex.length - 1] == _vertices.length);
    };
    this.isLastPartClosed = function () {
        return _partIndex != null && _partIndex.length > 0 && _vertices.length == _partIndex[_partIndex.length - 1];
    };
    this.close = function () {
        if (!this.isClosed()) {
            this.appendPart();
            this.events.fire('onsketchclosed');
        }
    };
    this.zoomTo = function (scale) {
        if (!_vertices || _vertices.length == 0)
            return;
        var minx = _vertices[0].x, miny = _vertices[0].y, maxx = _vertices[0].x, maxy = _vertices[0].y;
        for (var i = 1; i < _vertices.length; i++) {
            minx = Math.min(minx, _vertices[i].x);
            miny = Math.min(miny, _vertices[i].y);
            maxx = Math.max(maxx, _vertices[i].x);
            maxy = Math.max(maxy, _vertices[i].y);
        }
        if (scale)
            this.map.zoomToBoundsOrScale([minx, miny, maxx, maxy], scale);
        else
            this.map.zoomTo(webgis.calc.resizeBounds([minx, miny, maxx, maxy], webgis.usability.zoom.allowsFreeZooming() ? 1.3 : 1.0));
    };
    this.onClick = function (f, remove) {
        if (webgis.mapFramework == "leaflet") {
            for (var i = 0; i < _frameworkElements.length; i++) {
                var frameworkElement = _frameworkElements[i];
                if (remove) {
                    frameworkElement.off('click', f);
                }
                else {
                    frameworkElement.on('click', function (e) {
                        f(this, e);
                    }, this);
                }
            }
        }
    };
    this.statPoint = function (s) {
        var stat = 0.0, stat0 = 0.0;
        if (_vertices.length == 0)
            return null;
        var x1 = _vertices[0].x, y1 = _vertices[0].y;
        var X1 = _vertices[0].X, Y1 = _vertices[0].Y;
        for (var i = 1, to = _vertices.length; i < to; i++) {
            var x2 = _vertices[i].x, y2 = _vertices[i].y;
            var X2 = _vertices[i].X, Y2 = _vertices[i].Y;
            stat0 = stat;
            var l = Math.sqrt((X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1));
            if (l > 0) {
                stat += l;
                if (stat >= s) {
                    var t = s - stat0;
                    var dx = (x2 - x1) / l, dy = (y2 - y1) / l;
                    var dX = (X2 - X1) / l, dY = (Y2 - Y1) / l;
                    var x = x1 + dx * t, y = y1 + dy * t;
                    var X = X1 + dX * t, Y = Y1 + dY * t;
                    return { x: x, y: y, X: X, Y: Y };
                }
            }
            x1 = x2;
            y1 = y2;
            X1 = X2;
            Y1 = Y2;
        }
        return null;
    };

    this.metaInfo = function (s) {
        var info = {
            geometryType: _geometryType
        };
        if (_geometryType === 'circle') {
            info.radius = _circle_radius;
            if (_vertices.length > 0) {
                info.center = { lat: _vertices[0].y, lng: _vertices[0].x };
            }
        }
        return info;
    };

    this._referenceVertex = function () {
        if (this.isSketchMoving()) {
            return _vertices[_sketchMovingVertexIndex];
        }
        if (this.isSketchRotating()) {
            return _vertices[_sketchRotatingVertexIndex];
        }

        if (this.isInFanMode()) {
            return _vertices[0];
        }

        if (this.getCurrentPartLength() === 0) {
            return null;
        }

        return _vertices[_vertices.length - 1];
    }

    // War für 3D Messen (dynamisch) angedacht => erst einmal auf Eis gelegt
    //this.setHasZ = function (hasZ, getZCommand) {
    //    this._hasZ = hasZ;
    //    this._getZValuesCommand = getZCommand;

    //    this._getZValues();
    //}

    //this._getZValues = function () {
    //    console.log('_getZValues', this._hasZ, this._getZValuesCommand)
    //    if (this._hasZ === true && this._getZValuesCommand) {
    //        var queryZ = false;
    //        for (var i in _vertices) {
    //            console.log(_vertices[i].z);
    //            if (!_vertices[i].z || typeof _vertices[i].z !== 'number') {
    //                queryZ = true;
    //                break;
    //            }
    //        }

    //        if (queryZ) {
    //            var activeTool = this.map.getActiveTool();
    //            if (activeTool) {
    //                webgis.tools.onButtonClick(this.map,
    //                    {
    //                        command: this._getZValuesCommand,
    //                        type: 'servertoolcommand',
    //                        id: this.map.getActiveTool().id,
    //                        map: this.map
    //                    }, null, null, null);
    //            }
    //        }
    //    }
    //}

    this.toJson = function (crsId) {
        var ret = {
            geometry: {}, valid: false
        };
        if (_geometryType == 'point' && _vertices.length > 0) {
            ret.geometry.type = "Point";
            if (crsId)
                ret.geometry.SRID = crsId;
            ret.geometry.coordinates = crsId ? webgis.project(crsId, [_vertices[0].x, _vertices[0].y]) : [_vertices[0].x, _vertices[0].y];
            ret.geometry.COORDINATES = [_vertices[0].X, _vertices[0].Y];
        }
        if (_geometryType == 'polyline' || _geometryType == 'polygon') {
            if (_geometryType == 'polyline' && _vertices.length >= 2) {
                ret.geometry.type = 'LineString';
                ret.valid = true;
            }
            if (_geometryType == 'polygon' && _vertices.length >= 3) {
                ret.geometry.type = 'Polygon';
                ret.valid = true;
            }
            if (ret.valid) {
                if (crsId)
                    ret.geometry.SRID = crsId;
                ret.geometry.coordinates = [];
                ret.geometry.COORDINATES = [];
                for (var i = 0; i < _vertices.length; i++) {
                    ret.geometry.coordinates.push(crsId ? webgis.project(crsId, [_vertices[i].x, _vertices[i].y]) : [_vertices[i].x, _vertices[i].y]);
                    ret.geometry.COORDINATES.push([_vertices[i].X, _vertices[i].Y]);
                }
            }
        }
        return ret;
    };
    this.fromJson = function (json, append, readOnly) {
        if (append !== true)
            this.remove(true);

        this.setReadOnly(readOnly === true);

        this.setGeometryType(json.type);
        var crsId = json.SRID;

        var isMultiPartArray = _geometryType === 'polygon' || json.type.toLowerCase() === 'multilinestring' || _geometryType === 'dimpolygon';

        if (_geometryType === 'point' || _geometryType === 'distance_circle' || _geometryType === 'compass_rose' || _geometryType === 'circle' || _geometryType === 'text') {
            var coordinates = crsId ? webgis.unproject(crsId, json.coordinates) : json.coordinates;
            if (json.COORDINATES && json.COORDINATES.length > 1) {
                var XYIndex = 0;

                vertex = { x: coordinates[0], y: coordinates[1], X: json.COORDINATES[XYIndex++], Y: json.COORDINATES[XYIndex++], projEvent: 'original' };
                if (json.hasZ === true && XYIndex < json.COORDINATES.length) {
                    vertex.z = json.COORDINATES[XYIndex++];
                }
                if (json.hasM === true && XYIndex < json.COORDINATES.length) {
                    vertex.m = json.COORDINATES[XYIndex++];
                }

                this.addVertex(vertex);
            }
            else {
                this.addVertexCoords(coordinates[0], coordinates[1]);
            }
        }
        else if (isMultiPartArray) {
            for (var r = 0; r < json.coordinates.length; r++) {
                var coordinateArray = json.coordinates[r];
                var COORDINATEArray = json.COORDINATES ? json.COORDINATES[r] : null;
                var firstPoint = true;
                var vertices = [];
                for (var i = 0; i < coordinateArray.length; i++) {
                    var xy = coordinateArray[i];
                    xy = crsId ? webgis.unproject(crsId, xy) : xy;
                    if (i > 0 && i == coordinateArray.length - 1) {
                        if (coordinateArray[0][0] == xy[0] && coordinateArray[0][1] == xy[1])
                            continue;
                    }
                    if (firstPoint && _vertices.length > 0) {
                        this.appendPart();
                    }
                    firstPoint = false;
                    if (COORDINATEArray) {
                        var XY = COORDINATEArray[i], XYIndex = 0;

                        var vertex = { x: xy[0], y: xy[1], X: XY[XYIndex++], Y: XY[XYIndex++], projEvent: 'original' };
                        if (json.hasZ === true && XYIndex < XY.length) {
                            vertex.z = XY[XYIndex++];
                        }
                        if (json.hasM === true && XYIndex < XY.length) {
                            vertex.m = XY[XYIndex++];
                        }

                        vertices.push(vertex/*{ x: xy[0], y: xy[1], X: XY[0], Y: XY[1], projEvent: 'original' }*/);
                    }
                    else {
                        vertices.push({ x: xy[0], y: xy[1] });
                    }
                }
                if (vertices.length > 0) {
                    this.addVertices(vertices, false, readOnly);
                }
            }
        }
        else if (_geometryType === 'rectangle') {
            if (json.coordinates.length === 1) {
                var coordinateArray = json.coordinates[0], minx, miny, maxx, maxy;
                for (var i = 0; i < coordinateArray.length; i++) {
                    if (i === 0) {
                        minx = maxx = coordinateArray[i][0];
                        miny = maxy = coordinateArray[i][1];
                    } else {
                        minx = Math.min(coordinateArray[i][0], minx);
                        miny = Math.min(coordinateArray[i][1], miny);
                        maxx = Math.max(coordinateArray[i][0], maxx);
                        maxy = Math.max(coordinateArray[i][1], maxy);
                    }
                }

                this.addVertices([{ x: minx, y: maxy }, { x: maxx, y: miny }]);
            }
        }
        else {
            if (_geometryType === 'freehand')
                _lastFreehandVertexIndex = json.coordinates.length - 1;

            var partIndex = json.partindex;
            for (var i = 0; i < json.coordinates.length; i++) {
                if (partIndex && $.inArray(i, partIndex) >= 0)
                    this.appendPart();
                var xy = json.coordinates[i];
                xy = crsId ? webgis.unproject(crsId, xy) : xy;
                if (json.COORDINATES && json.COORDINATES.length > i) {
                    var XY = json.COORDINATES[i], XYIndex=0;

                    var vertex = { x: xy[0], y: xy[1], X: XY[XYIndex++], Y: XY[XYIndex++], projEvent: 'original' };
                    if (json.hasZ === true && XYIndex < XY.length) {
                        vertex.z = XY[XYIndex++];
                    }
                    if (json.hasM === true && XYIndex < XY.length) {
                        vertex.m = XY[XYIndex++];
                    }

                    this.addVertices([vertex]);
                }
                else {
                    this.addVertices([{ x: xy[0], y: xy[1] }]);
                }
                //var latLng = webgis.toWGS84(map.calcCrs(), xy[0], xy[1]);
                //this.addVertex(latLng.lng, latLng.lat);
            }
        }
        _originalSrs = json.COORDINATES_srefid ? json.COORDINATES_srefid : 0;
        _originalSrsP4Params = json.COORDINATES_sref_p4 ? json.COORDINATES_sref_p4 : null;

        if (this.isReadOnly() !== true &&
            json.snapped_coordinates &&
            json.snapped_coordinates.length == _vertices.length) {
            var hasFixedVertices = false;

            for (var i = 0; i < _vertices.length; i++) {
                if (json.snapped_coordinates[i] === true) {
                    _vertices[i].fixed = hasFixedVertices = true;
                }
            }

            if (hasFixedVertices) {
                this.redrawMarkers();
            }
        }
        //console.log('fromJson', json);
        //console.log('fromJson', _vertices);

        this._isDirty = false;
    };
    this.fromJsonFast = function (json, append) {
        if (append != true)
            this.remove(true);
        this.setGeometryType(json.type);
        var crsId = json.SRID;

        var isMultiPartArray = _geometryType === 'polygon' || json.type.toLowerCase() === 'multilinestring';

        if (_geometryType == 'point') {
            var coordinates = crsId ? webgis.unproject(crsId, json.coordinates) : json.coordinates;
            if (json.COORDINATES && json.COORDINATES.length > 1) {
                this.addVertex({ x: coordinates[0], y: coordinates[1], X: json.COORDINATES[0], Y: json.COORDINATES[1], projEvent: 'original' });
            }
            else {
                this.addVertexCoords(coordinates[0], coordinates[1]);
            }
        }
        else if (isMultiPartArray) {
            this.appendPart(json.coordinates, true);
            for (var r = 0; r < json.coordinates.length; r++) {
                var coorindateArray = json.coordinates[r];
                for (var i = 0; i < coorindateArray.length; i++) {
                    var xy = coorindateArray[i];
                    xy = crsId ? webgis.unproject(crsId, xy) : xy;
                    if (i > 0 && i == coorindateArray.length - 1) {
                        if (coorindateArray[0][0] == xy[0] && coorindateArray[0][1] == xy[1])
                            continue;
                    }
                    _vertices.push({ x: xy[0], y: xy[1] });
                    //_frameworkElements[_currentFrameworkIndex].addLatLng(L.latLng(xy[1], xy[0]));
                }
            }
        }
        else {
            for (var i = 0; i < json.coordinates.length; i++) {
                var xy = json.coordinates[i];
                xy = crsId ? webgis.unproject(crsId, xy) : xy;

                if (json.COORDINATES && json.COORDINATES.length > i) {
                    var XY = json.COORDINATES[i];
                    this.addVertex({ x: xy[0], y: xy[1], X: XY[0], Y: XY[1], projEvent: 'original' });
                }
                else {
                    this.addVertexCoords(xy[0], xy[1]);
                }
                //var latLng = webgis.toWGS84(map.calcCrs(), xy[0], xy[1]);
                //this.addVertex(latLng.lng, latLng.lat);
            }
        }
        this.events.fire('onchanged', this);

        this._isDirty = false;
    };

    this.toWKT = function (useWGS84) {
        if (_vertices.length == 0) {
            return '';
        }

        let wkt = '';

        if (_geometryType === 'point') {
            wkt += "MULTIPOINT(";
        }
        else if (_geometryType === 'polyline') {
            wkt += "MULTILINESTRING(";
        }
        else if (_geometryType === 'polygon' || _geometryType === 'circle') {
            wkt += "MULTIPOLYGON(";
        }

        var vertices = _vertices;

        if (_geometryType === 'circle') {
            vertices = [];
            var center = _frameworkElements[0]._latlng;
            var radius = _frameworkElements[0].getRadius();
            
            var alpha = radius / 6371000.0, lat = 90.0 - center.lat, lng = 90.0 - center.lng;
            var sin_a = Math.sin(alpha), cos_a = Math.cos(alpha);
            var sin_lat = Math.sin(lat * Math.PI / 180.0), cos_lat = Math.cos(lat * Math.PI / 180.0);
            var sin_lng = Math.sin(lng * Math.PI / 180.0), cos_lng = Math.cos(lng * Math.PI / 180.0);

            for (var t = 0; t < Math.PI * 2.0; t += .01) {  // ToDO: Strittweite: 0.01??
                // https://math.stackexchange.com/questions/643130/circle-on-sphere

                var x =  (sin_a * cos_lat * cos_lng) * Math.cos(t) + (sin_a * sin_lng) * Math.sin(t) - (cos_a * sin_lat * cos_lng);
                var y = -(sin_a * cos_lat * sin_lng) * Math.cos(t) + (sin_a * cos_lng) * Math.sin(t) + (cos_a * sin_lat * sin_lng);
                var z =  (sin_a * sin_lat) * Math.cos(t) + cos_a * cos_lat;

                var lat_ = Math.asin(z) * 180.0 /Math.PI;
                var lng_ = -Math.atan2(x, y) * 180.0 / Math.PI;

                if (useWGS84) {
                    vertices.push({ x: lng_, y: lat_  });
                } else {
                    var projected = webgis.fromWGS84(this.map.calcCrs(), lat_, lng_);
                    vertices.push({ X: projected.x, Y: projected.y });
                }
            }
        } 

        var addVertexFunc = function (wkt, vertex) {
            if (useWGS84 === true) {
                wkt += vertex.x + ' ' + vertex.y;
            }
            else {
                wkt += vertex.X + ' ' + vertex.Y;
                if (vertex.projEvent && vertex.projEvent !== 'original' && vertex.projEvent !== 'snapped') {
                    wkt += ' meta:' + vertex.projEvent;
                }
                if (vertex.srs) {
                    wkt += ' srs:' + vertex.srs;
                }
                if (typeof vertex.m === 'number') {  // can be null or undefined also
                    wkt += ' m:' + vertex.m;
                }
                if (typeof vertex.z === 'number') {
                    wkt += ' z:' + vertex.z;
                }
            }
            return wkt;
        };

        wkt += "(";
        for (var i = 0; i < vertices.length; i++) {
            if (i > 0) {
                if (this.isInFanMode() && i > 1) {
                    wkt += '),(';
                    wkt = addVertexFunc(wkt, vertices[0]);
                    wkt += ',';
                }
                else if (i < vertices.length - 1 && $.inArray(i, _partIndex) >= 0) {
                    wkt += '),(';
                }
                else {
                    wkt += ',';
                }
            }
            wkt = addVertexFunc(wkt, vertices[i]);
        }
        wkt += ")";
        if (wkt !== '')
            wkt += ")";

        return wkt;
    };

    this.toWKT2 = function (useWGS84, appendMeta, digits) {
        if (_vertices.length == 0)
            return '';

        let isMultipart = _partIndex.length > 2 || (_partIndex.length == 2 && _vertices.length > _partIndex[1]);  // its no an multipart if only the first is "closed";
        console.log('isMultipart', isMultipart, _partIndex);

        let wkt = '', closeVertex = null;
        if (_geometryType === 'point') {
            wkt += isMultipart ? "MULTIPOINT(" : "POINT(";
        }
        else if (_geometryType === 'polyline') {
            wkt += isMultipart ? "MULTILINESTRING(" : "LINESTRING(";
        }
        else if (_geometryType === 'polygon' || _geometryType === 'circle') {
            wkt += isMultipart ? "MULTIPOLYGON((" : "POLYGON((";
            closeVertex = _vertices.length > 0 ? _vertices[0] : null;
        }

        let vertices = _vertices;
        let roundFactor = 0.0;

        if (digits && digits > 0) {
            roundFactor = Math.pow(10.0, digits);
        }

        var addVertexFunc = function (wkt, vertex) {
            if (useWGS84 === true) {
                wkt += vertex.x + ' ' + vertex.y;
            }
            else {
                let X = vertex.X, Y = vertex.Y;
                if (roundFactor > 1.0) {

                    X = Math.round(X * roundFactor) / roundFactor;
                    Y = Math.round(Y * roundFactor) / roundFactor;
                }
                wkt += X + ' ' + Y;
                if (appendMeta) {
                    if (vertex.projEvent && vertex.projEvent !== 'original' && vertex.projEvent !== 'snapped') {
                        wkt += ' meta:' + vertex.projEvent;
                    }
                    if (vertex.srs) {
                        wkt += ' srs:' + vertex.srs;
                    }
                    if (typeof vertex.m === 'number') {  // can be null or undefined also
                        wkt += ' m:' + vertex.m;
                    }
                    if (typeof vertex.z === 'number') {
                        wkt += ' z:' + vertex.z;
                    }
                }
            }
            return wkt;
        };

        wkt += isMultipart ? "(" : "";
        for (var i = 0; i < vertices.length; i++) {
            if (i > 0) {
                if (i < vertices.length - 1 && $.inArray(i, _partIndex) >= 0) {
                    if (closeVertex) { 
                        wkt = addVertexFunc(wkt + ',', closeVertex);
                        closeVertex = vertices[i];
                    }
                    wkt += '),(';
                } else {
                    wkt += ',';
                }
            }
            wkt = addVertexFunc(wkt, vertices[i]);
        }

        if (closeVertex) {
            wkt = addVertexFunc(wkt + ',', closeVertex);
        }

        wkt += isMultipart ? ")" : "";
        if (wkt !== '') {
            if (_geometryType === 'polygon' || _geometryType === 'circle') {
                wkt += ")";
            }
            wkt += ")";
        }
        return wkt;
    };

    this.cloneGraphicElements = function () {
        var elements = [];
        var partIndex = 0;
        var element;
        if (webgis.mapFramework === "leaflet") {
            if (_geometryType === 'rectangle') {
                //console.log('rectangle has valid bounds', _frameworkElements[_currentFrameworkIndex].getBounds().isValid(), _frameworkElements[_currentFrameworkIndex].getBounds());
                var bounds = _frameworkElements[_currentFrameworkIndex].getBounds();
                if (bounds.getWest() !== bounds.getEast() && bounds.getSouth() !== bounds.getNorth()) {
                    element = this._createFrameworkElement();
                    element.setBounds(_frameworkElements[_currentFrameworkIndex].getBounds());
                    elements.push(element);
                }
            } else {
                element = this._createFrameworkElement();
                elements.push(element);

                var partLatLngs = [], latlngs=[];
                for (var i = 0; i < _vertices.length; i++) {
                    if (i == 0 || _partIndex[partIndex] == i) {
                        if (latlngs.length > 0) {
                            partLatLngs.push(latlngs);
                            latlngs = [];
                        }
                        partIndex++;
                    }
                    var vertex = _vertices[i];
                    if (element.addLatLng) {
                        latlngs.push(L.latLng(vertex.y, vertex.x));;
                    } else {
                        element.setLatLng(L.latLng(vertex.y, vertex.x));
                    }
                }

                if (latlngs.length > 0) {
                    partLatLngs.push(latlngs);
                }

                //console.log(partLatLngs);
                if (partLatLngs.length == 1) {
                    element.setLatLngs(partLatLngs[0]);
                } else if (partLatLngs.length) {
                    element.setLatLngs(partLatLngs);
                }
            }
        }
        return elements;
    };

    this.fromFrameworkElement = function (frameworkElement) {
        var jsonGeometry = frameworkElement.toGeoJSON().geometry;

        if (frameworkElement._webgis) {
            //console.log(frameworkElement._webgis);
            switch (frameworkElement._webgis.geometryType) {
                case 'freehand':
                    jsonGeometry.type = 'freehand';
                    break;
                case 'distance_circle':
                    jsonGeometry.type = 'distance_circle';
                    _distance_circle_radius = frameworkElement.getRadius();
                    _distance_circle_steps = frameworkElement.getSteps();
                    break;
                case 'compass_rose':
                    jsonGeometry.type = 'compass_rose';
                    _compass_rose_radius = frameworkElement.getRadius();
                    _compass_rose_steps = frameworkElement.getSteps();
                    break;
                case 'circle':
                    jsonGeometry.type = 'circle';
                    _circle_radius = frameworkElement.getRadius();
                    break;
                case 'rectangle':
                    jsonGeometry.type = 'rectangle';
                    break;
                case 'dimline':
                    jsonGeometry.type = 'dimline';
                    break;
                case 'dimpolygon':
                    jsonGeometry.type = 'dimpolygon';
                    _dimPolygon_areaUnit = frameworkElement.getAreaUnit();
                    _dimPolygon_labelEdges = frameworkElement.getLabelEdges();
                    break;
                case 'hectoline':
                    jsonGeometry.type = 'hectoline';
                    _hectoline_unit = frameworkElement.getUnit();
                    _hectoline_interval = frameworkElement.getInterval();
                    break;
                case 'point':
                    jsonGeometry.type = 'point';
                    break;
                    break;
                case 'text':
                    jsonGeometry.type = 'text';
                    _svg_text = frameworkElement.getText();
                    break;
            }
        }

        var geomType = jsonGeometry.type;

        //console.log('frameworkElement.options', frameworkElement.options)

        if (webgis.mapFramework === "leaflet" && frameworkElement.options) {
            this.setColor(frameworkElement.options.color || this.getColor(geomType), geomType);
            this.setOpacity(frameworkElement.options.opacity || this.getOpacity(geomType), geomType);
            this.setFillColor(frameworkElement.options.fillColor || this.getFillColor(geomType), geomType);
            this.setFillOpacity(frameworkElement.options.fillOpacity || this.getFillOpacity(geomType), geomType);
            this.setWeight(frameworkElement.options.weight || this.getWeight(geomType), geomType);

            if (frameworkElement._webgis && frameworkElement._webgis.lineStyle) {
                this.setLineStyle(frameworkElement._webgis.lineStyle, geomType);
            } else if (frameworkElement.options.dashArray) {
                this.setLineStyle(this._fromDashArray(frameworkElement.options.dashArray, geomType), geomType);
            } else {
                this.setLineStyle(null, geomType);
            }
            this.setTextSize(frameworkElement.options.fontSize || this.getTextSize(geomType), geomType);
            this.setTextStyle(frameworkElement.options.fontStyle || this.getTextStyle(geomType), geomType);
            this.setTextColor(frameworkElement.options.fontColor || this.getTextColor(geomType), geomType);
            _txtRefResolution = frameworkElement.options.refResolution || _txtRefResolution;
        }

        this.fromJson(jsonGeometry);

        //switch (jsonGeometry.type) {
        //    case 'text':
        //        console.log(_frameworkElements[_currentFrameworkIndex - 1]);
        //        _frameworkElements[_currentFrameworkIndex - 1].setText(text);
        //        break;
        //}

        if (jsonGeometry.type === 'freehand') {
            this.appendPart(); // Close it... => kein Weiterzeichnen von Freehand soll möglich sein!!  Aber hier nicht close verwenden;
        }
    };
};
