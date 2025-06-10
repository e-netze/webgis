webgis.map._queryResultFeatures = function (map) {
    this._map = map;
    this._queryResultLayer = null;
    this._unclusteredMarkers = [];
    this._queryResultFeatures = null;
    this._connectPolyline = null;
    this._toolMarker = null;

    this.show = function (features) {
        var me = this;
        this.recluster();

        if (webgis.mapFramework === 'leaflet') {
            if (this._queryResultLayer != null) {
                this._map.frameworkElement.removeLayer(this._queryResultLayer);
            }

            this._queryResultLayer = L.geoJson(features, {
                style: function (feature) {
                    //return { color: feature.properties.color };
                    return {};
                },
                onEachFeature: function (feature, layer) {
                    webgis.bindFeatureMarkerPopup(me._map, layer, feature);
                }
            });

            this._queryResultLayer.addTo(this._map.frameworkElement);
            this._map.zoomToBoundsOrScale(features.bounds, webgis.featuresZoomMinScale(features));
        }

        this._queryResultFeatures = features;
        this.refreshConnectionPolyline();
        this.refreshToolMarker();

        if (features.features.length > 0) {
            webgis.removeAutoCompleteMapItem(this._map);
        }

        this._map.ui.refreshUIElements();
        this._map.events.fire('after_showqueryresults', this._map, features);

        return this._queryResultFeatures;
    };
    this.showClustered = function (features, zoom2results, showSinglePopup) {
        this.recluster();

        //showSinglePop = true;

        if (features.method === 'append' && this._queryResultFeatures !== null) {
            for (var f in features.features) {
                this._queryResultFeatures.features.push(features.features[f]);
            }
            features = this._queryResultFeatures;
        }

        if (features._sortedCols && features._sortedCols.length > 0) {
            webgis.sortFeatures(features, features._sortedCols)
        } else {
            features._sortedCols = null;
        }

        if (webgis.mapFramework === 'leaflet') {
            if (!L.MarkerClusterGroup) {
                return this.show(features);
            }
            if (this._queryResultLayer != null) {
                this._map.frameworkElement.removeLayer(this._queryResultLayer);
            }

            // show pop or highlight features
            showSinglePopup =
                showSinglePopup
                && features.features.length === 1
                && features.metadata
                && features.metadata.selected !== true;  // if the feature already will be hightlighted, there is no need to highlight it.

            var singleMarker = null;
            this._queryResultLayer = new L.MarkerClusterGroup(webgis.markerClusterOptions);

            for (var f in features.features) {
                var feature = features.features[f];

                // Index einmal speichern. Falls feature aus ergebnissen gelöscht wird, soll die Marker nummer gleich bleiben...
                var fIndex = 0;
                if (typeof feature._fIndex === "undefined") {
                    // Wenn auf feature noch kein Index (Markernummer) gespeichert ist, dann den maximalen + 1 nehmen
                    for (var fe in features.features) {
                        if (typeof features.features[fe]._fIndex === "undefined")
                            continue;

                        fIndex = Math.max(features.features[fe]._fIndex + 1, fIndex);
                    }
                    feature._fIndex = fIndex;
                } else {
                    fIndex = feature._fIndex;
                }

                if (!feature.geometry)
                    continue;
                var latLng = L.latLng(feature.geometry.coordinates[1], feature.geometry.coordinates[0]);
                //var marker = new L.Marker(latLng);

                var icon = "query_result";
                if (this.isDynamicContent() === true) {
                    if (this.isExtentDependent() === true) {
                        icon = "dynamic_content_extenddependent";
                    } else {
                        icon = "dynamic_content";
                    }
                }

                var marker = webgis.createMarker({
                    lat: feature.geometry.coordinates[1],
                    lng: feature.geometry.coordinates[0],
                    index: fIndex,
                    feature: feature,
                    icon: icon,
                    name: this.isDynamicContent() ? this._map.getCurrentDynamicContentName() : null
                });

                if (feature.oid) {
                    marker.oid = feature.oid;
                }

                webgis.bindFeatureMarkerPopup(this._map, marker, feature);
                this._queryResultLayer.addLayer(marker);

                if (showSinglePopup) {
                    singleMarker = marker;
                }
            }

            this._queryResultLayer.addTo(this._map.frameworkElement);

            if (zoom2results != false) {
                //console.log('zoomToBoundsOrScale', features.bounds, webgis.featuresZoomMinScale(features));
                this._map.zoomToBoundsOrScale(features.bounds, webgis.featuresZoomMinScale(features));
            }

            if (singleMarker != null) {
                if (webgis.usability.useMarkerPopup === false) {
                    var map = this._map;

                    webgis.delayed(function () {
                        singleMarker.fire('click');
                        if (!map.ui.tabsInSidebar()) {
                            map.events.fire('hidequeryresults');
                        }
                    }, 100);

                } else {
                    singleMarker.openPopup();
                }
            }
        }
        this._queryResultFeatures = features;
        this.refreshConnectionPolyline();
        this.refreshToolMarker();

        if (features.features.length > 0) {
            webgis.removeAutoCompleteMapItem(this._map);
        }
        this._map.ui.refreshUIElements();
        this._map.events.fire('after_showqueryresults', this._map, features);

        return this._queryResultFeatures;
    };
    this.uncluster = function (featureOid, openPopup) {
        if (!this.markersVisible()) {
            return;
        }

        var me = this, cluster = this._queryResultLayer, map = this._map;
        if (webgis.mapFramework === "leaflet") {
            cluster.eachLayer(function (marker) {
                if (marker.oid === featureOid) {
                    cluster.removeLayer(marker);
                    marker.addTo(map.frameworkElement);
                    me._unclusteredMarkers.push(marker);

                    if (openPopup) {
                        if (webgis.usability.useMarkerPopup) {
                            marker.openPopup();
                        } else {
                            marker.fire('click', { suppressScrollResultRowTo: true });
                        }
                    }

                    return false;
                }
            });
        }
    };
    this.recluster = function () {
        if (webgis.mapFramework == "leaflet") {
            for (var i in this._unclusteredMarkers) {
                var marker = this._unclusteredMarkers[i];
                this._map.frameworkElement.removeLayer(marker);
                this._queryResultLayer.addLayer(marker);
            }
        }
        this._unclusteredMarkers = [];
    };
    this.refreshConnectionPolyline = function () {
        if (webgis.mapFramework === 'leaflet') {
            if (this._connectPolyline) {
                this._map.frameworkElement.removeLayer(this._connectPolyline);
                this._connectPolyline = null;
            }

            if (this._queryResultFeatures && this._queryResultFeatures.metadata && this._queryResultFeatures.metadata.connect === true) {
                var latLngs = [];

                for (var f in this._queryResultFeatures.features) {
                    var feature = this._queryResultFeatures.features[f];

                    if (!feature.geometry)
                        continue;

                    latLngs.push(L.latLng(feature.geometry.coordinates[1], feature.geometry.coordinates[0]));
                }

                this._connectPolyline = L.polyline(latLngs || [], { color: '#00f', weight: 4 });
                this._map.frameworkElement.addLayer(this._connectPolyline);
            }
        }
    };
    this.refreshToolMarker = function () {
        if (webgis.mapFramework === 'leaflet') {
            if (this._toolMarker != null) {
                this._map.frameworkElement.removeLayer(this._toolMarker);
                this._toolMarker = null;
            }

            if (this._queryResultFeatures &&
                this._queryResultFeatures.metadata &&
                this._queryResultFeatures.metadata.tool === 'tsp' &&
                this._queryResultFeatures.metadata.startPoint) {

                var me = this;

                this._toolMarker = webgis.createMarker({
                    lat: this._queryResultFeatures.metadata.startPoint.lat,
                    lng: this._queryResultFeatures.metadata.startPoint.lng,
                    draggable: true, icon: "marker_draggable_bottom"
                });

                me._map.frameworkElement.addLayer(this._toolMarker);

                this._toolMarker.on('dragend', function (marker) {
                    var latlng = this.getLatLng();

                    webgis.showProgress('Berechne Rundreise...');

                    webgis.delayed(function () {
                        var orderedFeatures = webgis.tsp.orderFeatures(me._map.calcCrs(), me._queryResultFeatures, latlng);;

                        webgis.hideProgress('Berechne Rundreise...');

                        // remove current dynamic content
                        if (me._map.hasCurrentDynamicContent()) {
                            me._map.unloadDynamicContent();
                        }

                        me._map.ui.showQueryResults(orderedFeatures, true, orderedFeatures.metadata ? orderedFeatures.metadata.dynamicContentDef : null);
                    }, 100);

                });
            }
        }
    };

    this.tryHighlightMarker = function (featureOid, exclusive) {
        if (exclusive === true)
            this.tryUnhighlightMarkers();

        //if (!this.markersVisible()) {
        //    return;
        //}

        if (this._queryResultLayer != null) {
            this._queryResultLayer.eachLayer(function (marker) {
                if (marker.oid === featureOid) {
                    webgis.tryHighlightMarker(marker, true);
                }
            });
        }

        if (this._unclusteredMarkers != null) {
            for (var i in this._unclusteredMarkers) {
                var marker = this._unclusteredMarkers[i];
                if (marker.oid === featureOid) {
                    webgis.tryHighlightMarker(marker, true);
                }
            }
        }
    };
    this.tryUnhighlightMarkers = function () {
        if (this._queryResultLayer != null) {
            this._queryResultLayer.eachLayer(function (marker) {
                webgis.tryHighlightMarker(marker, false);
            });
        }

        if (this._unclusteredMarkers != null) {
            for (var i in this._unclusteredMarkers) {
                var marker = this._unclusteredMarkers[i];
                webgis.tryHighlightMarker(marker, false);
            }
        }
    };
    this.tryHighlightFeature = function (feature) {
        if (feature && feature.oid) {
            var qid = feature.oid.split(':');
            var service = this._map.getService(qid[0]);

            if (service != null) {
                var query = service.getQuery(qid[1]);
                if (query != null) {
                    var layer = service.getLayer(query.layerid);
                    //if (layer != null && layer.selectable !== false) {
                    if (!layer || layer.selectable !== false) {   // if layer not in service (only visible on backend), hightlight anyway 
                        var selection = this._map.getSelection('query');
                        if (selection) {
                            selection.setTargetQuery(service, query.id, qid[2]);
                        }
                    }
                }
            }

            this._map.events.fire('feature-highlighted', this._map, feature);
        }
    };
    this.clear = function (suppressEvent) {
        this.recluster();
        if (webgis.mapFramework === 'leaflet') {
            if (this._queryResultLayer != null)
                this._map.frameworkElement.removeLayer(this._queryResultLayer);
        }

        this._queryResultLayer = null;

        this._queryResultFeatures = null;
        this.refreshConnectionPolyline();
        this.refreshToolMarker();

        if (suppressEvent !== true) {
            this._map.events.fire('clearqueryresults');
        }

        this._map.events.fire('querymarkersremoved');
        this._map.ui.refreshUIElements();
    };
    this.features = function () {
        return this._queryResultFeatures;
    };
    this.clonedFeatures = function (removeNotStringifyObjects) {
        var features = Object.assign({}, this._queryResultFeatures);

        if (removeNotStringifyObjects) {
            if (features.metadata && features.metadata.dynamicContentDef) {
                delete features.metadata.dynamicContentDef;
            }
        }

        return features;
    }
    this.getFeature = function (oid) {
        for (var f in this._queryResultFeatures.features) {
            var feature = this._queryResultFeatures.features[f];
            if (feature.oid === oid)
                return feature;
        }
        return null;
    };
    this.countFeatures = function () {
        return this._queryResultFeatures && this._queryResultFeatures.features ?
            this._queryResultFeatures.features.length : 0;
    };
    this.countCheckedFeatures = function () {
        return this.checkedFeatures().length;
    };
    this.countUncheckedFeatures = function () {
        return this.uncheckedFeatures().length;
    };
    this.checkedFeatures = function () {
        var result = [];
        if (this.countFeatures() > 0) {
            for (var f in this._queryResultFeatures.features) {
                var feature = this._queryResultFeatures.features[f];
                if (feature._tableChecked === true)
                    result.push(feature);
            }
        }

        return result;
    };
    this.uncheckedFeatures = function () {
        var result = [];
        if (this.countFeatures() > 0) {
            for (var f in this._queryResultFeatures.features) {
                var feature = this._queryResultFeatures.features[f];
                if (!feature._tableChecked)
                    result.push(feature);
            }
        }

        return result;
    };

    this.removeMarkersAndSelection = function () {
        var map = this._map;

        // let the UI Tool do the job, if exists (old)
        var removeButton = $(this).closest('.webgis-container').find('.webgis-toolbox-tool-item.remove-queryresults');
        if (removeButton.length > 0) {
            removeButton.click();
        } else {
            map.queryResultFeatures.clear(false);
            map.getSelection('selection').remove();
            map.getSelection('query').remove();
            map.getSelection('custom').remove();
        }
        map.unloadDynamicContent();
    };

    this.isExtentDependent = function () { return this._map && this._map.isCurrentDynamicContentExtentDependent() ? true : false; };
    this.isDynamicContent = function () { return this._map && this._map.hasCurrentDynamicContent() ? true : false };
    this.supportsZoomToFeature = function () { return this.isExtentDependent() === false; };
    this.supportsRemoveFeature = function () { return this.isExtentDependent() === false; };
    this.supportsRemoveFeatures = function () { return this.isExtentDependent() === false; };
    this.supportsBufferFeatures = function () { return this.isExtentDependent() === false && this.hasTableProperties(); };
    this.supportsHighlightFeature = function () { return this.isExtentDependent() == false && this.hasTableProperties(); };
    this.supportsSelectFeatures = function () { return this.isExtentDependent() === false && this.hasTableProperties(); };

    this.removeFeature = function (oid, suppressFireChangedEvent) {
        if (!this._queryResultFeatures || !this._queryResultFeatures.features)
            return;

        var features = [], found = false;

        for (var f in this._queryResultFeatures.features) {
            var feature = this._queryResultFeatures.features[f];
            if (feature.oid !== oid) {
                features.push(feature);
            } else {
                found = true;
            }
        }

        if (!found) {
            return false;
        }

        this._queryResultFeatures.features = features;
        this.showClustered(this._queryResultFeatures, false, false);
        this._map.events.fire('queryresult_removefeature', this._map, oid);

        if (suppressFireChangedEvent !== true) {
            this.fireFeaturesRemovedOrReplaced();
        }

        return true;
    };
    this.removeFeatures = function (ids) {
        var foundAny = false;

        if (!ids) {
            this.clear();
            foundAny = true;
        } else {
            for (var i in ids) {
                if (this.removeFeature(ids[i], true) == true) {
                    foundAny = true;
                }
            }
        }

        if (foundAny) {
            this.fireFeaturesRemovedOrReplaced();
        }
    };
    this.removeFeaturesFromInactiveTabs = function (ids) {
        var $tabControl = this._map.ui.getQueryResultTabControl();
        if (!$tabControl || !$.fn.webgis_tab_control) {
            return;
        }

        var tabOptions = $tabControl.webgis_tab_control('getTabsOptions');
        if (!tabOptions) {
            return;
        }

        for (var o in tabOptions) {
            var options = tabOptions[o];

            if (!options ||
                options.selected ||  // only the inactive tabs, for active use removeFeatures method
                !options.payload || !options.payload.features || !options.payload.features.features) {
                continue;
            }

            var featureCollection = options.payload.features;

            featureCollection.features = $.grep(featureCollection.features, function (feature, i) {
                return $.inArray(feature.oid, ids) < 0;
            });
        }
    };
    this.replaceFeatures = function (replaceFeatures) {
        if (!replaceFeatures || !replaceFeatures.features) {
            return;
        }

        var me = this, found = false;
        if (this._queryResultFeatures && this._queryResultFeatures.features) {
            for (var r in replaceFeatures.features) {
                var replaceFeature = replaceFeatures.features[r];

                var features = $.grep(this._queryResultFeatures.features, function (feature, i) {
                    return feature.oid === replaceFeature.oid;
                });

                if (features.length > 0) {
                    found = true;
                }

                $.each(features, function (i, feature) {
                    feature.geometry = replaceFeature.geometry || feature.geometry;
                    feature.properties = replaceFeature.properties || feature.properties;
                    feature.bounds = replaceFeature.bounds || feature.bounds;

                    me._map.events.fire('queryresult_replacefeature', me._map, feature);
                });
            }
        }

        if (found) {
            me._map.events.fire('queryresult_replacefeatures', me._map, replaceFeatures.features);

            this.fireFeaturesRemovedOrReplaced();
        }
    };
    this.replaceFeaturesFromInactiveTabs = function (replaceFeatures) {
        var $tabControl = this._map.ui.getQueryResultTabControl();
        if (!$tabControl || !$.fn.webgis_tab_control) {
            return;
        }

        var tabOptions = $tabControl.webgis_tab_control('getTabsOptions');
        if (!tabOptions) {
            return;
        }

        for (var o in tabOptions) {
            var options = tabOptions[o];

            if (!options ||
                options.selected ||  // only the inactive tabs, for active use replaceFeatures method
                !options.payload || !options.payload.features || !options.payload.features.features) {
                continue;
            }

            for (var r in replaceFeatures.features) {
                var replaceFeature = replaceFeatures.features[r];

                var features = $.grep(options.payload.features.features, function (feature, i) {
                    return feature.oid === replaceFeature.oid;
                });

                $.each(features, function (i, feature) {
                    feature.geometry = replaceFeature.geometry || feature.geometry;
                    feature.properties = replaceFeature.properties || feature.properties;
                    feature.bounds = replaceFeature.bounds || feature.bounds;
                });
            }
        }
    };

    this.reorderFeatures = function (oids) {
        var newFeatures = [];

        for (var i in oids) {
            var feature = this.getFeature(oids[i]);
            if (!feature) {
                throw 'unknown feature ' + oids;
            }

            newFeatures.push({
                type: feature.type,
                oid: feature.oid,
                geometry: $.extend({}, feature.geometry),
                bounds: feature.bounds,
                properties: $.extend({}, feature.properties),
                __queryId: feature.__queryId,
                __serviceId: feature.__serviceId,
                _findex: i
            });
        }

        var newFeatureCollection = {
            type: 'FeatureCollection',
            features: newFeatures,
            metadata: this._queryResultFeatures.metadata,
        };

        this._map.ui.showQueryResults(newFeatureCollection, true, newFeatureCollection.metadata ? newFeatureCollection.metadata.dynamicContentDef : null);
    };

    this.fireFeaturesRemovedOrReplaced = function () {
        this._map.events.fire('queryresult_removed-or-replaced', this._map);
    };

    this.hasTableProperties = function (features) {
        features = features || this._queryResultFeatures;
        if (!features || !features.features) {
            return false;
        }

        for (var f in features.features) {
            if (webgis.featureHasTableProperties(features.features[f])) {
                return true;
            }
        }

        return false;
    };
    this.tableFields = function () {
        if (this._queryResultFeatures && this._queryResultFeatures.metadata) {
            return this._queryResultFeatures.metadata.table_fields || null;
        }
        return null;
    };
    this.showTable = function (features, scrollTo) {
        features = features || this._queryResultFeatures;

        var map = this._map;
        var $content = this._map.ui.getQueryResultTabControl(),
            useTabControl = $content ? true : false;

        if (!this.hasTableProperties(features)) {  
            if (useTabControl) { // WMS Services with text/html GetFeatureInfo => simply show as result list without tool bar
                $content = $content.webgis_tab_control('currentContent').empty();
                map.ui._setResultFrameSize();
                $content.webgis_queryResultsList({ features: features.features, map: map, showToolbar: false });
            } 
            return;
        }
        
        var sortedCols = this._queryResultFeatures._sortedCols;
        var currentDynamicContent = map.getCurrentDynamicContent();

        webgis.sortFeatures(features, sortedCols || []);

        var service = this.firstService(features.features),
            query = this.firstQuery(features.features);

        const editToolId = 'webgis.tools.editing.edit';
        const editToolMergeId = 'webgis.tools.editing.desktop.advanced.mergefeatures';
        const editToolClipId = 'webgis.tools.editing.desktop.advanced.clipfeatures';
        const editToolCutId = 'webgis.tools.editing.desktop.advanced.cutfeatures';
        const editToolDeleteSelectedId = 'webgis.tools.editing.desktop.advanced.deleteselectedfeatures';
        const editToolMassattributionId = 'webgis.tools.editing.desktop.advanced.massattributation';
        const selectionToolAddId = 'webgis.tools.addtoselection';
        const selectionToolRemoveId = 'webgis.tools.removefromselection';

        if (!useTabControl) {
            this._map.ui.webgisContainer().webgis_dockPanel({
                title: 'Abfrageergebnisse',
                id: 'queryresults_dockpanel_single',
                dock: 'bottom',
                onload: function ($content, $dockpanel) {
                    $dockpanel.addClass('webgis-result webgis-result-table')  // Dockpanels mit diesem Attribut, werden geschlossen, wenn Abfrage entfernt wird, oder neue Abfrage gemacht wird
                        .attr('data-id', '');
                },
                maxSize: '40%',
                usePadding: false,
                autoResize: true,
                refElement: this._map.ui.mapContainer(),
                map: this._map
            });

            $content = this._map.ui.webgisContainer().webgis_dockPanel('content', { id: 'queryresults_dockpanel_single' });
        } else {
            $content = $content
                .webgis_tab_control('currentContent').empty();

            this._map.ui._setResultFrameSize();
        }

        $content
            .addClass('webgis-table-panel-content')
            .css('padding', '0px');

        var $tabToolsContainer = $("<div>")
            .addClass('webgis-result-table-tools-container')
            .appendTo($content);

        if (!webgis.usability.tableToolsContainerExtended) {
            $tabToolsContainer.addClass('collapsed');
            $content.addClass('tools-collapsed');
        }

        $("<div>")
            .addClass('toggle-button')
            .appendTo($tabToolsContainer)
            .click(function (e) {
                e.stopPropagation();

                var $toolsContainer = $(this).closest('.webgis-result-table-tools-container');
                var $content = $(this).closest('.webgis-table-panel-content');

                $toolsContainer.toggleClass('collapsed');
                if ($toolsContainer.hasClass('collapsed')) {
                    $content.addClass('tools-collapsed');
                    webgis.usability.tableToolsContainerExtended = false;
                } else {
                    $content.removeClass('tools-collapsed');
                    webgis.usability.tableToolsContainerExtended = true;
                }
            });

        var $tabTools = $("<ul>")
            .addClass('webgis-result-table-tools')
            .appendTo($tabToolsContainer);

        // Toolbar Functions
        var $selectButton, $addButton, $removeButton;
        var createToolbarButtonBlock = function (label, classes, elementType) {
            label = webgis.l10n.get(label);

            let $li = $("<li>")
                .addClass('webgis-toolbox-tool-block' + (classes ? ' ' + classes : ''))
                .appendTo($tabTools);

            $("<div>")
                .addClass('webgis-toolbox-tool-block-title')
                .text(label)
                .appendTo($li);

            return $(elementType ? "<" + elementType + ">" : "<ul>")
                .addClass('webgis-toolbox-tool-block-content')
                .appendTo($li);
        }

        var createToolbarButton = function ($parent, label, img, title) {
            label = webgis.l10n.get(label);
            title = webgis.l10n.get(title);

            let $li = $("<li>")
                .attr('title', title || label)
                .addClass("webgis-toolbox-tool-item")
                .appendTo($parent)

            let $content = $("<span>")
                .addClass("webgis-toolbox-tool-item-span")
                .appendTo($li);

            $("<img>")
                .attr('src', img)
                .appendTo($content);
            $("<span>")
                .addClass("webgis-toolbox-tool-item-label")
                .text(label)
                .appendTo($content);

            return $li;
        };

        let finalizeToolbar = function () {
            switch (map.getToolId(map.getActiveTool())) {
                case 'webgis.tools.addtoselection':
                    if ($addButton) {
                        $addButton.addClass('selected');
                    }
                    break;
                case 'webgis.tools.removefromselection':
                    if ($removeButton) {
                        $removeButton.addClass('selected');
                    }
                    break;
            }

            if ($addButton) $addButton.addClass('webgis-dependencies webgis-dependency-not-activetool ' + printToolIdClass);
            if ($removeButton) $removeButton.addClass('webgis-dependencies webgis-dependency-not-activetool ' + printToolIdClass);

            $tabTools.find('.webgis-toolbox-tool-item[data-toolid]').each(function () {
                let $button = $(this);
                let tool = map.getTool($button.attr('data-toolid'));
                if (tool && tool.dependencies && tool.dependencies.length > 0) {
                    $button.addClass('webgis-dependencies');
                    for (var d = 0; d < tool.dependencies.length; d++) {
                        $button.addClass('webgis-dependency-' + tool.dependencies[d]);
                    }
                }
            });

            
            if($tabTools.find('.webgis-toolbox-tool-item.webgis-dependency-activetool.webgis-tools-editing-edit').length > 0) {
               createToolbarButtonBlock('tip-edit','webgis-tips webgis-dependencies webgis-dependency-not-activetool webgis-tools-editing-edit', 'div')
                    .text(webgis.l10n.get("tip-edit-content"))
                    .click(function(e) {
                        e.stopPropagation();
                        $(this).closest('.webgis-result-table-tools').find('.webgis-selection-edittool-shortcut').trigger('click');
                    });
            }
            else if($tabTools.find('.webgis-toolbox-tool-item.webgis-dependencies.webgis-dependency-hasselection').length > 0) {
                createToolbarButtonBlock('tip-select', 'webgis-tips webgis-dependencies webgis-dependency-hasnoselection', 'div')
                    .text(webgis.l10n.get("tip-select-content"))
                    .click(function (e) {
                        e.stopPropagation();
                        $(this).closest('.webgis-result-table-tools').find('.webgis-selection-button').trigger('click');
                    });
            }

            createToolbarButtonBlock('tip-tools','webgis-tips', 'div')
                .text(webgis.l10n.get("tip-tools-content"));
                
        }; 

        let exportButtonClick = function (e) {
            e.stopPropagation();

            var $table = $(this)
                .closest('.webgis-table-panel-content')
                .find('.webgis-table-container')
                .children('table');

            // Server Side

            // collect featureIds from table (includes sorting)
            var featureIds = [], uniqueFeatureIds = true;
            $table.find('tr[data-id]').each(function () {
                var featureId = $(this).attr('data-id');
                if (featureId.indexOf(':') >= 0) {
                    var parts = featureId.split(':');
                    featureId = parts[parts.length - 1];
                }

                featureId = parseInt(featureId);
                if ($.inArray(featureId, featureIds) >= 0) {
                    uniqueFeatureIds = false;  // Unioned features?
                }
                featureIds.push(featureId);
            });

            var data = { format: $(this).data('format-id') };
            if (uniqueFeatureIds === true && map.queryResultFeatures.firstService() && map.queryResultFeatures.firstQuery) {
                data.serviceId = map.queryResultFeatures.firstService().id;
                data.queryId = map.queryResultFeatures.firstQuery().id;
                data.featureIds = featureIds.join(',');
            } else {
                var clonedFetures = map.queryResultFeatures.clonedFeatures(true);
                data.queryFeatures = JSON.stringify(clonedFetures, null, 2);
            }

            webgis.ajax({
                type: 'post',
                url: webgis.baseUrl + '/rest/exportfeatures',
                data: webgis.hmac.appendHMACData(data),
                success: function (result) {
                    if (result.success === true && result.downloadid) {
                        window.open(webgis.baseUrl + '/rest/download?id=' + result.downloadid + '&n=' + result.name);
                    }
                }
            });
        };

        let selectable = map.queryResultFeatures.supportsSelectFeatures();
        let $removeUnchecked = null, $removeChecked = null;
        // Result Menu

        let $selectionBlock = createToolbarButtonBlock("selection"), $toolBlock = null;

        if (selectable) {
            var printToolIdClass = 'webgis-tools-print';

            $markerButton = createToolbarButton($selectionBlock, "button-show-markers", webgis.css.imgResource('marker-26.png', 'tools'))
                .addClass('webgis-marker-button webgis-toggle-button selected webgis-dependencies webgis-dependency-not-activetool ' + printToolIdClass)
                .click(function () {
                    map.queryResultFeatures.setMarkerVisibility($(this).toggleClass('selected').hasClass('selected'), $(this))
                });

            $selectButton = createToolbarButton($selectionBlock, "button-select", webgis.css.imgResource('select_features.png', 'tools'))
                .addClass('webgis-selection-button webgis-toggle-button')
                .click(function () {
                    map.queryResultFeatures.setSelection($(this).toggleClass('selected').hasClass('selected'), $(this))
                });

            if (service && query && service.isEditLayer(query.layerid) &&
                $tabTools.closest('.webgis-container').find(".webgis-toolbox-tool-item[data-toolid='webgis.tools.editing.edit']").length >= 1) {
                var editToolIdClass = 'webgis-tools-editing-edit';

                if (useTabControl) {
                    createToolbarButton($selectionBlock, editToolId, webgis.css.imgResource(webgis.baseUrl + '/rest/toolresource/webgis-tools-editing-edit-edit', 'tools'), editToolId + '.tooltip')
                        .css('display', 'none')
                        .data('selectionButton', $selectButton)
                        .addClass('webgis-selection-edittool-shortcut webgis-dependencies webgis-dependency-not-activetool ' + editToolIdClass)
                        .click(function () {
                            $(this).closest('.webgis-container').find(".webgis-toolbox-tool-item[data-toolid='webgis.tools.editing.edit']").trigger('click');
                            var $selectButton = $(this).data('selectionButton');
                            if (!$selectButton.hasClass('selected')) {
                                $selectButton.trigger('click');
                            }
                        });


                    let $editingBlock = createToolbarButtonBlock('edit', 'webgis-dependencies webgis-dependency-hasselection webgis-dependency-activetool ' + editToolIdClass);

                    if (service.hasEditLayerPermissions(query.layerid, ['insert', 'update', 'delete', 'geometry'])) {
                        if (features.features.length > 1) {
                            createToolbarButton($editingBlock, editToolMergeId, webgis.css.imgResource(webgis.baseUrl + '/rest/toolresource/webgis-tools-editing-edit-merge', 'tools'), editToolMergeId+'.tooltip')
                                .css('display', 'none')
                                .addClass('webgis-dependencies webgis-dependency-hasselection webgis-dependency-activetool ' + editToolIdClass)
                                .click(function () {
                                    var args = [];
                                    webgis.tools.onButtonClick(map, { command: 'mergefeatures', type: 'servertoolcommand', map: map }, this, null, args);
                                });
                        }
                        createToolbarButton($editingBlock, editToolCutId, webgis.css.imgResource(webgis.baseUrl + '/rest/toolresource/webgis-tools-editing-edit-cut', 'tools'), editToolCutId + '.tooltip')
                            .css('display', 'none')
                            .addClass('webgis-dependencies webgis-dependency-hasselection webgis-dependency-activetool ' + editToolIdClass)
                            .click(function () {
                                var args = [];
                                webgis.tools.onButtonClick(map, { command: 'cutfeatures', type: 'servertoolcommand', map: map }, this, null, args);
                            });

                        createToolbarButton($editingBlock, editToolClipId, webgis.css.imgResource(webgis.baseUrl + '/rest/toolresource/webgis-tools-editing-edit-clip', 'tools'), editToolClipId + '.tooltip')
                            .css('display', 'none')
                            .addClass('webgis-dependencies webgis-dependency-hasselection webgis-dependency-activetool ' + editToolIdClass)
                            .click(function () {
                                var args = [];
                                webgis.tools.onButtonClick(map, { command: 'clipfeatures', type: 'servertoolcommand', map: map }, this, null, args);
                            });
                    }

                    if (service.hasEditLayerPermission(query.layerid, 'delete')) {
                        createToolbarButton($editingBlock, editToolDeleteSelectedId, webgis.css.imgResource(webgis.baseUrl + '/rest/toolresource/webgis-tools-editing-edit-delete', 'tools'), editToolDeleteSelectedId + '.tooltip')
                            .css('display', 'none')
                            .addClass('webgis-dependencies webgis-dependency-hasselection webgis-dependency-activetool ' + editToolIdClass)
                            .click(function () {
                                var args = [];
                                webgis.tools.onButtonClick(map, { command: 'deleteselectedfeatures', type: 'servertoolcommand', map: map }, this, null, args);
                            });
                    }

                    if (service.hasEditLayerPermission(query.layerid, 'massattributation')) {
                        createToolbarButton($editingBlock, webgis.l10n.get(editToolMassattributionId), webgis.css.imgResource(webgis.baseUrl + '/rest/toolresource/webgis-tools-editing-edit-mass', 'tools'), webgis.l10n.get(editToolMassattributionId + '.tooltip'))
                            .css('display', 'none')
                            .addClass('webgis-dependencies webgis-dependency-hasselection webgis-dependency-activetool ' + editToolIdClass)
                            .click(function () {
                                var args = [];
                                webgis.tools.onButtonClick(map, { command: 'massattributation', type: 'servertoolcommand', map: map }, this, null, args);
                            });
                    }
                }
            }

            if (map.queryResultFeatures.supportsBufferFeatures()) {
                createToolbarButton($toolBlock = $toolBlock || createToolbarButtonBlock('tools'), "buffer-results", webgis.css.imgResource('buffer.png', 'tools'))
                    .click(function () {
                        map.ui.showBufferDialog();
                    });
            }

            if (map.getTool('webgis.tools.addtoselection')) {
                $addButton = createToolbarButton($selectionBlock, selectionToolAddId, webgis.css.imgResource('cursor-plus-26-b.png', 'tools'), selectionToolAddId + ".tooltip")
                    .attr('data-toolid', 'webgis.tools.addtoselection')
                    .click(function () {
                        if ($(this).hasClass('selected')) {
                            map.setActiveTool(map.getDefaultTool());
                        } else {
                            var tool = map.getTool('webgis.tools.addtoselection');
                            if (tool)
                                map.setActiveTool(tool);
                        }
                    });
            }
            if (map.getTool('webgis.tools.removefromselection')) {
                $removeButton = createToolbarButton($selectionBlock, selectionToolRemoveId, webgis.css.imgResource('cursor-minus-26-b.png', 'tools'), selectionToolRemoveId + ".tooltip")
                    .attr('data-toolid', 'webgis.tools.removefromselection')
                    .click(function () {
                        if ($(this).hasClass('selected')) {
                            map.setActiveTool(map.getDefaultTool());
                        } else {
                            var tool = map.getTool('webgis.tools.removefromselection');
                            if (tool)
                                map.setActiveTool(tool);
                        }
                    });
            }

            $removeUnchecked = createToolbarButton($selectionBlock, "Nicht ausgewählte entfernen", webgis.css.imgResource('remove-unselected-26.png', 'tools'))
                .click(function () {
                    var uncheckedFeatures = map.queryResultFeatures.uncheckedFeatures();
                    for (var f in uncheckedFeatures) {
                        map.queryResultFeatures.removeFeature(uncheckedFeatures[f].oid, true);
                    }
                    map.queryResultFeatures.fireFeaturesRemovedOrReplaced();
                    refreshHeaderMenuButtons();
                });
            $removeChecked = createToolbarButton($selectionBlock, "Ausgewählte entfernen", webgis.css.imgResource('remove-selected-26.png', 'tools'))
                .click(function () {
                    var uncheckedFeatures = map.queryResultFeatures.checkedFeatures();
                    for (var f in uncheckedFeatures) {
                        map.queryResultFeatures.removeFeature(uncheckedFeatures[f].oid, true);
                    }
                    map.queryResultFeatures.fireFeaturesRemovedOrReplaced();
                    refreshHeaderMenuButtons();
                });
        }

        createToolbarButton($selectionBlock, "button-remove-markers", webgis.css.imgResource('marker-remove-26.png', 'tools'))
            .addClass('webgis-dependencies webgis-dependency-not-activetool ' + printToolIdClass)
            .click(function () {
                map.queryResultFeatures.removeMarkersAndSelection();
            });

        if (webgis.usability.tsp && webgis.usability.tsp.allowOrderFeatures === true && !map.queryResultFeatures.isExtentDependent()) {
            createToolbarButton($toolBlock = $toolBlock || createToolbarButtonBlock('tools'), "Rundreise", webgis.css.imgResource('roundtrip-26.png', 'tools'))
                .addClass('webgis-dependencies webgis-dependency-calc-tsp')
                .click(function () {
                    webgis.tsp.orderFeaturesWithUI(map);
                });
        }

        if ($toolBlock) $toolBlock.addClass('webgis-dependencies webgis-dependency-not-activetool ' + printToolIdClass);

        let $exportBlock = createToolbarButtonBlock('export-transfer').addClass('webgis-dependencies webgis-dependency-not-activetool ' + printToolIdClass);

        if (service && service.hasQueryFeatureTransfers(query.id)) {
            createToolbarButton($exportBlock, "Objekt Transfer", webgis.css.imgResource(webgis.baseUrl + '/rest/toolresource/webgis-tools-editing-edit-move', 'tools'), "Objekte in andere Featureklasse kopieren/verschieben")
                .addClass('webgis-dependencies webgis-dependency-hasselection')
                .click(function () {
                    var args = [];
                    webgis.tools.onButtonClick(map, { command: 'features_transfer', type: 'servertoolcommand_ext', id: editToolId, map: map }, this, null, args);
                });
        }

        createToolbarButton($exportBlock, "CSV", webgis.css.imgResource('export-csv-26.png'))
            .data('format-id', '_csv')
            .click(exportButtonClick);

        createToolbarButton($exportBlock, "MS Excel (CSV)", webgis.css.imgResource('export-csv-26-excel.png'))
            .data('format-id', '_csv_excel')
            .click(exportButtonClick);

        if (features.metadata && features.metadata.links) {
            $tabTools.data('feature.metadata', features.metadata);  // Links können sich ändern (add/remove features). Referenz auf Objekt merken und erst beim klick eigentlichen Link auslesen
            for (var l in features.metadata.links) {
                createToolbarButton($exportBlock, l, webgis.css.imgResource('external-link-26.png'))
                    .data('linkname', l)
                    .click(function () {
                        let featureMetadata = $(this).closest('.webgis-result-table-tools').data('feature.metadata');
                        if (featureMetadata) {
                            const linkname = $(this).data('linkname');
                            const linktarget = featureMetadata.linktargets[linkname];
                            if (linktarget === 'dialog') {
                                webgis.iFrameDialog(featureMetadata.links[linkname], linkname);
                            } else {
                                window.open(featureMetadata.links[linkname]);
                            }
                        }
                    });
            }
        }

        finalizeToolbar();

        if (query && query.table_export_formats) {
            for (var f in query.table_export_formats) {
                var tableExportFormat = query.table_export_formats[f];

                //console.log(tableExportFormat);

                createToolbarButton($exportBlock, tableExportFormat.name, webgis.css.imgResource('export-26.png'))
                    .data('format-id', tableExportFormat.id)
                    .click(exportButtonClick);
            }
        }

        var $menuItems = $("<ul>")
            .addClass('webgis-table-menuitems')
            .appendTo($content);

        var $tabTarget = $("<div>")
            .addClass('webgis-table-container')
            .appendTo($content)

        $tabTarget.webgis_queryResultsTable({
            map: this._map,
            features: features,
            reorderable: map.queryResultFeatures.reorderAble(features),
            addCheckboxes: $removeChecked && $removeUnchecked,
            addZoomTo: !currentDynamicContent || !currentDynamicContent.extentDependend,
            appendAdvancedFeatureButtons: useTabControl
        });

        $content.find('a').each(function (i, a) {
            $(a).closest('td').click(function (e) { e.stopPropagation(); });
        });
        $content.find('.webgis-result[data-id]').each(function (i, resultrow) {
            var $resultrow = $(resultrow);
            $resultrow
                .click(function (e) {
                    e.stopPropagation();

                    var $row = $(this).closest('.webgis-result');
                    var featureOid = $row.attr('data-id');
                    var feature = map.queryResultFeatures.getFeature(featureOid);
                    if (feature) {
                        map.queryResultFeatures.handlers.featureResultClick(feature,
                            $row.hasClass($.fn.webgis_queryResultsTable.selectedRowClass),
                            $row.hasClass('webgis-result-suppresszoom'));
                    }
                })
                .on('mouseenter', function (e) {
                    if (map.hoverSketch) {
                        var $row = $(this).closest('.webgis-result');
                        var featureOid = $row.attr('data-id');

                        var feature = map.queryResultFeatures.getFeature(featureOid);

                        //console.log('enter:' + feature.oid);

                        if (feature && feature.hover_geometry) {
                            map.hoverSketch.fromJson(feature.hover_geometry, false, true);  // append = false, readonly = true
                        }
                    }
                })
                .on('mouseleave', function (e) {
                    if (map.hoverSketch) {
                        map.hoverSketch.hide();
                    }
                });
        });

        var refreshHeaderMenuButtons = function () {
            var $headerCheck = $content.find('.webgis-result-table-header.webgis-result-table-menucell .menubutton.checkbox')
                .removeClass('checked')
                .removeClass('semi-checked');
            var checkedCount = map.queryResultFeatures.countCheckedFeatures(), uncheckedCount = map.queryResultFeatures.countUncheckedFeatures();

            if (uncheckedCount === 0) {
                $headerCheck.addClass('checked');
            }
            else if (checkedCount > 0) {
                $headerCheck.addClass('semi-checked');
            }

            if (checkedCount * uncheckedCount > 0 && $tabToolsContainer.hasClass('collapsed')) {
                $tabToolsContainer.children('.toggle-button').trigger('click');
            }

            $removeUnchecked.css('display', checkedCount === 0 || uncheckedCount === 0 ? 'none' : '').find('.webgis-toolbox-tool-item-label').text("[" + uncheckedCount + "] entfernen");
            $removeChecked.css('display', checkedCount === 0 || uncheckedCount === 0 ? 'none' : '').find('.webgis-toolbox-tool-item-label').text("[" + checkedCount + "] entfernen");
        }

        if ($removeChecked && $removeUnchecked) {
            refreshHeaderMenuButtons();
        }

        // Menucell - Rows
        $content.find('.webgis-result[data-id] .webgis-result-table-menucell .menubutton.checkbox')
            .click(function (e) {
                e.stopPropagation();

                var featureOid = $(this).closest('.webgis-result').attr('data-id');
                var feature = map.queryResultFeatures.getFeature(featureOid);
                if (feature) {
                    $(this).toggleClass('checked');
                    feature._tableChecked = $(this).hasClass('checked');
                }

                refreshHeaderMenuButtons();
            });

        $content.find('.webgis-result[data-id] .webgis-result-table-menucell .menubutton.zoom')
            .click(function (e) {
                e.stopPropagation();
                var featureOid = $(this).closest('.webgis-result').attr('data-id');
                var feature = map.queryResultFeatures.getFeature(featureOid);
                if (feature && feature.bounds) {
                    map.zoomToBoundsOrScale(feature.bounds, webgis.featureZoomMinScale(feature));
                }
            });
        $content.find('.webgis-result[data-id] .webgis-result-table-menucell .menubutton.pan')
            .click(function (e) {
                e.stopPropagation();
                var featureOid = $(this).closest('.webgis-result').attr('data-id');
                var feature = map.queryResultFeatures.getFeature(featureOid);
                if (feature && feature.bounds) {
                    var center = [(feature.bounds[0] + feature.bounds[2]) * 0.5, (feature.bounds[1] + feature.bounds[3]) * 0.5];
                    map.setCenter(center, 0.3);
                }
            });
        $content.find('.webgis-result[data-id] .webgis-result-table-menucell .menubutton.marker')
            .click(function (e) {
                e.stopPropagation();

                $(this).closest('tr').trigger('click');
            });

        // Menucell - Header
        $content.find('.webgis-result-table-header.webgis-result-table-menucell .menubutton.checkbox')
            .click(function (e) {
                e.stopPropagation();
                var featuresCollection = map.queryResultFeatures.features();
                var checkAll = !$(this).removeClass('semi-checked').hasClass('checked');

                for (var f in featuresCollection.features) {
                    var feature = featuresCollection.features[f];
                    feature._tableChecked = checkAll;
                    var $rowCheck = $content.find("tr[data-id='" + feature.oid + "'] .webgis-result-table-menucell .menubutton.checkbox");
                    if (checkAll) {
                        $rowCheck.addClass('checked');
                    } else {
                        $rowCheck.removeClass('checked');
                    }
                }

                refreshHeaderMenuButtons();
            });

        $content.find('.webgis-result-table-header.webgis-result-table-menucell .menubutton.zoom')
            .click(function (e) {
                e.stopPropagation();
                var featuresCollection = map.queryResultFeatures.features();
                if (featuresCollection && featuresCollection.bounds) {
                    map.zoomTo(featuresCollection.bounds);
                }
            });

        sortedCols = sortedCols || [];
        $content.find('.webgis-result-table').data('sortedColumns', sortedCols);

        $content.find('.webgis-result-table-header').each(function (i, col) {
            if ($(col).hasClass('webgis-result-table-menucell'))
                return;

            var html = $(col).html();
            if (!html)
                return;

            if ($.inArray(html, sortedCols) >= 0)
                $(col).addClass('sorted asc');
            else if ($.inArray("-" + html, sortedCols) >= 0)
                $(col).addClass('sorted desc');

            $(col)
                .attr('data-index', i)
                .click(function (e) {
                    e.stopPropagation();

                    var $this = $(this);
                    var $table = $this.closest('table');
                    var cols = $table.data('sortedColumns') || [];

                    var colName = $this.html();
                    var arrayIndex = $.inArray(colName, cols);
                    if (arrayIndex >= 0) {
                        cols[arrayIndex] = "-" + cols[arrayIndex];
                    } else {
                        arrayIndex = $.inArray("-" + colName, cols);
                        if (arrayIndex >= 0) {
                            cols = $.grep(cols, function (v) {  // remove from array
                                return v !== "-" + colName;
                            });
                        } else {
                            cols.push(colName);
                        }
                    }

                    //console.log(cols);
                    webgis.delayed(function (map) {
                        map.queryResultFeatures._queryResultFeatures._sortedCols = cols;
                        map.queryResultFeatures.showTable(/*map.queryResultFeatures._queryResultFeatures*/);
                    }, 1, map);
                });
        });

        if (scrollTo) {
            var $row = $content.find(".webgis-result[data-id='" + scrollTo + "']")
                .addClass($.fn.webgis_queryResultsTable.selectedRowClass);

            webgis.scrollTo($content, $row);
        }

        if (map.queryResultFeatures.isSelected()) {
            if ($selectButton) {
                $selectButton.trigger('click');
            }
        }

        if (map.queryResultFeatures.doShowMarkers() === false) {
            if ($markerButton) {
                $markerButton.trigger('click');
            }
        }

        if (features.metadata && features.metadata.warnings && features.metadata.warnings.length > 0) {
            var $warningsPanel = $("<div>")
                .addClass('webgis-result-warning-panel')
                .appendTo($content);

            for (var w in features.metadata.warnings) {
                $("<p>")
                    .addClass('webgis-message')
                    .text(features.metadata.warnings[w])
                    .appendTo($warningsPanel);
            }

            $("<button>")
                .data('features', features)
                .addClass('webgis-button')
                .text(webgis.l10n.get('got-it'))
                .appendTo($warningsPanel)
                .click(function (e) {
                    e.stopPropagation(e);

                    var $this = $(this), features = $this.data('features');
                    delete features.metadata.warnings;
                    $this.closest('.webgis-result-warning-panel').remove();
                });
        }
    };
    this.showFeatureTable = function (feature, queryToolId, $target, featureMetadata) {
        var me = this;

        if (typeof feature === 'string' || feature instanceof String) {
            var oid = feature;
            feature = null;

            var queryResultFeatures = queryToolId ? this._toolQueryResultFeatuers[queryToolId] : this._queryResultFeatures;

            if (queryResultFeatures && queryResultFeatures.features) {
                for (var f in queryResultFeatures.features) {
                    if (queryResultFeatures.features[f].oid === oid) {
                        feature = queryResultFeatures.features[f];
                        break;
                    }
                }
            }
        }
        if (!feature)
            return;

        //console.log('feature', feature);
        if (feature.oid.indexOf('#service:#default:') == 0 && feature.properties && feature.properties._details) {
            // Quicksearch result with details
            webgis.showFeatureResultDetails(this._map.guid, feature.properties._details, function (success, detailedFeature, title, metadata) {
                if (success == false) {
                    delete feature.properties._details;
                }
                me.showFeatureTable(detailedFeature || feature, queryToolId, $target, metadata);
            });
            return;
        }

        var $tools = $("<div>")
            .addClass('webgis-ui-optionscontainer contains-lables')
            .css('display', 'inline-block');

        var map = this._map, me = this, isExtentDependent = this.isExtentDependent();

        // Feature from current query?
        // Or may just the details view of multi feature quicksearch?
        var isCurrentQueryFeature = this._map && this._queryResultFeatures && this._queryResultFeatures.features &&
            this._queryResultFeatures.features.indexOf(feature) >= 0;   // IE11 do not implements Array.prototype.incudes()...
        var featureCount = isCurrentQueryFeature ? this._queryResultFeatures.features.length : 0;

        if (webgis.useMobileCurrent() && isCurrentQueryFeature) {  // show button "back to list view"
            $("<div><div class='webgis-ui-imagebutton-text'>Alle Ergebnisse</div></div>")
                .addClass('webgis-ui-imagebutton')
                .css('background-image', 'url(' + webgis.css.imgResource('enter.png', '') + ')')
                .appendTo($tools)
                .click(function () {
                    map.ui.webgisContainer().find('.webgis-dockpanel.webgis-result').webgis_dockPanel('remove');
                    let tab = map.ui.webgisContainer().find('#tab-queryresults');  // todo: may a better way to do this ;) ... open tab 
                    if (tab.length > 0 & !tab.hasClass('webgis-tabs-tab-selected')) {
                        tab.trigger('click');
                    }
                });
        }

        if (!isExtentDependent) {
            if (isCurrentQueryFeature && featureCount > 1) {   

                var featureIndex = this._queryResultFeatures.features.indexOf(feature);

                $("<div><div class='webgis-ui-imagebutton-text'>Vorheriges Objekt</div></div>")
                    .addClass('webgis-ui-imagebutton')
                    .addClass(featureIndex > 0 ? '' : 'inactive')
                    .attr('data-id', feature.oid)
                    .css('background-image', 'url(' + webgis.css.imgResource('prev-26.png', 'tools') + ')')
                    .appendTo($tools)
                    .click(function () {
                        if (!$(this).hasClass('inactive')) {
                            $('.webgis-geojuhu-results')
                                .children(".webgis-result[data-id='" + $(this).attr('data-id') + "']").prev().trigger('click');
                        }
                    });
                $("<div><div class='webgis-ui-imagebutton-text'>Nächstes Objekt</div></div>")
                    .addClass('webgis-ui-imagebutton')
                    .addClass(featureIndex < this._queryResultFeatures.features.length - 1 > 0 ? '' : 'inactive')
                    .attr('data-id', feature.oid)
                    .css('background-image', 'url(' + webgis.css.imgResource('next-26.png', 'tools') + ')')
                    .appendTo($tools)
                    .click(function () {
                        if (!$(this).hasClass('inactive')) {
                            $('.webgis-geojuhu-results')
                                .children(".webgis-result[data-id='" + $(this).attr('data-id') + "']").next().trigger('click');
                        }
                    });
            }


            if (feature.bounds) {
                $("<div><div class='webgis-ui-imagebutton-text'>Zoom auf Objekt</div></div>")
                    .addClass('webgis-ui-imagebutton')
                    .css('background-image', 'url(' + webgis.css.imgResource('zoomin.png', 'tools') + ')')
                    .appendTo($tools)
                    .click(function () {
                        map.zoomToBoundsOrScale(feature.bounds, webgis.featureZoomMinScale(feature));
                    });
            }
            //console.log('isCurrentQueryFeature', isCurrentQueryFeature, queryToolId, feature);
            if ((isCurrentQueryFeature || queryToolId) && feature.oid) {
                $("<div><div class='webgis-ui-imagebutton-text'>Ergebnis entfernen</div></div>")
                    .addClass('webgis-ui-imagebutton')
                    .data('feature-oid', feature.oid)
                    .css('background-image', 'url(' + webgis.css.imgResource('remove.png', 'tools') + ')')
                    .appendTo($tools)
                    .click(function () {
                        //console.log(queryToolId);
                        if (!queryToolId) {
                            if (me._queryResultFeatures &&
                                me._queryResultFeatures.features &&
                                me._queryResultFeatures.features.length === 1) {
                                $(map._webgisContainer).find('.webgis-toolbox-tool-item.remove-queryresults').trigger('click');
                            } else {
                                map.queryResultFeatures.removeFeature($(this).data('feature-oid'));
                            }
                        } else {
                            var args = [];
                            args["feature-oid"] = $(this).data('feature-oid');

                            var queryTool = map.getTool(queryToolId),
                                uiElement = queryTool && map.isActiveTool(queryToolId) ?
                                    queryTool.uiElement :
                                    $("<div>").addClass('webgis-temp-tool-ui-element remove-after-build').appendTo('body');   // tempräres Element => nur um event. persistent toolparameter zu setzen (zB Koordinatentabelle)

                            me.removeToolFeature(queryToolId, $(this).data('feature-oid'));
                            webgis.tools.onButtonClick(map, { command: 'remove-feature', type: 'servertoolcommand_ext', id: queryToolId, map: map, originalUiElement: uiElement }, this, null, args);
                        }
                    });
            }
        }

        if (webgis.usability.useCompassTool && feature.geometry.coordinates && feature.geometry.coordinates.length >= 2) {
            $("<div><div class='webgis-ui-imagebutton-text'>Kompass</div></div>")
                .addClass('webgis-ui-imagebutton')
                .css('background-image', 'url(' + webgis.css.imgResource('compass-26.png', 'tools') + ')')
                .data('feature', feature)
                .appendTo($tools)
                .click(function () {
                    webgis.require('nav-compass', function () {
                        var targetPos = { lat: feature.geometry.coordinates[1], lng: feature.geometry.coordinates[0] };
                        var label = feature.properties._fulltext;
                        var compass;

                        webgis.modalDialog('Kompass',
                            function ($content) {
                                compass = new navCompass($content.get(0));
                                compass.start(targetPos, label);

                                compass.onPositionChanged = function (position, compassDirection, pointDirection) {
                                    //console.log('new position', position);
                                    //console.log('current compass direction', compassDirection);
                                    //console.log('current direction to point', pointDirection);
                                };
                            }, function () {
                                if (compass) {
                                    compass.stop();
                                    compass = null;
                                }
                            });
                    });
                });
        }

        if ($.inArray(queryToolId, ["webgis.tools.coordinates", "webgis.tools.rasteridentify"]) < 0 && webgis.featureHasTableProperties(feature) && isCurrentQueryFeature) {
            if (!webgis.useMobileCurrent()) {   // show Button Table
                $("<div><div class='webgis-ui-imagebutton-text'>"+webgis.l10n.get("button-table")+"</div></div>")
                    .addClass('webgis-ui-imagebutton')
                    .attr('data-id', feature.oid)
                    .css('background-image', 'url(' + webgis.css.imgResource('results.png', 'tools') + ')')
                    .appendTo($tools)
                    .click(function () {
                        map.queryResultFeatures.showTable(null, $(this).attr('data-id'));
                    });
            }
        }

        if (isCurrentQueryFeature &&
            feature.oid && feature.oid.indexOf('#') !== 0 && feature.oid.indexOf(":") > 0) {  // Suchergebnisse aus der Schnellsuche (ohne Original) haben als Id: #service:#default:1, Ergebnisse aus XY, Höhenkote haben Ids ohne ":"

            var selectable = (isExtentDependent == false);

            var sq = feature.oid.split(':');
            var service = this._map.getService(sq[0]);
            if (service != null) {
                selectable = service.isQuerySelectable(sq[1]);
            }
            if (selectable) {
                $("<div><div class='webgis-ui-imagebutton-text'>Objekt hervorheben</div></div>")
                    .addClass('webgis-ui-imagebutton webgis-toggle-button')
                    .addClass('webgis-dependencies webgis-dependency-ishighlighted')
                    .attr('data-id', feature.oid)
                    .css('background-image', 'url(' + webgis.css.imgResource('highlight_features.png', 'tools') + ')')
                    .appendTo($tools)
                    .click(function () {
                        $('.webgis-geojuhu-results')
                            .children(".webgis-result[data-id='" + $(this).attr('data-id') + "']")
                            .addClass('webgis-result-suppresszoom')
                            .trigger('click')
                            .removeClass('webgis-result-suppresszoom');

                        map.ui.refreshUIElements();
                    });

                $("<div><div class='webgis-ui-imagebutton-text'>"+webgis.l10n.get("button-select")+"</div></div>")
                    .addClass('webgis-ui-imagebutton webgis-toggle-button')
                    .addClass('webgis-dependencies webgis-dependency-hasselection')
                    .css('background-image', 'url(' + webgis.css.imgResource('select_features.png', 'tools') + ')')
                    .appendTo($tools)
                    .click(function () {
                        $('.webgis-selection-button').trigger('click');
                    });
            }
        }

        if (this._map && this._queryResultFeatures &&
            this._queryResultFeatures.features &&
            this._queryResultFeatures.features.indexOf(feature) >= 0) {
            //console.log('features', this._queryResultFeatures);
            var featureOid = feature.oid, filterToolId = 'webgis.tools.presentation.visfilter';
            if (this._queryResultFeatures.metadata && this._queryResultFeatures.metadata.applicable_filters && map.getTool(filterToolId)) {

                $.each(this._queryResultFeatures.metadata.applicable_filters.split(','),
                    function (i, filterId) {
                        if (filterId.indexOf('~') > 0) {
                            var service = map.getService(filterId.split('~')[0]);
                            if (service) {
                                var filter = service.getFilter(filterId.split('~')[1]);
                                if (filter) {
                                    $("<div><div class='webgis-ui-imagebutton-text'>" + filter.name + "</div></div>")
                                        .addClass('webgis-ui-imagebutton')
                                        .attr('title', filter.name)
                                        .data('map', this._map)
                                        .data('oid', feature.oid)
                                        .data('filterid', filterId)
                                        .css('background-image', 'url(' + webgis.css.imgResource('filter-26.png', 'tools') + ')')
                                        .appendTo($tools)
                                        .click(function () {
                                            var args = [];
                                            args["feature_oid"] = featureOid;
                                            args["filter_id"] = $(this).data('filterid');
                                            webgis.tools.onButtonClick(map, { command: 'setfeaturefilter', type: 'servertoolcommand_ext', id: filterToolId, map: map }, this, null, args);
                                        });
                                }
                            }
                        }

                    });

                $("<div><div class='webgis-ui-imagebutton-text'>Alle entfernen</div></div>")
                    .addClass('webgis-ui-imagebutton')
                    .addClass('webgis-dependencies webgis-dependency-hasfilters')
                    .attr('data-applicable-filters', this._queryResultFeatures.metadata.applicable_filters)
                    .data('map', this._map)
                    .data('oid', feature.oid)
                    .css('background-image', 'url(' + webgis.css.imgResource('filter-remove-26.png', 'tools') + ')')
                    .appendTo($tools)
                    .click(function () {
                        var args = [];
                        args["feature_oid"] = featureOid;
                        webgis.tools.onButtonClick(map, { command: 'unsetfeaturefilter', type: 'servertoolcommand_ext', id: filterToolId, map: map }, this, null, args);
                    });
            }

            if (feature.__queryId && feature.__serviceId) {
                var editthemes = this._map.getEditThemes(feature.__serviceId, feature.__queryId);

                if (editthemes) {
                    var editToolId = 'webgis.tools.editing.updatefeature';
                    for (var e in editthemes) {
                        var edittheme = editthemes[e];

                        $("<div><div class='webgis-ui-imagebutton-text'>Bearbeiten: " + edittheme.name + "</div></div>")
                            .addClass('webgis-ui-imagebutton')
                            .data('edittheme', edittheme)
                            .data('map', this._map)
                            .data('oid', feature.oid)
                            .css('background-image', 'url(' + webgis.css.imgResource('edit-attributes-26.png', 'tools') + ')')
                            .appendTo($tools)
                            .click(function () {
                                var args = [];
                                args["feature-oid"] = $(this).data('oid')
                                args["edittheme"] = $(this).data('edittheme').themeid;
                                args["edit-map-scale"] = $(this).data('map').scale();
                                args["edit-map-crsid"] = $(this).data('map').calcCrs().id;

                                webgis.tools.onButtonClick(map, { command: 'editattributes', type: 'servertoolcommand_ext', id: editToolId, map: map }, this, null, args);
                            });
                    }
                }
            }
        }

        var buttonArray = [];
        var content = this._map.ui.featureResultTable(feature, null, false, true, buttonArray,
                      (featureMetadata || {}).table_fields || this.tableFields());

        
        if (webgis.isSuspiciousHtml(content)) {
            content = '<div>suppressing dangerous result...</div>';
        }

        for (var b in buttonArray) {
            var button = buttonArray[b];

            var $button = $(button.html)
                .addClass('webgis-ui-imagebutton')
                .css('background-image', 'url(' + button.img + ')')
                .appendTo($tools);
            $("<div>")
                .addClass('webgis-ui-imagebutton-text')
                .text(button.text)
                .appendTo($button);
        }

        if (webgis.usability.useMarkerPopup === true) {
            $('body').webgis_modal({
                title: (feature.properties && feature.properties._title ? feature.properties._title : ''),
                onload: function ($content) {
                    $content.css('padding', '10px');
                    $content.html(content);
                },
                width: '90%', height: '90%'
            });
        } else {
            if ($target && $target.length > 0) {
                if ($tools.children().length > 0) {
                    $tools.appendTo($target);
                    map.ui.refreshUIElements();
                }
                $(content).appendTo($target);
            } else { // default: show in dockPanel
                var map = this._map;
                this._map.ui.webgisContainer().webgis_dockPanel({
                    title: (feature.properties && feature.properties._title ? feature.properties._title : ''),
                    titleImg: this.markerImg(feature._fIndex, queryToolId) ? this.markerImg(feature._fIndex, queryToolId, true).url : null,
                    dock: 'bottom',
                    onload: function ($content, $dockpanel) {
                        $dockpanel.addClass('webgis-result')
                            .removeClass('webgis-result-table')
                            .attr('data-id', feature.oid);  // damit wird der Dialog automatisch geschlossen, wenn Marker mit "x" aus Ergebnissen entfernt wird
                        $content.css('padding', '10px');

                        if ($tools.children().length > 0) {
                            $tools.appendTo($content);
                            map.ui.refreshUIElements();
                        }

                        $(content).appendTo($content);

                        //var contentHeight = $content.outerHeight();
                        //var tableHeight = $content.children('table').height();
                        //if (tableHeight < contentHeight) {
                        //    $dockpanel.webgis_dockPanel('resize', { size: $dockpanel.height() - (contentHeight - tableHeight) + 22 });
                        //};
                    },
                    onclose: function ($panel) {
                        map.queryResultFeatures.tryUnhighlightMarkers();
                    },
                    size: '0px',
                    maxSize: '40%',
                    autoResize: true,
                    autoResizeBoth: !webgis.useMobileCurrent(),
                    refElement: this._map.ui.mapContainer(),
                    map: this._map
                });
            }
        }
        //if (feature.oid) {
        //    this.tryHighlightMarker(feature.oid, true);
        //}
    };
    this.count = function (features) {
        features = features ||
            (this._queryResultFeatures ? this._queryResultFeatures.features : null);

        if (!features)
            return 0;

        return features.length;
    };
    this.first = function (features) {
        features = features ||
            (this._queryResultFeatures ? this._queryResultFeatures.features : null);

        if (!features || !features.length === 0)
            return null;

        return features[0];
    };
    this.firstService = function (features) {
        var first = this.first(features);
        if (first == null)
            return null;
        return this._map.getService(first.oid.split(':')[0]);
    };
    this.firstQuery = function (features) {
        var first = this.first(features), service = this.firstService(features);

        if (first == null || service == null)
            return null;

        return service.getQuery(first.oid.split(':')[1]);
    };
    this.featureIds = function (objectIdOnly) {
        var ret = [];
        if (this.count() != 0) {
            for (var f in this._queryResultFeatures.features) {
                var oid = this._queryResultFeatures.features[f].oid;
                if (!oid)
                    continue;
                ret.push(objectIdOnly ? oid.split(':')[2] : oid);
            }
        }
        return ret;
    };
    this.markerImg = function (index, queryToolId, highlight) {
        index = parseInt(index);
        var feature;

        var queryResultFeatures = queryToolId ? this._toolQueryResultFeatuers[queryToolId] : this._queryResultFeatures;

        $.each(queryResultFeatures.features, function (i, f) {
            if (f._fIndex === index)
                feature = f;
        });
        //feature = this._queryResultFeatures.features[index];
        if (!feature) {
            return { url: '', width: 0, height: 0 };
        }

        var markerIcon;
        var dynamicName = this.isDynamicContent() ? this._map.getCurrentDynamicContentName() : null;

        if (this.isExtentDependent() === true) {
            markerIcon = webgis.markerIcons["dynamic_content_extenddependent"][dynamicName] || webgis.markerIcons["dynamic_content_extenddependent"]["default"];
        }
        else if (this.isDynamicContent() === true) {
            markerIcon = webgis.markerIcons["dynamic_content"][dynamicName] || webgis.markerIcons["dynamic_content"]["default"];
        }
        else {
            markerIcon = webgis.markerIcons["query_result"]["default"];
            if (feature.oid && feature.oid.indexOf(':') >= 0) {
                var p = feature.oid.split(':');
                if (webgis.markerIcons["query_result"][p[0] + ":" + p[1]]) {
                    markerIcon = webgis.markerIcons["query_result"][p[0] + ":" + p[1]];
                }
                else if (webgis.markerIcons["query_result"][p[1]]) {
                    markerIcon = webgis.markerIcons["query_result"][p[1]];
                }
            }
        }

        if (queryToolId) {
            if (webgis.markerIcons["query_result"][queryToolId]) {
                markerIcon = webgis.markerIcons["query_result"][queryToolId];
            }
        }

        let fIndex = typeof feature._fIndex === "undefined" ? index : parseInt(feature._fIndex);
        let className = markerIcon.className ? markerIcon.className(fIndex, feature) : null;
        let bgPos = markerIcon.bgPos ? markerIcon.pgPos(fIndex, feature) : null;

        return {
            url: highlight === true ? webgis.highlightMarkerUrl(markerIcon.url(fIndex, feature), true) : markerIcon.url(fIndex, feature),
            width: markerIcon.size(fIndex, feature)[0],
            height: markerIcon.size(fIndex, feature)[1],
            className: className,
            bgPos: bgPos
        };
    };
    this.isSelected = function () {
        return this._queryResultFeatures && this._queryResultFeatures.metadata && this._queryResultFeatures.metadata.selected;
    };
    this.setSelected = function (selected) {
        if (!this._queryResultFeatures.metadata)
            this._queryResultFeatures.metadata = {};
        this._queryResultFeatures.metadata.selected = selected;
    };
    this.setSelection = function (select, $senderButton) {
        var map = this._map;

        map.queryResultFeatures.setSelected(select);
        if (select) {
            var editShortCutDisplayStyle = 'none';
            if (map.queryResultFeatures.count() > 0) {
                var service = map.queryResultFeatures.firstService();
                if (service != null) {
                    var query = map.queryResultFeatures.firstQuery();
                    if (query != null) {
                        var selection = map.getSelection('selection');
                        if (selection) {
                            selection.setTargetQuery(service, query.id, map.queryResultFeatures.featureIds(true).toString());
                        }
                        if (service.isEditLayer(query.layerid)) {
                            editShortCutDisplayStyle = '';
                        }
                        else if (query.associatedlayers) {
                            for (var a in query.associatedlayers) {
                                if (service.isEditLayer(query.associatedlayers[a].id))
                                    editShortCutDisplayStyle = '';
                            }
                        }
                    }
                }
            }
            if ($senderButton) {
                $senderButton.parent().find('.webgis-selection-edittool-shortcut').css('display', editShortCutDisplayStyle);
            }
        }
        else {
            map.getSelection('selection').remove();
            if ($senderButton) {
                $senderButton.parent().find('.webgis-selection-edittool-shortcut').css('display', 'none');
            }
        }

        map.ui.refreshUIElements();
    };

    this.markersVisible = function () {
        if (this._queryResultLayer == null) {
            return false;
        }

        return this._map.frameworkElement.hasLayer(this._queryResultLayer);
    };
    this.doShowMarkers = function () {
        return this._queryResultFeatures && this._queryResultFeatures.metadata && this._queryResultFeatures.metadata.showMarkers;
    };
    this.setDoShowMarkers = function (show) {
        if (!this._queryResultFeatures.metadata)
            this._queryResultFeatures.metadata = {};
        this._queryResultFeatures.metadata.showMarkers = show;
    };
    this.setMarkerVisibility = function (show, $markerButton) {
        if (this._queryResultLayer == null) {
            return;
        }

        this._map.queryResultFeatures.setDoShowMarkers(show);
        if (show && !this.markersVisible()) {
            this._queryResultLayer.addTo(this._map.frameworkElement);
        } else if (!show && this.markersVisible()) {
            this.recluster();
            this._map.frameworkElement.removeLayer(this._queryResultLayer);
        }
    };

    this.reorderAble = function (features) {
        features = features || this._queryResultFeatures;

        return features && features.metadata && features.metadata.reorderAble;
    };
    this.connected = function (features) {
        features = features || this._queryResultFeatures;

        return features && features.metadata && features.metadata.connect;
    }

    this.hasLinks = function () {
        return this._queryResultFeatures &&
            this._queryResultFeatures.metadata &&
            this._queryResultFeatures.metadata.links;
    };
    this.getLinks = function () {
        if (this.hasLinks())
            return this._queryResultFeatures.metadata.links;

        return null;
    };
    this.setLinks = function (links) {
        if (this._queryResultFeatures && this._queryResultFeatures.metadata) {
            this._queryResultFeatures.metadata.links = links;
            //console.log('links-reset:', this._queryResultFeatures.metadata.links);
        }
    }

    // Serialization
    this.serialize = function (features) {
        features = features || this._queryResultFeatures;
        if (!features)
            return null;

        var oids = [],
            hashcodes = [],
            metadata = {},
            oidPrefix = null;

        if (features && features.metadata) {
            metadata.connect = features.metadata.connect || null;
            metadata.tool = features.metadata.tool || null;
            metadata.startPoint = features.metadata.startPoint || null;
            metadata.reorderAble = features.metadata.reorderAble || null;
            metadata.custom_selection = features.metadata.custom_selection || null;
            metadata.selected = features.metadata.selected;
        }

        for (var f in features.features) {
            var feature = features.features[f];

            if (!feature.oid)
                return null;

            var parts = feature.oid.split(':');
            if (parts.length != 3)
                return null;

            if (!oidPrefix) {
                oidPrefix = parts[0] + ':' + parts[1] + ":";
                metadata.service = parts[0];
                metadata.query = parts[1];
            } else if (feature.oid.indexOf(oidPrefix) !== 0) {
                return null;
            }

            oids.push(parts[2]);
            hashcodes.push(webgis.hmac._featureGeoHashCode(feature));
        }


        if (oids.length > 0) {
            var ser = {
                oids: oids,
                hashcodes: hashcodes,
                metadata: metadata,
                tab: features.tab
            };

            return ser;
        }

        return null;
    };

    // RasterIdentify, Coordinates
    this._toolQueryResultFeatuers = [];
    this._toolQueryResultLayer = [];
    this.showToolQueryResults = function (toolid, features) {
        var me = this;

        if (features.method === 'append' && this._toolQueryResultFeatuers[toolid]) {
            for (var f in features.features) {
                this._toolQueryResultFeatuers[toolid].features.push(features.features[f]);
            }
            features = this._toolQueryResultFeatuers[toolid];

            // remove method after appending!
            features.method = '';
        }

        if (webgis.mapFramework === 'leaflet') {
            if (this._toolQueryResultLayer[toolid] != null)
                this._map.frameworkElement.removeLayer(this._toolQueryResultLayer[toolid]);

            this._toolQueryResultLayer[toolid] = L.geoJson([], {
                style: function (feature) {
                    //return { color: feature.properties.color };
                    return {};
                }
            });
            this._toolQueryResultLayer[toolid].addTo(this._map.frameworkElement);

            for (var f in features.features) {
                var feature = features.features[f];

                var fIndex = 0;
                if (typeof feature._fIndex === "undefined") {
                    if (feature.properties._fIndex) {
                        feature._fIndex = fIndex = parseInt(feature.properties._fIndex);
                    } else {
                        // Wenn auf feature noch kein Index (Markernummer) gespeichert ist, dann den maximalen + 1 nehmen
                        for (var fe in features.features) {
                            if (typeof features.features[fe]._fIndex === "undefined")
                                continue;

                            fIndex = Math.max(features.features[fe]._fIndex + 1, fIndex);
                        }
                        feature._fIndex = fIndex;
                    }
                } else {
                    fIndex = feature._fIndex;
                }
                var marker = webgis.createMarker({
                    lat: feature.geometry.coordinates[1],
                    lng: feature.geometry.coordinates[0],
                    index: fIndex,
                    feature: feature,
                    icon: "query_result",
                    queryToolId: toolid,
                    label: webgis.markerIcons["query_result"][toolid] && webgis.markerIcons["query_result"][toolid].label
                        ? webgis.markerIcons["query_result"][toolid].label(fIndex, feature)
                        : undefined
                });
                if (feature.oid) {
                    marker.oid = feature.oid;
                }
                marker._showFeatureResultsModal = true;

                webgis.bindFeatureMarkerPopup(this._map, marker, feature, toolid);
                this._toolQueryResultLayer[toolid].addLayer(marker);
            }
        }

        this._toolQueryResultFeatuers[toolid] = features;
        if (features.features.length > 0) {
            webgis.showFeatureResultModal(this._map.guid, features.features[features.features.length - 1].oid, toolid);  // show the last result
        } else {
            me._map.events.fire('querymarkersremoved');  // or close all dialogs an stuff
        }
    };
    this.clearToolResults = function (toolid) {
        this._toolQueryResultFeatuers[toolid] = null;
        if (webgis.mapFramework === 'leaflet') {
            if (this._toolQueryResultLayer[toolid] != null) {
                this._map.frameworkElement.removeLayer(this._toolQueryResultLayer[toolid]);
                this._toolQueryResultLayer[toolid] = null;
            }
        }

        //console.log(".webgis-tool-result-" + toolid.replaceAll('.', '-'));
        $(".webgis-tool-result-" + toolid.replaceAll('.', '-')).remove();
        $(".webgis-tool-result-counter-" + toolid.replaceAll('.', '-')).val('0');

        this._map.ui.refreshUIElements();
    };
    this.removeToolFeature = function (toolid, oid) {
        if (this._toolQueryResultFeatuers[toolid]) {
            var features = [];
            for (var f in this._toolQueryResultFeatuers[toolid].features) {
                var feature = this._toolQueryResultFeatuers[toolid].features[f];
                if (feature.oid !== oid)
                    features.push(feature);
            }
            this._toolQueryResultFeatuers[toolid].features = features;

            //console.log('tool-features', toolid, this._toolQueryResultFeatuers[toolid]);

            this.showToolQueryResults(toolid, this._toolQueryResultFeatuers[toolid]);
        }
    };
    this.hasToolResults = function (toolid) {
        return this._toolQueryResultFeatuers[toolid] && this._toolQueryResultFeatuers[toolid].features && this._toolQueryResultFeatuers[toolid].features.length > 0;
    };
    this.firstToolResult = function (toolid) {
        if (this.hasToolResults(toolid)) {
            return this._toolQueryResultFeatuers[toolid].features[0];
        }

        return null;
    };
    this.toolMarkersVisible = function (toolid) {
        if (this._toolQueryResultLayer[toolid] == null) {
            return false;
        }

        return this._map.frameworkElement.hasLayer(this._toolQueryResultLayer[toolid]);
    };
    this.setToolMarkerVisibility = function (toolid, show) {
        var layer = this._toolQueryResultLayer[toolid];
        if (layer == null) return;

        if (show && !this.toolMarkersVisible(toolid)) {
            this._map.frameworkElement.addLayer(layer);
        } else if (!show && this.toolMarkersVisible(toolid)) {
            this._map.frameworkElement.removeLayer(layer);
        }

    };
    this.showToolMarkerLabels = function (toolid, show) {
        var layer = this._toolQueryResultLayer[toolid];
        if (layer == null) return;

        layer.eachLayer(function (marker) {
            console.log('marker',marker);
            if (marker.showLabel) marker.showLabel(show);
        });
    };
    this.queryTool = function () {
        if (this._queryResultFeatures && this._queryResultFeatures.metadata) {
            return this._queryResultFeatures.metadata.tool || '';
        }

        return '';
    }

    var _handlers = function (map) {
        var _map = map;

        this.featureResultClick = function (feature, removeSelection, suppressZoom) {
            if (removeSelection) {
                var selection = _map.getSelection('query');
                selection.remove();

                _map.queryResultFeatures.tryUnhighlightMarkers();
                if ($.fn.webgis_queryResultsTable)
                    $(null).webgis_queryResultsTable('unselectRows', { map: _map });

                _map.events.fire('feature-highlight-removed', _map, feature);
            }
            else {
                if (_map.queryResultFeatures.supportsZoomToFeature() == true &&
                    feature.bounds &&
                    suppressZoom !== true) {
                    if (_map.scale() < webgis.featureZoomMinScale(feature)) {
                        _map.zoomToBoundsOrScale(feature.bounds, webgis.featureZoomMinScale(feature));
                    } else {
                        var mapExtent = map.getExtent();
                        if (feature.bounds[0] < mapExtent[0] ||
                            feature.bounds[1] < mapExtent[1] ||
                            feature.bounds[2] > mapExtent[2] ||
                            feature.bounds[3] > mapExtent[3]) {
                            var center = [(feature.bounds[0] + feature.bounds[2]) * 0.5, (feature.bounds[1] + feature.bounds[3]) * 0.5];
                            _map.setCenter(center, 0.3);
                        }
                    }
                }

                if (feature.oid) {
                    // do this always, even if markers not visible
                    // ensures everything looks right, if user select Marker-Button 
                    _map.queryResultFeatures.recluster();
                    _map.queryResultFeatures.uncluster(feature.oid, true);
                    _map.queryResultFeatures.tryHighlightMarker(feature.oid, true);
                    _map.queryResultFeatures.tryHighlightFeature(feature);

                    // select row if markers not visible
                    // this is trigger otherweise form marker click event in webgis.bindFeatureMarkerPopup();
                    if (!_map.queryResultFeatures.markersVisible()) {
                        var $tabControl = _map.ui.getQueryResultTabControl();
                        if ($tabControl) {
                            var $currentTabContent = $tabControl.webgis_tab_control('currentContent');
                            $currentTabContent.webgis_queryResultsTable('selectRow', { dataId: feature.oid, scrollTo: false });
                        }
                    }
                }
            }
        };
    };

    this.handlers = new _handlers(map);
};