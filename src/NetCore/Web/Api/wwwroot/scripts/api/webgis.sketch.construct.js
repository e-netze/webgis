webgis.sketch.construct = new function (){
    var _constructions = [];

    this.register = function (construction) {
        _constructions.push(construction);
    }

    this.onMapMouseMove = function (sketch, latlng) {
        var tool = _currentConstructionTool(sketch);

        if (tool && tool.onMapMouseMove) {
            if (tool.onMapMouseMove(sketch, latlng)) {
                sketch.fireCurrentState();
                return true;
            }
        }

        return false;
    };

    this.onAddVertex = function (sketch, vertex) {
        var tool = _currentConstructionTool(sketch);

        if (tool && tool.onAddVertex) {
            if (tool.onAddVertex(sketch, vertex)) {
                sketch.fireCurrentState();
                return true;
            }
        }

        return false;
    };

    this.onMapKeyDown = function (sketch, e) {
        for (var c in _constructions) {
            if (_constructions[c].onMapKeyDown) {
                if (_constructions[c].onMapKeyDown(sketch, e) === true) {  // first wins!
                    sketch.fireCurrentState();
                    return true;
                }
            }
        }

        return false;
    };

    this.onMapKeyUp = function (sketch, e) {
        var tool = _currentConstructionTool(sketch);

        if (tool && tool.onMapKeyUp) {
            if (tool.onMapKeyUp(sketch, e)) {
                sketch.fireCurrentState();
                return true;
            }
        }

        return false;
    };

    this.onMarkerClick = function (sketch, marker) {
        var tool = _currentConstructionTool(sketch);

        if (tool && tool.onMarkerClick) {
            if (tool.onMarkerClick(sketch, marker)) {
                sketch.fireCurrentState();
                return true;
            }
        }

        return false;
    };

    this.suppressSnapping = function (sketch) {
        var tool = _currentConstructionTool(sketch);

        if (tool && tool.suppressSnapping) {
            if (tool.suppressSnapping(sketch)) {
                return true;
            }
        }

        return false;
    };

    this.cancel = function (sketch) {
        var tool = _currentConstructionTool(sketch);

        //console.log('cancel', tool);

        if (tool && tool.onCancel) {
            result = tool.onCancel(sketch);
            sketch.fireCurrentState();
        }

        this.setConstructionMode(sketch, null);
    };

    this.createContextMenuUI = function (sketch, $menu) {
        var tool = _currentConstructionTool(sketch);

        if (tool && tool.createContextMenuUI) {
            return tool.createContextMenuUI(sketch, $menu);
        }

        return false;
    };

    this.contextMenuItems = function (sketch) {
        var menuItems = [];

        for (var c in _constructions) {
            var contextMenuItem = _constructions[c].contextMenuItem ? _constructions[c].contextMenuItem(sketch) : null;

            if (contextMenuItem && contextMenuItem.mode) {
                menuItems.push(contextMenuItem);
            }
        }

        return menuItems;
    };

    this.setConstructionMode = function (sketch, mode, targetSelector) {
        //console.log('setConstrionMode', mode, targetSelector);
        sketch._constructionMode = mode;

        if (!sketch._constructionMode) {
            if (sketch.map && sketch.map.getActiveTool) {
                sketch.map.setCursor(sketch.map.getActiveTool() ? sketch.map.getActiveTool().cursor : 'pointer');
            }
        }

        sketch._constructionModeTargetSelector = targetSelector || null;
        sketch.fireCurrentState();

        var tool = _currentConstructionTool(sketch);
        if (tool && tool.init) {
            tool.init(sketch);
        };
    };
    this.getConstructionMode = function (sketch) { return sketch._constructionMode; };
    this.currentConstructionModeIncluded = function (sketch, modes) {
        if (!sketch._constructionMode)
            return false;

        return webgis.$.inArray(sketch._constructionMode, modes) >= 0;
    };

    this.getInfoItems = function (sketch) {
        var tool = _currentConstructionTool(sketch), result = false;

        if (tool && tool.getInfoItems) {
            return tool.getInfoItems(sketch);
        }

        return null;
    }

    var _currentConstructionTool = function (sketch) {
        var mode = webgis.sketch.construct.getConstructionMode(sketch);

        for (var c in _constructions) {
            if (_constructions[c].modes && webgis.sketch.construct.currentConstructionModeIncluded(sketch, _constructions[c].modes)) {
                return _constructions[c];
            }
        }

        return null;
    }

    this._sketchFromContextMenuItem = function (menuItem) {
        var map = $(menuItem).closest('.webgis-contextmenu').data('map'),
            sketch = map.toolSketch();

        return sketch;
    };
    this._closeContextMenu = function (menuItem) {
        $(menuItem).closest('.webgis-contextmenu').trigger('click');
    };
    this._isPolygon = function (sketch) { return sketch.getGeometryType() === "polygon" };
    this._isLineOrPolygon = function (sketch) { return $.inArray(sketch.getGeometryType(), ["polyline", "polygon", "dimline", "hectoline"]) >= 0 };

    this._ui = new function () {
        this.distanceContextUI = function (distanceElement, sketch, $menu, onSucceeded) {
            $("<li>")
                .appendTo($menu)
                .webgis_form({
                    name: 'construct_distance',
                    input: [
                        {
                            label: webgis.l10n.get("construction-label-radius"),
                            name: 'radius',
                            type: 'number',
                            required: true
                        }
                    ],
                    submitText: webgis.l10n.get("cunstruction-label-apply-distance"),
                    onSubmit: function (result) {
                        var sketch = webgis.sketch.construct._sketchFromContextMenuItem(this);
                        var radius = result.radius;

                        if (radius) {
                            var element = distanceElement;

                            var p0 = { x: element.getLatLngs()[0].lng, y: element.getLatLngs()[0].lat };
                            webgis.complementProjected(sketch.map.calcCrs(), [p0]);
                            var p1 = { X: p0.X += radius, Y: p0.Y };
                            webgis.complementWGS84(sketch.map.calcCrs(), [p1]);

                            element.setCirclePoint(L.latLng(p1.y, p1.x));
                            element._closed = true;
                        }

                        webgis.sketch.construct._closeContextMenu(this);

                        if (onSucceeded)
                            onSucceeded(sketch);
                    }
                });
        };
        this.directionContextUI = function (directionElement, sketch, $menu, onSucceeded) {
            $("<li>")
                .appendTo($menu)
                .webgis_form({
                    name: 'construct_direction',
                    input: [
                        {
                            label: webgis.l10n.get("construction-label-azimuth"),
                            name: 'azimut',
                            type: 'number',
                            value: 0
                        },
                        {
                            name: 'unit',
                            type: 'select',
                            options: [
                                { text: webgis.l10n.get("construction-label-unit-deg"), value: 'deg' },
                                { text: webgis.l10n.get("construction-label-unit-gon"), value: 'gon' }
                            ]
                        }
                    ],
                    submitText: webgis.l10n.get("construction-label-apply-direction"),
                    onSubmit: function (result) {
                        var sketch = webgis.sketch.construct._sketchFromContextMenuItem(this);
                        var azimut = result.azimut;
                        var unit = result.unit;

                        switch (unit) {
                            case 'gon':
                                azimut *= 0.9;
                                break;
                        }

                        //console.log('azimut (deg)', azimut);

                        webgis.sketch.construct._closeContextMenu(this);

                        _setDirection(sketch, directionElement, azimut, onSucceeded);
                    }
                });
        };
        this.addContainer = function (sketch, $menu, options, appendXYAbsolute, appendFixedDirection, directionElement, onSetDirectionSucceeded) {
            $container = $("<li>")
                .addClass("webgis-ui-optionscontainer contains-lables")
                .css('max-width', '320px')
                .appendTo($menu);

            if (options) {
                if (options.appendSnapping === true) {
                    sketch.ui._addSnapppingMenuItem($container);
                }
                if (options.appendXYAbsolute === true) {
                    sketch.ui._addXYMenuItem($container);
                    sketch.ui._addSnappingPointItems($container, sketch);
                }

                if (options.fixDirection && options.fixDirection.append === true &&
                    sketch._moverMarker && sketch._moverMarker.snapResult && sketch._moverMarker.snapResult.vertices) {
                    sketch.ui._addFixDirectionItems($container, sketch, function (sketch, azimut) {
                        _setDirection(sketch, options.fixDirection.directionElement, azimut, options.fixDirection.onSucceeded);
                    });
                }
            }

            return $container;
        };

        var _setDirection = function (sketch, directionElement, azimut, onSucceeded) {
            var vertices = directionElement.getVertices();
            var p = sketch.map.construct.distanceDirection(vertices[0], azimut, 10, 0);
            directionElement.setDirectionVertex(p);
            directionElement._closed = true;

            if (onSucceeded)
                onSucceeded(sketch);
        };

        this._infoItemClickConstructionPoint = function () {
            return {
                text: webgis.l10n.get("construction-info-apply-vertex")
            };
        };
        this._infoItemClickConstructionPoints = function () {
            return {
                text: webgis.l10n.get("construction-info-apply-vertices")
            };
        };

        this._infoItemRightMouseButton = function () {
            return {
                text: webgis.l10n.get("construction-info-tip-use-context-menu"),
                class: "info"
            }
        };
    }
}();