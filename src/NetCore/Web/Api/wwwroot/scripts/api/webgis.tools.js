webgis.tools = new function () {

    var $ = webgis.$;

    this.navigation = {
        fullExtent: 'webgis.tools.fullextent',
        zoomBack: 'webgis.tools.zoomback',
        refreshMap: 'webgis.tools.refreshmap',
        currentPos: 'webgis.tools.currentposition',
        continuousPos: 'webgis.tools.continuousposition',
        showlegend: 'webgis.tools.showlegend',
        serviceOrder: 'webgis.tools.serviceorder',
        bookmarks: 'webgis.tools.serialization.bookmarks',
        saveMap: 'webgis.tools.serialization.savemap',
        loadMap: 'webgis.tools.serialization.loadmap',
        shareMap: 'webgis.tools.serialization.sharemap',
        liveshareMap: 'webgis.tools.serialization.livesharemap',
        zoomIn: 'webgis.tools.boxzoomin',
        zoomToSketch: 'webgis.tools.zoomtosketch'
    };
    this.info = {
        identify: 'webgis.tools.identify',
        coordinates: 'webgis.tools.coordinates',
        rasteridentify: 'webgis.tools.rasteridentify',
        chainage: 'webgis.tools.chainage',
        queryResults: 'webgis.tools.queryresulttable',
        markerInfo: 'webgis.tools.markerinfo',
        addtoselection: 'webgis.tools.addtoselection',
        removefromselection:"webgis.tools.removefromselection"

    };
    this.advanced = {
        measureDistance: 'webgis.tools.measureline',
        measureArea: 'webgis.tools.measurearea',
        measureCircle: 'webgis.tools.measurecircle',
        profile: 'webgis.tools.profile.createprofile',
        mapmarkup: 'webgis.tools.mapmarkup.mapmarkup',
        edit: 'webgis.tools.editing.edit',
        snapping: 'webgis.tools.snapping',
        geocode: 'webgis.tools.georeferencing.georeference',
        georefImage: 'webgis.tools.georeferencing.image.georeference',
        treed: 'webgis.tools.threed.threed'
    };
    this.io = {
        print: 'webgis.tools.print',
        downloadmapimage: 'webgis.tools.downloadmapimage'
    };
    this.presenation = {
        labeling: 'webgis.tools.presentation.labeling',
        filter: 'webgis.tools.presentation.visfilter',
        filterRemove: 'webgis.tools.presentation.visfilterremoveall'
    };
    this._toolData = [];
    this.getToolData = function (toolId) {
        if (!this._toolData[toolId])
            this._toolData[toolId] = {};
        return this._toolData[toolId];
    };
    this.getToolName = function (id) {
        for (var category in this) {
            for (var name in this[category]) {
                if (this[category][name] === id)
                    return 'webgis.tools.' + category + '.' + name;
            }
        }
        return null;
    };
    this.getToolId = function (name) {
        var ret = Function('return ' + name)();
        return ret || name;
    };
    this.getToolOrder = function (id) {
        var order = 0;
        for (var category in this) {
            for (var name in this[category]) {
                if (this[category][name] === id)
                    return order;
                order++;
            }
        }
        return Number.MAX_VALUE;
    };
    this.userTool = function (t) {
        this.type = 'clienttool';
        this.tooltype = t.tooltype || 'click';
        this.id = 'webgis.usertool - ' + t.name;
        this.name = t.name;
        this.image = 'coordinates.png';
        this.hasui = true;
        this.ui = t.ui;
        this.oncommit = t.oncommit;
        this.onmapclick = t.onmapclick;
        this.command = t.command;
        if (!this.ui) {
            if (this.tooltype === "sketch0d" || this.tooltype === "sketch1d" || this.tooltype === "sketch2d" || this.tooltype==="sketchany" || this.tooltype === "sketchcircle") {
                this.ui = {
                    elements: [
                        {
                            text: 'Sketch entfernen',
                            buttontype: 'clientbutton',
                            buttoncommand: 'removesketch',
                            type: 'button'
                        }
                    ]
                };
            }
            else {
                this.ui = { elements: [] };
            }
        }
        if (this.ui && this.ui.elements && t.oncommit) {
            this.ui.elements.push({
                text: t.commit_text || 'Senden',
                buttontype: 'clientbutton',
                type: 'button',
                buttoncommand: function () {
                    var tool = this.map.getActiveTool();
                    if (tool.oncommit)
                        tool.oncommit(tool);
                }
            });
        }
    };
    this.userButton = function (t) {
        this.type = 'clientbutton';
        this.id = 'webgis.userbutton - ' + t.name;
        this.name = t.name;
        this.image = t.image || 'coordinates.png';
        this.command = t.command;
    };
    this.onButtonClick = function (map, tool, domElement, data, customEvent) {
        var confirmations = [];
        if (!tool || !this.checkConfirmations(map, tool, 'buttonclick', tool.command, null, confirmations))
            return;

        var activeTool = (map ? map.getActiveTool() : null);
        if (tool.type === 'clientbutton') {
            if (!this.checkConfirmations(map, activeTool, 'buttonclick', tool.command, null, confirmations)) {
                return;
            }

            switch (tool.command) {
                case 'refresh':
                    if (map) {
                        map.refresh();
                    }
                    break;
                case 'back':
                    if (map) {
                        map.zoomBack();
                    }
                    break;
                case 'fullextent':
                    if (map && map.initialBounds) {
                        map.zoomTo(map.initialBounds, true);
                    }
                    break;
                case 'removesketch':
                    if (map && map.sketch) {
                        map.sketch.remove();
                    }
                    break;
                case 'removecirclemarker':
                    if (map)
                        map.hideCircleMarker();
                    break;
                case 'queryresults':
                    if (map) {
                        var $tabControl = map.ui.getQueryResultTabControl();
                        if ($tabControl) {
                            $tabControl.webgis_tab_control('refresh');
                        } else {
                            map.queryResultFeatures.showTable();
                        }
                    }
                    break;
                case 'showtoolmodaldialog':
                    if (map && activeTool) {
                        map.ui.webgisContainer().webgis_modal('show', { id: activeTool.id });
                    }
                    break;
                case 'hidetoolmodaldialog':
                    if (map && activeTool) {
                        map.ui.webgisContainer().webgis_modal('hide', { id: activeTool.id });
                    }
                    break;
                case 'showmarkerinfo':
                    if (map) {
                        map.graphics._refreshInfoContainers();
                    }
                    break;
                case 'setparenttool':
                    webgis.confirmDiscardChanges(domElement, map, function () {
                        if (map && activeTool && activeTool.parentTool) {
                            webgis.tools.onButtonClick(map, activeTool.parentTool);
                        }
                    });
                    break;
                case 'setgraphicssymbol':
                case 'setgraphics_symbol_and_apply_to_selected':
                    if (map && map.graphics && tool.value)
                        map.graphics.setCurrentSymbol(tool.value);
                    break;
                case 'setgraphicslinecolor':
                case 'setgraphics_stroke_color_and_apply_to_selected':
                    if (map && map.graphics && tool.value) {
                        map.graphics.setColor(tool.value);
                    }
                    break;
                case 'setgraphicsfillcolor':
                case 'setgraphics_fill_color_and_apply_to_selected':
                    if (map && map.graphics && tool.value)
                        map.graphics.setFillColor(tool.value);
                    break;
                case 'setgraphicsfillopacity':
                case 'setgraphics_fill_opacity_and_apply_to_selected':
                    if (map && map.graphics && tool.value)
                        map.graphics.setFillOpacity(tool.value);
                    break;
                case 'setgraphicslineweight':
                case 'setgraphics_stroke_weight_and_apply_to_selected':
                    if (map && map.graphics && tool.value)
                        map.graphics.setWeight(tool.value);
                    break;
                case 'setgraphicslinestyle':
                case 'setgraphics_stroke_style_and_apply_to_selected':
                    if (map && map.graphics && tool.value)
                        map.graphics.setLineStyle(tool.value);
                    break;
                case 'setgraphicsdistancecirclesteps':
                    if (map && map.graphics && tool.value)
                        map.graphics.setDistanceCircleSteps(tool.value);
                    break;
                case 'setgraphicsdistancecircleradius':
                    if (map && map.graphics && tool.value)
                        map.graphics.setDistanceCircleRadius(tool.value);
                    break;
                case 'setgraphicstextsize':
                case 'setgraphics_text_size_and_apply_to_selected':
                    if (map && map.graphics && tool.value)
                        map.graphics.setTextSize(tool.value);
                    break;
                case 'setgraphicshectolineunit':
                    if (map && map.graphics && tool.value)
                        map.graphics.setHectolineUnit(tool.value);
                    break;
                case 'setgraphicshectolineinterval':
                    //console.log('setgraphicshectolineinterval', tool)
                    if (map && map.graphics && tool.value)
                        map.graphics.setHectolineInterval(tool.value);
                    break;
                case 'setgraphicstextstyle':
                case 'setgraphics_text_style_and_apply_to_selected':
                    if (map && map.graphics && tool.value)
                        map.graphics.setTextStyle(tool.value);
                    break;
                case 'setgraphicstextcolor':
                case 'setgraphics_text_color_and_apply_to_selected':
                    if (map && map.graphics && tool.value)
                        map.graphics.setTextColor(tool.value);
                    break;
                case 'setgraphicspointcolor':
                case 'setgraphics_point_color_and_apply_to_selected':
                    if (map && map.graphics && tool.value)
                        map.graphics.setPointColor(tool.value);
                    break;
                case 'setgraphicspointsize':
                case 'setgraphics_point_size_and_apply_to_selected':
                    if (map && map.graphics && tool.value)
                        map.graphics.setPointSize(tool.value);
                    break;
                case "assumecurrentgraphicselement":
                    if (map && map.graphics)
                        map.graphics.assumeCurrentElement();
                    break;
                case "removecurrentgraphicselement":
                    if (map && map.graphics)
                        map.graphics.removeCurrentElement();
                    break;
                case "refreshgraphicsui":
                    if (map && map.graphics)
                        map.graphics.refreshUI();
                    break;
                case "removelivesharemarker":
                    if (webgis.liveshare)
                        webgis.liveshare.removeUserMarker();
                    break;
                case 'currentpos':
                    if (webgis.liveshare && webgis.liveshare.isInitialized()) {
                        webgis.liveshare.setCurrentPosMarker();
                    }
                    else if (map) {
                        map.removeMarkerGroup('currentPos');
                        var cancelTracker = new webgis.cancelTracker();
                        webgis.showProgress('Aktuelle Position wird abgefragt...', null, cancelTracker);
                        //var asSketchVertex = webgis.currentPosition.useWithSketchTool === true &&
                        //                     map.getActiveTool() !== null &&
                        //                     map.getActiveTool().tooltype.indexOf('sketch') === 0;
                        var asSketchVertex = false;
                        webgis.currentPosition.get({
                            highAccuracy: asSketchVertex,
                            maxWatch: webgis.currentPosition.maxWatch,
                            onSuccess: function (pos) {
                                webgis.hideProgress('Aktuelle Position wird abgefragt...');
                                if (!cancelTracker.isCanceled()) {
                                    var lng = pos.coords.longitude, lat = pos.coords.latitude, acc = pos.coords.accuracy / (webgis.calc.R) * 180.0 / Math.PI;
                                    var displayAccuracy = pos.coords.accuracy > 1 ? Math.round(pos.coords.accuracy) : Math.round(pos.coords.accuracy * 100) / 100.0;
                                    if (asSketchVertex) {
                                        map.sketch.addVertex({ x: lng, y: lat });
                                    }
                                    else {
                                        map.zoomTo([lng - acc, lat - acc, lng + acc, lat + acc]);
                                        map.toMarkerGroup('currentPos', map.addMarker({
                                            lat: lat, lng: lng,
                                            text: "<h3>Mein Standort</h3>Genauigkeit: " + displayAccuracy + "m<br/>",
                                            openPopup: true,
                                            icon: "currentpos_red",
                                            buttons: [
                                                {
                                                    label: 'Aktualisieren',
                                                    onclick: function (map) {
                                                        webgis.tools.onButtonClick(map, { type: 'clientbutton', command: 'currentpos' });
                                                    }
                                                },
                                                {
                                                    label: 'Entfernen',
                                                    onclick: function (map, marker) {
                                                        //map.removeMarkerGroup('currentPos');
                                                        map.removeMarker(marker);
                                                    }
                                                }
                                                //,{
                                                //    label: 'Verfolgung starten',
                                                //    onclick: function (map, marker) {
                                                //        webgis.continuousPosition.start(map);
                                                //    }
                                                //}
                                            ]
                                        }));
                                    }
                                }
                            },
                            onError: function (err) {
                                webgis.hideProgress('Aktuelle Position wird abgefragt...');
                                webgis.alert('Ortung nicht möglich: ' + (err ? err.message + " (Error Code " + err.code + ")" : ""), "Fehler");
                            }
                        });
                    }
                    break;
                case 'boxzoomin':
                    var currentTool = map.getActiveTool();
                    if (currentTool.id === tool.id && currentTool.currentTool) {
                        map.setActiveTool(currentTool.currentTool);
                    }
                    else {
                        map.setActiveTool({
                            id: tool.id,
                            tooltype: 'box',
                            name: tool.name,
                            currentTool: currentTool,
                            onbox: function (map, bounds) {
                                if (map.getActiveTool().currentTool)
                                    map.setActiveTool(map.getActiveTool().currentTool);
                                map.zoomTo(bounds);
                            }
                        });
                        if (webgis.isTouchDevice() === false) {
                            webgis.showTip('tip-desktop-boxzoomin');
                        }
                    }
                    break;
                case 'print':
                    if (map) {
                        var printOptions = {};
                        if (domElement) {
                            var $holder = $(domElement).closest('.webgis-tabs-tab-content-holder');
                            if ($holder.length === 0)
                                $holder = $(domElement).closest('.webgis-modal-content');
                            if ($holder.length === 0)
                                $holder = $(domElement).parent();
                            printOptions.layout = $holder.find('.webgis-print-tool-layout').val();
                            printOptions.format = $holder.find('.webgis-print-tool-format').val();
                            printOptions.dpi = $holder.find('.webgis-print-tool-quality').val();
                            printOptions.scale = $holder.find('.webgis-map-scales-select').val();

                            printOptions.showQueryMarkers = $holder.find('.webgis-print-show-query-markers').val() === 'show';
                            printOptions.showCoordinateMarkers = $holder.find('.webgis-print-show-coords-markers').val() === 'show';
                            printOptions.showChainageMarkers = $holder.find('.webgis-print-show-chainage-markers').val() === 'show';
                            printOptions.queryMarkersLabelField = $holder.find('.webgis-print-query-markers-label-field').val();
                            printOptions.coordinateMarkersLabelField = $holder.find('.webgis-print-coords-markers-label-field').val();

                            printOptions.attachQueryResults = $holder.find('.webgis-print-attach-query-results').val();
                            printOptions.attachCoordinates = $holder.find('.webgis-print-attach-coordinates').val();
                            printOptions.attachCoordinatesField = $holder.find('.webgis-print-attach-coordinates-field').val();

                            printOptions.showSketch = $holder.find('.webgis-print-show-tool-sketch').val() !== '';
                            printOptions.showSketchLabels = $holder.find('.webgis-print-tool-sketch-labels').val();

                            //console.log('test', $holder.find('#print-sketch-selector--print_tool_sketch').length, $holder.find('#print-sketch-selector--print_tool_sketch_labels').length);

                            printOptions.rotation = map.viewLense.currentRotation();
                            $holder.find('.webgis-print-textelement').each(function (i, e) {
                                printOptions["LAYOUT_TEXT_" + $(e).attr('id')] = $(e).val();
                                map.setPersistentToolParameter('webgis.tools.print', "LAYOUT_TEXT_" + $(e).attr('id'), $(e).val());
                            });
                            $holder.find('.webgis-print-header-id-element').each(function (i, e) {
                                printOptions["LAYOUT_HEADER_ID"] = $(e).val();
                            });
                        }
                        webgis.showProgress('Drucken...', null);
                        map.print(printOptions, function (result) {
                            webgis.hideProgress('Drucken...');
                            if (result) {
                                if (result.url) {
                                    var data = webgis.tools.getToolData("webgis.tools.io.print");
                                    if (!data.prints)
                                        data.prints = [];
                                    data.prints.push(result);

                                    if (result.success == false && result.exception) {
                                        webgis.alert("Errors occurred during printing. The printout may not be complete. Please check if all relevant topics are included in the printout.:\n\n\n\n\n" + result.exception, "info", function () {
                                            webgis.delayed(function () {
                                                map.showPrintTasks();
                                            }, 500);
                                        });
                                    } else {
                                        map.showPrintTasks();
                                    }
                                } else {
                                    webgis.alert(result.exception, 'error');
                                }
                            } else {
                                webgis.alert('Unkonwn error!', 'error');
                            }
                        });
                    }
                    break;
                case 'downloadmapimage':
                    if (map) {
                        var downloadOptions = {};
                        if (domElement) {
                            var $holder = $(domElement).closest('.webgis-ui-holder');
                            downloadOptions.bbox = $holder.find('.webgis-download-mapimage-bbox').webgis_bbox_input('val');
                            downloadOptions.bbox_epsg = map.calcCrsId();
                            downloadOptions.image_epsg = map.crsId();
                            downloadOptions.size = $holder.find('.webgis-download-mapimage-size').webgis_size_input('val');
                            downloadOptions.dpi = $holder.find('.webgis-download-mapimage-dpi').val();
                            downloadOptions.format = $holder.find('.webgis-download-mapimage-format').val();
                            downloadOptions.worldfile = $holder.find('.webgis-download-mapimage-worldfile').val();
                            downloadOptions.displaysize = screen.width + "," + screen.height;
                        }
                        webgis.showProgress('Kartenbild herunterladen...', null);
                        map.downloadImage(downloadOptions, function (result) {
                            webgis.hideProgress('Kartenbild herunterladen...');
                            if (result && result.downloadid) {
                                window.open(webgis.baseUrl + '/rest/download/' + result.downloadid + "?n=" + result.name);
                            } else if (result.success === false) {
                                webgis.alert(result.exception || 'Fehler', 'error');
                            }
                        });
                    }
                    break;;
                case 'showprinttasks':
                    if (map)
                        map.showPrintTasks();
                    break;
                case 'zoom2sketch':
                    if (map) {
                        //console.log('isGraphicsTool', map.isGraphicsTool(map.getActiveTool()), map.graphics._sketch);
                        if (map.isGraphicsTool(map.getActiveTool()) && map.graphics._sketch) {
                            map.graphics._sketch.zoomTo(1000);
                        }
                        else if (map.sketch) {
                            map.sketch.zoomTo(1000);
                        }
                    }
                    break;
                case 'snapping':
                    if (map) {
                        $('body').webgis_modal({
                            title: webgis.l10n.get("snapping"),
                            onload: function ($content) {
                                $content.css('padding', '10px');
                                $content.webgis_snapping({ map: map });
                            },
                            onclose: function () {
                                map.refreshSnapping();
                            },
                            width: '420px',
                            height: '400px'
                        });
                    }
                    break;
                case 'clearselection':
                    map.getSelection('selection').remove();
                    break;
                case 'removequeryresults':
                    map.queryResultFeatures.clear();
                    map.events.fire('hidequeryresults');
                    map.getSelection('query').remove();
                    map.getSelection('selection').remove();
                    break;
                case 'removetoolqueryresults':
                    if (map.getActiveTool()) {
                        map.queryResultFeatures.clearToolResults(map.getActiveTool().id);
                    }
                    break;
                case 'removefromselection':
                    if (data != null && data.length) {
                        map.queryResultFeatures.removeFeatures(data);
                        map.getSelection('query').refresh();
                        map.getSelection('selection').refresh();
                    }
                    break;
                case 'visfilterremoveall':
                    if (map) {
                        map.unsetAllFilters();
                        map.refresh();
                        map.ui.refreshUIElements();
                    }
                    break;
                default:
                    if ($.isFunction(tool.command))
                        tool.command.apply(tool);
                    break;
                case 'showlegend':
                    if ($.presentationToc)
                        $.presentationToc.showLegend(null, map, map.services, " Darstellung, ©,...");
                    break;
                case 'serviceorder':
                    webgis.modalDialog(webgis.l10n.get("service-order") + ": " + webgis.l10n.get("service-order"),
                        function ($context) {
                            $context.webgis_serviceOrder({ map: map });
                        });
                    webgis
                    break;
                case 'setlayersvisible':
                    if (map) {
                        var serviceLayerDefs = {};
                        $.each(tool.argument.split(','), function (i, globalLayerId) {
                            var service = map.getService(globalLayerId.split(':')[0]);
                            if (service) {
                                if (!serviceLayerDefs[service.id])
                                    serviceLayerDefs[service.id] = [];

                                serviceLayerDefs[service.id].push({ id: globalLayerId.split(':')[1], visible: true });
                            }
                        });

                        for (var serviceId in serviceLayerDefs) {
                            map.getService(serviceId).setServiceVisibilityPro(serviceLayerDefs[serviceId], true);
                        }
                    }
                    break
                case 'showsketchanglehelperline':
                    if (map && map.sketch) {
                        map.sketch.setConstructionMode('angle', tool.argument);
                    }
                    break;
                case 'showsketchanglegeographichelperline':
                    if (map && map.sketch) {
                        map.sketch.setConstructionMode('angle_geographic', tool.argument);
                    }
                    break;
                case 'execute-customtool':
                    this.executeCustomTool(map, activeTool, null);
                    break;
            }

            //console.log(tool.command);
            if (tool.command
                && tool.command.startsWith('setgraphics_')
                && tool.command.endsWith('_and_apply_to_selected') > 0) {
                //console.log('apply to selected', tool.value);
                const c = tool.command.substring('setgraphics_'.length, tool.command.length - '_and_apply_to_selected'.length).replaceAll('_', '-');
                //console.log(c);
                if (map && map.graphics && tool.value)
                    map.graphics.applyStyleToSelectedElements(c);
            }
        }
        else if (tool.type === 'serverbutton') {
            webgis.tools.sendToolRequest(map, tool, 'buttonclick', null, tool.customParameters ? tool.customParameters : null);
        }
        else if (tool.type === 'servertool') {
            if (map.getActiveTool() && map.getActiveTool().id === tool.id) {
                if (tool.uiElement) {
                    // Persistent Elemente werden sonst nicht übergeben, weil in "getPersitentToolParameters" (unten) überprüft wird, ob das Element bereits vorhanden ist...
                    // zB Coordinatentabelle
                    // ??? Hat das auswirkungen auf irgendein anderes Tool? Wenn ja, sollte man für die Werkzeuge, die das brauchen (Coordinates) einen Flag setzen setzen zB tool.requiresFullUiRefresh = true
                    //$(tool.uiElement).empty();

                    // ToDo: Hat das jetzt irgendwelche Auswirkungen auf Persistent Element: Hier werden nicht alle Element entfernt, sondern persistent_topic Element in einen neuen bereich kopiert...
                    // Im $.fn.webgis_tool_persist_topic_handler wird erst einmal überprüft, ob sich darin persistent Element befinden, wenn ja => persistent_topic wird vorerst ignoriert
                    $(tool.uiElement).webgis_uibuilder('empty', { map: map });
                }
            }
            webgis.tools.sendToolRequest(map, tool, 'buttonclick');
        }
        else if (tool.type === 'servertoolcommand') {
            customEvent = customEvent || [];
            customEvent['_method'] = tool.command;
            if (tool.argument) {
                customEvent["_servercommand_argument"] = tool.argument;
            }
            if (this.checkConfirmations(map, map.getActiveTool(), 'buttonclick', tool.command, null, confirmations)) {
                webgis.tools.sendToolRequest(map, map.getActiveTool(), 'servertoolcommand', null, customEvent);
            }
        }
        else if (tool.type === 'servertoolcommand_ext') {
            customEvent = customEvent || [];
            customEvent['_method'] = tool.command;
            if (tool.argument) {
                customEvent["_servercommand_argument"] = tool.argument;
            }
            var originalTool = map.getTool(tool.id);
            if (originalTool) { // add all dependencies: scale_dependent, mapbbox_dependent
                for (var p in originalTool) {
                    if (typeof p === 'string' && p.indexOf('_dependent') > 0 && typeof tool[p] === 'undefined') {
                        tool[p] = originalTool[p];
                    } 
                }
                if (originalTool.type === 'serverbutton') {
                    tool.uiElement = originalTool.uiElement;
                    tool.name = originalTool.name;
                    tool.type = originalTool.type;
                }
            }
            webgis.tools.sendToolRequest(map, tool, 'servertoolcommand', tool.event || null, customEvent);
        }
        else if (tool.type === 'clienttool') {
            map.setActiveTool(tool);
            if (tool.ui) {
                tool.ui.onclose = tool.onRelease;
                tool.ui.oncloseArgument = tool;
                tool.ui.title = tool.name;
                tool.ui.map = map;
                $(tool.uiElement).webgis_uibuilder(tool.ui);
                map.ui.refreshUIElements();
            }
            else if (tool.hasui === true) {
                webgis.tools.sendToolRequest(map, tool, 'buttonclick');
            }
        }
        else if (tool.type === 'custombutton') {
            this.executeCustomTool(map, tool);
        }
        else if (tool.type === 'customtool') {
            // Falls Druckwerkzeug aktiv war
            map.viewLense.hide();

            var uiElements = [{ type: 'label', label: tool.description || '' }];
            if (tool.uiElements) {
                for (var i in tool.uiElements) {
                    var uiElement = tool.uiElements[i];
                    uiElements.push(uiElement);
                }
            }

            switch (tool.tooltype) {
                case "sketch0d":
                case "sketch1d":
                case "sketch2d":
                    uiElements.push({
                        text: 'Sketch entfernen',
                        buttontype: 'clientbutton',
                        buttoncommand: 'removesketch',
                        css: 'uibutton uibutton-cancel',
                        type: 'button'
                    });
                    uiElements.push({
                        type: 'button',
                        text: tool.name + ' anzeigen',
                        buttontype: 'clientbutton',
                        buttoncommand: 'execute-customtool'
                    });
                    break;
            }

            $('.webgis-toolbox-holder').webgis_uibuilder({
                map: map,
                tool: tool,
                title: tool.name,
                onclose: function (map) {
                    map.setActiveTool(null);
                    $('.webgis-toolbox-holder').webgis_uibuilder('empty', { map: map }).webgis_toolbox({ map: map });
                },
                oncloseArgument: map,
                elements: uiElements
            });
            map.setActiveTool(tool);
        }
    };
    this.onMapClick = function (map, e) {
        var m = new webgis.tools.mouseEvent(map, e);
        var activeTool = map.getActiveTool();
        if (!activeTool) {
            //this.sendToolRequest(map, { id: 'webgis.tools.identify', tooltype: 'click' }, 'toolevent', m);
        }
        else {
            if (activeTool.type === 'servertool') {
                if (activeTool.tooltype === 'click') {
                    this.sendToolRequest(map, activeTool, 'toolevent', m);
                }
            }
            else if (activeTool.type === 'clienttool') {
                var eventObject = m.eventObject();
                if (activeTool.onmapclick)
                    activeTool.onmapclick(activeTool, eventObject);

                map.events.fire('ativeclienttool-click', map, eventObject);
            }
            else if (activeTool.type === 'customtool') {
                switch (activeTool.tooltype) {
                    case 'sketch0d':
                    case 'sketch1d':
                    case 'sketch2d':
                        return; // to nothing
                    default:
                        this.executeCustomTool(map, activeTool, m.eventObject());
                        break;
                }
            }
        }
    };
    this.onOverlayGeoRefDefinition = function (map, geoRefDef) {
        var m = new webgis.tools.geoRefDefEvent(map, geoRefDef);
        var activeTool = map.getActiveTool();
        //console.log(activeTool);
        if (activeTool) {
            if (activeTool.type === 'servertool') {
                if (activeTool.tooltype === 'overlay_georef_def') {
                    this.sendToolRequest(map, activeTool, 'toolevent', m);
                }
            }
        }
    };
    this.onToolUndoClick = function (map, toolId, undo) {
        var tool = map.getTool(toolId);
        webgis.ajax({
            progress: 'Undo Reqeust ' + undo.title,
            cancelable: false,
            url: webgis.baseUrl + '/rest/toolundo',
            type: 'post',
            data: webgis.hmac.appendHMACData({ toolId: toolId, undo: JSON.stringify(undo) }),
            success: function (result) {
                map.ui.webgisContainer().webgis_modal('close', { id: 'webgis-undo-modal' });
                if (tool && tool.undos) {
                    tool.undos.splice(tool.undos.indexOf(undo), 1);
                    map.ui.refreshUIElements();
                }
                webgis.tools.handleToolResponse(map, map.getActiveTool(), null, null, result);
            },
            error: function () {
                webgis.alert('Ein allgemeiner Fehler ist aufgetreten', 'error');
            }
        });
    };
    this.onMapMousedown = function (map, e) {
        var m = new webgis.tools.mouseEvent(map, e);
        var toolSketch = map.toolSketch();
        if (toolSketch)
            toolSketch.onMapMousedown(map, e);
    };
    this.onMapMouseup = function (map, e) {
        var m = new webgis.tools.mouseEvent(map, e);
        var toolSketch = map.toolSketch();
        if (toolSketch)
            toolSketch.onMapMouseup(map, e);
    };
    this.onMapMouseMove = function (map, e) {
        var m = new webgis.tools.mouseEvent(map, e);
        var toolSketch = map.toolSketch();
        if (toolSketch) {
            toolSketch.onMapMouseMove(map, e.latlng);
        }
    };
    this.onMapKeyDown = function (map, e) {
        var toolSketch = map.toolSketch();
        if (toolSketch) {
            toolSketch.onMapKeyDown(map, e);
        }
    };
    this.onMapKeyUp = function (map, e) {
        var toolSketch = map.toolSketch();
        if (toolSketch) {
            toolSketch.onMapKeyUp(map, e);
        }
    };
    this.uploadFile = function (map, toolid, command, name, file) {
        var tool = map.getTool(toolid);
        var url = webgis.baseUrl + '/rest/toolevent';
        var formData = new FormData();
        var client = new XMLHttpRequest();
        formData.append('toolId', toolid);
        formData.append('eventType', 'servertoolcommand');
        var eventStr = '_method=' + command + '|' + this.toolDependencyString(map, tool, this.toolParameterString(map, tool, command));
        if (tool.device_dependent) {
            eventStr += (eventStr ? '|' : '') + '_deviceinfo=' + JSON.stringify({
                is_mobile_device: webgis.isMobileDevice(),
                screen_width: screen.width,
                screen_height: screen.height,
                advanced_tool_behaviour: map.ui.advancedToolBehaviour()
            });
        }
        formData.append('eventString', eventStr);
        formData.append(name, file);
        var hmac = webgis.hmac.appendHMACData({});
        for (var p in hmac) {
            formData.append(p, hmac[p]);
        }

        client.onerror = function (e) {
        };
        client.onload = function (e) {
            if (client.response) {
                var response = (typeof client.response === "string") ? $.parseJSON(client.response) : client.response;
                var activeTool = map.getActiveTool();
                if (activeTool != null && activeTool.id != tool.id) {  // call handleToolResponse with activetool => upload can be servertool_ext (eg. Upload Sketch)
                    //console.log('change tool ' + tool.id + ' => ' + activeTool.id);
                    tool = activeTool;
                }
                webgis.tools.handleToolResponse(
                    map,
                    tool,
                    {},
                    null,
                    response);
            }
        };
        client.open("POST", url);
        client.send(formData);
    };
    this.fireActiveToolEvents = function (map, event, arg) {
        var activeTool = map.getActiveTool();
        if (activeTool.event_handlers) {
            for (var i in activeTool.event_handlers) {
                if (activeTool.event_handlers[i] === event.channel) {
                    webgis.tools.sendToolRequest(map, activeTool, 'servertoolcommand', null, { _method: '_event_handler_' + event.channel, event_arg: arg ? JSON.stringify(arg) : null });
                }
            }
        }
    };
    this.sendToolRequest = function (map, tool, type, event, customEventStringObject) {
        try {
            var toolId = tool.id;
            if (tool._isDefaultTool === true) {
                toolId = toolId.substring(webgis._defaultToolPrefix.length, toolId.length);
            }
            var servercommand = customEventStringObject && customEventStringObject['_method'] ? customEventStringObject['_method'] : null;
            var eventStr = (event && event.toEventString ? event.toEventString(map, tool) : (event || this.toolParameterString(map, tool, servercommand)));
            if (customEventStringObject) {
                for (var p in customEventStringObject) {
                    if (eventStr !== '')
                        eventStr += '|';
                    eventStr += p + "=" + customEventStringObject[p];
                }
            }
            //if (tool.initial_arg) {
            //    if (eventStr != '') eventStr += '|';
            //    eventStr += '_inital_arg=' + tool.initial_arg;
            //}

            if (map
                && map.isSketchTool(tool)
                //&& (map.isActiveTool(tool.id) || type !== 'buttonclick')
            ) {
                //console.log('send sketch...' + type);
                if (eventStr !== '')
                    eventStr += '|';
                eventStr += '_sketch=' + map.sketch.toWKT() + "|_sketchWgs84=" + map.sketch.toWKT(true);
                eventStr += '|_sketchSrs=' + (map.sketch.originalSrs() && map.sketch.originalSrs() !== 0 ? map.sketch.originalSrs() : map.crsId());
                eventStr += '|_sketchInfo=' + JSON.stringify(map.sketch.metaInfo());
                if (eventStr.indexOf('mapcrs=') < 0)
                    eventStr += "|mapcrs=" + map.crsId();
            }
            eventStr = this.toolDependencyString(map, tool, eventStr);

            eventMeta = null;

            if (webgis.globals && webgis.globals.portal) {
                eventMeta = { portal: webgis.globals.portal.pageId, cat: webgis.globals.portal.mapCategory, map: webgis.globals.portal.mapName || webgis.globals.portal.projectName };
            }

            var data = {
                toolId: toolId,
                eventType: type,
                eventString: eventStr,
                toolOptions: tool.options ? JSON.stringify(tool.options) : null,
                eventMeta: eventMeta ? JSON.stringify(eventMeta) : null
            };
            if (tool.visfilter_dependent || tool.labeling_dependent) {
                map.appendRequestParameters(data);
            }
            if (tool.custom_service_reqeuest_parameters_dependent) {
                map.addServicesCustomRequestParameters(data);
            }

            webgis.ajax({
                progress: 'Tool Request: ' + tool.name || '',
                cancelable: true,
                url: webgis.baseUrl + '/rest/toolevent',
                type: 'post',
                data: webgis.hmac.appendHMACData(data),
                success: function (result) {
                    if (tool.handleToolResponseCustomHandler) {
                        tool.handleToolResponseCustomHandler(result);
                    } else {
                        webgis.tools.handleToolResponse(map, tool, event, customEventStringObject, result);
                    }
                },
                error: function () {
                    webgis.alert('Ein allgemeiner Fehler ist aufgetreten', 'error');
                }
            });
        } catch (ex) {
            alert(ex.toString());
        }
    };
    this.handleToolResponse = function (map, tool, event, customEventStringObject, response) {
        var isEventHandlerResponse =
            (customEventStringObject &&
             customEventStringObject._method &&
             customEventStringObject._method.indexOf('_event_handler_') === 0) ? true : false;

        if (response.success === false && response.exception) {
            webgis.alert(response.exception, response.exception_type === 'infoexception' ? 'info' : 'error');
        }

        var responseWarnings = [], responsWarningIsError = false;
        var responseInfos = [];

        if (response.error_message) {
            responseWarnings.push(response.error_message);
            responsWarningIsError = true;
        }

        if (response.action === 'download' && response.downloadId) {
            window.open(webgis.baseUrl + '/rest/download?id=' + response.downloadId + '&n=' + encodeURI(response.name));
            return;
        }

        var setActiveTool = false;
        if (response.ui && tool.uiElement && isEventHandlerResponse === false) { // nach einem Server Tool Eventhandler aufruf sollte setActiveTool nicht aufgerufen werden, weil sonst Änderungen im Sketch wieder überschrieben werden...
            map.setActiveTool(tool);
        }
        if (response.toolevents) {
            map.addToolEvents(response.toolevents);
        }
        if (response.setactivetooltype) {
            map.setActiveToolType(response.setactivetooltype);
        }
        if (response.features) {
            var queryToolId = response.querytoolid;
            if (queryToolId) {
                if (response.features) {
                    map.queryResultFeatures.showToolQueryResults(queryToolId, response.features);
                    if (response.zoomtoresults && response.features.bounds) {
                        map.zoomToBoundsOrScale(response.features.bounds, webgis.featuresZoomMinScale(response.features));
                    }
                }
            } else {
                map.events.fire('onnewfeatureresponse');
                if (map.ui.appliesQueryResultsUI()) { 
                    map.ui.showQueryResults(response.features, response.zoomtoresults);
                }
                else {
                    map.queryResultFeatures.showClustered(response.features, response.zoomtoresults, true);
                }
            }

            if (response.features.metadata) {
                if (response.features.metadata.warnings) {
                    for (var w in response.features.metadata.warnings) {
                        responseWarnings.push(response.features.metadata.warnings[w]);
                    }
                }
                if (response.features.metadata.infos) {
                    for (var i in response.features.metadata.infos) {
                        responseInfos.push(response.features.metadata.infos[i]);
                    }
                }
            }
        }
        if (response.features_links) {
            map.queryResultFeatures.setLinks(response.features_links);
        }
        
        if (response.sketch && map.sketch) {

            var activeTool = map.getActiveTool();
            if (activeTool && activeTool.tooltype === 'graphics') {
                map.graphics._resetGeoJsonType(response.sketch);
                map.graphics._sketch.fromJson(response.sketch);
                if (response.close_sketch === true) {
                    map.graphics._sketch.close();
                }
                if (response.focus_sketch === true) {
                    map.setSketchFocusView(0.3, map.graphics._sketch);
                }
            } else {
                if (response.sketch_readonly === true) {
                    map.sketch.fromJson(response.sketch, false, true);
                } else {
                    map.sketch.fromJson(response.sketch);
                }
                if (response.close_sketch === true) {
                    map.sketch.appendPart();
                }
                if (response.focus_sketch === true) {
                    map.setSketchFocusView(0.3);
                }
            }
        }

        if (response.remove_sketch === true) {
            //console.log('remove silent---');
            map.sketch.remove(true);
        }

        // War für 3D Messen (dynamisch) angedacht => erst einmal auf Eis gelegt
        //if (response.sketch_hasZ && map.sketch) {
        //    map.sketch.setHasZ(response.sketch_hasZ, response.sketch_getZ_command);
        //}
        if (response.chart) {
            map.showChart(response.chart);
        }
        if (response.setlayervisibility) {
            for (var serviceId in response.setlayervisibility) {
                var service = map.getService(serviceId);
                if (service && response.setlayervisibility[serviceId]) {

                    var layerDefs = [];

                    for (var layerId in response.setlayervisibility[serviceId]) {
                        layerDefs.push({ id: layerId, visible: response.setlayervisibility[serviceId][layerId] === true });
                    }

                    if (layerDefs.length > 0) {
                        service.setServiceVisibilityPro(layerDefs, true);
                    }
                }
            }
        }
        if (response.refreshservices) {
            map.refreshServices(response.refreshservices);
        }
        if (response.toolselection) {
            var service = response.toolselection.serviceid ? map.getService(response.toolselection.serviceid) : null;

            if (service) {
                var selection = map.getSelection(tool.id);
                selection.setTargetQuery(service, response.toolselection.queryid, response.toolselection.ids.toString());
            } else {
                if (map.selections[tool.id]) {
                    map.selections[tool.id].remove();
                }
            }
        }
        if (response.refreshselection) {
            map.refreshSelections();
        }
        if (response.serializationmapjson) {
            map.deserialize(response.serializationmapjson, { ui: false });
        }
        if (response.graphics) {
            if (response.graphics.geojson) {
                map.graphics.fromGeoJson(response.graphics);
            }
            if (response.graphics.activegraphicstool) {
                map.graphics.setTool(response.graphics.activegraphicstool);
            }
        }
        if (response.map_title) {
            //console.log('map-title', response.map_title);
            map.ui.setMapTitle(response.map_title);
        }
        
        if (response.sketchvertex_tooltips) {
            for (var i in response.sketchvertex_tooltips) {
                var tooltip = response.sketchvertex_tooltips[i];
                map.sketch.setMarkerTooltip({ x: tooltip.x, y: tooltip.y }, tooltip.tooltip);
            }
        }
        if (response.sketch_addvertex && response.sketch_addvertex.length === 2) {
            var sketch = map.toolSketch();
            if (sketch) {
                var newVertexAdded = sketch.addOrSetCurrentContextVertex({ x: response.sketch_addvertex[0], y: response.sketch_addvertex[1] });

                if (newVertexAdded) {
                    if (map.getActiveTool() && map.getActiveTool().tooltype === 'graphics') {
                        // if tool is mapmarkup (graphics) set the symbol (if current graphics tool is symbol)
                        map.graphics.tryAddGraphicsSymbolToMap({ lat: response.sketch_addvertex[0], lat: response.sketch_addvertex[1] });
                    }

                    var bounds = sketch.getBounds();

                    if (!map.inExtent([bounds.minLng, bounds.minLat, bounds.maxLng, bounds.maxLat])) {
                        sketch.zoomTo(1000);
                    }
                }
            }
        }
        if (response.replace_query_features) {
            map.queryResultFeatures.replaceFeatures(response.replace_query_features);
            map.queryResultFeatures.replaceFeaturesFromInactiveTabs(response.replace_query_features);
        }
        if (response.remove_query_features) {
            map.queryResultFeatures.removeFeatures(response.remove_query_features);
            map.queryResultFeatures.removeFeaturesFromInactiveTabs(response.remove_query_features);
        }
        if (response.setfilters) {
            for (var i in response.setfilters) {
                var filter = response.setfilters[i];
                map.setFilter(filter.id, filter.args);
                map.ui.refreshUIElements();
            }
        }
        if (response.unsetfilters) {
            for (var i in response.unsetfilters) {
                var filter = response.unsetfilters[i];
                map.unsetFilter(filter.id);
                map.ui.refreshUIElements();
            }
        }
        if (response.setlabeling) {
            for (var i in response.setlabeling) {
                var labeling = response.setlabeling[i];
                map.setLabeling(labeling);
            }
        }
        if (response.unsetlabeling) {
            for (var i in response.unsetlabeling) {
                var labeling = response.unsetlabeling[i];
                map.unsetLabeling(labeling);
            }
        }

        if (response.removestaticoverlay) {
            for (var i in response.removestaticoverlay) {
                var overlayDef = response.removestaticoverlay[i];
                var service = map.getService(overlayDef.id);
                if (service) {
                    map.removeServices([service.id]);
                }
            }
        }
        if (response.addstaticoverlay) {
            var serviceInfos = [], newServiceInfos = [];
            var staticOverlayZIndex = Math.max(map.maxStaticOverlayOrder(), map.maxBackgroundCoveringServiceOrder());

            for (var i in response.addstaticoverlay) {
                var overlayDef = response.addstaticoverlay[i];
                if (overlayDef.id) {
                    serviceInfos.push({
                        id: overlayDef.id,
                        name: overlayDef.name,
                        type: 'static_overlay',
                        overlay_url: overlayDef.overlay_url,
                        opacity: overlayDef.opacity,
                        widthHeightRatio: overlayDef.widthHeightRatio,
                        layers: [{ name: overlayDef.name, id: "0", selectable: false, visible: true }],
                        affinePoints: [overlayDef.topLeft, overlayDef.topRight, overlayDef.bottomLeft],
                        passPoints: overlayDef.passPoints || null
                    });
                }
            }
            for (var i in serviceInfos) {
                var serviceInfo = serviceInfos[i];
                var service = map.getService(serviceInfo.id);
                if (service) {
                    service.setAffinePoints(serviceInfo.affinePoints);
                } else {
                    newServiceInfos.push(serviceInfo);
                }
            }
            if (newServiceInfos.length > 0) {
                var services = map.addServices(newServiceInfos);
                
                for (var s in services) {
                    services[s].setOrder(++staticOverlayZIndex);
                    services[s].refresh();
                }
            }

            for (var i in response.addstaticoverlay) {
                var overlayDef = response.addstaticoverlay[i];
                if (overlayDef.editmode === true) {
                    var serviceIds = map.serviceIds();
                    for (var s in serviceIds) {
                        var service = map.getService(serviceIds[s]);
                        if (!service)
                            continue;

                        if (service.id === overlayDef.id) {
                            service.showPassPoints();
                        } else {
                            service.hidePassPoints();
                        }
                    }
                }
            }
        }
        
        if (response.dynamiccontent) {
            map.addDynamicContent([response.dynamiccontent], true);
        }
        if (response.printcontent) {
            var data = webgis.tools.getToolData("webgis.tools.io.print");
            if (!data.prints)
                data.prints = [];
            data.prints.push(response.printcontent);
            map.showPrintTasks();
        }
        if (response.refresh_snapping) {
            // afer set/unsetfilters
            map.refreshSnapping();
        }

        let responseActiveTool
        if (response.activetool) {
            responseActiveTool = map.getTool(response.activetool.id) || response.activetool;
            let parentTool = map.getTool(response.activetool.parentid) || map.getActiveTool();

            if (!responseActiveTool.uiElement) {
                responseActiveTool.uiElement = response.activetool.uiElement || parentTool.uiElement;
                responseActiveTool.onRelease = response.activetool.onRelease || parentTool.onRelease;
            }
            responseActiveTool.name = response.activetool.name;
            responseActiveTool.tooltype = response.activetool.tooltype;
            responseActiveTool.map = map;

            if (responseActiveTool.is_childtool === true) {
                // Tool darf nicht auf sich selbst als Parent verweisen!!!
                // Edit: einface Selektion durch klick springt nach Selektion damit zurück zum Werkzeugdialog (inkl aktualisierung)
                // Wenn das ParentTool danach auf sich selbst verweist, kann man es nicht mehr verlassen
                if (parentTool == null) {
                    responseActiveTool.parentTool = null;
                } else if (responseActiveTool.id != parentTool.id && parentTool.id != map.getDefaultToolId()) {
                    responseActiveTool.parentTool = parentTool;
                }
            }

            // Beim Editieren wird vom Selektor Tool der Sketch mitgeliefert (response.sketch) und 
            // danach die UI des neuen aktiven Tools (UpdateFeature) über response.activetool abgeholt.
            // Der Sketch wird beim Werkzeug immer in currentSketch gespeichert, damit jedes Werkzeug seinen eigenen Sketch hat.
            // Wichtig: CurrentSketch hier auf das neue Tool setzen. Sonst wird beim map.setActiveTool() wieder der "letzte,alte" sketch dargestellt
            // -> komischen Verhalten Editieren: wenn man ein bestehendes Objekt anklickt, kommt immer der Sketch vom als ersten Editierten Objekt!!!
            if (response.sketch && /*responseActiveTool.currentSketch*/ map.isSketchTool(responseActiveTool)) {
                //console.log('set current sketch', response.sketch);
                responseActiveTool.currentSketch = map.sketch.store();
            }
        }

        if (response.ui && tool.uiElement) {
            if (tool.type === 'serverbutton' || tool.type === 'clientbutton') {
                map.events.fire('requirebuttonui');
                response.ui.onclose = responseActiveTool && responseActiveTool.onRelease ? responseActiveTool.onRelease : tool.onRelease;
                response.ui.oncloseArgument = responseActiveTool || tool;
                response.ui.title = responseActiveTool ? responseActiveTool.name : tool.name;
                response.ui.map = map;
                response.ui.event = event;
                response.ui.tool = responseActiveTool || tool;
                response.ui.toolDialogId = responseActiveTool ? responseActiveTool.id : tool.id;
            } else {
                response.ui.onclose = responseActiveTool && responseActiveTool.onRelease ? responseActiveTool.onRelease : tool.onRelease;
                response.ui.oncloseArgument = responseActiveTool || tool;
                response.ui.title = responseActiveTool ? responseActiveTool.name : tool.name;
                response.ui.map = map;
                response.ui.event = event;
                response.ui.tool = responseActiveTool || tool;
                response.ui.toolDialogId = responseActiveTool ? responseActiveTool.id : tool.id;
            }

            //console.log('event', event);
            $(tool.uiElement || null).webgis_uibuilder(response.ui);

            if (tool.uiElement && !webgis.useMobileCurrent() && tool.allow_ctrlbbox) {
                $(tool.uiElement).webgis_uibuilder('append_ctrl_bbox_info', tool.ui);
            }
            if (tool._isDefaultTool && tool.uiElement) {
                tool.uiElement.find('.webgis-tool-parameter').addClass('webgis-default-tool-parameter').removeClass('webgis-tool-parameter');
                tool.uiElement.find('.webgis-tool-parameter-persistent').addClass('webgis-default-tool-parameter-persistent').removeClass('webgis-tool-parameter-persistent');
            }

            //if (tool.uiElement) {
            //    webgis.effects.unfold3d(tool.uiElement);
            //}

            map.ui.refreshUIElements();
            // Wenn nicht setActiveTool aufgerufen wird, muss mind. applyToolInitialization aufgerufen werden
            // Das braucht man beispielsweise beim Editing. Bei InsertFeature wird mit dieser Funktion dann die eigentlich FeatureMaske und GeometryType geholt, wenn noch nicht verhanden...
            // PersistentFields usw...
            map.applyToolInitialization(tool);
            //map.setActiveTool(tool);

            //map.ui.uncollapseSidebar();
        }
        else if (response.ui && tool.type === "servertoolcommand_ext") {
            response.ui.map = map;
            response.ui.tool = tool;
            response.ui.event = event;

            if (!tool.originalUiElement) {
                response.ui.toolDialogId = tool.id;
            }

            $(tool.originalUiElement || null)
                .webgis_uibuilder(response.ui);
        }
        if (response.clientcommands) {
            for (var i in response.clientcommands) {
                webgis.tools.onButtonClick(map, { type: 'clientbutton', command: response.clientcommands[i] }, null, response.clientcommanddata);
            }
        }
        if (response.remove_secondary_tool_ui && $.fn.webgis_uibuilder) {
            $(null).webgis_uibuilder('removeSecondoaryToolUI', { map: map });
        }

        if (responseActiveTool) {
            webgis.delayed(function (newTool) {
                webgis.tools.onButtonClick(map, newTool);
            }, 1, responseActiveTool);
        }

        if (response.toolcursor) {
            if (response.toolcursor === 'custom') {
                map.setCursor(response.customtoolcursor);
            } else {
                map.setCursor(response.toolcursor);
            }
        }
        if (response.toolundos) {
            var undoTool = response.undotool ? map.getTool(response.undotool) : map.getActiveTool();
            if (undoTool) {
                if (!undoTool.undos)
                    undoTool.undos = [];
                for (var u in response.toolundos) {
                    response.toolundos[u]._ticks = new Date().getTime();
                    undoTool.undos.push(response.toolundos[u]);
                }

                map.ui.refreshUIElements();
            }
        }
        if (response.applyeditingtheme) {
            var editService = map.getService(response.applyeditingtheme.serviceid);
            if (editService) {
                var editTheme = editService.getEditTheme(response.applyeditingtheme.id);
                if (editTheme) {
                    map.setCurrentEditTheme(editTheme);
                    // Apply Edittheme Snapping
                    if (editTheme.snapping) {
                        //console.log(editTheme.snapping);
                        map.clearSnapping();
                        for (var s in editTheme.snapping) {
                            var snappingService = map.getService(editTheme.snapping[s].serviceid);
                            if (snappingService) {
                                var snappingDef = editTheme.snapping[s];
                                map.setSnapping(snappingDef.serviceid + '~' + snappingDef.id, snappingDef.types);   
                            }
                        }
                        map.refreshSnapping();
                    }
                    //////////////////////////////////////
                }
            }
        }

        // Liveshare
        if (response.set_liveshare_clientname) {
            webgis.liveshare.setAnonymousClientname(response.set_liveshare_clientname);
        }
        if (response.init_liveshare_connection) {
            webgis.liveshare.init(map, response.init_liveshare_connection);
        }
        if (response.join_liveshare_session) {
            webgis.liveshare.joinSession(response.join_liveshare_session);
        }
        if (response.leave_liveshare_session) {
            webgis.liveshare.leaveSession(response.leave_liveshare_session);
        }
        if (response.exit_liveshare) {
            webgis.liveshare.closeConnection();
        }

        // MapViewLense
        if (response.mapviewlense) {
            map.viewLense.show(response.mapviewlense, response.mapviewlense.options);
        }
        else {
            map.viewLense.hide();
        }

        if (response.zoom_to_4326) {
            map.zoomTo(response.zoom_to_4326);
        }

        if (response.named_sketches && response.named_sketches.length > 0 &&
            (map.isSketchTool(map.getActiveTool()) || map.isGraphicsTool(map.getActiveTool()))) {
            $(null).select_sketch({
                map: map,
                named_sketches: response.named_sketches,
                close_sketch: response.close_sketch
            });
        }

        // 3d
        if (response.three_d_values && response.three_d_values.length > 0) {
            var threeDSchene;
            webgis.require('webgis-three-d', function () {
                $('body').webgis_modal({
                    title: '3D Modell',
                    animate: false,
                    onload: function ($content) {
                        webgis.delayed(function () {
                            $content.attr('id', 'webgis-3d-container');
                            threeDSchene = new webgis.threeD(map, 'webgis-3d-container', response);
                        }, 100);
                    },
                    onclose: function () {
                        threeDSchene.dispose();
                        threeDSchene = null;
                    },
                    width: 'calc(100% - 16px)',
                    height: 'calc(100% - 16px)'
                })
            });
        }

        // Wenn notwendig, am Schluss noch einmal aktuelles Werkzeug refreshen...
        //if (response.refreshActiveTool === true) {
        //    var activeTool = map.getActiveTool();
        //    if (activeTool) {
        //        webgis.delayed(function () {
        //            webgis.tools.onButtonClick(map, activeTool);
        //        }, 1);
        //    }
        //}
        if (response.triggerToolButtonClick) {
            var tool = map.getTool(response.triggerToolButtonClick);
            if (tool) {
                webgis.delayed(function () {
                    webgis.tools.onButtonClick(map, tool);
                }, 1);
            }
        }

        if (response.fire_custom_map_event) {
            map.events.fire(response.fire_custom_map_event);
        }

        if (responseInfos.length > 0) {
            var infoText = '';
            for (var i in responseInfos) {
                infoText += (infoText ? ', ' : '') + responseInfos[i];
            }

            webgis.toastMessage("Info", infoText);
        }
        if (responseWarnings.length > 0) {
            var warningText = '';
            for (var w in responseWarnings) {
                warningText += (warningText ? ', ' : '') + responseWarnings[w];
            }

            var $errorHolder = $(map._webgisContainer).find('.webgis-errors-holder');
            if ($errorHolder.length > 0) {
                $errorHolder.webgis_errors('add', {
                    map: map,
                    title: tool.name,
                    message: warningText,
                    suppressShowTab: true/* responsWarningIsError !== true*/
                });

            } else if (responsWarningIsError === true) {
                webgis.alert(warningText, 'error');
            }
        }
    };
    this.toolParameterString = function (map, tool, servercommand) {
        var ret = '';
        tool = tool || (map ? map.getActiveTool() : null);
        if (map && map.ui)
            map.ui.refreshUIElements(servercommand);
        var parameterClass = tool._isDefaultTool === true ? 'webgis-default-tool-parameter' : 'webgis-tool-parameter';
        var parameterPersistentClass = tool._isDefaultTool === true ? 'webgis-default-tool-parameter-persistent' : 'webgis-tool-parameter-persistent';

        //console.log('servercommand', servercommand);

        function writeParameter(element) {
            if ($(element).attr('data-parameter-servercommands')) {
                var parameterCommands = $(element).attr('data-parameter-servercommands').split(',');
                return $.inArray(servercommand, parameterCommands) >= 0;
            }

            return true;
        };

        var me = this;
        $('.' + parameterClass).each(function (i, e) {
            if (writeParameter(e)) {
                ret += (ret !== '' ? '|' : '');
                ret += e.id + '=' + webgis.tools.toParameterValue(me.getElementValue(e));

                // Extended Values... (eg. print scales)
                if ($.fn.webgis_extCombo && $(e).hasClass('webgis-extendable-combo')) {
                    ret += '|' + e.id + '.values=' + webgis.tools.toParameterValue($(e).webgis_extCombo('values').toString());
                }
            }
        });

        $('.' + parameterPersistentClass).each(function (i, e) {
            var val = me.getElementValue(e);
            if (map && tool && val !== null) {
                map.setPersistentToolParameter(tool, e.id, val);
            }
            if (writeParameter(e)) {

                let defaultValue;
                if (servercommand === "_event_handler_onupdatecombo") {
                    // Beim Updaten von (kaskadierenden) Combos den persistent wert setzten
                    // Falls der Wert null ist. Damit werden auch Combos in der 2.ten kaskading Stufe befüllt
                    // auch wenn das darüber liegenden erst durch den Request befüllt wird.
                    defaultValue=map.getPersistentToolParameter(tool, e.id)
                };

                ret += (ret !== '' ? '|' : '');
                ret += e.id + '=' + webgis.tools.toParameterValue(val, defaultValue);
            }
        });

        var ptp = map != null ? map.getPersistentToolParameters(tool) : [];
        for (var id in ptp) {
            if ($('#' + id).length === 0) {
                ret += (ret !== '' ? '|' : '');
                ret += id + '=' + webgis.tools.toParameterValue(ptp[id]);
            }
        }
        if (tool._isDefaultTool) {
            ret += (ret !== '' ? '|' : '');
            ret += '_as-default-tool=true';
        }
        return ret;
    };
    this.toolDependencyString = function (map, tool, eventStr) {
        eventStr = eventStr || '';

        if (tool.selectioninfo_dependent) {
            var selectionInfo = map.getSelectionInfo('selection');
            if (selectionInfo)
                eventStr += (eventStr ? '|' : '') + '_selectioninfo=' + JSON.stringify(selectionInfo);
        }
        if (tool.scale_dependent) {
            eventStr += (eventStr ? '|' : '') + '_mapscale=' + map.scale();
        }
        if (tool.mapcrs_dependent) {
            eventStr += (eventStr ? '|' : '') + '_mapcrs=' + map.crsId() + '|_calccrs=' + map.calcCrsId();
            if (map.hasDynamicCalcCrs()) {
                eventStr += '|_mapcrs_is_dynamic=true';
            }
        }
        if (tool.mapbbox_dependent) {
            eventStr += (eventStr ? '|' : '') + '_mapbbox=' + map.getExtent();
        }
        if (tool.printlayout_dependent) {
            eventStr += (eventStr ? '|' : '') + '_printlayout_rotation=' + map.viewLense.currentRotation();
        }
        if (tool.aside_dialog_exists_dependent) {
            eventStr += (eventStr ? '|' : '') + '_asidedialog_exists=' + ($('#' + $(null).webgis_dockPanel('panelId', { id: tool.id + "_aside" })).length > 0 ? "true" : "false");
        }
        if (tool.liveshare_clientname_dependent) {
            eventStr += (eventStr ? '|' : '') + '_liveshare_clientname=' + (webgis.liveshare ? webgis.liveshare.getClientname() : '');
        }
        if (tool.mapimagesize_dependent) {
            eventStr += (eventStr ? '|' : '') + '_mapsize=' + map.getSize();
        }
        if (tool.query_markers_visiblity_dependent) {
            eventStr += (eventStr ? '|' : '') + '_query_markers_visible=' + map.queryResultFeatures.markersVisible();
        }
        if (tool.coordinate_markers_visiblity_dependent) {
            eventStr += (eventStr ? '|' : '') + '_coordinate_markers_visible=' + map.queryResultFeatures.toolMarkersVisible('webgis.tools.coordinates'); 
        }
        if (tool.chainage_markers_visiblity_dependent) {
            eventStr += (eventStr ? '|' : '') + '_chainage_markers_visible=' + map.queryResultFeatures.toolMarkersVisible('webgis.tools.chainage'); 
        }
        if (tool.anonymous_userid_dependent) {
            eventStr += (eventStr ? '|' : '') + '_anonymous_userid=' + webgis.localStorage.getAnonymousUserId();
        }
        if (tool.device_dependent) {
            eventStr += (eventStr ? '|' : '') + '_deviceinfo=' + JSON.stringify({
                is_mobile_device: webgis.isMobileDevice(),
                screen_width: screen.width,
                screen_height: screen.height,
                advanced_tool_behaviour: map.ui.advancedToolBehaviour()
            });
        }
        if (tool.static_overlay_services_dependent) {
            var serviceIds = map.serviceIds(), overlayServiceIds = [];
            for (var s in serviceIds) {
                var service = map.getService(serviceIds[s]);
                if (service && service.type === "static_overlay") {
                    overlayServiceIds.push(service.id);
                }
            }
            eventStr += (eventStr ? '|' : '') + '_overlay_services=' + overlayServiceIds.toString();
        }
        if (tool.ui_element_dependent) {
            eventStr += (eventStr ? '|' : '') + '_ui_elements=';
            map.ui.webgisContainer().find('.webgis-ui-element-dependency').each(function (i, element) {
                eventStr += (i > 0 ? ',' : '') + element.nodeName.toLowerCase() + '.' + element.className.replaceAll(' ', '.');
            });
        }

        return eventStr;
    };
    this.getElementValue = function (e) {
        var val = '';
        var $e = $(e);
        var nodeName = e.nodeName.toUpperCase();

        switch (nodeName) {
            case 'TABLE':
                $e.children('tr').each(function (r, row) {
                    $(row).children('td[data-value]').each(function (c, cell) {
                        if (val) val += ';';
                        val += $(cell).attr('data-value');
                    });
                });
                break;
            default:
                if ($e.data('webgis-value-element')) {
                    val = $e.data('webgis-value-element').val();  // zb: webgis-bbox-input beim 3d tools
                }
                else {
                    val = $e.val();
                    if (!val) {
                        switch (nodeName) {
                            case 'SELECT':  // braucht man mehrstufige kaskadierende Auswahllisten bei Editmasken. Sonst werden mehrfach abhängige Auswahllisten in der UPDATE Maske nicht befüllt!!!
                                if ($e.children('option').length === 0 && $e.data('initial-value'))
                                    val = $e.data('initial-value');
                                break;
                        }
                    }
                }
                break;
        }

        if ($e.hasClass('webgis-tool-parameter-required clientside') && !val) {
            throw $e.data('required-message') || $e.attr('id') + " is requried";
        }

        return val;
    };
    this.toParameterValue = function (val, defaultValue) {
        if (!val) {
            val = defaultValue || '';
        }
        val = val.replace(/\|/g, '~~~&PIPE;~~~');

        return val;
    };
    this.checkConfirmations = function (map, tool, eventtype, command, customEvent, confirmations) {
        if (tool && tool.id && !tool.confirmmessages) {
            tool = map.getTool(tool.id);  // get original Tool
        }

        confirmations = confirmations || [];

        if (tool && tool.confirmmessages) {
            for (var t in tool.confirmmessages) {
                var confirmMessage = tool.confirmmessages[t];
                if (confirmMessage.eventtype !== eventtype)
                    continue;
                if (confirmMessage.command === command ||
                    (!confirmMessage.command && !command)) {

                    var message = confirmMessage.message;

                    $(map._webgisContainer).find('.webgis-tool-parameter').each(function (i, e) {
                        var $e = $(e);
                        var val = e.tagName == "SELECT" ? $e.find("option[value='" + $e.val() + "']").text() : $e.val();

                        message = message.replaceAll("{" + $e.attr('id') + "}", val);
                    });

                    if (confirmations[message] === true || confirmations[message] === false) {
                        return confirmations[message];
                    }

                    switch (confirmMessage.type) {
                        case 'yesno':
                            if (!confirm(message)) {
                                confirmations[message] = false;
                                return false;
                            }
                            break;
                        default:
                            webgis.alert(message, 'info');
                            break;
                    }

                    confirmations[message] = true;
                    console.log(confirmations);
                }
            }
        }

        return true;
    };

    this.allowSketchVertexSelection = function (map, tool) {
        //console.log('allowSketchVertexSelection', tool.tooltype);
        return webgis.usability.allowSelectSketchVertices === true &&
            tool != null &&
            (tool.tooltype === 'sketch1d' || tool.tooltype === 'sketch2d' ||
                (tool.tooltype === 'sketchany' && map.sketch && $.inArray(map.sketch.getGeometryType(), ["polyline", "polygon"]) >= 0) ||
                (tool.tooltype === 'graphics' && $.inArray(map.graphics.getTool(), ["line", "polygon", "dimline", "hectoline"]) >= 0));
    };

    this.replaceCustomToolUrl = function (map, url, eventObject) {
        var extent = map.getExtent(),
            projecttedExtent = map.getProjectedExtent();

        url = url.replaceAll('{map.minx}', extent[0].toString());
        url = url.replaceAll('{map.miny}', extent[1].toString());
        url = url.replaceAll('{map.maxx}', extent[2].toString());
        url = url.replaceAll('{map.maxy}', extent[3].toString());
        url = url.replaceAll('{map.bbox}', extent[0].toString() + ',' + extent[1].toString() + ',' + extent[2].toString() + ',' + extent[3].toString());
        url = url.replaceAll('{map.centerx}', (extent[0] * .5 + extent[2] * .5).toString());
        url = url.replaceAll('{map.centery}', (extent[1] * .5 + extent[3] * .5).toString());
        url = url.replaceAll('{map.scale}', (map.scale()).toString());

        url = url.replaceAll('{map.MINX}', projecttedExtent[0].toString());
        url = url.replaceAll('{map.MINY}', projecttedExtent[1].toString());
        url = url.replaceAll('{map.MAXX}', projecttedExtent[2].toString());
        url = url.replaceAll('{map.MAXY}', projecttedExtent[3].toString());
        url = url.replaceAll('{map.BBOX}', projecttedExtent[0].toString() + ',' + projecttedExtent[1].toString() + ',' + projecttedExtent[2].toString() + ',' + projecttedExtent[3].toString());
        url = url.replaceAll('{map.CENTERX}', (projecttedExtent[0] * .5 + projecttedExtent[2] * .5).toString());
        url = url.replaceAll('{map.CENTERY}', (projecttedExtent[1] * .5 + projecttedExtent[3] * .5).toString());
        url = url.replaceAll('{map.SCALE}', (map.scale()).toString());

        if (eventObject) {
            if (eventObject.world) {
                if (eventObject.world.lat && eventObject.world.lng) {
                    url = url.replaceAll('{x}', eventObject.world.lng.toString());
                    url = url.replaceAll('{y}', eventObject.world.lat.toString());
                }
                if (eventObject.world.X && eventObject.world.Y) {
                    url = url.replaceAll('{X}', eventObject.world.X.toString());
                    url = url.replaceAll('{Y}', eventObject.world.Y.toString());
                }

                if (eventObject.world.bounds) {
                    url = url.replaceAll("{minx}", eventObject.world.bounds[0].toString());
                    url = url.replaceAll("{miny}", eventObject.world.bounds[1].toString());
                    url = url.replaceAll("{maxx}", eventObject.world.bounds[2].toString());
                    url = url.replaceAll("{maxy}", eventObject.world.bounds[3].toString());
                    url = url.replaceAll("{bbox}", eventObject.world.bounds[0].toString() + ',' + eventObject.world.bounds[1].toString() + ',' + eventObject.world.bounds[2].toString() + ',' + eventObject.world.bounds[3].toString());
                }
                if (eventObject.world.BOUNDS) {
                    url = url.replaceAll("{MINX}", eventObject.world.BOUNDS[0].toString());
                    url = url.replaceAll("{MINY}", eventObject.world.BOUNDS[1].toString());
                    url = url.replaceAll("{MAXX}", eventObject.world.BOUNDS[2].toString());
                    url = url.replaceAll("{MAXY}", eventObject.world.BOUNDS[3].toString());
                    url = url.replaceAll("{BBOX}", eventObject.world.BOUNDS[0].toString() + ',' + eventObject.world.BOUNDS[1].toString() + ',' + eventObject.world.BOUNDS[2].toString() + ',' + eventObject.world.BOUNDS[3].toString());
                }
            }
        }

        return url;
    };
    this.executeCustomTool = function (map, tool, eventObject) {
        if (tool.command) {
            let command = this.replaceCustomToolUrl(map, tool.command, eventObject);

            switch (tool.tooltype) {
                case 'sketch0d':
                case 'sketch1d':
                case 'sketch2d':
                    var sketch = map.sketch;
                    if (!sketch || sketch.isValid() === false) {
                        webgis.alert('Sketch nicht gültig', 'error');
                        return;
                    }

                    var wkt4326 = sketch.toWKT2(true);
                    var wkt = sketch.toWKT2(false, false);
                    var wkt_digits_1 = sketch.toWKT2(false, false, 1);
                    var wkt_digits_2 = sketch.toWKT2(false, false, 2);
                    var wkt_digits_3 = sketch.toWKT2(false, false, 3);

                    command = command.replaceAll('{wkt-4326}', wkt4326);
                    command = command.replaceAll('{wkt}', wkt);
                    command = command.replaceAll('{wkt_digits_1}', wkt_digits_1);
                    command = command.replaceAll('{wkt_digits_2}', wkt_digits_2);
                    command = command.replaceAll('{wkt_digits_3}', wkt_digits_3);
                    command = command.replaceAll('{calc-srs}', map.calcCrsId());
                    command = command.replaceAll('{sketch-srs}', map.sketch.tryGetSketchSrs());

                    break;
                case 'click':
                    //console.log('custom-tool-click', eventObject);
                    if (map && eventObject && eventObject.world && eventObject.world.lat && eventObject.world.lng) {
                        map.removeMarkerGroup('custom-temp-marker');
                        map.toMarkerGroup('custom-temp-marker', map.addMarker({
                            lat: eventObject.world.lat, lng: eventObject.world.lng,
                            text: '<div>' + tool.name + '</div>',
                            openPopup: true,
                            buttons: [{
                                label: 'Marker entfernen',
                                onclick: function (map, marker) { map.removeMarker(marker); }
                            }]
                        }));
                    }
                    break;
            }

            // uiElements
            $(map._webgisContainer).find('.' + tool.id).each(function (i, input) {
                var $input = $(input);
                //console.log($input.attr('id'), $input.val());
                command = command.replaceAll('{' + $input.attr('id') + '}', $input.val());
            });

            if (tool.command_target === 'self') {
                document.location = command
            }
            else if (tool.command_target === 'dialog') {
                webgis.iFrameDialog(command, tool.name);
            }
            else if (typeof tool.command_target === 'function') {
                var response = fetch(command/*, {
                    method: 'GET',
                    mode: 'no-cors'
                }*/)
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error('Response not ok');
                    }

                    const contentType = response.headers.get('content-type');
                    console.log('content-type', contentType);

                    if (contentType && contentType.includes('application/json')) {
                        return response.json();
                    } else {
                        return response.text();
                    }
                })
                .then(function (result) {
                    var $ui = $('.webgis-tooldialog-content.' + tool.id);
                    tool.command_target({
                        result: result,
                        map: map,
                        uiElement: $ui.length > 0 ? $ui.get(0) : null
                    });
                })
                .catch(function (error) {
                    console.error('fetch error:', error);
                });
            }
            else {
                window.open(command);
            }
        }
    };
};

