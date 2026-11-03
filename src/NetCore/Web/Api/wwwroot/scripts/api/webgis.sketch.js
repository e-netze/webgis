webgis.sketch = function (map) {
    "use strict"

    var $ = webgis.$;

    webgis.implementEventController(this);
    this.map = map;
    this.ui = new webgis.sketch._ui(this);

    // vertices and parts => webgis.sketch.vertices.js
    // _vertices is the (stable) vertex array of the model => read only here, change it with _model methods
    var _model = new webgis.sketch.vertices(), _vertices = _model.vertices, _undos = [];
    var _geometryType = '', _originalSrs = 0, _originalSrsP4Params = null;
    var _frameworkElements = [], _currentFrameworkIndex = 0;
    var _vertexFrameworkElements = [];
    var _txtRefResolution = 0;
    var _sketchProperties = null;
    var _sketchAdvancedProperties = null;
    var _isDraggingMarker = false;
    this._lastChangeTime = new Date().getTime();
    var _ortho = false, _trace = false, _fan = false;
    this._isDirty = false;

    /********* Geometry types *********/
    // the type specific behavior is implemented in webgis.sketch.geometry.<type>.js
    // state => access to the closure variables for the geometry types
    var _state = {
        get vertices() { return _vertices; },
        get model() { return _model; },
        get geometryType() { return _geometryType; }, set geometryType(value) { _geometryType = value; },
        get frameworkElements() { return _frameworkElements; },
        get currentFrameworkIndex() { return _currentFrameworkIndex; }, set currentFrameworkIndex(value) { _currentFrameworkIndex = value; },
        get vertexFrameworkElements() { return _vertexFrameworkElements; },
        get sketchAdvancedProperties() { return _sketchAdvancedProperties; },
        get txtRefResolution() { return _txtRefResolution; },
        get suppressMover() { return _suppressMover; }, set suppressMover(value) { _suppressMover = value; },
        get ortho() { return _ortho; }, set ortho(value) { _ortho = value; },
        get trace() { return _trace; },
        get sketchProperties() { return _sketchProperties; }
    };
    // context for the geometry types and the sketch components (markers, ...)
    var _geometryTypeContext = {
        sketch: this,
        state: _state,
        typeOf: function (geometryType) { return _typeOf(geometryType); },
        type: function () { return _type(); }
    };
    var _geometryTypeInstances = {};
    var _typeOf = function (geometryType) {
        var name = webgis.sketch.geometryTypes.resolve(geometryType);
        if (!_geometryTypeInstances.hasOwnProperty(name)) {
            _geometryTypeInstances[name] = webgis.sketch.geometryTypes.create(name, _geometryTypeContext);
        }
        return _geometryTypeInstances[name];
    };
    var _type = function () { return _typeOf(_geometryType); };

    var _isLineOrPolygon = function () { return _type().isLineOrPolygon === true; };
    var _isLine = function () { return _type().isLine === true; };
    var _isPolygon = function () { return _type().isPolygon === true; };
    var _isSimplePoint = function () { return _type().isSimplePoint === true; };

    var _markers = new webgis.sketch.markers(_geometryTypeContext);
    var _style = new webgis.sketch.style(_geometryTypeContext);
    var _transform = new webgis.sketch.transform(_geometryTypeContext);
    var _mover = new webgis.sketch.mover(_geometryTypeContext);
    var _input = new webgis.sketch.input(_geometryTypeContext, { mover: _mover, markers: _markers });

    this._resetMarkerImage = function (frameworkElement) {
        frameworkElement.setIcon(webgis.createMarkerIcon({ vertex: frameworkElement.vertex_index + 1, icon: 'sketch_vertex' }));
    };
    this._addToMap = function (element) {
        element.addTo(this.map.frameworkElement);
    };
    this._createFrameworkElement = function (latLngs) {
        return _type().createFrameworkElement(latLngs);
    };

    this._isFirstPartVertex = function (index, partIndex) {
        return partIndex ? webgis.sketch.vertices.isFirstPartVertex(partIndex, index) : _model.isFirstPartVertex(index);
    };
    this._moverMarker = null;
    this._moverLine = null;
    this._constructMover = null;
    this._isReadonly = false;
    this._isEditable = false;

    var _cloneArray = function (arr) {
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
            parts: _model.getPartIndex(),
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

    this.setAdvancedProperties = function (properties) {
        _sketchAdvancedProperties = properties || null;
        if (!properties) return;

        for (var frameworkElement of _frameworkElements) {
            this._setElementAdvancedProperties(frameworkElement, properties);
        }
    };
    this._setElementAdvancedProperties = function (element, properties) {
        if (!element || !properties) return;

        if (element instanceof L.GraphicsElementSeries) {
            if (properties.element_width && properties.element_height) {
                element.setElementSize(properties.element_width, properties.element_height);
            }
        }
    };

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
        if (geom === 'multipoint' && _geometryType === 'elementseries')
            geom = 'elementseries'; // keep
        if (geom.toLocaleLowerCase() != _geometryType) {
            _geometryType = geom.toLowerCase();
            this.remove(true);
            return;
        }
        else {
            this.hide();
        }

        _frameworkElements = [];  // empty before render: createFrameworkElement reads e.g. the radius from the current element
        _frameworkElements = _type().render(_model);

        webgis.sketch.construct.cancel(this);

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
        if (_frameworkElements) {
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
        _model.clear();

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

        for (var i in _frameworkElements)
            this._addToMap(_frameworkElements[i]);
        for (var i in _vertexFrameworkElements) {
            this._addToMap(_vertexFrameworkElements[i]);
        }
    };
    this.hide = function () {
        if (_frameworkElements == null || this.map == null || this.map.frameworkElement == null)
            return;

        if (this.map) {
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
            this.setGeometryType(_geometryType);
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
    this.resetMarkerDragging = function () { _markers.resetDragging(); };
    /********* Vertex markers => webgis.sketch.markers.js *********/
    this._appendMarkerEvents = function (marker) {
        _markers.appendEvents(marker, this._menu);
    };
    this.setMarkerTooltip = function (vertex, text) {
        _markers.setTooltip(vertex, text);
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

        for (var v = 0, to = vertices.length; v < to; v++) {
            var vertex = vertices[v];
            if (!vertex || !vertex.x || !vertex.y)
                continue;
            if (!this.checkMaxBBox(vertex))
                break;
            var geometryTypeDef = _type();
            geometryTypeDef.beforeAddVertex(vertex);

            if (!geometryTypeDef.tryAddVertex(vertex, vertices, fireEvents, coords)) {
                var coords = vertex;
                if (fireEvents) {
                    this.events.fire('beforeaddvertex', this, coords);
                    if (coords.ignore === true) {
                        continue;
                    }
                }

                geometryTypeDef.pushVertex(coords);
                var lastVertex = null;

                var elementCount = _frameworkElements.length;
                _currentFrameworkIndex = geometryTypeDef.renderAddedVertex(_model, _frameworkElements, _currentFrameworkIndex, vertex);
                for (var e = elementCount; e < _frameworkElements.length; e++) {  // fan mode: new element for every ray
                    this.map.frameworkElement.addLayer(_frameworkElements[e]);
                }

                if (!readOnly && !this.isReadOnly()) {    
                    if (geometryTypeDef.addsVertexMarker()) {
                        var icon = "sketch_vertex";
                        if (geometryTypeDef.vertexMarkerIcon) {
                            icon = geometryTypeDef.vertexMarkerIcon;
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
    };
    this._addFreehandVertexMarker = function () {
        if (_vertices.length === 1)
            return true;

        if (_vertices.length % 10 === 0)
            return true;

        if (_vertices.length>0 && _vertices.length === _typeOf('freehand').lastVertexIndex + 1)
            return true;

        return false;
    };

    /********* Mouse feedback (mover line/marker, trace line) => webgis.sketch.mover.js *********/
    this.hideMoverLine = function () { _mover.hideLine(); };
    this.showMoverLine = function (latLngs) { _mover.showLine(latLngs); };
    this.hideMoverMarker = function () { _mover.hideMarker(); };
    this.hideTraceLine = function () { _mover.hideTraceLine(); };
    this.hideConstructMover = function () { _mover.hideConstructMover(); };

    this.addVertex = function (vertex) {
        if (_markers.isBubbleMoving()) {
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
            var undoKey = _typeOf(geometryType).addVertexUndoKey;
            if (undoKey) {
                sketch.addUndo(webgis.l10n.get(undoKey));
            }
        }

        if ((_isPolygon() && webgis.usability.sketch.checkForOverlappingPolygonSegments === true) ||
            (_isLine() && webgis.usability.sketch.checkForOverlappingPolylineSegments === true)) {
            var currentVertex = this.lastVertexInCurrentPart(0);
            var prevVertex = this.lastVertexInCurrentPart(1);

            if (currentVertex && prevVertex) {

                if (webgis.calc.isParallel(prevVertex, currentVertex, currentVertex, vertex) == -1) {
                    webgis.log('overlays with prev segment');

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

                    this.events.fire('onchangevertex', this, vertex);
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

    this._addAsLatLng = function (frameworkElement, vertex) {
        const latLng = frameworkElement.vertexToLatLng
            ? frameworkElement.vertexToLatLng(vertex)
            : L.latLng(vertex.y, vertex.x);

        if (frameworkElement.addLatLng) {
            frameworkElement.addLatLng(latLng);
        }
        else {
            frameworkElement.setLatLng(latLng);
        }
    }

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

    // map input handlers => webgis.sketch.input.js
    // mapOnRightClick/mapOnZoomEnd: same function references for map.events.off in destroy
    var mapOnRightClick = function (e, map, clickEvent) { _input.onRightClick(map, clickEvent); };
    var mapOnZoomEnd = function (e, map) {
        _markers.cancelBubbleMove(map);
    };
    this.map.events.on('rightclick', mapOnRightClick, this);
    this.map.events.on(['moveend', 'zoomend'], mapOnZoomEnd, this);
    this._mapMousePressed = false;

    this.onMapMousedown = function (map, e) { return _input.onMouseDown(map, e); };
    this.onMapMouseup = function (map, e) { return _input.onMouseUp(map, e); };

    var _suppressMover = false;

    this.onMapMouseMove = function (map, latlng) { return _input.onMouseMove(map, latlng); };
    this.onMapKeyDown = function (map, e) { return _input.onKeyDown(map, e); };
    this.onMapKeyUp = function (map, e) { return _input.onKeyUp(map, e); };

    // snapping + mover marker => webgis.sketch.mover.js
    this._showMoveMarker = function (latlng) { _mover.showMarker(latlng); };

    this._fixedDirectionVector = null;
    this._fixedDistance = null;
    this._snappedVertex = null;
    // screen point based => webgis.sketch.vertices.closestVertex/closestEdge
    var _toLayerPoint = function (map) {
        return function (vertex) { return map.frameworkElement.latLngToLayerPoint(L.latLng(vertex.y, vertex.x)); };
    };
    this._closestVertex = function (map, pos) { return _model.closestVertex(_toLayerPoint(map), pos); };
    this._closestEdge = function (map, pos) {
        if (_isLineOrPolygon(_geometryType)) {
            return _model.closestEdge(_toLayerPoint(map), pos, _type().hasClosingSegment);
        }
        return null;
    };

    this.canReverse = function () {
        return _isLine(_geometryType) && _vertices.length > 1 && _model.count() === 1;
    };
    this.reverse = function () {
        if (this.canReverse()) {
            this.addUndo(webgis.l10n.get("sketch-tool-reverse"));

            var vertices = _vertices.slice(0).reverse();

            _model.setVertices([]);  // empty before remove => remove fires no 'ongetsinvalid'
            this.remove(true);
            this.addVertices(vertices);
        }
    };

    // null => default: 'f' key for lines and polygons, context menu for polyline and polygon
    this.canCloseSection = function () {
        return _type().canCloseSection();
    };
    this.canMergeParts = function () {
        if (this.map.construct && _type().supportsMergeParts && _model.count() > 1) {
            return this.map.construct.mergePaths(this.getParts()) != null;
        }
        return false;
    };
    this.mergeParts = function () {
        if (this.canMergeParts()) {
            var vertices = this.map.construct.mergePaths(this.getParts());

            if (vertices != null) {
                this.addUndo(webgis.l10n.get("sketch-merge-sections"));

                _model.setVertices([]);  // empty before remove => remove fires no 'ongetsinvalid'
                this.remove(true);
                this.addVertices(vertices);
            }
        }
    };

    this.infoTextLines = function () {
        var properties = this.calcProperties("X", "Y");

        var infoText = [];

        if (_model.count() > 1) {
            infoText.push("Multipart: " + _model.count() + " " + webgis.l10n.get("sections"));
        }

        _type().infoTextLines(infoText, properties);

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

        this.events.fire('currentstate', args);
    };

    /*********************************/
    /******** Undo ************/
    var _suppressUndo = false;
    this.addUndo = function (text) {
        if (_suppressUndo === true)
            return;
        var snapshot = _model.snapshot();
        if (_undos.length >= 10) {
            var undos = new Array();
            for (var i = _undos.length - 10; i < _undos.length; i++)
                undos.push(_undos[i]);
            _undos = undos;
        }
        _undos.push({ vertices: snapshot.vertices, partIndex: snapshot.partIndex, text: text });
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
                vertices.push(webgis.sketch.vertices.copyVertex(undo.vertices[v]));
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

        return _type().calcProperties(xParam, yParam);
    };
    this.getCoords = function () {
        return _model.collect(function (vertex) { return { x: vertex.x, y: vertex.y }; });
    };

    this.getVertices = function (xParam, yParam) {
        if (!xParam)
            xParam = 'x';
        if (!yParam)
            yParam = 'y';
        return _model.collect(function (vertex) { return [vertex[xParam], vertex[yParam]]; });
    };
    this._getRawVertices = function () { return _vertices; }
    this.getVerticesPro = function (xParam, yParam, part) {
        if (!xParam)
            xParam = 'x';
        if (!yParam)
            yParam = 'y';
        return _model.collect(function (vertex) {
            var result = {};
            result[xParam] = vertex[xParam];
            result[yParam] = vertex[yParam];
            if (vertex.fixed === true) {
                result.fixed = true;
            }
            if (vertex.selected === true) {
                result.selected = true;
            }
            return result;
        });
    };
    this.getNeighborVertices = function (index, includeVertex) {
        if (!_isLineOrPolygon()) {
            return [];
        }
        return _model.neighbors(parseInt(index), includeVertex, _isPolygon());
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
                indices = _model.indicesInBounds(minX, minY, maxX, maxY);
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

        return vertices;
    };
    this.verticesHasProperty = function (propertyName) {
        return _model.allHaveProperty(propertyName);
    };
    this.partCount = function () { return _model.count(); };
    this.partVertices = function (partNumber) {
        return _model.partVertices(partNumber);
    };
    this.getParts = function () {
        var parts = [];
        for (var p = 0, to = this.partCount(); p < to; p++) {
            parts.push(this.partVertices(p));
        }
        return parts;
    };
    this.getVertexPartNumber = function (index) {
        return _model.partNumber(index);
    };
    this.getCurrentPartLength = function () {
        return _model.currentPartLength();
    };
    this.isValid = function () {
        return _type().isValid();
    };
    // bounds of the vertices (+ addLngLat) => webgis.sketch.vertices.bounds
    this.getBounds = function (projected, addLngLat) {
        var addVertex = null;
        if (addLngLat) {
            addVertex = projected ? this.map.construct.toProjectedVertex(this.map.calcCrs(), addLngLat) : { x: addLngLat.lng, y: addLngLat.lat };
        }
        return _model.bounds(projected, addVertex);
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
                webgis.alert(webgis.l10n.get('sketch-max-size-exceeded'), 'info');
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

    this.isInTraceMode = function () { return _trace === true; };
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
           
            this.redraw(true);

            // reset to last framework element, so next vertex is added to last element
            _currentFrameworkIndex = _frameworkElements.length - 1;

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
        for (var f in _frameworkElements) {
            if (_frameworkElements[f]) {
                try {
                    if (_frameworkElements[f].setStyle) {
                        _frameworkElements[f].setStyle(style);
                    }
                } catch (ex) {
                }
            }
        }
    };
    /********* Style values => webgis.sketch.style.js *********/
    this.setColor = function (col, geomType) { this.setStyle({ color: _style.set('color', geomType, col) }); };
    this.setOpacity = function (opacity, geomType) { this.setStyle({ opacity: _style.set('opacity', geomType, opacity) }); };
    this.setFillColor = function (col, geomType) { this.setStyle({ fillColor: _style.set('fillColor', geomType, col) }); };
    this.setFillOpacity = function (opacity, geomType) { this.setStyle({ fillOpacity: _style.set('fillOpacity', geomType, opacity) }); };
    this.setWeight = function (weight, geomType) {
        this.setStyle({ weight: _style.set('weight', geomType, weight) });
        this.setStyle({ dashArray: this._toDashArray(_style.get('lineStyle', geomType)) });
    };
    this.setLineStyle = function (style, geomType) {
        _style.set('lineStyle', geomType, style);

        if (!_frameworkElements[_currentFrameworkIndex]._webgis) {
            _frameworkElements[_currentFrameworkIndex]._webgis = {};
        }

        _frameworkElements[_currentFrameworkIndex]._webgis.lineStyle = style;
        this.setStyle({ dashArray: this._toDashArray(style) });
    };
    this.setTextColor = function (col, geomType) { this.setStyle({ fill: _style.set('textColor', geomType, col) }); };
    this.setTextStyle = function (style, geomType) { this.setStyle({ "font-style": _style.set('textStyle', geomType, style) }); };
    this.setTextSize = function (size, geomType) { this.setStyle({ "font-size": _style.set('textSize', geomType, size) }); };

    this.getColor = function (geomType) { return _style.get('color', geomType); };
    this.getOpacity = function (geomType) { return _style.get('opacity', geomType); };
    this.getFillColor = function (geomType) { return _style.get('fillColor', geomType); };
    this.getFillOpacity = function (geomType) { return _style.get('fillOpacity', geomType); };
    this.getWeight = function (geomType) { return _style.get('weight', geomType); };
    this.getLineStyle = function (geomType) { return _style.get('lineStyle', geomType); };
    this.getTextColor = function (geomType) { return _style.get('textColor', geomType); };
    this.getTextStyle = function (geomType) { return _style.get('textStyle', geomType); };
    this.getTextSize = function (geomType) { return _style.get('textSize', geomType); };

    this._toDashArray = function (arrayString, geomType) { return _style.toDashArray(arrayString, geomType); };
    this._fromDashArray = function (array, geomType) { return _style.fromDashArray(array, geomType); };

    /********* Type specific options => elementOptions in webgis.sketch.geometry.<type>.js *********/
    this.setCircleRadius = function (radius) { _typeOf('circle').setOption('radius', radius); };
    this.setDistanceCircleRadius = function (radius) { _typeOf('distance_circle').setOption('radius', radius); };
    this.setDistanceCircleSteps = function (steps) { _typeOf('distance_circle').setOption('steps', steps); };
    this.setCompassRoseRadius = function (radius) { _typeOf('compass_rose').setOption('radius', radius); };
    this.setCompassRoseSteps = function (steps) { _typeOf('compass_rose').setOption('steps', steps); };
    this.setHectolineUnit = function (unit) { _typeOf('hectoline').setOption('unit', unit); };
    this.setHectolineInterval = function (interval) { _typeOf('hectoline').setOption('interval', interval); };
    this.setDimLineLengthUnit = function (unit) { _typeOf('dimline').setOption('lengthUnit', unit); };
    this.setDimLineLabelTotalLength = function (doLabel) { _typeOf('dimline').setOption('labelTotalLength', doLabel); };
    this.setDimPolygonAreaUnit = function (unit) { _typeOf('dimpolygon').setOption('areaUnit', unit); };
    this.setDimPolygonLabelEdges = function (doLabel) { _typeOf('dimpolygon').setOption('labelEdges', doLabel); };
    this.setDimLineAngleUnit = function (unit) { _typeOf('dimline').setOption('angleUnit', unit); };
    this.setDimLineAngleType = function (angleType) { _typeOf('dimline').setOption('angleType', angleType); };
    this.setDimPolygonAngleUnit = function (unit) { _typeOf('dimpolygon').setOption('angleUnit', unit); };
    this.setViewAngleAngle = function (angle) { _typeOf('view_angle').setOption('angle', angle); };
    this.setViewAngleUnit = function (unit) { _typeOf('view_angle').setOption('unit', unit); };
    this.setViewAngleLabelLength = function (doLabel) { _typeOf('view_angle').setOption('labelLength', doLabel); };
    this.setIntersectionAngleUnit = function (unit) { _typeOf('intersection_angle').setOption('unit', unit); };
    this.setIntersectionAngleAngles = function (angles) { _typeOf('intersection_angle').setOption('angles', angles); };
    this.setCircleArcUnit = function (unit) { _typeOf('circle_arc').setOption('unit', unit); };
    this.setCircleArcLabels = function (labels) { _typeOf('circle_arc').setOption('labels', labels); };
    this.setPerpendicularStationFormat = function (format) { _typeOf('perpendicular').setOption('stationFormat', format); };
    this.setPerpendicularLengthUnit = function (unit) { _typeOf('perpendicular').setOption('lengthUnit', unit); };
    this.setBufferZoneSourceType = function (sourceType) {
        _typeOf('buffer_zone').setOption('sourceType', sourceType);
        if (_geometryType === 'buffer_zone') {
            _typeOf('buffer_zone').onSourceTypeChanged();
        }
    };
    this.setBufferZoneWidth = function (width) { _typeOf('buffer_zone').setOption('width', width); };
    // key: identifies the geometry/options the buffer was calculated for (L.BufferZone.key), null => current
    // returns false, if the key doesn't match the current sketch (stale server response)
    this.setBufferZoneBuffer = function (rings, key) {
        var element = _geometryType === 'buffer_zone' ? _frameworkElements[_currentFrameworkIndex] : null;
        var currentKey = element && element.getBufferKey ? element.getBufferKey() : null;
        if (!currentKey || (key && key !== currentKey))
            return false;

        _typeOf('buffer_zone').setOption('buffer', rings ? { key: currentKey, rings: rings } : null);
        return true;
    };
    this.setObjectDistanceLengthUnit = function (unit) { _typeOf('object_distance').setOption('lengthUnit', unit); };
    this.setObjectDistanceShowNumber = function (show) { _typeOf('object_distance').setOption('showNumber', show); };
    // object A/B (webgis.map.graphics => server 'object-distance-pick/-calc'), see webgis.sketch.geometry.object_distance.js
    this.getObjectDistanceData = function () { return _geometryType === 'object_distance' ? _typeOf('object_distance').getData() : null; };
    this.setObjectDistanceA = function (candidate, latLng) { if (_geometryType === 'object_distance') _typeOf('object_distance').setA(candidate, latLng); };
    this.addObjectDistanceLine = function (line) { if (_geometryType === 'object_distance') _typeOf('object_distance').addLine(line); };
    this.addObjectDistanceLines = function (lines) { return _geometryType === 'object_distance' ? _typeOf('object_distance').addLines(lines) : 0; };
    this.hasPerpendicularReferenceLine = function () { return _geometryType === 'perpendicular' && _typeOf('perpendicular').hasReferenceLine(); };
    this.addPerpendicularPoints = function (points) { return _geometryType === 'perpendicular' ? _typeOf('perpendicular').addPoints(points) : null; };
    this.removeObjectDistanceLine = function (index) { if (_geometryType === 'object_distance') _typeOf('object_distance').removeLine(index); };
    this.setMeasureGridCellWidth = function (width) { if (parseFloat(width) > 0) _typeOf('measure_grid').setOption('cellWidth', parseFloat(width)); };
    this.setMeasureGridCellHeight = function (height) { if (parseFloat(height) > 0) _typeOf('measure_grid').setOption('cellHeight', parseFloat(height)); };
    this.setMeasureGridLabels = function (labels) { _typeOf('measure_grid').setOption('labels', labels); };
    this.setMeasureGridAlignToGrid = function (align) {
        _typeOf('measure_grid').setOption('alignToGrid', align === true || align === 'true');
        if (_geometryType === 'measure_grid') {
            _typeOf('measure_grid').onAlignToGridChanged();
        }
    };
    // moves the grid, so that the origin (corner of cell A1) is at X/Y (calc crs), see webgis.sketch.geometry.measure_grid.js
    this.setMeasureGridOrigin = function (X, Y) { return _geometryType === 'measure_grid' ? _typeOf('measure_grid').setOrigin(X, Y) : false; };
    this.getMeasureGrid = function () { return _geometryType === 'measure_grid' ? _typeOf('measure_grid').getGrid() : null; };
    this.setText = function (text) { _typeOf('text').setOption('text', text); };

    this.getText = function () { return _typeOf('text').getOption('text'); };
    this.getCircleRadius = function () { return _typeOf('circle').getOption('radius'); };
    this.getDistanceCircleRadius = function () { return _typeOf('distance_circle').getOption('radius'); };
    this.getDistanceCircleSteps = function () { return _typeOf('distance_circle').getOption('steps'); };
    this.getCompassRoseRadius = function () { return _typeOf('compass_rose').getOption('radius'); };
    this.getCompassRoseSteps = function () { return _typeOf('compass_rose').getOption('steps'); };
    this.getHectolineUnit = function () { return _typeOf('hectoline').getOption('unit'); };
    this.getHectolineInterval = function () { return _typeOf('hectoline').getOption('interval'); };
    this.getDimLineLengthUnit = function () { return _typeOf('dimline').getOption('lengthUnit'); };
    this.getDimLineLabelTotalLength = function () { return _typeOf('dimline').getOption('labelTotalLength'); };
    this.getDimPolygonAreaUnit = function () { return _typeOf('dimpolygon').getOption('areaUnit'); };
    this.getDimPolygonLabelEdges = function () { return _typeOf('dimpolygon').getOption('labelEdges'); };
    this.getDimLineAngleUnit = function () { return _typeOf('dimline').getOption('angleUnit'); };
    this.getDimLineAngleType = function () { return _typeOf('dimline').getOption('angleType'); };
    this.getDimPolygonAngleUnit = function () { return _typeOf('dimpolygon').getOption('angleUnit'); };
    this.getViewAngleAngle = function () { return _typeOf('view_angle').getOption('angle'); };
    this.getViewAngleUnit = function () { return _typeOf('view_angle').getOption('unit'); };
    this.getViewAngleLabelLength = function () { return _typeOf('view_angle').getOption('labelLength'); };
    this.getIntersectionAngleUnit = function () { return _typeOf('intersection_angle').getOption('unit'); };
    this.getIntersectionAngleAngles = function () { return _typeOf('intersection_angle').getOption('angles'); };
    this.getCircleArcUnit = function () { return _typeOf('circle_arc').getOption('unit'); };
    this.getCircleArcLabels = function () { return _typeOf('circle_arc').getOption('labels'); };
    this.getPerpendicularStationFormat = function () { return _typeOf('perpendicular').getOption('stationFormat'); };
    this.getPerpendicularLengthUnit = function () { return _typeOf('perpendicular').getOption('lengthUnit'); };
    this.getBufferZoneSourceType = function () { return _typeOf('buffer_zone').getOption('sourceType'); };
    this.getBufferZoneWidth = function () { return _typeOf('buffer_zone').getOption('width'); };
    this.getBufferZoneBuffer = function () { return _typeOf('buffer_zone').getOption('buffer'); };
    this.getObjectDistanceLengthUnit = function () { return _typeOf('object_distance').getOption('lengthUnit'); };
    this.getObjectDistanceShowNumber = function () { return _typeOf('object_distance').getOption('showNumber'); };
    this.getMeasureGridCellWidth = function () { return _typeOf('measure_grid').getOption('cellWidth'); };
    this.getMeasureGridCellHeight = function () { return _typeOf('measure_grid').getOption('cellHeight'); };
    this.getMeasureGridLabels = function () { return _typeOf('measure_grid').getOption('labels'); };
    this.getMeasureGridAlignToGrid = function () { return _typeOf('measure_grid').getOption('alignToGrid'); };
    this.getBufferZoneBufferKey = function () {
        var element = _geometryType === 'buffer_zone' ? _frameworkElements[_currentFrameworkIndex] : null;
        return element && element.getBufferKey ? element.getBufferKey() : null;
    };
    this.hasValidBufferZoneBuffer = function () {
        var element = _geometryType === 'buffer_zone' ? _frameworkElements[_currentFrameworkIndex] : null;
        return element && element.getValidBufferRings ? element.getValidBufferRings() !== null : false;
    };
    // source geometry of the current element (same as the buffer key): { latLngs, partStarts }
    this.getBufferZoneSource = function () {
        var element = _geometryType === 'buffer_zone' ? _frameworkElements[_currentFrameworkIndex] : null;
        return element && element.getPartStarts ? { latLngs: element.getLatLngs() || [], partStarts: element.getPartStarts() } : null;
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
    /********* Bubble move (touch devices) => webgis.sketch.markers.js *********/
    this.startBubbleMoveVertex = function (index) { _markers.startBubbleMove(index); };
    this.endBubbleMoveVertex = function () { _markers.endBubbleMove(); };

    /********* Move / rotate sketch => webgis.sketch.transform.js *********/
    this.startSketchMoving = function (index) { _transform.startMoving(index); };
    this.isSketchMoving = function () { return _transform.isMoving(); };
    this.endSketchMoving = function () { _transform.endMoving(); };
    this.moveSketchPreview = function (latlng) { _transform.movePreview(latlng); };
    this.moveSketch = function (latlng) { _transform.move(latlng); };
    this.startSketchRotating = function (index, startAngle) { _transform.startRotating(index, startAngle); };
    this.isSketchRotating = function () { return _transform.isRotating(); };
    this.endSketchRotating = function () { _transform.endRotating(); };
    this.rotateSketchPreview = function (latlng) { _transform.rotatePreview(latlng); };
    this.rotateSketch = function (latlng) { _transform.rotate(latlng); };


    this.removeVertex = function (index, suppressUndo) {
        this.hide();
        if (suppressUndo !== true) {
            this.addUndo(webgis.l10n.get("sketch-remove-vertex"));
        }
        const isValidBeforeRemove = this.isValid();

        for (var i = 0; i < _vertices.length; i++) {
            if (i == index)
                continue;
            if (i > index && _vertexFrameworkElements[i]) {  // freehand: not every vertex has a marker
                _vertexFrameworkElements[i].vertex_index--;
                this._resetMarkerImage(_vertexFrameworkElements[i]);
            }
        }
        _vertexFrameworkElements.splice(index, 1);
        _model.removeVertex(index);

        this.redraw(true);
        this.redrawMarkers();

        if (isValidBeforeRemove === true && this.isValid() === false) {
            this.events.fire('ongetsinvalid', this);
        }
    };
    /******** Fix Vertices **********************/
    this.fixVertex = function (index, fix) {
        if (!_model.fix(index, fix)) {
            return;
        }

        this.redrawMarkers();
    };
    this.snapFixVertex = function (vertex) {
        var editTheme = this.map.getCurrentEditTheme();
        if (editTheme == null || !editTheme.snapping) {
            return false;
        }

        for (var s in editTheme.snapping) {
            var snapping = editTheme.snapping[s];
            if (snapping.fixto) {
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
                    if (snapResult) {
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
        _model.select(index, select);
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
        if (webgis.usability.allowSelectSketchVertices === true) {
            _model.selectAll(false);
        }

        this.redrawMarkers();
    };
    this.toggleVertexSelection = function () {
        if (webgis.usability.allowSelectSketchVertices === true) {
            _model.selectAll();
        }

        this.redrawMarkers();
    };
    this.removeSelectedVertices = function () {
        this.addUndo(webgis.l10n.get("sketch-remove-selected-vertices"));

        while (this.hasSelectedVertices()) {
            this.removeVertex(_model.firstSelectedIndex(), true);
        }
    };
    this.hasSelectedVertices = function () {
        return webgis.usability.allowSelectSketchVertices === true && _model.hasSelected();
    };

    /********************************************/
    this.addPrevVertex = function (index) {
        if (!(index > 0)) {  // no previous vertex => nothing to insert
            return;
        }
        this.addUndo(webgis.l10n.get("sketch-add-vertex"));
        var vertices = _cloneArray(_vertices),
            partIndex = _model.getPartIndex();
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

        _model.setPartIndex(partIndex);

        this.events.enable('onchanged');
        this.events.fire('onchanged', this);
    };
    this.addPostVertex = function (index, t) {
        this.addUndo(webgis.l10n.get("sketch-add-vertex"));
        _suppressUndo = true;

        var vertices = _cloneArray(_vertices),    // clone
            partIndex = _model.getPartIndex();  // clone
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
                    if (_type().hasClosingSegment) {
                        nVertex = vertices[_model.start(pIndex1)];
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
        if (isClosed) {
            this.appendPart();
        }

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
    };
    this.swapDirection = function () {
        var vertices = _cloneArray(_vertices),
            partIndex = _model.getPartIndex();
        this.remove(true);
        _model.setPartIndex(webgis.sketch.vertices.reversedPartIndex(partIndex, vertices.length));
        for (var i = vertices.length - 1; i >= 0; i--) {
            this.addVertex(vertices[i]);
        }
        this.redraw(true);
        _currentFrameworkIndex = Math.min(_model.count(), _frameworkElements.length) - 1;
    };
    this.appendPart = function (latLngs, silent) {
        if (!silent) {
            this.addUndo(webgis.l10n.get("sketch-close-section"));
        }
        if (_vertices.length === 0) {
            _model.clear();
        }
        _model.closePart();
        _frameworkElements = _type().renderAppendPart(_model, _frameworkElements, latLngs);

        _currentFrameworkIndex = _frameworkElements.length - 1;
        this._addToMap(_frameworkElements[_currentFrameworkIndex]);
        this.hideMoverLine();
    }; 
    this.isMultiPart = function () {
        return _model.count() > 1;
    }
    this.vertexPartIndex = function (vertexIndex, partIndex) {
        return partIndex ? webgis.sketch.vertices.partOf(partIndex, vertexIndex) : _model.partOf(vertexIndex);
    };
    this.firstVertexIndexInCurrentPart = function () {
        return _model.currentPartStart();
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
        return _model.isClosed();
    };
    this.isLastPartClosed = function () {
        return _model.isLastPartClosed();
    };
    this.close = function () {
        if (!this.isClosed()) {
            this.appendPart();
            this.events.fire('onsketchclosed');
        }
    };
    this.zoomTo = function (scale) {
        var bounds = this.bounds();
        if (!bounds) return;

        if (scale)
            this.map.zoomToBoundsOrScale(bounds, scale);
        else
            this.map.zoomTo(webgis.calc.resizeBounds(bounds, webgis.usability.zoom.allowsFreeZooming() ? 1.3 : 1.0));
    };
    this.onClick = function (f, remove) {
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
    };
    this.statPoint = function (s) { return _model.statPoint(s); };
    this.bounds = function () {
        // first the extent of the vertices
        var extent = _model.extent();
        if (!extent) return null;

        // than the bounds of the framework elements (parts) if implemented
        // Circles or Series has a larger extent than vertices
        for (var frameworkElement of _frameworkElements) {
            if (frameworkElement?.getBounds) {
                var bounds = frameworkElement.getBounds();
                webgis.log('frameworkElement bounds', bounds);
                if (bounds?.isValid && bounds.isValid()) {
                    extent[0] = Math.min(extent[0], bounds.getWest());
                    extent[1] = Math.min(extent[1], bounds.getSouth());
                    extent[2] = Math.max(extent[2], bounds.getEast());
                    extent[3] = Math.max(extent[3], bounds.getNorth());
                }
            }
        }

        return extent;
    };

    this.metaInfo = function (s) {
        var info = {
            geometryType: _geometryType
        };
        _type().metaInfo(info);
        return info;
    };

    this._referenceVertex = function () {
        if (this.isSketchMoving() || this.isSketchRotating()) {
            return _transform.referenceVertex();
        }

        if (this.isInFanMode()) {
            return _vertices[0];
        }

        if (this.getCurrentPartLength() === 0) {
            return null;
        }

        var index = _type().referenceVertexIndex();
        if (index !== null && index >= 0 && index < _vertices.length) {
            return _vertices[index];
        }

        return _vertices[_vertices.length - 1];
    }

    this.toJson = function (crsId) {
        var ret = {
            geometry: {}, valid: false
        };
        _type().toJson(ret, crsId);
        return ret;
    };
    this.fromJson = function (json, append, readOnly) {
        if (append !== true)
            this.remove(true);

        this.setReadOnly(readOnly === true);

        this.setGeometryType(json.type);
        var crsId = json.SRID;

        _type().fromJson(json, crsId, readOnly);

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

        this._isDirty = false;
    };
    this.fromJsonFast = function (json, append) {
        if (append != true)
            this.remove(true);
        this.setGeometryType(json.type);
        var crsId = json.SRID;

        _type().fromJsonFast(json, crsId);
        this.events.fire('onchanged', this);

        this._isDirty = false;
    };

    /********* WKT => webgis.sketch.wkt.js *********/
    this.toWKT = function (useWGS84) { return webgis.sketch.wkt.toWKT(_model, _type(), useWGS84, this.isInFanMode()); };
    this.toWKT2 = function (useWGS84, appendMeta, digits) { return webgis.sketch.wkt.toWKT2(_model, _type().wktType, useWGS84, appendMeta, digits); };

    this.cloneGraphicElements = function () {
        var elements = [];
        var element;
        var typeElements = _type().cloneGraphicElements();
        if (typeElements) {
            return typeElements;
        } else {
            element = this._createFrameworkElement();
            elements.push(element);

            // Do NOT add vertex by vertex (addLatLng), when cloning
            // otherwise multipart geometries not work correct with MapMarkup
            if (element.addLatLng) {
                var partLatLngs = _model.toLatLngs(false);
                if (partLatLngs.length == 1) {
                    element.setLatLngs(partLatLngs[0]);
                } else if (partLatLngs.length) {
                    element.setLatLngs(partLatLngs);
                }
            } else {
                for (var i = 0; i < _vertices.length; i++) {
                    element.setLatLng(L.latLng(_vertices[i].y, _vertices[i].x));
                }
            }
        }
        return elements;
    };

    this.fromFrameworkElement = function (frameworkElement) {
        var jsonGeometry = frameworkElement.toGeoJSON().geometry;

        if (frameworkElement._webgis) {
            _typeOf(frameworkElement._webgis.geometryType).fromFrameworkElement(frameworkElement, jsonGeometry);
        }
        var geomType = jsonGeometry.type;

        if (frameworkElement.options) {
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

        if (jsonGeometry.type === 'freehand') {
            this.appendPart(); // Close it... => kein Weiterzeichnen von Freehand soll möglich sein!!  Aber hier nicht close verwenden;
        }
    };
};
