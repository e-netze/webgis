var _openMap = null;
var _timer = new webgis.timer(selectionChanged, 1000);
var _map = null, _scale = null, _center = null, _bounds = null, _layers=null, _initial = {};
var _mapBuilderTools = [];

webgis.mapBuilder = {
    html_meta_tags: null,
    mapDescription: ''
};

webgis.usability.allowUserRemoveErrors = false;  // allways show errors
webgis.usability.cooperativeGestureHandling =
    webgis.usability.appendMiniMap = false; // don't show with MapBuilder!!!

$(document).ready(function () {
    
    webgis.init(function () {

        if (!webgis.isTouchDevice()) {
            webgis.markerIcons["sketch_vertex"] = webgis.markerIcons["sketch_vertex_square"];
        }
        else {
            webgis.markerIcons["sketch_vertex"] = webgis.markerIcons["sketch_vertex_bottom"];
        }

        $('#username').html("Angemeldet: " + webgis.clientName());


        createSelectorFromAjax("Ausdehungen (Extents)", "extents", "/rest/extents?f=json", { singleSelect: true }, false, "extents");
        //createSelectorFromAjax("Dienste (Services)", "services", "/rest/services?f=json", false, true, "services");
        createSelector("Dienste (Services)", "services", false, [], true);

        createSelector("Benutzeroberfläche (UI)", "ui", false, [
            { id: "appmenu", name: "App Menü", container: "Topbar", image: "content/api/img/menu-26-b.png" },
            { id: "detailsearch", name: "Detailsuche", container: "Topbar", image: "content/api/img/binoculars-26-b.png" },
            {
                id: "search", name: "Schnellsuche", container: "Topbar", image: "content/api/img/search-26-b.png",
                options: {
                    singleselect: false,
                    items:searchServiceItems()
                }
            },
            {
                id: "tabs-presentation", name: "Darstellungsvarianten", container: "Tabs", image: "content/api/img/toolbar/presentations.png",
                options: {
                    singleselect:false,
                    items:[
                        { id: "tabs-presentation-addservices", name: "Dienst hinzufügen" }
                    ]
                }
            },
            //{ id: "tabs-presentation-addservices", name: "Darstellungsvarianten + Dienst hinzufügen", container: "Tabs", image: "content/api/img/toolbar/presentations.png" },
            { id: "tabs-queryresults", name: "Abfrageergebnisse", container: "Tabs", image: "content/api/img/toolbar/markers.png" },
            { id: "tabs-tools", name: "Werkzeugkasten", container: "Tabs", image: "content/api/img/toolbar/tools.png" },
            {
                id: "tabs-settings", name: "Dienste-Einstellungen" ,subname: "veraltet, Darstellungsvarianten verwenden!", container: "Tabs", image: "content/api/img/toolbar/settings.png",
                options: {
                    singleselect: false,
                    items: [
                        { id: "tabs-settings-addservice", name: "Dienst hinzufügen" },
                        { id: "tabs-settings-themes", name: "Layerschaltung" }
                    ]
                }
            }
            //{ id: "tabs-settings-addservice", name: "Dienste-Einstellungen + Dienst hinzufügen", container: "Tabs", image: "content/api/img/toolbar/settings.png" }
        ]);

        createSelectorFromAjax("Werkzeuge (Toolbox)", "tools", "/rest/tools?f=json", { singleSelect: false, selectMode: 'json2', containerName: 'name', collectionName: 'tools' }, false, "tools");
        
        createSelector("Standardwerkzeug", "defaulttool", { singleSelect: true, selectMode:'json' }, [
            { id: "none", name: "Kein" },
            {
                id: "webgis.tools.identify", name: "Deep Identify",
                options: {
                    singleselect: true,
                    items: [{ id: "all-identify-tools", name: "Alle Abfragewerkzeuge" }]
                }
            }
        ]);

        createSelector("Werkzeuge (Schnellzugriff)", "tools-quick-access", false, [
            { id: "webgis.tools.navigation.zoomIn", name: "Zoom In", image: "zoomin.png" },
            { id: "webgis.tools.navigation.fullExtent", name: "Gesamter Kartenausschnitt", image: "home.png" },
            { id: "webgis.tools.navigation.zoomBack", name: "Zurück", image: "back.png" },
            { id: "webgis.tools.navigation.currentPos", name: "Aktuelle Position", image: "currentpos.png" }
        ]);

        createSelector("Dynamischer Inhalt", "dynamic-content", false, [
           /* { id: "dynamiccontent", name: "Inhalt auswählen", image: "content/api/img/menu-26-b.png" , click: function () { openDynamicContentSelector(); } },*/
        ], true);

        $("<div style='display:inline-block;cursor:pointer;font-size:20px;font-weight:bold;position:absolute;right:2px;top:0px'>&nbsp;[+]&nbsp;</div>").appendTo($(".selector-container[data-id='services']").find('.webgis-presentation_toc-title').css('position', 'relative'))
            .click(function (e) {
                e.stopPropagation();
                openAddServiceSelector();
            });

        $("<div style='display:inline-block;cursor:pointer;font-size:20px;font-weight:bold;position:absolute;right:2px;top:0px'>&nbsp;[+]&nbsp;</div>").appendTo($(".selector-container[data-id='dynamic-content']").find('.webgis-presentation_toc-title').css('position', 'relative'))
            .click(function (e) {
                e.stopPropagation();
                openDynamicContentSelector();
            });

        createSelector("Aktionen", "actions", false, [
            { id: 'onstart_zoom2position', name: 'Zoom auf Standort (mobile Clients)', container: 'Beim Start' },
            { id: 'onstart_zoom2position_all_clients', name: 'Zoom auf Standort (alle Clients)', container: 'Beim Start' }
        ]);


        var defaults = null;
        if (_openMap != null) {
            $.ajax({
                url: webgis.url.relative('../proxy/toolmethod/webgis-tools-portal-publish/mapjson'),
                data: { page: _openMap.mapPortal, category: _openMap.mapCategory, map: _openMap.urlName, add_meta: true, add_description: true },
                type:'post',
                async: false,
                success: function (result) {

                    var mapJson = result.mapjson || result.serializationmapjson;
                    var graphics = result.graphics;
                    webgis.mapBuilder.html_meta_tags = result.html_meta_tags || null;

                    if (typeof mapJson === 'string')
                        mapJson = jQuery.parseJSON(mapJson);
                    if (typeof graphics === 'string')
                        graphics = jQuery.parseJSON(graphics);

                    webgis.mapBuilder.mapDescription = result.map_description || '';

                    ///////////////////////////////////////////////////////////////////////////////////////
                    // Compatibility (ToolIds) -> webmapping.tools.api -> webgis.tools.
                    //console.log(mapJson);
                    var userdata = mapJson.userdata;
                    if (userdata && userdata.mapbuilder) {
                        if (userdata.mapbuilder.defaulttool) {
                            for (var t in userdata.mapbuilder.defaulttool) {
                                userdata.mapbuilder.defaulttool[t].id = webgis.compatiblity.toolId(userdata.mapbuilder.defaulttool[t].id);
                            }
                            //console.log(userdata.mapbuilder.defaulttool);
                        }

                        _snapshots = userdata.mapbuilder.snapshots || { active: 'default' };
                    }
                    //////////////////////////////////////////////////////////////////////////////////////////

                    defaults = mapJson && mapJson.userdata && mapJson.userdata.mapbuilder ? mapJson.userdata.mapbuilder : null;
                    
                    if (mapJson) {
                        _initial.scale = mapJson.scale;
                        _initial.center = mapJson.center;
                        _initial.layers = [];
                        _initial.service_opacity = [];
                        _initial.initialDefaults = mapJson.userdata.mapbuilder.initialDefaults || {};

                        if (mapJson.services) {
                            webgis.ajax({
                                type: 'post',
                                url: webgis.baseUrl + "/rest/services",
                                data: webgis.hmac.appendHMACData({ f: 'json' }),
                                success: function (serviceInfos) {
                                    for (var s in mapJson.services) {
                                        if (mapJson.services[s] && mapJson.services[s].layers)
                                            _initial.layers[mapJson.services[s].id] = mapJson.services[s].layers;

                                        _initial.service_opacity[mapJson.services[s].id] = mapJson.services[s].opacity;

                                        appendService(mapJson.services[s], serviceInfos);
                                    }

                                    fireSelectionChanged();
                                }
                            });
                        }
                        
                        if (defaults.dyncontent && defaults.dyncontent.length > 0) {
                            for (var i in defaults.dyncontent)
                                appendDynamicContent(defaults.dyncontent[i]);
                        }
                    }
                    _initial.graphics = graphics;
                }
            });
        } else {
            defaults = webgis.ajaxSync(webgis.url.relative('mapbuilder/defaults'));
            defaults = (typeof defaults === 'string' ? eval("(" + defaults + ")") : defaults);
        }

        if (defaults) {
            var extent = null, services = null;

            if(!defaults.defaulttool) {
                defaults.defaulttool = [{ id: 'webgis.tools.identify', selected: true }];
            }

            for (var containerName in defaults) {
                if (containerName == 'map') {
                    if (defaults[containerName].options) {
                        extent = defaults[containerName].options.extent;
                        services = defaults[containerName].options.services;
                    }
                    continue;
                }

                if (containerName == 'queries') {
                    _initial.queries = defaults[containerName];
                }

                $(".selector-container[data-id='" + containerName + "']").find(".webgis-toolbox-tool-item-selected").trigger('click');
                for (var itemName in defaults[containerName]) {

                    if (defaults[containerName][itemName] == true) {
                        $(".selector-container[data-id='" + containerName + "']").find(".webgis-toolbox-tool-item[data-id='" + itemName + "']").trigger('click');
                        $(".selector-container[data-id='" + containerName + "']").find(".webgis-toolbox-tool-item-option[data-id='" + itemName + "']").trigger('click');
                    } else if (defaults[containerName][itemName] && defaults[containerName][itemName].id && defaults[containerName][itemName].selected == true) {
                        var $item = $(".selector-container[data-id='" + containerName + "']").find(".webgis-toolbox-tool-item[data-id='" + defaults[containerName][itemName].id + "']").trigger('click');
                        if (defaults[containerName][itemName].options) {
                            for (var o in defaults[containerName][itemName].options) {
                                var option = defaults[containerName][itemName].options[o];
                                $item.find(".webgis-toolbox-tool-item-option[data-id='" + option + "']").trigger('click');
                            }
                        }
                    }
                }
            }
            
            if (extent) {
                $(".selector-container[data-id='extents']").find(".webgis-toolbox-tool-item[data-id='" + extent + "']").trigger('click');
            }

            if (services) {
                webgis.ajax({
                    type: 'post',
                    url: webgis.baseUrl + "/rest/services",
                    data: webgis.hmac.appendHMACData({ f: 'json' }),
                    success: function (serviceInfos) {
                        for (var i in services.split(',')) {
                            appendService({ id: services.split(',')[i] }, serviceInfos);
                        }
                    }
                });
            }
        }

        checkAllEpsg($('#mapbuilder-container'));

        if ($('.webportal-layout-sidebar-items.top').length === 1) {
            webgis.toolInfos({}, 'mapbuilder', function (tools) {
                $.each(tools, function (i, tool) {
                    tool.uiElement = $('#mapbuilder-tools-content');
                    tool.onRelease = function (t) {
                        _map.setActiveTool(null);
                        $(t.uiElement).empty();
                        $('#webgis-container').find('#tab-tools-content').empty().webgis_toolbox({ map: _map });
                    };
                    _mapBuilderTools.push(tool);

                    var $sidebarItem = $("<li>")
                        .data('tool', tool)
                        .addClass('webportal-layout-sidebar-item')
                        .insertBefore($('.webportal-layout-sidebar-items.top').children('.webportal-layout-sidebar-item.hr'))
                        .click(function (e) {
                            webgis.tools.onButtonClick(_map, $(this).data('tool'));
                        });
                    $("<img>")
                        .attr('src', webgis.css.imgResource(tool.image, 'tools'))
                        .css('filter', 'invert(0)')
                        .appendTo($sidebarItem);
                    $("<a hef=''>" + tool.name + "</a>")
                        .appendTo($sidebarItem)
                        .click(function (e) {
                            e.stopPropagation();
                            $(this).parent().trigger('click');
                            return false;
                        });
                });

                var $sidebarItem = $("<li>")
                    .addClass('webportal-layout-sidebar-item')
                    .insertBefore($('.webportal-layout-sidebar-items.top').children('.webportal-layout-sidebar-item.hr'))
                    .click(function (e) {
                        $('body').webgis_mapbuilder_snapshotSelector({
                            snapshots: _snapshots.snapshots,
                            active: _snapshots.active,
                            show_properties: true,
                            title: 'Snapshot: aktiven Snapshot setzen',
                            description: 'Hier kann der Snapshot gesetzt werden, der beim öffnen der Karte verwendet werden sollte. Außerdem können einen Eigenschaften aus den Snapshots gelöscht werden.',
                            onselect: function (name) {
                                _snapshots.active = name;
                                resetToSnapshot();
                            }
                        });
                    });
                $("<img>")
                    .attr('src', webgis.css.imgResource('snapshot-26.png', 'tools'))
                    .css('filter', 'invert(0)')
                    .appendTo($sidebarItem);
                $("<a hef=''>Auf Snapshot zurücksetzen</a>")
                    .appendTo($sidebarItem)
                    .click(function (e) {
                        e.stopPropagation();
                        $(this).parent().trigger('click');
                        return false;
                    });
            });

            
        }
    });
})