webgis.tools.mouseEvent = function (map, e) {
    var _m;
    if (webgis.mapFramework === "leaflet") {
        if (!e.latlng) {
            _m = e;
            return;
        }
        var xy = { x: e.latlng.lng, y: e.latlng.lat };
        if (map.calcCrs())
            xy = webgis.fromWGS84(map.calcCrs(), e.latlng.lat, e.latlng.lng);
        _m = {
            world: {
                lng: e.latlng.lng,
                lat: e.latlng.lat,
                X: xy.x,
                Y: xy.y
            },
            click: {
                x: e.containerPoint ? e.containerPoint.x : null,
                y: e.containerPoint ? e.containerPoint.y : null
            },
            meta: {
                scale: map.scale()
            },
            e: e.originalEvent
        };
    }
    else {
        _m = e;
    }
    this.longitude = function () {
        return _m.world.lng;
    };
    this.latitude = function () {
        return _m.world.lat;
    };
    this.eventObject = function () {
        return _m;
    };
    this.toClickString = function (map, tool) {
        var ret = '';
        if (_m && _m.world) {
            ret = 'lng=' + _m.world.lng + '|lat=' + _m.world.lat + '|crs=4326';
        }
        if (_m && _m.click) {
            ret += (ret !== '' ? '|' : '') + 'x=' + _m.click.x + '|y=' + _m.click.y;
        }
        if (_m.meta) {
            ret += (ret !== '' ? '|' : '') + 'event_scale=' + _m.meta.scale;
        }
        if (map) {
            ret += (ret !== '' ? '|' : '') + 'mapcrs=' + map.crsId();
        }
        var toolParameters = webgis.tools.toolParameterString(map, tool);
        if (toolParameters && toolParameters !== '') {
            ret += (ret !== '' ? '|' : '');
            ret += toolParameters;
        }
        return ret;
    };
    this.toEventString = function (map, tool) {
        if (tool.tooltype === 'click') {
            return this.toClickString(map, tool);
        }
        else {
            return webgis.tools.toolParameterString(map, tool);
        }
    };
};
webgis.tools.boxEvent = function (map, e, box, crs) {
    var _box = box;
    var _crs = crs;

    this.world = {
        bounds: box,
        lng: box[0] * .5 + box[2] * .5,
        lat: box[1] * .5 + box[3] * .5
    };
    if (crs) {
        var ll = webgis.project(crs, [box[0], box[1]]);
        var ur = webgis.project(crs, [box[2], box[3]]);
        this.world.BOUNDS = [ll[0], ll[1], ur[0], ur[1]];
        this.world.X = ll[0] * .5 + ur[0] * .5;
        this.world.Y = ll[1] * .5 + ur[1] * .5;
    }

    var p1 = map.frameworkElement.latLngToLayerPoint([box[0], box[1]]);
    var p2 = map.frameworkElement.latLngToLayerPoint([box[2], box[3]]);
    //console.log(p1, p2);

    this.toEventString = function (map, tool) {
        var ret = '';
        ret += "box=" + box[0] + "," + box[1] + "," + box[2] + "," + box[3] + "|crs=" + (crs ? crs : "4326") + "|";
        ret += "boxsize=" + Math.abs((p2.y - p1.y)) + "," + Math.abs((p2.x - p1.x)) + "|";
        ret += 'mapcrs=' + map.crsId();
        var toolParameters = webgis.tools.toolParameterString(map, tool);
        if (toolParameters && toolParameters !== '') {
            ret += (ret !== '' ? '|' : '');
            ret += toolParameters;
        }
        return ret;
    };
};
webgis.tools.geoRefDefEvent = function (map, geoRefDef) {
    var _geoRefDef = geoRefDef;

    this.toEventString = function (map, tool) {
        var ret = '';
        ret += "overlay_georef_def=" + JSON.stringify(_geoRefDef);
        var toolParameters = webgis.tools.toolParameterString(map, tool);
        if (toolParameters && toolParameters !== '') {
            ret += (ret !== '' ? '|' : '');
            ret += toolParameters;
        }
        return ret;
    }
};
webgis.tools.wrapperEvent = function (event, customParameters) {
    let _event = event;
    let _customParameters = customParameters;

    this.toEventString = function (map, tool) {
        var ret = _event && _event.toEventString ? _event.toEventString(map, tool) : '';

        if (_customParameters) {
            for (var name in _customParameters) {
                if (ret) ret += '|';
                ret += name + '=' + webgis.tools.toParameterValue(_customParameters[name]);
            }
        }

        return ret;
    };
};

