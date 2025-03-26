webgis.map._ui = function (map) {
    var $ = webgis.$;
    this._map = map;
    var _allowAddServices = false;
    var _advancedToolBehaviour = false;
    var _tabsInSidebar = false;
    var _focusSelectors = {
        MapOverlayUIEments: ".webgis-map-overlay-ui-element",
        TabPresentions: "#tab-presentations.webgis-tabs-tab",
        TabQueryResults: "#tab-queryresults.webgis-tabs-tab",
        TabTools: "#tab-tools.webgis-tabs-tab",
        TabSettings: "#tab-settings.webgis-tabs-tab"
    };

    this.allowAddServices = function () { return _allowAddServices !== false && _allowAddServices !== 0; }
    this.advancedToolBehaviour = function () { return _advancedToolBehaviour };
    this.tabsInSidebar = function () { return _tabsInSidebar; }

    this.createPresentationToc = function (selector, options) {
        options = options = webgis.loadOptions(options);
        options.map = map;
        if ($.fn.webgis_presentationToc) {
            _allowAddServices |= options && options.gdi_button;
            return $(typeof selector === 'string' ? '#' + selector : selector).webgis_presentationToc(options);
        }
    };
    this.createServicesToc = function (selector, options) {
        options = options = webgis.loadOptions(options);
        options.map = map;
        if ($.fn.webgis_servicesToc) {
            _allowAddServices |= options && options.gdi_button;
            return $(typeof selector === 'string' ? '#' + selector : selector).webgis_servicesToc(options);
        }
    };
    this.createAddServicesToc = function (selector, options) {
        options = options = webgis.loadOptions(options);
        options.map = map;
        if ($.fn.webgis_addServicesToc)
            return $(typeof selector === 'string' ? '#' + selector : selector).webgis_addServicesToc(options);
    };
    this.createToolbox = function (selector, options) {
        options = options = webgis.loadOptions(options);
        options.map = map;
        if ($.fn.webgis_toolbox)
            return $(typeof selector === 'string' ? '#' + selector : selector).webgis_toolbox(options);
    };
    this.createHourglass = function (selector, options) {
        selector = this._stringSelector(selector, 'createHourglass');
        if (selector) {
            this._serializeOptions.push({
                element: 'hourglass', selector: selector, options: this._cloneOptions(options)
            });
            options = options = webgis.loadOptions(options);
            options.map = map;
            if ($.fn.webgis_hourglass)
                return $(selector).webgis_hourglass(options);
        }
    };
    this.createSearch = function (selector, options) {
        selector = this._stringSelector(selector, 'createSearch');
        if (selector) {
            this.createTopbar(selector, options);
            /*
            this._serializeOptions.push({ element: 'search', selector: selector, options: this._cloneOptions(options) });
            options = options = webgis.loadOptions(options);
            options.map = map;
            
            if ($.fn.webgis_search)
                return $(selector).webgis_search(options);
                */
        }
    };
    this.createTopbar = function (selector, options) {
        selector = this._stringSelector(selector, 'createTopbar');
        if (selector) {
            this._serializeOptions.push({
                element: 'topbar', selector: selector, options: this._cloneOptions(options)
            });
            options = options = webgis.loadOptions(options);
            options.map = map;
            if ($.fn.webgis_topbar)
                return $(selector).webgis_topbar(options);
        }
    };
    this.createTabs = function (selector, options) {
        if (options.options_tools) {
            for (var c in options.options_tools.containers) {
                var container = options.options_tools.containers[c];
                if (container.tools) {
                    for (var t = 0; t < container.tools.length; t++) {
                        container.tools[t] = webgis.compatiblity.toolId(container.tools[t]);
                    }
                }
            }
        }
        selector = this._stringSelector(selector, 'createTabs');
        if (selector) {
            var clonedOptions = this._cloneOptions(options);
            //
            // auf Standardwerte setzten, damit es nach dem speichern für jedes Layout (Desktop & Mobil funitoniert)
            //

            //
            // Wenn es eine Toolbar gibt (Desktop Layout muss add_tools immer true gesetzt werden)
            // Das wird eigentlich im Layout unter tabs-layout-container-options auf false gesetzt, damit die Tools nicht in einem Tab erscheinen sonder in #toolbar
            // => was hier auf clonedOptions gesetzt wird, wird dann auch in gespeicherten Karten und links gespeichert
            // 
            var $toolbarContainer = $(this._map._webgisContainer).find('#toolbar');
            //console.log('$toolbarContainer.length',$toolbarContainer.length);

            clonedOptions.left = null;
            clonedOptions.right = 0;
            clonedOptions.bottom = 24;
            clonedOptions.top = null;
            clonedOptions.width = 320;
            clonedOptions.add_tools = options.add_tools ||
                ($toolbarContainer.length > 0 && $toolbarContainer.css('display') != 'none');
            clonedOptions.content_size = null;

            _allowAddServices |= (clonedOptions.options_presentations && clonedOptions.options_presentations.gdi_button === true) ||
                (clonedOptions.options_settings && clonedOptions.options_settings.gdi_button === true);

            this._serializeOptions.push({
                element: 'tabs', selector: /*selector*/ '#webgis-container', options: clonedOptions  // => '#webgis-container' => auch hier Standard setzen, damit gespeicherte Karte in jedem Layout funktioniert...
            });
            options = webgis.loadOptions(options);
            options.map = map;
            if ($.fn.webgis_tabs) {
                if (webgis.useMobileCurrent() === true) {
                    options.selected = null;
                }

                if (options.add_tools === false && options.add_tool_content === true) {  // tablet oder desktop modus (ohne toolbar in den Tabs)
                    _advancedToolBehaviour = true;
                }

                var webgisSidebar = $(selector).closest('#webgis-sidebar');
                if (_advancedToolBehaviour && webgisSidebar.length > 0) {
                    _tabsInSidebar = true;
                    if (webgisSidebar.attr('data-options-width')) {
                        options.width = webgisSidebar.attr('data-options-width');
                    }
                }

                return $(selector).webgis_tabs(options);
            }
        }
    };
    this.addUserTool = function (selector, options) {
        options = options = webgis.loadOptions(options);
        options.map = map;
        if ($.fn.webgis_toolbox)
            $('#' + selector).webgis_toolbox('addtool', options);
    };

    this.featureResultTable = function (feature, max, linebreak, hideTitle, buttonArray, tableFields) {
        //linebreak = true;

        var buttons = '';

        if (webgis.usability.singleResultButtons && webgis.usability.singleResultButtons.length > 0) {
            if (!buttonArray) {
                buttons = "<div style='text-align:right;height:30px;' class='webgis-singleresultbuttons'>";
            }

            for (var b in webgis.usability.singleResultButtons) {
                var singleResultButton = webgis.usability.singleResultButtons[b];

                var attributes = '';
                if (singleResultButton.url) {
                    var url = singleResultButton.url;
                    if (feature) {
                        if (feature.geometry && feature.geometry.coordinates && feature.geometry.coordinates.length >= 2) {
                            url = url.replaceAll('{lon}', feature.geometry.coordinates[0]);
                            url = url.replaceAll('{lat}', feature.geometry.coordinates[1]);
                        }
                    }
                    attributes += " target='_blank' href='" + url + "'";
                }

                if (buttonArray) {
                    buttonArray.push({
                        html: "<a" + attributes + "></a>",
                        img: singleResultButton.img,
                        text: singleResultButton.name
                    });
                } else {
                    buttons += "<a" + attributes + " class='webgis-button webgis-singleresultbutton'>" + singleResultButton.name + "</a>";
                }
            }

            if (!buttonArray) {
                buttons += "</div>";
            }
        }

        var propertiesArray = Array.isArray(feature.properties) ? feature.properties : [feature.properties];
        if (propertiesArray.length == 0) propertiesArray = [{}];

        var tab = "", hasMore = false, majorCols = 3, hasTables = false, isUnion = propertiesArray.length > 1, unionIndex = 0;

        for (var p in propertiesArray) {
            var properties = propertiesArray[p];
            var count = 0;
            var tabContent = '';
           
            for (var property in properties) {
                if (property.substring(0, 1) === "_" && property !== '_distanceString') {
                    continue;
                }
                if (properties.hasOwnProperty(property)) {
                    var vals = properties[property];
                    if (!vals || vals === '')
                        continue;

                    var tableField = tableFields ? $.grep(tableFields, function (f) { return f.name === property }) : null;
                    if (tableField && tableField.length === 1 && tableField[0].visible === false)
                        continue;

                    if (!Array.isArray(vals)) {
                        vals = [vals];
                    }

                    if (max && count >= max) {
                        hasMore = true;
                        break;
                    }

                    if (isUnion && count === 0) {
                        tabContent += "<tr onclick='$(this).closest(\".webgis-result-table\").toggleClass(\"collapsed\");'>";
                    } else {
                        tabContent += "<tr>";
                    }
                    tabContent += "<td class='webgis-result-table-header' style='white-space:nowrap' " + (linebreak === true ? "colspan=" + vals.length * 2 : "") + " >" +
                        (count == 0 && isUnion === true ? (unionIndex + 1) + ": " : "") +
                        (property === '_distanceString' ? '📐' : property) +
                        "</td>";
                    if (linebreak) {
                        tabContent += "</tr><tr>";
                    }

                    let valCount = vals.length, copyButtonCount = 0;

                    for (let v in vals) {
                        let val = vals[v], valType = typeof (val);

                        if (valType === 'string' && val.indexOf("<a") === 0) {
                            let $val = $(val);
                            if ($val.attr('target') === 'dialog') {
                                let href = $val.attr('href');
                                $val
                                    .attr('href', '')
                                    .attr('onclick', "webgis.iFrameDialog('" + href + "','" + $val.text() + "');return false;");
                                val = $val.prop('outerHTML');
                            }
                        }

                        tabContent += "<td class='webgis-result-table-cell " + v + "' style='" + (linebreak === false ? "white-space:nowrap;" : "") + (valCount > 1 ? "width:auto;" : "") + "'>" + val + "</td>";

                        if (val && valType === 'string' && (val.indexOf('<') < 0 || val.indexOf("<a") === 0)) {
                            tabContent += "<td class='webgis-result-table-copybutton'><div class='webgis-copy' onclick=\"webgis.copy(this.parentNode.parentNode,'.webgis-result-table-cell." + v + "')\" title='Wert in die Zwischenablage kopiern' style='opacity:" + (valCount > 1 ? ".3" : "1") + "'></div></td>";
                            copyButtonCount++;
                        } else {
                            tabContent += "<td></td>";
                        }
                    }

                    if (copyButtonCount > 1) {
                        tabContent += "<td class='webgis-result-table-copybutton'><div class='webgis-copy' onclick=\"webgis.copyAll(this.parentNode.parentNode, '.webgis-result-table-cell')\" title='Zeile in die Zwischenablage kopiern'></div></td>";
                    } else if (valCount > 1) {
                        tabContent += "<td></td>";
                    }

                    tabContent += "</tr>";

                    count++;
                }
            }

            if ( /*typeof(max)=="undefined" && */count === 0 && properties.hasOwnProperty('_popuptext')) {
                tab = properties['_popuptext'];
            }
            else if ( /*typeof (max) == "undefined" &&*/count === 0 && properties.hasOwnProperty('_fulltext')) {
                tabContent += "<tr>";
                tabContent += "<td class='webgis-result-table-cell'>" + properties['_fulltext'] + "</td>";
                tabContent += "</tr>";
            }
            if (tabContent) {
                tab += "<table class='webgis-result-table feature" + (isUnion ? ' union collapsed' : '') +"''>" + tabContent + "</table>";
                hasTables = true;
            }

            unionIndex++;
        }

        if (hasTables) {
            tab = buttons + tab;
        }

        if (!hideTitle && propertiesArray[0] && propertiesArray[0]._title) {
            tab = "<h3>" + propertiesArray[0]._title + "</h3>" + tab;
        }
        if (propertiesArray[0].hasOwnProperty('_details')) {
            tab += "<button onclick=\"webgis.showFeatureResultDetails('" + this._map.guid + "','" + propertiesArray[0]['_details'] + "')\" class='webgis-button' >Mehr...</button>";
        }

        return tab + (hasMore === true ? '...' : '');
    };

    this.refreshUIElements = function (servercommand) {
        //console.log('refrshUIElements', servercommand);

        var me = this, qString = '', eString = '', tString = '', sString = '', vfString = '', lString = '';
        var currentScale = this._map.scale();

        $('.webgis-all-queries').each(function (i, e) {
            if (qString === '') {
                var serviceIds = me._map.serviceIds();
                for (var s = 0; s < serviceIds.length; s++) {
                    var service = me._map.services[serviceIds[s]];
                    for (var q = 0, to = service.queries.length; q < to; q++) {
                        var query = service.queries[q];

                        if (query.visible == false)
                            continue;
                        // associated layer in scale??
                        if (query.associatedlayers && query.associatedlayers.length > 0) {
                            var inScale = false;
                            for (var a = 0; a < query.associatedlayers.length; a++) {
                                var associatedLayerDef = query.associatedlayers[a];
                                inScale = inScale || service.layerInScale(associatedLayerDef.id, currentScale);
                            }
                            if (!inScale) {
                                continue;
                            }
                        }
                        if (qString.length > 0)
                            qString += ';';
                        qString += service.id + ',' + query.id + ',';
                        // associated layer visible??
                        var vis = 0;
                        if (query.associatedlayers) {
                            for (var a = 0; a < query.associatedlayers.length; a++) {
                                var associatedLayerDef = query.associatedlayers[a];
                                var associatedService = service;
                                if (associatedLayerDef.serviceid) {
                                    associatedService = me._map.services[associatedLayerDef.serviceid];
                                }
                                if (!associatedService)
                                    continue;
                                vis = (associatedService.checkLayerVisibility([associatedLayerDef.id]));
                                if (vis == 1)
                                    break;
                            }
                        }
                        qString += vis;
                    }
                }
            }
            $(e).val(qString);
        });
        $('.webgis-all-editthemes').each(function (i, e) {
            if (eString == '') {
                var serviceIds = me._map.serviceIds();
                for (var s = 0; s < serviceIds.length; s++) {
                    var service = me._map.services[serviceIds[s]];
                    for (var i = 0, to = service.editthemes.length; i < to; i++) {
                        var edittheme = service.editthemes[i];
                        if (eString.length > 0)
                            eString += ';';
                        eString += service.id + ',' + edittheme.layerid + ',' + edittheme.themeid + "," + edittheme.name + "," + service.checkLayerVisibility([edittheme.layerid]);
                    }
                }
            }
            $(e).val(eString);
        });
        $('.webgis-all-editthemes-inscale').each(function (i, e) {
            if (eString == '') {
                var serviceIds = me._map.serviceIds();
                for (var s = 0; s < serviceIds.length; s++) {
                    var service = me._map.services[serviceIds[s]];
                    for (var i = 0, to = service.editthemes.length; i < to; i++) {
                        var edittheme = service.editthemes[i];

                        if (!edittheme /*|| !service.checkLayerVisibility([edittheme.layerid])*/ || !service.layerInScale(edittheme.layerid, currentScale)) {
                            continue;
                        }

                        if (eString.length > 0)
                            eString += ';';
                        eString += service.id + ',' + edittheme.layerid + ',' + edittheme.themeid + "," + edittheme.name + "," + service.checkLayerVisibility([edittheme.layerid]);
                    }
                }
            }
            $(e).val(eString);
        });
        $('.webgis-all-services').each(function (i, e) {
            if (sString == '') {
                var serviceIds = me._map.serviceIds();
                for (var s = 0; s < serviceIds.length; s++) {
                    if (sString.length > 0)
                        sString += ';';
                    sString += serviceIds[s];
                }
                ;
            }
            $(e).val(sString);
        });
        //$('.webgis-active-visfilters').each(function (i, e) {
        //    if (vfString == '') {
        //        for (var f in me._map._visfilters) {
        //            if (vfString != '')
        //                vfString += ',';
        //            vfString += me._map._visfilters[f].id;
        //        }
        //    }
        //    $(e).val(vfString);
        //});
        //$('.webgis-active-labelings').each(function (i, e) {
        //    if (lString == '') {
        //        for (var f in me._map._labels) {
        //            if (lString != '')
        //                lString += ',';
        //            lString += me._map._labels[f].id;
        //        }
        //    }
        //    $(e).val(lString);
        //});
        $('.webgis-map-tools').each(function (i, e) {
            if (tString == '') {
                for (var t in me._map._tools) {
                    if (tString != '')
                        tString += ',';
                    tString += me._map._tools[t].id;
                }
            }
            $(e).val(tString);
        });
        let activeTool = me._map.getActiveTool();
        $('.webgis-tool-options').each(function () {
            var tool = activeTool;
            if (tool && tool.options) {
                $(this).val(JSON.stringify(tool.options));
            }
            else {
                $(this).val("");
            }
        });
        // set focus elments 
        if (activeTool && activeTool.ui_element_focus) {
            for (var f in _focusSelectors) {
                if ($.inArray(f, activeTool.ui_element_focus) >= 0) {
                    $(_focusSelectors[f]).removeClass('webgis-display-none-important');
                } else {
                    $(_focusSelectors[f]).addClass('webgis-display-none-important');
                }
            }
        } else {  // or release display-hide
            for (var f in _focusSelectors) {
                $(_focusSelectors[f]).removeClass('webgis-display-none-important');
            }
        }

        $('.webgis-map-scale').val(currentScale);
        $('.webgis-map-scales').val(this._map.scales().toString());
        $('.webgis-map-crsid').val(this._map.crsId());
        if ($('.webgis-map-bbox').length > 0) {
            try {
                $('.webgis-map-bbox').val(this._map.getProjectedExtent().join(','));
            }
            catch (e) { }
        }
        if ($('.webgis-map-size').length > 0) {
            try {
                $('.webgis-map-size').val(this._map.getMapImageSize().join(','));
            }
            catch (e) { }
        }
        /* Graphics */
        //console.log('servercommand', servercommand);

        $('.webgis-map-graphics-tool').val(this._map.graphics.getTool());

        var graphicsTool = this._map.graphics.isRegularTool(servercommand)
            ? servercommand
            : this._map.graphics.getTool();

        $('.webgis-map-graphics-color').val(this._map.graphics.getColor(graphicsTool));
        $('.webgis-map-graphics-opacity').val(this._map.graphics.getOpacity(graphicsTool));
        $('.webgis-map-graphics-fillcolor').val(this._map.graphics.getFillColor(graphicsTool));
        $('.webgis-map-graphics-fillopacity').val(this._map.graphics.getFillOpacity(graphicsTool));
        $('.webgis-map-graphics-lineweight').val(this._map.graphics.getWeight(graphicsTool));
        $('.webgis-map-graphics-linestyle').val(this._map.graphics.getLineStyle(graphicsTool));
        $('.webgis-map-graphics-symbol').val(this._map.graphics.getCurrentSymbolId(graphicsTool));
        $('.webgis-map-graphics-fontstyle').val(this._map.graphics.getTextStyle(graphicsTool));
        $('.webgis-map-graphics-fontsize').val(this._map.graphics.getTextSize(graphicsTool));
        $('.webgis-map-graphics-fontcolor').val(this._map.graphics.getTextColor(graphicsTool));
        $('.webgis-map-graphics-pointcolor').val(this._map.graphics.getPointColor(graphicsTool));
        $('.webgis-map-graphics-pointsize').val(this._map.graphics.getPointSize(graphicsTool));
        if ($('.webgis-map-graphics-geojson').length > 0) {
            var geoJson = JSON.stringify(this._map.graphics.toGeoJson(), null, 2);
            $('.webgis-map-graphics-geojson').val(geoJson);
        }
        if ($('.webgis-current-toolsketch').length > 0) {
            var toolSketch = activeTool && activeTool.tooltype === 'graphics' ?
                this._map.graphics._sketch :
                this._map.sketch;
            $('.webgis-current-toolsketch').val(toolSketch.toWKT(true));
        }
        if ($('.webgis-map-serialization').length > 0) {
            var mapJson = JSON.stringify(this._map.serialize({
                ui: true,
                ignoreCustomServices: true
            }), null, 2);
            $('.webgis-map-serialization').val(mapJson);
        }
        if ($('.webgis-map-currentvisibility').length > 0) {
            var currentVisibility = JSON.stringify(this._map.serializeCurrentVisibility());
            $('.webgis-map-currentvisibility').val(currentVisibility);
        }
        if (webgis.mapBuilder && webgis.mapBuilder.html_meta_tags && $('.webgis-mapbuilder-html-meta-tags').length > 0) {
            $('.webgis-mapbuilder-html-meta-tags').val(webgis.mapBuilder.html_meta_tags);
        }
        if (webgis.mapBuilder && $('.webgis-mapbuilder-description-raw').length > 0) {
            $('.webgis-mapbuilder-description-raw').val(webgis.mapBuilder.mapDescription);
        }
        $('.input-setborder-onchange').change(function () {
            $(this).css('border', '2px solid #b5dbad');
        }).keydown(function () {
            $(this).css('border', '2px solid #b5dbad');
        });

        if (webgis.globals && webgis.globals.portal) {
            if (webgis.globals.portal.pageId) {
                $('.webgis-map-page-id').val(webgis.globals.portal.pageId);
            }
            if (webgis.globals.portal.mapName) {
                $('.webgis-map-name').val(webgis.globals.portal.mapName);
            }
            if (webgis.globals.portal.mapCategory) {
                $('.webgis-map-category').val(webgis.globals.portal.mapCategory);
            }
        }

        $('.webgis-map-name').val();

        /* Undo Buttons */
        $('.webgis-undobutton[data-undotool]').each(function () {
            var undoTool = me._map.getTool($(this).attr('data-undotool'));
            if (undoTool && undoTool.undos && undoTool.undos.length > 0)
                $(this).css('display', $(this).hasClass('webgis-ui-imagebutton') ? 'inline-block' : 'block');
            else
                $(this).css('display', '');
        });

        let activeToolIdClass = activeTool ? activeTool.id.replaceAll(".", "-") : null;
        let activeToolParentIdClass = activeTool && activeTool.parentid ? activeTool.parentid.replaceAll(".", "-") : null;

        /* tool dependencies */
        $('.webgis-dependencies').each(function (i, e) {
            var $e = $(e);

            if ($e.hasClass('webgis-dependency-elementvalue'))  // Nicht für Element, die von einer Auswahlliste etc abhängen. Diese werden beim Ändern des Elements sichtbar/unsichtbar gemacht
                return;

            var vis = true;
            if ($e.hasClass("webgis-dependency-servicesexists")) {
                vis = me._map.hasServices();
            }
            if (vis === true && $e.hasClass("webgis-dependency-queriesexists")) {
                vis = me._map.hasServiceQueries();
            }
            if (vis === true && $e.hasClass("webgis-dependency-queryresultsexists")) {
                vis = me._map.hasQueryResults();
            }
            if (vis === true && $e.hasClass("webgis-dependency-toolsketchesexists")) {
                vis = me._map.currentToolSketches().length > 0;
            }
            if (vis === true && $e.hasClass("webgis-dependency-editthemesexists")) {
                vis = me._map.hasServiceEditthemes();
            }
            if (vis === true && $e.hasClass("webgis-dependency-edittheme-selected")) {
                var $editThemeTree = $e.closest('.webgis-tooldialog-content').find('.webgis-edittheme-tree');
                if ($editThemeTree.length === 1) {
                    //console.log('editThemeTreeValue', $editThemeTree.webgis_editthemeTree('value'));
                    vis = $editThemeTree.webgis_editthemeTree('editThemeSelected') ? true : false;
                } else {
                    vis = false;
                }
            }
            if (vis === true && $e.hasClass("webgis-dependency-chainagethemesexists")) {
                vis = me._map.hasServiceChainagethemes();
            }
            if (vis === true && $e.hasClass("webgis-dependency-focused-service-exists")) {
                vis = me._map.hasFocusedServices();
            }
            if (vis === true && $e.hasClass("webgis-dependency-hasselection")) {
                var selection = me._map.getSelection("selection");
                if ((selection && selection.isActive() && selection.count() > 0) || me._map.queryResultFeatures.selected()) {
                    if ($e.hasClass('webgis-toggle-button')) {
                        $e.addClass('selected');
                    } else {
                        vis = true;
                    }
                } else {
                    if ($e.hasClass('webgis-toggle-button')) {
                        $e.removeClass('selected');
                    } else {
                        vis = false;
                    }
                }
            }
            if (vis === true && $e.hasClass("webgis-dependency-hasnoselection")) {
                var selection = me._map.getSelection("selection");
                vis = selection && selection.isActive() === false;
            }
            if (vis === true && $e.hasClass("webgis-dependency-hasmorethanoneselected")) {
                var selection = me._map.getSelection("selection");
                vis = selection && selection.isActive() && selection.count() > 1;
            }
            if (vis === true && $e.hasClass("webgis-dependency-ishighlighted")) {
                var ishighlighted = false;

                var featureOid = $e.attr('data-id');
                if (featureOid) {
                    var querySelection = me._map.getSelection('query');

                    ishighlighted = querySelection && querySelection.isActive() && querySelection.includesOid(featureOid);
                }

                if (ishighlighted === true) {
                    if ($e.hasClass('webgis-toggle-button')) {
                        $e.addClass('selected');
                    } else {
                        vis = true;
                    }
                } else {
                    if ($e.hasClass('webgis-toggle-button')) {
                        $e.removeClass('selected');
                    } else {
                        vis = false;
                    }
                }
            }
            if (vis === true && $e.hasClass("webgis-dependency-hastoolresults") && me._map.getActiveTool()) {
                vis = me._map.queryResultFeatures.hasToolResults(me._map.getActiveTool().id);
            }
            if (vis === true && $e.hasClass("webgis-dependency-hasmarkerinfo")) {
                var tool = me._map.getActiveTool();
                if (tool && tool.is_graphics_tool === true) {
                    vis = false;
                } else {
                    vis = me._map.graphics.hasMarkerInfos();
                }
            }
            if (vis === true && $e.hasClass("webgis-dependency-queryfeatureshastableproperties")) {
                if (!me._map.queryResultFeatures.hasTableProperties()) {
                    vis = false;
                }
            }
            if (vis === true && $e.hasClass("webgis-dependency-hastoolresults_coordinates")) {
                vis = me._map.queryResultFeatures.hasToolResults('webgis.tools.coordinates');
            }
            if (vis === true && $e.hasClass("webgis-dependency-hastoolresults_chainage")) {
                vis = me._map.queryResultFeatures.hasToolResults('webgis.tools.chainage');
            }
            if (vis === true && $e.hasClass("webgis-dependency-hastoolresults_coordinates_or_queryresults")) {
                vis = me._map.hasQueryResults() ||
                    me._map.queryResultFeatures.hasToolResults('webgis.tools.coordinates');
            }
            if (vis === true && $e.hasClass("webgis-dependency-hastoolresults_coordinates_or_chainage_or_queryresults")) {
                vis = me._map.hasQueryResults() ||
                    me._map.queryResultFeatures.hasToolResults('webgis.tools.coordinates') ||
                    me._map.queryResultFeatures.hasToolResults('webgis.tools.chainage');
            }
            if (vis === true && $e.hasClass("webgis-dependency-hasfilters")) {
                var applicableFilters = $e.attr('data-applicable-filters');
                if (applicableFilters) {
                    var filterIds = applicableFilters.split(',');
                    var hasFilter = false;
                    for (var f in filterIds) {
                        if (me._map.hasFilter(filterIds[f])) {
                            hasFilter = true;
                            break;
                        }
                    }
                    vis = hasFilter;
                } else {
                    vis = me._map.hasFilters();
                }
            }
            if (vis == true &&
                ($e.hasClass('webgis-dependency-layersvisible') || $e.hasClass('webgis-dependency-layersinscale')) &&
                $e.attr('data-dependency-arguments')) {

                var dependency_arguments = $e.attr('data-dependency-arguments').split(',');
                var logical_operator = $e.attr('data-dependency-logical_operator');
                var condition_result = $e.attr('data-dependency-condition_result') === "true";

                var result = logical_operator === 'and' ? true : false;

                var applyLogicalOperator = function (val) {
                    val = val ? true : false; // val 2 bool;
                    switch (logical_operator) {
                        case 'and':
                            result = (result & val) ? true : false;
                            break;
                        default:
                            result = (result | val) ? true : false;
                            break;
                    }
                }

                $.each(dependency_arguments, function (i, argument) {
                    var visible = false;

                    var service = map.getService(argument.split(':')[0]);
                    if (service) {
                        var layerId = argument.split(':')[1];

                        if ($e.hasClass('webgis-dependency-layersvisible') && $e.hasClass('webgis-dependency-layersinscale')) {
                            visible = service.layerVisibleAndInScale(layerId, currentScale);
                        }
                        else if ($e.hasClass('webgis-dependency-layersvisible')) {
                            var layer = service.getLayer(layerId);
                            visible = layer.visible;
                        }
                        else if ($e.hasClass('webgis-dependency-layersinscale')) {
                            visible = service.layerInScale(layerId, currentScale);
                        }
                    }

                    applyLogicalOperator(visible);
                });

                vis = result === condition_result;
            }

            if (vis == true &&
                $e.hasClass('webgis-dependency-haslivesharehubconnection')) {
                vis = webgis.liveshare && webgis.liveshare.hasHubConnection();
            }
            if (vis == true &&
                $e.hasClass('webgis-dependency-isinlivesharesession')) {
                vis = webgis.liveshare && typeof webgis.liveshare.sessionId() === 'string';
            }
            if (vis == true &&
                $e.hasClass('webgis-dependency-isnotinlivesharesession')) {
                vis = webgis.liveshare && (webgis.liveshare.sessionId() === null);
            }
            if (vis == true &&
                $e.hasClass('webgis-dependency-islivesharesessionowner')) {
                vis = webgis.liveshare && (webgis.liveshare.isSessionOwner());
            }
            if (vis == true &&
                $e.hasClass('webgis-dependency-isnotlivesharesessionowner')) {
                vis = webgis.liveshare && (webgis.liveshare.isSessionOwner() === false);
            }
            if (vis == true &&
                $e.hasClass('webgis-dependency-calc-tsp')) {
                
                vis = webgis.usability.tsp && webgis.usability.tsp.allowOrderFeatures === true &&
                    map.queryResultFeatures.countFeatures() > 1 &&
                    map.queryResultFeatures.countFeatures() <= webgis.usability.tsp.maxFeatures;
            }
            if (vis == true &&
                $e.hasClass('webgis-dependency-activetool')) {
                vis = activeToolIdClass && $e.hasClass(activeToolIdClass);
            }
            if (vis == true &&
                $e.hasClass('webgis-dependency-not-activetool')) {
                vis = !activeToolIdClass || !$e.hasClass(activeToolIdClass);
                if (vis == true) {
                    vis = !activeToolParentIdClass || !$e.hasClass(activeToolParentIdClass);
                }
            }
            if (vis == true &&
                $e.hasClass('webgis-dependency-hasgraphicsstagedelement')) {
                vis = map.graphics.hasStagedElement();
            }
            
            $e.css('display', vis ? '' : 'none');

            if (vis == false && $e.hasClass('webgis-toolbox-tool-item')) {
                var toolId = $e.attr('data-toolid');
                var activeTool = me._map.getActiveTool();
                if (activeTool && activeTool.id == toolId) {
                    webgis.delayed(function ($e) {  // verzögert aufrufen, weil die Funktion mit unterschiedlichen Konstellationen aufgerufen. Nur wenn Tool am schluss wirklich unsichtbar ist -> zum Defaulttool
                        if ($e.css('display') == 'none') {
                            //console.trace('set default tool delayed from '+toolId);
                            me._map.setActiveTool(me._map.getDefaultTool());
                        }
                    }, 300, $e);
                }
            }
        });

        if ($('.webgis-map-anonymous-user-id').length > 0) {
            var currentId = webgis.localStorage.getAnonymousUserId();
            $('.webgis-map-anonymous-user-id').each(function () {
                if (currentId) {
                    $(this).val(currentId);
                } else if ($(this).val()) {
                    currentId = $(this).val();
                    webgis.localStorage.setAnonyousUserId(currentId);
                }
            });
        }

        //var isGraphicsTool = this._map.graphics._isGraphicsToolActive();
        //console.log( "RefreshUIElements  "+ isGraphicsTool)
        //$('.webgis-graphics-tool.show').css('display', isGraphicsTool ? '' : 'none');
        //$('.webgis-graphics-tool.hide').css('display', isGraphicsTool ? 'none' : '');

        if (this._map.graphics.getTool() === 'distance_circle') {
            $('.webgis-graphics-distance_circle-radius').val(Math.round(map.graphics.getDistanceCircleRadius() * 100) / 100);
            $('.webgis-graphics-distance_circle-steps').val(map.graphics.getDistanceCircleSteps());
        }

        if (this._map.graphics.getTool() === 'compass_rose') {
            $('.webgis-graphics-compass_rose-steps').val(map.graphics.getCompassRoseSteps());
        }

        if (this._map.graphics.getTool() === 'compass_rose') {
            $('.webgis-graphics-compass_rose-radius').val(Math.round(map.graphics.getCompassRoseRadius() * 100) / 100);
            $('.webgis-graphics-compass_rose-steps').val(map.graphics.getCompassRoseSteps());
        }

        if (this._map.graphics.getTool() === 'hectoline') {
            $('.webgis-graphics-hectoline-unit').val(map.graphics.getHectolineUnit());
            $('.webgis-graphics-hectoline-interval').val(map.graphics.getHectolineInterval());
        }
        this._map.events.fire('onrefreshuielements', this._map);
    };
    this._cloneOptions = function (options) {
        if (typeof options === 'string' || typeof options === 'number' || typeof options === 'boolean' || !options)
            return options;
        if (Object.prototype.toString.call(options) === '[object Array]') {
            var array = [];
            for (var i in options) {
                array.push(this._cloneOptions(options[i]));
            }
            return array;
        }
        var o = {};
        for (var p in options) {
            o[p] = this._cloneOptions(options[p]);
        }
        ;
        return o;
    };
    this._stringSelector = function (selector, sourceMethod) {
        if (typeof selector === 'string') {
            if (selector.indexOf('#') == 0 || selector.indexOf('.') == 0)
                return selector;
            return '#' + selector;
        }

        if ($(selector).length == 0) {
            return null;
        }

        if (!$(selector).attr('id'))
            throw sourceMethod + ' - Element ' + selector.nodeName + ' has no id-attribute';

        return '#' + $(selector).attr('id');
    };
    this._optionsFromElement = function ($element, options) {
        //return options;
        // DoTo: Nachm Urlaub
        $.each($element[0].attributes, function (i, a) {
            if (a.name.indexOf('data-option-') == 0) {
                var optionName = a.name.substr(12, a.name.length - 12);
                switch (typeof a.value === 'string' ? a.value.toLowerCase() : a.value) {
                    case 'true':
                        options[optionName] = true;
                        break;
                    case 'false':
                        options[optionName] = false;
                        break;
                    case 'null':
                        options[optionName] = null;
                        break;
                    default:
                        options[optionName] = a.value;
                        break;
                }
            }
        });
        //alert(JSON.stringify(options));
        return options;
    };
    this._serializeOptions = [];
    this.serialize = function () {
        return {
            options: this._serializeOptions
        };
    };
    this.deserialize = function (uiJson) {
        if (uiJson.options) {
            for (var i in uiJson.options) {
                var uiElementJson = uiJson.options[i];
                var $target = $(uiElementJson.selector);
                if ($target.find('.' + uiElementJson.element + "-layout-container").length > 0)
                    $target = $target.find('.' + uiElementJson.element + "-layout-container");
                if (uiElementJson.element === 'hourglass') {
                    this.createHourglass($target, uiElementJson.options);
                }
                if (uiElementJson.element === 'search') {
                    this.createSearch($target, uiElementJson.options);
                }
                if (uiElementJson.element === 'topbar') {
                    this.createTopbar($target, uiElementJson.options);
                }
                if (uiElementJson.element === 'tabs') {
                    var $optionsElement = $('.tabs-layout-container-options').length === 1 ? $('.tabs-layout-container-options') : $target;
                    var options = this._optionsFromElement($optionsElement, uiElementJson.options);
                    this.createTabs($target, options);
                }
            }
        }
    };
    this._hasQuickInfo = false;
    this.hideQuickInfo = function () {
        if (this._hasQuickInfo)
            $(this._map.elem).find('.ui-quickinfo').remove();
        this._hasQuickInfo = false;
    };
    this.showQuickInfo = function (options) {
        this.hideQuickInfo();
        this._hasQuickInfo = true;
        var $quickInfo = $("<div class='ui-quickinfo'></div>").data('options', options).appendTo($(this._map.elem));
        var $content = $("<div class='ui-quickinfo-content'>" + options.content + "</dic>").appendTo($quickInfo);
        var $close = $("<div class='ui-quickinfo-close'>X</div>").appendTo($quickInfo);
        var left = options.pos.left - 80, top = options.pos.top - 20;
        var width = $quickInfo.width(), height = $quickInfo.height(), mapWidth = $(this._map.elem).width(), mapHeight = $(this._map.elem).height();
        if (parseInt(left + width) > parseInt(mapWidth))
            left = mapWidth - width - 23;
        if (parseInt(top + height) > parseInt(mapHeight))
            top = mapHeight - height;
        $quickInfo.css({
            position: 'absolute',
            left: Math.max(0, left),
            top: Math.max(0, top)
        });
        $close.click(function (event) {
            event.stopPropagation();
            $(this).closest('.ui-quickinfo').remove();
        });
        $quickInfo.click(function (event) {
            event.stopPropagation();
            var $quickInfo = $(this).closest('.ui-quickinfo');
            var options = $quickInfo.data('options');
            if (options.click)
                options.click(options.clickdata);
            $quickInfo.remove();
        });
    };

    this.addQuickAccessButton = function (options) {
        var $button = $("<div></div>")
            .addClass('webgis-quick-access-button')
            .appendTo($(this._map._webgisContainer))
            .webgis_bubble({
                map: this._map,
                onClick: options.onClick || null,
                onEnable: options.onEnable || null,
                onDisable: options.onDisable || null,
                onLongClick: options.onLongClick || null,
                bgImage: options.bgImage || null,
                text: options.text || null,
                textClass: options.textClass || null,
                helpName: options.helpName || null,
                dragTolerance: options.dragTolerance || null
            });
        if (options.addClass)
            $button.addClass(options.addClass);
        this.animElement($button);
        return $button;
    };
    this.removeAllQuickAccessButtons = function () {
        $(this._map._webgisContainer).find('.webgis-quick-access-button').webgis_bubble('remove');
    };
    this.isQuickAccessButtonActive = function (quickButtonClass) {
        var $button = $(".webgis-quick-access-button." + quickButtonClass);

        if ($button.length === 1 && !$button.hasClass('disabled')) {
            return true;
        }

        return false;
    };

    var _$clickBubble = null;
    this.addClickBubble = function () {
        if (_$clickBubble === null) {
            _$clickBubble = $("<div></div>")
                .addClass('webgis-click-bubble')
                .appendTo($(this._map._webgisContainer))
                .webgis_bubble({
                    id: 'webgis-click-bubble',
                    map: this._map,
                    bgImage: webgis.css.img('click-26-w.png'),
                    trashPosition: webgis.useMobileCurrent() ? 'none' : 'top',
                    parkPosition: 'top',
                    disabled: false,
                    rememberDisabled: true,
                    parkable: true,
                    text: '...',
                    textClass: 'tool-name small',
                    onDragEnd: function (pointer) {
                        var map = $(this).data('map'), mapPos = $(map.elem).position();
                        var clickEvent = map.eventHandlers.createMouseEvent({
                            clientX: pointer.clientX - parseInt(mapPos.left),
                            clientY: pointer.clientY - parseInt(mapPos.top)
                        });
                        map.eventHandlers.click.apply(map, [clickEvent]);
                        map.eventHandlers.move.apply(map, [{}]);
                    },
                    onDragMove: function (pointer) {
                        var map = $(this).data('map'), mapPos = $(map.elem).position();
                        var moveEvent = map.eventHandlers.createMouseEvent({
                            clientX: pointer.clientX - parseInt(mapPos.left),
                            clientY: pointer.clientY - parseInt(mapPos.top)
                        });
                        map.eventHandlers.move.apply(map, [moveEvent]);
                    },
                    onDisable: function () {
                        var map = $(this).data('map');
                        map.eventHandlers.move.apply(map, [{}]);
                    },
                    onParked: function () {
                        var map = $(this).data('map');
                        if (map.sketch)
                            map.sketch.resetMarkerDragging();
                        map.eventHandlers.move.apply(map, [{}]);
                    },
                    onClick: function (e) {
                        var map = $(this).data('map');
                        map.ui.showHelp('click-bubble');
                    }
                });
        }
        else {
            _$clickBubble.css('display', '');
        }
        this.animElement(_$clickBubble);
    };
    this.removeClickBubble = function () {
        if (_$clickBubble)
            _$clickBubble.css('display', 'none');
    };
    this.useClickBubble = function () {
        return _$clickBubble && _$clickBubble.css('display') != 'none' && _$clickBubble.webgis_bubble('isEnabled');
    };
    this.animClickBubble = function () {
        this.animElement(_$clickBubble);
    };
    this.isClickBubbleActive = function () {
        return _$clickBubble != null && !_$clickBubble.hasClass('disabled') && _$clickBubble.css('display') != 'none';
    };
    this.setClickBubbleLatLng = function (latLng) {
        if (_$clickBubble) {
            if (latLng) {
                var point = this._map.frameworkElement.latLngToContainerPoint(latLng);
                var moveTo = {
                    x: point.x,
                    y: point.y,
                    target: this._map.elem
                };
                _$clickBubble.webgis_bubble('moveTo', moveTo);
            }
            else {
                _$clickBubble.webgis_bubble('park');
            }
        }
    };
    var _$contextMenuBubble = null;
    this.addContextMenuBubble = function () {
        if (_$contextMenuBubble == null) {
            _$contextMenuBubble = $("<div></div>")
                .addClass('webgis-contextmenu-bubble')
                .appendTo($(this._map._webgisContainer))
                .webgis_bubble({
                    map: this._map,
                    bgImage: webgis.css.img('menu-26-w.png'),
                    trashPosition: 'none',
                    text: 'Kontext<br/>menü',
                    top: 200 + (webgis.useMobileCurrent() ? -80 : 0),
                    parkable: true,
                    onDragEnd: function (pointer) {
                        var map = $(this).data('map'), mapPos = $(map.elem).position();
                        var clickEvent = map.eventHandlers.createMouseEvent({
                            clientX: pointer.clientX - parseInt(mapPos.left),
                            clientY: pointer.clientY - parseInt(mapPos.top)
                        });
                        // Hier kein "move" aufrufen, weil das den SketchMover usw. löscht, die für das Contextmenu wichtig sind!!
                        //map.eventHandlers.move.apply(map, [{}]);
                        map.events.fire('rightclick', map, clickEvent);
                    },
                    onDragMove: function (pointer) {
                        var map = $(this).data('map'), mapPos = $(map.elem).position();

                        var moveEvent = map.eventHandlers.createMouseEvent({
                            clientX: pointer.clientX - parseInt(mapPos.left),
                            clientY: pointer.clientY - parseInt(mapPos.top)
                        });
                        map.eventHandlers.move.apply(map, [moveEvent]);
                    },
                    onClick: function (e) {
                        var map = $(this).data('map'), mapPos = $(map.elem).position();
                        var clickEvent = map.eventHandlers.createMouseEvent({
                            clientX: $(this).position().left,
                            clientY: $(this).position().top // - parseInt(mapPos.top)
                        });
                        map.events.fire('rightclick', map, clickEvent);
                    }
                });
        }
        else {
            _$contextMenuBubble.css('display', '');
        }
        this.animElement(_$contextMenuBubble);
    };
    this.removeContextMenuBubble = function () {
        if (_$contextMenuBubble)
            _$contextMenuBubble.css('display', 'none');
    };

    var _$toolBubble = null;  // Für reine Bubble Tools (zB Liveshare)
    this._addToolBubble = function () {
        if (_$toolBubble === null) {
            _$toolBubble = $("<div></div>")
                .addClass('webgis-click-bubble')
                .appendTo($(this._map._webgisContainer))
                .webgis_bubble({
                    id: 'webgis-tool-bubble',
                    map: this._map,
                    bgImage: webgis.css.img('click-26-w.png'),
                    trashPosition: 'none',
                    parkPosition: 'top',
                    disabled: false,
                    rememberDisabled: false,
                    parkable: true,
                    text: this._map.getActiveTool().name,
                    textClass: 'tool-name small',
                    onDragEnd: function (pointer) {
                        //var map = $(this).data('map'), mapPos = $(map.elem).position();
                        //var clickEvent = map.eventHandlers.createMouseEvent({
                        //    clientX: pointer.clientX - parseInt(mapPos.left),
                        //    clientY: pointer.clientY - parseInt(mapPos.top)
                        //});
                        //map.eventHandlers.click.apply(map, [clickEvent]);
                        //map.eventHandlers.move.apply(map, [{}]);
                    },
                    onDragMove: function (pointer) {
                        var map = $(this).data('map'), mapPos = $(map.elem).position();
                        var clientX = pointer.clientX - parseInt(mapPos.left);
                        var clientY = pointer.clientY - parseInt(mapPos.top);
                        var latLng = map.frameworkElement.layerPointToLatLng([clientX, clientY]);

                        map.events.fire('tool-bubble-move', map, {
                            x: clientX,
                            y: clientY,
                            lat: latLng.lat,
                            lng: latLng.lng
                        });
                    },
                    onDisable: function () {
                        var map = $(this).data('map');
                        map.eventHandlers.move.apply(map, [{}]);
                    },
                    onParked: function () {
                        var map = $(this).data('map');
                        if (map.sketch)
                            map.sketch.resetMarkerDragging();
                        map.eventHandlers.move.apply(map, [{}]);
                    },
                    onClick: function (e) {
                        var map = $(this).data('map');
                        map.ui.showHelp('click-bubble');
                    }
                });
        }
        else {
            _$toolBubble.css('display', '');
        }
        this.animElement(_$toolBubble);
    }
    this._removeToolBubble = function () {
        if (_$toolBubble)
            _$toolBubble.css('display', 'none');
    };

    this.animElement = function (element) {
        if (element) {
            var $element = $(element);
            $element.addClass('webgis-spin');
            webgis.delayed(function () {
                $element.removeClass('webgis-spin');
            }, 400);
        }
    };
    this.showHelp = function (name, index) {
        if (!index)
            index = 'index';
        $('body').webgis_modal({
            title: 'Click Bubble',
            onload: function ($content) {
                $content.css('padding', '10px');
                $.get(webgis.baseUrl + '/content/help/' + name + '/' + index + '.html', function (data) {
                    //console.log(data);
                    $content.html(data.replaceAll('{api}', webgis.baseUrl));
                });
            },
            width: '800px', height: '90%'
        });
    };
    this.webgisContainer = function () {
        if (this._map)
            return $(this._map._webgisContainer || 'body');
        return $('body');
    };
    this.mapContainer = function () {
        return $(this._map.elem);
    };

    /* Alle Dialoge (Tab) schließen (zB auf Handies mit Back Button) */
    this.canCleanupDisplay = function () {
        if (webgis.isTouchDevice()) {
            if ($(this.webgisContainer).find('.webgis-display-cleanup[data-hash]').length > 0)
                return true;

            if ($(this.webgisContainer).find('.webgis-tabs-tab.webgis-tabs-tab-selected').length === 1)
                return true;

            if ($(this.webgisContainer).find('.webgis-detail-search-holder').length === 1 &&
                $(this.webgisContainer).find('.webgis-detail-search-holder').css('display') !== 'none')
                return true;

            if (!this._map.isDefaultTool(this._map.getActiveTool()))
                return true;
        }

        return false;
    };
    this.cleanupDisplay = function () {
        if (webgis.isTouchDevice()) {
            if ($(this.webgisContainer).find('.webgis-display-cleanup[data-hash]').length > 0) {
                var hash = 0, $element = null;
                $(this.webgisContainer).find('.webgis-display-cleanup[data-hash]').each(function (i, element) {
                    var elementHash = parseInt($(element).attr('data-hash'));
                    if (elementHash > hash) {
                        hash = elementHash;
                        $element = $(element);
                    }
                });
                if ($element != null) {
                    $element.trigger('click');
                }
            }
            else if ($(this.webgisContainer).find('.webgis-tabs-tab.webgis-tabs-tab-selected').length === 1) {
                $(this.webgisContainer).find('.webgis-tabs-tab.webgis-tabs-tab-selected').trigger('click');
            }

            else if ($(this.webgisContainer).find('.webgis-detail-search-holder').length === 1 &&
                $(this.webgisContainer).find('.webgis-detail-search-holder').css('display') !== 'none') {
                $(this.webgisContainer).find('.webgis-detail-search-holder').css('display', 'none');
            }

            else if (!this._map.isDefaultTool(this._map.getActiveTool())) {
                this._map.setParentTool();
            }
        }
    };

    this.scaleDialog = function () {
        var map = this._map;
        if (map && $.fn.webgis_modal) {
            $('body').webgis_modal({
                id: 'setscale-modal',
                title: webgis.l10n.get("mapscale"),
                width: '340px',
                height: '300px',
                onload: function ($content) {
                    var $scaleInput;
                    if (webgis.usability.zoom.allowsFreeZooming() === true || map.viewLense.isActive()) {

                        var $tr = $("<tr>").appendTo($("<table>").css('width', '100%').appendTo($content));

                        var $label = $("<div>")
                            .text('1:');
                        $scaleInput = $('<input>')
                            .attr('type', 'number')
                            .addClass('webgis-input')
                            .css('width', '250px')
                            .val(Math.round(map.calcCrsScale() / 10) * 10);

                        if (map.viewLense.isActive()) {
                            $scaleInput.attr('readonly', 'readonly');
                        } else {
                            $scaleInput.keypress(function (e) {
                                if (e.keyCode === 13) {
                                    map.setCalcCrsScale(parseInt($(this).val()), map.getCenter());
                                    $(null).webgis_modal('close', { id: 'setscale-modal' });
                                }
                            });
                        }

                        $("<td>")
                            .css('width', '30px')
                            .append($label)
                            .appendTo($tr);
                        $("<td>")
                            .css('width', '100%')
                            .append($scaleInput)
                            .appendTo($tr);

                    } else {
                        $scaleInput = $('<select>')
                            .addClass('webgis-input')
                            .appendTo($content);

                        var scales = map.scales();
                        for (var i in scales) {
                            var scale = Math.round(scales[i] / 10) * 10;

                            var selected = '';
                            if (Math.abs(scale - map.scale()) < 10) {
                                selected = ' selected ';
                            }
                            $("<option " + selected + " value='" + scale + "'>1 : " + scale.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ".") + "</option>").appendTo($scaleInput);
                        }

                        $scaleInput.change(function (e) {
                            map.setScale(parseInt($(this).val()), map.getCenter());
                            //$(null).webgis_modal('close', { id: 'setscale-modal' });
                        });
                    }

                    $("<br>").appendTo($content);
                    var $setAsPreferencedScale = $("<input>")
                        .attr({ type: 'checkbox', id: 'check-use-as-pref-scale' })
                        .appendTo($content);
                    $("<label>")
                        .attr("for", "check-use-as-pref-scale")
                        .text(webgis.l10n.get('as-preference-scale'))
                        .appendTo($content);
                    $("<div>").addClass('webgis-info').text(webgis.l10n.get('preference-scale-info')).appendTo($content);

                    $bottonConatainer = $("<div>").addClass('webgis-uibutton-container').appendTo($content);

                    if (webgis.getUserPreferencedMinScale() > 0) {
                        $('<button>')
                            .addClass('uibutton uibutton-cancel webgis-button')
                            .text("Reset 1:" + Math.round(webgis.getUserPreferencedMinScale()))
                            .appendTo($bottonConatainer)
                            .click(function (e) {
                                webgis.resetUserPreferenceMinScale();
                                $(null).webgis_modal('close', { id: 'setscale-modal' });
                            });
                    }

                    $('<button>')
                        .addClass('uibutton webgis-button')
                        .text("Ok")
                        .appendTo($bottonConatainer)
                        .click(function (e) {
                            map.setScale(parseInt($scaleInput.val()), map.getCenter());
                            if ($setAsPreferencedScale.prop('checked') === true) {
                                //console.log('set user preferenced scale: ' + parseFloat($scaleInput.val()));
                                webgis.setUserPreferenceMinScale(parseFloat($scaleInput.val()));
                            }

                            $(null).webgis_modal('close', { id: 'setscale-modal' });
                        });

                }
            });
        }
    };

    this.uncollapseSidebar = function () {
        if (this._map && this._map._webgisContainer && $(this._map._webgisContainer).hasClass('sidebar-collapsed')) {
            $(this._map._webgisContainer).removeClass('sidebar-collapsed');
            this._map.resize();
        }
    };

    this.getQueryResultTabControl = function () {
        var $queryResultsContainer = this._map.ui.webgisContainer().find('.query-results-tab-control-container');

        if ($queryResultsContainer.length === 1) {
            return $queryResultsContainer;
        }

        return null;
    }

    var _mapTitle;
    this.setMapTitle = function (title) {
        title = title || _mapTitle;
        $(this._map._webgisContainer).find('.webgis-map-title').text(_mapTitle = title);

        this._map.events.fire('maptitlechanged');
    };
    this.getMapTitle = function () { return _mapTitle; }

    // Qeueries & Dynamic Content
    this.appliesQueryResultsUI = function () {
        if ($.fn.webgis_queryResultsList) {
            return true;
        }

        return false;
    };
    this.showQueryResults = function (result, zoom2results, dynamicContentQuery /* optionaler Parameter wird bei Dynamischen Inhalten mitübergeben */) {
        var map = this._map;
        var isServiceQuery = false, addSilent = false;

        var $tabControl = map.ui.getQueryResultTabControl();
        if ($tabControl && result.tab && result.tab.addSilent) {
            addSilent = true;
            delete result.tab.addSilent;
        }

        if (result && result.features && result.features.length > 0) {
            for (var f in result.features) { // zB Höhenabfrage, Stationierung sind keine echen Abfragen auf ein Service. Hies soll der Ergebnissreiter nicht angezeigt werden, dafür gleich das Popup aufgehen.
                var feature = result.features[f];
                if (feature.oid) {
                    var qid = feature.oid.split(':');
                    if (qid[0] && qid[0] !== 'unknown') {
                        isServiceQuery = true;
                        break;
                    }
                }
            }
        }

        if (!addSilent) {
            map.getSelection('selection').remove();
            map.getSelection('query').remove();

            var customSelection = map.getSelection('custom'); // zB Pufferfläche
            customSelection.remove();

            var $resultHolder = $(map._queryResultsHolder);
            result = map.queryResultFeatures.showClustered(result,
                zoom2results,
                $resultHolder.length === 0 || isServiceQuery === false || webgis.usability.showSingleResultPopup === true // SingleResultPopup
            );

            if ($resultHolder.length > 0) {
                $resultHolder.get(0).search_term = '';
            }
            $resultHolder
                .find('.webgis-search-result-list')
                .webgis_queryResultsList('empty', { map: map });
        }

        if (result && result.features && result.features.length === 0) {
            if (!dynamicContentQuery) {
                if (!addSilent) {
                    var msg = 'Es wurden keine Ergebnisse gefunden'
                    if (result.metadata) {
                        if (result.metadata.infos)
                            msg += '\n\n' + result.metadata.infos.toString();
                        if (result.metadata.warnings)
                            msg += '\n\n' + result.metadata.warnings.toString();
                    }
                    webgis.alert(msg, 'info');
                }
                if ($.webgis_search_result_histories[map] != null) {
                    var $list = $resultHolder.find('.webgis-search-result-list');
                    $.webgis_search_result_histories[map].render($list);
                }
            }
        }
        else {
            if (isServiceQuery) {
                if ($tabControl) {
                    var tabId, title, pinned = false;
                    if (!dynamicContentQuery) {
                        var query = map.queryResultFeatures.firstQuery(result.features);
                        tabId = query.id;
                        title = query.name;
                    } else {
                        tabId = dynamicContentQuery.id;
                        title = dynamicContentQuery.name;
                    }

                    if (result.tab) {
                        tabId = result.tab.id || tabId;
                        title = result.tab.title || title;
                        pinned = result.tab.pinned || false;
                    }

                    if (!tabId) {
                        if (result.features && result.features.length === 1) {
                            // simple quicksearch feature with no original
                            map.queryResultFeatures.showFeatureTable(result.features[0]);
                            // close quicksearch (multiple results) tab
                            $tabControl.webgis_tab_control('remove', { id: 'webgis-search-tabcontrol-tab' });
                        }
                        return;
                    }

                    var $tab = $tabControl.webgis_tab_control('getTab', { id: tabId });
                    var tabPayload = { features: result, map: map };

                    if (!$tab) {
                        $tabControl
                            .webgis_tab_control('add', {
                                id: tabId,
                                title: title,
                                payload: tabPayload,
                                customData: dynamicContentQuery ? { type: 'dynamic', dynamicContent: dynamicContentQuery } : null,
                                select: !addSilent,
                                icon: dynamicContentQuery ? this.dynamicContentIcon(dynamicContentQuery) : null,
                                pinable: dynamicContentQuery ? false : true,
                                pinned: pinned,
                                counter: dynamicContentQuery ? null : result.features.length,
                                onCreated: function ($control, $tab, $content) {
                                    $tab.data('suppressShowQueryResults', !addSilent);
                                },
                                onSelected: function ($control, $tab, tabOptions) {
                                    var map = tabOptions.payload.map, features = tabOptions.payload.features;
                                    if ($tab.data('suppressShowQueryResults')) {
                                        $tab.data('suppressShowQueryResults', false);
                                    } else {
                                        if (!tabOptions|| !tabOptions.customData || !tabOptions.customData.dynamicContent) {
                                            map.unloadDynamicContent();
                                        }
                                        map.ui.showQueryResults(features, false);
                                    }

                                    map.ui.refreshUIElements();
                                },
                                onPinned: function ($control, $tab, tabOptions) {
                                    var id = $tab.attr('tab-id');

                                    if (!$tab.hasClass('pinned-processed')) {
                                        $tab.addClass('pinned-processed');

                                        if (tabOptions.clickEvent) {
                                            options = { fromId: id, toId: id + "-" + webgis.guid().toString() };

                                            $control.webgis_tab_control('changeTabId', options);

                                            var features = tabOptions.payload.features;

                                            features.tab = features.tab || {};
                                            features.tab.id = options.toId;

                                            id = options.toId;

                                            $control.webgis_tab_control('editTitle', { id: id, setTitleEditable: true });
                                        } else {
                                            $control.webgis_tab_control('setTitleEditable', { id: id, editable: true });
                                        }
                                    }
                                },
                                onRemoved: function ($control, tabOptions) {
                                    if (tabOptions && tabOptions.customData && tabOptions.customData.dynamicContent &&
                                        tabOptions.customData.dynamicContent.id == map.getCurrentDynamicContentId()) {
                                        map.queryResultFeatures.clear(false);
                                        map.unloadDynamicContent();
                                    } else if (tabOptions.selected) {
                                        map.queryResultFeatures.clear(false);
                                        map.getSelection('selection').remove();
                                        map.getSelection('query').remove();
                                        map.getSelection('custom').remove();
                                    }

                                    map.ui._setResultFrameSize($control);
                                }
                            });
                    } else {
                        $tab.data('suppressShowQueryResults', !addSilent);
                        $tabControl
                            .webgis_tab_control('setTabPayload', { id: tabId, payload: tabPayload })
                            .webgis_tab_control('select', {
                                id: tabId,
                                title: $tab.hasClass('pinned-processed') ? null : title,
                                counter: dynamicContentQuery ? null : result.features.length
                            });
                    }
                    if (addSilent !== true) {
                        map.queryResultFeatures.showTable();
                    } else {
                        this._setResultFrameSize();
                    }
                } else {
                    var $list = $resultHolder.find('.webgis-search-result-list');
                    if (result.title) {
                        $("<div>" + result.title + "</div>").appendTo($list);
                    }
                    //$.webgis_search_result_list($list, map, result.features, false, dynamicContentQuery);
                    $list.webgis_queryResultsList({ map: map, features: result.features, beforeAdd: false, dynamicContentQuery: dynamicContentQuery });

                    // history result

                    if ($.webgis_search_result_histories[map] == null)
                        $.webgis_search_result_histories[map] = new resultHistoryItem(map);

                    if (!dynamicContentQuery) {
                        $.webgis_search_result_histories[map].add(result).render($list, result);
                    }
                };

                if (!addSilent) {
                    map.events.fire('showqueryresults', {
                        suppressShowResult: dynamicContentQuery && dynamicContentQuery.suppressShowResult,
                        map: map,
                        buttons: { clearresults: true, showtable: true }
                    });

                    if (result.metadata && result.metadata.custom_selection) {
                        var service = map.queryResultFeatures.firstService(result.features);
                        //console.log(result);
                        console.log('customSelection', result.metadata.custom_selection);
                        if (customSelection.setTargetCustomId) {
                            customSelection.setTargetCustomId(service, result.metadata.custom_selection);
                        }
                    }
                }
            }

            // Select dynamic content toc items
            map.ui.selectDynamicContentItem(dynamicContentQuery);
        }
    };
    this.selectDynamicContentItem = function (dynamicContentQuery) {
        if (dynamicContentQuery) {
            $(".webgis-presentation_toc-dyncontent-item")
                .removeClass('webgis-presentation_toc-dyncontent-item-selected');

            $("#" + dynamicContentQuery.id + ".webgis-presentation_toc-dyncontent-item")
                .addClass('webgis-presentation_toc-dyncontent-item-selected');
        } else {
            $(".webgis-presentation_toc-dyncontent-item")
                .removeClass('webgis-presentation_toc-dyncontent-item-selected');

            this._map.unloadDynamicContent();
        }
    };
    this.dynamicContentIcon = function (dynamicContentQuery) {
        var img = '';

        if (dynamicContentQuery) {
            if (dynamicContentQuery.img) {
                img = content.img;
            }
            else {
                img = webgis.css.imgResource('toc/layers-16.png');
                switch (dynamicContentQuery.type) {
                    case 'geojson':
                    case 'geojson-embedded':
                        img = webgis.css.imgResource('json-16.png', 'toc');
                        break;
                    case 'georss':
                        img = webgis.css.imgResource('rss-16.png', 'toc');
                        break;
                    case 'api-query':
                        img = webgis.css.imgResource('binoculars-16.png', 'toc');
                        break;
                    default:
                        img = webgis.css.imgResource('markers-16.png', 'toc');
                        break;
                }
            }
        }

        return img;
    };
    map.events.on('onloaddynamiccontent', function (channel, sender, args) {
        var dynamicContentId = this._map.getCurrentDynamicContentId();
        var $tabControl = map.ui.getQueryResultTabControl();

        if ($tabControl) {
            var tabs = $tabControl.webgis_tab_control('getTabsOptions');
            $.each(tabs, function (i, tab) {
                if (tab.customData && tab.customData.dynamicContent && tab.customData.dynamicContent.id !== dynamicContentId) {
                    $tabControl.webgis_tab_control('remove', tab);
                }
            });
        }

        if (!dynamicContentId) {
            $(".webgis-presentation_toc-dyncontent-item")
                .removeClass('webgis-presentation_toc-dyncontent-item-selected');
        }
    }, this);

    map.events.on('clearqueryresults', function (channel, sender, args) {
        var $tabControl = map.ui.getQueryResultTabControl();

        if ($tabControl) {
            $tabControl.webgis_tab_control('select', { id: null });
            this._setResultFrameSize($tabControl);
        }
    }, this);

    map.events.on('queryresult_removefeature', function (channel, sender, oid) {
        var $tabControl = map.ui.getQueryResultTabControl();

        if ($tabControl) {
            var $content = $tabControl.webgis_tab_control('currentContent');
            if ($content) {
                $content
                    .find('.webgis-selection-button.selected')
                    .removeClass('selected')
                    .trigger('click'); // Refresh Selection
            }

            var selectedTab = $tabControl.webgis_tab_control('getSelectedTabOptions');
            if (selectedTab && selectedTab.counter) {
                var countFeatuers = map.queryResultFeatures.countFeatures();

                if (countFeatuers === 0) {
                    $tabControl.webgis_tab_control('remove', {
                        id: selectedTab.id
                    });
                } else {
                    $tabControl.webgis_tab_control('setTabCounter', {
                        id: selectedTab.id,
                        counter: map.queryResultFeatures.countFeatures()
                    });
                }
            }
        }
    }, this);

    map.events.on('queryresult_replacefeatures', function (e, map, features) {
        var $tabControl = map.ui.getQueryResultTabControl();

        if ($tabControl) {
            $tabControl.webgis_tab_control('refresh');
        }
    }, this);

    map.events.on('resize', function (channel, sender, args) {
        if (!sender || !sender._webgisContainer) {
            return;
        }

        var $webgisContainer = $(sender._webgisContainer);

        $webgisContainer.find('.webgis-position-relative-to[data-position-element]').each(function (i, element) {
            var $element = $(element);

            var $positionElement = $webgisContainer.find($element.attr('data-position-element'));
            if ($positionElement.length === 1) {
                var positionElementOffset = $positionElement.offset();
                var parentElementOffset = $element.parent().offset();

                if ($element.attr('data-position-left')) {
                    $element.css({
                        left: positionElementOffset.left - parentElementOffset.left + parseInt($element.attr('data-position-left'))
                    });
                }
                if($element.attr('data-position-top')) {
                    $element.css({
                        top: positionElementOffset.top - parentElementOffset.top + parseInt($element.attr('data-position-top'))
                    });
                }
                
            }
        });
    });

    var resultHistoryItem = function (map) {
        var _history = [];
        this._map = map;

        this.contains = function (result) {
            var hash = result && result.metadata ? result.metadata.hash : null;

            for (var r in _history) {
                if (_history[r] == result)
                    return true;

                var historyHash = _history[r].metadata ? _history[r].metadata.hash : null;
                if (hash && hash === historyHash)
                    return true;
            }
            return false;
        };

        this.add = function (result) {
            if (!result || this.contains(result))
                return this;
            if (result.features == null || result.features.length === 0 || result.features[0].oid == null)
                return this;
            // Nur suchen mit Query kommen in die History
            var qid = result.features[0].oid.split(':');
            if (qid.length < 3) // Kein : in oid -> keine normale Abfrage sondern event. Dynamische Inhalt -> gehört nicht in die History
                return this;
            var service = map.services[qid[0]];
            if (!service)
                return this;
            var query = service.getQuery(qid[1]);
            if (!query)
                return this;
            _history.push(result);
            return this;
        };

        this.remove = function (result) {
            var history = [];
            for (var r in _history) {
                if (_history[r] != result)
                    history.push(_history[r]);
            }
            _history = history;
            return this;
        };

        this.count = function () { return _history.length; };

        this.render = function ($parent, exclude) {
            var first = true;
            var $holder = $parent.find('.webgis-result-history-holder');
            if ($holder.length === 0) {
                $holder = $("<div>")
                    .addClass('webgis-result-history-holder collapsed')
                    .css('opacity', .5)
                    .appendTo($parent);

                if (webgis.queryResultOptions.showHistory === false)
                    $holder.css('display', 'none');
            }
            for (var r = _history.length - 1; r >= 0; r--) {
                var result = _history[r];
                if (result === exclude)
                    continue;
                var resultName, fulltext, tool = result.metadata ? result.metadata.tool : '', imgUrl, selected = result.metadata && result.metadata.selected;
                console.log('queryresult tool:', tool);
                for (var f in result.features) {
                    var feature = result.features[f];
                    var query = null;
                    if (feature.oid.substring(0, 1) !== '#') { // Searchservice result....
                        var qid = feature.oid.split(':');
                        var service = map.services[qid[0]];
                        if (!service)
                            continue;
                        query = service.getQuery(qid[1]);
                        if (!query)
                            continue;
                    }
                    if (query) {
                        resultName = (webgis.queryResultOptions.showHeadingCount == false ? "" : result.features.length + " aus ") + query.name;
                        fulltext = feature.properties ? feature.properties._fulltext : null;
                        break;
                    }
                }
                if (first) {
                    first = false;
                    $("<div>")
                        .addClass('webgis-result-history-dropdown')
                        .text("Ergebnisse Verlauf")
                        .appendTo($holder)
                        .click(function (e) {
                            e.stopPropagation();

                            var $holder = $(this).closest('.webgis-result-history-holder');
                            $holder.toggleClass('collapsed');
                        });
                }
                var $div = $("<div>").addClass('webgis-result-history-item')
                    .data('history', this)
                    .data('result', result)
                    .css('padding-left', '34px')
                    .appendTo($holder);
                if (selected)
                    $div.addClass('webgis-result-history-item-selection');
                $("<div>&nbsp;X&nbsp</div>").css({ float: 'right', fontWeight: 'bold' }).appendTo($div)
                    .click(function (event) {
                        event.stopPropagation();
                        var $div = $(this).closest('.webgis-result-history-item'), $holder = $div.closest('.webgis-result-history-holder');
                        var result = $div.data('result');
                        var history = $div.data('history');
                        history.remove(result);
                        $div.remove();
                        if ($holder.find('.webgis-result-history-item').length === 0)
                            $holder.remove();
                    });
                var $info = $("<div>" + resultName + "</div>").appendTo($div);
                switch (tool) {
                    case 'buffer':
                        imgUrl = webgis.css.imgResource('buffer.png', 'tools');
                        break;
                    case 'query':
                        imgUrl = webgis.css.imgResource('binoculars-26-b.png');
                        break;
                    case 'search':
                        imgUrl = webgis.css.imgResource('search-26.png', 'ui');
                        break;
                    case 'edit':
                        imgUrl = webgis.css.imgResource('rest/toolresource/webgis-tools-editing-edit-edit', 'tools');
                        break;
                    case 'tsp':
                        imgUrl = webgis.css.imgResource('roundtrip-26.png', 'tools');
                        break;
                    default:
                        imgUrl = webgis.css.imgResource('rest/toolresource/webgis-tools-identify-identify');
                        break;
                }
                if (imgUrl)
                    $div.css({ backgroundImage: 'url(' + imgUrl + ')', backgroundRepeat: 'no-repeat', backgroundPosition: '4px 4px' });
                if (fulltext) {
                    $("<div>" + fulltext + "...</div>").css({ fontSize: '.8em', color: '#444' }).appendTo($div);
                }
                $div.click(function () {
                    var history = $(this).data('history');
                    $(this).data('history')._map.getSelection('selection').remove();
                    $(this).data('history')._map.ui.showQueryResults($(this).data('result'), true);
                });
            }
            return this;
        };

        map.events.on('queryresult_replacefeatures', function (e, map, features) {
            $.each(_history, function (i, history) {
                if (history.features) {
                    $.each(history.features, function (i, historyFeature) {
                        $.each(features, function (f, feature) {
                            if (historyFeature.oid == feature.oid) {
                                historyFeature.geometry = feature.geometry || historyFeature.geometry;
                                historyFeature.properties = feature.properties || historyFeature.properties;
                                historyFeature.bounds = feature.bounds || historyFeature.bounds;
                            }
                        });
                    });
                }
            });

            // Refresh Table
            var $panel = $(".webgis-dockpanel.webgis-result.webgis-result-table");
            console.log('table-panel', $panel, $panel.length);
            if ($panel.length > 0) {
                map.queryResultFeatures.showTable();
            }
        });
    };

    this.showBufferDialog = function () {
        if (!$.fn.webgis_modal) {
            webgis.alert('WebGIS UI library (webis_modal) is available', 'error');
            return;
        }
        var map = this._map;

        var service = map.queryResultFeatures.firstService();
        var query = map.queryResultFeatures.firstQuery();
        $('body').webgis_modal({
            title: 'Nachbarschaft berechnen',
            id: 'buffer-modal',
            onload: function ($content) {
                $content.css('padding', '10px').addClass('webgis-buffer-holder');
                $("<div style='webgis-input-label'>Thema auswählen</div>").appendTo($content);
                var $select = $("<select class='webgis-select' name='query' />").appendTo($content)
                    .webgis_queryCombo({
                        map: map, type: 'identify' /*'query'*/
                    });
                if (service && query)
                    $select.val(service.id + ':' + query.id);
                $("<div style='webgis-input-label'>Puffer Distanz [m]</div>").css('margin-top', '14px').appendTo($content);
                $("<input class='webgis-input' value='30' name='distance' />").css('width', '287px').appendTo($content);
                $("<br/>").appendTo($content);
                $("<button class='webgis-button'>Nachbarschaft berechnen</button>").css('margin-top', '14px').data('map', map).appendTo($content)
                    .click(function () {
                        var map = $(this).data('map');
                        var $holder = $(this).closest('.webgis-buffer-holder');
                        var serviceId = $holder.find("[name='query']").val().split(':')[0];
                        var queryId = $holder.find("[name='query']").val().split(':')[1];
                        var distance = $holder.find("[name='distance']").val();
                        var sourceServiceId = map.queryResultFeatures.firstService().id;
                        var sourceQueryId = map.queryResultFeatures.firstQuery().id;
                        var sourceOids = map.queryResultFeatures.featureIds(true).toString();
                        webgis.ajax({
                            progress: 'Nachbarschaftsberechnung...',
                            url: webgis.baseUrl + '/rest/services/' + serviceId + '/queries/' + queryId + "?f=json",
                            data: webgis.hmac.appendHMACData(map.addServicesCustomRequestParameters(
                                map.appendRequestParameters({  // resquestParameters inkl filters
                                    c: 'buffer',
                                    source_serviceid: sourceServiceId,
                                    source_queryid: sourceQueryId,
                                    source_oids: sourceOids,
                                    distance: distance,
                                    calc_srs: map.calcCrs().id
                                }, serviceId)), true),
                            type: 'post',
                            success: function (result) {
                                if (webgis.checkResult(result)) {
                                    map.getSelection('selection').remove();
                                    map.getSelection('query').remove();
                                    map.events.fire('onnewfeatureresponse');  // close dock panel...

                                    map.ui.showQueryResults(result, true);
                                }
                            },
                            error: function () {
                            }
                        });
                        $('body').webgis_modal('close', { id: 'buffer-modal' });
                    });
            },
            width: '330px', height: '280px'
        });
    }

    this._setResultFrameSize = function ($tabControl) {
        $tabControl = $tabControl || this._map.ui.getQueryResultTabControl();
        if ($.fn.webgis_splitter) {
            var tabs = $tabControl.webgis_tab_control('getTabsOptions');
            var hasSelected = $.grep(tabs, function (tab) { return tab.selected }).length > 0;
            var featureCount = this._map.queryResultFeatures.countFeatures();

            if (!hasSelected) {
                $tabControl
                    .closest('.webgis-splitter')
                    .webgis_splitter('setSize', {
                        selectorClass: 'query-results-tab-control-container',
                        size: tabs.length > 0 ? 30 : 0, absolute: true,
                        map: map
                    });
            } else {
                $tabControl
                    .closest('.webgis-splitter')
                    .webgis_splitter('setSize', {
                        selectorClass: 'query-results-tab-control-container',
                        size: 105 + (Math.min(featureCount, 3) + 1) * 34 + 30 + 20, 
                        // +20 ...scrollbar size
                        map: map
                    });
            }
        }
    };

    $(this._map._webgisContainer).find('.webgis-map-title').click(function (e) {
        e.stopPropagation();
        $('#webgis-info').trigger('click');
    });

    $(this._map._webgisContainer).find('.query-results-tab-control-container').webgis_tab_control({ map: map });
};