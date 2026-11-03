webgis.map._graphics = function (map) {
    var $ = webgis.$;
    this._map = map;
    this._elements = [];
    this._currentElement = null;
    this._tool = 'symbol';
    this._currentMarker = null;
    this._currentMarkerSymbol = null;
    this._currentPointMarker = null;
    this._currentPointMarkerSymbol = null;
    this._currentPointMarkerColor = '#ff0000';
    this._currentPointMarkerSize = 10;
    this._sketch = new webgis.sketch(map);
    this._sketch.ui.SetAllowChangePresentation(false);
    this._sketchMetaText = '';
    this._sketchMetaSouce = null;
    this._stagedElement = null;

    this._tools = ["pointer", "symbol", "text", "point", "line", "polygon", "rectangle", "circle", "distance_circle", "compass_rose", "dimline", "dimpolygon", "hectoline", "view_angle", "intersection_angle",     "circle_arc", "perpendicular", "buffer_zone", "object_distance"];

    this._sketch.events.on(['onchangevertex'], function (e, sender, coord) {
        if (this._tool === 'symbol' && this._currentMarker) {
            this._currentMarker.setLatLng({
                lng: coord.x, lat: coord.y
            });
        }
        else if (this._tool === 'point' && this._currentPointMarker) {
            this._currentPointMarker.setLatLng({
                lng: coord.x, lat: coord.y
            });
        }
    }, this);

    this._sketch.events.on(['onsketchclosed'], function (e, sender) {
        //console.log('onsketchclosed')
        if (this._defaultBehaviour(this._tool)) {
            //console.log('assumeCurrentElement');
            this.commitCurrentElement();
        }
    }, this);

    this._sketch.events.on(['onremoved'], function (e, sender) {
        if (this._tool === 'freehand') {
            this._map._enableBBoxToolHandling();
        }
    }, this);

    this._sketch.events.on(['ongetsvalid'], function (e, sender) {
        if (!this.hasSelectedElements()) {
            this._stageElement({
                type: this._tool,
                metaText: this._sketchMetaText
            });
            this._refreshInfoContainers();
        }
    }, this);

    this._sketch.events.on(['ongetsinvalid'], function (e, sender) {
        //console.log('ongetinvalid', e, sender);
        if (this.hasStagedElement()) {
            this._unstageElement();
            this._refreshInfoContainers();
        }
    }, this);

    this._sketch.events.on(['beforeaddvertex', 'onchangevertex'], function (e, sender, coord) {
        // always project. Otherwise construction tools not working

        if (!this.hasUpdatingElement()) {
            this.clearElementSelection();
            this._refreshInfoContainers();
        }

        var crs = this._map.hasDynamicCalcCrs() ? this._map.crs : this._map.calcCrs();
        if (crs) {
            //console.log('redling csr', crs.id);

            if (coord.X && coord.Y && coord.projEvent === 'snapped' && this._map.construct.snappingSrsId() === crs.id) {
                // do nothing: do not reproject snapped vertices 
            } else {
                var pCoord = webgis.fromWGS84(crs, coord.y, coord.x);

                if (pCoord != null) {
                    coord.X = pCoord.x;
                    coord.Y = pCoord.y;
                }

                //console.log(coord);
            }
        }
    }, this);

    this.setCurrentSymbol = function (symbol) {
        this._currentMarkerSymbol = symbol;
        if (this._currentMarker && webgis.mapFramework === "leaflet") {
            var icon = webgis.createMarkerIcon(symbol);

            this._currentMarker.setIcon(icon);
            var element = this._elementByFrameworkElement(this._currentMarker);
            if (element != null) {
                element.symbol = symbol;
            }
        }
    };

    this.getCurrentSymbolId = function () {
        if (this._currentMarkerSymbol)
            return this._currentMarkerSymbol.id;
        return '';
    };

    this.setTool = function (tool) {
        if (tool === this._tool)
            return;

        this.hideContextMenu(true);
        if (this._sketch.isValid()) {
            this.commitCurrentElement(this._sketch && this._sketch.isValid());
        }
        this._sketch.remove(true);

        this._sketchMetaText = null;

        if (tool === 'pointer') {
            this._appendClickEvents();
            this._map._disableSketchClickHandling();
        }
        else {
            this._removeClickEvents();
            //console.log('graphics-tool', tool);
            this._sketch.setGeometryType(tool);
            this._map._enableSketchClickHandling();
        }

        if (tool === 'freehand') {
            this._map._enableBBoxToolHandling();
            this.setLineStyle(null);
        }
        else if (this._tool === 'freehand') {
            this._map._disableBBoxToolHandling();
        }

        //if (tool === 'dimline' || tool === 'hectoline') {
        //    this.setColor('#000');
        //    this.setWeight(1);
        //}

        this._tool = tool;

        if (this._tool !== 'pointer') {
            this._map.refreshSnapping();
        }

        if (this._map.getActiveTool() && this._map.getActiveTool().is_graphics_tool === true) {
            //console.log(this._tool);
            this._map._refreshToolBubbles(this._map.getActiveTool(),
                $.inArray(this._tool, ["symbol", "text", "point", "line", "polygon", "rectangle", "circle", "distance_circle",                                "compass_rose", "dimline", "hectoline", "view_angle", "intersection_angle", "circle_arc", "perpendicular", "buffer_zone", "object_distance"]) >= 0,
                                $.inArray(this._tool, ["symbol", "text", "line", "polygon", "dimline", "dimpolygon", "hectoline", "view_angle", "intersection_angle", "circle_arc", "perpendicular", "buffer_zone"]) >= 0);
        }

        this._refreshInfoContainers();
    };

    this.getTool = function () {
        return this._tool;
    };

    this.isRegularTool = function (tool) { return this._tools.includes(tool); }

    this.getToolDescription = function (tool) {
        return webgis.l10n.get("mapmarkup-" + (tool || this._tool)) || '';
    };

    this.setColor = function (col) {
        this._sketch.setColor(col);
    };

    this.setOpacity = function (opacity) {
        this._sketch.setOpacity(opacity);
    };

    this.setFillColor = function (col) {
        this._sketch.setFillColor(col);
    };

    this.setFillOpacity = function (opacity) {
        this._sketch.setFillOpacity(opacity);
    };

    this.setWeight = function (weight) {
        this._sketch.setWeight(weight);
    };

    this.setLineStyle = function (style) {
        this._sketch.setLineStyle(style);
    };

    this.setMetaText = function (txt) {
        if (this._tool === 'symbol') {
            var element = this._elementByFrameworkElement(this._currentMarker);
            if (element) {
                element.metaText = txt;
            }
        } else if (this._tool === 'point') {
            var element = this._elementByFrameworkElement(this._currentPointMarker);
            if (element) {
                element.metaText = txt;
            }
        } else if (this._tool === 'text') {
            this._sketch.setText(txt);
            this._sketchMetaText = txt;
        } else {
            this._sketchMetaText = txt;
        }

        var stagedOrUpdatingElement = this.getStagedElement() || this.getUpdatingElement();
        if (stagedOrUpdatingElement) {
            stagedOrUpdatingElement.metaText = txt;
        }
    };

    this.setTextColor = function (col) {
        this._sketch.setTextColor(col);
    };

    this.setTextStyle = function (style) {
        this._sketch.setTextStyle(style);
    };

    this.setTextSize = function (size) {
        this._sketch.setTextSize(size);
    };

    this.setPointColor = function (col) {
        this._currentPointMarkerColor = col;
        if (this._currentPointMarker && this._currentPointMarker.setStyle) {
            this._currentPointMarker.setStyle({ color: col, fillColor: col });
        }
    };

    this.setPointSize = function (size) {
        this._currentPointMarkerSize = size;
        if (this._currentPointMarker && this._currentPointMarker.setSize) {
            this._currentPointMarker.setSize(size);
        }
    };

    this._setStyle = function (frameworkElement, style) {
        if (frameworkElement) {
            try {
                if (frameworkElement.setStyle) {
                    frameworkElement.setStyle(style);
                }
            } catch (ex) {
                //console.log('sketch.setStyle', style, ex.message);get
            }
        }
    };

    this._getStyle = function (frameworkElement, style) {
        if (frameworkElement) {
            try {
                if (frameworkElement.getStyle) {
                    frameworkElement.getStyle(style);
                }
            } catch (ex) {
                //console.log('sketch.setStyle', style, ex.message);
            }
        }
    };

    this._toDashArray = function (arrayString, weight) {
        //console.log('_doDashArray', arrayString, weight);
        if (!arrayString == null)
            return null;

        var ret = [], values = arrayString.split(',');
        for (var v in values) {
            var d = parseFloat(values[v]) * weight / 10.0;
            ret.push(d);
        }
        return ret;
    };
    this._fromDashArray = function (array, weight) {
        //console.log('_fromDashArray', array, weight);
        var ret = '';
        if (!array || !array.length) return "1";

        for (var i = 0; i < array.length; i++) {
            if (ret != '')
                if (ret != '')
                    ret += ',';
            ret += array[i] * 10.0 / weight;
        }
        return ret;
    };

    this.applyStyleToSelectedElements = function (style) {
        let styleClassesFunc;

        switch (style) {
            case "point-color": styleClassesFunc = (fe) => [{ 'color': this.getPointColor() }, { 'fillColor': this.getPointColor() }];
                break;
            case "point-size": styleClassesFunc = (fe) => [{ 'size': this.getPointSize() }];
                break;
            case "stroke-color": styleClassesFunc = (fe) => [{ 'color': this.getColor() }];
                break;
            case "stroke-weight": styleClassesFunc = (fe) => {
                    var weight = this.getWeight();
                    var dashArray = this._fromDashArray(fe.options.dashArray, fe.options.weight);
                    fe._webgis = fe._webgis || {}; fe._webgis.lineStyle = dashArray.toString(); 
                    return [{ 'weight': weight }, { 'dashArray': this._toDashArray(dashArray, weight) }];
                } 
                break;
            case "stroke-style": styleClassesFunc = (fe) => {
                    fe._webgis = fe._webgis || {}; fe._webgis.lineStyle = this.getLineStyle();
                    //console.log(fe._webgis);
                    return [{ 'dashArray': this._toDashArray(this.getLineStyle(), fe.options.weight || 3) }];
                }
                break;
            case "fill-color": styleClassesFunc = (fe) => [{ 'fillColor': this.getFillColor() }];
                break;
            case "fill-opacity": styleClassesFunc = (fe) => [{ 'fillOpacity': this.getFillOpacity() }];
                break;
            case "text-color": styleClassesFunc = (fe) => [{ 'fill': this.getTextColor() }];
                break;
            case "text-size": styleClassesFunc = (fe) => [{ 'font-size': this.getTextSize() }];
                break;
            case "symbol": styleClassesFunc = (fe) => [{ 'symbol': this._currentMarkerSymbol }]; // clone _currentMarkerSymbol???
                break;

        }

        if (!styleClassesFunc) return;

        for (let element of this.selectedElements()) {
            const styleTypes = this.getStyleTypes(element.type)
            if (styleTypes && styleTypes.styles.includes(style)) {
                //console.log(element.type);
                for (let styleClass of styleClassesFunc(element.frameworkElement)) {
                    if (element.type === 'symbol' && styleClass.symbol) {
                        const icon = webgis.createMarkerIcon(styleClass.symbol);
                        element.frameworkElement.setIcon(icon);
                        element.symbol = styleClass.symbol; 

                        continue;
                    }

                    //console.log('apply style', styleClass);
                    this._setStyle(element.frameworkElement, styleClass);
                }
            }
        }
    };

    this.refreshUI = function() { webgis.ui.refreshGraphicsInfoContainers(); };

    this.getColor = function (geomType) {
        return this._sketch.getColor(geomType);
    };

    this.setCircleRadius = function (radius) {
        this._sketch.setCircleRadius(radius);
    };

    this.setDistanceCircleRadius = function (radius) {
        this._sketch.setDistanceCircleRadius(radius);
    };
    this.setDistanceCircleSteps = function (steps) {
        this._sketch.setDistanceCircleSteps(steps);
    };

    this.setCompassRoseRadius = function (radius) {
        this._sketch.setCompassRoseRadius(radius);
    };
    this.setCompassRoseSteps = function (steps) {
        this._sketch.setCompassRoseSteps(steps);
    };

    this.setHectolineUnit = function (unit) {
        this._sketch.setHectolineUnit(unit);
    };

    this.setHectolineInterval = function (interval) {
        this._sketch.setHectolineInterval(interval);
    };

    this.setDimLineLengthUnit = function (unit) {
        this._sketch.setDimLineLengthUnit(unit);
    };
    this.setDimLineLabelTotalLength = function (doLabel) {
        this._sketch.setDimLineLabelTotalLength(doLabel);
    };
    this.setDimPolygonAreaUnit = function (unit) {
        this._sketch.setDimPolygonAreaUnit(unit);
    };
    this.setDimPolygonLabelEdges = function (doLabel) {
        this._sketch.setDimPolygonLabelEdges(doLabel);
    };
    this.setDimLineAngleUnit = function (unit) {
        this._sketch.setDimLineAngleUnit(unit);
    };
    this.setDimLineAngleType = function (angleType) {
        this._sketch.setDimLineAngleType(angleType);
    };
    this.setDimPolygonAngleUnit = function (unit) {
        this._sketch.setDimPolygonAngleUnit(unit);
    };
    // angle input in the current unit (gon => gon, otherwise degrees)
    this.setViewAngleAngle = function (angle) {
        angle = parseFloat(angle);
        if (isNaN(angle))
            return;
        if (this.getViewAngleUnit() === 'gon')
            angle = angle * 0.9;
        this._sketch.setViewAngleAngle(angle);
    };
    this.setViewAngleUnit = function (unit) {
        this._sketch.setViewAngleUnit(unit);
        this._map.ui.refreshUIElements();
    };
    this.setViewAngleLabelLength = function (doLabel) {
        this._sketch.setViewAngleLabelLength(doLabel);
    };
    this.setIntersectionAngleUnit = function (unit) {
        this._sketch.setIntersectionAngleUnit(unit);
    };
    this.setIntersectionAngleAngles = function (angles) {
        this._sketch.setIntersectionAngleAngles(angles);
    };
    this.setCircleArcUnit = function (unit) {
        this._sketch.setCircleArcUnit(unit);
    };
    this.setCircleArcLabels = function (labels) {
        this._sketch.setCircleArcLabels(labels);
    };
    this.setPerpendicularStationFormat = function (format) {
        this._sketch.setPerpendicularStationFormat(format);
    };
    this.setPerpendicularLengthUnit = function (unit) {
        this._sketch.setPerpendicularLengthUnit(unit);
    };
    this.setBufferZoneSourceType = function (sourceType) {
        this._sketch.setBufferZoneSourceType(sourceType);
        this._requestBufferZoneBuffer();
    };
    this.setBufferZoneWidth = function (width) {
        width = parseFloat(width);
        if (isFinite(width) && width !== 0) {  // negative => inner buffer (polygons only, see L.BufferZone.isValidWidth)
            this._sketch.setBufferZoneWidth(width);
            this._requestBufferZoneBuffer();
        }
    };
    // server response of 'buffer-zone-calc' => { key, rings }
    this.setBufferZoneBuffer = function (data) {
        if (!data || !data.key)
            return;

        var rings = data.rings && data.rings.length ? data.rings : null;
        if (this._sketch.setBufferZoneBuffer(rings, data.key))
            return;

        // the element is already committed (eg. tool changed before the response arrived)
        for (var i in this._elements) {
            var frameworkElement = this._elements[i].frameworkElement;
            if (this._elements[i].type === 'buffer_zone' && frameworkElement && frameworkElement.getBufferKey && frameworkElement.getBufferKey() === data.key) {
                frameworkElement.setBuffer({ key: data.key, rings: rings });
            }
        }
    };

    // debounced server request: the buffer is calculated by the server (MapMarkup tool command 'buffer-zone-calc')
    this._bufferZoneTimer = null;
    this._requestBufferZoneBuffer = function () {
        if (this._bufferZoneTimer) {
            clearTimeout(this._bufferZoneTimer);
            this._bufferZoneTimer = null;
        }
        if (this._tool !== 'buffer_zone' || this._serializing === true)
            return;

        var me = this;
        this._bufferZoneTimer = setTimeout(function () {
            me._bufferZoneTimer = null;

            var sketch = me._sketch, map = me._map, tool = map ? map.getActiveTool() : null;
            if (me._tool !== 'buffer_zone' || !tool || !sketch.isValid() || sketch.hasValidBufferZoneBuffer())
                return;

            var key = sketch.getBufferZoneBufferKey(), source = sketch.getBufferZoneSource();
            if (!key || !source)
                return;

            var sourceType = sketch.getBufferZoneSourceType() || 'line', latLngs = source.latLngs;
            if (!L.BufferZone.isValidWidth(sourceType, sketch.getBufferZoneWidth()))
                return;
            if (sourceType === 'point')
                latLngs = latLngs.slice(0, 1);

            webgis.tools.sendToolRequest(map, tool, 'servertoolcommand', null, {
                _method: 'buffer-zone-calc',
                'bz-coords': JSON.stringify(latLngs.map(function (latLng) { return [latLng.lng, latLng.lat]; })),
                'bz-source-type': sourceType,
                'bz-parts': L.BufferZone.isPointSource(sourceType) ? '' : L.BufferZone.partStartsToString(source.partStarts),
                'bz-width': sketch.getBufferZoneWidth(),
                'bz-calc-crs': L.ViewAngle.toCalc(latLngs).crsId || 0,
                'bz-key': key
            });
        }, 500);
    };
    this._sketch.events.on(['onchanged'], function (e, sender) {
        if (this._tool === 'buffer_zone') {
            this._requestBufferZoneBuffer();
        }
    }, this);

    /********* object_distance: the objects A/B are selected by map clicks (webgis.sketch.geometry.object_distance.js) *********/
    // map click => server 'object-distance-pick' (candidates at the click position) => one candidate: apply, more: menu
    // apply: no A => A, A => ignored, existing B => removed (toggle), otherwise => server 'object-distance-calc' => new line A-B
    this._objectDistanceCounter = 0;
    this._objectDistancePick = null;  // pending pick request: { key, latLng }
    this._objectDistanceCalc = null;  // pending calc request: { key, candidate }

    this.setObjectDistanceLengthUnit = function (unit) {
        this._sketch.setObjectDistanceLengthUnit(unit);
    };
    this.setObjectDistanceShowNumber = function (show) {
        this._sketch.setObjectDistanceShowNumber(show === true || show === 'true');
    };

    this._sketch.events.on(['onobjectdistancepick'], function (e, sender, vertex) {
        var map = this._map, tool = map ? map.getActiveTool() : null;
        if (this._tool !== 'object_distance' || !tool || !vertex)
            return;

        this._removeObjectDistanceMenu();
        var key = 'od-' + (++this._objectDistanceCounter);
        this._objectDistancePick = { key: key, latLng: L.latLng(vertex.y, vertex.x) };

        webgis.tools.sendToolRequest(map, tool, 'servertoolcommand', null, {
            _method: 'object-distance-pick',
            'od-lng': vertex.x,
            'od-lat': vertex.y,
            'od-map-scale': map.scale(),
            'od-queries': map.ui.allQueriesString(),
            'od-key': key
        });
    }, this);

    // server response of 'object-distance-pick' => { key, candidates: [{ ref, layer, oid, geometry }] }
    this.setObjectDistanceCandidates = function (data) {
        var pick = this._objectDistancePick;
        if (!data || !pick || data.key !== pick.key)
            return;

        this._objectDistancePick = null;
        if (this._tool !== 'object_distance')
            return;

        var candidates = data.candidates || [], current = this._sketch.getObjectDistanceData();
        if (candidates.length === 0) {
            webgis.toastMessage(webgis.l10n.get('info'), webgis.l10n.get('object-distance-no-object'));
            return;
        }

        // A can't be selected again => only A at the click position: ignore the click
        if (current && current.a) {
            candidates = candidates.filter(function (c) { return c.ref !== current.a.ref; });
            if (candidates.length === 0)
                return;
        }

        if (candidates.length === 1) {
            this._applyObjectDistanceCandidate(candidates[0], pick.latLng);
        } else {
            this._showObjectDistanceMenu(candidates, pick.latLng);
        }
    };

    this._applyObjectDistanceCandidate = function (candidate, latLng) {
        var map = this._map, tool = map ? map.getActiveTool() : null, sketch = this._sketch;
        var current = sketch.getObjectDistanceData();
        if (!current || !candidate)
            return;

        if (!current.a) {
            sketch.setObjectDistanceA(candidate, latLng);
            return;
        }
        if (candidate.ref === current.a.ref)
            return;

        for (var i = 0; i < current.lines.length; i++) {
            if (current.lines[i].ref === candidate.ref) {
                sketch.removeObjectDistanceLine(i);
                return;
            }
        }

        if (!tool)
            return;

        var key = 'od-' + (++this._objectDistanceCounter);
        this._objectDistanceCalc = { key: key, candidate: candidate };

        webgis.tools.sendToolRequest(map, tool, 'servertoolcommand', null, {
            _method: 'object-distance-calc',
            'od-a': current.a.ref,
            'od-b': JSON.stringify([candidate.ref]),
            'od-calc-crs': L.ViewAngle.toCalc([L.latLng(current.a.y, current.a.x), latLng]).crsId || 0,
            'od-key': key
        });
    };

    // server response of 'object-distance-calc' => { key, a, found_a, lines: [{ ref, found, from, to, length, touching }] }
    this.setObjectDistanceLines = function (data) {
        var calc = this._objectDistanceCalc;
        if (!data || !calc || data.key !== calc.key)
            return;

        this._objectDistanceCalc = null;
        if (this._tool !== 'object_distance')
            return;

        var current = this._sketch.getObjectDistanceData();
        if (!current || !current.a || current.a.ref !== data.a)
            return;  // A changed (undo, new sketch) in the meantime

        if (data.found_a !== true) {
            webgis.toastMessage(webgis.l10n.get('info'), webgis.l10n.get('object-distance-a-not-found'));
            return;
        }

        var candidate = calc.candidate;
        if (calc.fromSelection === true) {
            this._addObjectDistanceLinesFromSelection(data.lines || [], current);
            return;
        }
        for (var line of (data.lines || [])) {
            if (line.found !== true) {
                webgis.toastMessage(webgis.l10n.get('info'), webgis.l10n.get('object-distance-no-object'));
                continue;
            }
            if (line.touching === true) {
                webgis.toastMessage(webgis.l10n.get('info'), (candidate.layer || '') + ': ' + webgis.l10n.get('object-distance-touching'));
            }
            this._sketch.addObjectDistanceLine($.extend({}, line, {
                layer: candidate.layer,
                oid: candidate.oid,
                geometry: candidate.geometry
            }));
        }
    };

    /********* add from selection (object_distance: objects B, perpendicular: points) *********/

    // object_distance: selection => objects B of the current object A (server 'object-distance-from-selection' => setObjectDistanceLines)
    this.objectDistanceFromSelection = function () {
        var map = this._map, tool = map ? map.getActiveTool() : null;
        if (this._tool !== 'object_distance' || !tool)
            return;

        var current = this._sketch.getObjectDistanceData();
        if (!current || !current.a) {
            webgis.toastMessage(webgis.l10n.get('info'), webgis.l10n.get('object-distance-select-a-first'));
            return;
        }

        this._removeObjectDistanceMenu();
        var key = 'od-' + (++this._objectDistanceCounter);
        this._objectDistanceCalc = { key: key, fromSelection: true };

        var aLatLng = L.latLng(current.a.y, current.a.x);
        webgis.tools.sendToolRequest(map, tool, 'servertoolcommand', null, {
            _method: 'object-distance-from-selection',
            'od-a': current.a.ref,
            'od-b-existing': JSON.stringify(current.lines.map(function (l) { return l.ref; })),
            'od-calc-crs': L.ViewAngle.toCalc([aLatLng, aLatLng]).crsId || 0,
            'od-key': key
        });
    };
    this._addObjectDistanceLinesFromSelection = function (lines, current) {
        var existing = current.lines.map(function (l) { return l.ref; });
        var add = [], notFound = 0, touching = 0;

        for (var line of lines) {
            if (line.found !== true) {
                notFound++;
                continue;
            }
            if (line.ref === current.a.ref || existing.indexOf(line.ref) >= 0)
                continue;
            if (line.touching === true)
                touching++;
            existing.push(line.ref);
            add.push(line);
        }

        var added = this._sketch.addObjectDistanceLines(add);
        if (notFound > 0)
            webgis.toastMessage(webgis.l10n.get('info'), webgis.l10n.get('object-distance-from-selection-not-found').replace('{0}', notFound));
        if (touching > 0)
            webgis.toastMessage(webgis.l10n.get('info'), webgis.l10n.get('object-distance-from-selection-touching').replace('{0}', touching));
        if (added === 0 && notFound === 0)
            webgis.toastMessage(webgis.l10n.get('info'), webgis.l10n.get('from-selection-nothing-added'));

        if (added > 0)
            this._zoomToSketchIfOutside();
    };

    // perpendicular: selected points => points of the current sketch (server 'perpendicular-from-selection' => setPerpendicularPoints)
    this._perpendicularCounter = 0;
    this._perpendicularFromSelection = null;  // pending request: { key }
    this.perpendicularFromSelection = function () {
        var map = this._map, tool = map ? map.getActiveTool() : null;
        if (this._tool !== 'perpendicular' || !tool)
            return;

        if (!this._sketch.hasPerpendicularReferenceLine()) {
            webgis.toastMessage(webgis.l10n.get('info'), webgis.l10n.get('perpendicular-draw-line-first'));
            return;
        }

        var key = 'pp-' + (++this._perpendicularCounter);
        this._perpendicularFromSelection = { key: key };

        webgis.tools.sendToolRequest(map, tool, 'servertoolcommand', null, {
            _method: 'perpendicular-from-selection',
            'pp-key': key
        });
    };
    // server response of 'perpendicular-from-selection' => { key, points: [[lng, lat], ...] }
    this.setPerpendicularPoints = function (data) {
        var request = this._perpendicularFromSelection;
        if (!data || !request || data.key !== request.key)
            return;

        this._perpendicularFromSelection = null;
        if (this._tool !== 'perpendicular')
            return;

        var result = this._sketch.addPerpendicularPoints(data.points || []);
        if (!result) {
            webgis.toastMessage(webgis.l10n.get('info'), webgis.l10n.get('perpendicular-draw-line-first'));
            return;
        }
        if (result.added === 0) {
            webgis.toastMessage(webgis.l10n.get('info'), webgis.l10n.get('from-selection-nothing-added'));
            return;
        }

        this._zoomToSketchIfOutside();
    };

    // zoom to the whole sketch only if it isn't completely inside the current map extent
    this._zoomToSketchIfOutside = function () {
        var map = this._map, extent = this._sketch.bounds();
        if (!map || !extent)
            return;

        var mapBounds = map.frameworkElement.getBounds();
        if (!mapBounds.contains(L.latLngBounds([extent[1], extent[0]], [extent[3], extent[2]]))) {
            this._sketch.zoomTo();
        }
    };

    // more than one candidate at the click position => menu (like select_sketch, webgis.ui.plugins.construct.js), hover => highlight
    this._removeObjectDistanceMenu = function () {
        $('.webgis-object-distance-menu-blocker').remove();
        if (this._objectDistanceHighlight) {
            this._objectDistanceHighlight.remove();
            this._objectDistanceHighlight = null;
        }
    };
    this._showObjectDistanceMenu = function (candidates, latLng) {
        var me = this, map = this._map;
        this._removeObjectDistanceMenu();

        var highlight = function (candidate) {
            if (me._objectDistanceHighlight) {
                me._objectDistanceHighlight.remove();
                me._objectDistanceHighlight = null;
            }
            if (candidate && candidate.geometry) {
                me._objectDistanceHighlight = L.geoJSON(candidate.geometry, {
                    style: { color: L.ObjectDistance.highlightColorB, weight: 4, opacity: 0.9, fillOpacity: 0.2 },
                    pointToLayer: function (feature, ll) {
                        return L.circleMarker(ll, { radius: 8, color: L.ObjectDistance.highlightColorB, weight: 3, fillOpacity: 0.3 });
                    },
                    interactive: false
                }).addTo(map.frameworkElement);
            }
        };

        var $blocker = $("<div class='blocker webgis-object-distance-menu-blocker' style='z-index:9999999;position:absolute;left:0px;right:0px;top:0px;bottom:0px;background:rgba(0,0,0,0.2)' oncontextmenu='return false;'></div>")
            .appendTo('body')
            .on('click', function () {
                me._removeObjectDistanceMenu();
            });

        var $menuHolder = $("<div class='webgis-contextmenu icons' style='position:absolute;background:#fff;display:inline-block;max-height:60vh;overflow-y:auto'>")
            .appendTo($blocker);

        var $menu = $("<ul>").addClass('webgis-toolbox-tool-item-group-details').css({
            padding: 0, margin: 0, minWidth: '200px'
        }).appendTo($menuHolder);

        $("<li>")
            .addClass('webgis-toolbox-tool-item')
            .append($("<strong>").text(webgis.l10n.get('object-distance-select-object')))
            .appendTo($menu)
            .on('click', function (e) { e.stopPropagation(); });

        for (var candidate of candidates) {
            $("<li>")
                .text(candidate.layer || '')
                .addClass('webgis-toolbox-tool-item')
                .data('candidate', candidate)
                .append('<br>')
                .append($("<span style='color:#aaa;font-size:.8em'>").text('OID: ' + candidate.oid))
                .appendTo($menu)
                .on('click', function (e) {
                    e.stopPropagation();
                    var c = $(this).data('candidate');
                    me._removeObjectDistanceMenu();
                    me._applyObjectDistanceCandidate(c, latLng);
                })
                .on('mouseenter', function () {
                    highlight($(this).data('candidate'));
                })
                .on('mouseleave', function () {
                    highlight(null);
                });
        }

        $("<li>").text(webgis.l10n.get('cancel'))
            .addClass('webgis-toolbox-tool-item')
            .appendTo($menu)
            .on('click', function (e) {
                e.stopPropagation();
                me._removeObjectDistanceMenu();
            });

        var pos = [10, 10];
        if (map.elem) {
            var offset = $(map.elem).offset();
            var point = map.frameworkElement.latLngToContainerPoint(latLng);
            pos[0] = offset.left + point.x + 10;
            pos[1] = offset.top + point.y + 10;
        }

        // keep the menu inside the window
        var width = $menuHolder.outerWidth(), height = $menuHolder.outerHeight();
        pos[0] = Math.max(0, Math.min(pos[0], $(window).width() - width - 10));
        pos[1] = Math.max(0, Math.min(pos[1], $(window).height() - height - 10));

        $menuHolder.css({ left: pos[0], top: pos[1] });
    };
    this.getOpacity = function (geomType) {
        return this._sketch.getOpacity(geomType);
    };

    this.getFillColor = function (geomType) {
        return this._sketch.getFillColor(geomType);
    };

    this.getFillOpacity = function (geomType) {
        return this._sketch.getFillOpacity(geomType);
    };

    this.getWeight = function (geomType) {
        return this._sketch.getWeight(geomType);
    };

    this.getLineStyle = function (geomType) {
        return this._sketch.getLineStyle(geomType);
    };

    this.getCircleRadius = function () {
        return this._sketch.getCircleRadius();
    };

    this.getDistanceCircleRadius = function () {
        return this._sketch.getDistanceCircleRadius();
    };

    this.getDistanceCircleSteps = function () {
        return this._sketch.getDistanceCircleSteps();
    };

    this.getCompassRoseRadius = function () {
        return this._sketch.getCompassRoseRadius();
    };

    this.getCompassRoseSteps = function () {
        return this._sketch.getCompassRoseSteps();
    };

    this.getHectolineUnit = function () {
        return this._sketch.getHectolineUnit();
    };

    this.getHectolineInterval = function () {
        return this._sketch.getHectolineInterval();
    };
    this.getDimLineLengthUnit = function () {
        return this._sketch.getDimLineLengthUnit();
    };
    this.getDimLineLabelTotalLength = function () {
        return this._sketch.getDimLineLabelTotalLength();
    };
    this.getDimPolygonAreaUnit = function () {
        return this._sketch.getDimPolygonAreaUnit();
    };
    this.getDimPolygonLabelEdges = function () {
        return this._sketch.getDimPolygonLabelEdges();
    };
    this.getDimLineAngleUnit = function () {
        return this._sketch.getDimLineAngleUnit();
    };
    this.getDimLineAngleType = function () {
        return this._sketch.getDimLineAngleType();
    };
    this.getDimPolygonAngleUnit = function () {
        return this._sketch.getDimPolygonAngleUnit();
    };
    // angle in the current unit (gon => gon, otherwise degrees)
    this.getViewAngleAngle = function () {
        var angle = this._sketch.getViewAngleAngle();
        if (this.getViewAngleUnit() === 'gon')
            angle = angle / 0.9;
        return Math.round(angle * 100) / 100;
    };
    this.getViewAngleUnit = function () {
        return this._sketch.getViewAngleUnit();
    };
    this.getViewAngleLabelLength = function () {
        return this._sketch.getViewAngleLabelLength();
    };
    this.getIntersectionAngleUnit = function () {
        return this._sketch.getIntersectionAngleUnit();
    };
    this.getIntersectionAngleAngles = function () {
        return this._sketch.getIntersectionAngleAngles();
    };
    this.getCircleArcUnit = function () {
        return this._sketch.getCircleArcUnit();
    };
    this.getCircleArcLabels = function () {
        return this._sketch.getCircleArcLabels();
    };
    this.getPerpendicularStationFormat = function () {
        return this._sketch.getPerpendicularStationFormat();
    };
    this.getPerpendicularLengthUnit = function () {
        return this._sketch.getPerpendicularLengthUnit();
    };
    this.getBufferZoneSourceType = function () {
        return this._sketch.getBufferZoneSourceType();
    };
    this.getBufferZoneWidth = function () {
        return this._sketch.getBufferZoneWidth();
    };
    this.getObjectDistanceLengthUnit = function () {
        return this._sketch.getObjectDistanceLengthUnit();
    };
    this.getObjectDistanceShowNumber = function () {
        return this._sketch.getObjectDistanceShowNumber();
    };

    this.getTextColor = function (geomType) { return this._sketch.getTextColor(geomType); };

    this.getTextStyle = function (geomType) { return this._sketch.getTextStyle(geomType); };

    this.getTextSize = function (geomType) { return this._sketch.getTextSize(geomType); };

    this.getPointColor = function () {
        
        if (this._currentPointMarker) {
            this._currentPointMarkerColor = this._currentPointMarker.options.color;
        }

        return this._currentPointMarkerColor;
    };
    this.getPointSize = function () {

        if (this._currentPointMarker) {
            this._currentPointMarkerSize = this._currentPointMarker.options.size;
        }

        return this._currentPointMarkerSize;
    };

    var _lastOnElementClickTime = new Date().getTime();
    this._onElementClick = function (e) {
        // stop propagation
        if (e.originalEvent) {
            e.originalEvent.preventDefault();
        }

        _lastOnElementClickTime = new Date().getTime();

        if (this.map.graphics._isGraphicsToolActive()) {  // Nur mit aktivem Redling Werkzeug sollte das selektieren funktionieren.
            this.map.graphics.setTool(this.type);
            //console.log('tool', this.type);
            // Delayed: Damit mouseevent nicht auf den Sketch übertragen wird, und beim selektieren ein neuer Punkt gezeichnet wird!
            // Stop Propagation funktioniert anscheind nicht :(
            webgis.delayed(function (args) {
                args.graphics.map.graphics._sketch.fromFrameworkElement(args.frameworkElement);
                args.graphics.map.graphics._sketch.projectedUnprojectedVertices(args.graphics.map.hasDynamicCalcCrs() ? args.graphics.map.crs : args.graphics.map.calcCrs());
                args.graphics.map.fireToolEvent('graphics-elementselected');
                args.graphics.map.graphics._refreshInfoContainers();
            }, 10, { graphics: this, frameworkElement: this.frameworkElement });

            //this.map.graphics._sketch.fromFrameworkElement(this.frameworkElement);
            let element = this.map.graphics._elementByFrameworkElement(this.frameworkElement);

            if (webgis.mapFramework === "leaflet" && this.map.graphics._defaultBehaviour(this.type)) {
                this.map.frameworkElement.removeLayer(this.frameworkElement);
            }
            if (this.type === 'symbol') {
                this.map.graphics._currentMarker = this.frameworkElement;
                this.map.graphics._currentMarkerSymbol = this.symbol;
            }
            else if (this.type === 'point') {
                this.map.graphics._currentPointMarker = this.frameworkElement;
                this.map.graphics._currentPointMarkerSymbol = this.symbol;
            }
            else if (this.map.graphics._defaultBehaviour(this.type)) {
                if (element) {
                    this.map.graphics._sketchMetaText = element.metaText || null;
                    this.map.graphics._sketchMetaSouce = element.metaSource || null;
                }
                //this.map.graphics._removeElementByFrameworkElement(this.frameworkElement, true);
            }
            if (element) {
                this.map.graphics.clearElementSelection();
                element.selected = element.updating = true;

                this._sketchMetaText = element.metaText || null;
            } else {
                this._sketchMetaText = null;
            }
        } else {
            var frameworkElement = e.originalElement ? e.originalElement.frameworkElement : e.target;

            for (var i in this.map.graphics._elements) {
                this.map.graphics._elements[i].readonly_selected = false;
            }
            var element = this.map.graphics._elementByFrameworkElement(frameworkElement);
            if (element) {
                element.readonly_selected = true;
            }

            this.map.graphics._refreshInfoContainers();
        }
        return false;
    };

    this._appendClickEvents = function () {
        this._removeClickEvents();
        for (var i in this._elements) {
            var element = this._elements[i];
            if (element.frameworkElement) {
                if (webgis.mapFramework === "leaflet") {
                    element.frameworkElement.on('click', this._onElementClick, element);
                }
            }
        }
    };

    this._removeClickEvents = function () {
        for (var i in this._elements) {
            var element = this._elements[i];
            if (element.frameworkElement) {
                if (webgis.mapFramework === "leaflet") {
                    element.frameworkElement.off('click', this._onElementClick, element);
                }
            }
        }
    };

    this._elementByFrameworkElement = function (frameworkElement) {
        for (var i in this._elements) {
            if (this._elements[i].frameworkElement === frameworkElement)
                return this._elements[i];
        }
        return null;
    };

    this._removeElementByFrameworkElement = function (frameworkElement, silent) {
        var e = this._elementByFrameworkElement(frameworkElement);
        if (e) {
            // Diese Funktion wird auch aufgerufen, wenn ein Objekte durch anklicken bearbeitet wird
            // => es verschwindet aus der Liste und wird dann wie ein neues gehandelt.
            // fireEvent sollte dabei 'false' sein, sonst verschwindet das Element bei allen LiveShare Clients
            // => Element sollte erst Verschwinden, wenn auf den Mistkübel gelickt wird. Darum gibt es das Event dann erst beim removceCurrrentElement
            this._removeElement(e, false, false);
        }
    };

    this._removeElement = function (element, silent, fireEvent) {
        //console.log('remove', silent);
        //console.trace('remove');

        if (element) {
            this._elements.splice($.inArray(element, this._elements), 1);
        }

        if (silent !== true) {
            this._refreshInfoContainers();
        }

        this.removePreview();

        if (fireEvent !== false) {
            this._map.events.fire('graphics_changed', this._map);
        }
    };

    this._suppressRefreshInfoContainer = false;
    this._refreshInfoContainers = function () {
        if (this._suppressRefreshInfoContainer === true) {
            return;
        }
        if ($.fn.webgis_graphicsInfoContainer) {
            var isGraphicsTool = this._isGraphicsToolActive();

            var containerOptions = { type: isGraphicsTool ? 'editable' : 'readonly' };
            if (/*$().webgis_graphicsInfoStage('hasContainers') ||*/
                webgis.ui.hasGraphicsInfoContainers(containerOptions)) {
                //$(null).webgis_graphicsInfoStage('refreshAllContainers');
                webgis.ui.refreshGraphicsInfoContainers(containerOptions);
            } else if (this._elements.length > 0 && isGraphicsTool === false && webgis.usability.useGraphicsMarkerPopups === false && webgis.usability.showMarkerInfoPanel !== false) {
                // create simple readonly containerwebgis_graphicsInfoStage
                var map = this._map;
                if ($.fn.webgis_dockPanel) {
                    this._map.ui.webgisContainer().webgis_dockPanel({
                        title: 'Marker Info',
                        dock: 'bottom',
                        onload: function ($content, $dockpanel) {
                            $content.empty();
                            var $div = $("<div>").css('max-width', '320px').appendTo($content);
                            $div.webgis_graphicsInfoContainer({ map: map, readonly: true });
                        },
                        size: '0px',
                        maxSize: '40%',
                        autoResize: true,
                        //width:'320px'
                        autoResizeBoth: !webgis.useMobileCurrent(),
                        refElement: this._map.ui.mapContainer(),
                        map: this._map
                    });
                }
            }
        }

        //this._map.ui.refreshUIElements();
    };

    this._hasInfoContainers = function () {
        return $.fn.webgis_graphicsInfoContainer &&
            webgis.ui.hasGraphicsInfoContainers();
    };

    this._isGraphicsToolActive = function () {
        return this._map.getActiveTool() != null && this._map.getActiveTool().is_graphics_tool === true;
    };

    this._stageElement = function (element) {
        this._stagedElement = element;
        this._stagedElement.selected = true;

        this._map.ui.refreshUIElements();
    }
    this._unstageElement = function () {
        this._stagedElement = null;

        this._unstagePendingElements();

        this._map.ui.refreshUIElements();
    }
    this._unstagePendingElements = function () {
        for (let element of this._elements) {
            if (element._pending_staged) element._pending_staged = false;
        }
    }
    this.getStagedElement = function () { return this._stagedElement; }
    this.hasStagedElement = function () { return this._stagedElement !== null; }
    

    this.clearElementSelection = function () {
        for (let element of this._elements) {
            element.selected = element.updating = false;
        }
    }
    this.selectedElements = function () {
        return this._stagedElement
            ? [ this._stagedElement ]
            : this._elements.filter(e => e.selected === true);
        //return this._elements.filter(e => e.selected === true);
    }
    this.hasSelectedElements = function () {
        return this.selectedElements().length > 0
    }
    this.getUpdatingElement = function () {
        const updatingElements = this._elements.filter(e => e.updating === true);
        return updatingElements.length === 1  // there can only be one
            ? updatingElements[0]
            : null;
    }
    this.hasUpdatingElement = function () {
        return this._elements.filter(e => e.updating === true).length > 0;
    }
    this.commitCurrentElement = function (silent) {
        //console.log('commitCurrentElement-valid-sketch', this);
        if (!this._sketch || !this._sketch.isValid())
            return;

        if (this._defaultBehaviour(this._tool)) {
            
            var frameworkElements = this._sketch.cloneGraphicElements();
            var selectedElements = this.selectedElements();

            if (this.hasStagedElement() || selectedElements.length == 0) { // add new element(s)
                for (var frameworkElement of frameworkElements) {

                    frameworkElement.addTo(this._map.frameworkElement);
                    var newElement = {
                        type: this._tool,
                        map: this._map,
                        frameworkElement: frameworkElement,
                        metaText: this._sketchMetaText,
                        metaSource: this._sketchMetaSouce
                    };

                    this._elements.push(newElement);
                }
            } else if (selectedElements.length === 1 && frameworkElements.length === 1) {
                frameworkElements[0].addTo(this._map.frameworkElement);
                selectedElements[0].frameworkElement = frameworkElements[0];
                selectedElements[0].metaText = this._sketchMetaText;
            }
            
            if (this._tool === 'freehand') {
                this._map._enableBBoxToolHandling();
            }
        }
        this._sketch.remove(true);
        if (this._currentMarker && webgis.mapFramework === "leaflet") {
            this._currentMarker.closePopup();
        }

        this._currentMarker = null;
        this._currentPointMarker = null;

        // old...
        //if (this._tool !== "text") {
        //    this._sketchMetaText = null;
        //}
        // unset text always instead of only if tool is not text
        this._sketch.setText('');
        this._sketchMetaText = null;

        this._sketchMetaSouce = null;

        this._unstageElement();
        this.clearElementSelection();

        if (silent !== true) {
            this._refreshInfoContainers();
        }

        this._sketch.remove(true);
        this._sketch.cleanUndos();

        this._map.events.fire('graphics_changed', this._map);
    };

    this.removeCurrentElement = function () {
        this._sketch.remove(true);
        if (this._currentMarker) {
            this._currentMarker.closePopup();
            if (this._tool === 'symbol') {  // sollte man das nicht immer machen??  Sonst funktionert beim Symbol am Handy => Sketch entfernen nicht
                const element = this._elements.filter((e) => e.frameworkElement == this._currentPointMarker);
                //console.log(this._elements, element);
                this._removeElement(element, true, false);
                this._map.frameworkElement.removeLayer(this._currentMarker);
            }
        }
        if (this._currentPointMarker) {
            if (this._tool === 'point') {  // sollte man das nicht immer machen??  Sonst funktionert beim Symbol am Handy => Sketch entfernen nicht
                const element = this._elements.filter((e) => e.frameworkElement == this._currentPointMarker);
                //console.log(this._elements, element);
                this._removeElement(element, true, false);
                this._map.frameworkElement.removeLayer(this._currentPointMarker);
                
            }
        }
        this._currentMarker = null;
        this._currentPointMarker = null;

        this._unstageElement();
        this._refreshInfoContainers();
        this.removePreview();

        this._map.events.fire('graphics_changed', this._map);
    };

    this.removeElement = function (element, silent, fireEvent) {
        if (this._defaultBehaviour(element.type)) {
            this._map.frameworkElement.removeLayer(element.frameworkElement);
        } else if (element.type === 'symbol' && this._hasInfoContainers() === true) {  // sonst funktioniert das über das Popup mit "Marker entfernen"
            this._map.removeMarker(element.frameworkElement);
            this._sketch.remove(true);
        } else if (element.type === 'point' && this._hasInfoContainers() === true) {  // sonst funktioniert das über das Popup mit "Marker entfernen"
            this._map.frameworkElement.removeLayer(element.frameworkElement);
            this._sketch.remove(true);
        }

        this._removeElement(element, silent, fireEvent);
    };

    this.removeElements = function (elements) {
        for (let element of elements) {
            this.removeElement(element, true, false);
        }

        this._refreshInfoContainers();
        this._map.events.fire('graphics_changed', this._map);
    }

    this.labelElement = function (element) {
        if (!element) {
            element = { frameworkElement: this._sketch.cloneGraphicElements()[0] };
            //console.log(element);
            element.type = this._tool;
        }
        map.graphics.assumeCurrentElement();

        var elementSketch = new webgis.sketch(this._map); // sollte auch ohne Map gehen...
        elementSketch.setGeometryType(element.type);
        elementSketch.fromFrameworkElement(element.frameworkElement);

        var properties = elementSketch.calcProperties('X', 'Y');

        elementSketch.destroy();

        var label = null, unit = null;
        switch (element.type) {
            case 'polygon':
                var area = properties.area;
                if (area) {
                    if (area >= 1000000) {
                        area = Math.round(area / 1000000 * 100) / 100;
                        unit = 'km²';
                    } else {
                        area = Math.round(area * 100) / 100;
                        unit = 'm²';
                    }
                    label = area + unit;
                }
                break;
            case 'polyline':
            case 'line':
                var length = properties.length;
                if (length) {
                    if (length >= 1000) {
                        length = Math.round(length / 1000 * 100) / 100;
                        unit = 'km';
                    } else {
                        length = Math.round(length * 100) / 100;
                        unit = 'm';
                    }
                    label = length + unit;
                }
                break;
            case 'point':
                //label = 'EPSG ' + properties.srs + ': ' + Math.round(properties.posX * 100) / 100 + ' ' + Math.round(properties.posY * 100) / 100;
                label = element.metaText || webgis.l10n.get("mapmarkup-tool-point");
                break;
        }

        //console.log(label);
        if (label) {
            this.setTool('text');
            this.setMetaText(label);
            this._map.fireToolEvent('graphics-elementselected');
        }
    };

    this.clear = function () {
        this.commitCurrentElement();
        this._currentElement = null;
        this._currentMarker = null;
        this._currentPointMarker = null;

        for (var i in this._elements) {
            var element = this._elements[i];
            this._map.frameworkElement.removeLayer(element.frameworkElement);
        }
        this._elements = [];
    };

    this.onMapClick = function (e) {
        if (this._tool === 'pointer' || (Math.abs(new Date().getTime() - _lastOnElementClickTime)) < 500) {
            //console.log('suppress OnMapClick');
            return;
        }

        var latlng = e.latlng;
        this._sketch.addVertexCoords(latlng.lng, latlng.lat, true);

        this.tryAddGraphicsSymbolToMap(latlng, e.text);
    };

    this.tryAddGraphicsSymbolToMap = function(latlng, metaText) {
        if (this._tool === 'symbol' && this._currentMarkerSymbol) {
            var symbol = $.extend({}, this._currentMarkerSymbol);

            // Loction from sketch vertex if available => mabye differ from click if snapping is applied
            var vertices = this._sketch.getVertices();
            if (vertices && vertices.length === 1) {
                symbol.lat = vertices[0][1];
                symbol.lng = vertices[0][0];
            } else {
                symbol.lat = latlng.lat;
                symbol.lng = latlng.lng;
            }

            this._currentMarker = webgis.createMarker(symbol);
            this._currentMarker._wgmap = this._map;

            //console.log('currentMarker', this._currentMarker);

            //this._currentMarker.bindLabel('', { noHide: true }).addTo(this._map.frameworkElement).hideLabel();
            this._currentMarker.addTo(this._map.frameworkElement);
            var symbolElement = {
                type: 'symbol',
                symbol: symbol,
                frameworkElement: this._currentMarker,
                map: this._map,
                metaText: this._currentMarker.metaText = metaText,
                _pending_staged: true
            };

            if (!this._hasInfoContainers() && webgis.usability.useGraphicsMarkerPopups === true) {
                var $popup = $("<div>").appendTo($(this._map.elem));

                // Editable
                var $div = $("<div style='margin-top:30px'>").addClass('webgis-graphics-tool show').appendTo($popup);
                $div.get(0)._symbol = symbolElement;

                $("<button class='webgis-button'>").text(webgis.l10n.get('button-remove-markers')).appendTo($div)
                    .click(function () {
                        this.parentNode._symbol.map.graphics._removeElementByFrameworkElement(this.parentNode._symbol.frameworkElement);
                        this.parentNode._symbol.map.removeMarker(this.parentNode._symbol.frameworkElement);
                    });
                // Readonly
                var $div = $("<div style='margin-top:30px'>")
                    .addClass('webgis-graphics-tool hide')
                    .text(metaText || '')
                    .appendTo($popup);

                if (webgis.mapFramework === "leaflet") {
                    this._currentMarker.bindPopup($popup.get(0)).openPopup();
                }
            } else {
                if (webgis.mapFramework === "leaflet") {
                    this._currentMarker.on('click', function (e) {
                        var map = this._wgmap;
                        if (map) {
                            var element = map.graphics._elementByFrameworkElement(this);
                            if (element) {
                                map.graphics._onElementClick.apply(element, [e]);
                            }
                        }
                    });
                }
            }
            this._unstagePendingElements();
            this._elements.push(symbolElement);
            this._refreshInfoContainers();

            this._map.events.fire('graphics_changed', this._map);
        }
        if (this._tool === 'point') {
            this._currentPointMarker = L.pointSymbol(latlng, { color: this.getPointColor(), size: this.getPointSize() });
            this._currentPointMarker._wgmap = this._map;
            this._currentPointMarker._webgis = { geometryType: 'point' };

            this._currentPointMarker.addTo(this._map.frameworkElement);

            if (webgis.mapFramework === "leaflet") {
                this._currentPointMarker.on('click', function (e) {
                    var map = this._wgmap;
                    if (map) {
                        var element = map.graphics._elementByFrameworkElement(this);
                        if (element) {
                            map.graphics._onElementClick.apply(element, [e]);
                        }
                    }
                });
            }

            this._unstagePendingElements();
            this._elements.push(this._currentPointMarkerSymbol = {
                type: 'point',
                frameworkElement: this._currentPointMarker,
                map: this._map,
                metaText: this._currentPointMarker.metaText = metaText,
                _pending_staged: true
            });

            this._refreshInfoContainers();
        }
    };

    this._serializing = false;
    this.toGeoJson = function (supressAssumeCurrent) {
        this._serializing = true;

        if (supressAssumeCurrent !== true) {
            // sollte nicht gemacht werden, wenn beispielsweise graphic über den LiveShare ausgelesen werden
            // => Sollst kann wird beim "schellen" Zeichnen von Symbolen der Sketch nicht angezeigt
            this.commitCurrentElement(this._sketch && this._sketch.isValid());
        }

        var geoJson = {
            type: "FeatureCollection",
            features: []
        };

        // serialization defaults
        var _color = '#ff0000', _fillColor = '#ffff00', _opacity = 0.8, _fillOpacity = 0.2, _weight = 4;

        for (var i in this._elements) {
            var element = this._elements[i];
            if (!element.frameworkElement)
                continue;
            var properties = {};
            switch (element.type) {
                case 'symbol':
                    properties.symbol = element.symbol.id;
                    if (element.symbol.text)
                        properties.text = element.symbol.text;
                    properties["_meta"] = {
                        tool: "symbol",
                        text: element.metaText || element.symbol.text,
                        source: element.metaSouce,
                        symbol: { id: element.symbol.id }
                    };
                    if (element.symbol.icon) { properties["_meta"].symbol.icon = element.symbol.icon; }
                    if (element.symbol.iconSize) { properties["_meta"].symbol.iconSize = element.symbol.iconSize; }
                    if (element.symbol.iconAnchor) { properties["_meta"].symbol.iconAnchor = element.symbol.iconAnchor; }
                    if (element.symbol.popupAnchor) { properties["_meta"].symbol.popupAnchor = element.symbol.popupAnchor; }
                    break;
                case 'line':
                    if (element.frameworkElement.options) {
                        properties["stroke"] = element.frameworkElement.options.color || _color;
                        properties["stroke-opacity"] = element.frameworkElement.options.opacity || _opacity;
                        properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                        if (element.frameworkElement._webgis && element.frameworkElement._webgis.lineStyle) {
                            properties["stroke-style"] = element.frameworkElement._webgis.lineStyle;
                        } else {
                            properties["stroke-style"] = element.frameworkElement.options.dashArray ? element.frameworkElement.options.dashArray.toString() : "1";
                        }
                        properties["_meta"] = {
                            tool: "line",
                            text: element.metaText,
                            source: element.metaSource
                        };
                    }
                    ;
                    break;
                case 'circle':
                case 'rectangle':
                case 'polygon':
                    if (element.frameworkElement.options) {
                        properties["stroke"] = element.frameworkElement.options.color || _color;
                        properties["stroke-opacity"] = element.frameworkElement.options.opacity || _opacity;
                        properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                        if (element.frameworkElement._webgis && element.frameworkElement._webgis.lineStyle) {
                            properties["stroke-style"] = element.frameworkElement._webgis.lineStyle;
                        } else {
                            properties["stroke-style"] = element.frameworkElement.options.dashArray ? element.frameworkElement.options.dashArray.toString() : "1";
                        }
                        properties["fill"] = element.frameworkElement.options.fillColor || _fillColor;
                        properties["fill-opacity"] = element.frameworkElement.options.fillOpacity || _fillOpacity;
                        properties["_meta"] = {
                            tool: element.type,
                            text: element.metaText,
                            source: element.metaSource
                        };

                        if (element.type === 'circle') {
                            properties['circle-radius'] = element.frameworkElement.getRadius();
                        }
                    }
                    break;
                case 'freehand':
                    properties["stroke"] = element.frameworkElement.options.color || _color;
                    properties["stroke-opacity"] = element.frameworkElement.options.opacity || _opacity;
                    properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                    properties["stroke-style"] = "1";
                    properties["_meta"] = {
                        tool: "freehand",
                        text: element.metaText,
                        source: element.metaSource
                    };
                    break;
                case 'distance_circle':
                    properties["stroke"] = element.frameworkElement.options.color || _color;
                    properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                    properties["fill"] = element.frameworkElement.options.fillColor || _fillColor;
                    properties["fill-opacity"] = element.frameworkElement.options.fillOpacity || _fillOpacity;
                    properties["dc-radius"] = element.frameworkElement.options.radius || 1000;
                    properties["dc-steps"] = element.frameworkElement.options.steps || 2;
                    properties["_meta"] = {
                        tool: "distance_circle",
                        text: element.metaText,
                        source: element.metaSource
                    };
                    break;
                case 'compass_rose':
                    properties["stroke"] = element.frameworkElement.options.color || _color;
                    properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                    properties["cr-radius"] = element.frameworkElement.options.radius || 1000;
                    properties["cr-steps"] = element.frameworkElement.options.steps || 2;
                    properties["_meta"] = {
                        tool: "compass_rose",
                        text: element.metaText,
                        source: element.metaSource
                    };
                    break;
                case 'text':
                    properties["font-color"] = element.frameworkElement.options.fontColor || this.getTextColor();
                    properties["font-style"] = element.frameworkElement.options.fontStyle || this.getTextStyle();
                    properties["font-size"] = element.frameworkElement.options.fontSize || this.getFontSize();
                    properties["_meta"] = {
                        tool: "text",
                        text: element.metaText,
                        source: element.metaSource
                    };
                    break;
                case 'point':
                    properties["point-color"] = element.frameworkElement.options.color || this.getPointColor();
                    properties["point-size"] = element.frameworkElement.options.size || this.getPointSize();
                    properties["_meta"] = {
                        tool: "point",
                        text: element.metaText,
                        source: element.metaSource
                    };
                    break;
                case 'dimline':
                    if (element.frameworkElement.options) {
                        properties["stroke"] = element.frameworkElement.options.color || _color;
                        properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                        properties["font-color"] = element.frameworkElement.options.fontColor || '#000';
                        properties["font-style"] = element.frameworkElement.options.fontStyle || '';
                        properties["font-size"] = element.frameworkElement.options.fontSize || 14;
                        properties["length-unit"] = element.frameworkElement.options.lengthUnit || 'm'; 
                        properties["label-total-length"] = element.frameworkElement.options.labelTotalLength === true; 
                        properties["dim-angle-unit"] = element.frameworkElement.options.angleUnit || 'none';
                        properties["dim-angle-type"] = element.frameworkElement.options.angleType || 'inner';
                        properties["_meta"] = {
                            tool: "dimline",
                            text: element.metaText,
                            source: element.metaSource
                        };
                    }
                    break;
                case 'dimpolygon':
                    if (element.frameworkElement.options) {
                        properties["fill"] = element.frameworkElement.options.fillColor || _fillColor;
                        properties["fill-opacity"] = element.frameworkElement.options.fillOpacity || _fillOpacity;
                        properties["stroke"] = element.frameworkElement.options.color || _color;
                        properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                        properties["font-color"] = element.frameworkElement.options.fontColor || '#000';
                        properties["font-style"] = element.frameworkElement.options.fontStyle || '';
                        properties["font-size"] = element.frameworkElement.options.fontSize || 14;
                        properties["area-unit"] = element.frameworkElement.options.areaUnit || 'm²';
                        properties["label-edges"] = element.frameworkElement.options.labelEdges !== false;
                        properties["dim-angle-unit"] = element.frameworkElement.options.angleUnit || 'none';
                        properties["_meta"] = {
                            tool: "dimpolygon",
                            text: element.metaText,
                            source: element.metaSource
                        };
                    }
                    break;
                case 'hectoline':
                    if (element.frameworkElement.options) {
                        properties["stroke"] = element.frameworkElement.options.color || _color;
                        properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                        properties["font-color"] = element.frameworkElement.options.fontColor || '#000';
                        properties["font-style"] = element.frameworkElement.options.fontStyle || '';
                        properties["font-size"] = element.frameworkElement.options.fontSize || 14;
                        properties["hl-unit"] = element.frameworkElement.options.unit || 'm';
                        properties["hl-interval"] = element.frameworkElement.options.interval || 100;
                        properties["_meta"] = {
                            tool: "hectoline",
                            text: element.metaText,
                            source: element.metaSource
                        };
                    }
                    break;
                case 'view_angle':
                    if (element.frameworkElement.options) {
                        properties["stroke"] = element.frameworkElement.options.color || _color;
                        properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                        properties["font-color"] = element.frameworkElement.options.fontColor || '#000';
                        properties["font-size"] = element.frameworkElement.options.fontSize || 14;
                        properties["va-angle"] = element.frameworkElement.options.angle || 30;
                        properties["va-unit"] = element.frameworkElement.options.unit || 'deg';
                        properties["va-label-length"] = element.frameworkElement.options.labelLength === true;
                        properties["_meta"] = {
                            tool: "view_angle",
                            text: element.metaText,
                            source: element.metaSource
                        };
                    }
                    break;
                case 'intersection_angle':
                    if (element.frameworkElement.options) {
                        properties["stroke"] = element.frameworkElement.options.color || _color;
                        properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                        properties["font-color"] = element.frameworkElement.options.fontColor || '#000';
                        properties["font-size"] = element.frameworkElement.options.fontSize || 14;
                        properties["ia-unit"] = element.frameworkElement.options.unit || 'both';
                        properties["ia-angles"] = element.frameworkElement.options.angles || 'angle';
                        properties["_meta"] = {
                            tool: "intersection_angle",
                            text: element.metaText,
                            source: element.metaSource
                        };
                    }
                    break;
                case 'circle_arc':
                    if (element.frameworkElement.options) {
                        properties["stroke"] = element.frameworkElement.options.color || _color;
                        properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                        properties["font-color"] = element.frameworkElement.options.fontColor || '#000';
                        properties["font-size"] = element.frameworkElement.options.fontSize || 14;
                        properties["ca-unit"] = element.frameworkElement.options.unit || 'both';
                        properties["ca-labels"] = element.frameworkElement.options.labels || 'all';
                        var caMeasures = element.frameworkElement.getMeasures();
                        if (caMeasures) {  // export attributes only (recalculated when loading)
                            properties["ca-radius"] = Math.round(caMeasures.radius * 100.0) / 100.0;
                            properties["ca-arc-length"] = Math.round(caMeasures.length * 100.0) / 100.0;
                            properties["ca-angle-deg"] = Math.round(caMeasures.angle * 100.0) / 100.0;
                            properties["ca-angle-gon"] = Math.round(caMeasures.angle * 1000.0 / 9.0) / 100.0;
                        }
                        properties["_meta"] = {
                            tool: "circle_arc",
                            text: element.metaText,
                            source: element.metaSource
                        };
                    }
                    break;
                case 'perpendicular':
                    if (element.frameworkElement.options) {
                        properties["stroke"] = element.frameworkElement.options.color || _color;
                        properties["stroke-width"] = element.frameworkElement.options.weight || _weight;
                        properties["font-color"] = element.frameworkElement.options.fontColor || '#000';
                        properties["font-size"] = element.frameworkElement.options.fontSize || 14;
                        properties["pp-line-vertices"] = element.frameworkElement.getLineVertexCount();
                        properties["pp-station-format"] = element.frameworkElement.options.stationFormat || 'm';
                        properties["pp-length-unit"] = element.frameworkElement.options.lengthUnit || 'm';
                        properties["_meta"] = {
                            tool: "perpendicular",
                            text: element.metaText,
                            source: element.metaSource
                        };
                    }
                    break;
                case 'buffer_zone':
                    if (element.frameworkElement.options) {
                        var bzOptions = element.frameworkElement.options;
                        properties["stroke"] = bzOptions.color || _color;
                        properties["stroke-width"] = bzOptions.weight || _weight;
                        properties["fill"] = bzOptions.fillColor || '#aaaaaa';
                        properties["fill-opacity"] = bzOptions.fillOpacity >= 0 ? bzOptions.fillOpacity : 0.2;
                        properties["font-color"] = bzOptions.fontColor || '#000';
                        properties["font-size"] = bzOptions.fontSize || 14;
                        properties["bz-source-type"] = bzOptions.sourceType || 'line';
                        properties["bz-width"] = bzOptions.width || 10;
                        var bzParts = L.BufferZone.isPointSource(bzOptions.sourceType) ? '' : L.BufferZone.partStartsToString(element.frameworkElement.getPartStarts());
                        if (bzParts) {
                            properties["bz-parts"] = bzParts;  // string: see bz-buffer
                        }
                        var bzRings = element.frameworkElement.getValidBufferRings();
                        if (bzRings) {
                            properties["bz-buffer"] = JSON.stringify(bzRings);  // string: server-side FeatureDTO can't convert nested arrays
                        }
                        properties["_meta"] = {
                            tool: "buffer_zone",
                            text: element.metaText,
                            source: element.metaSource
                        };
                    }
                    break;
                case 'object_distance':
                    if (element.frameworkElement.options) {
                        var odOptions = element.frameworkElement.options, odData = element.frameworkElement.getData();
                        properties["stroke"] = odOptions.color || _color;
                        properties["stroke-width"] = odOptions.weight || _weight;
                        properties["font-color"] = odOptions.fontColor || '#000';
                        properties["font-size"] = odOptions.fontSize || 14;
                        properties["od-length-unit"] = odOptions.lengthUnit || 'm';
                        properties["od-show-number"] = odOptions.showNumber === true;
                        // strings: server-side FeatureDTO can't convert nested objects/arrays, no geometries of A/B (not recalculated when loading)
                        if (odData.a) {
                            properties["od-a"] = JSON.stringify({ ref: odData.a.ref, layer: odData.a.layer, oid: odData.a.oid, x: odData.a.x, y: odData.a.y });
                        }
                        properties["od-lines"] = JSON.stringify(odData.lines.map(function (line) {
                            return {
                                ref: line.ref, layer: line.layer, oid: line.oid, length: line.length, touching: line.touching === true,
                                from: [line.from.lng, line.from.lat], to: [line.to.lng, line.to.lat]
                            };
                        }));
                        properties["_meta"] = {
                            tool: "object_distance",
                            text: element.metaText,
                            source: element.metaSource
                        };
                    }
                    break;
            }

            // calcCrs for DimLine, Hectoline, ...
            var calcCrs = element.frameworkElement.getCalcCrs ? element.frameworkElement.getCalcCrs() : 0;
            if (calcCrs > 0)
                properties["_calcCrs"] = calcCrs;

            var geometry = element.frameworkElement.toGeoJSON().geometry;
            if (geometry.coordinates == null || geometry.coordinates.length == 0)
                continue;
            if (geometry.type == 'Polygon' && (geometry.coordinates[0].length == 0 || geometry.coordinates[0][0] == null))
                continue;
            geoJson.features.push({
                type: "Feature",
                geometry: geometry,
                properties: properties
            });
        }

        this._serializing = false;
        return geoJson;
    };

    // "add from selection" (gr.usecurrentstyle): the server only sends the geometry and the _meta (tool, text)
    // => the current style/options of the tool (by _meta.tool) are used for the missing properties
    this._currentStyleProperties = function () {
        var me = this;
        var strokeFill = function (geomType, fill) {
            var p = {
                'stroke': me.getColor(geomType),
                'stroke-opacity': me.getOpacity(geomType),
                'stroke-width': me.getWeight(geomType),
                'stroke-style': me.getLineStyle(geomType)
            };
            if (fill) {
                p['fill'] = me.getFillColor(geomType);
                p['fill-opacity'] = me.getFillOpacity(geomType);
            }
            return p;
        };

        return {
            point: { 'point-color': this.getPointColor(), 'point-size': this.getPointSize() },
            line: strokeFill('polyline'),
            polygon: strokeFill('polygon', true),
            dimline: $.extend(strokeFill('dimline'), {
                'font-size': this.getTextSize('dimline'),
                'length-unit': this.getDimLineLengthUnit(),
                'label-total-length': this.getDimLineLabelTotalLength(),
                'dim-angle-unit': this.getDimLineAngleUnit(),
                'dim-angle-type': this.getDimLineAngleType()
            }),
            dimpolygon: $.extend(strokeFill('dimpolygon', true), {
                'font-size': this.getTextSize('dimpolygon'),
                'area-unit': this.getDimPolygonAreaUnit(),
                'label-edges': this.getDimPolygonLabelEdges(),
                'dim-angle-unit': this.getDimPolygonAngleUnit()
            }),
            hectoline: $.extend(strokeFill('hectoline'), {
                'font-size': this.getTextSize('hectoline'),
                'hl-unit': this.getHectolineUnit(),
                'hl-interval': this.getHectolineInterval()
            }),
            buffer_zone: $.extend(strokeFill('buffer_zone', true), {
                'font-size': this.getTextSize('buffer_zone')
            })
        };
    };

    this.fromGeoJson = function (gr) {
        try {
            this._serializing = true;
            this._suppressRefreshInfoContainer = true;
            if (gr.replaceelements === true)
                this.clear();
            if (typeof gr.geojson === "string")
                gr.geojson = $.parseJSON(gr.geojson);
            //alert(JSON.stringify(gr.geojson));
            var bounds = null, bufferZoneRings = null;
            var currentStyle = gr.usecurrentstyle === true ? this._currentStyleProperties() : null;

            for (var i in gr.geojson.features) {
                var feature = gr.geojson.features[i];
                if (currentStyle) {
                    feature.properties = feature.properties || {};
                    var styleProperties = feature.properties["_meta"] ? currentStyle[feature.properties["_meta"].tool] : null;
                    for (var styleProperty in styleProperties || {}) {
                        if (feature.properties[styleProperty] === undefined) {
                            feature.properties[styleProperty] = styleProperties[styleProperty];
                        }
                    }
                }
                switch (feature.geometry.type.toLowerCase()) {
                    case 'point':
                        var meta = feature.properties["_meta"], txt;
                        if (meta && (meta.tool === 'distance_circle' || meta.tool === 'circle' || meta.tool === 'compass_rose')) {
                            this.setTool(meta.tool);
                            this.setColor(feature.properties["stroke"]);
                            this.setWeight(feature.properties["stroke-width"]);
                            this.setFillColor(feature.properties["fill"]);
                            this.setFillOpacity(feature.properties["fill-opacity"]);
                            if (meta.tool === 'distance_circle') {
                                this.setDistanceCircleRadius(feature.properties["dc-radius"]);
                                this.setDistanceCircleSteps(feature.properties["dc-steps"]);
                            } else if (meta.tool === 'compass_rose') {
                                this.setCompassRoseRadius(feature.properties["cr-radius"]);
                                this.setCompassRoseSteps(feature.properties["cr-steps"]);
                            } else if (meta.tool === 'circle') {
                                this.setCircleRadius(feature.properties["circle-radius"]);
                                this.setLineStyle(feature.properties["stroke-style"]);
                            }
                        } else if (meta && (meta.tool === 'text')) {
                            this.setTool('text');
                            this.setTextColor(feature.properties["font-color"]);
                            this.setTextStyle(feature.properties["font-style"]);
                            this.setTextSize(feature.properties["font-size"]);
                            this.setMetaText(meta.text);
                        } else if (meta && (meta.tool === 'point')) { 
                            this.setTool('point');  
                            this.setPointColor(feature.properties["point-color"]);
                            this.setPointSize(feature.properties["point-size"]);
                            txt = meta.text;
                        } else {
                            this.setTool('symbol');
                            // set currentMarker to null
                            //     otherwise setCurrentSymbol will set also to the latest/current marker
                            this._currentMarker = null; 
                            var symbolFound = false;
                            for (var s in gr.symbols) {
                                if (feature.properties.symbol === gr.symbols[s].id) {
                                    this.setCurrentSymbol(gr.symbols[s]);
                                    symbolFound = true;
                                    break;
                                }
                            }

                            if (symbolFound === false && meta && meta.symbol && meta.symbol.icon && meta.symbol.iconSize) {
                                this.setCurrentSymbol(meta.symbol);
                            }
                            if (feature) {
                                txt = meta && meta.text ? meta.text : (feature.properties.text || feature.properties.__metaText);
                            }
                        }

                        this.onMapClick({
                            latlng: {
                                lng: feature.geometry.coordinates[0], lat: feature.geometry.coordinates[1]
                            },
                            text: txt || null
                        });
                        break;
                    case 'linestring':
                    case 'multilinestring':
                        if (feature.properties["_meta"] && feature.properties["_meta"].tool === "freehand") {
                            this.setTool('freehand');
                            feature.geometry.type = 'freehand';
                            if (feature.properties) {
                                this.setColor(feature.properties["stroke"]);
                                this.setOpacity(feature.properties["stroke-opacity"]);
                                this.setWeight(feature.properties["stroke-width"]);
                                this.setLineStyle("1");
                            }
                        } else {
                            if (feature.properties["_meta"] && feature.properties["_meta"].tool === "dimline") {
                                this.setTool('dimline');
                                feature.geometry.type = 'dimline';
                                this.setTextSize(feature.properties["font-size"]);
                                this.setDimLineLengthUnit(feature.properties["length-unit"]);
                                this.setDimLineLabelTotalLength(feature.properties["label-total-length"] === true); 
                                this.setDimLineAngleUnit(feature.properties["dim-angle-unit"] || 'none');
                                this.setDimLineAngleType(feature.properties["dim-angle-type"] || 'inner');
                            } else if (feature.properties["_meta"] && feature.properties["_meta"].tool === "hectoline") {
                                this.setTool('hectoline');
                                feature.geometry.type = 'hectoline';
                                this.setHectolineUnit(feature.properties["hl-unit"]);
                                this.setHectolineInterval(feature.properties["hl-interval"]);
                                this.setTextSize(feature.properties["font-size"]);
                            } else if (feature.properties["_meta"] && feature.properties["_meta"].tool === "view_angle") {
                                this.setTool('view_angle');
                                feature.geometry.type = 'view_angle';
                                this.setViewAngleUnit(feature.properties["va-unit"] || 'deg');
                                this._sketch.setViewAngleAngle(parseFloat(feature.properties["va-angle"]) || 30);  // always degrees
                                this.setViewAngleLabelLength(feature.properties["va-label-length"] === true);
                                this.setTextSize(feature.properties["font-size"]);
                            } else if (feature.properties["_meta"] && feature.properties["_meta"].tool === "intersection_angle") {
                                this.setTool('intersection_angle');
                                feature.geometry.type = 'intersection_angle';
                                this.setIntersectionAngleUnit(feature.properties["ia-unit"] || 'both');
                                this.setIntersectionAngleAngles(feature.properties["ia-angles"] || 'angle');
                                this.setTextSize(feature.properties["font-size"]);
                            } else if (feature.properties["_meta"] && feature.properties["_meta"].tool === "circle_arc") {
                                this.setTool('circle_arc');
                                feature.geometry.type = 'circle_arc';
                                this.setCircleArcUnit(feature.properties["ca-unit"] || 'both');
                                this.setCircleArcLabels(feature.properties["ca-labels"] || 'all');
                                this.setTextSize(feature.properties["font-size"]);
                            } else if (feature.properties["_meta"] && feature.properties["_meta"].tool === "perpendicular") {
                                this.setTool('perpendicular');
                                feature.geometry.type = 'perpendicular';
                                feature.geometry.lineVertexCount = feature.properties["pp-line-vertices"];
                                this.setPerpendicularStationFormat(feature.properties["pp-station-format"] || 'm');
                                this.setPerpendicularLengthUnit(feature.properties["pp-length-unit"] || 'm');
                                this.setTextSize(feature.properties["font-size"]);
                            } else if (feature.properties["_meta"] && feature.properties["_meta"].tool === "buffer_zone") {
                                this.setTool('buffer_zone');
                                feature.geometry.type = 'buffer_zone';
                                feature.geometry.partindex = L.BufferZone.partStartsFromString(feature.properties["bz-parts"]);
                                this._sketch.setBufferZoneSourceType(feature.properties["bz-source-type"] || 'line');
                                this._sketch.setBufferZoneWidth(parseFloat(feature.properties["bz-width"]) || 10);
                                this.setFillColor(feature.properties["fill"] || '#aaaaaa');
                                this.setFillOpacity(feature.properties["fill-opacity"] >= 0 ? feature.properties["fill-opacity"] : 0.2);
                                this.setTextSize(feature.properties["font-size"]);
                                bufferZoneRings = null;
                                try {
                                    bufferZoneRings = feature.properties["bz-buffer"] ? JSON.parse(feature.properties["bz-buffer"]) : null;
                                } catch (ex) { }
                            } else if (feature.properties["_meta"] && feature.properties["_meta"].tool === "object_distance") {
                                this.setTool('object_distance');
                                feature.geometry.type = 'object_distance';
                                try {
                                    feature.geometry.od = {
                                        a: feature.properties["od-a"] ? JSON.parse(feature.properties["od-a"]) : null,
                                        lines: feature.properties["od-lines"] ? JSON.parse(feature.properties["od-lines"]) : []
                                    };
                                } catch (ex) {
                                    feature.geometry.od = null;
                                }
                                this.setObjectDistanceLengthUnit(feature.properties["od-length-unit"] || 'm');
                                this.setObjectDistanceShowNumber(feature.properties["od-show-number"] === true || feature.properties["od-show-number"] === 'true');
                                this.setTextSize(feature.properties["font-size"]);
                            } else {
                                this.setTool('line');
                            }
                            if (feature.properties) {
                                this.setColor(feature.properties["stroke"]);
                                this.setOpacity(feature.properties["stroke-opacity"]);
                                this.setWeight(feature.properties["stroke-width"]);
                                this.setLineStyle(feature.properties["stroke-style"]);
                            }

                        }
                        this._sketch.fromJson(feature.geometry);
                        if (bufferZoneRings) {
                            this._sketch.setBufferZoneBuffer(bufferZoneRings);  // after the vertices => current key
                            bufferZoneRings = null;
                        }
                        break;
                    case 'polygon':
                        var meta = feature.properties["_meta"];
                        if (meta && meta.tool === 'rectangle') {
                            this.setTool('rectangle');
                            if (feature.geometry.coordinates.length === 1) {
                                var ring = feature.geometry.coordinates[0], minx, miny, maxx, maxy;
                                for (var i = 0; i < ring.length; i++) {
                                    if (i === 0) {
                                        minx = maxx = ring[i][0];
                                        miny = maxy = ring[i][1];
                                    } else {
                                        minx = Math.min(ring[i][0], minx);
                                        miny = Math.min(ring[i][1], miny);
                                        maxx = Math.max(ring[i][0], maxx);
                                        maxy = Math.max(ring[i][1], maxy);
                                    }
                                }

                                this._sketch.addVertices([{ x: minx, y: maxy }, { x: maxx, y: miny }]);
                            }
                        } else if (feature.properties["_meta"] && feature.properties["_meta"].tool === "dimpolygon") {
                            this.setTool('dimpolygon');
                            feature.geometry.type = 'dimpolygon';
                            this.setTextSize(feature.properties["font-size"]);
                            this.setDimPolygonAreaUnit(feature.properties["area-unit"]);
                            this.setDimPolygonLabelEdges(feature.properties["label-edges"] !== false); 
                            this.setDimPolygonAngleUnit(feature.properties["dim-angle-unit"] || 'none');

                            this._sketch.fromJson(feature.geometry);
                        } else {
                            this.setTool('polygon');

                            this._sketch.fromJson(feature.geometry);
                        }

                        if (feature.properties) {
                            this.setColor(feature.properties["stroke"]);
                            this.setOpacity(feature.properties["stroke-opacity"]);
                            this.setWeight(feature.properties["stroke-width"]);
                            this.setLineStyle(feature.properties["stroke-style"]);
                            this.setFillColor(feature.properties["fill"]);
                            this.setFillOpacity(feature.properties["fill-opacity"]);
                        }
                        break;
                }
                ;
                var sketchBounds = this._sketch.getBounds();
                //alert(sketchBounds);
                if (sketchBounds != null) {
                    if (bounds == null) {
                        bounds = {
                            minLat: sketchBounds.minLat, maxLat: sketchBounds.maxLat,
                            minLng: sketchBounds.minLng, maxLng: sketchBounds.maxLng
                        };
                    }
                    else {
                        bounds.minLat = Math.min(bounds.minLat, sketchBounds.minLat);
                        bounds.maxLat = Math.max(bounds.maxLat, sketchBounds.maxLat);
                        bounds.minLng = Math.min(bounds.minLng, sketchBounds.minLng);
                        bounds.maxLng = Math.max(bounds.maxLng, sketchBounds.maxLng);
                    }
                }

                this._sketchMetaText =
                    (feature.properties["_meta"] && feature.properties["_meta"].text ? feature.properties["_meta"].text : null) ||
                    (feature.properties.__metaText || null);

                this._sketchMetaSouce = feature.properties["_meta"] && feature.properties["_meta"].source ? feature.properties["_meta"].source : null;
                this.commitCurrentElement(true);
            }

        } catch (ex) {
            webgis.log('map.graphics.fromJson', ex);
        }

        this._serializing = false;
        this._suppressRefreshInfoContainer = false;

        //alert(bounds);
        //alert(JSON.stringify(gr.geojson));
        if (bounds && gr.suppressZoom !== true) {
            this._map.zoomTo([bounds.minLng, bounds.minLat, bounds.maxLng, bounds.maxLat]);
        }

        this.setTool('pointer');
        this._refreshInfoContainers();
    };

    this._resetGeoJsonType = function (json) {
        var activeTool = this._map.getActiveTool();

        if (activeTool && activeTool.tooltype === 'graphics') {
            switch (this.getTool()) {
                case 'hectoline':
                case 'view_angle':
                case 'intersection_angle':
                case 'circle_arc':
                case 'perpendicular':
                case 'buffer_zone':
                    if (json.type && json.type !== 'buffer_zone' && !json.sourceGeometryType) {
                        json.sourceGeometryType = json.type;  // => sketch type buffer_zone sets the source type from the feature geometry
                    }
                    json.type = this.getTool();
                    break;
                case 'dimpolygon': 
                case 'dimline':
                    json.type = this.getTool();
                    break;
            }
        }
    };

    this.hasMarkerInfos = function () {
        if (!this._elements || this._elements.length === 0) {
            return false;
        }

        for (var i in this._elements) {
            var element = this._elements[i];
            switch (element.type) {
                case 'symbol':
                case 'point':
                case 'line':
                case 'rectangle':
                case 'polygon':
                case 'circle':
                case 'freehand':
                case 'distance_circle':
                case 'compass_rose':
                case 'dimline':
                case 'dimpolygon': 
                case 'hectoline':
                case 'view_angle':
                case 'intersection_angle':
                case 'circle_arc':
                case 'perpendicular':
                case 'buffer_zone':
                case 'object_distance':
                    return true;
            }
        }

        return false;
    };
    this.getElements = function () {
        var result = [];

        var stagedElement = this.getStagedElement();

        if (stagedElement) {
            result.push({
                type: stagedElement.type,
                originalElement: stagedElement,
                metaText: stagedElement.metaText,
                readonly_selected: false
            });
            result[result.length - 1].updating = true;
        }

        var updatingElement = !stagedElement
            ? this.getUpdatingElement()
            : null;

        for (let element of this._elements) {
            if (element._pending_staged === true) {  // points & symbols are automatically added to _elements, even if they are still staged...
                continue;
            }
            
            switch (element.type) {
                case 'symbol':
                    result.push({
                        type: 'symbol',
                        symbolid: element.symbol.id,
                        text: element.symbol.text,
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected
                    });
                    break;
                case 'line':
                    result.push({
                        type: 'line',
                        stroke: element.frameworkElement.options.color,
                        strokeOpacity: element.frameworkElement.options.opacity,
                        strokeWidth: element.frameworkElement.options.weight,
                        strokeStyle: element.frameworkElement.options.dashArray ? element.frameworkElement.options.dashArray.toString() : "1",
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected
                    });
                    break;
                case 'rectangle':
                case 'polygon':
                    result.push({
                        type: element.type,
                        stroke: element.frameworkElement.options.color,
                        strokeOpacity: element.frameworkElement.options.opacity,
                        strokeWidth: element.frameworkElement.options.weight,
                        strokeStyle: element.frameworkElement.options.dashArray ? element.frameworkElement.options.dashArray.toString() : "1",
                        fill: element.frameworkElement.options.fillColor,
                        fillOpacity: element.frameworkElement.options.fillOpacity,
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected
                    });
                    break;
                case 'circle':
                    result.push({
                        type: 'circle',
                        stroke: element.frameworkElement.options.color,
                        strokeOpacity: element.frameworkElement.options.opacity,
                        strokeWidth: element.frameworkElement.options.weight,
                        strokeStyle: element.frameworkElement.options.dashArray ? element.frameworkElement.options.dashArray.toString() : "1",
                        fill: element.frameworkElement.options.fillColor,
                        fillOpacity: element.frameworkElement.options.fillOpacity,
                        originalElement: element,
                        metaText: element.metaText,
                        circle_radius: element.frameworkElement.options.radius,
                        readonly_selected: element.readonly_selected
                    });
                    break;
                case 'freehand':
                    result.push({
                        type: 'freehand',
                        stroke: element.frameworkElement.options.color,
                        strokeOpacity: element.frameworkElement.options.opacity,
                        strokeWidth: element.frameworkElement.options.weight,
                        strokeStyle: "1",
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected,
                    });
                    break;
                case 'text':
                    result.push({
                        type: 'text',
                        text: element.frameworkElement.options.text,
                        fontColor: element.frameworkElement.options.fontColor,
                        fontSize: element.frameworkElement.options.fontSize,
                        fontStyle: element.frameworkElement.options.fontStyle,
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected
                    });
                    break;
                case 'point':
                    result.push({
                        type: 'point',
                        color: element.frameworkElement.options.color,
                        size: element.frameworkElement.options.size,
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected
                    });
                    break;
                case 'distance_circle':
                    result.push({
                        type: 'distance_circle',
                        stroke: element.frameworkElement.options.color,
                        strokeWidth: element.frameworkElement.options.weight,
                        fill: element.frameworkElement.options.fillColor,
                        fillOpacity: element.frameworkElement.options.fillOpacity,
                        dc_radius: element.frameworkElement.options.radius,
                        dc_steps: element.frameworkElement.options.steps,
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected
                    });
                    break;
                case 'compass_rose':
                    result.push({
                        type: 'compass_rose',
                        stroke: element.frameworkElement.options.color,
                        strokeWidth: element.frameworkElement.options.weight,               
                        cr_radius: element.frameworkElement.options.radius,
                        cr_steps: element.frameworkElement.options.steps,
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected
                    });
                    break;
                case 'dimline':
                    result.push({
                        type: 'dimline',
                        stroke: element.frameworkElement.options.color,
                        strokeWidth: element.frameworkElement.options.weight,
                        fontColor: element.frameworkElement.options.fontColor,
                        fontSize: element.frameworkElement.options.fontSize,
                        fontStyle: element.frameworkElement.options.fontStyle,
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected
                    });
                    break;
                case 'dimpolygon':
                    result.push({
                        type: 'dimpolygon',
                        fill: element.frameworkElement.options.fillColor,
                        fillOpacity: element.frameworkElement.options.fillOpacity,
                        stroke: element.frameworkElement.options.color,
                        strokeWidth: element.frameworkElement.options.weight,
                        fontColor: element.frameworkElement.options.fontColor,
                        fontSize: element.frameworkElement.options.fontSize,
                        fontStyle: element.frameworkElement.options.fontStyle,
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected
                    });
                    break;
                case 'hectoline':
                case 'view_angle':
                case 'intersection_angle':
                case 'circle_arc':
                case 'perpendicular':
                case 'object_distance':
                    result.push({
                        type: element.type,
                        stroke: element.frameworkElement.options.color,
                        strokeWidth: element.frameworkElement.options.weight,
                        fontColor: element.frameworkElement.options.fontColor,
                        fontSize: element.frameworkElement.options.fontSize,
                        fontStyle: element.frameworkElement.options.fontStyle,
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected
                    });
                    break;
                case 'buffer_zone':
                    result.push({
                        type: element.type,
                        fill: element.frameworkElement.options.fillColor,
                        fillOpacity: element.frameworkElement.options.fillOpacity,
                        stroke: element.frameworkElement.options.color,
                        strokeWidth: element.frameworkElement.options.weight,
                        fontColor: element.frameworkElement.options.fontColor,
                        fontSize: element.frameworkElement.options.fontSize,
                        originalElement: element,
                        metaText: element.metaText,
                        readonly_selected: element.readonly_selected
                    });
                    break;
            }

            if (element === updatingElement) {
                result[result.length - 1].updating = true;
            }
        }

        return result;
    };
    this.countElements = function () {
        return this._elements.length;
    };

    this.getStyleTypes = function (type) {
        switch (type) {
            case 'symbol': return { name: 'symbol', styles: ['symbol'] };
            case 'point': return { name: 'point', styles: ['point-color', 'point-size'] };
            case 'text': return { name: 'text', styles: ['text-color', 'text-size'] };
            case 'freehand':
            case 'line': return { name: 'line', styles: ['stroke-color', 'stroke-weight', 'stroke-style'] };
            case 'polygon':
            case 'rectangle':
            case 'circle': return { name: 'polygon', styles: ['fill-color', 'fill-opacity', 'stroke-color', 'stroke-weight', 'stroke-style'] };
            case 'distance_circle': return { name: 'line', styles: ['stroke-color', 'fill-collor'] };
            case 'compass_rose': return { name: 'line', styles: ['stroke-color'] };
            case 'dimline':
            case 'dimpolygon':
            case 'hectoline':
            case 'view_angle':
            case 'intersection_angle':
            case 'circle_arc':
            case 'perpendicular':
            case 'object_distance': return { name: 'line', styles: ['stroke-color'] };
            case 'buffer_zone': return { name: 'line', styles: ['stroke-color', 'fill-color', 'fill-opacity'] };
            default: return [];
        }
    };

    this.zoomTo = function (element) {
        if (webgis.mapFramework === "leaflet") {
            if (element.originalElement && element.originalElement.frameworkElement) {
                if (element.originalElement.frameworkElement.getBounds) {
                    this._map.frameworkElement.fitBounds(element.originalElement.frameworkElement.getBounds());
                } else if (element.originalElement.frameworkElement._latlng) {
                    this._map.setScale(1000, element.originalElement.frameworkElement._latlng);
                }
            }

        }
    };

    this._previewSketch1 = new webgis.sketch(map);
    this._previewSketch1.setColor('#ff0');
    this._previewSketch1.setFillColor('#ff0');
    this._previewSketch1.setWeight(20);

    this._previewSketch2 = new webgis.sketch(map);
    this._previewSketch2.setColor('#00f');
    this._previewSketch2.setFillColor('#00f');
    this._previewSketch2.setWeight(5);

    this.addPreviewFromJson = function (json, zoomto, readonly) {
        //console.log('addPreviewFromJson', json, zoomto, readonly);
        this._previewSketch1.fromJson(json, false, true);
        this._previewSketch2.fromJson(json, false, readonly);
    };
    this.removePreview = function () {
        //console.log('removePreview');
        this._previewSketch1.remove();
        this._previewSketch2.remove();
    };

    this._defaultBehaviour = function (type) {
        return $.inArray(type, ['line', 'polygon', 'freehand', 'distance_circle', 'compass_rose', 'circle', 'rectangle', 'text', 'dimline', 'dimpolygon', 'hectoline', 'view_angle', 'intersection_angle',         'circle_arc', 'perpendicular', 'buffer_zone', 'object_distance']) >= 0;
    };

    this.hideContextMenu = function () {
        if (this._sketch) {
            this._sketch.ui.hideContextMenu(true);
        }
    }

    // Legacy names for methods
    this.assumeCurrentElement = function (silent) { this.commitCurrentElement(silent); }
};