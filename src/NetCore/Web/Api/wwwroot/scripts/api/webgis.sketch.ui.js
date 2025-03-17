webgis.sketch._ui = function (parent) {
    "use strict"
    var sketch = parent;

    var constructContextMenu = null;
    var suppressConstructionForms = false;
    var allowChangePresentation = true;

    var _isSimplePoint = function () { return sketch.getGeometryType() === 'point' };
    var _isLineOrPolygon = function () { return $.inArray(sketch.getGeometryType(), ["polyline", "polygon", "dimline", "hectoline"]) >= 0 };
    // dont allow multiparts with dimline or hectoline
    var _allowMultipart = function () { return $.inArray(sketch.getGeometryType(), ["polyline", "polygon"]) >= 0 };
    var _isLine = function () { return $.inArray(sketch.getGeometryType(), ["polyline", "dimline", "hectoline"]) >= 0 };
    var _isPolygon = function () { return sketch.getGeometryType() === "polygon" };

    this.SetAllowChangePresentation = function (value) {
        allowChangePresentation = value;
    };

    this.showContextMenu = function (clickEvent) {
        var map = sketch.map;

        var closestVertex = sketch._closestVertex(map, clickEvent.layerPoint);
        var closestEdge = closestVertex !== null && closestVertex.dist < 15 ? null : sketch._closestEdge(map, clickEvent.layerPoint);
        var hasSelectedVertices = sketch.hasSelectedVertices();

        var azimut = 0, deltaXY = [0, 0], dist = 0;
        if (sketch._moverLine) {
            var latLngs = sketch._moverLine.getLatLngs();
            if (latLngs.length >= 2) {
                azimut = map.construct.azimutFromWGS84(latLngs[0].lng, latLngs[0].lat, latLngs[1].lng, latLngs[1].lat);
                deltaXY = map.construct.deltaXYFromWGS84(latLngs[0].lng, latLngs[0].lat, latLngs[1].lng, latLngs[1].lat);
                dist = map.construct.distFromWGS84(latLngs[0].lng, latLngs[0].lat, latLngs[1].lng, latLngs[1].lat);
            }
        }

        var vertices = sketch._getRawVertices();

        if (constructContextMenu == null) {
            constructContextMenu = $("<div class='webgis-contextmenu webgis-tool-contextmenu icons' style='z-index:9999999;display:none;position:absolute;background:#fff' oncontextmenu='return false;'>")
                .data('map', map)
                .appendTo('body')
                .click(function () {
                    var map = $(this).css('display', 'none').data('map');
                    if (map && map.toolSketch()) {
                        map.toolSketch()._isConstructContextMenuVisible = false;
                        if (webgis.usability.contextMenuBubble === true)
                            map.toolSketch().onMapMouseMove($(this).css('display', 'none').data('map'), null);
                    }
                });
        }

        constructContextMenu.empty();
        var $menu = $("<ul>").addClass('webgis-toolbox-tool-item-group-details').css({
            padding: 0, margin: 0, minWidth: '200px', maxWidth:'100%'
        }).appendTo(constructContextMenu);

        var constructionMode = webgis.sketch.construct ? webgis.sketch.construct.getConstructionMode(sketch) : null;

        if (!constructionMode) {  // if in construction mode: let sketch._contextVertexIndex untouched
            if (closestVertex != null && closestVertex.dist < 15) {
                sketch._contextVertexIndex = closestVertex.index;
            } else {
                sketch._contextVertexIndex = null;
            }
        }

        if (constructionMode && map.calcCrs() && !suppressConstructionForms) {
            $("<li style='font-weight:bold'><i>Konstruktionsmodus: " + webgis.i18n.get("construction-mode-" + constructionMode) + "&nbsp;&nbsp;</i><div class='webgis-close-button'></div></li>")
                .appendTo($menu);

            $("<li>Konstruktionsmodus abbrechen</li>")
                .css('background-image', 'url(' + webgis.css.imgResource('sketch_remove_fix-26.png', 'tools') + ')')
                .appendTo($menu)
                .click(function () {
                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                    if (sketch) {
                        webgis.sketch.construct.cancel(sketch);
                    }
                });

            webgis.sketch.construct.createContextMenuUI(sketch, $menu);
        }
        else if (sketch._fixedDirectionVector && !sketch._fixedDistance && !_isSimplePoint() && !suppressConstructionForms) {
            $("<li style='font-weight:bold'><i>Konstruktionsmodus:</i><div class='webgis-close-button'></div></li>")
                .appendTo($menu);

            if (sketch.isInOrthoMode()) {
                if (_isPolygon()) {
                    $("<li>Polygon rechtwinkelig abschließen</li>")
                        .css('background-image', 'url(' + webgis.css.imgResource('sketch_ortho_close-26.png', 'tools') + ')')
                        .appendTo($menu)
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                            if (sketch) {
                                var partVertices = sketch.partVertices(sketch.partCount() - 1),
                                    pointCount = partVertices.length;

                                if (pointCount < 3) {
                                    webgis.alert("Polygonabschnitt muss aus mindestens drei Stützpunkten bestehen", "Error");
                                    return;
                                }

                                var result = webgis.calc.intersectionPoint(map.calcCrs(),
                                    [
                                        { x: partVertices[pointCount - 1].x, y: partVertices[pointCount - 1].y },
                                        { x: partVertices[pointCount - 2].x, y: partVertices[pointCount - 2].y }
                                    ],
                                    [
                                        { x: partVertices[0].x, y: partVertices[0].y },
                                        { x: partVertices[1].x, y: partVertices[1].y }
                                    ],
                                    'close_perpendicular');

                                //console.log('result', result);

                                if (result && Math.abs(result.D) > 1e-2) {
                                    //console.log("D", result.D);
                                    sketch.addVertex(result);
                                    sketch.close();
                                } else {
                                    webgis.alert("Keine Lösung gefunden, um das Polygon rechtwicklig abzuschließen", "Error");
                                }
                            }
                        });
                }

                $("<li>Orthogonal-Modus beenden</li>")
                    .css('background-image', 'url(' + webgis.css.imgResource('sketch_ortho_off-26.png', 'tools') + ')')
                    .appendTo($menu)
                    .click(function () {
                        var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                        if (sketch) {
                            sketch.stopOrthoMode();
                        }
                    });
            }

            $("<li>Richtung fixieren: aus</li>")
                .css('background-image', 'url(' + webgis.css.imgResource('sketch_remove_fix-26.png', 'tools') + ')')
                .appendTo($menu)
                .click(function () {
                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                    if (sketch) {
                        sketch._fixedDirectionVector = null;
                    }
                });

            $("<li>")
                .appendTo($menu)
                .webgis_form({
                    name: 'set_distance',
                    input: [
                        {
                            label: 'Distanz [m]',
                            name: 'distance',
                            type: 'number',
                            required: true
                        }
                    ],
                    submitText: 'Punkt setzen',
                    onSubmit: function (result) {
                        var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                        if (sketch) {
                            var vertices = sketch.getConstructionVerticesPro(),
                                lastVertex = sketch.getReferenceVertexPro();

                            var azimut = ((90 - Math.atan2(sketch._fixedDirectionVector.Y, sketch._fixedDirectionVector.X) * 180.0 / Math.PI) + 360.0) % 360.0;

                            if (sketch._snappedVertex) {
                                var sign = map.construct.projectOrthogonalSign(sketch._snappedVertex, lastVertex, sketch._fixedDirectionVector);
                                if (sign < 0) {
                                    azimut += 180.0;
                                }
                            }

                            var p = map.construct.distanceDirection(lastVertex, azimut, result.distance);

                            if (p) {
                                sketch._snappedVertex = p;
                                sketch.addVertexCoords(p.x, p.y, true); // x,y sind dummy -> es wird sowie der Snapped Vertex genommen...
                                $(this).closest('.webgis-contextmenu').trigger('click');
                            }
                        }
                    }
                });

            _addMoreFunctionsMenuItem($menu, clickEvent);

        }
        else if (sketch._fixedDistance && !sketch._fixedDirectionVector && !_isSimplePoint() && !suppressConstructionForms) {
            $("<li style='font-weight:bold'><i>Konstruktionsmodus:</i><div class='webgis-close-button'></div></li>")
                .appendTo($menu);

            $("<li>Distanz fixieren: aus</li>")
                .css('background-image', 'url(' + webgis.css.imgResource('sketch_remove_fix-26.png', 'tools') + ')')
                .appendTo($menu)
                .click(function () {
                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                    if (sketch) {
                        sketch._fixedDirectionVector = 0;
                    }
                });
            $("<li>")
                .appendTo($menu)
                .webgis_form({
                    name: 'set_azimut',
                    input: [
                        {
                            label: 'Azimut [deg]',
                            name: 'azimut',
                            type: 'number',
                            required: true
                        }
                    ],
                    submitText: 'Punkt setzen',
                    onSubmit: function (result) {
                        var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                        if (sketch) {
                            var vertices = sketch.getConstructionVerticesPro(),
                                lastVertex = sketch.getReferenceVertexPro();

                            var p = map.construct.distanceDirection(lastVertex, result.azimut, sketch._fixedDistance);

                            if (p) {
                                sketch._snappedVertex = p;
                                sketch.addVertexCoords(p.x, p.y, true); // x,y sind dummy -> es wird sowie der Snapped Vertex genommen...
                                $(this).closest('.webgis-contextmenu').trigger('click');
                            }
                        }
                    }
                });

            _addMoreFunctionsMenuItem($menu, clickEvent);
        }
        else if (sketch.isSketchMoving() && !suppressConstructionForms) {
            $("<li style='font-weight:bold'><i>Konstruktionsmodus:</i><div class='webgis-close-button'></div></li>")
                .appendTo($menu);

            $("<li>Sketch verschieben beenden</li>")
                .css('background-image', 'url(' + webgis.css.imgResource('sketch_move_off-26.png', 'tools') + ')')
                .appendTo($menu)
                .click(function () {
                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                    if (sketch)
                        sketch.endSketchMoving();
                });

            $("<li>")
                .appendTo($menu)
                .webgis_form({
                    name: 'set_vector',
                    input: [
                        {
                            label: 'dx',
                            name: 'dx',
                            type: 'number',
                            value: deltaXY[0],
                        },
                        {
                            label: 'dy',
                            name: 'dy',
                            type: 'number',
                            value: deltaXY[1],
                        }
                    ],
                    submitText: 'Übernehmen',
                    onSubmit: function (result) {
                        var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                        if (sketch) {
                            var vertices = sketch.getConstructionVerticesPro(),
                                lastVertex = sketch.getReferenceVertexPro();

                            var p = { X: lastVertex.X + (result.dx || 0), Y: lastVertex.Y + (result.dy || 0) };
                            webgis.complementWGS84(map.calcCrs(), [p]);
                            //console.log(p);

                            if (p) {
                                sketch._snappedVertex = p;
                                sketch.addVertexCoords(p.x, p.y, true); // x,y sind dummy -> es wird sowie der Snapped Vertex genommen...
                                $(this).closest('.webgis-contextmenu').trigger('click');
                            }
                        }
                    }
                });

            if (sketch._moverMarker && sketch._moverMarker.snapResult) {
                if (sketch._moverMarker.snapResult.vertices && sketch._moverMarker.snapResult.vertices.length === 2) {
                    var $container = $_menuContainer($menu);
                    _addFixdirectionConstructionItems($container, sketch);
                }
            }

            _addMoreFunctionsMenuItem($menu, clickEvent);
        }
        else if (sketch.isSketchRotating() && !suppressConstructionForms) {
            $("<li style='font-weight:bold'><i>Konstruktionsmodus:</i><div class='webgis-close-button'></div></li>")
                .appendTo($menu);

            $("<li>Sketch drehen beenden</li>")
                .css('background-image', 'url(' + webgis.css.imgResource('sketch_rotate_off-26.png', 'tools') + ')')
                .appendTo($menu)
                .click(function () {
                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                    if (sketch)
                        sketch.endSketchRotating();
                });

            $("<li>")
                .appendTo($menu)
                .webgis_form({
                    name: 'set_angle',
                    input: [
                        {
                            label: 'Winkel [deg]',
                            name: 'angle',
                            type: 'number',
                            value: (90 - azimut + 360.0) % 360.0,
                        }
                    ],
                    submitText: 'Übernehmen',
                    onSubmit: function (result) {
                        var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                        if (sketch) {
                            var vertices = sketch.getConstructionVerticesPro(),
                                lastVertex = sketch.getReferenceVertexPro();

                            var p = map.construct.distanceDirection(lastVertex, -(result.angle || 0) + 90, 1);

                            if (p) {
                                sketch._snappedVertex = p;
                                sketch.addVertexCoords(p.x, p.y, true); // x,y sind dummy -> es wird sowie der Snapped Vertex genommen...
                                $(this).closest('.webgis-contextmenu').trigger('click');
                            }
                        }
                    }
                });

            if (sketch._moverMarker && sketch._moverMarker.snapResult) {
                if (sketch._moverMarker.snapResult.vertices && sketch._moverMarker.snapResult.vertices.length === 2) {
                    var $container = $_menuContainer($menu);
                    _addFixdirectionConstructionItems($container, sketch);
                }
            }

            _addMoreFunctionsMenuItem($menu, clickEvent);
        }
        else if (sketch.isInFanMode() === true) {
            $("<li style='font-weight:bold'><i>Konstruktionsmodus:</i><div class='webgis-close-button'></div></li>")
                .appendTo($menu);

            $("<li>Fan Modus beenden</li>")
                .css('background-image', 'url(' + webgis.css.imgResource('sketch_fan_off-26.png', 'tools') + ')')
                .appendTo($menu)
                .click(function () {
                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                    if (sketch)
                        sketch.stopFanMode();
                });
        }
        else {
            suppressConstructionForms = false;

            var $header = $("<li style='font-weight:bold'>").appendTo($menu);
            $("<i>").text('Sketch').appendTo($header); 
            $("<div class='webgis-close-button'>").appendTo($header);
            $("<div class='webgis-help-button'>")
                .appendTo($header)
                .click(function (e) {
                    e.stopPropagation();
                    webgis.showHelp('tools/sketch/index.html');
                });

            var infoTextLines = sketch.infoTextLines();
            if (infoTextLines) {
                for (var i in infoTextLines) {
                    //$("<br>").appendTo($header);
                    $("<div style='color:#aaa;font-size:.8em;font-weight:normal'>")
                        .text(infoTextLines[i])
                        .appendTo($header);
                }
            }
            
            if (sketch.isReadOnly() !== true) {
                if (sketch.canUndo()) {
                    $("<li>Rückgängig/Undo<br/><span style='color:#aaa;font-size:.8em'>" + sketch.undoText() + "</span></li>")
                        .css('background-image', 'url(' + webgis.css.imgResource('sketch_undo-26.png', 'tools') + ')')
                        .appendTo($menu)
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                            if (sketch)
                                sketch.undo();
                        });
                }

                if (hasSelectedVertices) {
                    $("<li>Selektierte Vertices</li>").addClass('header').appendTo($menu);
                    $container = $_menuContainer($menu);

                    $_menuItem($container, "Aufheben", webgis.css.imgResource('sketch_unselect_vertices-26.png', 'tools'))
                        .click(function () {
                            var sketch = map.toolSketch();
                            if (sketch)
                                sketch.unselectAllVertices();
                        });

                    $_menuItem($container, "Umkehren", webgis.css.imgResource('sketch_toggle_selected_vertices-26.png', 'tools'))
                        .click(function () {
                            var sketch = map.toolSketch();
                            if (sketch)
                                sketch.toggleVertexSelection(); 
                        });

                    $_menuItem($container, "Entfernen", webgis.css.imgResource('sketch_remove_selected_vertices-26.png', 'tools'))
                        .click(function () {
                            var sketch = map.toolSketch();
                            if (sketch)
                                sketch.removeSelectedVertices();
                        });
                }

                var $container = $_menuContainer($menu);
                if (closestVertex != null && closestVertex.dist < 15) {
                    $_menuItem($container, "Vertex (" + (parseInt(closestVertex.index) + 1) + ") entfernen", webgis.css.imgResource('sketch_remove_marker-26.png', 'tools'))
                        .data('vertex-index', closestVertex.index)
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                            if (sketch)
                                sketch.removeVertex($(this).data('vertex-index'));
                        });

                    var fixText = closestVertex.vertex.fixed === true ?
                        "Vertex (" + (parseInt(closestVertex.index) + 1) + "): Fixierung aufheben" :
                        "Vertex(" + (parseInt(closestVertex.index) + 1) + ") fixieren/anschließen";

                    $_menuItem($container, fixText, webgis.css.imgResource('sketch_fix_marker-26.png', 'tools'))
                        .data('vertex-index', closestVertex.index)
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                            if (sketch) {
                                sketch.fixVertex($(this).data('vertex-index'));
                            }
                        });

                    if (map.ui.isClickBubbleActive()) {
                        $_menuItem($container, "Vertex (" + (parseInt(closestVertex.index) + 1) + ") verschieben", '')
                            .data('vertex-index', closestVertex.index)
                            .click(function () {
                                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                if (sketch)
                                    sketch.startBubbleMoveVertex($(this).data('vertex-index'));
                            });
                    }

                    if (webgis.currentPosition.canUsedWithSketchTool && map.ui.isQuickAccessButtonActive('webgis-gps')) {
                        if (webgis.continuousPosition.current && webgis.continuousPosition.current.status === 'ok') {
                            $("<li>Vertex (" + (parseInt(closestVertex.index) + 1) + ") auf GPS Position verschieben</li>")
                                .appendTo($menu)
                                .data('vertex-index', closestVertex.index)
                                .click(function () {
                                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                    var vertexIndex = $(this).data('vertex-index');
                                    var current = webgis.continuousPosition.current;
                                    if (sketch && current.status === 'ok') {
                                        //console.log('move vertex (' + vertexIndex + ')', current);
                                        try {
                                            var vertexFrameworkElement = null;
                                            if (sketch.getGeometryType() === 'point') {
                                                if (_frameworkElements.length > 0) {
                                                    vertexFrameworkElement = _frameworkElements[0];
                                                }
                                            } else {
                                                if (vertexIndex >= 0 && vertexIndex < _vertexFrameworkElements.length) {
                                                    vertexFrameworkElement = _vertexFrameworkElements[vertexIndex];
                                                }
                                            }

                                            if (vertexFrameworkElement !== null) {
                                                vertexFrameworkElement.fire('dragstart');
                                                vertexFrameworkElement.setLatLng(L.latLng(current.lat, current.lng));
                                                sketch._suppressFireSetEditableChanged = true;
                                                vertexFrameworkElement.fire('dragend');

                                                if (vertexFrameworkElement.sketch_vertex_coords) {
                                                    vertexFrameworkElement.sketch_vertex_coords.m = current.acc || 0;
                                                }
                                            }
                                        } catch (ex) { }
                                        sketch._suppressFireSetEditableChanged = false;
                                    }
                                });
                        }
                    }
                }
                else if (closestEdge != null && closestEdge.dist < 10) {
                    $_menuItem($container, "Vertex hinzufügen", webgis.css.imgResource('sketch_add_marker-26.png', 'tools'))
                        .data('edge', closestEdge)
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch(), edge = $(this).data('edge');
                            sketch.addPostVertex(edge.index, edge.t);
                        });
                }
                if (vertices.length > 0) {
                    $_menuItem($container, "Sketch entfernen", webgis.css.imgResource('sketch_remove-26.png', 'tools'))
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                            if (sketch)
                                sketch.remove();
                        });
                }
                if (_allowMultipart() && !sketch.isLastPartClosed()) {
                    $_menuItem($container, "Abschnitt schließen/neuen beginnen", webgis.css.imgResource('sketch_new_section-26.png', 'tools'))
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                            if (sketch)
                                sketch.appendPart();
                        });
                }
                if (sketch.canMergeParts() === true) {
                    $_menuItem($container, "Abschnitte zusammenführen (merge)", webgis.css.imgResource('sketch_merge_sections-26.png', 'tools'))
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                            if (sketch)
                                sketch.mergeParts();
                        });
                }
                
                if (vertices.length === 0 && !_isSimplePoint()) {
                    $_menuItem($container, "Aus Geo-Objektgeometrie übernehmen...", webgis.css.imgResource('sketch_fromgeometry-26.png', 'tools'))
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();

                            $(null).webgis_sketchFromGeometry({ map: map, sketch: sketch });
                        });
                }
                if (/*vertices.length === 0 && */_isLineOrPolygon()) {
                    $_menuItem($container, "Sketch hochladen (GPX, GeoJson)", webgis.css.imgResource('sketch_upload-26.png', 'tools'))
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                            $('body').webgis_modal({
                                title: 'Sketch hochladen',
                                width: '340px',
                                height: '400px',
                                onload: function ($content) {
                                    $content.webgis_uploadSketch({ map: map, sketch: sketch });
                                }
                            });
                        });
                }
                if (sketch.isValid() && _isLineOrPolygon()) {
                    $_menuItem($container, "Sketch herunterladen (GPX, GeoJson)", webgis.css.imgResource('sketch_download-26.png', 'tools'))
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                            $('body').webgis_modal({
                                title: 'Sketch herunterladen',
                                width: '340px',
                                height: '400px',
                                onload: function ($content) {
                                    $content.webgis_downloadSketch({ map: map, sketch: sketch });
                                }
                            });
                        });
                }

                if (webgis.usability.constructionTools === true) {
                    $("<li>Segmenterstellungs Modus</li>").addClass('header').appendTo($menu);
                    $container = $_menuContainer($menu);

                    if (!_isSimplePoint()) {
                        $_menuItem($container, "Gerade", webgis.css.imgResource('sketch_mode_default-26.png', 'tools'))
                            .addClass(sketch.isInDefaultMode() ? 'selected' : '')
                            .click(function () {
                                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                if (sketch) {
                                    sketch.startDefaultMode();
                                }
                            });

                        $_menuItem($container, "Orthogonal-Modus", webgis.css.imgResource('sketch_ortho-26.png', 'tools'))
                            .addClass(sketch.isInOrthoMode() ? 'selected' : '')
                            .click(function () {
                                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                if (sketch) {
                                    sketch.startOrthoMode();
                                }
                            });

                        if (map.construct.hasSnapping()) {
                            $_menuItem($container, "Trace-Modus", webgis.css.imgResource('sketch_trace-26.png', 'tools'))
                                .addClass(sketch.isInTraceMode() ? 'selected' : '')
                                .click(function () {
                                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                    if (sketch) {
                                        sketch.startTraceMode();
                                    }
                                });
                        }

                        if (sketch.getGeometryType() === "polyline") {
                            $_menuItem($container, "Fan-Modus", webgis.css.imgResource('sketch_fan-26.png', 'tools'))
                                .addClass(sketch.isInFanMode() ? 'selected' : '')
                                .click(function () {
                                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                    if (sketch) {
                                        sketch.startFanMode();
                                    }
                                });
                        };
                    }

                    $("<li>Sketch Werkzeuge</li>").addClass('header').appendTo($menu);
                    var $container = $_menuContainer($menu);

                    if (sketch.canReverse() === true) {
                        $_menuItem($container, "Vertexreihenfolge umkehren", webgis.css.imgResource('sketch_reverse-26.png', 'tools'))
                            .click(function () {
                                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                if (sketch)
                                    sketch.reverse();
                            });
                    }

                    if (closestVertex != null && closestVertex.dist < 15) {
                        $_menuItem($container, "Sketch verschieben", webgis.css.imgResource('sketch_move-26.png', 'tools'))
                            .data('vertex-index', closestVertex.index)
                            .click(function () {
                                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                if (sketch)
                                    sketch.startSketchMoving($(this).data('vertex-index'));
                            });

                        $_menuItem($container, "Sketch drehen", webgis.css.imgResource('sketch_rotate-26.png', 'tools'))
                            .data('vertex-index', closestVertex.index)
                            .click(function () {
                                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                if (sketch)
                                    sketch.startSketchRotating($(this).data('vertex-index'));
                            });
                    }
                    else if (closestEdge != null && closestEdge.dist < 10) {
                        $_menuItem($container, "Sketch drehen", webgis.css.imgResource('sketch_rotate-26.png', 'tools'))
                            .data('edge', closestEdge)
                            .click(function () {
                                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch(), edge = $(this).data('edge');
                                var vertexIndex = edge.index;
                                var angle = -sketch.getEdgeAngle(edge.index);

                                if (edge.t > 0.5) {
                                    vertexIndex = (vertexIndex + 1) % vertices.length;
                                    angle += Math.PI;
                                }

                                //console.log('angle', angle, vertexIndex);

                                sketch.startSketchRotating(vertexIndex, angle);
                            });
                    }

                    if (vertices.length > 1 && !sketch.isMultiPart()) { // not for multipart features!!
                        $_menuItem($container, "Sketch parallel versetzen...", webgis.css.imgResource('sketch_sketch_parallel-26.png', 'tools'))
                            .click(function () {
                                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                if (sketch) {
                                    var offset = 0;
                                    if (sketch._moverLine) {
                                        var latLngs = sketch._moverLine.getLatLngs();
                                        offset = map.construct.distFromWGS84(latLngs[0].lng, latLngs[0].lat, latLngs[1].lng, latLngs[1].lat);
                                        var vertices = sketch.getVerticesPro("x", "y");
                                        var v1 = new map.construct.vector2d(latLngs[1].lng - latLngs[0].lng, latLngs[1].lat - latLngs[0].lat);
                                        var v2 = new map.construct.vector2d(vertices[vertices.length - 1].x - vertices[vertices.length - 2].x, vertices[vertices.length - 1].y - vertices[vertices.length - 2].y);
                                        v1.normalize();
                                        v2.normalize();
                                        var a = (v2.vectorAngle(v1));
                                        offset = offset * Math.sin(a);
                                    }
                                    else {
                                        // Do Nothing
                                    }
                                    $('body').webgis_modal({
                                        title: 'Parallel versetzen',
                                        id: 'webgis-sketch-offset-modal',
                                        width: '320',
                                        height: '220px',
                                        onload: function ($content) {
                                            $content.webgis_offsetControl({
                                                offset: offset,
                                                offset_unit: 'm',
                                                on_apply: function (result) {
                                                    var vertices = sketch.getConstructionVerticesPro();
                                                    var hasSelectedVertices = sketch.hasSelectedVertices();
                                                    var orignalFixedVertices = [];

                                                    if (hasSelectedVertices) {
                                                        for (var i in vertices) {
                                                            if (vertices[i].fixed === true) {
                                                                orignalFixedVertices.push(vertices[i]);
                                                            }
                                                            vertices[i].fixed = vertices[i].fixed || vertices[i].selected !== true;
                                                        }
                                                    }

                                                    // Calc offset
                                                    var offsetVertices = map.construct.offset(vertices, result.offset_m);
                                                    if (offsetVertices == null || offsetVertices.length === 0) {
                                                        webgis.alert("Versetzen diese Geometrie leider nicht möglich", "Hinweis");
                                                        return;
                                                    }

                                                    if (hasSelectedVertices) {
                                                        for (var i in offsetVertices) {
                                                            if (!offsetVertices[i].fixed) {
                                                                offsetVertices[i].selected = true;
                                                            } else {
                                                                var orginalyFixed = false;

                                                                for (var o in orignalFixedVertices) {
                                                                    if (Math.abs(orignalFixedVertices[o].x - offsetVertices[i].x) <= 1e-8 &&
                                                                        Math.abs(orignalFixedVertices[o].y - offsetVertices[i].y) <= 1e-8) {
                                                                        orginalyFixed = true;
                                                                    }
                                                                    else if (Math.abs(orignalFixedVertices[o].X - offsetVertices[i].X) <= 1e-4 &&
                                                                        Math.abs(orignalFixedVertices[o].Y - offsetVertices[i].Y) <= 1e-4) {
                                                                        orginalyFixed = true;
                                                                    }
                                                                }

                                                                offsetVertices[i].fixed = orginalyFixed;
                                                            }
                                                        }
                                                    }

                                                    sketch.addUndo('Parallel versetzen');
                                                    sketch.remove(true);
                                                    sketch.addVertices(offsetVertices);
                                                    $('body').webgis_modal('close', { id: 'webgis-sketch-offset-modal' });
                                                }
                                            });
                                        }
                                    });
                                }
                            });
                    }

                    if (vertices.length > 1 && _isLine() && !sketch.isMultiPart()) {
                        $_menuItem($container, "Sketch(linie) verlängern", webgis.css.imgResource('sketch_extend_line-26.png', 'tools'))
                            .click(function () {
                                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                $('body').webgis_modal({
                                    title: 'Sketchlinie verlängern',
                                    width: '340px',
                                    height: '220px',
                                    id: 'webgis-sketch-extend-line-modal',
                                    onload: function ($content) {
                                        $content.webgis_extendLineSketchControl({
                                            on_apply: function (result) {
                                                console.log('on_apply', result);

                                                if (result.extend_m) {
                                                    var vertices = sketch._getRawVertices();

                                                    sketch.addUndo('Sketch verlängern');

                                                    if (result.ends == 0 || result.ends == -1) { //  extend begin
                                                        var dX = vertices[0].X - vertices[1].X,
                                                            dY = vertices[0].Y - vertices[1].Y;

                                                        var a = Math.atan2(dY, dX), len = Math.sqrt(dX * dX + dY * dY) + result.extend_m;
                                                        console.log(a, len);
                                                        vertices[0].X = vertices[1].X + Math.cos(a) * len; delete vertices[0].x;
                                                        vertices[0].Y = vertices[1].Y + Math.sin(a) * len; delete vertices[0].y;

                                                        webgis.complementWGS84(map.calcCrs(), [vertices[0]]);
                                                    }
                                                    if (result.ends == 0 || result.ends == 1) {  // extend end
                                                        var dX = vertices[vertices.length - 1].X - vertices[vertices.length - 2].X,
                                                            dY = vertices[vertices.length - 1].Y - vertices[vertices.length - 2].Y;

                                                        var a = Math.atan2(dY, dX), len = Math.sqrt(dX * dX + dY * dY) + result.extend_m;

                                                        vertices[vertices.length - 1].X = vertices[vertices.length - 2].X + Math.cos(a) * len; delete vertices[vertices.length - 1].x;
                                                        vertices[vertices.length - 1].Y = vertices[vertices.length - 2].Y + Math.sin(a) * len; delete vertices[vertices.length - 1].y;

                                                        webgis.complementWGS84(map.calcCrs(), [vertices[vertices.length - 1]]);
                                                    }

                                                    sketch.redraw(true);
                                                    sketch.redrawMarkers();
                                                }

                                                $('body').webgis_modal('close', { id: 'webgis-sketch-extend-line-modal' });
                                            }
                                        });
                                    }
                                });
                            });
                    }

                    $("<li>Snapping</li>").addClass('header').appendTo($menu);
                    $container = $_menuContainer($menu);

                    this._addSnapppingMenuItem($container);

                    if (sketch._moverMarker && sketch._moverMarker.snapResult) {
                        this._addSnappingPointItems($container, sketch);

                        if (sketch._moverMarker.snapResult.vertices && sketch._moverMarker.snapResult.vertices.length === 2) {
                            _addFixdirectionConstructionItems($container, sketch);
                        }
                    }

                    if (vertices.length > 0 && sketch._snappedVertex && !_isSimplePoint()) {
                        $_menuItem($container, "Distanz fixieren", webgis.css.imgResource('sketch_distance_fix-26.png', 'tools'))
                            .click(function () {
                                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                if (sketch) {
                                    var vertices = sketch.getVertices("X", "Y"), lastVertex = vertices[vertices.length - 1];
                                    var dx = sketch._snappedVertex.X - lastVertex[0], dy = sketch._snappedVertex.Y - lastVertex[1];
                                    sketch._fixedDistance = Math.sqrt(dx * dx + dy * dy);
                                }
                            });
                    }

                    $("<li>" + (sketch._contextVertexIndex !== null ? "Stützpunkt " + (parseInt(sketch._contextVertexIndex) + 1) + " konstruieren" : "Stützpunkt(e) konstruieren") + "</li>").addClass('header').appendTo($menu);
                    $container = $_menuContainer($menu);

                    if (!sketch.isInOrthoMode() || vertices.length < 2) {
                        this._addXYMenuItem($container)
                    }

                    if (vertices.length > 0 && !_isSimplePoint() && sketch._contextVertexIndex == null) {
                        $_menuItem($container, "Richtung/Entfernung", webgis.css.imgResource('sketch_polar-26.png', 'tools'))
                            .click(function () {
                                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                if (sketch) {
                                    $('body').webgis_modal({
                                        title: 'Richtung/Entfernung',
                                        id: 'webgis-sketch-dir-dist-modal',
                                        onload: function ($content) {
                                            $content.webgis_dirDistControl({
                                                azimut: azimut,
                                                azimut_unit: 'deg',
                                                azimut_readonly: sketch._fixedDirectionVector ? true : false,
                                                distance: dist,
                                                distance_unit: 'm',
                                                distance_readonly: sketch._fixedDistance ? true : false,
                                                on_apply: function (result) {
                                                    var vertices = sketch.getConstructionVerticesPro(),
                                                        lastVertex = sketch.getReferenceVertexPro();

                                                    var p = map.construct.distanceDirection(lastVertex, result.azimut_deg, result.distance_m, result.z_angle_deg);

                                                    if (p) {
                                                        sketch._snappedVertex = p;
                                                        sketch.addVertexCoords(p.x, p.y, true); // x,y sind dummy -> es wird sowie der Snapped Vertex genommen...
                                                        $('body').webgis_modal('close', { id: 'webgis-sketch-dir-dist-modal' });
                                                    }
                                                }
                                            });
                                        },
                                        onclose: function () {
                                        },
                                        width: '320',
                                        height: '380px'
                                    });
                                }
                            });
                    }

                    if (webgis.sketch.construct) {
                        var menuItems = webgis.sketch.construct.contextMenuItems(sketch);
                        for (var m in menuItems) {
                            $_menuItem($container, menuItems[m].title || webgis.i18n.get("construction-mode-" + menuItems[m].mode), menuItems[m].icon)
                                .data('mode', menuItems[m].mode)
                                .click(function () {
                                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                                    webgis.sketch.construct.setConstructionMode(sketch, $(this).data('mode'));
                                });
                        }
                    }
                }
            }

            /* Darstellung */
            if (allowChangePresentation === true && _isLineOrPolygon()) {
                $("<li>Darstellung</li>").addClass('header').appendTo($menu);

                var $colors = $("<li></li>").css({ padding: 2 }).appendTo($menu);
                $.each(['#ff0000', '#00ff00', '#0000ff', '#ffff00', '#00ffff', '#ff00ff', '#ffffff', '#000000'], function (index, col) {
                    $("<div>")
                        .css({
                            width: 32, height: 32, display: 'inline-block', cursor: 'pointer', backgroundColor: col, margin: 2
                        })
                        .data('col', col)
                        .appendTo($colors)
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map');
                            map.sketch.setColor($(this).data('col'));
                        });
                });

                var $widths = $("<li></li>").css({ padding: 2 }).appendTo($menu);
                $.each([1, 2, 3, 4, 5, 6, 7, 8], function (index, width) {
                    var $btn = $("<div>")
                        .css({
                            width: 32, height: 32, display: 'inline-block', cursor: 'pointer', margin: 2
                        })
                        .data('width', width)
                        .appendTo($widths)
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map');
                            map.sketch.setWeight($(this).data('width'));
                        });
                    $("<div>")
                        .css({
                            backgroundColor: 'black',
                            width: 28, height: width, marginLeft: 2, marginRight: 2, marginTop: 16 - width / 2
                        })
                        .appendTo($btn);
                });
            }
        }

        $menu.find('li').addClass('webgis-toolbox-tool-item');

        // remove empty options containers
        $menu.find('.webgis-ui-optionscontainer').each(function (i, container) {
            var $container = $(container);
            if ($container.find('.webgis-ui-imagebutton').length === 0) {
                $container.prev('.header').remove();
                //$container.remove();
            }
        });

        constructContextMenu.css('display', 'inline-block');
        constructContextMenu.css({
            left: Math.max(0, Math.min($(window).width() - constructContextMenu.width() - 10, clickEvent.originalEvent.clientX)),
            top: Math.max(0, Math.min($(window).height() - constructContextMenu.height() - 10, clickEvent.originalEvent.clientY))
        });
    }

    this.hideContextMenu = function (cancelConstruction) {
        if (constructContextMenu) {
            constructContextMenu.css('display', 'none');
        }

        if (cancelConstruction && webgis.sketch.construct) {
            webgis.sketch.construct.cancel(sketch);
        }
    };

    // Classic Menu Items
    //var $_menuContainer = function ($menu) {
    //    return $menu;
    //};
    //var $_menuItem = function ($container, name, icon) {
    //    return $("<li></li>")
    //        .text(name)
    //        .css('background-image', 'url(' + icon + ')')
    //        .appendTo($container);
    //};

    this._addXYMenuItem = function ($container) {
        $_menuItem($container, "Koordinaten (absolut)", webgis.css.imgResource('sketch_xy-26.png', 'tools'))
            .click(function () {
                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                $('body').webgis_modal({
                    title: 'Koordinaten (absolut)',
                    //id: 'webgis-sketch-xy-aboslute',
                    width: '340px',
                    height: '400px',
                    onload: function ($content) {
                        $content.webgis_xyAbsoluteControl({ map: map, sketch: sketch });
                    }
                });
            });
    };

    this._addSnapppingMenuItem = function ($container) {
        $_menuItem($container, "Snapping...", webgis.css.imgResource('sketch_snapping-26.png', 'tools'))
            .click(function () {
                var map = $(this).closest('.webgis-contextmenu').data('map');
                webgis.tools.onButtonClick(map, { type: 'clientbutton', command: 'snapping' });
            });
    };

    this._addFixDirectionItems = function ($container, sketch, onclick) {
        $_menuItem($container, "Richtung übernehmen: Parallel", webgis.css.imgResource('sketch_parallel_lines-26.png', 'tools'))
            .data('vertices', sketch._moverMarker.snapResult.vertices)
            .click(function () {
                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch(), vertices = $(this).data('vertices');
                if (sketch) {
                    var v1 = map.construct.getTopoVertex(vertices[0]), v2 = map.construct.getTopoVertex(vertices[1]);
                    var dx = v2[0] - v1[0], dy = v2[1] - v1[1], len = Math.sqrt(dx * dx + dy * dy);

                    if (len > 0) {
                        onclick(sketch, 90.0 - Math.atan2(dy / len, dx / len) * 180.0 / Math.PI);
                    }
                }
            });

        $_menuItem($container, "Richtung übernehmen: Rechtwicklig", webgis.css.imgResource('sketch_orthogonal_lines-26.png', 'tools'))
            .data('vertices', sketch._moverMarker.snapResult.vertices)
            .click(function () {
                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch(), vertices = $(this).data('vertices');
                if (sketch) {
                    var v1 = map.construct.getTopoVertex(vertices[0]), v2 = map.construct.getTopoVertex(vertices[1]);
                    var dx = v2[0] - v1[0], dy = v2[1] - v1[1], len = Math.sqrt(dx * dx + dy * dy);
                    if (len > 0) {
                        onclick(sketch, 90.0 - Math.atan2(dx / len, -dy / len) * 180.0 / Math.PI);
                    }
                }
            });
    };

    this._addSnappingPointItems = function ($container, sketch) {
        if (sketch._moverMarker && sketch._moverMarker.snapResult) {
            if (sketch._moverMarker.snapResult.vertices && sketch._moverMarker.snapResult.vertices.length === 2) {
                if (sketch._fixedDirectionVector && !sketch._fixedDistance) {
                    $_menuItem($container, "Mit Kante verschneiden", webgis.css.imgResource('sketch_intersect_lines-26.png', 'tools'))
                        .data('vertices', sketch._moverMarker.snapResult.vertices)
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch(), vertices = $(this).data('vertices');
                            if (sketch) {
                                var v1 = map.construct.getTopoVertex(vertices[0]), v2 = map.construct.getTopoVertex(vertices[1]);
                                var r1 = { X: v2[0] - v1[0], Y: v2[1] - v1[1] };
                                var vertices = sketch.getVertices("X", "Y"), lastVertex = { X: vertices[vertices.length - 1][0], Y: vertices[vertices.length - 1][1] };
                                var p = map.construct.intersectLines(lastVertex, sketch._fixedDirectionVector, { X: v1[0], Y: v1[1] }, r1);
                                if (p) {
                                    sketch._snappedVertex = p;
                                    sketch.addVertexCoords(p.x, p.y, true);
                                }
                            }
                        });
                }
                else if (!sketch._fixedDirectionVector && sketch._fixedDistance) {
                    $_menuItem($container, "Mit Kante verschneiden", webgis.css.imgResource('sketch_intersect_lines-26.png', 'tools'))
                        .data('vertices', sketch._moverMarker.snapResult.vertices)
                        .click(function () {
                            var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch(), vertices = $(this).data('vertices');
                            if (sketch) {
                                var v1 = map.construct.getTopoVertex(vertices[0]), v2 = map.construct.getTopoVertex(vertices[1]);
                                var r1 = { X: v2[0] - v1[0], Y: v2[1] - v1[1] };
                                var vertices = sketch.getVertices("X", "Y"), lastVertex = { X: vertices[vertices.length - 1][0], Y: vertices[vertices.length - 1][1] };
                                var p = map.construct.intersectCircleLine(lastVertex, sketch._fixedDistance, { X: v1[0], Y: v1[1] }, r1);
                                if (p) {
                                    var index = map.construct.closestVertexIndex(sketch._snappedVertex, p);
                                    sketch._snappedVertex = p[index];
                                    sketch.addVertexCoords(p[index].x, p[index].y, true); // x,y sind dummy -> es wird sowie der Snapped Vertex genommen...
                                }
                            }
                        });
                }

                $_menuItem($container, "Mittelpunkt Kante", webgis.css.imgResource('sketch_edge_center-26.png', 'tools'))
                    .data('vertices', sketch._moverMarker.snapResult.vertices)
                    .click(function () {
                        var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch(), vertices = $(this).data('vertices');
                        if (sketch) {
                            var v1 = map.construct.getTopoVertex(vertices[0]), v2 = map.construct.getTopoVertex(vertices[1]);
                            var p = map.construct.midPoint({ X: v1[0], Y: v1[1] }, { X: v2[0], Y: v2[1] });
                            if (p) {
                                sketch._snappedVertex = p;
                                sketch.addVertexCoords(p.x, p.y, true); // x,y sind dummy -> es wird sowie der Snapped Vertex genommen...
                            }
                        }
                    });
            }
        }
    };

    // Modern Menu Items (Image Buttons)
    var $_menuContainer = function ($menu) {
        return $("<li>")
            .addClass("webgis-ui-optionscontainer contains-lables")
            .css('max-width', '320px')
            .appendTo($menu);
    };

    var $_menuItem = function ($container, name, icon) {
        var $item = $("<div>")
            .attr('title', name)
            .addClass("webgis-ui-imagebutton")
            .css('background-image', 'url(' + icon + ')')
            .appendTo($container);
        $("<div>")
            .addClass("webgis-ui-imagebutton-text")
            .text(name)
            .appendTo($item);

        return $item;
    };

    var _addMoreFunctionsMenuItem = function ($menu, clickEvent) {
        $("<li>Weitere Funktionen...</li>")
            .css('background-image', 'url(' + webgis.css.imgResource('enter-26.png', 'ui') + ')')
            .appendTo($menu)
            .click(function (e) {
                e.stopPropagation();
                var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch();
                suppressConstructionForms = true;

                sketch.ui.showContextMenu.apply(sketch.ui, [clickEvent]);
            });
    }

    var _addFixdirectionConstructionItems = function ($container, sketch) {
        var vertices = sketch._getRawVertices();

        if (vertices.length > 0 && !_isSimplePoint()) {
            $_menuItem($container, "Richtung fixieren: Parallel", webgis.css.imgResource('sketch_parallel_lines-26.png', 'tools'))
                .data('vertices', sketch._moverMarker.snapResult.vertices)
                .click(function () {
                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch(), vertices = $(this).data('vertices');
                    if (sketch) {
                        var v1 = map.construct.getTopoVertex(vertices[0]), v2 = map.construct.getTopoVertex(vertices[1]);
                        var dx = v2[0] - v1[0], dy = v2[1] - v1[1], len = Math.sqrt(dx * dx + dy * dy);
                        if (len > 0)
                            sketch._fixedDirectionVector = { X: dx / len, Y: dy / len };
                    }
                });

            $_menuItem($container, "Richtung fixieren: Rechtwicklig", webgis.css.imgResource('sketch_orthogonal_lines-26.png', 'tools'))
                .data('vertices', sketch._moverMarker.snapResult.vertices)
                .click(function () {
                    var map = $(this).closest('.webgis-contextmenu').data('map'), sketch = map.toolSketch(), vertices = $(this).data('vertices');
                    if (sketch) {
                        var v1 = map.construct.getTopoVertex(vertices[0]), v2 = map.construct.getTopoVertex(vertices[1]);
                        var dx = v2[0] - v1[0], dy = v2[1] - v1[1], len = Math.sqrt(dx * dx + dy * dy);
                        if (len > 0)
                            sketch._fixedDirectionVector = { X: dy / len, Y: -dx / len };
                    }
                });
        }
    }
}