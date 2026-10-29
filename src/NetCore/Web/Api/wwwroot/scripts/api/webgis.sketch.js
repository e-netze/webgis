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
    var _vertexFrameworkElements = [], _currentBubbleMoveVertextFrameworkElement = null;
    var _color = ['#ff0000', '#000000'], _fillColor = ['#ffff00', '#aaaaaa'], _opacity = [0.8, 1.0], _fillOpacity = [0.2, 0.2], _weight = [2, 2], _linesStyle = [null, null];
    var _txtColor = ['#000','#000'], _txtStyle = ['',''], _txtSize = [12, 14], _txtRefResolution = 0; 
    var _sketchProperties = null;
    var _sketchAdvancedProperties = null;
    var _isDraggingMarker = false;
    this._lastChangeTime = new Date().getTime();
    var _sketchMovingVertexIndex = -1, _sketchRotatingVertexIndex = -1;
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
        get txtRefResolution() { return _txtRefResolution; }
    };
    var _geometryTypeContext = { sketch: this, state: _state };
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
    var _traceLine = null;
    var _sketchMoveFrameworkElements = [];
    var _sketchRotateFrameworkElements = [];
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
    this.resetMarkerDragging = function () {
        if (this._isMarkerDragging) {
            if (_currentBubbleMoveVertextFrameworkElement != null) {
                _currentBubbleMoveVertextFrameworkElement.setLatLng(_currentBubbleMoveVertextFrameworkElement._dragstartLatLng);
                _currentBubbleMoveVertextFrameworkElement = null;
            }
            this._isMarkerDragging = false;
        }
    };
    /********* Vertex marker events *********/
    // the handlers are called with "this" = marker (marker.sketch = sketch)

    // if the GPS sketch tool is possible, the m-value is used for the GPS accuracy
    // => reset it, if the vertex is moved
    var _resetGpsAccuracy = function (marker) {
        if (marker.sketch_vertex_coords.m &&
            marker.sketch && marker.sketch.map &&
            webgis.currentPosition.canUsedWithSketchTool && webgis.currentPosition.canUsedWithSketchTool(marker.sketch.map.getActiveTool(), marker.sketch)) {
            marker.sketch_vertex_coords.m = 0;
        }
    };
    var _setMarkerClickTolerance = function (marker, clickTolerance) {
        try {
            if (_typeOf(marker.sketch.getGeometryType()).isPlainPath) {
                marker.dragging._draggable.options.clickTolerance = clickTolerance;
            }
        } catch (ex) { }
    };
    var _onMarkerDragStart = function () {
        this.sketch._isMarkerDragging = true;
        this._dragstartLatLng = this.getLatLng();
        this._forceRedrawMarkers = this.sketch_vertex_coords.fixed === true;
        this.sketch_vertex_coords.fixed = null;
        this.sketch.hideMoverLine();
        $(this._icon).addClass('dragging');
    };
    var _onMarkerDragEnd = function () {
        var geomTypeDef = _typeOf(this.sketch.getGeometryType());
        var latlng;
        this.sketch._lastChangeTime = new Date().getTime();
        var newLatLng = this.getLatLng();
        if (!this.sketch.checkMaxBBox({ x: newLatLng.lng, y: newLatLng.lat })) {
            this.setLatLng(this._dragstartLatLng);
            this.sketch.redraw(geomTypeDef.isPlainPath);
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
                _resetGpsAccuracy(this);
            }
            this.setLatLng({ lng: result.x, lat: result.y });
            latlng = this.getLatLng();
            this.sketch._moverMarker.snapResult = null;
        }
        else {
            latlng = this.getLatLng();
            if (!this.sketch_vertex_coords) {
                this.sketch_vertex_coords = { x: latlng.lng, y: latlng.lat };
            } else {
                this.sketch_vertex_coords.x = latlng.lng;
                this.sketch_vertex_coords.y = latlng.lat;
                this.sketch_vertex_coords.projEvent = '';
                _resetGpsAccuracy(this);
            }
        }

        geomTypeDef.onVertexMarkerDragEnd(this, latlng);

        this.sketch.redraw(geomTypeDef.redrawOnVertexMoved);
        this.sketch.events.fire('onchangevertex', this.sketch, this.sketch_vertex_coords);
        this.sketch.events.fire('onchanged', this.sketch);
        this.sketch._isDirty = true;
        this.sketch._isMarkerDragging = false;

        if (this.sketch.snapFixVertex(this.sketch_vertex_coords) || this._forceRedrawMarkers === true) {
            this._forceRedrawMarkers = false;
            this.sketch.redrawMarkers();
        }

        try {
            if (geomTypeDef.isPlainPath) {
                this.dragging._draggable.options.clickTolerance = webgis.usability.mapSketchClickTolerance;
            }
        } catch (ex) { }

        $(this._icon).removeClass('dragging');
    };
    var _onMarkerDrag = function () {
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

        _typeOf(this.sketch.getGeometryType()).onVertexMarkerDrag(this, latlng, latLngs);
    };
    var _onMarkerClick = function (e) {
        // stop propagation
        e.originalEvent.preventDefault();
        var sketch = this.sketch;

        if (webgis.sketch.construct.onMarkerClick(sketch, this) === true) {
            return;
        }

        if (new Date().getTime() - this._creationDate < 1000) {
            sketch.close();
            webgis.delayed(function (marker) {
                if (marker && marker._map)
                    marker._map.closePopup();
            }, 10, this);
            return;
        }

        if (sketch.isClosed()) {
            return;
        }

        var clickedGeomTypeDef = _typeOf(sketch.getGeometryType());
        if (!clickedGeomTypeDef.isPlainPath) {
            return;
        }

        var latlng = this.getLatLng();
        if (clickedGeomTypeDef.hasClosingSegment && this.vertex_index === sketch.firstVertexIndexInCurrentPart()) {
            if (sketch.isInTraceMode() == true) {
                sketch.addVertexCoords(latlng.lng, latlng.lat, true);
            }
            sketch.close();
        } else {
            sketch.addVertexCoords(latlng.lng, latlng.lat, true);
        }
    };
    var _onMarkerMouseOver = function (e) {
        _setMarkerClickTolerance(this, webgis.usability.mapClickTolerance);
        _suppressMover = true;
    };
    var _onMarkerMouseOut = function (e) {
        if (!this.sketch._isMarkerDragging) {
            _setMarkerClickTolerance(this, webgis.usability.mapSketchClickTolerance);
            _suppressMover = false;
        }
    };

    // popup menu for a vertex marker (touch devices, ...)
    var _appendMarkerPopupMenuItem = function ($ul, l10nKey, onClick) {
        $("<li class='webgis-toolbox-tool-item'>").text(webgis.l10n.get(l10nKey)).appendTo($ul)
            .click(function () {
                onClick(this.parentNode.sketch, this.parentNode.vertex_index);
            });
    };
    var _createMarkerPopupMenu = function (marker) {
        var sketch = marker ? marker.sketch : null;
        if (!sketch)
            return webgis.l10n.get('sketch-no-sketch');
        var index = marker.vertex_index;
        var $ul = $('#__sketch_context_menu').length > 0 ? $('#__sketch_context_menu') : $("<ul id='__sketch_context_menu' style='list-style:none;padding:0px'></ul>").appendTo(document.body);
        var ul = $ul.addClass('webgis-toolbox-tool-item-group-details').css('padding-top', 20).get(0);
        ul.sketch = sketch;
        ul.vertex_index = index;
        $ul.empty();
        _appendMarkerPopupMenuItem($ul, 'sketch-delete-vertex', function (sketch, vertexIndex) { sketch.removeVertex(vertexIndex); });
        if (_isLine() || _isPolygon()) {
            if (sketch.hasPrevVertex(index)) {
                _appendMarkerPopupMenuItem($ul, 'sketch-insert-vertex-before', function (sketch, vertexIndex) { sketch.addPrevVertex(vertexIndex); });
            }
            if (sketch.hasPostVertex(index)) {
                _appendMarkerPopupMenuItem($ul, 'sketch-insert-vertex-after', function (sketch, vertexIndex) { sketch.addPostVertex(vertexIndex); });
            }
            _appendMarkerPopupMenuItem($ul, 'sketch-delete-sketch', function (sketch) { sketch.remove(); });
            _appendMarkerPopupMenuItem($ul, 'sketch-reverse-vertex-order', function (sketch) { sketch.swapDirection(); });
            _appendMarkerPopupMenuItem($ul, 'sketch-start-new-section', function (sketch) { sketch.appendPart(); });
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
    };

    this._appendMarkerEvents = function (marker) {
        marker.on('dragstart', _onMarkerDragStart, marker);
        marker.on('dragend', _onMarkerDragEnd, marker);
        marker.on('drag', _onMarkerDrag, marker);
        marker.on('click', _onMarkerClick, marker);
        marker.on('mouseover', _onMarkerMouseOver, marker);
        marker.on('mouseout', _onMarkerMouseOut, marker);
        if (webgis.usability.sketchMarkerPopup === true && webgis.usability.contextMenuBubble === false) {  // Nur anzeigen, wenn keine rechte Maus möglich ist (TouchDevice oder custom.js...)
            webgis.bindMarkerPopup(marker, this._menu, _createMarkerPopupMenu);
        }
        marker._creationDate = new Date().getTime();
    };
    this.setMarkerTooltip = function (vertex, text) {
        try {
            var elements = _typeOf(this.getGeometryType()).frameworkElementIsVertexMarker ? _frameworkElements : _vertexFrameworkElements;
            if (vertex.x && vertex.y) {
                for (var v in elements) {
                    var element = elements[v];
                    var latLng = element.getLatLng();
                    if (Math.abs(latLng.lng - vertex.x) < 1e-7 && Math.abs(latLng.lat - vertex.y) < 1e-7) {
                        element.bindTooltip(text).openTooltip();
                    }
                }
            }
            else {
                if (elements.length > vertex) {
                    var element = elements[vertex];
                    element.bindTooltip(text).openTooltip();
                }
            }
        }
        catch (e) { }
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

            this._mapMousePressed = true;
        }
    };
    this.onMapMouseup = function (map, e) {
        if (e && e.originalEvent && e.originalEvent.button === 0) {  // Left button
            _type().onMapMouseUp();
            this._mapMousePressed = false;
        }
    };

    var _suppressMover = false;

    // the projected vertex at the mouse position (or the snapped vertex)
    var _mouseVertex = function (sketch, latlng) {
        return sketch._snappedVertex || sketch.map.construct.toProjectedVertex(sketch.map.calcCrs(), latlng);
    };
    // ortho mode, fixed direction, fixed distance => moves the mover position (ll) relative to the reference vertex
    var _applyFixedDirectionAndDistance = function (sketch, latlng, ll) {
        var p1, p0, p;
        if (sketch.getCurrentPartLength() === 1 && _ortho === true) {
            p1 = sketch._referenceVertex();
            p0 = _mouseVertex(sketch, latlng);
            var dx = p1.x - p0.x, dy = p1.y - p0.y, len = Math.sqrt(dx * dx + dy * dy);
            dx /= len;
            dy /= len;

            var vec = sketch._fixedDirectionVector ?
                { X: sketch._fixedDirectionVector.X, Y: sketch._fixedDirectionVector.Y } :  // current
                { X: 1.0, Y: 0.0 };  // x-axis

            // cross product (mouse x vec)
            var w1 = dx * vec.Y - dy * vec.X;
            // cross product (mouse x orthogonal(vec))
            var w2 = dx * (-vec.X) - dy * vec.Y;

            // compare cross products (sin(alpah))
            if (Math.abs(w1) <= Math.abs(w2)) {
                sketch._fixedDirectionVector = { X: vec.X, Y: vec.Y };  // take vector
            } else {
                sketch._fixedDirectionVector = { X: -vec.Y, Y: vec.X };  // take ortogonal vector
            }
        }
        if (sketch._fixedDirectionVector) {
            p1 = sketch._referenceVertex();
            p0 = _mouseVertex(sketch, latlng);
            p = sketch.map.construct.projectOrthogonal(p0, p1, sketch._fixedDirectionVector);
            if (p) {
                ll = { lng: p.x, lat: p.y };
                sketch._snappedVertex = p;
            }
        }
        if (sketch._fixedDistance > 0) {
            p1 = sketch._referenceVertex();
            p0 = _mouseVertex(sketch, latlng);
            p = sketch.map.construct.projectLength(p0, p1, sketch._fixedDistance);
            if (p) {
                ll = { lng: p.x, lat: p.y };
                sketch._snappedVertex = p;
            }
        }
        return ll;
    };
    // trace mode: path along the topology from the last vertex to the snapped vertex
    // null => no trace result
    var _traceLatLngs = function (sketch, map) {
        if (!_trace ||
            _vertices.length === 0 ||
            !sketch._snappedVertex ||
            sketch.isSketchMoving() !== false ||
            sketch.isSketchRotating() !== false) {
            return null;
        }

        var lastVertex = _vertices[_vertices.length - 1];
        var lastNodeIndex = map.construct.getTopVertexIndex(lastVertex.X, lastVertex.Y);
        var currentNodeIndex = map.construct.getTopVertexIndex(sketch._snappedVertex.X, sketch._snappedVertex.Y);
        if (!(lastNodeIndex >= 0 && currentNodeIndex >= 0)) {
            return null;
        }

        var result = map.construct.traceNodes(lastNodeIndex, currentNodeIndex);
        if (!result) {
            return null;
        }

        var traceLatLngs = [];
        for (var i in result) {
            var vertex = map.construct.getTopoVertexWGS84(result[i]);
            if (vertex)
                traceLatLngs.push([vertex[1], vertex[0]]);
        }
        return traceLatLngs;
    };
    // latLngs null => remove the trace line
    var _updateTraceLine = function (sketch, latLngs) {
        if (latLngs) {
            if (_traceLine == null) {
                _traceLine = L.polyline(latLngs, { color: 'blue', weight: 3 });
                sketch._addToMap(_traceLine);
            }
            else {
                _traceLine.setLatLngs(latLngs);
            }
        }
        else if (_traceLine != null) {
            sketch.map.frameworkElement.removeLayer(_traceLine);
            _traceLine = null;
        }
    };
    // red => the sketch would exceed the max. bbox
    var _updateMoverLineColor = function (sketch, latlng) {
        if (_sketchProperties && _sketchProperties.maxBBox && !sketch.isSketchMoving() && !sketch.isSketchRotating()) {
            var bbox = sketch.getBounds(true, latlng);
            if (bbox.width > _sketchProperties.maxBBox.width ||
                bbox.height > _sketchProperties.maxBBox.height) {
                sketch._moverLine.setStyle({ color: 'red' });
            }
            else {
                sketch._moverLine.setStyle({ color: 'green' });
            }
        }
    };

    this.onMapMouseMove = function (map, latlng) {
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

        if (_type().handleMapMouseMove(latlng)) {
            return;
        }

        var constructionMode = webgis.sketch.construct.getConstructionMode(this);
        if (constructionMode != null && constructionMode != 'normal') {
            return;
        }

        if (_type().handleFreeDrawMouseMove(latlng)) {
            return;
        }

        var latLngs = [], ll = this._snappedVertex ? { lng: this._snappedVertex.x, lat: this._snappedVertex.y } : { lat: latlng.lat, lng: latlng.lng };
        var referenceVertex = this._referenceVertex();

        if (referenceVertex) {
            ll = _applyFixedDirectionAndDistance(this, latlng, ll);

            latLngs.push([referenceVertex.y, referenceVertex.x]);
            latLngs.push([ll.lat, ll.lng]);
        }

        if (_isLine() || _isPolygon()) {
            if (!this.isClosed()) {
                var traceLatLngs = _traceLatLngs(this, map);
                if (traceLatLngs && traceLatLngs.length > 0) {
                    latLngs = traceLatLngs;
                }
                _updateTraceLine(this, traceLatLngs ? latLngs : null);

                if (_isPolygon() && _vertices.length >= 2 && !this.isSketchMoving() && !this.isSketchRotating()) {
                    latLngs.push([_vertices[_model.currentPartStart()].y, _vertices[_model.currentPartStart()].x]);
                }
            }

            if (!this.isClosed() || this.isSketchMoving() || this.isSketchRotating() && _suppressMover !== true) {
                this.showMoverLine(latLngs);
                _updateMoverLineColor(this, latlng);
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
            $focus.hasClass('webgis-autocomplete') ||
            $focus.closest('.select2-container').length > 0) {
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
            latlng.lng = snapResult.result.x;
            latlng.lat = snapResult.result.y;
            this._snappedVertex = snapResult.result;
        }
        else {
            this._snappedVertex = null;
        }
        // Captured before being overwritten below, so we can tell whether the snap state actually
        // changed (entered/left a snap target) - needed so the "leaving snap" fix below doesn't
        // fire currentstate on every single mouse move (which caused visible overlay flicker).
        var hadSnapResult = this._moverMarker != null && this._moverMarker.snapResult ? true : false;

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
            this.hideConstructMover();

            // Only fire when a snap target was actually left (transition snapped -> unsnapped) -
            // NOT on every plain mouse move without snapping, otherwise the sketch-info overlay
            // re-renders constantly while just moving the mouse and visibly flickers. Callers like
            // the vertex-drag handler ('drag' event on marker) rely solely on this call to keep the
            // overlay's snapping section in sync, since - unlike onMapMouseMove - they don't fire
            // currentstate again on their own afterwards.
            if (hadSnapResult) {
                this.fireCurrentState();
            }
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
            if (_type().hasClosingSegment)
                to++;
            for (var i = 0; i < to; i++) {
                var v1 = _vertices[i], v2 = null;
                var nextPartIndex = (i + 1) >= _vertices.length ? currentPartIndex + 1 : this.vertexPartIndex(i + 1);
                if (nextPartIndex > currentPartIndex) {
                    if (_type().hasClosingSegment) {
                        v2 = _vertices[_model.start(currentPartIndex)];
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
                    bounds.minLng = bounds.maxLng = addLngLat.lng;
                    bounds.minLat = bounds.maxLat = addLngLat.lat;
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

        _typeOf('circle').radius = radius;
    };
    this.setDistanceCircleRadius = function (radius) {
        if (_frameworkElements[_currentFrameworkIndex].setRadius)
            _frameworkElements[_currentFrameworkIndex].setRadius(radius);

        _typeOf('distance_circle').radius = radius;
    };
    this.setDistanceCircleSteps = function (steps) {
        if (_frameworkElements[_currentFrameworkIndex].setSteps)
            _frameworkElements[_currentFrameworkIndex].setSteps(steps);

        _typeOf('distance_circle').steps = steps;
    };
    this.setCompassRoseRadius = function (radius) {
        if (_frameworkElements[_currentFrameworkIndex].setRadius)
            _frameworkElements[_currentFrameworkIndex].setRadius(radius);

        _typeOf('compass_rose').radius = radius;
    };
    this.setCompassRoseSteps = function (steps) {
        if (_frameworkElements[_currentFrameworkIndex].setSteps)
            _frameworkElements[_currentFrameworkIndex].setSteps(steps);

        _typeOf('compass_rose').steps = steps;
    };
    this.setHectolineUnit = function (unit) {
        if (_frameworkElements[_currentFrameworkIndex].setUnit)
            _frameworkElements[_currentFrameworkIndex].setUnit(unit);

        _typeOf('hectoline').unit = unit;
    };
    this.setHectolineInterval = function (interval) {
        if (_frameworkElements[_currentFrameworkIndex].setInterval)
            _frameworkElements[_currentFrameworkIndex].setInterval(interval);

        _typeOf('hectoline').interval = interval;
    };
    this.setDimLineLengthUnit = function (unit) {
        if (_frameworkElements[_currentFrameworkIndex].setLengthUnit)
            _frameworkElements[_currentFrameworkIndex].setLengthUnit(unit);
        _typeOf('dimline').lengthUnit = unit;
    };
    this.setDimLineLabelTotalLength = function (doLabel) {
        if (_frameworkElements[_currentFrameworkIndex].setLabelTotalLength)
            _frameworkElements[_currentFrameworkIndex].setLabelTotalLength(doLabel);
        _typeOf('dimline').labelTotalLength = doLabel;
    };
    this.setDimPolygonAreaUnit = function (unit) {
        if (_frameworkElements[_currentFrameworkIndex].setAreaUnit)
            _frameworkElements[_currentFrameworkIndex].setAreaUnit(unit);

        _typeOf('dimpolygon').areaUnit = unit;
    };
    this.setDimPolygonLabelEdges = function (doLabel) {
        if (_frameworkElements[_currentFrameworkIndex].setLabelEdges)
            _frameworkElements[_currentFrameworkIndex].setLabelEdges(doLabel);

        _typeOf('dimpolygon').labelEdges = doLabel;
    };
    this.setText = function (text) {
        if (_frameworkElements[_currentFrameworkIndex].setText)
            _frameworkElements[_currentFrameworkIndex].setText(text);

        _typeOf('text').text = text;
    };
    this.setTextColor = function (col, geomType) { this.setStyle({ fill: _txtColor[_getStyleIndex(geomType)] = col }); };
    this.setTextStyle = function (style, geomType) { this.setStyle({ "font-style": _txtStyle[_getStyleIndex(geomType)] = style }); };
    this.setTextSize = function (size, geomType) { this.setStyle({ "font-size": _txtSize[_getStyleIndex(geomType)] = size }); };

    this.getText = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getText)
            _typeOf('text').text = _frameworkElements[_currentFrameworkIndex].getText();

        return _typeOf('text').text;
    };
    this.getColor = function (geomType) { return _color[_getStyleIndex(geomType)]; };
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
            _typeOf('circle').radius = _frameworkElements[_currentFrameworkIndex].getRadius();

        return _typeOf('circle').radius;
    };
    this.getDistanceCircleRadius = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getRadius)
            _typeOf('distance_circle').radius = _frameworkElements[_currentFrameworkIndex].getRadius();

        return _typeOf('distance_circle').radius;
    };
    this.getDistanceCircleSteps = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getSteps)
            _typeOf('distance_circle').steps = _frameworkElements[_currentFrameworkIndex].getSteps();

        return _typeOf('distance_circle').steps;
    };
    this.getCompassRoseRadius = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getRadius)
            _typeOf('compass_rose').radius = _frameworkElements[_currentFrameworkIndex].getRadius();

        return _typeOf('compass_rose').radius;
    };
    this.getCompassRoseSteps = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getSteps)
            _typeOf('compass_rose').steps = _frameworkElements[_currentFrameworkIndex].getSteps();

        return _typeOf('compass_rose').steps;
    };
    this.getHectolineUnit = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getUnit)
            _typeOf('hectoline').unit = _frameworkElements[_currentFrameworkIndex].getUnit();

        return _typeOf('hectoline').unit;
    };
    this.getHectolineInterval = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getInterval)
            _typeOf('hectoline').interval = _frameworkElements[_currentFrameworkIndex].getInterval();

        return _typeOf('hectoline').interval;
    };
    this.getDimLineLengthUnit = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getLengthUnit)
            _typeOf('dimline').lengthUnit = _frameworkElements[_currentFrameworkIndex].getLengthUnit();
        return _typeOf('dimline').lengthUnit;
    };
    this.getDimLineLabelTotalLength = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getLabelTotalLength)
            _typeOf('dimline').labelTotalLength = _frameworkElements[_currentFrameworkIndex].getLabelTotalLength();
        return _typeOf('dimline').labelTotalLength;
    };
    this.getDimPolygonAreaUnit = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getAreaUnit)
            _typeOf('dimpolygon').areaUnit = _frameworkElements[_currentFrameworkIndex].getAreaUnit();

        return _typeOf('dimpolygon').areaUnit;
    };
    this.getDimPolygonLabelEdges = function () {
        if (_frameworkElements[_currentFrameworkIndex] && _frameworkElements[_currentFrameworkIndex].getLabelEdges)
            _typeOf('dimpolygon').labelEdges = _frameworkElements[_currentFrameworkIndex].getLabelEdges();

        return _typeOf('dimpolygon').labelEdges;
    };

    var _getStyleIndex = function (geomType) {
        return _typeOf(geomType || _geometryType).styleIndex;
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
        if (_type().frameworkElementIsVertexMarker) {
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
    // gray preview polylines (one per part) shown while moving/rotating the sketch
    var _addSketchPreviewElements = function (sketch) {
        var elements = [], parts = _model.toLatLngs(_type().hasClosingSegment);
        for (var p = 0; p < parts.length; p++) {
            var polyline = L.polyline([], { color: 'gray', opacity: .8, weight: 4 });
            for (var l = 0; l < parts[p].length; l++) {
                polyline.addLatLng(parts[p][l]);
            }
            elements.push(polyline);
        }
        for (var e in elements) {
            sketch._addToMap(elements[e]);
        }
        return elements;
    };
    var _previewLatLngs = function (elements) {
        return elements.map(function (element) { return element.getLatLngs(); });
    };
    var _setPreviewLatLngs = function (elements, latLngParts) {
        for (var p in elements) {
            elements[p].setLatLngs(latLngParts[p]);
        }
    };
    var _sketchMoveRefLatLng = null;
    this.startSketchMoving = function (index) {
        this.endSketchRotating();

        if (index >= 0 && index < _vertices.length) {
            this.endSketchMoving();

            this.hide();
            var vertex = _vertices[index];

            if (_vertices.length > 1) {
                _sketchMoveFrameworkElements = _addSketchPreviewElements(this);
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
            this.map.frameworkElement.removeLayer(_sketchMoveFrameworkElements[e]);
        }
        _sketchMoveFrameworkElements = [];
        this.show();
    };
    this.moveSketchPreview = function (latlng) {
        if (_sketchMovingVertexIndex >= 0 && _sketchMovingVertexIndex < _vertices.length) {
            
            var deltaLng = (this._snappedVertex != null ? this._snappedVertex.x : latlng.lng) - _sketchMoveRefLatLng.lng;
            var deltaLat = (this._snappedVertex != null ? this._snappedVertex.y : latlng.lat) - _sketchMoveRefLatLng.lat;
            _sketchMoveRefLatLng = this._snappedVertex != null ? L.latLng(this._snappedVertex.y, this._snappedVertex.x) : latlng;

            var latLngParts = _previewLatLngs(_sketchMoveFrameworkElements);
            _model.translateLatLngs(latLngParts, deltaLng, deltaLat, this.hasSelectedVertices());
            _setPreviewLatLngs(_sketchMoveFrameworkElements, latLngParts);
        }
    };
    this.moveSketch = function (latlng) {
        if (_sketchMovingVertexIndex >= 0 && _sketchMovingVertexIndex < _vertices.length) {

            this.addUndo(webgis.l10n.get("sketch-tool-move"));

            var vertex = _vertices[_sketchMovingVertexIndex];

            var deltaX = (this._snappedVertex != null ? this._snappedVertex.x : latlng.lng) - vertex.x;
            var deltaY = (this._snappedVertex != null ? this._snappedVertex.y : latlng.lat) - vertex.y;
            _sketchMoveRefLatLng = null;

            _model.translate(deltaX, deltaY, this.hasSelectedVertices());
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
                _sketchRotateFrameworkElements = _addSketchPreviewElements(this);
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
            this.map.frameworkElement.removeLayer(_sketchRotateFrameworkElements[e]);
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

            var latLngParts = _previewLatLngs(_sketchRotateFrameworkElements);
            _model.rotateLatLngs(latLngParts, vertices[0], angle, crs, this.hasSelectedVertices());
            _setPreviewLatLngs(_sketchRotateFrameworkElements, latLngParts);
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

            _model.rotate(vertices[0], angle, crs, this.hasSelectedVertices());
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
    this.bounds = function () {
        if (!_vertices || _vertices.length === 0) return null;

        // first calculate bounds of vertices
        var minX = _vertices[0].x;
        var minY = _vertices[0].y;
        var maxX = _vertices[0].x;
        var maxY = _vertices[0].y;

        for (var i = 1; i < _vertices.length; i++) {
            var vertex = _vertices[i];
            minX = Math.min(minX, vertex.x);
            minY = Math.min(minY, vertex.y);
            maxX = Math.max(maxX, vertex.x);
            maxY = Math.max(maxY, vertex.y);
        }

        // than calculate bounds of framework elements (parts) if implemented
        // Circles or Series has a larger extent than vertices
        for (var frameworkElement of _frameworkElements) {
            if (frameworkElement?.getBounds) {
                var bounds = frameworkElement.getBounds();
                webgis.log('frameworkElement bounds', bounds);
                if (bounds?.isValid && bounds.isValid()) {
                    minX = Math.min(minX, bounds.getWest());
                    minY = Math.min(minY, bounds.getSouth());
                    maxX = Math.max(maxX, bounds.getEast());
                    maxY = Math.max(maxY, bounds.getNorth());
                }
            }
        }

        return [minX, minY, maxX, maxY];
    };

    this.metaInfo = function (s) {
        var info = {
            geometryType: _geometryType
        };
        _type().metaInfo(info);
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

    // "x y" (WGS84) or "X Y [meta:..] [srs:..] [m:..] [z:..]"
    var _wktVertex = function (vertex, useWGS84, appendMeta, roundFactor) {
        if (useWGS84 === true) {
            return vertex.x + ' ' + vertex.y;
        }

        var X = vertex.X, Y = vertex.Y;
        if (roundFactor > 1.0) {
            X = Math.round(X * roundFactor) / roundFactor;
            Y = Math.round(Y * roundFactor) / roundFactor;
        }
        var wkt = X + ' ' + Y;
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
        return wkt;
    };

    this.toWKT = function (useWGS84) {
        if (_vertices.length == 0) {
            return '';
        }

        let wkt = '';

        var geometryTypeDef = _type();
        if (geometryTypeDef.wktType) {
            wkt += "MULTI" + geometryTypeDef.wktType + "(";
        }

        var vertices = geometryTypeDef.wktVertices(useWGS84);
        var addVertexFunc = function (wkt, vertex) {
            return wkt + _wktVertex(vertex, useWGS84, true, 0);
        };

        wkt += "(";
        for (var i = 0; i < vertices.length; i++) {
            if (i > 0) {
                if (this.isInFanMode() && i > 1) {
                    wkt += '),(';
                    wkt = addVertexFunc(wkt, vertices[0]);
                    wkt += ',';
                }
                else if (i < vertices.length - 1 && _model.isPartStart(i)) {
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

        let isMultipart = _model.count() > 2 || (_model.count() == 2 && _vertices.length > _model.start(1));  // its no an multipart if only the first is "closed";
        webgis.log('isMultipart', isMultipart, _model.getPartIndex());

        let wkt = '', closeVertex = null;
        let wktType = _type().wktType;
        if (wktType === 'POLYGON') {
            wkt += isMultipart ? "MULTIPOLYGON((" : "POLYGON((";
            closeVertex = _vertices.length > 0 ? _vertices[0] : null;
        }
        else if (wktType) {
            wkt += (isMultipart ? "MULTI" + wktType : wktType) + "(";
        }
        let vertices = _vertices;
        let roundFactor = 0.0;

        if (digits && digits > 0) {
            roundFactor = Math.pow(10.0, digits);
        }

        var addVertexFunc = function (wkt, vertex) {
            return wkt + _wktVertex(vertex, useWGS84, appendMeta, roundFactor);
        };

        wkt += isMultipart ? "(" : "";
        for (var i = 0; i < vertices.length; i++) {
            if (i > 0) {
                if (i < vertices.length - 1 && _model.isPartStart(i)) {
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
            if (wktType === 'POLYGON') {
                wkt += ")";
            }
            wkt += ")";
        }
        return wkt;
    };

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
