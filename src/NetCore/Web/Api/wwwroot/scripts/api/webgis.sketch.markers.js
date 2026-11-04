// Vertex marker events and the vertex marker popup menu of webgis.sketch (webgis.sketch.js)
// ctx => { sketch, state, typeOf(name), type() } (same context as the geometry types)
//
// The event handlers are called with "this" = marker (marker.sketch = sketch).
// Creating/redrawing the markers stays in the sketch.

webgis.sketch.markers = function (ctx) {
    "use strict";

    var $ = webgis.$, state = ctx.state;

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
            if (ctx.typeOf(marker.sketch.getGeometryType()).isPlainPath) {
                marker.dragging._draggable.options.clickTolerance = clickTolerance;
            }
        } catch (ex) { }
    };

    /********* Events *********/
    var _onMarkerDragStart = function () {
        this.sketch._isMarkerDragging = true;
        this._dragstartLatLng = this.getLatLng();
        this._forceRedrawMarkers = this.sketch_vertex_coords.fixed === true;
        this.sketch_vertex_coords.fixed = null;
        this.sketch.hideMoverLine();
        $(this._icon).addClass('dragging');
    };
    var _onMarkerDragEnd = function () {
        var geomTypeDef = ctx.typeOf(this.sketch.getGeometryType());
        var latlng;
        this.sketch._lastChangeTime = new Date().getTime();
        var newLatLng = this.getLatLng();
        if (!this.sketch.checkMaxBBox({ x: newLatLng.lng, y: newLatLng.lat })) {
            this.setLatLng(this._dragstartLatLng);
            // redrawOnVertexMoved: types that move their element live while dragging (text) must be reset
            this.sketch.redraw(geomTypeDef.isPlainPath || geomTypeDef.redrawOnVertexMoved);
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

        ctx.typeOf(this.sketch.getGeometryType()).onVertexMarkerDrag(this, latlng, latLngs);
    };
    var _onMarkerClick = function (e) {
        // stop propagation
        e.originalEvent.preventDefault();
        var sketch = this.sketch;

        if (webgis.sketch.construct.onMarkerClick(sketch, this) === true) {
            return;
        }

        if (new Date().getTime() - this._creationDate < 1000) {
            if (sketch.canCloseSection() === true) {
                // double click: only finish the current section (eg. reference line of 'perpendicular'), not the whole sketch
                sketch.appendPart();
            } else {
                sketch.close();
            }
            webgis.delayed(function (marker) {
                if (marker && marker._map)
                    marker._map.closePopup();
            }, 10, this);
            return;
        }

        if (sketch.isClosed()) {
            return;
        }

        var clickedGeomTypeDef = ctx.typeOf(sketch.getGeometryType());
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
        state.suppressMover = true;
    };
    var _onMarkerMouseOut = function (e) {
        if (!this.sketch._isMarkerDragging) {
            _setMarkerClickTolerance(this, webgis.usability.mapSketchClickTolerance);
            state.suppressMover = false;
        }
    };

    /********* Popup menu (touch devices, ...) *********/
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
        if (ctx.type().isLine === true || ctx.type().isPolygon === true) {
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

    /********* Public (used by sketch._appendMarkerEvents, sketch.setMarkerTooltip) *********/
    this.appendEvents = function (marker, menu) {
        marker.on('dragstart', _onMarkerDragStart, marker);
        marker.on('dragend', _onMarkerDragEnd, marker);
        marker.on('drag', _onMarkerDrag, marker);
        marker.on('click', _onMarkerClick, marker);
        marker.on('mouseover', _onMarkerMouseOver, marker);
        marker.on('mouseout', _onMarkerMouseOut, marker);
        if (webgis.usability.sketchMarkerPopup === true && webgis.usability.contextMenuBubble === false) {  // Nur anzeigen, wenn keine rechte Maus möglich ist (TouchDevice oder custom.js...)
            webgis.bindMarkerPopup(marker, menu, _createMarkerPopupMenu);
        }
        marker._creationDate = new Date().getTime();
    };
    this.setTooltip = function (vertex, text) {
        try {
            var elements = ctx.type().frameworkElementIsVertexMarker ? state.frameworkElements : state.vertexFrameworkElements;
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

    /********* Bubble move (touch devices) *********/
    // simulates dragging a vertex marker: the marker follows the map mouse position (click bubble)
    // start => dragstart, mouse move => drag, end (next map click) => dragend
    // the null checks are kept as before (!== null, != null, truthy), the element may be undefined (sparse freehand markers)
    var _bubbleElement = null;

    this.isBubbleMoving = function () { return _bubbleElement ? true : false; };
    this.startBubbleMove = function (index) {
        var sketch = ctx.sketch;
        if (ctx.type().frameworkElementIsVertexMarker) {
            if (state.frameworkElements.length > 0) {
                _bubbleElement = state.frameworkElements[0];
            }
        } else {
            if (index < 0 || index >= state.vertexFrameworkElements.length)
                return;

            sketch.hideMoverLine();
            sketch.hideMoverMarker();
            sketch.hideTraceLine();

            _bubbleElement = state.vertexFrameworkElements[index];
        }

        _bubbleElement.fire('dragstart');
        sketch.map.ui.setClickBubbleLatLng(_bubbleElement.getLatLng());
    };
    this.endBubbleMove = function () {
        if (_bubbleElement !== null) {
            _bubbleElement.fire('dragend');
            _bubbleElement = null;
        }
    };
    // map mouse move while a marker is dragged
    this.bubbleMoveTo = function (latlng) {
        if (_bubbleElement !== null && latlng != null) {
            _bubbleElement.setLatLng(latlng);
            _bubbleElement.fire('drag');
        }
    };
    // map moved/zoomed => cancel without dragend
    this.cancelBubbleMove = function (map) {
        if (_bubbleElement !== null) {
            _bubbleElement = null;
            ctx.sketch._isMarkerDragging = false;
            map.ui.setClickBubbleLatLng(null); // Park
        }
    };
    // => sketch.resetMarkerDragging: back to the drag start position
    this.resetDragging = function () {
        var sketch = ctx.sketch;
        if (sketch._isMarkerDragging) {
            if (_bubbleElement != null) {
                _bubbleElement.setLatLng(_bubbleElement._dragstartLatLng);
                _bubbleElement = null;
            }
            sketch._isMarkerDragging = false;
        }
    };
};