function fireSelectionChanged() {
    _timer.Start();
}
function selectionChanged() {
    var extent = $('#extents').val();
    var services = orderedServices();
    var tools = $('#tools').val();
    var toolsQuickAccess = $('#tools-quick-access').val();
    var queries = $('#queries').length > 0 ? $('#queries').val().replace(/\"selected\":/g, '\"visible\":') : null;
    var defaultTool = tryEval($('#defaulttool').val());

    if (!queries && _initial && _initial.queries) {
        queries = _initial.queries;
    }

    var graphicsJson = '';
    if (extent != "" && services != "") {
        var serviceOpacity;

        if (_map != null) {
            _map.setActiveTool(null);  // Release open Mapbuilder MapTools
            graphicsJson = _map.graphics.toGeoJson();

            _scale = _map.scale();
            _center = _map.getCenter();
            _bounds = _map.getExtent();
            _layers = [];
            serviceOpacity = [];

            // remember current map properties
            for (var s in _map.services) {
                var service = _map.services[s];

                if (!service) continue;

                // remember the existing opacity settigs
                serviceOpacity[service.id] = service.opacity;

                if (!service.layers) continue;

                // remember the existing layer settings (visiblility)
                _layers[service.id] = [];
                for (var l in service.layers) {
                    var layer = service.layers[l];
                    _layers[service.id].push({ id: layer.id, name: layer.name, visible: layer.visible });
                }
            }
            _map.destroy();
        } else {
            _layers = null;
        }

        var mapLayers = _initial && _initial.layers ? _initial.layers : _layers;
        if (_snapshots && _snapshots.active) {
            var snapshot = getSnapShot(_snapshots.active);

            // Apply Bounds, center, scale
            if (snapshot.map) {
                if (snapshot.map.scale) {
                    _scale = snapshot.map.scale;
                    if (_initial && _initial.scale) { _initial.scale = _scale; }
                }
                if (snapshot.map.center && snapshot.map.center.length === 2) {
                    _center = snapshot.map.center;
                    if (_initial && _initial._center) { _initial.center = _center; }
                }
                if (snapshot.map.bounds && snapshot.map.bounds.length === 4) {
                    _bounds = snapshot.map.bounds;
                    if (_initial && _initial.bounds) { _initial.bounds = _bounds; }
                }
            }

            // Apply Layer Visibility
            if (snapshot.presentations && snapshot.presentations.visibility) {
                for (var v in snapshot.presentations.visibility) {
                    var visibility = snapshot.presentations.visibility[v];
                    var serviceLayers = mapLayers[visibility.service];

                    if (serviceLayers) {
                        for (var i in serviceLayers) {
                            serviceLayers[i].visible = $.inArray(serviceLayers[i].id, visibility.layers) >= 0;
                        }
                    }
                }
            }
        }

        var $container = $('#webgis-container');
        $container.empty();

        $("<div id='map' style='position:absolute;top:0px;left:0px;bottom:0px;right:0px'></div>").appendTo($container);
        $("<div style='z-index:10;position:absolute;right:0px;width:320px;bottom:0px;height:24px;background:#aaa'><div id='hourglass'></div></div>").appendTo($container);
        $("<div id='topbar' style='position:absolute;right:0px;top:0px;z-index:999;text-align:right;'>").appendTo($container);

        $('#map').empty();
        $('#hourglass').empty();
        $('#topbar').empty();
        $('#webgis-container').find('.webgis-tool-button-bar').remove();

        if (toolsQuickAccess) {
            $("<div class='webgis-tool-button-bar shadow' style='position:absolute;left:9px;top:109px' data-tools='" + toolsQuickAccess + "'></div>").appendTo('#webgis-container');
        }

        var dynContent = [], includedDynContent=getDynamicContent();
        for (var d in includedDynContent) {
            if (includedDynContent[d].selected == true)
                dynContent.push(includedDynContent[d]);
        }

        var queryDefinitions = queries ? eval(queries) : null;
        _map = webgis.createMap('map', {
            extent: extent,
            services: services,
            queries: queryDefinitions,
            layers: mapLayers,
            dynamiccontent: dynContent
        });

        if (defaultTool) {
            for (var i in defaultTool) {
                if (defaultTool[i].selected == true) {
                    _map.setDefaultToolId(defaultTool[i].id);
                    break;
                }
            }
        }

        serviceOpacity = _initial && _initial.service_opacity ? _initial.service_opacity : serviceOpacity;
        if (serviceOpacity) {
            for (var serviceId in serviceOpacity) {
                var service = _map.getService(serviceId);
                if (service) service.setOpacity(serviceOpacity[serviceId]);
            }
        }

        if (_scale && _center) {
            _map.setScale(_scale, _center);
        }
        else if (_bounds) {
            _map.zoomTo(_bounds);
        }

        _map.ui.createHourglass('hourglass');
        _map.events.on('onserialize', function (e, userdata) {
            userdata.mapbuilder = createDefaultsObject();
        }, _map);

        _map.events.on('onmapserialized', function (e, serializedMap) {
            if (serializedMap?.services) {
                // only if map is serialized with services (==saved/published)
                // sometimes eg. for createing an UI Master Template map are only
                // serialized temporary only with UI Elements
                // => serializedMap.services is undefined then
                const serializeMapServices = [];
                const serviceIds = services.split(',');

                // only add the services, that are in the ordered services list
                // In the map can be different services, that are not in the ordered services list
                // eg. Services that are items of a collection service,
                //     collection services are not serialized directly, but the items are serialized
                for (const serviceId of serviceIds) {
                    let serializedMapService = serializedMap.services.find(s => s.id === serviceId);
                    if (!serializedMapService) {  // maybe a collection services (not directly in the map)
                        serializedMapService = {
                            id: serviceId,
                            layers: [],
                            order: serializeMapServices.length === 0
                                ? 1
                                : (serializeMapServices[serializeMapServices.length - 1].order + 1)
                        };
                    }

                    serializeMapServices.push(serializedMapService);
                }

                serializedMap.services = serializeMapServices;
            }
        }, _map);

        if (_initial && _initial.center && _initial.scale) {
            _map.setScale(_initial.scale, _initial.center);
        }

        if (_initial && _initial.graphics)
            _map.graphics.fromGeoJson(_initial.graphics);

        var ui = $('#ui').val().split(',');
        if (intersectArray(['search', 'detailsearch', 'appmenu'], ui).length > 0) {
            var quick_search_service = '';
            for (var s in ui) {
                var serviceName = ui[s];
                if (serviceName.indexOf('search-service-') == 0) {
                    if (quick_search_service.length > 0) quick_search_service += ',';
                    quick_search_service += serviceName.substr(15, serviceName.length - 15);
                }
            }
            if (quick_search_service == '')
                quick_search_service = 'geojuhu';

            _map.ui.createTopbar('topbar',
                {
                    quick_search_service: quick_search_service,
                    quick_search: jQuery.inArray('search', ui) >= 0,
                    detail_search: jQuery.inArray('detailsearch', ui) >= 0,
                    app_menu: jQuery.inArray('appmenu', ui) >= 0
                });
        }
        if (intersectArray(['tabs-presentation', 'tabs-queryresults', 'tabs-tools', 'tabs-settings'], ui).length > 0) {
            //console.log(tryEval(tools));

            var headerButtons = [];
            headerButtons["presentations"] = [
                {
                    img: webgis.css.imgResource("snapshot-26.png", "tools"),
                    click: function (e) {
                        $('body').webgis_mapbuilder_snapshotSelector({
                            snapshots: _snapshots.snapshots,
                            active: _snapshots.active,
                            title: 'Snapshot: Sichtbarkeit & Container',
                            description: 'Die aktuelle Layer/Themen Sichtbar und die aktuelle aufgeklappten Darstellungsvarianten Container und Gruppen',
                            submitText: 'Speichern',
                            onselect: function (name) {
                                _snapshots.active = name;
                                createPresentationSnapshot();
                            }
                        });

                    }
                }
            ];
            var tabs = _map.ui.createTabs('#webgis-container', {
                left: null, right: 0, bottom: 24, top: null, width: 320,
                add_presentations: intersectArray(['tabs-presentation'], ui).length > 0,
                add_settings: intersectArray(['tabs-settings'], ui).length > 0,
                add_tools: intersectArray(['tabs-tools'], ui).length > 0,
                add_queryResults: intersectArray(['tabs-queryresults'], ui).length > 0,
                options_presentations: {
                    gdi_button: intersectArray(['tabs-presentation-addservices'], ui).length > 0
                },
                options_settings: {
                    gdi_button: intersectArray(['tabs-settings-addservice'], ui).length > 0,
                    themes: intersectArray(['tabs-settings-themes'], ui).length > 0
                },
                options_tools: {
                    containers: tools != null && tools.length > 0 ? tryEval(tools) : null
                },
                header_buttons: headerButtons
            });
        } else if ($.fn.webgis_errors) {
            // Simple Error Handling (if not tabs in the map)
            var $errorTab = $("<div><img src='" + webgis.css.imgResource('error-32.png', 'toolbar') + "' /><div class='webgis-tabs-tab-text webgis-error-counter'></div></div>")
                .attr('id', 'tab-errors')
                .addClass('webgis-tabs-tab')
                .css({ zIndex: 999999, position: 'absolute', right: 10, bottom: 10, width: 65, height: 40, borderRadius: 5, display: 'none' })
                .appendTo('#webgis-container')
                .click(function () {
                    $('#webgis-mapbuilder-errors').css({ display: $('#webgis-mapbuilder-errors').css('display') === 'none' ? 'block' : 'none' });
                });
            $("<div>")
                .attr('id', 'webgis-mapbuilder-errors')
                .css({ zIndex: 999999, position: 'absolute', right: 10, bottom: 55, width: 320, height: 240, borderRadius: 5, display: 'none', background: 'white', padding:10, boxSizing:'border-box', overflow: 'auto' })
                .appendTo('#webgis-container')
                .click(function () {
                    $(this).css('display','none')
                })
                .webgis_errors({
                    map: _map,
                    tab_element_selector: $errorTab,
                    allow_remove: webgis.usability.allowUserRemoveErrors
                });
        }

        var mapQueries = [];
        for (var s = 0; s < _map.serviceIds().length;s++) {
            var service = _map.services[_map.serviceIds()[s]];
            for (var q in service.queries) {
                var query = service.queries[q];

                var image = '';
                if (query.items && query.items.length > 0) {
                    image = 'content/api/img/binoculars-info-26-b.png';
                } else {
                    image = 'rest/toolresource/webgis-tools-identify-identify';
                }

                mapQueries.push({ id: query.id, name: query.name, container: service.name, selected: query.visible, service: service.id, query: query.id, image: image });
            }
        }
        if (mapQueries.length > 0) {
            createSelector("Abfragen", "queries", { singleSelect: false, selectMode: 'json', selectModeFields: ["service", "query"] }, mapQueries);
        }

        if (_map != null) {

            var $initialContainer = createCustomSelector("Defaults (beim Kartenstart aktiv)", "initial-defaults");

            // Initial Tool

            var $initialToolSelect = $("<select>").addClass('value');
            $("<option value=''></option>").text("-- Werkzeug (optional) --").appendTo($initialToolSelect);
            var mapTools = [], toolElements = tryEval('(' + $('#tools').val() + ')');
            for (var c in toolElements) {
                if (toolElements[c].tools && toolElements[c].tools.length) {
                    var $optGroup = $("<optgroup>").attr('label', toolElements[c].name).appendTo($initialToolSelect);
                    if (toolElements[c].tools) {
                        for (var i in toolElements[c].tools) {
                            var toolName = webgis.tools.getToolName(toolElements[c].tools[i]);
                            if (webgis.tools._tools) {
                                var tool = $.grep(webgis.tools._tools, function (el) { return el.id == toolName });
                                if (tool.length > 0) {
                                    if (tool[0].type !== 'servertool' && tool[0].type !== 'clienttool' && tool[0].type !== 'serverbutton')
                                        continue;
                                    toolName = tool[0].name;
                                }
                            }
                            $("<option>")
                                .attr('value', toolElements[c].tools[i])
                                .text(toolName)
                                .appendTo($optGroup);
                        }
                    }
                    if ($optGroup.children().length == 0)
                        $optGroup.remove();
                }
            };

            var $initialTooltem = createCustomSelectorItem($initialContainer, 'initial-tool');
            $initialToolSelect
                .appendTo($initialTooltem.empty());

            if (_map.hasServices) {
                var serviceIds = _map.serviceIds();

                // Initial Query
                //var queries = $('#queries').length > 0 && $('#queries').val() ? JSON.parse($('#queries').val().replace(/\"selected\":/g, '\"visible\":')) : null;

                var $initialQuerySelect = $("<select>").addClass('value');
                $("<option value=''></option>").text("-- Abfragethema (optional) --").appendTo($initialQuerySelect);
                for (var s in serviceIds) {
                    var service = _map.getService(serviceIds[s]);
                    if (!service || !service.queries || service.queries.length === 0)
                        continue;

                    var $optGroup = $("<optgroup>").attr('label', service.name).appendTo($initialQuerySelect);

                    for (var q in service.queries) {
                        var query = service.queries[q];
                        $("<option>")
                            .attr('value', query.id)
                            .text(query.name)
                            .appendTo($optGroup);
                    }
                }

                var $initialQueryItem = createCustomSelectorItem($initialContainer, 'initial-query');
                $initialQuerySelect.appendTo($initialQueryItem.empty());

                // Initial Edittheme
                var $initialEditthemeSelect = $("<select>").addClass('value');
                $("<option value=''></option>").text("-- Editthema (optional) --").appendTo($initialEditthemeSelect);
                for (var s in serviceIds) {
                    var service = _map.getService(serviceIds[s]);
                    if (!service || !service.editthemes || service.editthemes.length == 0)
                        continue;

                    var $optGroup = $("<optgroup>").attr('label', service.name).appendTo($initialEditthemeSelect);

                    for (var e in service.editthemes) {
                        var edittheme = service.editthemes[e];
                        $("<option>")
                            .attr('value', edittheme.themeid)
                            .text(edittheme.name)
                            .appendTo($optGroup);
                    }
                }

                var $initialEditthemeItem = createCustomSelectorItem($initialContainer, 'initial-edittheme');
                $initialEditthemeSelect.appendTo($initialEditthemeItem.empty());
            }

            $initialContainer.find('.value')
                .each(function (i, e) {
                    var $e = $(e), $item = $e.closest('[data-id]');
                    if (_initial && _initial.initialDefaults) {
                        $e.val(_initial.initialDefaults[$item.attr('data-id')]);
                    }

                    // Why? _initial === null when called: see end of this function (selectionChanged)
                    //$e.change(function () {
                    //    var $item = $(this).closest('[data-id]');
                    //    _initial.initialDefaults[$item.attr('data-id')] = $(this).val();
                    //});
                });

            $("<div>")
                .css({
                    position: 'absolute',
                    left: '2px', bottom: '2px',
                    width: '36px', height: '36px',
                    backgroundImage: 'url(' + webgis.css.imgResource('snapshot-26.png', 'tools'),
                    backgroundRepeat: 'no-repeat',
                    backgroundPosition: 'center',
                    backgroundColor: '#fff',
                    border: '1px solid #ccc',
                    borderRadius: '3px',
                    cursor: 'pointer',
                    zIndex: 9998
                })
                .appendTo($(_map.elem))
                .click(function (e) {
                    e.stopPropagation();
                    $('body').webgis_mapbuilder_snapshotSelector({
                        snapshots: _snapshots.snapshots,
                        active: _snapshots.active,
                        title: 'Snapshot: Kartenausdehnung & Maßstab',
                        description: 'Die aktuelle Kartenausdehnung und der aktuelle Kartenmaßstab',
                        submitText: 'Speichern',
                        onselect: function (name) {
                            _snapshots.active = name;
                            createBoundsSnapshot();
                        }
                    });
                });
        }

        $.each(_mapBuilderTools, function (i, tool) {
            _map.addTool(tool);
        });

        _map.events.on('onchangeactivetool', function () {
            var activeTool = this.getActiveTool();
            if (activeTool && activeTool.client_name === 'mapbuilder') {
                $('#webgis-container').find('#tab-tools-content').empty().webgis_toolbox({ map: _map });
            } else {
                $('#mapbuilder-tools-content').empty();
            }
        }, _map);
        _map.events.on('beforechangeactivetool', function (channel, sender, newTool) {
            var activeTool = this.getActiveTool();
            if (activeTool && activeTool.id === 'webgis.tools.mapmarkup.mapmarkupmapbuilder') {
                if (newTool && newTool.id === activeTool.id) {
                    // Do Nothing -> same tool
                } else {
                    this.graphics.assumeCurrentElement(true);
                }
            }
        }, _map);

        if (graphicsJson && graphicsJson.features && graphicsJson.features.length > 0) {
            webgis.delayed(function () {
                _map.graphics.fromGeoJson({
                    geojson: graphicsJson,
                    replaceelements: true,
                    suppressZoom: true
                });
            }, 500);  
        }

        webgis.delayed(function () {
            if (_snapshots && _snapshots.active) {
                var snapshot = getSnapShot(_snapshots.active);
                if (snapshot.presentations) {
                    var $toc = $('#tab-presentations-content');
                    if (snapshot.presentations.expanded && $.fn.webgis_presentationToc) {
                        $toc.webgis_presentationToc('expand', { names: snapshot.presentations.expanded, exclusive: true });
                    }
                }
            }
        }, 500);
    }

    _initial = null;
};

function createDefaultsObject() {
    var extent = $('#extents').val();
    var services = orderedServices();
    var ui = {}, uiElements = $('#ui').val().split(',');
    for (var i in uiElements) {
        if (uiElements[i] == '') continue;
        ui[uiElements[i]] = true;
    };
    var queries = $('#queries').length > 0 ? tryEval('(' + $('#queries').val().replace(/\"selected\":/g, '\"visible\":') + ')') : null;
    var tools = {}, toolElements = tryEval('(' + $('#tools').val() + ')');
    
    for (var c in toolElements) {
        if (toolElements[c].tools) {
            for (var i in toolElements[c].tools) {
                var toolName = webgis.tools.getToolName(toolElements[c].tools[i]);
                if (!toolName) continue;
                tools[toolName] = true;
            }
        } else if (toolElements[c].options) {
            for (var i in toolElements[c].options) {
                tools[toolElements[c].options[i]] = true;
            }
        }
    };
    var toolsQuickAccess = {}; toolsQuickAccessElements = $('#tools-quick-access').val().split(',');
    for (var i in toolsQuickAccessElements) {
        if (toolsQuickAccessElements[i] == '') continue;
        toolsQuickAccess[toolsQuickAccessElements[i]] = true;
    };
    var actions = {}; actionElements=$('#actions').val().split(',');
    for (var i in actionElements) {
        if (actionElements[i] == '') continue;
        actions[actionElements[i]] = true;
    }

    var initialDefaults = {};
    $(".selector-container[data-id='initial-defaults']").find('li[data-id]')
        .each(function (i, e) {
            initialDefaults[$(e).attr('data-id')] = $(e).find('.value').val();
        });

    var defaults = {};
    defaults.map = {};
    defaults.map.options = {};
    defaults.map.options.extent = extent;
    defaults.map.options.services = services;
    defaults.ui = ui;
    defaults.tools = tools;
    defaults.defaulttool = tryEval($('#defaulttool').val());
    defaults.actions = actions;
    defaults["tools-quick-access"] = toolsQuickAccess;
    defaults.queries = queries;
    defaults.dyncontent = getDynamicContent();
    defaults.initialDefaults = initialDefaults;
    defaults.snapshots = _snapshots;

    return defaults;
}

function searchServiceItems() {
    result = webgis.ajaxSync(webgis.baseUrl + '/rest/search');

    var items=[]
    if (result && result.folders) {
        for (var i in result.folders) {
            var searchService = result.folders[i];

            items.push({ id: 'search-service-' + searchService.id, name: searchService.name });
        }
    }
    return items;
}

function tryEval(obj) {
    try {
        if (obj == "()")
            return null;
        return eval(obj);
    } catch (e) {
        console.log(obj);
        console.log('error:');
        console.log(e);
        return null;
    }
}

function orderedServices() {
    return $('#services').val().split(',').reverse().toString();
}

function createTemplate(template) {
    var extent = $('#extents').val();
    var services = orderedServices();
    var ui = $('#ui').val();
    var queries = $('#queries').length > 0 ? $('#queries').val().replace(/\"selected\":/g, '\"visible\":') : null;
    var tools = $('#tools').val();
    var toolsQuickAccess = $('#tools-quick-access').val();
    var geoJson = _map.graphics.getElements().length > 0 ?
        JSON.stringify(_map.graphics.toGeoJson()) :
        '';

    var dynContent = [], includedDynContent = getDynamicContent();
    for (var d in includedDynContent) {
        if (includedDynContent[d].selected === true)
            dynContent.push(_map._serializeDynamicContent(includedDynContent[d]));
    }
    var dynContentString = JSON.stringify(dynContent);

    _scale = _map.scale();
    _center = _map.getCenter();
    _bounds = _map.getExtent();

    var visibility = [], opacity = [];
    for (var s in _map.services) {
        var service = _map.services[s];
        if (!service)
            continue;

        var layerIds = service.getLayerVisibility();
        if (layerIds.length === 0 && service.isBasemap === true)
            continue;

        visibility.push({
            id: service.id,
            layers: layerIds,
            isbasemap: service.isBasemap === true /*&& service.basemapType === 'normal'*/,
            isoverlaybasemap: service.isBasemap === true && service.basemapType === 'overlay',
            opacity: service.opacity
        });
    };

    $.ajax({
        url: webgis.url.relative("mapbuilder/createtemplate?template=" + template),
        data: {
            clientid: webgis.clientid,
            extent: extent,
            services: services,
            ui: ui,
            queries: queries,
            tools: tools,
            scale: _scale,
            centerLng: _center[0],
            centerLat: _center[1],
            visibility: JSON.stringify(visibility),
            tools_quick_access: toolsQuickAccess,
            dynamiccontent: dynContentString,
            graphics: geoJson
        },
        type: 'post',
        success: function (result) {
            var editor = null;
            $('body').webgis_modal({
                title: 'HTML',
                onload: function ($content) {
                    //$("<textarea style='width:98%;height:95%'>" + result.html + "</textarea>").appendTo($content);
                    $('<div id="editor" style="position:absolute;left:0px;top:0px;right:0px;bottom:0px;"></div>').appendTo($content);

                    webgis.require('monaco-editor', function (result) {
                        editor = monaco.editor.create(document.getElementById('editor'), {
                            language: 'html',
                            automaticLayout: true,
                            readOnly: true,
                            contextmenu: false,
                            theme: 'vs'
                        });

                        editor.setValue(result.html);
                    }, result);
                }
            });
        }
    });
};

function publishMap(portalId, mapCategory, mapName) {
    if (!_map) {
        return AlertCreateMap();
    }

    if ($.fn.webgis_errors && $(null).webgis_errors('hasErrors') === true) {
        webgis.alert("Die aktuelle Karte enthält Fehler (siehe UI Tab Errors) und kann nicht veröffentlicht werden", "error");
        return;
    }

    var $errors = $(".webgis-toolbox-tool-item-group-details .webgis-toolbox-tool-item.webgis-toolbox-tool-item-selected.error");
    if ($errors.length > 0) {
        webgis.alert("Die aktuelle Konfiguration enthält Fehler und kann nicht veröffentlichet werden: " + $errors.find('.item-title').text(), "error");
        return;
    }

    customParameters=[];
    customParameters['page-id'] = portalId;
    customParameters['map-category'] = mapCategory || '';
    customParameters['map-name'] = mapName || '';
    webgis.tools.onButtonClick(_map, {
        id: 'webgis.tools.portal.publish',
        type: 'serverbutton',
        name: 'Publish',
        map: _map,
        uiElement: document.body,
        customParameters: customParameters
    });
}

function publishDefaults(portalId) {
    if (!_map) {
        return AlertCreateMap();
    }

    webgis.confirm({
        title: "Als Standard definieren",
        message: "Die aktuellen Einstellungen (Dienste, Ausdehnung, Werkzeuge, ...) werden als Standard definiert.\n\nImmer wenn der MapBuilder für diese Portalseite aufgerufen wird, um eine neue Karte zu erstellen, werden in Zukunft die aktuellen Einstellungen angezeigt.\n\nDadurch müssen Grundeinstellungen (Ausdehnung, Basisdienste), die in jeder Karte identisch sind nicht immer neu eingestellt werden.",
        cancelText: 'Nein, danke',
        okText: 'Einstellungen speichern',
        onOk: function () {
            var json = JSON.stringify(createDefaultsObject(), null, ' ');

            $.ajax({
                url: webgis.url.relative("mapbuilder/setdefaults"),
                data: { id: portalId, defaults: json },
                type: 'post',
                success: function (result) {
                    if (result.success) {
                        webgis.alert('Die aktuellen Einstellungen wurden als Standard übernommen', 'info');
                    } else {
                        webgis.alert(result.excption);
                    }
                }
            });
        }
    });

    
}

function manageUIMaster(portalId, mapCategory) {
    //try {
    //    customParameters = [];
    //    customParameters['page-id'] = portalId;
    //    customParameters['map-category'] = mapCategory || '';
    //    webgis.tools.onButtonClick(_map, { id: 'webgis.tools.portal.master', type: 'serverbutton', name: 'Master', map: _map, uiElement: document.body, customParameters: customParameters });
    //} catch (e) {
    //    console.log(e);
    //}

    if (!_map) {
        return AlertCreateMap();
    }

    $.ajax({
        url: webgis.url.relative("mapbuilder/master"),
        data: {
            id: portalId,
            category: mapCategory
        },
        type: 'post',
        success: function (result) {
            var editor = null;
            $("body").webgis_modal({
                title: "Master (" + mapCategory + ")",
                onload: function ($content) {

                    // Top
                    var $top = $('<div style="position:absolute;left:0px;top:0px;right:0px;height:50px;padding:5px;background:#efefef"></div>').appendTo($content);
                    var previousMasterType;
                    var $masterTypeCombo = $("<select><option value='0'>Master für alle Kategorien</option><option value='1'>Master für Kategorie: " + mapCategory + "</option><select>")
                        .addClass('webgis-input')
                        .css('max-width','250px')
                        .appendTo($top)
                        .val('1')
                        .on('focus', function () {
                            previousMasterType = $(this).val();
                            console.log('previousMasterType', previousMasterType);
                        })
                        .change(function () {
                            if (editor) {
                                result["master" + previousMasterType] = editor.getValue();
                                //console.log('set master' + previousMasterType, editor.getValue());
                                previousMasterType = $(this).val();
                                editor.setValue(result["master" + $(this).val()]);
                            }
                        });
                    
                    $("<button>Aus aktuellen Einstellungen übernehmen »</button>")
                        .addClass('uibutton-cancel uibutton')
                        .prependTo($top)
                        .click(function () {
                            var masterJson = _map.serialize({
                                ui: true,
                                asMaster: true
                            });

                            // delete unnessssary elements from masterJson
                            if (masterJson?.ui?.options) {
                                const tabElement = masterJson.ui.options.find(e => e.element === "tabs");
                                if (tabElement && tabElement.options && tabElement.options.header_buttons) {
                                    delete tabElement.options.header_buttons;
                                };
                            }

                            editor.setValue(JSON.stringify({
                                ui: masterJson.ui
                            }, null, 2));
                        });
                    

                    $("<button>Übernehmen</button>")
                        .addClass('uibutton')
                        .appendTo($top)
                        .click(function () {
                            var val = editor.getValue();

                            if (val) {
                                try {
                                    jQuery.parseJSON(val);
                                } catch (e) {
                                    console.log(e);
                                    alert('Parsing Error:\n' + e.message);
                                    return;
                                }
                            }

                            $.ajax({
                                url: webgis.url.relative("mapbuilder/setmaster"),
                                type: 'post',
                                data: {
                                    id: portalId,
                                    category: $masterTypeCombo.val() === '0' ? '' : mapCategory,
                                    master: val
                                },
                                success: function (result) {
                                    if (result.success) {
                                        alert("UI Master wurde erfolgreich übernommen");
                                    } else {
                                        alert("Error: " + result.message);
                                    }
                                }
                            });
                        });

                    $("<button>?</button>")
                        .addClass('uibutton-cancel uibutton')
                        .appendTo($top)
                        .click(function () {
                            window.open('https://docs.webgiscloud.com/de/webgis/apps/map_builder/uimaster/index.html');
                        });

                    // Editor
                    $('<div id="editor" style="position:absolute;left:0px;top:50px;right:0px;bottom:0px;"></div>').appendTo($content);
                    webgis.require('monaco-editor', function (result) {
                        editor = monaco.editor.create(document.getElementById('editor'), {
                            language: 'javascript',
                            automaticLayout: true,
                            theme: 'vs'
                        });

                        editor.setValue(result.master1);
                    }, result);
                }
            });
        }
    });
};

function emptyUIMaster() {
    webgis.confirm({
        title: "Leeren UI Master erzeugen",
        message: "Dieses Werkzeug entfernt alle UI Element (Tabs, Schnellsuche) aus der Karte. Das kann hilfreich sein, wenn eine neue UI Master Vorlage angelegt werden soll. Nach dem Entferen der UI Elemente, können die gewünschten UI Elemente und Werkzeuge wieder angeklickt werden und daraus eine UI Master Vorlage erstellt werden. Das Löschen betrifft nicht die gespeicherte Karte. Die Karte sollte vorher gespeichert werden!",
        cancelText: 'Lieber nicht!',
        okText: "UI Elemente entfernen",
        onOk: function () { 
            $(".selector-container[data-id='ui']")
                .find('.webgis-toolbox-tool-item-option.webgis-toolbox-tool-item-selected')
                .trigger('click');
            $(".selector-container[data-id='ui']")
                .find('.webgis-toolbox-tool-item.webgis-toolbox-tool-item-selected')
                .trigger('click');

            $(".selector-container[data-id='tools']")
                .find('.webgis-toolbox-tool-item-option.webgis-toolbox-tool-item-selected')
                .trigger('click');
            $(".selector-container[data-id='tools']")
                .find('.webgis-toolbox-tool-item.webgis-toolbox-tool-item-selected')
                .trigger('click');
        }
    });
};

function editMapDescription() {
    let editor = null, dialogId = 'edit-map-desciption-dialog';

    $("body").webgis_modal({
        title: "Map Description",
        id: dialogId,
        onload: function ($content) {

            // Top
            var $top = $('<div style="position:absolute;left:0px;top:0px;right:0px;height:50px;padding:5px;background:#efefef"></div>').appendTo($content);

            $("<button>Beschreibung der Dineste hinzufügen/updaten »</button>")
                .addClass('uibutton-cancel uibutton')
                .prependTo($top)
                .click(function () {
                    var description = editor.getValue();

                    var serviceids = _map.serviceIds();
                    for (var s in serviceids) {
                        description = description.removeSection(serviceids[s]);
                        var service = _map.getService(serviceids[s]);

                        if (service.servicedescription) {
                            description += "\n\n@section: " + service.id + "\n";
                            description += "*" + service.name + ":*\n\n" + service.servicedescription;
                            description += "\n@endsection";
                        }
                    }

                    console.log(description);
                    editor.setValue(description);
                });

            if (mapUrlName && mapCategory) {
                $("<button>Direkt Veröffentlichen</button>")
                    .addClass('uibutton')
                    .appendTo($top)
                    .click(function () {
                        webgis.confirm({
                            title: "Beschreibung veröffentlichen",
                            message: "Die Beschreibung kann direkt für die Karte '" + mapUrlName + "' aus der Kategorie '" + mapCategory + "' veröffentlicht werden",
                            onOk: function () {
                                var description = editor.getValue();
                                $.ajax({
                                    url: 'UpdateMapDescription',
                                    data: { map: mapUrlName, category: mapCategory, description: description },
                                    type: 'post',
                                    success: function (result) {
                                        if (result.success) {
                                            webgis.mapBuilder.mapDescription = description;
                                            webgis.alert('Beschreibung wurde erfolgreich veröffentlicht', 'info');
                                        } else {
                                            webgis.alert('Beim Veröffentlichen der Beschreibung sind Fehler aufgetreten', "error");
                                        }
                                    }
                                });
                            }
                        });
                    });
            }

            if (mapUrlName && mapCategory) {
                $("<button>Übernehmen</button>")
                    .addClass('uibutton')
                    .appendTo($top)
                    .click(function () {
                        webgis.mapBuilder.mapDescription = editor.getValue();
                        $("body").webgis_modal('close', { id: dialogId });
                    });
            }

            $("<button>?</button>")
                .addClass('uibutton-cancel uibutton')
                .appendTo($top)
                .click(function () {
                    window.open('https://docs.webgiscloud.com/de/webgis/apps/map_builder/mapdescription/index.html');
                });

            // Editor
            $('<div id="editor" style="position:absolute;left:0px;top:50px;right:0px;bottom:0px;"></div>').appendTo($content);
            webgis.require('monaco-editor', function (result) {
                editor = monaco.editor.create(document.getElementById('editor'), {
                    language: 'text',
                    automaticLayout: true,
                    theme: 'vs'
                });

                editor.setValue(webgis.mapBuilder.mapDescription);
            }, result);
        }
    });
};

function editHtmlMetaTags() {
    var editor = null;

    $("body").webgis_modal({
        id: 'edit-html-meta-tags-dialog',
        title: "HTML Meta-Tags",
        onload: function ($content) {

            // Top
            var $top = $('<div style="position:absolute;left:0px;top:0px;right:0px;height:50px;padding:5px;background:#efefef"></div>').appendTo($content);

            $("<button>Übernehmen</button>")
                .addClass('uibutton')
                .appendTo($top)
                .click(function () {
                    webgis.mapBuilder.html_meta_tags = editor.getValue();
                    $('body').webgis_modal('close', { id: 'edit-html-meta-tags-dialog' });
                });

            // Editor
            $('<div id="editor" style="position:absolute;left:0px;top:50px;right:0px;bottom:0px;"></div>').appendTo($content);
            webgis.require('monaco-editor', function (result) {
                editor = monaco.editor.create(document.getElementById('editor'), {
                    language: 'html',
                    automaticLayout: true,
                    theme: 'vs'
                });

                editor.setValue(webgis.mapBuilder.html_meta_tags);
            }, result);
        }
    });
};

function AlertCreateMap() {
    webgis.alert("Bitte erstellen sie zuerst ein Karte (Ausdehung und mindestens ein Dienst), um diese Funktion aufrufen zu können!", "info");
};

function intersectArray(array1, array2) {
    var ret=[];
    for (var i in array1) {
        var v1 = array1[i];
        for (var j in array2) {
            var v2 = array2[j];
            if (v1 == v2)
                ret.push(v1);
        }
    }
    return ret;
}

function showHelp(element) {
    var container = $(element).closest('.selector-container');

    var titleSpan = $(element).find('span').clone();
    titleSpan.find('img').css({ maxWidth: 16, maxHeight: 16 });
    $('body').webgis_modal({
        title: container.find('.webgis-presentation_toc-title').html() + ": " + titleSpan.html(),
        onload: function ($content) {
            loadHelpContent($(element), $content);
        }
    });
}

function loadHelpContent($element, $content) {
    var containerId = $element.closest('.selector-container').attr('data-id');

    var html=''
    if (containerId == 'extents') {
        var result = webgis.ajaxSync(webgis.baseUrl + '/rest/extents/' + $element.attr('data-id'));
        html = createHelpHtml(result, [
            {
                name: "Allgemein",
                fields: [
                    { id: "id", name: "Name" },
                    { id: "epsg", name: "EPSG-Code" }
                ]
            },
            {
                name: "Ausdehnung",
                fields: [
                    { id: 'extent', name: "Ausdehnung" }
                ]
            },
            {
                name: "Tiling/Auflösung",
                fields: [
                    { id: 'resolutions', name: "Auflösungen" }
                ]
            }
        ]);
    }
    else if (containerId == 'services') {
        var result = webgis.ajaxSync(webgis.baseUrl + '/rest/services/' + $element.attr('data-id'));
        html = createHelpHtml(result.services[0], [
            {
                name: "Allgemein",
                fields: [
                    { id: 'name', name: 'Name', }
                ]
            },
            {
                name: 'Layer',
                fields: [
                    {
                        id: 'layers', name: 'Layer', template: function (e) {
                            return e.name;
                        }
                    }]
            }
        ])
    }

    if (html == '') {
        html = 'Für dieses Thema ist keine Infomation vorhanden';
    }

    $content.html(html);

    $content.find('#help-wizard').webgis_wizard();
}

function createHelpHtml(json, options) {
    var html = "<div id='help-wizard' style='position:absolute;left:0px;top:0px;right:0px;bottom:0px'>";

    for (var s in options) {
        var step = options[s];

        html += "<div class='webgis-wizard-step' data-wizard-step-title='" + step.name + "'>";

        for (var f in step.fields) {
            var field = step.fields[f];

            html += "<span>" + field.name + "<span><br/>";
            var val=json[field.id];
            if(jQuery.isArray(val)) {
                html += "<div style='font-size:1.2em;background-color:#c8c8c8;border:1px solid #e2e2e2;padding:10px'>";
                for (var i in val) {
                    html += (field.template ? field.template(val[i]) : val[i]) + "<br/>";
                }
                html+="</div>";
            } else {
                html += "<input type='text' readonly='readonly' value='" + (field.template ? field.template(val) : val) + "' /><br/>";
            }
        }

        html += "</div>";
    }

    html += "</div>";

    return html;
}

function appendService(service, serviceInfos, append) {

    if ($.inArray(service.id, orderedServices().split(',')) >= 0)  // kein doppeltes Einfügen
        return false;

    $ul = $(".selector-container[data-id='services']").find('.webgis-toolbox-tool-item-group-details');

    var serviceInfo = $.grep(serviceInfos.services, function (n, i) { return n.id === service.id });
    serviceInfo = serviceInfo && serviceInfo.length > 0 ? serviceInfo[0] : null;

    var title = serviceInfo ? serviceInfo.name : service.id + ': Unknown Service';
    if (!checkEpsg({ _element: serviceInfo })) {
        alert('Service ' + title + ' do not support EPSG: ' + _selectedEpsg);
        return false;
    }

    var img = serviceInfo ? webgis.baseUrl + '/content/api/img/' + serviceInfo.type + "-service.png" : webgis.css.imgResource('error-32.png', 'toolbar');

    var $li = $("<li class='webgis-toolbox-tool-item' style='position:relative' data-id='" + service.id + "' data-containers='*'><span class='item-title'><img src='" + img + "'>&nbsp;" + title + "</span></li>")
    if (!serviceInfo) {
        $li.addClass('error');
    }

    if (append === true) {
        $li.appendTo($ul);
    } else {
        $li.prependTo($ul);
    }
    $li.click(function () {
        $(this).toggleClass('webgis-toolbox-tool-item-selected');

        var $parent = $(this).closest('.selector-container').parent();
        calcContainerValues($parent, this);

        fireSelectionChanged();
    });

    $("<div>✖</div>")
        .addClass('delete-service')
        .css({
            position: 'absolute', right: 0, top: 0, 'font-weight': 'bold', "text-align": 'center',
            width: 26, height: 26
        }).appendTo($li).click(function (event) {
            event.stopPropagation();
            if ($(this).hasClass('suppress-confirm') || confirm("Service aus Karte entfernen?")) {
                var $item = $(this).closest('.webgis-toolbox-tool-item');
                var $parent = $(this).closest('.selector-container').parent();

                $item.remove();

                calcContainerValues($parent);

                fireSelectionChanged();
            }
        });

    $li.trigger('click');

    return true;
}

function openAddServiceSelector() {
    if (_selectedEpsg <= 0) {
        alert('Select an extent first...');
        return;
    }

    $('body').webgis_modal({
        title: 'Dienst hinzufügen',
        width: '640px',
        onload: function ($content) {
            $content
                .css('padding', '0px')
                .webgis_mapbuilder_serviceSelector({
                    serviceIds: orderedServices().split(','),
                    onAddService: function (serviceInfo, serviceInfos) {
                        return appendService(serviceInfo, serviceInfos, serviceInfo.isbasemap === true);   // Basemaps unten anfügen
                    },
                    onRemoveService: function (serviceInfo, serviceInfos) {
                        $('.webgis-toolbox-tool-item[data-id="' + serviceInfo.id + '"]')
                            .find('.delete-service')
                            .addClass('suppress-confirm')
                            .trigger('click');
                        return true;
                    },
                    checkAvailability: function (serviceInfo) {
                        return checkEpsg({ _element: serviceInfo });
                    }
                });
        }
    });
}

function openDynamicContentSelector(editContent) {
    $('body').webgis_modal({
        title: 'Dynamischer Inhalt',
        width:'640px',
        onload: function ($content) {
            $content.webgis_mapbuilder_dynamicContentSelector({
                content: editContent,
                onselect: function (content) {
                    if (editContent) {
                        updateDynamicContent(content);
                    } else {
                        appendDynamicContent(content);
                    }
                    $('body').webgis_modal('close');
                }
            });
        }
    });
};

function appendDynamicContent(content) {
    $ul = $(".selector-container[data-id='dynamic-content']").find('.webgis-toolbox-tool-item-group-details');

    content.id = guid();
    content.selected = true;

    var img = '';
    switch (content.type) {
        case 'geojson':
        case 'geojson-embedded':
            img = 'json-26-b.png';
            break;
        case 'georss':
            img = 'rss-26-b.png';
            break;
        case 'api-query':
            img = 'binoculars-26-b.png';
            break;
    }

    var $li = $("<li class='webgis-toolbox-tool-item webgis-toolbox-tool-item-selected' style='position:relative' data-id='" + content.id + "' data-containers='*'><span class='item-title'><img src='" + webgis.baseUrl + '/content/api/img/' + img + "'>&nbsp;" + content.name + "</span></li>").appendTo($ul).data('content', content)
        .click(function () {
            $(this).toggleClass('webgis-toolbox-tool-item-selected');
            $(this).data('content').selected = !$(this).data('content').selected;
            fireSelectionChanged();
        });

    $("<div>✖</div>").css({
        position: 'absolute', right:0, top:0, 'font-weight':'bold', "text-align":'center',
        width: 26, height: 26
    }).appendTo($li).click(function (event) {
        event.stopPropagation();
        if (confirm("Dynamischen Inhalt entfernen?")) {
            $(this).closest('.webgis-toolbox-tool-item').remove();
            fireSelectionChanged();
        }
    });
    $("<div>Edit</div>").css({
        position: 'absolute', right: 30, top: 16, 'color': '#aaa', "text-align": 'center',
        width: 46, height: 26
    }).appendTo($li).click(function (event) {
        event.stopPropagation();
        var content = $(this).closest('.webgis-toolbox-tool-item').data('content');
        
        openDynamicContentSelector(content);
    });

    fireSelectionChanged();
};

function updateDynamicContent(content) {
    $ul = $(".selector-container[data-id='dynamic-content']").find('.webgis-toolbox-tool-item-group-details');

    content.selected = true;

    var img = '';
    switch (content.type) {
        case 'geojson':
            img = 'json-26-b.png';
            break;
        case 'georss':
            img = 'rss-26-b.png';
            break;
        case 'api-query':
            img = 'binoculars-26-b.png';
            break;
    }

    var selected = false;
    $ul.find('.webgis-toolbox-tool-item').each(function (i, e) {
        var $li = $(e);
        if ($li.attr('data-id') == content.id) {
            selected = content.selected = $li.data('content').selected;
            $li.find('.item-title').html("<img src='" + webgis.baseUrl + '/content/api/img/' + img + "'>&nbsp;" + content.name);
            $li.data('content', content);
        }
    });

    if (selected)
        fireSelectionChanged();
};

function getDynamicContent() {
    $ul = $(".selector-container[data-id='dynamic-content']").find('.webgis-toolbox-tool-item-group-details');

    var contents = [];
    $ul.find('.webgis-toolbox-tool-item').each(function (i, e) {
        contents.push($(e).data('content'));
    });
    return contents;
}

var _snapshots = { active: "default", snapshots:[] };

function getSnapShot(name) {
    if (!_snapshots.snapshots)
         _snapshots.snapshots = [];

    var snapshot = webgis.firstOrDefault(_snapshots.snapshots, function (s) { return s.name === name });
    if (!snapshot) {
        snapshot = { name: name };
        _snapshots.snapshots.push(snapshot);
    }

    return snapshot;
};

function createPresentationSnapshot() {
    _snapshots.active = _snapshots.active || "default";

    var snapshot = getSnapShot(_snapshots.active);

    var $tab = $("#tab-presentations-content");

    snapshot.presentations = snapshot.presentations || {};
    snapshot.presentations.expanded = $tab.webgis_presentationToc('get_expanded_names');
    snapshot.presentations.visibility = [];

    var serviceIds = _map.serviceIds();
    for (var s in serviceIds) {
        var service = _map.getService(serviceIds[s]);
        if (service && !service.isBasemap) {
            snapshot.presentations.visibility.push({
                service: serviceIds[s],
                layers: service.getLayerVisibility()
            });
        }
    }
};

function createBoundsSnapshot() {
    if (_map) {
        _snapshots.active = _snapshots.active || "default";

        var snapshot = getSnapShot(_snapshots.active);

        snapshot.map = snapshot.map || {};
        snapshot.map.scale = _map.scale();
        snapshot.map.center = _map.getCenter();
        snapshot.map.bounds = _map.getExtent();
    }
}

function resetToSnapshot() {
    if (_snapshots && _snapshots.active) {
        var snapshot = getSnapShot(_snapshots.active);

        if (snapshot.presentations) {
            var $tab = $("#tab-presentations-content");
            $tab.webgis_presentationToc('expand', { names: snapshot.presentations.expanded, exclusive: true });

            if (_map && snapshot.presentations.visibility) {
                for (var v in snapshot.presentations.visibility) {
                    var visibility = snapshot.presentations.visibility[v];
                    var service = _map.getService(visibility.service);

                    if (service) {
                        service.setServiceVisibility(visibility.layers);
                    }
                }
            }
        }

        if (snapshot.map && _map) {
            var scale = null, center = null, bounds = null;

            if (snapshot.map.scale) {
                scale = snapshot.map.scale;
            }
            if (snapshot.map.center && snapshot.map.center.length === 2) {
                center = snapshot.map.center;
            }
            if (snapshot.map.bounds && snapshot.map.bounds.length === 4) {
                bounds = snapshot.map.bounds;
            }

            if (scale && center) {
                _map.setScale(scale, center);
            }
            else if (bounds) {
                _map.zoomTo(bounds);
            }
        }
    }
};

function guid() {
    function s4() {
        return Math.floor((1 + Math.random()) * 0x10000)
          .toString(16)
          .substring(1);
    }
    return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
      s4() + '-' + s4() + s4() + s4();
};

// implement JSON.stringify serialization
JSON.stringify = JSON.stringify || function (obj) {
    var t = typeof (obj);
    if (t != "object" || obj === null) {
        // simple data type
        if (t == "string") obj = '"' + obj + '"';
        return String(obj);
    }
    else {
        // recurse array or object
        var n, v, json = [], arr = (obj && obj.constructor == Array);
        for (n in obj) {
            v = obj[n]; t = typeof (v);
            if (t == "string") v = '"' + v + '"';
            else if (t == "object" && v !== null) v = JSON.stringify(v);
            json.push((arr ? "" : '"' + n + '":') + String(v));
        }
        return (arr ? "[" : "{") + String(json) + (arr ? "]" : "}");
    }
};
