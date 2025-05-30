webgis.map = function (felem, crs, initialExtent, name) {
    "use strict";

    var $ = webgis.$;
    
    webgis.implementEventController(this);
    this._parseToolbars = function () {
        if (!this._webgisContainer)
            return;

        var mapElementTop = this.elem ?
            $(this.elem).offset().top :
            0;

        //console.log('mapElementTop: ', mapElementTop);

        var toolButtonBars = $(this._webgisContainer).find('.webgis-tool-button-bar');
        if (toolButtonBars.length > 0) {
            // bestehende Löschen, sonst werden es beim Laden von Karten (Projekten) immer mehr
            toolButtonBars.empty();

            toolButtonBars.addClass('webgis-ui-trans');
            var map = this;
            var $leafletControl = ($(this.elem).find('.leaflet-control-zoom'));
            var height = $leafletControl.find('.leaflet-control-zoom-in').height();
            var width = $leafletControl.width();
            var outerWidth = $leafletControl.outerWidth(true);
            webgis.toolInfos({}, null, function (tools) {
                toolButtonBars.each(function (i, b) {
                    var $b = $(b);
                    var toolIds = $(b).attr('data-tools').split(',');
                    if ($b.hasClass('horizontal')) {
                        $b.css({ height: height || 40, width: toolIds.length * 40 });
                    }
                    else {
                        var offsetZoomIn = $leafletControl.find('.leaflet-control-zoom-in').offset();
                        var offsetWebgisContainer = $leafletControl.find('.leaflet-control-zoom-in').closest('.webgis-container').offset() || $leafletControl.find('.leaflet-control-zoom-in').closest('#webgis-container').offset();
                        $b.css({
                            width: outerWidth || 43, height: toolIds.length * 43 + 3,
                            left: offsetZoomIn.left - offsetWebgisContainer.left - 3, // 3 = button margin-left!!
                        });
                        $b.css({ top: ($leafletControl.position().top - mapElementTop) + $leafletControl.outerHeight(true) + 10 });
                        var elemPos = $(map.elem).position();

                        //if (elemPos.top && parseInt(elemPos.top) > 0) {
                        //    $b.css('top', parseInt($b.css('top')) + parseInt(elemPos.top));
                        //}
                        if (parseInt(offsetZoomIn.top) != 44) {
                            $b.css('top', parseInt($b.css('top')) + (parseInt(offsetZoomIn.top) - 44));
                        }
                    }
                    for (var t in toolIds) {
                        var toolId = webgis.tools.getToolId(toolIds[t]);
                        var $button = $("<div style='text-align:center'>").css({ width: Math.min(width || 32, 32) }).appendTo($b);
                        for (var i in tools) {
                            var tool = tools[i];
                            if (tool.id === toolId) {
                                var imgSrc = /*(tool.image.indexOf('/') >= 0) ? webgis.baseUrl + '/' + tool.image :*/ webgis.css.imgResource(tool.image, 'tools');
                                $("<img src='" + imgSrc + "' />").appendTo($button);
                                $button.attr('alt', tool.tooltip).attr('title', tool.tooltip)
                                    .click(function () {
                                        webgis.tools.onButtonClick(this.map, this.tool, this);
                                    });
                                $button.get(0).map = map;
                                $button.get(0).tool = tool;
                                $button.addClass('webgis-tool-button tool-' + webgis.validClassName(tool.id));
                            }
                        }
                    }
                });
            });
        }

        //
        // Allways inclulded buttons
        //

        // Remove All Visfilters:
        var $filterButton = $("<div style='text-align:center'>")
            .css({ width: Math.min(width || 32, 32), backgroundColor: '#ffdddd' })
            .addClass('webgis-tool-button webgis-dependencies webgis-dependency-hasfilters')
            .data('map', this)
            .attr('alt', 'Alle Darstellungsfilter entfernen')
            .appendTo(toolButtonBars)
            .click(function () {
                var map = $(this).data('map');
                if (map) {
                    map.unsetAllFilters();
                    map.refresh();
                    map.ui.refreshUIElements();
                }
            });
        $("<img src='" + webgis.css.imgResource('filter-remove-26.png', 'tools') + "' />").appendTo($filterButton);

        // Remove all queryresults
        var $queryResultButton = $("<div style='text-align:center'>")
            .css({ width: Math.min(width || 32, 32), backgroundColor: '#ffdddd' })
            .addClass('webgis-tool-button webgis-dependencies webgis-dependency-queryresultsexists')
            .data('map', this)
            .attr('alt', 'Alle Ergebnisse aus der Karte entfernen')
            .appendTo(toolButtonBars)
            .click(function () {
                var map = $(this).data('map');
                if (map) {
                    map.queryResultFeatures.removeMarkersAndSelection();
                }
            });
        $("<img src='" + webgis.css.imgResource('marker-remove-26.png', 'tools') + "' />").appendTo($queryResultButton);

        // reset (service) focus
        var $resetFocusButton = $("<div style='text-align:center'>")
            .css({ width: Math.min(width || 32, 32), backgroundColor: '#ffdddd' })
            .addClass('webgis-tool-button webgis-dependencies webgis-dependency-focused-service-exists')
            .data('map', this)
            .attr('alt', 'Focus auf Dienst entfernen')
            .appendTo(toolButtonBars)
            .click(function () {
                var map = $(this).data('map');
                map.resetFocusServices();
            });
        $("<img src='" + webgis.css.imgResource('focus_remove-26.png', 'tools') + "' />").appendTo($resetFocusButton);
    };
    this._toolBoxLayer = null;
    this.name = name || 'webgis5_map';
    this._resizeTimer = new webgis.timer(function (map) {
        map.viewLense.refresh(true);
        map.refreshDisableBlocker();
        map.events.fire('resize', map, {});
    }, 1000, this);
    this._currentCoordsContainer = null;
    this._currentCursorPosition = { lat: null, lng: null };
    this._mouseDownContainerPointer = null;
    this._mouseDown = false, this._mouseDownOffset = null, this._mouseMovePrevOffset = null;
    this.guid = webgis.guid();

    var _calcCrsId = 0;
    var _zoomBoundsAtZoomstart = null;
    
    this.init = function (felem, crs, initialExtent, name) {
        this.name = name || this.name;
        this.elem = null;
        this._webgisContainer = null;
        this.frameworkElement = felem;
        this.crs = crs;
        this.initialExtent = initialExtent;
        this.initialBounds = initialExtent && initialExtent.extent ? initialExtent.extent : null;
        this.activeTool = null;
        this.id = '';
        if (webgis.sketch) {
            this.sketch = new webgis.sketch(this);

            this.hoverSketch = new webgis.sketch(this);
            for (var geomType of ["polyline", "polygon"]) {
                this.hoverSketch.setColor('#ff8', geomType);
                this.hoverSketch.setOpacity(0.8, geomType);
                this.hoverSketch.setFillColor('#ff8', geomType);
                this.hoverSketch.setFillOpacity(0.5, geomType);
                this.hoverSketch.setWeight(4, geomType);
            }
        }
        this._zoomStack = new webgis.fixedStack(10);

        this._baseMaps = {
            current: null,
            currentOverlays: [],
            services: []
        };
        if (webgis.mapFramework === 'leaflet') {
            this.elem = this.frameworkElement._container;
            $(this.elem).on('contextmenu', function () { return false; });
            //this.elem._map = this;
            this.id = this.elem.id;
            this._webgisContainer = $(this.elem).closest('.webgis-container');
            this._webgisContainer = (this._webgisContainer.length === 0 ? this.elem.parentNode : this._webgisContainer.get(0));

            if ($(this.elem).closest('.webgis-container').find('.webgis-currentcoords-container').length > 0) {
                this._currentCoordsContainer = $(this.elem).closest('.webgis-container').find('.webgis-currentcoords-container');
                $(this._currentCoordsContainer).find('.epsg')
                    .html(this.crs.id)
                    .click(function (e) {
                        e.stopPropagation();
                        webgis.showCrsInfo(parseInt($(this).html()));
                    });
                $(this._currentCoordsContainer).click(function (e) {
                    e.stopPropagation();
                    $(this).closest('.webgis-container').find(".webgis-toolbox-tool-item[data-toolid='webgis.tools.coordinates']").trigger('click');
                });
            }

            felem.on("zoomstart", function () {
                this.incRequestId();
                this._hourglassCounter.reset();
                this._hourglassCounterGuid.reset();
                this.ui.hideQuickInfo();
                try {
                    _zoomBoundsAtZoomstart = _zoomBoundsAtZoomstart || this.frameworkElement.getBounds();
                }
                catch (e) {
                    _zoomBoundsAtZoomstart = null;
                }
                this.setLastZoomEventTime();
                this.events.fire('zoomstart', this);
            }, this);
            felem.on("zoomend", function () {
                this.events.fire('zoomend', this); // refresh (siehe unten)
            }, this);
            felem.on("movestart", function () {
                this.incRequestId();
                this._hourglassCounter.reset();
                this._hourglassCounterGuid.reset();
                this.ui.hideQuickInfo();
                try {
                    _zoomBoundsAtZoomstart = _zoomBoundsAtZoomstart || this.frameworkElement.getBounds();
                }
                catch (e) {
                    _zoomBoundsAtZoomstart = null;
                }
                this.setLastZoomEventTime();
                this.events.fire('movestart', this);
            }, this);
            felem.on("moveend", function () {
                this.events.fire('moveend', this); // refresh (siehe unten)
            }, this);
            felem.on("viewreset", function () {
                this.events.fire('viewreset', this); // refresh (siehe unten)
            }, this);
            felem.on("move", function () {
                this.incRequestId();
            }, this);
            felem.on("resize", function () {
                this.events.fire('resize-live', this, {});
                this._resizeTimer.Start();
            }, this);
            felem.on("click", function (e) {
                //console.log('click', e);
                if (!e || !e.originalEvent) {
                    console.log('cancel click: unknown source...');
                    //
                    //  Es kann vorkommen, dass man beim Klicken auch einen "PAN" oder änliches macht.
                    //  Dabei wird die Karte neue aufgebaut. Durch einen (Leaflet?) Bug können dann die Koordinaten nicht mehr stimmen,
                    //  bzw. beziehen sich auf das Dokument (Body) und nicht auf die Karte (man erkennt das, weil es anscheinend auch kein originalEvent gibt)
                    //  --------
                    //  Bei Anwendungen, bei denen die Karte auf der Seite nicht auf (0,0) ist, zB OLE, bewirkt dieser Effekt,
                    //  das der Klick verschoben ist (zB ein gezeichnet Vertext ist genau um die Koordinaten Left/Top des Kartenelements) verschoben
                    //  Damit dieser Fehler nicht auftritt, werden solche Klicks einfach ignoriert.
                    //  ---------
                    //  Theoretisch könnte man noch prüfen, ob das Kartenelement auch in der linken oberen Ecke der Webseite ist und erst 
                    //  dann abbrechen
                    //
                    return;
                }

                if ($(e.originalEvent.target).closest('.leaflet-control-container').length > 0) {
                    // Click auf zB Übersichtskarte nicht weiterleiten!
                    console.log('cancel click: ignore control container');
                    return;
                }

                if (this.ui.useClickBubble()) {
                    this.ui.animClickBubble();
                    if ($.fn.webgis_tabs && webgis.useMobileCurrent())
                        $(this._webgisContainer).find('.webgis-tabs-holder').webgis_tabs('hideAll');
                }
                else {
                    this.eventHandlers.click.apply(this, [e]);
                }

                if (e.latlng) {
                    this.events.fire('click', this, {
                        lng: e.latlng.lng,
                        lat: e.latlng.lat,
                        originalEvent: e.originalEvent
                    });
                }
            }, this);
            felem.on("dblclick", function (e) {
                webgis.delayedToggleStop('defaulttool-click');
            }, this);
            felem.on('dragstart', function (e) {
                //console.log('dragstart', e);
            }, this);
            felem.on('dragend', function (e) {
                //console.log('dragend', e);
                if (e.distance <= 15) {
                    var startPoint = e.target.dragging._draggable._startPoint;
                    //console.log(startPoint);
                    startPoint = startPoint.add(e.target.dragging._draggable._newPos).subtract(e.target.dragging._draggable._startPos);
                    //console.log(startPoint);
                    var elementPos = $(this.elem).position();
                    //console.log(elementPos);
                    startPoint = startPoint.subtract(L.point(elementPos.left, elementPos.top));
                    //console.log(startPoint);
                    var latLng = this.frameworkElement.containerPointToLatLng(startPoint);
                    //console.log(latLng);
                    this.frameworkElement.fire('click', { latlng: latLng, containerPoint: startPoint, force: true });
                }
            }, this);
            felem.on("mousedown", function (e) {
                //console.log('mousedown', e);
                if (webgis.tools) {
                    webgis.tools.onMapMousedown(this, e);
                }
                var ctrlKeyBox = this._activeTool != null &&
                    (this._activeTool.allow_ctrlbbox || webgis.tools.allowSketchVertexSelection(this, this._activeTool)) &&
                    !webgis.useMobileCurrent() && e.originalEvent.ctrlKey;

                if (ctrlKeyBox) {
                    this._enableBBoxToolHandling();
                }

                if (this._activeTool != null && e.originalEvent.button === 0 && (this._activeTool.tooltype === 'box' || ctrlKeyBox)) {
                    var bounds = L.latLngBounds(e.latlng, e.latlng);
                    this._toolBoxLayer = L.rectangle(bounds, { color: '#ff7800', weight: 2 });
                    this._toolBoxLayer.touchDownLatLng = e.latlng;
                    this._toolBoxLayer.ctrlKeyBox = ctrlKeyBox && this._activeTool.tooltype != 'box';  // if tool is a bbox tool, do not use the ctrl behavoir
                    this.frameworkElement.addLayer(this._toolBoxLayer);
                }
                //if (this._activeTool !== null && (this._activeTool.tooltype === 'sketch1d' || this._activeTool.tooltype === 'sketch2d')) {
                //    this._mouseDownContainerPoint = e.containerPoint;
                //}
                this._mouseDown = true;
                if (e.originalEvent) {
                    this._mouseDownOffset = { x: e.originalEvent.clientX, y: e.originalEvent.clientY };
                    if (this.viewLense.currentTool()) {
                        this.viewLense._performMapLenseToolInit(this._mouseDownOffset);
                    }
                }

                this.events.fire('mousedown', this, e);
            }, this);
            felem.on("mouseup", function (e) {
                //console.log('mouseup', e);
                this._mouseDown = false;
                this._mouseDownOffset = this._mouseMovePrevOffset = null;

                if (e.originalEvent.button === 2) { // rightClick 
                    if (Math.abs(new Date().getTime() - this._lastRightClickTime) > 500) {
                        this._lastRightClickTime = new Date().getTime();

                        if ($.fn.webgis_map_contextmenu) {
                            $(null).webgis_map_contextmenu('hide');
                        }

                        this.events.fire('rightclick', this, e);

                        if (webgis.usability.allowMapContextMenu && $.fn.webgis_map_contextmenu) {
                            $('body').webgis_map_contextmenu({
                                map: this,
                                clickX: e.originalEvent.clientX,
                                clickY: e.originalEvent.clientY
                            });
                        }
                    }
                }

                if (e.originalEvent) {
                    if (this.viewLense.currentTool()) {
                        this.viewLense._performMapLenseToolRelease();
                    }
                }

                if (webgis.tools) {
                    webgis.tools.onMapMouseup(this, e);
                }

                var ctrlKeyBox = this._toolBoxLayer != null &&
                    this._activeTool != null &&
                    (this._activeTool.allow_ctrlbbox || webgis.tools.allowSketchVertexSelection(this, this._activeTool)) &&
                    e.originalEvent.ctrlKey;

                if (ctrlKeyBox || this._toolBoxLayer != null) {
                    // Firefox sends "click" event directly after mouse up, event after bbox...
                    // => workaround check last bbox in webgis.map.handlers.click function
                    // otherwise after an Crtl BBOX a click event is send immediatlely
                    this._setLastBBoxEventTime();  
                    this._disableBBoxToolHandling();
                }

                if (e.originalEvent.button === 0 && this._activeTool != null && (this._activeTool.tooltype === 'box' || ctrlKeyBox) && this._toolBoxLayer != null) {
                    var bounds = [this._toolBoxLayer.touchDownLatLng.lng, this._toolBoxLayer.touchDownLatLng.lat, e.latlng.lng, e.latlng.lat];
                    //this.frameworkElement.removeLayer(this._toolBoxLayer);
                    //this._toolBoxLayer = null;

                    if (this._activeTool.onbox) {
                        this._activeTool.onbox(this, bounds);
                    }
                    else if (webgis.tools.allowSketchVertexSelection(this, this._activeTool)) {
                        var minX = Math.min(bounds[0], bounds[2]),
                            minY = Math.min(bounds[1], bounds[3]),
                            maxX = Math.max(bounds[0], bounds[2]),
                            maxY = Math.max(bounds[1], bounds[3]);

                        var w = maxX - minX, h = maxY - minY;

                        var sketch = this._activeTool.tooltype === 'graphics' ? this.graphics._sketch : this.sketch;

                        if (w > 0 || h > 0) {
                            sketch.selectVertices(sketch.getVertexIndicesFromBounds({ minX: minX, minY: minY, maxX: maxX, maxY: maxY }), true);
                        } else {
                            sketch.selectVertices(sketch.getVertexIndicesFromBounds({ minX: minX, minY: minY, maxX: maxX, maxY: maxY }));  // toogle
                        }
                    }
                    else if (this._activeTool.type === 'servertool') {
                        var customEvent = [];
                        customEvent['_method'] = 'box';
                        webgis.tools.sendToolRequest(this, this._activeTool, 'servertoolcommand', new webgis.tools.boxEvent(this, e, bounds), customEvent);
                    }
                    else if (this._activeTool.type === 'customtool') {
                        webgis.tools.executeCustomTool(this, this._activeTool, new webgis.tools.boxEvent(this, e, bounds, this.crs.id));
                        this.setActiveTool(null);
                    }
                }
                //if (this._activeTool !== null && (this._activeTool.tooltype === 'sketch1d' || this._activeTool.tooltype === 'sketch2d')) {
                //    this._enableSketchClickHandling();
                //    this._mouseDownContainerPoint = null;
                //}
                if (this._toolBoxLayer != null) {
                    this.frameworkElement.removeLayer(this._toolBoxLayer);
                    this._toolBoxLayer = null;
                }
            }, this);
            felem.on("mousemove", function (e) {
                if (!this.ui.useClickBubble()) {
                    this.eventHandlers.move.apply(this, [e]);
                }

                //if (this._activeTool !== null && (this._activeTool.tooltype === 'sketch1d' || this._activeTool.tooltype === 'sketch2d')) {
                //    if (this._mouseDownContainerPoint != null) {
                //        console.log(e);
                //        var dx = e.containerPoint.x - this._mouseDownContainerPoint.x,
                //            dy = e.containerPoint.y - this._mouseDownContainerPoint.y;

                //        if (dx * dx + dy * dy > 1000) {
                //            console.log('disable');
                //            this._disableSketchClickHandling();
                //        }
                //    }
                //}

                if (this._mouseDown && this.viewLense.currentTool() && e.originalEvent) {
                    var event = {
                        x: e.originalEvent.clientX, y: e.originalEvent.clientY,
                        shiftKey: e.originalEvent.shiftKey, altKey: e.originalEvent.altKey, ctrlKey: e.originalEvent.ctrlKey
                    };
                    this.viewLense._performMapLenseTool(event, this._mouseMovePrevOffset || this._mouseDownOffset, this._mouseDownOffset);
                    this._mouseMovePrevOffset = event;
                }

            }, this);
            felem.on("mouseout", function (e) {
               //console.log('mouseout', e);
            }, this);
            felem.on("mouseover", function (e) {
                //console.log('mouseover', e);
                if (e.originalEvent.buttons === 0) {  
                    
                    // if no button is pressed, remove BBOX if exists
                    // otherwise the bbox will stay in map forever 
                    var disableBBoxHandling =
                        this._toolBoxLayer != null && this._toolBoxLayer.ctrlKeyBox &&
                        this._activeTool != null &&
                        (this._activeTool.allow_ctrlbbox || webgis.tools.allowSketchVertexSelection(this, this._activeTool));

                    if (disableBBoxHandling) {   // disable the handling, if bbox is startet with bbox
                        this._setLastBBoxEventTime();
                        this._disableBBoxToolHandling();
                    }

                    if (this._toolBoxLayer != null) {
                        this.frameworkElement.removeLayer(this._toolBoxLayer);
                        this._toolBoxLayer = null;
                    }
                }
                
            }, this);
            var map = this;
            $('html').on("keydown", function (e) {
                map.eventHandlers.keydown.apply(map, [e]);
            });
            $('html').on("keyup", function (e) {
                map.eventHandlers.keyup.apply(map, [e]);
            });

            if (this.sketch) {
                this.sketch.events.on(['beforeaddvertex', 'onchangevertex'], function (e, sender, coord) {
                    if (coord.projEvent === 'snapped' && coord.X && coord.Y) {
                        // Do not change coordinates, but set SRS if different
                        if (this.hasDynamicCalcCrs() === true || (sender.originalSrs() > 0 && this.construct.snappingSrsId() > 0 && sender.originalSrs() !== this.construct.snappingSrsId())) {
                            coord.srs = this.construct.snappingSrsId();
                        };
                    }
                    else {
                        var project = !(coord.X && coord.Y && e.channel === 'beforeaddvertex');

                        if (project) {
                            var pCoord = null, calcCrs = this.calcCrs([{ x: coord.x, y: coord.y }]);
                            //console.log('calcCrs', calcCrs, sender.originalSrs(), sender.originalSrsP4Params());
                            if (calcCrs &&
                                (sender.originalSrs() === 0 || sender.originalSrs() === calcCrs.id)) {
                                pCoord = webgis.fromWGS84(calcCrs, coord.y, coord.x);

                                if (this.hasDynamicCalcCrs() === true) {
                                    coord.srs = calcCrs.id;
                                }
                            }
                            else if (sender.originalSrs() > 0 && sender.originalSrsP4Params()) {
                                pCoord = webgis.fromWGS84ToProj4(sender.originalSrs(), sender.originalSrsP4Params(), coord.y, coord.x);

                                if (this.hasDynamicCalcCrs() === true) {
                                    coord.srs = sender.originalSrs();
                                }
                            }
                            if (pCoord != null) {
                                coord.X = pCoord.x;
                                coord.Y = pCoord.y;
                            }
                            coord.projEvent = e.channel == 'beforeaddvertex' ? 'added' : 'changed';

                            //console.log('originalCrsd', sender.originalSrs(), sender.originalSrsP4Params(), coord);
                        }
                    }
                    webgis.tools.fireActiveToolEvents(this, e, coord);
                }, this);
                this.sketch.events.on(['onchanged', 'onremoved', 'onchangegeometrytype'], function (e) {
                    var prop = this.sketch.calcProperties("X", "Y");
                    if (prop.length || prop.set_values)
                        $('.webgis-sketch-length').val(webgis.calc.round(prop.length || 0, 2));
                    if (prop.circumference || prop.set_values || 0)
                        $('.webgis-sketch-circumference').val(webgis.calc.round(prop.circumference || 0, 2));
                    if (prop.area || prop.set_values)
                        $('.webgis-sketch-area').val(webgis.calc.round(prop.area || 0, 2));

                    $(".webgis-ui-emtpy-onchage-sketch").empty();   // zB 3D Messen Tabelle nach jeder änderung des Sketches wieder verwerfen
                }, this);
                this.sketch.events.on(['onvertexadded'], function (e, sender, coord) {
                    webgis.tools.fireActiveToolEvents(this, e, coord);
                }, this);
                this.sketch.events.on(['onsketchclosed'], function (e, sender) {
                    webgis.triggerEvent('.webgis-event-trigger-onsketchclosed');
                }, this);
                this.sketch.events.on(['oneditablechanged'], function (e, sender) {
                    this._refreshToolBubbles();
                }, this);
            }

            $(this.elem).find('.leaflet-control-zoom').css('margin-top', 40).addClass('webgis-ui-trans');
            this._parseToolbars();

            this.events.on('queryresult_removed-or-replaced', function (channel, sender) {
                //console.log('queryresult-changed-event');

                if (this.queryResultFeatures.hasLinks() &&
                    this.queryResultFeatures.firstService() &&
                    this.queryResultFeatures.firstQuery()) {
                    var args = [];
                    args["service"] = this.queryResultFeatures.firstService().id;
                    args["query"] = this.queryResultFeatures.firstQuery().id;
                    args["oids"] = this.queryResultFeatures.featureIds(true);

                    webgis.tools.onButtonClick(this, { command: 'refresh-query-links', type: 'servertoolcommand_ext', id: 'webgis.tools.removefromselection', map: this }, null, null, args);
                }
            }, this);
        }

        var refreshTimer = new webgis.timer(function (map) {
            //console.log('refresh');
            if (_zoomBoundsAtZoomstart != null && _zoomBoundsAtZoomstart.ignore !== true) {
                //console.log('_zoomBoundsAtZoomstart', _zoomBoundsAtZoomstart);
                map._zoomStack.push(_zoomBoundsAtZoomstart);
            }

            _zoomBoundsAtZoomstart = null;
            map.ui.refreshUIElements();

            map.events.fire('refresh', map);

            if (map.getActiveTool() == null || map.getActiveTool().id.indexOf(webgis._defaultToolPrefix) == 0) // default Tool... -> sollte nicht immer als Marker sichtbar sein.
                map.hideDraggableClickMarker();
            map.refreshSnapping();
            map.setLastZoomEventTime();

            var activeTool = map.getActiveTool();
            if (activeTool != null && activeTool.tooltype === 'current_extent') {
                if (activeTool.type === 'servertool') {
                    //var customEvent = [];
                    //customEvent['_method'] = 'box';
                    webgis.tools.sendToolRequest(map, activeTool, 'toolevent', {}/*, customEvent*/);
                }
            }

            if (map.viewLense.isActive() === true) {
                map.viewLense.updateOptions();
            }
            if (map._currentDynamicContent && map._currentDynamicContent.extentDependend === true) {
                map.loadDynamicContent(map._currentDynamicContent);
            }

        }, 500, this);
        var refreshEvents = ['viewreset', 'moveend', 'zoomend'];
        for (var e in refreshEvents) {
            this.events.on(refreshEvents[e], function () {
                refreshTimer.Start();
            });
        }
        webgis.continuousPosition.events.on('startwatching', function (e, sender) {
            console.log('start watching...');
            webgis.variableContent.set('gps-accuracy', '---');
            $('.webgis-gps-acc-dependent').addClass('gps-inaccurate');
        }, this);
        webgis.continuousPosition.events.on('stopwatching', function (e, sender) {
            //console.log('stop watching...');
            webgis.variableContent.set('gps-accuracy');
        }, this);
        webgis.continuousPosition.events.on('watchposition', function (e, sender, pos) {
            webgis.variableContent.set('gps-accuracy', '&pm;' + (Math.round(pos.coords.accuracy * 100) / 100) + 'm');
            if (pos.coords.accuracy > webgis.currentPosition.minAcc || !webgis.continuousPosition.isOk())
                $('.webgis-gps-acc-dependent').addClass('gps-inaccurate');
            else
                $('.webgis-gps-acc-dependent').removeClass('gps-inaccurate');
        }, this);

        this.events.on(['onnewfeatureresponse', 'querymarkersremoved', 'onnewdynamiccontentloaded'], function () {
            // Dialoge mit Ergebnissen schließen
            this.ui.webgisContainer().find('.webgis-dockpanel.webgis-result').webgis_dockPanel('remove');
        }, this);
        this.events.on(['onnewfeatureresponse'], function () {
            this.unloadDynamicContent();
        }, this);

        //if (webgis.usability.enableHistoryManagement === true && this.elem) {
        //    var $historyBtton = $("<div>")
        //        .attr('id', 'webgis-toolhistory-button')
        //        .css('display', 'none')
        //        .data('map',this)
        //        .appendTo($(this.elem))
        //        .click(function () {
        //            var map = $(this).data('map');
        //            if (!map.isDefaultTool(map.getActiveTool())) {
        //                map.setParentTool();
        //            }
        //        });

        //    webgis.setHistoryItem($historyBtton);
        //}

        var $layoutToggleButton = $(".webgis-ui-toggle-layout-button[data-toggle-class]");
        $layoutToggleButton.data('map', this);

        //
        // Clickevent nur einmal definieren.
        // Diese Methode kann öfter aufgerufen werden, wenn zB Karte geladen wird
        // Dann ändert sich für diesen Button nur das map-Objekte, 
        // das Clickevent sollte aber nur einmal angeführt werden.
        // Sonst kann man nicht mehr zuklappen, weil Event doppelt aufgerufe wird
        //
        if (!$layoutToggleButton.attr('data-clickevent')) {
            $layoutToggleButton.attr('data-clickevent', 'true');
            $layoutToggleButton.click(function () {
                $(this).closest('.webgis-container').toggleClass($(this).attr('data-toggle-class'));
                $(this).data('map').resize();
            });
        }

        var addToolboxEvents = function (map) {
            $(map._webgisContainer)
                .find('.webgis-toolbox-holder')
                .data('map', map)
                .mouseleave(function (e) {
                    var $this = $(this);
                    var map = $this.data('map'), activeTool = map.getActiveTool();

                    if (map.isSketchTool(activeTool) || map.isGraphicsTool(activeTool)) {
                        if (e.relatedTarget) {
                            var $focus = $this.find(':focus');
                            $focus.blur();
                        }
                    }
                });
        }

        if ($(this._webgisContainer).find('.webgis-toolbox-holder').length > 0) {
            addToolboxEvents(this)
        } else {
            this.events.on('toolboxloaded', function () {
                addToolboxEvents(this)
            }, this);
        }

        if ($.fn.webgis_splitter) {
            $(".webgis-splitter").webgis_splitter({ map: this });
        }

        webgis.delayed(function () {
            webgis.events.fire('map-initialized', webgis, map);
        }, 0);
    };
    this.init(felem, crs, initialExtent);
    this._lastZoomEventTime = null;
    this.setLastZoomEventTime = function () {
        this._lastZoomEventTime = new Date().getTime();
    };
    this.ifLastZoomEventMilliSec = function (milliseconds) {
        if (this._lastZoomEventTime == null)
            return true;
        return Math.abs(new Date().getTime() - this._lastZoomEventTime) > milliseconds;
    };

    this._lastBBoxEventTime = null;
    this._setLastBBoxEventTime = function () {
        this._lastBBoxEventTime = new Date().getTime();
    };
    this._ifLastBBoxEventMilliSec = function (milliseconds) {
        if (this._lastBBoxEventTime == null)
            return true;
        return Math.abs(new Date().getTime() - this._lastBBoxEventTime) > milliseconds;
    };

    this._lastRightClickTime = new Date().getTime();

    this._hourglassCounter = new webgis.hourglassCounter();
    this._hourglassCounter.events.on('showhourglass', function (e, sender) {
        this.events.fire('showhourglass', this, sender.names());
    }, this);
    this._hourglassCounter.events.on('hidehourglass', function (e, sender) {
        this.events.fire('hidehourglass', this);
    }, this);

    this._hourglassCounterGuid = new webgis.hourglassCounter();
    this._hourglassCounterGuid.events.on('showhourglass', function (e, sender) {
        this.events.fire('showhourglass_guids', this, sender.names());
    }, this);
    this._hourglassCounterGuid.events.on('hidehourglass', function (e, sender) {
        this.events.fire('hidehourglass_guids', this);
    }, this);

    this._requestSequence = webgis.guid();
    this.incRequestId = function () {
        //if (this._requestSequence > 1000000)
        //    this._requestSequence = 0;
        //this._requestSequence++;
        this._requestSequence = webgis.guid();
        this.events.fire('requestidchanged', this);
        //console.log('new requestId=' + this._requestSequence);
    };
    this.currentRequestId = function () {
        return this._requestSequence;
    };
    /********* Serialization ***********/
    this.destroy = function () {
        webgis._removeMap(this);

        if (webgis.mapFramework === 'leaflet') {
            if (this.selections) {
                // Destroy Selections
                for (var s in this.selections) {
                    var selection = this.selections[s];
                    selection.destroy();
                }
            }
            this.selections = [];

            this.frameworkElement.remove();
            this.frameworkElement = null;
        }
        this.services = {};
    };
    this.isDestroyed = function () {
        return this.frameworkElement == null;
    };
    this.serialize = function (options) {
        //debugger;
        if (this.serviceIds().length === 0 || !this.crs)
            return {};

        var asMaster = options && options.asMaster === true;

        var intialBounds = null;
        if (this.initialBounds && this.initialBounds.length === 4) {
            var sw = this.crs.frameworkElement.projection.unproject(L.point(this.initialBounds[0], this.initialBounds[1]));
            var ne = this.crs.frameworkElement.projection.unproject(L.point(this.initialBounds[2], this.initialBounds[3]));
            intialBounds = [sw.lng, sw.lat, ne.lng, ne.lat];
        }

        var o = asMaster === true ? {} : {
            crs: { epsg: this.crs.id, id: this.crs.name },
            scale: this.scale(),
            center: this.getCenter(),
            bounds: this.getExtent(),
            initialbounds: intialBounds,
            services: [],
            dynamiccontent: [],
            userdata: {},
            selections: []
        };
        if (options.ui === true) {
            o.ui = this.ui.serialize();

            if (asMaster === true && o.ui.options) {
                o.ui.options = $.grep(o.ui.options, function (e) {
                    switch (e.element) {
                        case 'topbar':
                            delete e.selector;
                            return true;
                        case 'tabs':
                            delete e.selector;
                            if (e.options) {
                                delete e.options.left;
                                delete e.options.right;
                                delete e.options.bottom;
                                delete e.options.top;
                                delete e.options.width;
                                delete e.options.content_size;
                            }
                            return true;
                    }
                    return false;
                });
            }
        }

        var query_results = [];

        var $tabControl = this.ui.getQueryResultTabControl();
        if ($tabControl) {
            // store pinned results in modern-result-behavoir
            var tabsOptions = $tabControl.webgis_tab_control('getTabsOptions');
            for (var t in tabsOptions) {
                var tabOptions = tabsOptions[t];
                if (!tabOptions.pinned)
                    continue;

                var ser = this.queryResultFeatures.serialize(tabOptions.payload.features);
                if (ser) {
                    ser.tab = { id: tabOptions.id, title: tabOptions.title, selected: tabOptions.selected, pinned: tabOptions.pinned };
                    query_results.push(ser);
                }
            }
        } else if (this.queryResultFeatures.connected() === true) {
            // only store "connected" results from TSP in classic-result-behavoir
            query_results = [this.queryResultFeatures.serialize()];
        }

        if (query_results.length > 0) {
            o.query_results = query_results;
        }

        if (asMaster !== true) {
            if (/*options.selection ==*/true) {
                o.selections = [];
                for (var s in this.selections) {
                    var selection = this.selections[s];
                    if (selection.isActive())
                        o.selections.push(selection.serialize());
                }
            }
            for (var s in this.services) {
                let service = this.services[s];

                if (!service) {
                    continue;
                }

                if (options) {
                    if (options.visibleBasemapsOnly === true &&
                        service.isBasemap && service.hasVisibleLayers() === false) {
                        continue;
                    }
                    if (options.ignoreCustomServices && service.type === 'custom') {
                        continue;
                    }
                }

                o.services.push(service.serialize(options));
            }
            for (var d in this.dynamicContent) {
                o.dynamiccontent.push(this._serializeDynamicContent(this.dynamicContent[d]));
            }
            //if (this.selections.query && this.selections.query.service) {
            //    o.selections.push({
            //        type: 'query',
            //        fids: this.selections.query.fids,
            //        queryid: this.selections.query.queryid,
            //        serviceid: this.selections.query.service.id
            //    });
            //}
            if (this._webgisContainer && $(this._webgisContainer).find('.webgis-tool-button-bar').length > 0) {
                o.toolbars = [];
                $(this._webgisContainer).find('.webgis-tool-button-bar').each(function (i, e) {
                    var $e = $(e);
                    o.toolbars.push({
                        cls: $e.attr('class'),
                        style: $e.attr('style'),
                        "data-tools": $e.attr('data-tools')
                    });
                });
            }

            if (_focusedServices) {
                o.focused_services = _focusedServices;
            }

            this.events.fire('onserialize', o.userdata);
            o.userdata.meta = {
                author: webgis.hmac.userName()
            };
        }

        this.events.fire('onmapserialized', o);

        return o;
    };

    this.serializeCurrentVisibility = function () {
        var result = {};
        var serviceIds = this.serviceIds();

        for (var s in serviceIds) {
            var service = this.getService(serviceIds[s]);
            if (!service)
                continue;

            result[service.id] = service.getLayerVisibilityPro();
        }

        return result;
    };

    // CalcCrs / Crs 
    this._crs = [];
    this.setCalcCrs = function (crsId) {
        //console.trace('setCalcCrs', crsId);
        _calcCrsId = crsId;
    };
    this.calcCrs = function (coords) {
        var crsId = _calcCrsId;
        if (crsId === 0)
            return this.crs;

        if (typeof crsId === 'function') {
            if (!coords) {
                var center = this.getCenter();
                coords = [{ x: center[0], y: center[1] }];
                //console.log('coords', coords);
            }

            crsId = crsId(coords);
        }

        if (!this._crs[crsId]) {
            var crsDef = webgis.crsInfo(crsId);
            if (crsDef.id && crsDef.name && crsDef.p4) {
                this._crs[crsId] = webgis.createCRS(crsId, crsDef.name, crsDef.p4, {}) || {};
                //console.log('map-calcCrs-created', this._crs[crsId]);
            }
        }

        return this._crs[crsId];
    };
    this.hasDynamicCalcCrs = function () { return typeof _calcCrsId === 'function' };

    this.calcCrsId = function () {
        var calcCrs = this.calcCrs();
        if (calcCrs) {
            return calcCrs.id;
        }

        return 0;
    };

    this.crsId = function () {
        if (this.crs) {
            return crs.id;
        }

        return 0;
    };

    // For Load Projekt
    this.deserialize = function (mapJson, options) {
        if (typeof mapJson === 'string') {
            mapJson = webgis.$.parseJSON(mapJson);
        }

        // remove/clean UI Elements
        if ($.fn.webgis_dockPanel) {
            $(this._webgisContainer).webgis_dockPanel('removeAll');
        }

        var $tabControl = this.ui.getQueryResultTabControl();
        if ($tabControl) {
            $tabControl.webgis_tab_control('removeAll');
        }

        // remove services & destroy map
        this.removeServices(this.serviceIds());
        this.destroy();

        var mapOptions = {
            extent: mapJson.crs.id,
            services: '',
            layers: [],
            custom: {},
            query_results: mapJson.query_results
        };
        for (var s in mapJson.services) {
            if (mapOptions.services != '')
                mapOptions.services += ',';
            mapOptions.services += mapJson.services[s].id;
            if (mapJson.services[s].layers) {
                mapOptions.layers[mapJson.services[s].id] = mapJson.services[s].layers;
            }

            if (mapJson.services[s].custom_request_parameters) {
                for (var c in mapJson.services[s].custom_request_parameters) {
                    mapOptions.custom['custom.' + mapJson.services[s].id + '.' + c] = mapJson.services[s].custom_request_parameters[c];
                }
            }
        }

        webgis._createMap(this.elem.id, mapOptions, this);
        if (mapJson.scale && mapJson.center)
            this.setScale(mapJson.scale, mapJson.center);

        // Set Opacity, Order
        for (var s in mapJson.services) {
            var jsonService = mapJson.services[s];
            var service = this.getService(jsonService.id);
            if (service) {
                service.updateProperties(jsonService);
            }
        }

        // Focus Services
        if (mapJson.focused_services && mapJson.focused_services.ids && mapJson.focused_services.ids.length > 0) {
            this.focusServices(mapJson.focused_services.ids, mapJson.focused_services.getOpacity());
        }

        //debugger;
        if (options && options.ui == true) {
            this.ui.deserialize(mapJson.ui);
        }
    };
    this.deserializeUI = function (mapJson) { 
        if (typeof mapJson === 'string') {
            mapJson = webgis.$.parseJSON(mapJson);
        }

        if (this._webgisContainer && mapJson.toolbars && mapJson.toolbars.length > 0) {
            for (var t in mapJson.toolbars) {
                var toolbar = mapJson.toolbars[t];
                $("<div class='" + toolbar.cls + "' style='" + toolbar.style + "' data-tools='" + toolbar["data-tools"] + "'></div>").appendTo($(this._webgisContainer));
            }
            this._parseToolbars();
        }
        this.ui.deserialize(mapJson.ui);
    };
    /******* Services/Queries *********/
    this.services = {};
    this.serviceIds = function () {
        var ids = [];
        for (var serviceId in this.services) {
            if (this.services[serviceId] != null)
                ids.push(serviceId);
        }
        return ids;
    };
    this.addCustomService = function (options) {
        var customId = '__custom_' + (options.id || webgis.hash(options.name));

        if (!this.getService(customId)) {

            this.addServices([{
                id: customId,
                name: options.name,
                frameworkElement: options.frameworkElement,
                isbasemap: options.isBasemap || false,
                fallback: options.fallback || null,
                previewImageUrl: options.previewImageUrl || null,
                onAdd: options.onAdd || null,
                onRemove: options.onRemove || null,
                layers: options.layers || [
                    {
                        "name": "_alllayers",
                        "id": "0",
                        "type": "unknown"
                    }
                ],
                type: 'custom'
            }]);

            return this.getService(customId);
        }

        return null;
    };
    this.addServices = function (services, layerVisibilities) {
        var ret = [];

        for (var s in services) {
            var serviceInfo = services[s];
            if (serviceInfo.supportedCrs && this.crs && $.inArray(this.crs.id, serviceInfo.supportedCrs) < 0) {
                webgis.alert("Crs " + this.crs.id + " is not supported for service '" + serviceInfo.name + "'", "info");
                continue;
            }

            serviceInfo.insertOrder = serviceInfo.order || (this.servicesCount() + 1) * (serviceInfo.isbasemap ? 1 : 10);
            //console.log('insertOrder', serviceInfo.id, serviceInfo.insertOrder);
            var service = new webgis.service(this, serviceInfo);

            if (layerVisibilities != null && layerVisibilities[service.id] != null) {
                service.setServiceVisibilityPro(layerVisibilities[service.id], false);
            }

            var uniqueServiceIndex = this._uniqueServiceIndex(serviceInfo.id);

            var pane = null;
            if (webgis.leaflet.onePanePerService === true) {
                pane = webgis.guid();
            }

            this.services[uniqueServiceIndex] = service;
            if (webgis.mapFramework === 'leaflet') {
                var layer = null;

                if (serviceInfo.isbasemap) {
                    this._baseMaps.services[serviceInfo.id] = service;
                    if (serviceInfo.properties && serviceInfo.properties.basemap_type === 'overlay') {

                        if (layerVisibilities != null && layerVisibilities[service.id] != null) {
                            if ($.grep(layerVisibilities[service.id], function (l) { return l.visible !== false }).length > 0) {
                                this._baseMaps.currentOverlays.push(service);
                                service.setLayerVisibility(["*"], true);
                            } else {
                                service.setLayerVisibility(["*"], false);
                            }
                        }
                    } else {
                        if (layerVisibilities != null && layerVisibilities[service.id] != null) {
                            if ($.grep(layerVisibilities[service.id], function (l) { return l.visible !== false }).length > 0) {
                                if (this._baseMaps.current != null) {
                                    this._baseMaps.current.setLayerVisibility(["*"], false);
                                }
                                this._baseMaps.current = service;
                                service.setLayerVisibility(["*"], true);
                            } else {
                                service.setLayerVisibility(["*"], false);
                            }
                        } else {
                            if (this._baseMaps.current == null) {
                                this._baseMaps.current = service;
                                service.setLayerVisibility(["*"], true);
                            }
                            else {
                                service.setLayerVisibility(["*"], false);
                            }
                        }
                    }
                }

                if (serviceInfo.type === 'tile') {
                    //console.log('add tilecache to :' + (webgis.leaflet.tilePane || 'tilePane'));
                    var tileLayerMaxZoom = (this.crs && this.crs.resolutions && this.crs.resolutions.length > 0) ? this.crs.resolutions.length - 1 : serviceInfo.properties.resolutions.length - 1;
                    if (serviceInfo.properties.hide_beyond_maxlevel === true)
                        tileLayerMaxZoom = serviceInfo.properties.resolutions.length - 1;

                    layer = L.tileLayer(serviceInfo.properties.tileurl, {
                        continuousWorld: true,
                        pane: webgis.leaflet.tilePane || 'tilePane',
                        tileSize: serviceInfo.properties.tilesize,
                        subdomains: serviceInfo.properties.domains,
                        maxZoom: tileLayerMaxZoom,
                        maxNativeZoom: serviceInfo.properties.resolutions.length - 1,
                        x_: function (m) { return m.x.toString(16).lpad('0', 8); },
                        y_: function (m) { return m.y.toString(16).lpad('0', 8); },
                        z_: function (m) { return m.z.toString().lpad('0', 2); }
                    });
                    this._leafletLayers[uniqueServiceIndex] = layer;
                    if (serviceInfo.isbasemap) {
                        if (this._baseMaps.current === service || this._baseMaps.currentOverlays.includes(service)) {
                            layer.addTo(this.frameworkElement);
                        }
                    }
                    else {
                        // sollte im service.setFrameworkElement passieren, wenn Dienst sichtbar ist
                        //layer.addTo(this.frameworkElement);
                    }

                    if (this._miniMapRequiresService('tile')) {
                        this._addMiniMap(L.tileLayer(serviceInfo.properties.tileurl, {
                            continuousWorld: true,
                            tileSize: serviceInfo.properties.tilesize,
                            subdomains: serviceInfo.properties.domains,
                            maxZoom: (this.crs && this.crs.resolutions && this.crs.resolutions.length > 0) ? this.crs.resolutions.length - 1 : serviceInfo.properties.resolutions.length - 1,
                            maxNativeZoom: serviceInfo.properties.resolutions.length - 1,
                            x_: function (m) { return m.x.toString(16).lpad('0', 8); },
                            y_: function (m) { return m.y.toString(16).lpad('0', 8); },
                            z_: function (m) { return m.z.toString().lpad('0', 2); }
                        }));
                    }
                }
                else if (serviceInfo.type === 'vtc') {
                    layer = L.maplibreGL({ style: serviceInfo.properties.vtc_styles_url });
                    this._leafletLayers[uniqueServiceIndex] = layer;

                    serviceInfo.fallback = serviceInfo.properties.fallback;
                    
                    service.onAdd = function (map, frameworkElement) {
                        console.log('VTC - onAdd -update service');
                        frameworkElement._update();
                    }

                    if (serviceInfo.isbasemap) {
                        if (this._baseMaps.current === service || this._baseMaps.currentOverlays.includes(service)) {
                            layer.addTo(this.frameworkElement);
                        }
                    }

                    if (this._miniMapRequiresService('vtc')) {
                        this._addMiniMap(L.maplibreGL({ style: serviceInfo.properties.vtc_styles_url }));
                    }
                }
                else if (serviceInfo.type === 'collection' && serviceInfo.services && serviceInfo.services.length > 0) {
                    layer = L.imageOverlay.webgis_collection(webgis.baseUrl + '/rest/services/' + serviceInfo.id + '/getmap', service);
                    this._leafletLayers[uniqueServiceIndex] = layer;
                    layer.addTo(this.frameworkElement);
                }
                else if (serviceInfo.type === 'image') {
                    this.frameworkElement.createPane(pane);
                    layer = L.imageOverlay.webgis_service(webgis.baseUrl + '/rest/services/' + serviceInfo.id + '/getmap', service, pane ? { pane: pane } : {})
                    this._leafletLayers[uniqueServiceIndex] = layer;
                    layer.addTo(this.frameworkElement);
                }
                else if (serviceInfo.type === 'static_overlay') {
                    this.frameworkElement.createPane(pane);
                    layer = L.imageOverlay.webgis_static_service(serviceInfo.id, service, pane ? { pane: pane } : {});
                    layer.on('passpoints-changed', function (e) {
                        var geoRefDef = e.target.getGeoRefDefintion ? e.target.getGeoRefDefintion() : null;
                        //console.log(e.target.getGeoRefDefintion, geoRefDef);
                        if (geoRefDef) {
                            console.log('geoRefDef', geoRefDef);
                            webgis.tools.onOverlayGeoRefDefinition(this, geoRefDef);
                        }
                    }, this);
                    this._leafletLayers[uniqueServiceIndex] = layer;

                    layer.addTo(this.frameworkElement);
                }
                else if (serviceInfo.type === 'custom' && serviceInfo.frameworkElement) {
                    layer = serviceInfo.frameworkElement;
                    this._leafletLayers[uniqueServiceIndex] = layer;
                    //layer.addTo(this.frameworkElement);
                }

                if (serviceInfo.properties) {
                    serviceInfo.previewImageUrl = serviceInfo.properties.preview_url;  // fix preview image
                }

                service.setFrameworkElement(layer);
                service.events.on('beginredraw', function (e, sender) {
                    this._hourglassCounter.inc(sender.name);
                    this._hourglassCounterGuid.inc(sender.guid);
                }, this);
                service.events.on('endredraw', function (e, sender) {
                    this._hourglassCounter.dec(sender.name);
                    this._hourglassCounterGuid.dec(sender.guid);
                }, this);
                service.events.on(['onchangevisibility','onchangevisibility-delayed'], function (e, sender) {
                    this.ui.refreshUIElements();
                    this.events.fire('onservice-changevisibility', this, sender);
                }, this);
            }

            service.events.on('onerror', function (channel, sender, args) {
                args.service = sender;
                args.map = this;

                //var requestId = sender.currentRequestId();
                this.events.fire('onserviceerror', this, args);
            }, this);

            this.events.fire('onaddservice', service);
            ret.push(service);
        }
        this.ui.refreshUIElements();
        return ret;
    };
    this.servicesCount = function () {
        var count = 0;
        for (var s in this.services) {
            if (this.services[s] != null) {
                count++;
            }
        }

        return count;
    };
    this.servicesCountByType = function (type) {
        return this.servicesByType(type).length;
    };
    this.servicesByType = function (type) {
        var services = [];

        for (var s in this.services) {
            if (this.services[s] != null && this.services[s].serviceInfo && this.services[s].serviceInfo.type === type) {
                services.push(this.services[s]);
            }
        }

        return services;
    };
    this.sortedServices = function (sortProperty) {
        var services = [];

        for (var s in this.services) {
            services.push(this.services[s]);
        }

        services.sort(function (a, b) {
            if (a[sortProperty] < b[sortProperty])
                return -1;
            if (a[sortProperty] > b[sortProperty])
                return 1;
            return 0;
        });

        return services;
    };
    this.removeServices = function (serviceIds) {
        var newServices = {};

        for (var s in this.services) {
            var service = this.services[s];

            if (!service)
                continue;
            if ($.inArray(s, serviceIds) >= 0) {
                this.services[s] = null;
                service.remove();
                console.log('remove: ' + s);
                this.events.fire('onremoveservice', service);
            } else {
                newServices[service.id] = service;
            }
        }
        this.services = newServices;
        this.ui.refreshUIElements();
    };
    // Wird nicht mehr unterstützt
    //this.setServiceOrder = function (serviceIds) {
    //    var order = serviceIds.length;
    //    for (var i in serviceIds) {
    //        var service = this.services[serviceIds[i]];
    //        console.log(serviceIds[i], service);
    //        order--;
    //        if (!service)
    //            continue;
    //        service.setOrder(order);
    //    }
    //};
    this.maxBackgroundCoveringServiceOrder = function () {
        var maxOrder = 0;
        for (var serviceId in this.services) {
            var service = this.services[serviceId];
            if (service && (service.type === "tile")) {
                maxOrder = Math.max(maxOrder, service.getOrder());
            }
        }
        return maxOrder;
    };
    this.maxServiceInsertOrder = function (serviceInfo) {
        var maxOrder = 0;

        for (var serviceId in this.services) {
            var service = this.services[serviceId];

            var serviceOrder = service.getOrder();
            if (serviceInfo.isbasemap === true) {
                if (service.isBasemap === true) {
                    if (serviceInfo.properties && service.serviceInfo.properties &&
                        (serviceInfo.properties.basemap_type === "overlay" || serviceInfo.properties.basemap_type == service.serviceInfo.properties.basemap_type)) {
                        maxOrder = Math.max(maxOrder, serviceOrder);
                    }
                }
            } else {
                maxOrder = Math.max(maxOrder, serviceOrder);
            }
        }

        return serviceInfo.isbasemap ? maxOrder + 1 : maxOrder + 10;
    };
    this.maxStaticOverlayOrder = function () {
        var maxOrder = 0;
        for (var serviceId in this.services) {
            var service = this.services[serviceId];
            if (service && (service.type === "static_overlay")) {
                maxOrder = Math.max(maxOrder, service.getOrder());
            }
        }
        return maxOrder;
    };
    this.maxServiceOrder = function () {
        var maxOrder = 0;
        for (var serviceId in this.services) {
            var service = this.services[serviceId];
            if (service) {
                maxOrder = Math.max(maxOrder, service.getOrder());
            }
        }
        return maxOrder;
    };
    this.refreshServices = function (serviceIds) {
        for (var serviceId in this.services) {
            if ($.inArray(serviceId, serviceIds) >= 0 || (serviceIds.length == 1 && serviceIds[0] == '*'))
                this.services[serviceId].refresh();
        }
    };
    this.getService = function (id) {
        for (var s in this.services) {
            var service = this.services[s];
            if (service == null)
                continue;
            if (service.id == id)
                return service;
        }
        return null;
    };
    this.recalcServiceOrder = function () {
        var servicesArray = [];
        for (var s in this.services) {
            servicesArray.push(this.services[s]);
        }

        var orderFunc = function (s1, s2) {
            if (s1.getOrder() == s2.getOrder())
                return 0;
            if (s1.getOrder() < s2.getOrder())
                return -1;

            return 1;
        };

        servicesArray.sort(orderFunc);
        for (var i in servicesArray) {
            servicesArray[i].setOrder((parseInt(i) + 1) * 10);
            //console.log('new service order ' + servicesArray[i].id, servicesArray[i].getOrder());
        }
    };
    this.addServicesCustomRequestParameters = function (params) {
        for (var s in this.services) {
            if (this.services[s]) {
                this.services[s].addCustomRequestParameters(params)
            }
        }

        return params;
    };

    this.setServiceVisibility = function (id, layerIds) {
        var service = this.getService(id);
        if (service) service.setServiceVisibility(layerIds);
    };
    this.setServiceOpacity = function (id, opacity) {
        var service = this.getService(id);
        if (service) service.setOpacity(opacity);
    };

    var _focusedServices = null;
    this.focusServices = function (ids, opacity) {
        if (!ids)
            return;

        opacity = opacity || 0.25;

        ids = typeof ids === 'string' ? [ids] : ids;

        for (var s in this.services) {
            if (this.services[s]) {
                if ($.inArray(this.services[s].id, ids) >= 0) {
                    this.services[s].focus();
                } else {
                    this.services[s].unfocus(opacity);
                }
            }
        }

        _focusedServices = { ids: ids, opacity: opacity };

        this.events.fire('onfocusservices', this, _focusedServices = { ids: ids, opacity: opacity });
        this.ui.refreshUIElements();
    };
    this.resetFocusServices = function () {
        for (var s in this.services) {
            if (this.services[s]) {
                this.services[s].resetFocus();
            }
        }

        _focusedServices = null;

        this.events.fire('onresestfocusservices', this);
        this.ui.refreshUIElements();
    };

    this.hasFocusedServices = function () { return _focusedServices !== null };
    this.focusedServices = function () { return _focusedServices ? { ids: _focusedServices.ids, opacity: _focusedServices.opacity } : null };

    this.showServiceExclusive = function (serviceId, opacity) {
        for (var s in this.services) {
            if (this.services[s]) {
                if (this.services[s].id === serviceId) {
                    this.services[s].show();
                } else {
                    this.services[s].hide(opacity);
                }
            }
        }
    };
    this.resetShowServiceExcusives = function () {
        for (var s in this.services) {
            if (this.services[s]) {
                this.services[s].show();
            }
        }
    };

    this.showServicePassPoints = function () {
        for (var s in this.services) {
            var service = this.services[s];
            if (service && service.type === 'static_overlay') {
                service.showPassPoints();
            }
        }
    };
    this.hideServicePassPoints = function () {
        for (var s in this.services) {
            var service = this.services[s];
            if (service && service.type === 'static_overlay') {
                service.hidePassPoints();
            }
        }
    };

    this._uniqueServiceIndex = function (serviceId) {
        if (!this.services[serviceId]) {
            return serviceId;
        }

        var count = 1;
        while (true) {
            var uniqueServiceIndex = serviceId + ":" + (count++);
            if (!this.services[uniqueServiceIndex]) {
                return uniqueServiceIndex;
            }
        }
    };

    this.getQuery = function (serviceId, queryId) {
        var service = this.getService(serviceId);
        if (service) {
            return service.getQuery(queryId);
        }

        return null;
    };
    this.getEditThemes = function (serviceId, queryId) {
        var service = this.getService(serviceId);
        if (!service) {
            return null;
        }

        var query = service.getQuery(queryId);
        if (!query) {
            return;
        }

        if (query.layerid /*associatedlayers.length === 1*/) {
            var layerId = query.layerid/*associatedlayers[0].id*/;

            var editthemes = $.grep(service.editthemes, function (n, i) {
                return (n.layerid == layerId)
            });

            return editthemes;
        }

        return null;
    };
    /********* Leaflet  *********/
    this._leafletLayers = [];
    this.getLeafletLayer = function (serviceId) {
        return this._leafletLayers[serviceId];
    };

    /****** Copyright/Description ********/
    this._copyright = [];
    this.addCopyright = function (copyright) {
        if (!copyright)
            return;
        if (Array.isArray(copyright)) {
            for (var c in copyright) {
                this.addCopyright(copyright[c]);
            }
        }
        else {
            this._copyright.push(copyright);
        }
    };
    this.getCopyright = function (id) {
        for (var c in this._copyright) {
            if (this._copyright[c].id == id)
                return this._copyright[c];
        }
        return null;
    };

    var _mapDescription = '';
    this.setMapDescription = function (description) {
        _mapDescription = description || '';
    };
    this.getMapDescription = function (removeServiceSections) {
        var description = _mapDescription;

        if (removeServiceSections) {
            var serviceIds = this.serviceIds();
            for (var i in serviceIds) {
                description = description.removeSection(serviceIds[i]);
            }
        }

        return description.removeAllSectionDecoration();
    };
    /****** Selection *******/
    this.selections = [];
    this.getSelection = function (name) {
        for (var i in this.selections) {
            if (this.selections[i].name === name)
                return this.selections[i];
        }

        //console.log('createSelection:', name);
        var selection = new webgis.selection(this, name);
        if (webgis.mapFramework === 'leaflet') {
            var selectionElement = L.imageOverlay.webgis_selection(webgis.baseUrl + '/rest/services/', selection);
            selection.setFrameworkElement(selectionElement);
            selectionElement.addTo(this.frameworkElement);
        }
        this.selections[name] = selection;
        return selection;
    };
    this.getSelectionInfo = function (name) {
        var selection = this.selections[name];
        if (!selection || !selection._isActive || !selection.service || !selection.queryid || !selection.fids)
            return null;
        var query = selection.service.getQuery(selection.queryid);
        if (!query || !query.layerid /*|| !query.associatedlayers || query.associatedlayers.length == 0*/)
            return null;
        var layer = selection.service.getLayer(query.layerid /*|| query.associatedlayers[0].id*/);
        if (!layer)
            return null;
        return {
            serviceid: selection.service.id,
            layerid: layer.id,
            queryid: selection.queryid,
            ids: selection.fids.split(',').map(function (item) { return parseInt(item); }),
            geometrytype: layer.type
        };
    };
    this.refreshSelections = function () {
        for (var s in this.selections) {
            var selection = this.selections[s];
            if (selection)
                selection.refresh();
        }
    };
    /****** Query Results ********/

    this.queryResultFeatures = new webgis.map._queryResultFeatures(this);
    /****** Queries *******/
    this._queryDefinitions = [{ service: '*', query: '*', visible: true }];
    this.setQueryDefinitions = function (queries) {
        this._queryDefinitions = queries;
    };
    this.isQueryVisible = function (serviceId, queryId) {
        if (this._queryDefinitions == null)
            return true;
        for (var q in this._queryDefinitions) {
            var qDef = this._queryDefinitions[q];
            if (qDef.service == '*' && qDef.queryId == '*')
                return qDef.visible;
            if (qDef.service == serviceId && qDef.queryId == '*')
                return qDef.visible;
            if (qDef.service == serviceId && qDef.query == queryId)
                return qDef.visible;
        }
        return true;
    };
    /****** Map Navigation, Refresh... *****/
    this.refresh = function () {
        for (var s in this.services) {
            var service = this.services[s];
            if (service && service.refresh)
                service.refresh();
        }

        this.refreshSnapping();
    };
    this.zoomTo = function (bounds, project) {
        //console.trace('zoomTo', bounds);
        if (webgis.mapFramework === "leaflet") {
            if (bounds && bounds.length === 4) {
                if (project) {
                    var b = bounds;
                    if (this.crs) {
                        var sw = this.crs.frameworkElement.projection.unproject(L.point(bounds[0], bounds[1]));
                        var ne = this.crs.frameworkElement.projection.unproject(L.point(bounds[2], bounds[3]));
                        b = L.latLngBounds(sw, ne);
                    }
                    else {
                        this.zoomTo(b, false);
                    }
                    this.frameworkElement.fitBounds(b);
                }
                else {
                    this.frameworkElement.fitBounds(L.latLngBounds(L.latLng(bounds[1], bounds[0]), L.latLng(bounds[3], bounds[2])));
                }
            }
        }
    };
    this.zoomToBoundsOrScale = function (bounds, scale, resizePercent) {
        //console.trace('zoomTo', bounds);
        if (webgis.usability.zoom.allowsFreeZooming() && resizePercent !== 0) {
            resizePercent = resizePercent || 10.0;
            bounds = webgis.calc.resizeBounds(bounds, 1.0 + resizePercent / 100.0);
        }

        var boundsScale = this.estimatedBoundsScale(bounds);
        if (scale && boundsScale && boundsScale < scale) {
            //console.log('zoom/pan to scale: ' + scale, boundsScale);
            this.setScale(scale, [(bounds[0] + bounds[2]) / 2.0, (bounds[1] + bounds[3]) / 2.0]);
        }
        else { 
            //console.log('zoom to bounds', boundsScale)
            this.zoomTo(bounds);
        }
    };
    this.zoomBack = function () {
        var bounds = this._zoomStack.pull();
        if (bounds != null) {
            if (webgis.mapFramework == "leaflet") {
                _zoomBoundsAtZoomstart = { ignore: true };
                this.frameworkElement.fitBounds(bounds);
                webgis.delayed(function () {
                    _zoomBoundsAtZoomstart = null;
                }, 300);
            }
        }
    };
    this.getExtent = function () {
        if (webgis.mapFramework == 'leaflet') {
            var bounds = this.frameworkElement.getBounds();
            return [bounds.getWest(), bounds.getSouth(), bounds.getEast(), bounds.getNorth()];
        }
    };
    this.getSize = function () {
        if (webgis.mapFramework == 'leaflet') {
            var size = this.frameworkElement.getSize();
            return [size.x, size.y];
        }
    };
    this.inExtent = function (rect) {
        if (!rect || rect.length < 4)
            return false;

        var extent = this.getExtent();

        return extent[0] <= rect[0] &&
            extent[1] <= rect[1] &&
            extent[2] >= rect[2] &&
            extent[3] >= rect[3];
    };

    this.getProjectedExtent = function () {
        if (webgis.mapFramework == 'leaflet') {
            var bounds = this.frameworkElement.getBounds();
            var crs = this.frameworkElement.options.crs;
            if (crs) {
                var nw = crs.project(bounds.getNorthWest()), se = crs.project(bounds.getSouthEast());
                return [nw.x, se.y, se.x, nw.y];
            }
            return [bounds.getWest(), bounds.getSouth(), bounds.getEast(), bounds.getNorth()];
        }
    };
    this.getCalcCrsProjectedExtent = function () {
        if (webgis.mapFramework == 'leaflet') {
            var bounds = this.frameworkElement.getBounds();

            var calcCrs = this.calcCrs();
            var nw = webgis.fromWGS84(calcCrs, bounds.getNorthWest().lat, bounds.getNorthWest().lng);
            var se = webgis.fromWGS84(calcCrs, bounds.getSouthEast().lat, bounds.getSouthEast().lng);

            return [nw.x, se.y, se.x, nw.y];
        }
    };
    this.getMapImageSize = function () {
        if (webgis.mapFramework == 'leaflet') {
            var bounds = this.frameworkElement.getBounds();
            var topLeft = this.frameworkElement.latLngToLayerPoint(bounds.getNorthWest());
            var mapSize = this.frameworkElement.latLngToLayerPoint(bounds.getSouthEast()).subtract(topLeft);
            return [mapSize.x, mapSize.y];
        }
        return [0, 0];
    };
    this.getCenter = function () {
        if (webgis.mapFramework == 'leaflet') {
            var latLng = this.frameworkElement.getCenter();
            return [latLng.lng, latLng.lat];
        }
    };
    this.setCenter = function (lngLat, duration) {
        if (webgis.mapFramework == 'leaflet') {
            var center = L.latLng(lngLat[1], lngLat[0]);
            this.frameworkElement.panTo(center, { duration: duration || 1 });
        }
    };
    this.setViewFocus = function (bbox, duration) {
        if (bbox && bbox.length === 4) {
            var mapBbox = this.getExtent();
            var ratio = webgis.calc.bboxSizeRatio(mapBbox, bbox);

            if (ratio < 1 || ratio > 5) {
                this.zoomTo(webgis.calc.bboxResizePerRatio(bbox, 1.2));
            }
            else if (!webgis.calc.bboxIntersects(mapBbox, bbox)) {
                var center = [(bbox[0] + bbox[2]) / 2.0, (bbox[1] + bbox[3]) / 2.0];
                this.setCenter(center, duration);
            }
        }
    };
    this.setSketchFocusView = function (duration, sketch) {
        sketch = sketch || this.sketch;
        if (!sketch)
            return;

        let vertices = sketch.getVerticesPro(),
            mapBbox = this.getExtent(),
            mapCenter = this.getCenter();

        let focusVertex = webgis.calc.nearestVertex({ x: mapCenter[0], y: mapCenter[1] }, vertices);
        let verticesBbox = webgis.calc.verticesBbox(vertices);

        console.log('focusVertex', focusVertex);
        console.log('verticesBbox', verticesBbox);
        console.log('center', mapCenter);

        const ratio = webgis.calc.bboxSizeRatio(mapBbox, verticesBbox);
        console.log('ratio', ratio);

        const innerMapBbox = webgis.calc.bboxResizePerRatio(mapBbox, 0.6),
            fVertex = [focusVertex.x, focusVertex.y];
        const featureMinScale = webgis.featureZoomMinScale(this.queryResultFeatures?.first()) || 250;

        if (/*ratio == 'Infinity' ||*/ ratio > 100) {  // point => 'Infinity' alway > 100
            
            if (!webgis.calc.bboxContains(innerMapBbox, fVertex)
                || this.scale() > featureMinScale) {
                this.setScale(webgis.usability.zoom.minFeatureZoom > 1
                    ? Math.max(featureMinScale, webgis.usability.zoom.minFeatureZoom)
                    : featureMinScale, fVertex);
            }
        }
        else if (ratio > 5) {
            this.zoomTo(webgis.calc.bboxResizePerRatio(verticesBbox, 1.2));
        } 
        else if (focusVertex) {
            if (!webgis.calc.bboxContains(innerMapBbox, fVertex)) {
                this.setCenter(fVertex, duration);
            }
        }
    };

    this.isWebMercator = function () {
        return this.crs && this.crs.id === 3857;
    };
    // Maßstab, der sich "intern" aus den ZoomLevels (Resolutions) ergibt
    this.scale = function () {
        if (this.serviceIds().length == 0)
            return 0.0;
        if (webgis.mapFramework == 'leaflet') {
            if (this.crs && this.crs.resolutions) {
                var zoom = this.frameworkElement.getZoom();

                return this._scaleFromZoom(zoom) || 1;
            }
            else {
                return this._scaleFromBounds(this.frameworkElement.getBounds());
            }
        }
        return 'unknown';
    };
    // Maßstab, der dem Anwender angezeigt wird
    // entspricht am ehesten der "Wirklichkeit"
    this.calcCrsScale = function () {
        if (this.calcCrsId() === this.crs.id) {
            return this.scale();
        }

        var dpm = 96.0 / 0.0254;

        return this.calcCrsResolution() * dpm;
    };
    // Maßstab der in den Services beim GetMap Verwendet wird.
    // Kann für die TOC Elemente (webgis-scaledependent) verwendet werden.
    // Bei WebMercator entspricht der Maßstab, der angezeigt wird nicht dem Maßstab beim abholen des Dienstes
    this.crsScale = function () {
        if (!this.frameworkElement.getZoom())
            return 0;

        var bounds = this.getProjectedExtent();
        var size = this.getMapImageSize();
        var worldWidth = Math.abs(bounds[2] - bounds[0]);

        var crsScale = worldWidth / size[0] * 96.0 / 0.0254;

        //console.log('crsScale', crsScale, 'scale', this.scale(), 'calcCrsScale', this.calcCrsScale());

        return crsScale;
    }

    this._scaleFromZoom = function (zoom) {
        if (this.crs && this.crs.resolutions) {
            var dpm = 96.0 / 0.0254;

            if (zoom >= this.crs.resolutions.length - 1) {
                return Math.round(this.crs.resolutions[this.crs.resolutions.length - 1] * dpm);
            } else {
                var z0 = Math.floor(zoom);
                var z1 = z0 + 1;

                var resZ0 = this.crs.resolutions[z0];
                var resZ1 = this.crs.resolutions[z1]

                var scale0 = resZ0 * dpm
                var scale1 = resZ1 * dpm

                return Math.round(scale0 - (scale0 - scale1) * (zoom - z0));
            }
        }

        return null;
    }
    this._scaleFromBounds = function (lBounds) {
        var size = this.frameworkElement.getSize();
        var R = 6378137.0, deg2rad = Math.PI / 180.0;
        var dLambda = Math.abs(lBounds.getEast() - lBounds.getWest()) * deg2rad;
        var dPhi = Math.abs(lBounds.getNorth() - lBounds.getSouth()) * deg2rad;
        var phi0 = (lBounds.getNorth() + lBounds.getSouth()) * 0.5 * deg2rad;
        var W = dLambda * R * Math.cos(phi0), H = dPhi * R;

        return (H / size.y * 96.0 / 0.0254 + W / size.x * 96.0 / 0.0254) * 0.5;
    }

    this.estimatedBoundsScale = function (bounds) {
        if (!bounds || bounds.length < 4) {
            return webgis.getUserPreferencedMinScale() || 1000;
        }
        var lBounds = L.latLngBounds(L.latLng(bounds[1], bounds[0]), L.latLng(bounds[3], bounds[2]));

        return this._scaleFromZoom(this.frameworkElement.getBoundsZoom(lBounds)) || this._scaleFromBounds(lBounds);
    }
    
    this.scales = function () {
        var ret = [];
        if (webgis.mapFramework === 'leaflet') {
            if (this.crs && this.crs.resolutions) {
                var dpm = 96.0 / 0.0254;
                for (var r in this.crs.resolutions) {
                    ret.push(this.crs.resolutions[r] * dpm);
                }
            }
        }
        return ret;
    };
    this.resolution = function () {
        if (this.serviceIds().length == 0) {
            return 0.0;
        }

        var res = 0;
        if (webgis.usability.zoom.zoomSnap < 1) {
            var dpm = 96.0 / 0.0254;
            res = this.scale() / dpm;
        }
        else {
            if (webgis.mapFramework == 'leaflet') {
                if (this.crs && this.crs.resolutions) {
                    var zoom = this.frameworkElement.getZoom(), dpm = 96.0 / 0.0254;
                    res = this.crs.resolutions[parseInt(zoom)];
                }
            }
        }

        return res;
    };
    this.calcCrsResolution = function () {
        if (this.calcCrsId() === this.crs.id) {
            return this.resolution();
        }

        var bounds = this.getCalcCrsProjectedExtent();
        var size = this.getMapImageSize();
        var worldWidth = Math.abs(bounds[2] - bounds[0]);

        return worldWidth / size[0];
    };
    this.setScale = function (s, center, suppressAnimation) {
        if (webgis.mapFramework == 'leaflet') {
            var zoomToLevel = -1;
            var dpm = 96.0 / 0.0254;
            var exactRes = s / dpm, zoomRes = -1;

            if (this.crs && this.crs.resolutions) {
                for (var zoom in this.crs.resolutions) {
                    zoomRes = this.crs.resolutions[zoom];

                    var resScale = Math.round(this.crs.resolutions[zoom] * dpm);
                    if (resScale == s) {
                        zoomToLevel = zoom;
                        break;
                    }
                    if (resScale < s) {
                        zoomToLevel = Math.max(0, zoom - 1);
                        break;
                    }
                    if (zoom == this.crs.resolutions.length - 1) { // smallest Resolution
                        zoomToLevel = (s < resScale) ? this.crs.resolutions.length - 1 : 0;
                    }
                }
            }
            if (zoomToLevel >= 0) {

                if (zoomToLevel < this.crs.resolutions.length - 1) {
                    zoomToLevel = parseInt(zoomToLevel);

                    var res0 = this.crs.resolutions[zoomToLevel];
                    var res1 = this.crs.resolutions[zoomToLevel + 1];

                    //console.log(res0, res1);
                    //console.log(exactRes);

                    if (res0 > exactRes) {
                        zoomToLevel += (res0 - exactRes) / (res0 - res1);
                        //console.log(zoomToLevel);
                    }
                }

                //this.frameworkElement.setZoom(zoomToLevel);
                //var me = this;
                //webgis.delayed(function () {
                //    //alert('pan: ' + center);
                //    if (center && center.length == 2)
                //        me.frameworkElement.setView({ lng: center[0], lat: center[1] });
                //    else if (center && center.lng && center.lat)
                //        me.frameworkElement.setView(center);

                //    webgis.delayed(function () {
                //        me.frameworkElement.setZoom(zoomToLevel);
                //    }, 300);
                //}, 200);

                if (suppressAnimation === true) {
                    if (center && center.length == 2)
                        this.frameworkElement.setView({ lng: center[0], lat: center[1] }, zoomToLevel, { animate: false });
                    else if (center && center.lng && center.lat)
                        this.frameworkElement.setView(center, zoomToLevel, { animate: false });
                } else {
                    var me = this;

                    webgis.delayed(function () {
                        //alert('pan: ' + center);
                        if (center && center.length == 2)
                            me.frameworkElement.setView({ lng: center[0], lat: center[1] }, zoomToLevel);
                        else if (center && center.lng && center.lat)
                            me.frameworkElement.setView(center, zoomToLevel);
                    }, 200);
                }
            }
        }
    };
    this.setCalcCrsScale = function (s, center, counter) {
        if (this.calcCrsId() === this.crs.id) {
            return this.setScale(s, center);
        }

        var scale = this.scale();
        var calcCrsScale = this.calcCrsScale();

        //console.log('old-scale', scale, 'old-calcCrsScale', calcCrsScale);

        // approximation!!
        var fac = scale / calcCrsScale;
        this.setScale(s * fac, center, true);

        if (webgis.usability.zoom.useIterativeZoomingToFixScale === true) {
            webgis.delayed(function (args) {
                if (args.counter < 10) {
                    var newCalcScale = args.map.calcCrsScale();
                    console.log(args.counter + ' new-scale', args.map.scale(), 'new-calcCrsScale', newCalcScale);
                    if (Math.abs(newCalcScale - s) > 5)
                        args.map.setCalcCrsScale(s, center, args.counter);
                }
            }, 50, { map: this, counter: (counter || 0) + 1 });
        }
    };

    this.invalidateSize = function () {
        if (webgis.mapFramework == 'leaflet') {
            this.frameworkElement.invalidateSize();
        }
    };
    var _dpi = -1.0;
    this.Dpi = function () {
        return _dpi;
    }
    this.setDpi = function (dpi) {

        if (_dpi <= 0.0 && dpi <= 0.0)
            return;

        if (_dpi !== dpi) {
            _dpi = dpi;

            //console.log('setdpi', dpi);

            this.refresh();
        }
    }
    this.resetDpi = function () { this.setDpi(-1.0); }

    this.resize = function () {
        webgis.delayed(function (map) {
            map.frameworkElement._onResize();
            map.events.fire('resize', map);
        }, 300, this);;
    };
    /****** Tools *****/
    this.setCursor = function (cursor) {
        if (cursor && cursor.indexOf('.') > 0) {  // *.cur, *.png, *.*
            $(this.elem).css('cursor', 'url(' + webgis.baseUrl + '/content/api/cursors/' + cursor + '),pointer');
        } else {
            $(this.elem).css('cursor', cursor);
        }
    };
    this._tools = [];
    this.addTool = function (tool) {
        if (tool && tool.id) {
            if (this._tools[tool.id])
                return;
            if (tool.marker && tool.marker.iconUrl) {
                if (!tool.marker.iconUrl.toLowerCase().indexOf("http://") == 0 && !tool.marker.iconUrl.toLowerCase().indexOf("https://") == 0)
                    tool.marker.iconUrl = webgis.baseUrl + "/" + tool.marker.iconUrl;
            }
            
            this._tools[tool.id] = tool;
            if (tool.id == this._defaultToolId) {
                var defaultTool = $.extend({}, {}, tool); // clone;
                defaultTool.uiElement = $("<div style='display:none'></div>").appendTo("body");
                defaultTool._isDefaultTool = true;
                defaultTool.id = webgis._defaultToolPrefix + defaultTool.id;
                this._tools[defaultTool.id] = defaultTool;

                webgis.tools.onButtonClick(this, defaultTool);
            }
        }
    };
    this.getTool = function (id) {
        return this._tools[webgis.compatiblity.toolId(id)];
    };
    this.isServerButton = function (id) {
        var tool = this.getTool(id);
        return tool && tool.type === 'serverbutton';
    };
    this._activeTool = null;
    this.setDefaultTool = function () { this.setActiveTool(null); };
    this.setActiveTool = function (tool) {
        if (tool && (tool.type == 'clientbutton' || tool.type == 'serverbutton')) { // nur tools können active Tool sein;
            return;
        }
        if (this.isSketchTool(this._activeTool)) {

            // Readonly Sketches müssen nicht gespeichert werden
            if (this.sketch.isReadOnly() !== true) {
                // sketch immer speichern, sonst könnten letzte Punkte versschwinden, wenn man zB beim Messen 
                // noch einmal auf den Tool Button click. Dann würde weiter unter der letzte Sketch geladen werdern ...
                this._activeTool.currentSketch = this.sketch.store();

                //console.log('stored currentSketch', this._activeTool.id, this._activeTool.currentSketch);
            }
        }

        if (this.isCustomTool(this._activeTool)) {
            this.removeMarkerGroup('custom-temp-marker');
        };

        if ( /*tool == null*/tool != this._activeTool) {
            if (this.isSketchTool(this._activeTool)) {
                //this._activeTool.currentSketch = this.sketch.store();
                if (webgis.currentPosition.useWithSketchTool === true) {
                    this.ui.removeAllQuickAccessButtons();
                }
                this.sketch.ui.hideContextMenu(true);
            }
            if (this.isGraphicsTool(this._activeTool)) {
                this.graphics.hideContextMenu(true);
            }

            if (webgis.mapFramework === "leaflet" && this.frameworkElement) {
                this.frameworkElement.doubleClickZoom.enable();
            }
            this.hideDraggableClickMarker();
            if (this.sketch)
                this.sketch.hide();
            if (this._activeTool) {
                if ($.fn.webgis_modal)
                    $(null).webgis_modal('close', {
                        id: this._activeTool.id
                    });
                if ($.fn.webgis_dockPanel)
                    $(null).webgis_dockPanel('close', {
                        id: this._activeTool.id
                    });
                $('.tool-' + webgis.validClassName(this._activeTool.id)).removeClass('active');
                this._activeTool._isInitialized = false;

                this.ui._removeToolBubble();
            }
            this._toolEvents = [];

            if (this._activeTool != null) {
                if (this._activeTool.tooltype === 'box') {
                    this.touchEventHandlers.unbindBoxEvents();
                }

                if (this._activeTool.tooltype === 'watch_position') {
                    webgis.continuousPosition.stop();
                }

                if (this._activeTool.tooltype === 'graphics') {
                    this.graphics.assumeCurrentElement(true);
                }

                if (this.selections[this._activeTool.id]) {
                    this.selections[this._activeTool.id].remove();
                }

                if (this._activeTool.tooltype === "overlay_georef_def") {
                    this.hideServicePassPoints();
                }

                if (this._activeTool.id === "webgis.tools.print") {
                    this.queryResultFeatures.setMarkerVisibility(true);
                    this.queryResultFeatures.showToolMarkerLabels('webgis.tools.coordinates', true);
                    this.queryResultFeatures.showToolMarkerLabels('webgis.tools.chainage', true);
                    this.queryResultFeatures.setToolMarkerVisibility('webgis.tools.coordinates', true);
                    this.queryResultFeatures.setToolMarkerVisibility('webgis.tools.chainage', true);
                }
            }
        }

        this.events.fire('beforechangeactivetool', this, tool);
        this._activeTool = tool;
        this.events.fire('onchangeactivetool', this);

        this._disableBBoxToolHandling();
        this._disableSketchClickHandling();

        if (tool == null) {
            if (this.getDefaultTool() != null) {
                webgis.tools.onButtonClick(this, this.getDefaultTool());
            } else {
                // Falls Druckwerkzeug aktiv war
                this.viewLense.hide();
            }
            this.setCursor('');
            return;
        }
        else {
            this.setCursor(tool.cursor);

            if (tool.id === "webgis.tools.print") {
                this.queryResultFeatures.showToolMarkerLabels('webgis.tools.coordinates', false);
                this.queryResultFeatures.showToolMarkerLabels('webgis.tools.chainage', false);
            }
        }

        // What is this good for? (artefact from webgis 5!)?
        // => eg. ChildTools a not registered in webgis_toolbox process with map.addTool(), because they are not visible in the toolbox
        //    with this, also childtools will registered. Is is nessessary. Otherwise childtools like webgis.editing.advanced.clipfeatures
        //    will not get back their requireded dependencies like current selection.
        //    => callbacks (webgis_uibilder.appendCallback will try to add the original tool instead of the "tool.id" only; see "// assign original tool, if possible" there)
        if (!this.getTool(tool.id)) {
            this.addTool(tool);
        }

        if (tool.tooltype === 'click') {
            // Clickmarker to center??
        }
        else if (tool.tooltype === 'box') {
            this._enableBBoxToolHandling();
        }
        else if (tool.tooltype === 'print_preview') {
            if (webgis.mapFramework === "leaflet" && this.frameworkElement) {
                this.frameworkElement.touchZoom.disable();
                this.frameworkElement.doubleClickZoom.disable();
                this.frameworkElement.scrollWheelZoom.disable();
                this.frameworkElement.boxZoom.disable();
                this.frameworkElement.keyboard.disable();
                $(this.elem).closest('.webgis-container').find('.leaflet-control-container, .webgis-tool-button-bar').css('display', 'none');
            }
        }
        else if (tool.tooltype === 'watch_position') {
            webgis.continuousPosition.start(this);
        }
        else if (tool.tooltype === 'bubble') {
            this.ui._addToolBubble();
        }

        if (tool.tooltype && tool.tooltype.indexOf('sketch') === 0) {
            if (webgis.mapFramework === "leaflet") {
                this.frameworkElement.doubleClickZoom.disable();
            }
        }

        //console.log('tool.id', tool.id, tool.tooltype);
        //console.log('tool.currentSketch', tool.currentSketch);
        if (tool.tooltype === 'sketch0d' && this.sketch) {
            if (tool.currentSketch != null && tool.currentSketch.geometryType === 'point') {
                this.sketch.load(tool.currentSketch);
            } else {
                this.sketch.remove(true);
                this.sketch.unsetOriginalSrs();
                this.sketch.setReadOnly(false);
                this.sketch.setGeometryType('point');
            }
        }
        else if (tool.tooltype === 'sketch1d' && this.sketch) {
            if (tool.currentSketch != null && tool.currentSketch.geometryType === 'polyline') {
                this.sketch.load(tool.currentSketch);
            } else {
                this.sketch.remove(true);
                this.sketch.unsetOriginalSrs();
                this.sketch.setReadOnly(false);
                this.sketch.setGeometryType('polyline');
            }
            this._enableSketchClickHandling();
        }
        else if (tool.tooltype === 'sketch2d' && this.sketch) {
            if (tool.currentSketch != null && tool.currentSketch.geometryType === 'polygon') {
                this.sketch.load(tool.currentSketch);
            } else {
                this.sketch.remove(true);
                this.sketch.unsetOriginalSrs();
                this.sketch.setReadOnly(false);
                this.sketch.setGeometryType('polygon');
            }
            this._enableSketchClickHandling();
        }
        else if (tool.tooltype === 'sketchcircle' && this.sketch) {
            if (tool.currentSketch != null && tool.currentSketch.geometryType === 'circle') {
                this.sketch.load(tool.currentSketch);
            } else {
                this.sketch.remove(true);
                this.sketch.unsetOriginalSrs();
                this.sketch.setReadOnly(false);
                this.sketch.setGeometryType('circle');
            }
            this._enableSketchClickHandling();
        }
        else if (tool.tooltype === 'sketchany' && this.sketch) {
            if (tool.currentSketch) {
                this.sketch.load(tool.currentSketch);
            } else {
                this.sketch.remove(true);
                this.sketch.unsetOriginalSrs();
                this.sketch.setReadOnly(false);
            }
            this._enableSketchClickHandling();
        }
        else if (tool.tooltype === 'overlay_georef_def') {
            // do not automatically show. 
            // Tool must responde with response.addstaticoverlay.editmode = true (see webgis.tools.js)

            //this.showServicePassPoints();
        }
        else if (this.sketch && this.sketch.loadedForTool !== tool.id) {  // beim Drucken werden beispielsweise Sketches von anderen Werkzeugen (Messen) angezeigt, um diese Drucken zu können
            //console.log('this.sketch.loadForTool', this.sketch.loadedForTool);
            this.sketch.hide();
        }

        this._refreshToolBubbles(tool);

        $('.tool-' + webgis.validClassName(this._activeTool.id)).addClass('active');
        this.applyToolInitialization(tool);
        if (tool && tool.name) {
            webgis.variableContent.set('tool-name', tool.name.replaceAll('/', '<br/>'));
        }

        //if (!this.isDefaultTool(tool)) {
        //    webgis.setHistoryItem($(this.elem).find('#webgis-toolhistory-button'));
        //} else {
        //    webgis.removeHistoryItem($(this.elem).find('#webgis-toolhistory-button'));
        //}

        this.refreshSnapping();
    };
    this.getActiveTool = function () {
        if (this._activeTool == null)
            return this.getDefaultTool();
        return this._activeTool;
    };
    this.isActiveTool = function (toolid) {
        return (this._activeTool !== null && this._activeTool.id == toolid);
    };
    this.setActiveToolType = function (tooltype) {
        if (this._activeTool) {
            if (this._activeTool != null && this._activeTool.tooltype == 'box')
                this.touchEventHandlers.unbindBoxEvents();
            this._activeTool.tooltype = tooltype;
            this.setActiveTool(this._activeTool);
        }
    };
    this.isSketchTool = function (tool) {
        if (!tool)
            return false;

        return $.inArray(tool.tooltype, ['sketch0d', 'sketch1d', 'sketch2d', 'sketchany', 'sketchcircle']) >= 0;
    };
    this.isGraphicsTool = function (tool) {
        if (!tool)
            return false;

        return $.inArray(tool.tooltype, ['graphics']) >= 0;
    };
    this.isCustomTool = function (tool) {
        if (!tool)
            return false;

        return tool.type === 'customtool';
    };
    this.currentToolSketches = function () {
        var result = [];
        for (var t in this._tools) {
            var tool = this._tools[t];
            if (this.isSketchTool(tool) && tool.currentSketch && tool.currentSketch.vertices && tool.currentSketch.vertices.length > 0) {
                result.push({
                    id: tool.id,
                    name: tool.name,
                    storedSketch: tool.currentSketch
                });
            }
        }
        return result;
    };
    this.getToolId = function (tool) {
        if (!tool)
            return '';
        return (typeof tool === 'string' || tool instanceof String) ? tool : tool.id;
    };
    this._defaultToolId = 'webgis.tools.identify';
    this.setDefaultToolId = function (id) {
        this._defaultToolId = webgis.compatiblity.toolId(id);
    };
    this.getDefaultToolId = function () {
        return this._defaultToolId;
    };
    this.getDefaultTool = function () {
        return this.getTool(webgis._defaultToolPrefix + this._defaultToolId);
    };
    this.isDefaultTool = function (tool) {
        return tool && tool.id === webgis._defaultToolPrefix + this._defaultToolId;
    }
    this.setParentTool = function () {
        var tool = this.getActiveTool();
        if (!tool)
            return;

        var parentTool = tool.parentTool;
        var defaultTool = this.getDefaultTool();

        if (tool.id === defaultTool.id)
            return;

        if (parentTool) {
            webgis.tools.onButtonClick(this, parentTool);
        } else {
            webgis.tools.onButtonClick(this, defaultTool);
            $(tool.uiElement).webgis_uibuilder('empty', { map: this }).webgis_toolbox({ map: this });
        }
    };
    this.setToolOptions = function (tool, options) {
        if (tool)
            tool.options = options;
    };
    this._toolPersistence = [];
    this.setPersistentToolParameter = function (tool, paramId, val, prefix) {

        //if (val === null) {
        //    console.trace('set val to NULL');
        //}

        if (!tool)
            return;
        var context = (typeof tool === 'string' || tool instanceof String) ?
            tool :
            tool.persistencecontext || tool.id;
        if (prefix) context = prefix + ":" + context;

        if (!this._toolPersistence[context])
            this._toolPersistence[context] = [];
        this._toolPersistence[context][paramId] = val;
    };
    this.getPersistentToolParameter = function (tool, paramId, prefix) {
        var params = this.getPersistentToolParameters(tool, prefix);
        return params[paramId];
    };
    this.getPersistentToolParameters = function (tool, prefix) {
        if (!tool)
            return [];
        var context = (typeof tool === 'string' || tool instanceof String) ?
            tool :
            tool.persistencecontext || tool.id;

        if (prefix) context = prefix + ":" + context;
        var params = this._toolPersistence[context];

        return params || [];
    };
    this.applyPersistentToolParameters = function (tool, callbackAfterChanged) {
        let params = this.getPersistentToolParameters(tool);
        //console.log('persistant parameters: ', params)
        for (let id in params) {
            let $e = $('#' + id);
            if ($e.hasClass('webgis-tool-parameter-persistent') || true) {
                $e.val(params[id]);

                if (callbackAfterChanged) {
                    callbackAfterChanged($e);
                }
            }
        }
        params = this.getPersistentToolParameters(tool, "force-set");  // force this values. They come eg from URL Parameters (ed_EDITFIELD, ...). Should also be set, if the editfield is not an resistant field
        //console.log('persistant parameters, force-set', params)
        for (let id in params) {
            let $e = $('#' + id);
            if ($e.length > 0) {

                //console.log('set an force-set parameter', id, params[id], $e.length)
                $e.val(params[id]);

                if (callbackAfterChanged) {
                    callbackAfterChanged($e);
                }
            }
        }
    };
    this.applyToolInitialization = function (tool) {
        var params = this.getPersistentToolParameters(tool);
        //console.log(params);
        var initializationRequired = false;
        $('.webgis-tool-initialization-parameter').each(function (i, e) {
            var $elem = $(e);
            var id = e.id;
            if (!params[id] && $elem.val()) {
                params[id] = $elem.val();
                initializationRequired = true;
            }
            else if ($elem.hasClass('webgis-tool-initialization-parameter-important')) {
                initializationRequired = true;
            }
        });
        //console.log(initializationRequired);
        //console.log(tool);
        if (initializationRequired && !tool._isInitialized) {
            tool._isInitialized = true;
            //console.log('init tool:' + tool.id);
            var type = this.isServerButton(tool.id) ? 'servertoolcommand_ext' : 'servertoolcommand';
            webgis.tools.onButtonClick(this, {
                command: 'init', type: type, id: tool.id, map: this
            });
        }
    };
    this._toolEvents = [];
    this.addToolEvents = function (events) {
        if (!events)
            return;
        for (var e in events) {
            this._toolEvents.push(events[e]);
        }
        ;
    };
    this.fireToolEvent = function (event) {
        this.ui.refreshUIElements();
        var command = '', activeTool = this.getActiveTool();
        for (var e in this._toolEvents) {
            if (this._toolEvents[e].event == event) {
                if (command != '')
                    command += ",";
                command += this._toolEvents[e].command;
            }
        }
        if (command != '' && activeTool) {
            webgis.tools.onButtonClick(this, {
                command: command, type: 'servertoolcommand', id: activeTool.id, map: this
            });
        }
    };
    this.executeTool = function (id) {
        var tool = this._tools[id];
        if (!tool)
            tool = this._tools[webgis.tools.getToolId(id)];
        if (tool) {
            webgis.tools.onButtonClick(this, tool);
            return true;
        }
        return false;
    };
    this.toolSketch = function () {
        if (!this._activeTool || !this._activeTool.tooltype)
            return null;
        if (this._activeTool && this._activeTool.tooltype == 'graphics') {
            if (this.graphics.getTool() != 'pointer')
                return this.graphics._sketch;
        }
        if (this._activeTool && this._activeTool.tooltype.indexOf('sketch') == 0)
            return this.sketch;

        return null;
    };
    this._enableBBoxToolHandling = function () {
        if (webgis.mapFramework === "leaflet" && this.frameworkElement) {
            this.frameworkElement.dragging.disable();
            this.frameworkElement.boxZoom.disable();
            this.frameworkElement.keyboard.disable();
            this.touchEventHandlers.bindBoxEvents();
        }
    };
    this._disableBBoxToolHandling = function () {
        if (webgis.mapFramework === "leaflet" && this.frameworkElement) {
            this.frameworkElement.dragging.enable();
            this.frameworkElement.touchZoom.enable();
            this.frameworkElement.doubleClickZoom.enable();
            this.frameworkElement.scrollWheelZoom.enable();
            this.frameworkElement.boxZoom.enable();
            this.frameworkElement.keyboard.enable();
            $(this.elem).closest('.webgis-container').find('.leaflet-control-container, .webgis-tool-button-bar').css('display', '');
        }
    };
    this._enableSketchClickHandling = function () {
        if (webgis.mapFramework === "leaflet") {
            try {
                this.frameworkElement.dragging._draggable.options.clickTolerance = webgis.usability.mapSketchClickTolerance;
            } catch (e) { }
        }
    };
    this._disableSketchClickHandling = function () {
        if (webgis.mapFramework === "leaflet") {
            try {
                this.frameworkElement.dragging._draggable.options.clickTolerance = webgis.usability.mapClickTolerance;
            } catch (e) { }
        }
    };
    this._mapToolsToServerContainers = function (containers) {

        //
        //  Hier werden bestehende Tools in die Container übertragen die Serverseitig im C# definiert wurden.
        // Ansonsten bleiben diese immer so wie beim Erstellen einer Karte mit dem MapBuilder(zB Speicher und Laden => "Karte")
        //

        //console.log('_mapToolsToServerContainers', containers);
        let serverContainers = [];

        //console.log('containers', containers);

        for (let c in containers) {
            let container = containers[c];
            if (!container.tools || container.tools.length === 0) {
                if (container.options && container.options.length > 0) {  // Tool Options übernehmen: zB Drucklayouts, ...
                    serverContainers.push(container);
                }
                continue;
            }

            for (let t in container.tools) {
                const tool = this._tools[container.tools[t]];
                if (!tool)
                    continue;

                // if tool.container is an array => take arry otherwise [ tool.container ]
                // so a tool can be in multiple containers (eg, Start and Query)
                const currentToolContainers = Array.isArray(tool.container) ? tool.container : [tool.container];    

                for (let currentToolContainer of currentToolContainers) {
                    let toolContainer = $.firstOrDefault($.grep(serverContainers, function (c) { return c.name === currentToolContainer; }));

                    if (!toolContainer) {
                        toolContainer = { name: currentToolContainer, tools: [] };
                        serverContainers.push(toolContainer);
                    }

                    toolContainer.tools.push(container.tools[t]);
                }
            }
        }

        //console.log('serverContainers', serverContainers);

        var toolContainerOrder = webgis.usability.toolContainerOrder || [];
        if (toolContainerOrder.length > 0) { 
            // sort serverContainers by name using an existing array toolContainerOrder
            // serverContinaer witch is not in the list => order at the end...
            serverContainers.sort(function (a, b) {
                var indexA = toolContainerOrder.indexOf(a.name);
                var indexB = toolContainerOrder.indexOf(b.name);

                // If both containers are in the toolContainerOrder array, sort by their order
                if (indexA >= 0 && indexB >= 0) {
                    return indexA - indexB;
                }
                // If only a is in the array, it comes first
                else if (indexA >= 0) {
                    return -1;
                }
                // If only b is in the array, it comes first
                else if (indexB >= 0) {
                    return 1;
                }
                // If neither is in the array, maintain their relative order
                return 0;
            });
        }

        return serverContainers;
    };
    this._refreshToolBubbles = function (tool, forceShowClickBubble, forceShowContextMenuBubble) {
        tool = tool || this.getActiveTool();

        if (webgis.usability.clickBubble === true &&
            tool &&
            tool.tooltype &&
            ((tool.tooltype.indexOf('sketch') === 0 && this.sketch.isEditable()) ||
                tool.tooltype === 'click' ||
                forceShowClickBubble === true)) {
            this.ui.addClickBubble();
        }
        else {
            this.ui.removeClickBubble();
        }

        if (webgis.usability.contextMenuBubble === true &&
            tool &&
            tool.tooltype &&
            ((tool.tooltype.indexOf('sketch') === 0 && this.sketch.isEditable()) ||
                forceShowContextMenuBubble === true)) {
            this.ui.addContextMenuBubble();
        }
        else {
            this.ui.removeContextMenuBubble();
        }

        //if (webgis.currentPosition.useWithSketchTool === true && tool && tool.tooltype && tool.tooltype.indexOf('sketch') === 0 && this.sketch.isEditable()) {
        if (webgis.currentPosition.canUsedWithSketchTool && webgis.currentPosition.canUsedWithSketchTool(tool, this.sketch)) {
            this.ui.removeAllQuickAccessButtons();
            var $button = this.ui.addQuickAccessButton({
                bgImage: webgis.css.img('edit-26-w.png'),
                text: 'GPS',
                textClass: 'gps-accuracy',
                addClass: 'webgis-gps',
                onClick: function (e) {
                    var current = webgis.continuousPosition.current;
                    //console.log(current);
                    if (current && current.status === 'ok') {
                        var map = $(this).data('map');
                        map.sketch.addVertex({ x: current.lng, y: current.lat, m: current.acc || null });
                    }
                },
                onLongClick: function () {
                    webgis.continuousPosition.showInfo();
                },
                onEnable: function (e) {
                    var map = $(this).data('map');
                    console.log('start watching');
                    webgis.continuousPosition.start(map, {
                        marker: 'currentpos_red',
                        marker_ok: 'currentpos_green',
                        minAcc: webgis.currentPosition.minAcc,
                        maxAgeSeconds: webgis.currentPosition.maxAgeSeconds
                    });
                    if (webgis.usability.clickBubble === true)
                        map.ui.removeClickBubble();
                },
                onDisable: function (e) {
                    var map = $(this).data('map');
                    //console.log('disable watching');
                    webgis.continuousPosition.stop();
                    if (webgis.usability.clickBubble === true)
                        map.ui.addClickBubble();
                },
                helpName: 'gps-sketch',
                dragTolerance: 10
            });
            $button.addClass('webgis-gps-acc-dependent');
        } else {
            this.ui.removeAllQuickAccessButtons();
        }
    };
    /****** Draggable Marker ******/
    this._draggableClickMarker = null;
    this.showDraggableClickMarker = function (lat, lng, txt) {
        if (webgis.mapFramework == "leaflet") {
            if (this._draggableClickMarker != null)
                this.frameworkElement.removeLayer(this._draggableClickMarker);
            this._draggableClickMarker = webgis.createMarker({
                lat: lat, lng: lng, draggable: true, icon: (this.getActiveTool()) ? this.getActiveTool().marker : null
            });
            this._draggableClickMarker.addTo(this.frameworkElement);
            this._draggableClickMarker.on('dragend', function (e) {
                if (webgis.tools) {
                    var latlng = this._draggableClickMarker.getLatLng();
                    webgis.tools.onMapClick(this, {
                        latlng: latlng, containerPoint: { x: 0, y: 0 }, originalEvent: null
                    });
                }
            }, this);
        }
    };
    this.hideDraggableClickMarker = function () {
        if (this._draggableClickMarker == null)
            return;
        if (webgis.mapFramework == "leaflet") {
            this.frameworkElement.removeLayer(this._draggableClickMarker);
        }
        this._draggableClickMarker == null;
    };
    this.setDraggableMarkerPosition = function (lat, lng, txt, openPopup) {
        if (webgis.mapFramework == "leaflet") {
            if (!this._draggableClickMarker)
                this.showDraggableClickMarker(lat, lng);
            else
                this._draggableClickMarker.setLatLng({
                    lat: lat, lng: lng
                });
            if (txt) {
                var popup = this._draggableClickMarker.bindPopup(txt);
                if (openPopup)
                    popup.openPopup();
            }
        }
    };
    /****** Marker *******/
    var _tempMarker = null;
    this.addMarker = function (options) {
        //alert(lat + "\n" + lng);
        if (webgis.mapFramework === "leaflet") {
            var marker = webgis.createMarker({
                lat: options.lat,
                lng: options.lng,
                icon: options.icon,
                angle: options.angle,
                draggable: options.draggable || false
            });
            marker.addTo(this.frameworkElement);
            this.addMarkerPopup(marker, options);
            return marker;
        }
    };
    this.addMarkerPopup = function (marker, options) {
        var txt = options.text || '';
        if (options.buttons) {
            for (var b in options.buttons) {
                var button = options.buttons[b];
                button._id = webgis.guid();
                txt += "<button class='webgis-button' id='" + button._id + "' style='margin-right:4px' onclick='webgis._handleMarkerBubbleButtonClick(this)'>" + button.label + "</button>";
            }
        }
        if (txt) {
            var popup = marker.bindPopup(txt);
            popup.openPopup();

            if (options.buttons) {
                for (var b in options.buttons) {
                    var button = options.buttons[b];
                    var btnElement = $('#' + button._id).get(0);
                    webgis._appendMarkerBubbleButtonClickEvent(btnElement, this, marker, button.onclick);
                    //btnElement._map = this;
                    //btnElement._marker = marker;
                    //btnElement._onclick = button.onclick;
                    //$(btnElement).click(function () {
                    //    console.log('click');
                    //    console.log(this._map);
                    //    this._onclick(this._map, this._marker);
                    //});
                }
            }
            if (!options.openPopup)
                popup.closePopup();
        }
    };
    this.removeMarker = function (marker) {
        if (!marker)
            return;
        if (webgis.mapFramework == "leaflet") {
            this.frameworkElement.removeLayer(marker);
        }
        webgis._removeMarkerBubbleEvents(marker);
    };
    this._markerGroups = [];
    this.toMarkerGroup = function (groupName, marker) {
        if (!marker)
            return;
        if (!this._markerGroups[groupName])
            this._markerGroups[groupName] = [];
        this._markerGroups[groupName].push(marker);
    };
    this.removeMarkerGroup = function (groupName) {
        if (!this._markerGroups[groupName])
            return;
        for (var i in this._markerGroups[groupName]) {
            var marker = this._markerGroups[groupName][i];
            this.removeMarker(marker);
        }
        this._markerGroups[groupName] = null;
    };
    this.showTempMarker = function (pos, text) {
        if (!_tempMarker) {
            _tempMarker = this.addMarker({
                lat: pos.y, lng: pos.x, icon: 'currentpos_red'
            });
        }
        if (_tempMarker.setLatLng)
            _tempMarker.setLatLng({
                lat: pos.y, lng: pos.x
            });
    };
    this.removeTempMarker = function () {
        this.removeMarker(_tempMarker);
        _tempMarker = null;
    };
    /****** Basemap ******/
    this.setBasemap = function (serviceId, overlay, fireLiveshareEvent) {
        if (this._baseMaps.current != null && this._baseMaps.current.id == serviceId) {
            return;
        }

        var service = this.getBasemap(!overlay ? serviceId : (this._baseMaps.current ? this._baseMaps.current.id : ''));
        var overlayService = overlay ? this.getBasemap(serviceId) : null;
        var basemapTypes = ['tile', 'vtc', 'custom'];

        if (this._baseMaps.current != null) {
            if (webgis.mapFramework === "leaflet" && $.inArray(this._baseMaps.current.serviceInfo.type, basemapTypes) >= 0) {
                this.frameworkElement.removeLayer(this._baseMaps.current.frameworkElement);
                this._baseMaps.current._fireCustomOnRemove();
            }
            this._baseMaps.current.setLayerVisibility(["*"], false, fireLiveshareEvent);
            this._baseMaps.current = null;
        }
        if (service) {
            if (webgis.mapFramework === "leaflet" && $.inArray(service.serviceInfo.type, basemapTypes) >= 0) {
                this.frameworkElement.addLayer(service.frameworkElement);
                service._fireCustomOnAdd();
            }
            this._baseMaps.current = service;
            service.setLayerVisibility(["*"], true, fireLiveshareEvent);
        }
        if (overlayService) {
            if (this._baseMaps.currentOverlays.includes(overlayService)) {  // if included: turn off
                if (webgis.mapFramework === "leaflet" && $.inArray(overlayService.serviceInfo.type, basemapTypes) >= 0) {
                    this.frameworkElement.removeLayer(overlayService.frameworkElement);
                    overlayService._fireCustomOnRemove();
                }
                overlayService.setLayerVisibility(["*"], false);
                this._baseMaps.currentOverlays = this._baseMaps.currentOverlays.filter((s) => s != overlayService);
            } else {   // otherwise: turn on
                if (webgis.mapFramework === "leaflet" && $.inArray(overlayService.serviceInfo.type, basemapTypes) >= 0) {
                    this.frameworkElement.addLayer(overlayService.frameworkElement);
                    overlayService._fireCustomOnAdd();
                }
                this._baseMaps.currentOverlays.push(overlayService);
                if (service) {
                    overlayService.setOpacity(service.getOpacity());
                }
                overlayService.setLayerVisibility(["*"], true, fireLiveshareEvent);
            }
        }
        if (service || overlayService) {
            this.events.fire('basemap_changed', this, this._baseMaps.current);
        } else {
            this.events.fire('basemap_changed', this, null);
        }
    };
    this.currentBasemapServiceId = function () {
        if (this._baseMaps.current == null)
            return '';
        return this._baseMaps.current.id;
    };
    this.currentBasemapOverlayServiceIds = function () {
        if (this._baseMaps.currentOverlays == null)
            return [];

        return this._baseMaps.currentOverlays.map((s) => s.id);
    };
    this.getBasemap = function (serviceId) {
        if (!this._baseMaps.services)
            return null;
        for (var id in this._baseMaps.services) {
            if (id == serviceId)
                return this._baseMaps.services[id];
        }
    };
    /***** Dependencies ****/
    this.hasServices = function () {
        return this.serviceIds().length > 0;
    };
    this.hasServiceQueries = function () {
        for (var serviceId in this.services) {
            if (this.services[serviceId] != null && this.services[serviceId].queries != null && this.services[serviceId].queries.length > 0)
                return true;
        }
        return false;
    };
    this.hasQueryResults = function () {
        return this.queryResultFeatures.count() > 0;
    };
    this.hasServiceEditthemes = function () {
        for (var serviceId in this.services) {
            if (this.services[serviceId] != null && this.services[serviceId].editthemes != null && this.services[serviceId].editthemes.length > 0)
                return true;
        }
        return false;
    };
    this.hasServiceChainagethemes = function () {
        for (var serviceId in this.services) {
            if (this.services[serviceId] != null && this.services[serviceId].chainagethemes != null && this.services[serviceId].chainagethemes.length > 0)
                return true;
        }
        return false;
    };

    /***** Dynamic Content ******/
    this.dynamicContent = [];
    this._currentDynamicContent = null;
    this._serializeDynamicContent = function (content) {
        var result = {
            id: content.id,
            name: content.name,
            url: content.url,
            type: content.type,
            img: content.img || null,
            autoZoom: !(content.autoZoom === false) && content.extentDependend !== true,
            suppressShowResult: content.showResult === false || content.suppressShowResult === true,
            suppressAutoLoad: content.autoLoad === false || content.suppressAutoLoad === true,
            extentDependend: content.extentDependend === true,
            featureCollection: content.featureCollection
        };

        if (content.fromTocTheme === true) {
            result.fromTocTheme = true;
        }
        return result;
    };
    this.addDynamicContent = function (contentItems, loadFirst) {
        //console.log('addDynamicContent', contentItems);
        if (contentItems == null || contentItems.length === 0)
            return;

        var first2load = null;
        for (var i in contentItems) {
            var content = this._serializeDynamicContent(contentItems[i]);
            if (this.getDynamicContent(content.id) != null)
                continue;

            if (!first2load && !content.suppressAutoLoad) {
                first2load = content;
            }
            content.map = this;
            this.dynamicContent.push(content);
            this.events.fire('onadddynamiccontent', content);
        }
        if (loadFirst && first2load) {
            this.loadDynamicContent(first2load);
        }
    };
    this.getDynamicContent = function (id) {
        for (var i in this.dynamicContent) {
            var dynamicContent = this.dynamicContent[i];
            if (dynamicContent.id === id)
                return dynamicContent;
        }
        return null;
    };
    this.getCurrentDynamicContentName = function () {
        return this._currentDynamicContent ? this._currentDynamicContent.name : null;
    };
    this.getCurrentDynamicContentId = function () {
        return this._currentDynamicContent ? this._currentDynamicContent.id : null;
    };
    this.loadDynamicContent = function (contentDef) {
        this._currentDynamicContent = contentDef;

        this.events.fire('onloaddynamiccontent');

        if (contentDef) {
            if (contentDef.type === "feature-collection") {
                if (contentDef.featureCollection) {
                    this.events.fire('onnewdynamiccontentloaded');

                    contentDef.featureCollection.metadata = contentDef.featureCollection.metadata || {};
                    contentDef.featureCollection.metadata.dynamicContentDef = contentDef;

                    if (this.ui.appliesQueryResultsUI())
                        this.ui.showQueryResults(contentDef.featureCollection, true, contentDef);
                }
            } else {
                var map = this, params = '?';

                var data = {
                    type: contentDef.type, url: contentDef.url
                };

                if (contentDef.extentDependend) {
                    if (this.viewLense.isActive() && this.viewLense.currentExtent()) {
                        params += "_bbox=" + this.viewLense.currentExtent() + "&_bbox_rotation=" + this.viewLense.currentRotation() + "&_limit=2500&_scale=" + this.viewLense.currentScale();
                    } else {
                        params += "_bbox=" + this.getExtent().toString() + "&_limit=2500&_scale=" + this.scale();
                    }
                };

                webgis.ajax({
                    progress: 'Lade: ' + contentDef.name,
                    url: webgis.baseUrl + '/rest/DynamicContentProxy' + params,
                    data: webgis.hmac.appendHMACData(this.appendRequestParameters(data)),
                    type: 'post',
                    success: function (result) {
                        //console.log(result);
                        result.isDynamicContent = true;

                        var responseWarnings = [], responseInfos = [];
                        if (result.metadata) {
                            if (result.metadata.warnings) {
                                for (var w in result.metadata.warnings) {
                                    responseWarnings.push(result.metadata.warnings[w]);
                                }
                            }
                            if (result.metadata.infos) {
                                for (var i in result.metadata.infos) {
                                    responseInfos.push(result.metadata.infos[i]);
                                }
                            }
                        }

                        if (!result || !result.features || result.features.length === 0) {
                            if (map._currentDynamicContent && map._currentDynamicContent.extentDependend === true) {
                                if (map.queryResultFeatures.countFeatures() > 0) {
                                    var removeButton = $(map._webgisContainer).closest('.webgis-container').find('.webgis-toolbox-tool-item.remove-queryresults');
                                    if (removeButton.length > 0) {
                                        removeButton.click();
                                    } else {
                                        map.queryResultFeatures.clear(false);
                                        map.getSelection('selection').remove();
                                    }
                                }
                                map.ui.selectDynamicContentItem( contentDef);
                            } else {
                                map.refresh();
                                webgis.alert("Es wurden keine Ergebnisse zum dynamischen Inhalt \"" + contentDef.name + "\" gefunden", "info");
                            }
                        } else {
                            var contentLoadedHook = webgis.hooks["dynamic_content_loaded"][contentDef.name] || webgis.hooks["dynamic_content_loaded"]["default"];
                            var featureLoadedHook = webgis.hooks["dynamic_content_feature_loaded"][contentDef.name] || webgis.hooks["dynamic_content_feature_loaded"]["default"];

                            if (typeof contentLoadedHook === 'function')
                                contentLoadedHook(result);

                            for (var f in result.features) {
                                var feature = result.features[f];
                                if (!feature)
                                    continue;

                                if (typeof featureLoadedHook === 'function')
                                    featureLoadedHook(feature);

                                if (!feature.oid) {
                                    feature.oid = webgis.guid();
                                }
                                if (!feature.bounds) {
                                    if (feature.geometry && feature.geometry.type === 'Point' && feature.geometry.coordinates && feature.geometry.coordinates.length >= 2) {
                                        feature.bounds = [feature.geometry.coordinates[0], feature.geometry.coordinates[1], feature.geometry.coordinates[0], feature.geometry.coordinates[1]];
                                    }
                                }
                            }

                            //console.log('contentDef', contentDef);
                            map.events.fire('onnewdynamiccontentloaded');

                            result.metadata = result.metadata || {};
                            result.metadata.dynamicContentDef = contentDef;

                            if (map.ui.appliesQueryResultsUI())
                                map.ui.showQueryResults(result, contentDef.autoZoom != false, contentDef);

                        }

                        if (responseInfos.length > 0) {
                            var infoText = '';
                            for (var i in responseInfos)
                                infoText += (infoText ? ', ' : '') + responseInfos[i];

                            webgis.toastMessage("Info", infoText);
                        }
                        if (responseWarnings.length > 0) {
                            var infoText = '';
                            for (var i in responseWarnings)
                                infoText += (infoText ? ', ' : '') + responseWarnings[i];

                            webgis.toastMessage("Warning", infoText, null, 'warning');
                        }
                    },
                    error: function () {
                    }
                });
            }
        }
    };
    this.unloadDynamicContent = function () { this.loadDynamicContent(null); };
    this.isCurrentDynamicContentExtentDependent = function () {
        return (this._currentDynamicContent && this._currentDynamicContent.extentDependend === true) ? true : false;
    };
    this.isCurrentDynamicContentFromTocTheme = function () {
        return (this._currentDynamicContent && this._currentDynamicContent.fromTocTheme === true) ? true : false;
    };
    this.hasCurrentDynamicContent = function () {
        return this._currentDynamicContent ? true : false;
    };
    this.getCurrentDynamicContent = function () {
        return this._currentDynamicContent;
    };
    /***** Filters ****/
    this._visfilters = [];
    this.setFilter = function (filterId, args) {
        //console.log('setfilter', filterId, args);
        this.unsetFilter(filterId);
        this._visfilters.push({
            id: filterId, args: args
        });
    };
    this.unsetFilter = function (filterId) {
        filterId = filterId || '';

        var visfilters = [];
        for (var f in this._visfilters) {
            var visfilter = this._visfilters[f];
            if (visfilter.id === filterId || visfilter.id.indexOf('~' + filterId) > 0 || filterId.indexOf('~' + visfilter.id) > 0 || filterId == '')
                continue;

            visfilters.push(visfilter);
        }
        this._visfilters = visfilters;
    };
    this.hasFilter = function (filterId) {
        if (!filterId) {
            return false;
        }
        for (var f in this._visfilters) {
            var visfilter = this._visfilters[f];
            //console.log(filterId, visfilter);
            if (visfilter.id === filterId || visfilter.id.indexOf('~' + filterId) > 0 || filterId.indexOf('~' + visfilter.id) > 0) {
                return true;
            }
        }
        return false;
    };
    this.hasFilters = function () {
        return this._visfilters && this._visfilters.length > 0;
    };
    this.unsetAllFilters = function () {
        this._visfilters = [];
    };
    /***** Labeling ****/
    this._labels = [];
    this.setLabeling = function (labeling) {
        this.unsetLabeling(labeling);
        this._labels.push(labeling);
    };
    this.unsetLabeling = function (labelingId) {
        if (!labelingId)
            labelingId = '';
        labelingId = (typeof labelingId === "string") ? labelingId : labelingId.id;
        var labels = [];
        for (var l in this._labels) {
            var label = this._labels[l];
            if (label.id == labelingId || label.id.indexOf('~' + labelingId) > 0 || labelingId == '')
                continue;
            labels.push(label);
        }
        this._labels = labels;
    };
    this.hasLabeling = function (labelingId) {
        for (var l in this._labels) {
            if (this._labels[l].id == labelingId)
                return true;
        }
        return false;
    };
    /***** Request Parameters ****/
    this.appendRequestParameters = function (data, serviceId) {
        //console.log("appendRequestParameters: " + serviceId);
        if (data == null)
            return;
        // Filters
        var serviceFilters = [];
        for (var f in this._visfilters) {
            var visfilter = this._visfilters[f];
            if (serviceId && visfilter.id.indexOf('~') > 0 && visfilter.id.indexOf(serviceId + '~') < 0)
                continue;
            serviceFilters.push(visfilter);
        }
        if (serviceFilters.length > 0)
            data.filters = JSON.stringify(serviceFilters);
        // Labeling
        var labels = [];
        for (var l in this._labels) {
            var label = this._labels[l];
            if (serviceId && label.id.indexOf('~') > 0 && label.id.indexOf(serviceId + '~') < 0)
                continue;
            labels.push(label);
        }
        if (labels.length > 0)
            data.labels = JSON.stringify(labels);

        if (!data.dpi && _dpi > 0) {
            data.dpi = _dpi;
        }
        return data;
    };
    /***** Chart *****/
    this.showChart = function (chartDef) {
        var me = this;
        this.ui.webgisContainer().webgis_dockPanel({
            title: '',
            dock: 'bottom',
            refElement: this.ui.mapContainer(),
            map: this,
            onload: function ($content) {
                $content.css({
                    overflow: 'hidden',
                    margin: 10
                });
                /************************************** Gregor **********************************************************/
                var chart = new webgis.chart(me);
                chart.init({
                    data: chartDef.data,
                    grid: chartDef.grid,
                    axis: chartDef.axis,
                    point: chartDef.point
                }, function () {
                    if (chartDef.sketchconnected == true) {
                        chart.bindSketch(me.sketch, chartDef.data);
                    }
                    chart.create({
                        target: $content.get(0)
                    });
                });
                /************************************************************************************************/
            },
            size: '50%'
        });
    };
    /****** Print *****/
    this.print = function (options, callback) {
        var graphicsJson = JSON.stringify(this.graphics.toGeoJson(), null, 2);
        var mapJson = JSON.stringify(this.serialize({ ui: false, applyFallback: true, visibleBasemapsOnly: true }), null, 2);

        console.log('printoptions', options);

        if (!this.queryResultFeatures.hasTableProperties()) {
            options.attachQueryResults = '';
        }
        var data = {
            graphics: graphicsJson,
            map: mapJson,
            queryResultFeatures: options.showQueryMarkers === true || options.attachQueryResults ?
                JSON.stringify(this.queryResultFeatures._queryResultFeatures, null, 2) :
                null,
            coordinateResultFeatures: options.showCoordinateMarkers === true || options.attachCoordinates ?
                JSON.stringify(this.queryResultFeatures._toolQueryResultFeatuers["webgis.tools.coordinates"], null, 2) :
                null,
            chainageResultFeatures: options.showChainageMarkers === true || options.attachChainage ?
                JSON.stringify(this.queryResultFeatures._toolQueryResultFeatuers["webgis.tools.chainage"], null, 2) :
                null,
        };

        if (options.showSketch === true && this.sketch && this.sketch.isValid()) {
            var sketchCoords = this.sketch.getCoords();
            var calcCrsId = this.calcCrs(sketchCoords) ?
                this.calcCrs(sketchCoords).id : webgis.calc.getCalcCrsId(sketchCoords);

            data._calcSrs = calcCrsId > 0 ? calcCrsId : (this.calcCrs() ? this.calcCrs().id : 0);

            data._sketchWgs84 = this.sketch.toWKT(true);
            data._calcsketch = this.sketch.toWKT();
            data._calcSketchSrs = (this.sketch.originalSrs() && this.sketch.originalSrs() !== 0 ? this.sketch.originalSrs() : calcCrsId);
            data._sketchLabels = options.showSketchLabels;
        }

        //console.log(data);

        if (options.dpi) {
            data.dpi = options.dpi;  // Sollte beim Drucken nicht in appendRequestParameters(data) überschriben werden!!! => darum hier setzen
        }

        if (webgis.globals && webgis.globals.portal) {
            data.eventMeta = JSON.stringify({ portal: webgis.globals.portal.pageId, cat: webgis.globals.portal.mapCategory, map: webgis.globals.portal.mapName || webgis.globals.portal.projectName });
        }

        this.addServicesCustomRequestParameters(data);

        webgis.ajax({
            type: 'post',
            url: webgis.baseUrl + '/rest/print',
            data: webgis.hmac.appendHMACData($.extend({}, options,
                this.appendRequestParameters(data))),
            success: function (result) {
                if (callback)
                    callback(result);
            },
            error: function () {
                if (callback)
                    callback(null);
            }
        });
    };
    this.showPrintTasks = function () {
        var data = webgis.tools.getToolData("webgis.tools.io.print");
        if (!data.prints)
            data.prints = [];
        $('body').webgis_modal({
            title: 'Druckaufträge',
            onload: function ($content) {
                for (var i = data.prints.length - 1; i >= 0; i--) {
                    //$("<a href=\"mailto:?subject=WebGIS Karte&attachments=['" + result.url + "']\">E-Mail versenden...</a>").appendTo($content);
                    var $tile = $("<div><img src='" + data.prints[i].preview + "' /></div>").css({
                        display: 'inline-block', cursor: 'pointer', border: '1px solid #aaa', margin: '2px'
                    }).appendTo($content);
                    if (data.prints[i].downloadid) {
                        if (data.prints[i].length) {
                            var length = data.prints[i].length, b = ' bytes';
                            if (length > 1000) {
                                length /= 1000;
                                b = ' KB';
                            }

                            if (length > 1000) {
                                length /= 1000;
                                b = ' MB';
                            }

                            $("<br/><span>" + Math.round(length * 100) / 100 + b + "</span>").appendTo($tile);
                        }
                        $tile.data('url', webgis.baseUrl + '/rest/download/' + data.prints[i].downloadid + (data.prints[i].n ? '?n=' + data.prints[i].n : ''));
                    }
                    else {
                        $tile.data('url', data.prints[i].url);
                    }
                    $tile.click(function () {
                        window.open($(this).data('url'));
                    });
                    //$("<div><span>" + data.prints[i].length + "</span></div>").addClass('webgis-icon-down').appendTo($tile);
                }
            }
            //width: 320
        });
    };
    this.downloadImage = function (options, callback) {
        var graphicsJson = JSON.stringify(this.graphics.toGeoJson(), null, 2);
        var serializedMap = this.serialize({ ui: false, applyFallback: true, visibleBasemapsOnly: true });
        if (options.useCustomServices) {
            serializedMap.services = options.useCustomServices;
        }
        var mapJson = JSON.stringify(serializedMap, null, 2);
        webgis.ajax({
            type: 'post',
            url: webgis.baseUrl + '/rest/downloadmapimage',
            data: webgis.hmac.appendHMACData($.extend({}, options, this.appendRequestParameters({ graphics: graphicsJson, map: mapJson }))),
            success: function (result) {
                if (callback)
                    callback(result);
            },
            error: function () {
                if (callback)
                    callback(null);
            }
        });
    };
    // Für externe Dienste (zB Hybrid Forms): options: { format: 'png' }
    this.downloadCurrentImage = function (options, callback) {

        options.bbox = this.getProjectedExtent().toString();
        options.size = this.getSize().toString();
        options.displaysize = screen.width + "," + screen.height;
        options.dpi = 96;
        options.worldfile = false;

        this.downloadImage(options, function (result) {
            if (callback) {
                if (result.downloadid && result.name) {
                    result.href = webgis.baseUrl + '/rest/download?id=' + result.downloadid + '&n=' + encodeURI(result.name);
                }
                callback(result);
            }
        });
    };

    /****** Snapping *****/
    this.construct = new webgis_construct(this);
    this.snapping = [];
    this.setSnapping = function (id, types) {
        this.snapping[id] = types;
    };
    this.clearSnapping = function () {
        this.snapping = [];
    };
    this.refreshSnapping = function () {
        if (!this.hasActiveSnappingInScale()) {
            this.construct.initSnapping(null);
            return;
        }
        var me = this;
        var data = {}, hasSnapping = false;
        for (var s in this.snapping) {
            var snapping = this.snapping[s];
            if (snapping != null && snapping.length > 0) {
                data[s] = snapping.toString();
                hasSnapping = true;
            }
        }

        // visfilters
        if (this._visfilters && this._visfilters.length > 0) {
            data.filters = JSON.stringify(this._visfilters);
        }

        if (hasSnapping) {
            data['scale'] = this.scale();
            data['bbox'] = this.getExtent().toString();
            data['calc_crs'] = this.calcCrs() ? this.calcCrs().id : '0';
            me._hourglassCounter.inc('Snapping');
            webgis.ajax({
                url: webgis.baseUrl + "/rest/snapping",
                data: webgis.hmac.appendHMACData(data),
                type: 'post',
                success: function (result) {
                    me.construct.initSnapping(result);
                    me._hourglassCounter.dec('Snapping');
                },
                error: function () {
                    me._hourglassCounter.dec('Snapping');
                }
                //progress: 'Load Snapping...'
            });
        }
    };
    this.getSnappingTypes = function (id) {
        return this.snapping[id];
    };
    this.hasActiveSnappingInScale = function () {
        if (this.toolSketch() == null)
            return false;
        var hasSnapping = false;
        for (var s in this.snapping) {
            var snapping = this.snapping[s];
            if (snapping != null && snapping.length > 0) {
                var serviceId = s.split('~')[0];
                var snappingId = s.split('~')[1];
                var service = this.getService(serviceId);
                if (service) {
                    var serviceSnapping = service.getSnapping(snappingId);
                    if (serviceSnapping && serviceSnapping.min_scale >= this.scale())
                        return true;
                }
            }
        }
        return false;
    };

    this._isEditingToolActive = function () {
        return this._activeTool && this._activeTool.id.indexOf('webgis.tools.editing.') === 0;
    }
    this._currentEditTheme = null;
    this.setCurrentEditTheme = function (editTheme) {
        this._currentEditTheme = editTheme;
    };
    this.getCurrentEditTheme = function () {
        if (!this._isEditingToolActive())
            return null;

        return this._currentEditTheme;
    };

    /****** CircleMarker ******/
    this._circleMarker = null;
    this._circleMarkerOptions = { pos: null, radius: 100 };
    this.showCircleMarker = function (pos, radius) {
        this.hideCircleMarker();
        if (webgis.mapFramework === "leaflet") {
            pos = pos || this._circleMarkerOptions.pos;
            radius = radius || this._circleMarkerOptions.radius;
            if (pos) {
                this._circleMarker = L.circle([pos[1], pos[0]], { radius: radius });
                this.frameworkElement.addLayer(this._circleMarker);
                this._circleMarkerOptions.pos = pos;
            }
            this._circleMarkerOptions.radius = radius;
        }
    };
    this.hideCircleMarker = function () {
        if (webgis.mapFramework == "leaflet") {
            if (this._circleMarker != null) {
                this.frameworkElement.removeLayer(this._circleMarker);
                this._circleMarker = null;
            }
        }
    };
    /****** Enable/Disable ******/
    this.enable = function (enable) {
        var $blocker = $(this._webgisContainer).find('.webgis-disable-blocker');
        if (enable == true) {
            $blocker.addClass('collapse');
        }
        else {
            if ($blocker.length == 0) {
                $blocker = $("<div>")
                    .addClass('webgis-disable-blocker')
                    .data('map', this)
                    .css({
                        position: 'absolute',
                    })
                    .appendTo($(this._webgisContainer))
                    .click(function () {
                        $(this).data('map').enable(false);
                    });
                $("<div>Zum Aktivieren der Karte hier klicken...</div>")
                    .addClass('button')
                    .appendTo($blocker)
                    .click(function (e) {
                        e.stopPropagation();
                        $(this).closest('.webgis-disable-blocker').data('map').enable(true);
                    });
            }
            $blocker.removeClass('collapse');
        }
        this.refreshDisableBlocker();
    };
    this.isEnabled = function () {
        var $blocker = $(this._webgisContainer).find('.webgis-disable-blocker');
        if ($blocker.length === 0 || $blocker.hasClass('collapsed')) {
            return true;
        }

        return false;
    };


    this._miniMapRequiresService = function (serviceType) {
        return this._miniMapAdded !== true && webgis.usability.appendMiniMap === true && this.servicesCountByType(serviceType) === 1;
    }

    this._miniMapAdded = false;
    this._addMiniMap = function (miniMapLayer) {
        if (this._miniMapAdded) return;

        this._miniMapAdded = true;
        var mapFrameworkElement = this.frameworkElement;

        webgis.require("leaflet-minimap", function () {
            var addMiniMapFunc = function (miniMapLayer) {
                try {  // kann fehler werden => besser minimap wird nich angezeigt, als es geht die Karte gar nix auf
                    var miniMap = new L.Control.MiniMap(miniMapLayer, webgis.usability.miniMapOptions || {}).addTo(mapFrameworkElement);
                } catch (ex) { }
            };
            // Minimap will beim Laden von Projekten in einem eigenen Thred laufen, sonst gibt exception... 
            // geht auch mit 0ms aber bissi verzögert schadet auch nix 
            webgis.delayed(function (miniMapLayer) {
                addMiniMapFunc(miniMapLayer);
            }, 1000, miniMapLayer)
        });
    };

    this.refreshDisableBlocker = function () {
        var $blocker = $(this._webgisContainer).find('.webgis-disable-blocker');
        if ($blocker.length > 0) {
            $blocker.css({
                left: $(this.elem).position().left,
                top: $(this.elem).position().top,
                width: $blocker.hasClass('collapse') ? '' : $(this.elem).width(),
                height: $blocker.hasClass('collapse') ? '' : $(this.elem).height(),
            });
        }
    };
    this._touchEventHandlers = function (map) {
        this._map = map;
        map.elem.map = map;
        this._onTouchStart = function (e) {
            try {
                var map = this.map;
                var origin = $(this).find('.leaflet-map-pane').offset(), x = /*e.offsetX ||*/ parseInt(e.touches[0].pageX - origin.left), y = /*e.offsetY ||*/ parseInt(e.touches[0].pageY - origin.top);
                var latLng = map.frameworkElement.layerPointToLatLng(L.point(x, y));

                if (map._activeTool != null && map._activeTool.tooltype === 'box') {

                    var bounds = L.latLngBounds(latLng, latLng);
                    map._toolBoxLayer = L.rectangle(bounds, { color: '#ff7800', weight: 2 });
                    map._toolBoxLayer.touchDownLatLng = latLng;
                    map.frameworkElement.addLayer(map._toolBoxLayer);
                }
                else if (webgis.tools) {
                    e.latlng = latLng;
                    e.originalEvent.button = 0;
                    webgis.tools.onMapMousedown(map, e);
                }
            } catch (ex) { /* do nothing */ }
        };
        this._onTouchMove = function (e) {
            try {
                var map = this.map;
                var origin = $(this).find('.leaflet-map-pane').offset(), x = /*e.offsetX ||*/ parseInt(e.touches[0].pageX - origin.left), y = /*e.offsetY ||*/ parseInt(e.touches[0].pageY - origin.top);
                var latLng = map.frameworkElement.layerPointToLatLng(L.point(x, y));

                if (map._activeTool != null && map._activeTool.tooltype === 'box') {
                    map._toolBoxLayer.setBounds(L.latLngBounds(map._toolBoxLayer.touchDownLatLng, latLng));
                } else if (!map.ui.useClickBubble()) {
                    e.latlng = latLng;
                    e.originalEvent.button = 0;
                    map.eventHandlers.move.apply(map, [e]);
                }
            } catch (ex) { /* do nothing */ }
        };
        this._onTouchEnd = function (e) {
            try {
                var map = this.map;
                var origin = $(this).find('.leaflet-map-pane').offset(), x = /*e.offsetX ||*/ parseInt(e.changedTouches[0].pageX - origin.left), y = /*e.offsetY ||*/ parseInt(e.changedTouches[0].pageY - origin.top);
                var latLng = map.frameworkElement.layerPointToLatLng(L.point(x, y));

                if (map._activeTool != null && map._activeTool.tooltype === 'box') {
                    var bounds = [map._toolBoxLayer.touchDownLatLng.lng, map._toolBoxLayer.touchDownLatLng.lat, latLng.lng, latLng.lat];
                    map.frameworkElement.removeLayer(map._toolBoxLayer);
                    map._toolBoxLayer = null;
                    if (map._activeTool.onbox)
                        map._activeTool.onbox(map, bounds);
                    else if (map._activeTool.type === 'servertool') {
                        var customEvent = [];
                        customEvent['_method'] = 'box';
                        webgis.tools.sendToolRequest(map, map._activeTool, 'servertoolcommand', new webgis.tools.boxEvent(map, e, bounds), customEvent);
                    }
                } else if (webgis.tools) {
                    e.latlng = latLng;
                    e.originalEvent.button = 0;
                    webgis.tools.onMapMouseup(map, e);
                }
            } catch (ex) { /* do nothing */ }
        };
        this._boxEventsEnabled = false;
        this.bindBoxEvents = function () {
            if (this._boxEventsEnabled == false && webgis.isTouchDevice()) {
                //if (window.navigator.msPointerEnabled) {
                //    this.elem.addEventListener('pointerdown', onTouchStart, false);
                //    this.elem.addEventListener('poindermove', onTouchMove, false);
                //    this.elem.addEventListener('pointerup', onTouchEnd, false);
                //} else {
                $(this._map.elem).on('touchstart', this._onTouchStart);
                $(this._map.elem).on('touchmove', this._onTouchMove);
                $(this._map.elem).on('touchend', this._onTouchEnd);
                //}
                this._boxEventsEnabled = true;
                //console.log('box_touch_events ... bind');
            }
        };
        this.unbindBoxEvents = function () {
            if (this._boxEventsEnabled == true && webgis.isTouchDevice()) {
                $(this._map.elem).off('touchstart', this._onTouchStart);
                $(this._map.elem).off('touchmove', this._onTouchMove);
                $(this._map.elem).off('touchend', this._onTouchEnd);
                this._boxEventsEnabled = false;
                //console.log('box_touch_events ... unbind');
            }
        };
    };
    this.touchEventHandlers = new this._touchEventHandlers(this);


    this.ui = new webgis.map._ui(this);
    this.graphics = new webgis.map._graphics(this);
    this.viewLense = new webgis.map._viewLense(this);
    this.eventHandlers = new webgis.map._eventHandlers(this);
};