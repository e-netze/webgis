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

    this._tools = ["pointer", "symbol", "text", "point", "line", "polygon", "rectangle", "circle", "distance_circle", "compass_rose", "dimline", "hectoline"];

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

        this._sketchMetaText = null;

        this.hideContextMenu(true);
        if (this._sketch.isValid()) {
            this.commitCurrentElement(this._sketch && this._sketch.isValid());
        }
        this._sketch.remove(true);
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

        if (this._map.getActiveTool() && this._map.getActiveTool().is_graphics_tool === true) {
            //console.log(this._tool);
            this._map._refreshToolBubbles(this._map.getActiveTool(),
                $.inArray(this._tool, ["symbol", "text", "point", "line", "polygon", "rectangle", "circle", "distance_circle","compass_rose", "dimline", "hectoline"]) >= 0,
                $.inArray(this._tool, ["symbol", "text", "line", "polygon", "dimline", "hectoline"]) >= 0);
        }

        this._refreshInfoContainers();
    };

    this.getTool = function () {
        return this._tool;
    };

    this.isRegularTool = function (tool) { return this._tools.includes(tool); }

    this.getToolDescription = function (tool) {
        return webgis.l10n.get("redlining-" + (tool || this._tool)) || '';
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

    this.refreshUI = function() { $().webgis_graphicsInfoContainer('refreshAllContainers'); };

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
                $().webgis_graphicsInfoContainer('hasContainers', containerOptions)) {
                //$(null).webgis_graphicsInfoStage('refreshAllContainers');
                $().webgis_graphicsInfoContainer('refreshAllContainers', containerOptions);
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
            $(null).webgis_graphicsInfoContainer('hasContainers');
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
        //console.log('commitCurrentElement-valid-sketch', this._sketch.isValid());
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

        if (this._tool !== "text") {
            this._sketchMetaText = null;
        }
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
                label = element.metaText || webgis.l10n.get("redlining-tool-point");
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

                $("<button class='webgis-button'>Marker Entfernen</button>").appendTo($div)
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
                        properties["_meta"] = {
                            tool: "dimline",
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

    this.fromGeoJson = function (gr) {
        try {
            this._serializing = true;
            this._suppressRefreshInfoContainer = true;
            if (gr.replaceelements === true)
                this.clear();
            if (typeof gr.geojson === "string")
                gr.geojson = $.parseJSON(gr.geojson);
            //alert(JSON.stringify(gr.geojson));
            var bounds = null;

            for (var i in gr.geojson.features) {
                var feature = gr.geojson.features[i];
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
                            } else if (feature.properties["_meta"] && feature.properties["_meta"].tool === "hectoline") {
                                this.setTool('hectoline');
                                feature.geometry.type = 'hectoline';
                                this.setHectolineUnit(feature.properties["hl-unit"]);
                                this.setHectolineInterval(feature.properties["hl-interval"]);
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
            console.log('map.graphics.fromJson', ex);
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
                case 'hectoline':
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
                case 'hectoline':
                    result.push({
                        type: 'hectoline',
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
            case 'hectoline': return { name: 'line', styles: ['stroke-color'] };
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
        this._previewSketch1.fromJson(json, false, true);
        this._previewSketch2.fromJson(json, false, readonly);
    };
    this.removePreview = function () {
        //console.log('removePreview');
        this._previewSketch1.remove();
        this._previewSketch2.remove();
    };

    this._defaultBehaviour = function (type) {
        return $.inArray(type, ['line', 'polygon', 'freehand', 'distance_circle', 'compass_rose', 'circle', 'rectangle', 'text', 'dimline', 'hectoline']) >= 0;
    };

    this.hideContextMenu = function () {
        if (this._sketch) {
            this._sketch.ui.hideContextMenu(true);
        }
    }

    // Legacy names for methods
    this.assumeCurrentElement = function (silent) { this.commitCurrentElement(silent); }
};