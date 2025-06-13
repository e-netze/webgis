var globals = {};
webgis.globals = webgis.globals || {};
webgis.globals.portal = webgis.globals.portal || {};
webgis.globals.forceAddTools = false;
webgis.globals.urlParameters = null;
globals.viewerLayoutTemplates = null;
globals.viewerLayoutStyleClass = null;
globals.viewerLayoutTemplateStorageKey = 'map.properties.template.'

webgis.mapInitializer = (function (m) {
    "use strict"

    var $ = webgis.$;

    var map = null;
    var relPathPrefix = '';
    var absPath = m.portalPath ? m.portalPath + '/' + m.portalPage + '/map/' + m.category + '/' : '';

    globals.isUserProject = m.projectUrlName ? true : false;

    var mapPortalPage = m.portalPage;
    var mapPortalPageName = m.portalPageName;
    var mapCategory = m.category;
    var mapUrlName = m.urlName;
    var projectUrlName = m.projectUrlName;
    var calcCrs = m.calcCrs;
    var targetId = m.targetId;
    var onReady = m.onReady;

    if (mapUrlName && mapCategory) {
        webgis.advancedOptions.get_serviceinfo_purpose = mapCategory + '/' + mapUrlName;
    } else if (projectUrlName) {
        webgis.advancedOptions.get_serviceinfo_purpose = 'User-project';
    }

    globals.mapPortalPage = webgis.globals.portal.pageId = m.portalPage;
    globals.mapUrlName = webgis.globals.portal.mapName = m.urlName;
    globals.mapCategory = webgis.globals.portal.mapCategory = m.category;
    globals.projectUrlName = webgis.globals.portal.projectName = m.projectUrlName;

    if (webgis.initialParameters.original) {
        for (var p in webgis.initialParameters.original) {
            if (p == '_temp_graphics' || p == '_temp_graphics_x')
                continue;

            webgis.globals.urlParameters = webgis.globals.urlParameters || {};
            webgis.globals.urlParameters[p] = webgis.initialParameters.original[p];
        }
    }
    console.log('webgis.globals.urlParameters', webgis.globals.urlParameters);

    $(document).ready(function () {

        var info = "<table style='color:#aaa'>";
        info += "<tr><td>API-Version</td><td>" + webgis.api_version + "</td></tr>";
        info += "<tr><td>Map-Framework</td><td>" + webgis.mapFramework + " " + webgis.mapFramework_version + "</td></tr>";
        info += "</table>";
        webgis.$(info).appendTo($('#api-version-info'));

        var init = function () {
            if (calcCrs > 0 || typeof calcCrs === 'function') {
                webgis.calc.setCalcCrs(calcCrs);
            }

            if (projectUrlName) {
                relPathPrefix = absPath + '';
                webgis.globals.forceAddTools = true;   // bei alten Projekten wurde in ui.options.tabs.add_tools leider auf false gesetzt
                webgis.ajax({
                    progress: 'Lade Projekt Informationen',
                    url: webgis.url.relative(absPath + '../proxy/toolmethod/webgis-tools-serialization-loadmap/mapjson'),
                    data: { project: projectUrlName/*, add_master: m.queryMaster || false*/ },
                    type: 'post',
                    success: loadMap
                });
            } else {
                relPathPrefix = absPath + '../../';
                webgis.ajax({
                    progress: 'Lade Karten Informationen',
                    url: webgis.url.relative(absPath + '../../../proxy/toolmethod/webgis-tools-portal-publish/mapjson'),
                    type: 'post',
                    data: { page: mapPortalPage, category: _serializationMapCategory || mapCategory, map: _serializationMapUrlName || mapUrlName, add_master: m.queryMaster || false },
                    success: loadMap
                });
            }
            globals.relPathPrefix = relPathPrefix;
            
            webgis.$('#webgis-info').click(function () {
                if ($.fn.webgis_presentationToc) {
                    var title = webgis.globals && webgis.globals.portal ? webgis.globals.portal.mapName : null;
                    $.presentationToc.showLegend(null, map, map.services, title || "Darstellung, ©,...", 2);
                } else {
                    webgis.$('body').webgis_modal({
                        title: 'Info',
                        onload: function ($content) {
                            var $pane = $('#webgis-info-pane').clone();
                            $pane.find('.tabs').find('.tab')
                                .click(function () {
                                    $(this).closest('.tabs').find('.tab').removeClass('selected');
                                    $(this).closest('.tab-control').find('.tab-pane').css('display', 'none');
                                    $(this).addClass('selected');
                                    $(this).closest('.tab-control').find('.' + $(this).attr('data-ref')).css('display', '');
                                });
                            $pane.css('display', '').appendTo($content);
                        },
                        width: '330px', height: '400px'
                    });
                }
            }).css('backgroundImage', 'url(' +/* webgis.url.relative(relPathPrefix + '../Content/img/user-24.png')*/ webgis.css.imgResource('user-24.png') + ')');

            $('#mapimage-preview').css('backgroundImage', 'url(' + webgis.url.relative(relPathPrefix + 'mapimage?category=' + webgis.url.encodeString(mapCategory) + '&map=' + webgis.url.encodeString(mapUrlName)) + ')');
            $('#upload-image-form').attr('action', webgis.url.relative(relPathPrefix + 'UploadMapImage'));

            $('#topbar, #toolbar').addClass('webgis-map-overlay-ui-element');
        };

        webgis.init(function () {
            var initFunction = function () {
                if (webgis.usability.clickBubble === true) {
                    webgis.markerIcons["sketch_vertex"] = webgis.markerIcons["sketch_vertex_square"];
                }
                else if (!webgis.isTouchDevice()) {
                    webgis.markerIcons["sketch_vertex"] = webgis.markerIcons["sketch_vertex_square"]; // webgis.markerIcons["sketch_vertex_circle"];
                }
                else {
                    webgis.markerIcons["sketch_vertex"] = webgis.markerIcons["sketch_vertex_bottom"];
                }

                if (m.queryLayout === true) {
                    var queryLayout = function (template) {
                        $.ajax({
                            url: portalUrl + '/' + mapPortalPage + '/maplayout',
                            data: { width: $(window).width(), height: $(window).height(), template: template || 'default' },
                            type: 'post',
                            success: function (result) {
                                if (result.succeeded && result.html) {
                                    $('#webgis-container').empty().html(result.html);
                                }
                                init();
                            },
                            error: function () {
                                init();
                            }
                        });
                    };
                    var queryLayoutAndTemplates = function () {
                        $.ajax({
                            url: portalUrl + '/' + mapPortalPage + '/maplayout-templates',
                            data: { width: $(window).width(), height: $(window).height() },
                            type: 'post',
                            success: function (result) {
                                globals.viewerLayoutTemplates = result;
                                var template = result && result.width ? 
                                    webgis.localStorage.get(globals.viewerLayoutTemplateStorageKey + result.width) :
                                    null;

                                queryLayout(template);
                            },
                            error: function () {
                                globals.viewerLayoutTemplates = null;
                                queryLayout();
                            }
                        });
                    };

                    if (webgis.usability.allowViewerLayoutTemplateSelection) {
                        queryLayoutAndTemplates();
                    } else {
                        queryLayout();
                    }
                } else {
                    init();
                }

                if (mapMessage) {
                    webgis.delayed(function () {  // show delayed...
                        webgis.alert(mapMessage, 'info');
                    }, 1000);
                }

                if (webgis.usability.showMapTipsSinceVersion &&
                    webgis.localStorage.usable()) {

                    var lastTipsFrom = webgis.localStorage.get('webgis-last-tips-from');
                    if (!lastTipsFrom || lastTipsFrom < webgis.usability.showMapTipsSinceVersion) {

                        webgis.delayed(function () {
                            webgis.confirm({
                                title: 'Neue WebGIS Funktionalität',
                                height: '470px',
                                message: 'WebGIS bietet seit Version ' + webgis.usability.showMapTipsSinceVersion + ' neue Funktionalitäten an. Möchten Sie mehr über diese Neuerungen erfahren?',
                                iconUrl: webgis.css.imgResource('tips-100.png'),
                                okText: 'Ja, Neuerungen anzeigen',
                                maybeText: 'Vielleicht später',
                                cancelText: 'Nein Danke, bereits bekannt',
                                onOk: function () {
                                    webgis.localStorage.set('webgis-last-tips-from', webgis.usability.showMapTipsSinceVersion);
                                    webgis.showHelp('news/build_' + webgis.usability.showMapTipsSinceVersion.replaceAll('.', '_') + '.html');
                                },
                                onCancel: function () {
                                    webgis.localStorage.set('webgis-last-tips-from', webgis.usability.showMapTipsSinceVersion);
                                }
                            });
                        }, 1100);
                    } else if (webgis.isTouchDevice() === false) {
                        webgis.showTip('tip-desktop-navigation');
                    }
                }
            };

            if (webgis.hmac) {
                webgis.mapExtensions.checkFavoriteStatus(initFunction, false);
            } else {
                initFunction();
            }
        });

        var $historyButton = $("<div>exit</div>")
            .css('display', 'none')
            .appendTo('body')
            .click(function () {

                // Cleanup
                if (globals.map && globals.map.ui.canCleanupDisplay()) {
                    globals.map.ui.cleanupDisplay();
                    webgis.delayed(function (button) {
                        webgis.setHistoryItem(button);
                    }, 500, this);
                    return;
                }

                if (confirm("Möchten Sie die Karte " + mapUrlName + " verlassen?")) {
                    window.history.back();
                } else {
                    webgis.delayed(function (button) {
                        webgis.setHistoryItem(button);
                    }, 500, this);
                }
            });

        webgis.delayed(function () {
            webgis.setHistoryItem($historyButton);
        }, 500);
    });

    webgis.$(document).on("keyup", function (e) {
        if (e.key === "Escape") {

            const selectors = [
                '.webgis-modal-close:visible',   // Modal Dialogs
                '.webgis-dockpanel-close:visible', // Modal Dialogs
                '.webgis-modal button.uibutton-cancel:visible',  // Close/Exit Tool
                '.webgis-tooldialog-header.secondary .close:visible', // Secondary Tool Window
                '.webgis-tooldialog-header.primary .close:visible' // Primary Tool Window
            ];

            for (var s in selectors) {
                let selector = selectors[s];
                let $element = $(selector).last();
                if ($element.length === 1) {
                    //console.log($element);
                    //console.log('trigger => ' + selector);
                    $element.trigger('click');
                    break;
                }
                //console.log(selector + " => " + $element.length);
            }
        }
    });
    webgis.$(document).on('keydown', function (e) {
        if (e.ctrlKey === true) {

            const selectors = [
                '.webgis-modal .webgis-button',   // Modal Dialogs
                //'.webgis-dockpanel-close:visible', // Modal Dialogs
                //'.webgis-modal button.uibutton-cancel:visible',  // Close/Exit Tool
                //'.webgis-tooldialog-header.secondary .close:visible', // Secondary Tool Window
                //'.webgis-tooldialog.primary .close:visible' // Primary Tool Window
            ];

            for (var s in selectors) {
                let selector = selectors[s];
                let $element = $(selector + "[data-ctrl-shortcut='" + e.key.toLowerCase() + "']").first();
                if ($element.length === 1) {
                    $element.trigger('click');
                    return false;
                }
                //console.log(selector + " => " + $element.length);
            }
        }
    });

    function MapUiOptions(target, master) {
        if (!master || !master.ui || !master.ui.options ||
            !target) {
            return target || master;
        } 

        // Mapping Functions
        function MapBoolProperties(target, master, properties) {
            if (target && master) {
                for (var p in properties) {
                    var property = properties[p];
                    target[property] = target[property] || master[property];
                }
            }
        }
        function MapArray(target, master) {
            if (master) {
                for (var m in master) {
                    if ($.inArray(master[m], target) < 0) {
                        //console.log('append to array', master[m]);
                        target.push(master[m]);
                    }
                }
            }
        }
        function MapStringArrayProperties(target, master, properties) {
            if (target && master) {
                for (var p in properties) {
                    var property = properties[p];

                    if (!target[property]) {
                        target[property] = master[property];
                    } else if (master[property]) {
                        var targetArray = target[property].toString().split(',');
                        var masterArray = master[property].toString().split(',');

                        $.each(targetArray, function (i, element) {
                            if ($.inArray(element, masterArray) < 0) {
                                masterArray.push(element);
                            }
                        });

                        target[property] = masterArray.toString();

                        //console.log('map array:', target[property]);
                    }
                }
            }
        }

        // Map
        try {
            if (!target.ui) {
                target.ui = master.ui;
            }
            if (!target.ui.options) {
                target.ui.options = master.ui.options;
            }

            $.each(master.ui.options, function (index, masterOptionElement) {
                if (masterOptionElement && masterOptionElement.options) {

                    var targetOptionElement = $.grep(target.ui.options, function (targetOptionElement) {
                        return targetOptionElement.element === masterOptionElement.element;
                    });
                    // fristOrDefault
                    targetOptionElement = targetOptionElement.length > 0 ? targetOptionElement[0] : null;

                    if (targetOptionElement === null) {
                       // console.log('targetOptionElement == null', masterOptionElement.element);
                        target.ui.options.push(masterOptionElement);
                    } else if (!targetOptionElement.options || targetOptionElement.options.length === 0) {
                        targetOptionElement.options = masterOptionElement.options;
                    } else {

                        if (masterOptionElement.element === 'topbar' && masterOptionElement.options) {
                            MapStringArrayProperties(targetOptionElement.options, masterOptionElement.options, ["quick_search_service"]);
                            MapBoolProperties(targetOptionElement.options, masterOptionElement.options, ["quick_search", "detail_search", "app_menu"]);
                        }

                        else if (masterOptionElement.element === 'tabs') {

                            MapBoolProperties(targetOptionElement.options, masterOptionElement.options, ["add_presentations", "add_settings", "add_tools", "add_queryResults"]);

                            if (masterOptionElement.options.options_presentations && masterOptionElement.options.options_presentations) {
                                MapBoolProperties(targetOptionElement.options.options_presentations, masterOptionElement.options.options_presentations, ["gdi_button"]);
                            }

                            // Tool Containers
                            if (masterOptionElement.options.options_tools && masterOptionElement.options.options_tools.containers)
                                if (!targetOptionElement.options.options_tools) {
                                    targetOptionElement.options.options_tools = masterOptionElement.options.options_tools;
                                }
                                else if (!targetOptionElement.options.options_tools.containers) {
                                    targetOptionElement.options.options_tools.containers = masterOptionElement.options.options_tools.containers;
                                }
                                else {
                                    $.each(masterOptionElement.options.options_tools.containers, function (c_index, masterContainer) {
                                        if (masterContainer && masterContainer.name) {
                                            var targetContainer = $.grep(targetOptionElement.options.options_tools.containers, function (targetContainer) {
                                                return targetContainer.name === masterContainer.name;
                                            });

                                            targetContainer = targetContainer.length > 0 ? targetContainer[0] : null;

                                            if (targetContainer === null) {
                                                targetOptionElement.options.options_tools.containers.push(masterContainer);
                                            } else {
                                                if (!targetContainer.tools && masterContainer.tools) {
                                                    targetContainer.tools = masterContainer.tools;
                                                }
                                                if (!targetContainer.options && masterContainer.options) {
                                                    targetContainer.options = masterContainer.options;
                                                }

                                                MapArray(targetContainer.tools, masterContainer.tools);
                                                MapArray(targetContainer.options, masterContainer.options);
                                            }
                                        }
                                    });
                                }
                        }
                    }
                }
            });

        } catch (e) {
            console.log(e);
        }

        return target;
    }

    function loadMap(json) {
        //console.log(json);
        var refMapJson = new webgis.refParameter(json.mapjson || json.serializationmapjson);
        var graphics = json.graphics;

        if (typeof refMapJson.value === 'string')
            refMapJson.value = webgis.$.parseJSON(refMapJson.value);

        if (json.success === false) {
            webgis.alert(
                'Beim Laden der Karte ist ein Fehler aufgetreten: ' + json.exception,
                'error',
                function () {
                    document.location = portalUrl + '/' + mapPortalPage;
                }
            );
            return;
        }

        /******** Master ************************************/
        if (json.ui_master0 || json.ui_master1) {
            try {
                var master0 = typeof json.ui_master0 === 'string' ? webgis.$.parseJSON(json.ui_master0) : json.ui_master0;
                var master1 = typeof json.ui_master1 === 'string' ? webgis.$.parseJSON(json.ui_master1) : json.ui_master1;

                //console.log('master0', master0);
                //console.log('master1', master1);

                master1 = MapUiOptions(master1, master0);
                refMapJson.value = MapUiOptions(refMapJson.value, master1);

                //console.log('json.ui', refMapJson.value.ui);
            } catch (e) {
                console.log("Map master properties error", e);
            }
        }

        //console.log(refMapJson);

        /******** Mapbuilder initialDefaults ********/
        if (refMapJson.value.userdata && refMapJson.value.userdata.mapbuilder && refMapJson.value.userdata.mapbuilder.initialDefaults) {
            var initialDefaults = refMapJson.value.userdata.mapbuilder.initialDefaults;
            webgis.initialParameters = webgis.initialParameters || {};
            webgis.initialParameters.toolid = webgis.initialParameters.toolid || initialDefaults["initial-tool"];
            webgis.initialParameters.query = webgis.initialParameters.query; // || initialDefaults["initial-query"];
            webgis.initialParameters.querythemeid = webgis.initialParameters.querythemeid || webgis.initialParameters.query || initialDefaults["initial-query"];
            webgis.initialParameters.editthemeid = webgis.initialParameters.editthemeid || initialDefaults["initial-edittheme"];
        }

        /******** UI Bereinigungen ***************************/

        /******** Tabs to default ****************************/
        /** Sonst gibt es Probleme, wenn man Karte im Desktop Layout speichert und dann im Mobile Layout öffnet **/

        var tabsElement = $.grep(refMapJson.value.ui.options, function (e) {
            return e.element === 'tabs';
        });

        if (tabsElement.length === 1) {
            tabsElement[0].options.add_tools = tabsElement[0].options.add_tools || webgis.globals.forceAddTools;

            if (!tabsElement[0].options.add_tools) {
                $('#webgis-container').find('#toolbar').remove();
            }

            if (tabsElement[0].selector !== '#webgis-container') {
                tabsElement[0].selector = '#webgis-container';
                tabsElement[0].options = tabsElement[0].options || {};
                tabsElement[0].options.left = null;
                tabsElement[0].options.right = 0;
                tabsElement[0].options.bottom = 24;
                tabsElement[0].options.top = null;
                tabsElement[0].options.add_tools = true;
                tabsElement[0].options.content_size = null;
            }
        }

        /***********************************************************/
        var userdata = refMapJson.value.userdata;

        // Apply Snapshot 
        if (userdata && userdata.mapbuilder && userdata.mapbuilder.snapshots && userdata.mapbuilder.snapshots.active) {
            var snapshot0 =
                webgis.firstOrDefault(userdata.mapbuilder.snapshots.snapshots, function (s) { return webgis.initialParameters.snapshot_name && s.name === webgis.initialParameters.snapshot_name });
            var snapshot1 =
                webgis.firstOrDefault(userdata.mapbuilder.snapshots.snapshots, function (s) { return s.name === userdata.mapbuilder.snapshots.active });

            var snapshotMap =
                (snapshot0 && snapshot0.map ? snapshot0.map : null) ||
                (snapshot1 && snapshot1.map ? snapshot1.map : null);

            // Apply Bounds, center, scale
            if (snapshotMap) {
                if (snapshotMap.scale) {
                    refMapJson.value.scale = snapshotMap.scale;
                }
                if (snapshotMap.center && snapshotMap.center.length === 2) {
                    refMapJson.value.center = snapshotMap.center;
                }
                if (snapshotMap.bounds && snapshotMap.bounds.length === 4) {
                    refMapJson.value.bounds = snapshotMap.bounds;
                }
            }

            var snapshotPresentations =
                (snapshot0 && snapshot0.presentations ? snapshot0.presentations : null) ||
                (snapshot1 && snapshot1.presentations ? snapshot1.presentations : null);

            // Apply Layer Visibility
            if (snapshotPresentations && snapshotPresentations.visibility) {
                for (var v in snapshotPresentations.visibility) {
                    var visibility = snapshotPresentations.visibility[v];
                    var service = webgis.firstOrDefault(refMapJson.value.services, function (s) { return s.id === visibility.service && s.layers });

                    if (service) {
                        for (var l in service.layers) {
                            service.layers[l].visible = $.inArray(service.layers[l].id, visibility.layers) >= 0;
                        }
                    }
                }
            }
        }

        var ui = refMapJson.value.ui;
        if (ui && ui.options) {
            var topbar = ui.options.find((i) => i.element === "topbar");

            if (topbar && webgis.initialParameters.appendsearchservices) {
                if (topbar.options.quick_search_service === "geojuhu") {
                    topbar.options.quick_search_service = webgis.initialParameters.appendsearchservices;
                } else {
                    topbar.options.quick_search_service += ((topbar.options.quick_search_service) ? "," : ":") + webgis.initialParameters.appendsearchservices;
                }
            }

            //console.log('topbar', topbar);
        }

        map = webgis.createMapFromJson(targetId, refMapJson, function () {
                if (graphics) {
                    map.graphics.fromGeoJson(graphics);
                }
                $('#' + targetId).css('opacity', 1);
            },
            mapUrlName,
            webgis.initialParameters.appendservices);
        $('#' + targetId).data('webgis-map', map);

        globals.map = map;

        webgis.events.fire('viewer-map-created', webgis, map);

        console.log('map.calcCrs',map.calcCrs());
        if (calcCrs === 0 && map.calcCrs()) {
            webgis.calc.setCalcCrs(map.calcCrs().id);
        }

        // DoTo: eventuell sollte der Code ausgeführt werden, bevor der erste MapRequest rausgeht...
        if (webgis.initialParameters && webgis.initialParameters.filters) {
            for (var f in webgis.initialParameters.filters) {
                var filter = webgis.initialParameters.filters[f];
                map.setFilter(filter.id, filter.args);
            }
        }

        // apply Map Viewer Properties
        $(null).webgis_mapProperties('applySettings', { map: map });

        // Perform Parameter query when service is loaded...
        var onAddService = function (e, service) {
            var query = service.getQuery(webgis.initialParameters.query);
            if (query && query.items) {
                var items = {}, foundItem = false;
                for (var i in query.items) {
                    var item = query.items[i];
                    if (webgis.initialParameters.original[item.id]) {
                        foundItem = true;
                        items[item.id] = webgis.initialParameters.original[item.id];
                        webgis.initialParameters.original[item.id] = null;  // avoid executing query twice
                    }
                }
                if (foundItem === true) {
                    map.appendRequestParameters(items, service.id);
                    var timer = new webgis.timer(function (items) {
                        webgis.ajax({
                            url: webgis.baseUrl + '/rest/services/' + service.id + '/queries/' + query.id + '?f=json',
                            type: 'post',
                            data: webgis.hmac.appendHMACData(items),
                            success: function (result) {
                                map.ui.showQueryResults(result, true);
                                if (webgis.initialParameters.scale && result.bounds && result.bounds.length === 4) {
                                    var boundsCenter = L.latLng((result.bounds[1] + result.bounds[3]) * .5, (result.bounds[0] + result.bounds[2]) * .5);
                                    //console.log(boundsCenter);
                                    service.map.setScale(webgis.initialParameters.scale, boundsCenter);
                                }

                                if (result && result.features) {
                                    var noselect = webgis.initialParameters.original["mode"] === "noselect";
                                    var nohighlight = webgis.initialParameters.original["mode"] === "nohighlight";

                                    if (!noselect && !nohighlight) {
                                        $('.webgis-selection-button').trigger('click'); // Select (quick & drity)
                                    }
                                    else if (!nohighlight && result.features && result.features.length === 1) {
                                        webgis.delayed(function () {
                                            $('.webgis-toggle-button.webgis-dependencies.webgis-dependency-ishighlighted').trigger('click'); // Select (quick & drity)
                                        }, 500);
                                    }
                                }
                            }
                        });
                    }, 500, items);
                    timer.start();
                }
            }

            if (webgis.initialParameters.presentations) {
                for (var p in webgis.initialParameters.presentations) {
                    var presentationName = webgis.initialParameters.presentations[p];
                    var presentationVisible = true;

                    if (webgis.initialParameters.presentations[p].indexOf('=') > 0) {
                        presentationName = webgis.initialParameters.presentations[p].split('=')[0];
                        presentationVisible = webgis.initialParameters.presentations[p].split('=')[1].toLowerCase() !== 'off';
                    }
                    var $item = $(".webgis-presentation_toc-item[data-dvid='" + presentationName + "']");
                    if ($item.length > 0) {
                        webgis.initialParameters.presentations[p] = "";
                        if ($item.hasClass('webgis-presentation_toc-checkable')) {
                            var $check = $item.find('.webgis-presentation_toc-checkable-icon');
                            var checkSrc = $check.attr('src') || '';
                            if ((checkSrc.indexOf('check0') >= 0 && presentationVisible === true) ||
                                (checkSrc.indexOf('check1') >= 0 && presentationVisible === false) ||
                                (checkSrc.indexOf('option0') >= 0 && presentationVisible === true) ||
                                (checkSrc.indexOf('option1') >= 0 && presentationVisible === false)) {
                                $item.trigger('click');
                            }
                        } else {
                            $item.trigger('click');
                        }
                    }
                }
            }
            if (webgis.initialParameters.showlayers) {
                //console.log('service', service.id);
                //console.log('webgis.initialParameters.showLayers', webgis.initialParameters.showlayers)
                var ids = service.getLayerIdsFromNames(webgis.initialParameters.showlayers);
                //console.log('ids', ids);
                if (ids && ids.length > 0) {
                    service.setLayerVisibility(ids, true);
                }
            }
            if (webgis.initialParameters.hidelayers) {
                var ids = service.getLayerIdsFromNames(webgis.initialParameters.hidelayers);
                if (ids && ids.length > 0) {
                    service.setLayerVisibility(ids, false);
                }
            }
        };
        map.events.on('onaddservice', onAddService);
        for (var s in map.services) {
            onAddService({}, map.services[s]);
        };

        var mapJson = refMapJson.value;

        // Toolbar: wird über event getriggert (sobald toolbox fertig ist... danach sind die tools mit .appTool() in der map...)
        var initToolBars = function () {
            //console.log('Init Tool Bars');
            if (mapJson.ui && mapJson.ui.options) {
                for (var o in mapJson.ui.options) {
                    if (mapJson.ui.options[o].element === "tabs" && mapJson.ui.options[o].options && mapJson.ui.options[o].options.options_tools && mapJson.ui.options[o].options.options_tools.containers) {
                        if ($('#webgis-container').find('#toolbar').length > 0) {
                            $('#webgis-container').find('#toolbar').webgis_toolbar({
                                map: map,
                                containers: mapJson.ui.options[o].options.options_tools.containers
                            });
                        }
                    }
                }
            }
        }

        // Toolbar: manchmal ist die webgis_toolbox schon fertig und das Event wurde schon schon getriggert
        //          Meistens bei FF => dann initToolBars gleich aufrufen!! Sonst bleibt Toolbox leer
        if ($.fn.webgis_toolbox && $.fn.webgis_toolbox._toolsLoaded === true) {
            console.log('$.fn.webgis_toolbox._toolsLoaded', $.fn.webgis_toolbox._toolsLoaded);
            initToolBars();
        } else {
            console.log('register toolboxloaded');
            map.events.on('toolboxloaded', initToolBars);
        }

        // Default Tool
        var defaultToolOptions = null;
        if (userdata && userdata.mapbuilder && userdata.mapbuilder.defaulttool) {
            for (var i in userdata.mapbuilder.defaulttool) {
                var defaultTool = userdata.mapbuilder.defaulttool[i]
                if (defaultTool.selected === true) {
                    map.setDefaultToolId(defaultTool.id);
                    if (defaultTool.options)
                        defaultToolOptions = defaultTool.options;
                    break;
                }
            }
        }

        webgis.delayed(function () {
            if (webgis.initialParameters) {
                if (webgis.initialParameters.bbox)
                    map.zoomTo(webgis.initialParameters.bbox);

                if (webgis.initialParameters.center) {
                    map.setScale(webgis.initialParameters.scale || webgis.usability.zoom.minFeatureZoom, webgis.initialParameters.center);
                }

                if (webgis.initialParameters.marker && webgis.initialParameters.marker.lng && webgis.initialParameters.marker.lat) {
                    map.addMarker(webgis.initialParameters.marker);
                    if (!webgis.initialParameters.bbox && !webgis.initialParameters.center)
                        map.setScale(webgis.initialParameters.scale || webgis.usability.zoom.minFeatureZoom, webgis.initialParameters.marker);
                }
                if (webgis.initialParameters.markers && webgis.initialParameters.markers.length > 1) {
                    var markerCollection = {
                        type: "FeatureCollection",
                        features: []
                    };
                    for (var m in webgis.initialParameters.markers) {
                        var marker = webgis.initialParameters.markers[m];
                        markerCollection.features.push({
                            oid: m,
                            type: "Feature",
                            geometry: { type: "Point", coordinates: [marker.lng, marker.lat] },
                            properties: { _fulltext: marker.text || '' }
                        });
                    }

                    map.addDynamicContent([{
                        id: webgis.guid(),
                        name: webgis.initialParameters.markers_name || 'Markers',
                        type: 'feature-collection',
                        featureCollection: markerCollection
                    }], true);
                }

                if (webgis.initialParameters.basemap) {
                    var basemaps = webgis.initialParameters.basemap.split(',');
                    for (var b in basemaps) {
                        map.setBasemap(basemaps[b], b > 0);
                    }
                }

                if (webgis.initialParameters.original._temp_graphics && webgis.initialParameters.original._temp_graphics_x) {

                    var grGuid = webgis.initialParameters.original._temp_graphics,
                        pwGuid = webgis.initialParameters.original._temp_graphics_x,
                        temp_graphics = webgis.localStorage.get('_temp_graphics_' + grGuid);

                    if (temp_graphics) {
                        webgis.delayed(function () {
                            try {
                                map.graphics.fromGeoJson({ geojson: webgis.cryptoJS.simpleDecrypt(pwGuid, temp_graphics) });
                            } catch (e) { }
                        }, 2000);

                        webgis.localStorage.remove('_temp_graphics_' + grGuid);
                    }
                }
            }
            $('#' + targetId).css('opacity', 1);

            if (!map.getActiveTool() && map.getDefaultToolId()) {
                webgis.loadTool(map.getDefaultToolId(), function (tool) {
                    map.addTool(tool);
                });
            }
            if (defaultToolOptions)
                map.setToolOptions(map.getDefaultTool(), defaultToolOptions);

            if (userdata && userdata.mapbuilder && userdata.mapbuilder.actions) {
                if ((userdata.mapbuilder.actions.onstart_zoom2position && webgis.isMobileDevice()) ||
                    (userdata.mapbuilder.actions.onstart_zoom2position_all_clients)) {
                    var cancelTracker = new webgis.cancelTracker();
                    webgis.showProgress('Aktuelle Position wird abgefragt...', null, cancelTracker);

                    webgis.currentPosition.get({
                        maxWatch: 1,
                        onSuccess: function (pos) {
                            webgis.hideProgress('Aktuelle Position wird abgefragt...');
                            if (!cancelTracker.isCanceled()) {
                                var lng = pos.coords.longitude, lat = pos.coords.latitude, acc = pos.coords.accuracy / (webgis.calc.R) * 180.0 / Math.PI;
                                map.zoomTo([lng - acc, lat - acc, lng + acc, lat + acc]);
                                //map.toMarkerGroup('currentPos',
                                //    map.addMarker({
                                //        lat: lat, lng: lng,
                                //        text: "<h3>Mein Standort</h3>Genauigkeit: " + pos.coords.accuracy + "m<br/>",
                                //        openPopup: true,
                                //        icon: "currentpos_red",
                                //        buttons: [
                                //            {
                                //                label: 'Aktualisieren',
                                //                onclick: function (map) {
                                //                    webgis.tools.onButtonClick(map, { type: 'clientbutton', command: 'currentpos' });
                                //                }
                                //            },
                                //            {
                                //                label: 'Entfernen',
                                //                onclick: function (map, marker) {
                                //                    //map.removeMarkerGroup('currentPos');
                                //                    map.removeMarker(marker);
                                //                }
                                //            }
                                //        ]
                                //    })
                                //);
                            }
                        },
                        onError: function (err) {
                            webgis.hideProgress('Aktuelle Position wird abgefragt...');
                            //alert('Ortung nicht möglich: ' + (err ? err.message + " (Error Code " + err.code + ")" : ""));
                        }
                    });
                }
            }

            if (userdata && userdata.mapbuilder && userdata.mapbuilder.snapshots && userdata.mapbuilder.snapshots.active) {
                var snapshot0 =
                    webgis.firstOrDefault(userdata.mapbuilder.snapshots.snapshots, function (s) { return webgis.initialParameters.snapshot_name && s.name === webgis.initialParameters.snapshot_name });
                var snapshot1 =
                    webgis.firstOrDefault(userdata.mapbuilder.snapshots.snapshots, function (s) { return s.name === userdata.mapbuilder.snapshots.active });

                var snapshotPresentations =
                    (snapshot0 && snapshot0.presentations ? snapshot0.presentations : null) ||
                    (snapshot1 && snapshot1.presentations ? snapshot1.presentations : null);

                if (snapshotPresentations) {
                    var $toc = $('#tab-presentations-content');
                    if (snapshotPresentations.expanded && $.fn.webgis_presentationToc) {
                        $toc.webgis_presentationToc('expand', { names: snapshotPresentations.expanded, exclusive: true });
                    }
                }
            }

            if (webgis.initialParameters && webgis.initialParameters.toolid) {
                var initialTool = map.getTool(webgis.initialParameters.toolid);
                if (initialTool) {
                    webgis.tools.onButtonClick(map, initialTool);
                }
            }

            if (webgis.initialParameters && webgis.initialParameters.editfieldvalues) {
                //console.log(webgis.initialParameters);
                for (var id in webgis.initialParameters.editfieldvalues) {
                    //console.log(id);
                    //console.log(webgis.initialParameters.editfieldvalues[id]);
                    map.setPersistentToolParameter('webgis.tools.editing.edit', 'editfield_' + id, webgis.initialParameters.editfieldvalues[id], "force-set");
                }
            }

            if (webgis.initialParameters && !webgis.initialParameters.query && $.fn.webgis_queryCombo) {
                //console.log('Select first query in list');
                $(".webgis-detail-search-combo").webgis_queryCombo('selectfirst');
            }

            if (webgis.usability.useMyPresentations === true && webgis.hmac.isAnonymous() === false) {
                if (webgis.globals.portal.mapName) {
                    $('body').webgis_favoritesTocContainer({
                        relPathPrefix: relPathPrefix,
                        map: map,
                        pageId: webgis.globals.portal.pageId,
                        mapName: webgis.globals.portal.mapName
                    });
                } else if (webgis.globals.portal.projectTitle) {
                    $('body').webgis_favoritesTocContainer({
                        relPathPrefix: relPathPrefix,
                        map: map,
                        pageId: webgis.globals.portal.pageId,
                        mapName: webgis.globals.portal.projectTitle,
                        toolid: 'webgis.tools.serialization.userfavoritiesprojectpresentations'
                    });
                }
            }

        }, 500);

        map.ui.setMapTitle(json.map_title || mapCategory + ': ' + mapUrlName);
        if (json.map_title) {
            webgis.globals.portal.projectTitle = json.map_title;
        }

        map.events.on('onshowappmenu', function (sender, target) {
            var $target = $(target).empty();
            var map = this;

            function showMapCollection() {
                webgis.ajax({
                    progress: 'Lade Karten Kategorien',
                    url: webgis.url.relative(relPathPrefix + '../proxy/toolmethod/webgis-tools-portal-publish/map-categories'),
                    data: { 'page-id': mapPortalPage },
                    dataType: 'json',
                    success: function (result) {
                        $target.empty();
                        $target.css('display', 'none');

                        if (!result || !result.categories)
                            return;

                        // Versteckte Categorien aussortiern
                        var resultCategories = [];
                        for (var c in result.categories) {
                            if (result.categories[c] && result.categories[c].indexOf("@@") !== 0) {
                                resultCategories.push(result.categories[c]);
                            }
                        }
                        result.categories = resultCategories;


                        $('body').webgis_modal({
                            title: webgis.l10n.get("more-maps"),
                            onload: function ($target) {
                                var $ul = $("<ul class='webgis-page-category-list'>")
                                    .css({
                                        display: 'inline-block',
                                        margin: '0px',
                                        padding: '5px 0px',
                                        width: '100%'
                                    })
                                    .data('map', map)
                                    .appendTo($target);

                                var $li = $("<li></li>").appendTo($ul);
                                var $content = $("<div id='category-content'></div>")
                                    .addClass('webgis-page-category-item')
                                    .data('categories', result.categories)
                                    .appendTo($li);

                                function refreshTiles($content) {
                                    $content.each(function (i, e) {
                                        var $container = $(e).css({ display: 'block', position: 'relative' });
                                        var tileWidth = parseInt($container.attr('data-tilewidth')),
                                            tileHeight = 140,
                                            containerWidth = $container.width(),
                                            gutter = 4;

                                        var maxTilesPerRow = Math.floor((containerWidth + gutter) / (tileWidth + gutter));
                                        var currentTileWidth = (containerWidth - (maxTilesPerRow + 1) * gutter) / maxTilesPerRow;

                                        var items = $container.children('.tile').css({ position: 'absolute', left: 0, top: 0 });
                                        var rows = items.length / maxTilesPerRow;
                                        rows = Math.floor(rows) != rows ? parseInt(rows) + 1 : parseInt(rows);

                                        var height = rows * tileHeight + (rows) * gutter
                                        $container.css('height', '0px').data('height', height);

                                        var x = 0, y = 0;
                                        items.each(function (j, item) {
                                            var $item = $(item);

                                            if (j > 0 && j % maxTilesPerRow === 0) {
                                                y += tileHeight + gutter;
                                                x = 0;
                                            }

                                            $item.css({
                                                width: currentTileWidth,
                                                left: x, top: y
                                            });

                                            x += currentTileWidth + gutter;
                                        });

                                        webgis.delayed(function () {
                                            $container.css('height', $container.data('height') + 'px');
                                        }, 1);
                                    });
                                }

                                function laodMaps(category, $content) {
                                    $content.addClass('webgis-tile-container').attr('data-tilewidth', 140);
                                    $.ajax({
                                        url: webgis.url.relative(relPathPrefix + '../proxy/toolmethod/webgis-tools-portal-publish/category-maps'),
                                        data: { page: mapPortalPage, category: category },
                                        dataType: 'json',
                                        success: function (result) {
                                            $content.slideUp(1, function () {
                                                $content.empty();

                                                for (var i in result.maps) {
                                                    var map = result.maps[i];
                                                    var $tile = $("<div class='webgis-page-map-item tile' data-category='" + (map.category || category) + "' data-map='" + map.name + "'></div>")
                                                        .css('position', 'absolute')
                                                        .appendTo($content)
                                                        .click(function () {
                                                            var catName = $(this).attr('data-category');
                                                            var mapName = $(this).attr('data-map');
                                                            var map = $(this).closest('.webgis-page-category-list').data('map');
                                                            var bbox = map.getExtent().join();

                                                            var mapLocationUrl = webgis.url.relative(relPathPrefix + 'map/' + webgis.url.encodeString(catName) + '/' + webgis.url.encodeString(mapName)) + "?bbox=" + bbox;
                                                            function changeMap() {
                                                                document.location = mapLocationUrl;
                                                            }

                                                            var countGraphicsElements = map.graphics.countElements();

                                                            if (countGraphicsElements > 0) {
                                                                webgis.confirm({
                                                                    title: "Karte wechseln...",
                                                                    message: "In der altuellen Karte befinden sich " + countGraphicsElements + " Map-Markup Element(e). Diese werden beim Wechseln einer Karte standardmäßig nicht übernommen.\n\nMöchten Sie das Map-Markup dennoch in die neue Karte übernehmen? Dazu müssen die Objekte lokal im Browser temporär zwischengespeichert werden.\n\nTipp: Um Map-Markup Objekte dauerhaft zu erhalten, kann es auch als Map-Markup-Projekt (JSON) herunter geladen und in einer anderen Karte hochgeladen werden.",
                                                                    iconUrl: webgis.css.imgResource('drawing-100.png'),
                                                                    okText: 'Map-Markup in neue Karte übernehmen',
                                                                    onOk: function () {
                                                                        var grGuid = webgis.guid(), pwGuid = webgis.guid();
                                                                        mapLocationUrl += '&_temp_graphics=' + grGuid + '&_temp_graphics_x=' + pwGuid;
                                                                        webgis.localStorage.set('_temp_graphics_' + grGuid, webgis.cryptoJS.simpleEcrypt(pwGuid, JSON.stringify(map.graphics.toGeoJson())));
                                                                        changeMap();
                                                                    },
                                                                    cancelText: 'Map-Markup verwerfen',
                                                                    onCancel: function() {
                                                                        changeMap();
                                                                    }
                                                                });
                                                            } else {
                                                                changeMap();
                                                            }
                                                        });

                                                    var $img = $("<div class='webgis-page-map-item-image' style=\"background:url(" + webgis.url.relative(relPathPrefix + "/mapimage?category=" + webgis.url.encodeString(map.category || category) + "&map=" + webgis.url.encodeString(map.name)) + ") no-repeat center center\"></div>").appendTo($tile);
                                                    var $title = $("<div class='webgis-page-map-item-text'></div>").appendTo($tile);
                                                    $("<div style='height:50px'>" + (map.displayname || map.name) + "</div>").appendTo($title);

                                                    if (map.hidden == true) {
                                                        $tile.addClass('hidden');
                                                    }
                                                }

                                                refreshTiles($content);
                                            });

                                        }
                                    });
                                }

                                function showCategories($content) {
                                    // Unused (old)
                                    //if ($tileContainer) {
                                    //    if ($tileContainer.height() > 0) {
                                    //        $tileContainer.css('height', '0px');
                                    //    } else {
                                    //        $tileContainer.css('height', $tileContainer.data('height') + 'px');
                                    //    }
                                    //}
                                };

                                function parseCategoryName(category) {
                                    if (category == "_Favoriten")
                                        return "♥ Favoriten";
                                    return category;
                                }

                                function showCategory(category, $content) {
                                    var $div = $content.empty();

                                    var $title = $("<table>").addClass('webgis-page-category-header').appendTo($div);
                                    var $row = $("<tr>").appendTo($title);
                                    var $category = $("<div>" + parseCategoryName(category) + "</div>").addClass('webgis-page-category-item-title').appendTo(
                                        $("<td>").addClass('webgis-page-category-item-selected').css({ width: '100%' }).appendTo($row))
                                        .click(function () {
                                            showCategories($content);
                                        });
                                    var $more = $("<div>...</div>").addClass('webgis-page-category-item-title more').appendTo(
                                        $("<td>").appendTo($row))
                                        .click(function () {
                                            showCategories($content);
                                        });

                                    var $maps = $("<div>").appendTo($div);
                                    laodMaps(category, $maps);

                                    var categories = $div.data('categories');
                                    for (var c in categories) {
                                        var cat = categories[c];
                                        if (cat === category)
                                            continue;

                                        var $catDiv = $("<div>")
                                            .addClass('webgis-page-category-header change')
                                            .data('category', cat)
                                            .appendTo($div)
                                            .click(function () {
                                                webgis.delayed(function (element) {
                                                    showCategory($(element).data('category'), $(element).closest('#category-content'));
                                                }, 350, this);

                                                webgis.delayed(function (element) {
                                                    if ($("#category-content").offset().top < $([document.documentElement, document.body]).scrollTop()) {
                                                        //console.log('autoscroll');
                                                        $([document.documentElement, document.body]).animate({
                                                            scrollTop: $("#category-content").offset().top
                                                        }, 100);
                                                    }
                                                    $(element)
                                                        .addClass('changing')
                                                        .css({
                                                            top: '-' + $(element).position().top + 'px'
                                                        });
                                                }, 1, this);

                                            });
                                        var $catTitle = $("<div>" + parseCategoryName(cat) + "</div>").addClass('webgis-page-category-item-title').appendTo($catDiv);
                                    }
                                }

                                if (result.categories.length > 0) {
                                    webgis.delayed(function () {
                                        showCategory(result.categories[0], $content);
                                    }, 500);
                                }
                            }
                        });
                    }
                });
            };

            var $ul = new $("<ul>").addClass('webgis-map-app-menu').appendTo($target);
            
            $("<li>"+webgis.l10n.get("mapinfo-and-copyright")+"...<div style='font-size:.9em; color:#777; margin-top: 6px' class='webgis-map-title'></div></li>")
                .addClass('copyright')
                .css("backgroundImage","url("+webgis.css.imgResource("copyright-26.png","tools")+")")
                .appendTo($ul)
                .click(function () {
                    $('#webgis-info').trigger('click');
                });

            if (mapPortalPageName) {
                $("<li>")
                    .html(webgis.emptyIfSuspiciousHtml(webgis.l10n.get("portal") + ": " + mapPortalPageName))
                    .addClass('portal')
                    .css("backgroundImage", "url(" + webgis.css.imgResource("portal-26.png", "tools") + ")")
                    .appendTo($ul)
                    .click(function () {
                        document.location = portalUrl + '/' + mapPortalPage;
                    });
            }

            $("<li>")
                .text(webgis.l10n.get("more-maps")+"...")
                .addClass('maps')
                .css("backgroundImage", "url(" + webgis.css.imgResource("maps-26.png", "tools") + ")")
                .appendTo($ul)
                .click(function () {
                    showMapCollection();
                });

            if (webgis.usability.allowMapContextMenu === true) {
                $("<li>")
                    .text(webgis.l10n.get("map-context-menu")+"...")
                    .css("backgroundImage", "url(" + webgis.css.imgResource("context_menu-26.png", "ui") + ")")
                    .appendTo($ul)
                    .click(function (e) {
                        $('body').webgis_map_contextmenu({
                            map: map,
                            clickX: e.originalEvent.clientX,
                            clickY: e.originalEvent.clientY,
                            force: true
                        });
                    });
            }

            $("<li>")
                .text(webgis.l10n.get('viewer-settings')+"...")
                .css("backgroundImage", "url(" + webgis.css.imgResource("admin-26.png", "ui") + ")")
                .appendTo($ul)
                .click(function (e) {
                    $('body').webgis_mapProperties({
                        map: map
                    });
                });

            $ul.children('li')
                .click(function () {
                    map.events.fire('onhideappmenu', $target);
                });

            map.ui.setMapTitle();  // Titel übernehmen
        }, map);

        map.events.on('onnewfeatureresponse', function (sender, features) {
            if (features && features.metadata) {
                if (webgis.usability.userPreferences.has("show-markers-on-new-queries")) {
                    features.metadata.showMarkers = webgis.usability.userPreferences.get("show-markers-on-new-queries") === 'yes';
                }
                else if (webgis.defaults["user.preferences.show-markers-on-new-queries"]) {
                    features.metadata.showMarkers = webgis.defaults["user.preferences.show-markers-on-new-queries"] === 'yes';
                }

                if (webgis.usability.userPreferences.has("select-new-query-results")) {
                    //features.metadata.selected = features.metadata.selected || webgis.usability.userPreferences.get("select-new-query-results") === 'yes';
                    features.metadata.selected ||= webgis.usability.userPreferences.get("select-new-query-results") === 'yes';
                }
                else if (webgis.defaults["user.preferences.select-new-query-results"]) {
                    //features.metadata.selected = features.metadata.selected || webgis.defaults["user.preferences.select-new-query-results"] === 'yes';
                    features.metadata.selected ||= webgis.defaults["user.preferences.select-new-query-results"] === 'yes';
                }
            };
            //console.trace('onnewfeatureresponse', features);
        });

        $('#webgis-info-pane-portal').html("<a href='" + webgis.url.relative(relPathPrefix + '../' + mapPortalPage) + "' >" + mapPortalPageName + "</a>");
        $('#webgis-info-pane-category').html(mapCategory);
        $('#webgis-info-pane-map').html(mapUrlName);
        $('#webgis-info-pane-username').html(webgis.hmac.userDisplayName() || 'Gast (anonym)');

        if (mapJson.userdata && mapJson.userdata.meta && mapJson.userdata.meta.author) {

            //$('#btnChangeImage').css('display', '');
            //$('.mapimage-preview').click(function () {
            //    $('#upload-image-form input:first').click();
            //});

            $('#webgis-info-pane-mapauthor').html(mapJson.userdata.meta.author);

            if (globals.isUserProject !== true &&
                mapJson.userdata.meta.author.toLowerCase() === webgis.hmac.userName().toLowerCase()) {

                globals.isMapAuthor = true;

                if ($('.mapimage-preview').length > 0) {
                    $('.mapimage-preview')
                        .css('cursor', 'pointer')
                        .get(0)
                        .setAttribute("onclick", "$('#upload-image-form input:first').click()");

                    $("<br/><br/><a class='uibutton' target='_blank' href='" + webgis.url.relative("../../mapbuilder?category=" + webgis.url.encodeString(mapCategory) + "&map=" + webgis.url.encodeString(mapUrlName)) + "'>MapBuilder...</a><br/><br/>")
                        .appendTo($('#webgis-info-pane').find('.tab-admin'));

                    $('#tab-admin')
                        .css('display', 'inline-block');
                }
            }

            map.setMapDescription(mapDescription);
        }

        $('#webgis-copyright').webgis_copyright({ map: map });

        if (onReady)
            onReady(map);
    }

    
});

webgis.mapExtensions = new function () {
    return {
        checkFavoriteStatus: function (callback, forceDialog) {
            if (webgis.hmac) {
                webgis.hmac.checkFavoritesStatus(portalUrl + '/favorites/contentmessage', mapCategory + '/' + mapUrlName,
                    callback,
                    function () {
                        $.get(portalUrl + '/favorites/join?join=true', function () {
                        });
                    },
                    function () {
                        $.get(portalUrl + '/favorites/join?join=false', function () {
                        });
                    }, forceDialog);
            }
        },

        deleteMap: function () {
            if (globals.isMapAuthor) {
                if (confirm("Karte unwiderruflich löschen: " + globals.mapCategory + "/" + globals.mapUrlName + "?")) {
                    $.ajax({
                        url: '../../DeleteMap',
                        data: { map: globals.mapUrlName, category: globals.mapCategory },
                        type: 'post',
                        success: function (result) {
                            if (result.success) {
                                document.location = webgis.url.relative(globals.relPathPrefix + '../' + globals.mapPortalPage);
                            }
                        }
                    });
                }
            }
        }
    };
};


