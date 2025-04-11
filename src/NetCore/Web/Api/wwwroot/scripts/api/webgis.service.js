webgis.service = function (map, serviceInfo) {
    "use strict";

    let $ = webgis.$;

    webgis.implementEventController(this);
    this.map = map;
    this.frameworkElement = null;
    this.serviceInfo = serviceInfo;
    this.name = serviceInfo.name;
    this.type = serviceInfo.type;
    this.id = serviceInfo.id;
    this.layers = serviceInfo.layers || [];
    this.presentations = serviceInfo.presentations || [];
    this.queries = serviceInfo.queries || [];
    this.chainagethemes = serviceInfo.chainagethemes || [];
    this.filters = serviceInfo.filters || [];
    this.labeling = serviceInfo.labeling || [];
    this.editthemes = serviceInfo.editthemes || [];
    this.snapping = serviceInfo.snapping || [];
    this.opacity = serviceInfo.opacity || 1.0;
    this.isBasemap = serviceInfo.isbasemap;
    this.showInToc = serviceInfo.show_in_toc;
    this.basemapType = serviceInfo.properties ? serviceInfo.properties.basemap_type : 'normal';
    this.copyrightId = serviceInfo.copyright;
    this.servicedescription = serviceInfo.servicedescription;
    this.copyrighttext = serviceInfo.copyrighttext;
    this.guid = webgis.guid();

    let _order = 0;
    let _isInEditMode = false, _isTopMost = false;

    this.map.events.on('requestidchanged', function (channel, sender) {
        this.events.fire('requestidchanged', this);
    }, this);

    for (let l in this.layers) {
        this.layers[l].service = this;
        if (serviceInfo.type === 'collection') {
            this.layers[l].setChildVis = function () {
                let map = this.service.map;
                let p = this.id.split(':');
                let childService = map.services[p[0]];
                //alert(childService + " " + p[1]);
                if (childService)
                    childService.setLayerVisibility([p[1]], this.visible);
            };
        }
    }
    for (let p in this.presentations)
        this.presentations[p].service = this;
    for (let q in this.queries) {
        this.queries[q].visible = this.map.isQueryVisible(this.id, this.queries[q].id);
    }

    this.isDestroyed = function () {
        return map.isDestroyed();
    };

    this.setFrameworkElement = function (felem) {
        this.frameworkElement = felem;
        if (felem && felem.events) {
            felem.events.on('beginredraw', function (e) {
                this.events.fire('beginredraw', this);
            }, this);
            felem.events.on('endredraw', function (e) {
                this.events.fire('endredraw', this);
            }, this);
        }

        if (this.serviceInfo.insertOrder) {
            this.setOrder(this.serviceInfo.insertOrder);
        }
        this.setOpacity(this.getOpacity());

        if (this.serviceInfo.type === 'tile' && this.isBasemap === false) {
            if (this.layers.length > 0 && this.layers[0]) {
                this._setTileCacheVisibility(this.layers[0].visible);
            }
        }

        if (this.serviceInfo.type === 'static_overlay') {
            if (this.serviceInfo.affinePoints) {
                this.setAffinePoints(this.serviceInfo.affinePoints);
                this.serviceInfo.affinePoints = null;
            }
            if (this.serviceInfo.passPoints) {
                this.setPassPoints(this.serviceInfo.passPoints);
                this.serviceInfo.passPoints = null;
            }
        }
    };

    this.serialize = function (options) {
        let id = this.id;

        //console.log('service-serialize', options, this.type, this.serviceInfo)

        if ((this.type === 'custom' || this.type === 'vtc') 
            && options
            && options.applyFallback
            && this.serviceInfo && this.serviceInfo.fallback) {
            id = this.serviceInfo.fallback;
        }
    
        let o = {
            id: id,
            opacity: this.getOpacity(),
            order: this.getOrder(),
            layers: [],
            queries: []
        };
        for (let l in this.layers) {
            o.layers.push({ id: this.layers[l].id, name: this.layers[l].name, visible: this.layers[l].visible });
        }
        for (let q in this.queries) {
            o.queries.push({ id: this.queries[q].id, visible: this.queries[q].visible });
        }

        if (this.serviceInfo && this.serviceInfo.custom_request_parameters) {
            o.custom_request_parameters = serviceInfo.custom_request_parameters;
        }

        return o;
    };

    this._setServiceVisibility = function (layerids, eventName, fireLiveShareEvent) {
        if (!layerids)
            layerids = [];

        //console.log('setServiceVisibility', this.id, layerids);

        for (let l = 0; l < this.layers.length; l++) {
            let layer = this.layers[l];
            layer.visible = $.inArray(layer.id, layerids) >= 0 || $.inArray("*", layerids) >= 0;
            if (layer.setChildVis)
                layer.setChildVis.apply(layer);
        }

        if (this.layers.length === 1 && this.layers[0]) {
            this._setTileCacheVisibility(this.layers[0].visible);
        }

        this.incRequestId();
        this.events.fire(eventName, this);

        if (fireLiveShareEvent !== false) {
            this.events.fire('onchangevisibility_liveshare', this);
        }
    }

    this.setServiceVisibility = function (layerids, fireLiveShareEvent) {
        this._setServiceVisibility(layerids, 'onchangevisibility', fireLiveShareEvent);
    };

    this.setServiceVisibilityDelayed = function (layerids, fireLiveShareEvent) {
        this._setServiceVisibility(layerids, 'onchangevisibility-delayed', fireLiveShareEvent);
    };

    this.getLayerVisibility = function () {
        let layerIds = [];
        if (this.layers) {
            for (let l = 0; l < this.layers.length; l++) {
                let layer = this.layers[l];
                if (layer.visible === true) {
                    layerIds.push(layer.id);
                }
            }
        }

        return layerIds;
    };

    this.getLayerVisibilityPro = function () {
        let layerIds = [];
        if (this.layers) {
            for (let l = 0; l < this.layers.length; l++) {
                let layer = this.layers[l];
                if (layer.visible === true) {
                    layerIds.push({ name: layer.name, id: layer.id });
                }
            }
        }

        return layerIds;
    };

    this.setServiceVisibilityPro = function (layerDefs, fireEvent, fireLiveshareEvent) {
        if (layerDefs == null)
            return;

        //console.log('setServiceVisibilityPro', layerDefs);

        for (let lDef in layerDefs) {
            let layerDef = layerDefs[lDef];
            let layer = this.getLayerFromName(layerDef.name) || this.getLayer(layerDef.id);
            if (!layer)
                continue;
            layer.visible = layerDef.visible === true;

            this._setTileCacheVisibility(layer.visible);

            if (layer.setChildVis)
                layer.setChildVis.apply(layer);
        }
        if (fireEvent !== false) {
            this.incRequestId();
            this.events.fire('onchangevisibility', this);
            if (fireLiveshareEvent !== false) {
                this.events.fire('onchangevisibility_liveshare', this);
            }
        }
    };

    this.setServiceVisibilityPro2 = function (layerDefs, fireEvent, fireLiveshareEvent) {
        if (layerDefs == null)
            return;

        //console.log('setServiceVisibilityPro2', layerDefs);

        if (!this.isBasemap)
            this.setServiceVisibility();  // alle aus

        for (let lDef in layerDefs) {
            let layerDef = layerDefs[lDef];
            let layer = this.getLayerFromName(layerDef.name) || this.getLayer(layerDef.id);
            if (!layer)
                continue;
            layer.visible = true;

            this._setTileCacheVisibility(layer.visible);
            if (this.isBasemap) {
                map.setBasemap(this.serviceInfo.id, this.serviceInfo.properties && this.serviceInfo.properties.basemap_type === 'overlay');
            }

            if (layer.setChildVis)
                layer.setChildVis.apply(layer);
        }
        if (fireEvent !== false) {
            this.incRequestId();
            this.events.fire('onchangevisibility', this);
            if (fireLiveshareEvent !== false) {
                this.events.fire('onchangevisibility_liveshare', this);
            }
        }
    };

    this._setLayerVisibility = function (layerids, visible, eventName, fireLiveshareEvent) {
        if (!layerids)
            layerids = [];

        //console.log('setLayerVisibility', layerids, visible, this.serviceInfo);

        for (let l = 0; l < this.layers.length; l++) {
            let layer = this.layers[l];
            if ($.inArray(layer.id, layerids) >= 0 || $.inArray("*", layerids) >= 0) {
                layer.visible = visible;
                if (layer.setChildVis)
                    layer.setChildVis.apply(layer);
            }
        }
        this.incRequestId();
        this.events.fire(eventName, this);
        if (fireLiveshareEvent !== false) {
            this.events.fire('onchangevisibility_liveshare', this);
        }

        this._setTileCacheVisibility(visible);
    };

    this.setLayerVisibility = function (layerids, visible, fireLiveshareEvent) {
        this._setLayerVisibility(layerids, visible, 'onchangevisibility', fireLiveshareEvent);
    };

    this.setLayerVisibilityDelayed = function (layerids, visible, fireLiveshareEvent) {
        this._setLayerVisibility(layerids, visible, 'onchangevisibility-delayed', fireLiveshareEvent);
    };

    this.checkLayerVisibility = function (layerids) {
        if (!layerids)
            return 0;
        let counter = 0;
        for (let l = 0; l < this.layers.length; l++) {
            if (($.inArray(this.layers[l].id, layerids) >= 0 || $.inArray("*", layerids) >= 0) && this.layers[l].visible === true)
                counter++;
        }
        if (counter === 0)
            return 0; // no layer matched
        if (counter < layerids.length)
            return -1; // some layers matched
        return 1; // all matched
    };

    this._setTileCacheVisibility = function (visible) {
        //
        // Falls Dienst ein Tilecache aber keine Basemap ist => hier hinzufügen/entfernen
        //
        if (this.serviceInfo && this.serviceInfo.type === 'tile' && this.isBasemap === false &&
            this.frameworkElement &&
            this.map && this.map.frameworkElement) {
            //console.log(this.frameworkElement);
            if (visible === true) {
                //console.log('add tile layer: '+this.name);
                this.map.frameworkElement.addLayer(this.frameworkElement);
            } else {
                //console.log('remove tile layer: ' + this.name);
                this.map.frameworkElement.removeLayer(this.frameworkElement);
            }
        };
    }

    this.getLayerIdsFromNames = function (layernames) {
        let ids = [];
        if (layernames) {
            for (let l = 0; l < this.layers.length; l++) {
                if ($.inArray(this.layers[l].name, layernames) >= 0)
                    ids.push(this.layers[l].id);
            }
        }
        return ids;
    };
    this.getLayer = function (id) {
        for (let l in this.layers) {
            if (this.layers[l].id === id)
                return this.layers[l];
        }
        return null;
    };
    this.getLayers = function (ids) {
        let layers = [];

        if (ids && ids.length > 0) {
            for (let l in this.layers) {
                if ($.inArray(this.layers[l].id, ids) >= 0) {
                    layers.push(this.layers[l]);
                }
            }
        }

        return layers;
    };
    this.getLayerIds = function () {
        let ids = [];
        for (let l in this.layers) {
            ids.push(this.layers[l].id);
        }
        return ids;
    };
    this.getLayerFromName = function (name) {
        for (let l in this.layers) {
            if (this.layers[l].name === name)
                return this.layers[l];
        }
        return null;
    };
    this.layerInScale = function (id, scale) {
        let layer = this.getLayer(id);
        //if (layer == null)
        //    layer = this.getLayerFromName(id);
        if (layer == null)
            return false;

        scale = scale || this.map.scale();

        //console.log('layerInScale', scale, layer.name, layer.minscale, layer.maxscale);

        if (layer.minscale > 1 && scale < layer.minscale)
            return false;
        if (layer.maxscale > 1 && scale > layer.maxscale)
            return false;
        return true;
    };
    this.layerVisibleAndInScale = function (id, scale) {
        let layer = this.getLayer(id);
        //if (layer == null)
        //    layer = this.getLayerFromName(id);
        if (layer === null || layer.visible === false)
            return false;

        scale = scale || this.map.scale();

        //console.log('layerVisibleAndInScale', scale, layer.name, layer.minscale, layer.maxscale);

        if (layer.minscale > 1 && scale < layer.minscale)
            return false;
        if (layer.maxscale > 1 && scale > layer.maxscale)
            return false;

        return true;
    };
    this.hasVisibleLayersInScale = function () {
        let scale = this.map.scale();
        for (let l in this.layers) {
            if (this.layerVisibleAndInScale(this.layers[l].id, scale)) {
                return true;
            }
        }
        return false;
    };
    this.hasVisibleLayers = function () {
        for (let l in this.layers) {
            if (this.layers[l] && this.layers[l].visible === true) {
                return true;
            }
        }
        return false;
    };
    this.canLegend = function () {
        if (this.serviceInfo.type === 'tile') {
            return this.serviceInfo.has_legend === true;
        }
        return true;
    };
    this.hasLegend = function () {
        if (this.serviceInfo.has_legend === false)
            return false;

        for (let l in this.layers) {
            if (this.layers[l].legend === true)
                return true;
        }

        return false;
    };
    this.hasVisibleLegendInScale = function () {
        let scale = this.map.scale();
        for (let l in this.layers) {
            if (this.layers[l].legend === true && this.layerVisibleAndInScale(this.layers[l].id, scale))
                return true;
        }
        return false;
    };

    this.getQuery = function (queryId) {
        if (!queryId)
            return null;
        for (let q in this.queries) {
            if (this.queries[q].id === queryId)
                return this.queries[q];
        }
        return null;
    };
    this.getQueryByLayerId = function (layerId) {
        if (!layerId)
            return null;
        for (let q in this.queries) {
            if (this.queries[q].layerid === layerId)
                return this.queries[q];
        }
    };
    this.isQuerySelectable = function (queryId) {
        let query = this.getQuery(queryId);
        if (query != null) {
            let layer = this.getLayer(query.layerid);
            if (layer != null && layer.selectable === false)
                return false;
        }

        return true;  // default;
    };
    this.hasQueryFeatureTransfers = function (queryId) {
        let query = this.getQuery(queryId);

        if (query != null) {
            return query.has_feature_transfers === true;
        }

        return false;
    };

    this.getFilter = function (filterId) {
        if (!filterId)
            return null;
        for (let f in this.filters) {
            if (this.filters[f].id === filterId)
                return this.filters[f];
        }
        return null;
    };
    this.getSnapping = function (snappingId) {
        if (!snappingId)
            return null;
        for (let s in this.snapping) {
            if (this.snapping[s].id === snappingId)
                return this.snapping[s];
        }
        return null;
    };
    this.getPresentation = function (presentationId) {
        //console.log(presentationId);
        if (!presentationId)
            return null;
        for (let p in this.presentations) {
            //console.log(this.presentations[p].id);
            if (this.presentations[p].id === presentationId)
                return this.presentations[p];
        }
        return null;
    };
    this.refresh = function () {
        this.incRequestId();
        this.events.fire('refresh');
    };
    this.remove = function () {
        this.incRequestId();
        this.events.fire('remove');
        if (webgis.mapFramework === "leaflet") {
            this.map.frameworkElement.removeLayer(this.frameworkElement);
        }
    };

    this.getEditLayerTheme = function (layerId) {
        if (this.editthemes) {
            for (let e in this.editthemes) {
                if (this.editthemes[e].layerid === layerId) {
                    return this.editthemes[e];
                }
            }
        }
        return null;
    }

    this.isEditLayer = function (layerId) {
        return this.getEditLayerTheme(layerId) !== null;
    };
    
    this.hasEditLayerPermission = function (layerId, permission) {
        let editTheme = this.getEditLayerTheme(layerId);

        return this.hasEditPermission(editTheme, permission);
    };

    this.hasEditLayerPermissions = function (layerId, permissions) {
        let editTheme = this.getEditLayerTheme(layerId);

        return this.hasEditPermissions(editTheme, permissions);
    };

    this.getEditTheme = function (themeId) {
        if (this.editthemes) {
            for (let e in this.editthemes) {
                if (this.editthemes[e].themeid === themeId) {
                    return this.editthemes[e];
                }
            }
        }

        return null;
    };

    this.hasEditPermission = function (editTheme, permission) {
        if (!editTheme) {
            return false;
        }

        if (!editTheme || !editTheme.dbrights) {
            return false;
        }

        switch (permission.toLowerCase()) {
            case 'insert':
                permission = 'i';
                break;
            case 'update':
                permission = 'u';
                break;
            case 'delete':
                permission = 'd';
                break;
            case 'geometry':
                permission = 'g';
                break;
            case 'massattr':
            case 'massattributation':
                permission = 'm';
                break;
        }

        return editTheme.dbrights.indexOf(permission) >= 0;
    };

    this.hasEditPermissions = function (editTheme, permissions) {
        if (!permissions || permissions.length === 0) {
            return false;
        }

        for (let p in permissions) {
            let permission = permissions[p];

            if (!this.hasEditPermission(editTheme, permission)) {
                return false;
            }
        }

        return true;
    };

    this.setOrder = function (order) {
        if (this.isWatermark()) {
            order = 2147483647;
        }

        //console.log('setOrder', this.id , order);
        if (this.frameworkElement) {
            if (this.frameworkElement.setOrder) {
                this.frameworkElement.setOrder(order);
            } else if (this.frameworkElement.setZIndex) {
                this.frameworkElement.setZIndex(order);
            }
        }

        _order = order;
        _isTopMost = false;
    };
    this.getOrder = function () { return _order; };
    this.resetOrder = function () { this.setOrder(_order); }
    this.isTopMost = function () { return _isTopMost; }
    this.bringToTop = function () {
        $.each(this.map.services, function (i, service) {
            if (service.isTopMost() === true)
                service.resetOrder();
        });

        console.log('setTopMost');
        if (this.frameworkElement) {
            if (this.frameworkElement.setOrder) {
                this.frameworkElement.setOrder(this.map.maxServiceOrder() + 1);
            } else if (this.frameworkElement.setZIndex) {
                this.frameworkElement.setZIndex(this.map.maxServiceOrder() + 1);
            }
        }
        _isTopMost = true;
    };

    this.setOpacity = function (opacity) {
        
        opacity = Math.min(Math.max(opacity, 0.0), 1.0);

        if (this.isWatermark()) {  // watermark services shoud not unfocused!!!
            opacity = this.serviceInfo.opacity || 1.0;
        }

        this.opacity = opacity
        opacity = Math.round(opacity * this.serviceInfo.opacity_factor * 100) / 100;

        //console.log(this.id + ".setOpacity: ", opacity, this.opacity);

        if (this.frameworkElement && this.frameworkElement.setOpacity) {
            //console.trace('setopacity', this.name, opacity);
            this.frameworkElement.setOpacity(opacity);
        } else if(this.serviceInfo.type === "vtc") {
            //console.log(this.frameworkElement);
            if (this.frameworkElement._container) {
                $(this.frameworkElement._container).css('opacity', opacity);
            }
        }
    };
    this.getOpacity = function () {
        //console.log(this.id + ".getOpacity() =>", this.opacity);
        return this.opacity;
    };
    this.focus = function () {
        if (this.frameworkElement && this.frameworkElement.setOpacity)
            this.frameworkElement.setOpacity(1.0);
    };
    this.unfocus = function (opacity) {
        if (this.isWatermark()) {  // watermark services shoud not unfocused!!!
            return;
        }

        if (this.frameworkElement && this.frameworkElement.setOpacity)
            this.frameworkElement.setOpacity(opacity);
    };
    this.resetFocus = function () {
        this.setOpacity(this.getOpacity());
    };
    this.hide = function (opacity) {
        if (this.isWatermark()) {  // watermark services can't be hidden!!!
            return;
        }

        opacity = opacity || 0.01;

        if (this.frameworkElement && this.frameworkElement.setOpacity)
            this.frameworkElement.setOpacity(opacity);
    };
    this.show = function () {
        if (this.isWatermark()) {  // watermark services can't be hidden!!!
            return;
        }

        if (this.frameworkElement && this.frameworkElement.setOpacity) {
            let opacity = this.getOpacity();

            let focusedServices = this.map.focusedServices();
            if (focusedServices && focusedServices.ids) {
                if ($.inArray(this.id, focusedServices.ids) >= 0) {
                    opacity = 1.0;
                } else {
                    opacity = focusedServices.getOpacity();
                }
            }

            this.frameworkElement.setOpacity(opacity);
        }
    };

    this.updateProperties = function (jsonService) {
        //console.log('updateProperties', this.name, jsonService)
        if (jsonService.opacity <= 0.0) {
            this.setOpacity(0.0);
        } else if (jsonService.opacity >= 1.0) {
            this.setOpacity(1.0);
        } else {
            this.setOpacity(jsonService.opacity || 1.0);
        }

        if (jsonService.order) {
            //console.log('setOrder: '+this.name, jsonService.order);
            this.setOrder(jsonService.order);
        }
    };

    // Request Id
    this._requestId = webgis.guid();
    this.incRequestId = function () {
        this._requestId = webgis.guid();
        this.events.fire('requestidchanged', this);
    };
    this.currentRequestId = function () {
        return this.map.currentRequestId() + '.' + this._requestId;
    };

    this.getPreviewUrl = function (r) {
        if (this.serviceInfo && this.serviceInfo.previewImageUrl) {
            return this.serviceInfo.previewImageUrl;
        }
        if (this.isBasemap === true) {
            let url = this.getSampleTileUrl();
            if (url)
                return url;
        }
        let url = webgis.baseUrl + "/rest/services/" + this.id + "/getmap?f=bin";
        if (webgis.mapFramework === "leaflet") {
            let map = this.map.frameworkElement;
            let crs = map.options.crs;
            if (!r.bbox || r.bbox.length !== 4) {
                let bounds = map.getBounds(), topLeft = map.latLngToLayerPoint(bounds.getNorthWest()), mapSize = map.latLngToLayerPoint(bounds.getSouthEast()).subtract(topLeft), nw = crs.project(bounds.getNorthWest()), se = crs.project(bounds.getSouthEast());
                r.bbox = [nw.x, se.y, se.x, nw.y];
            }

            if (!r.layers || r.layers.length === 0) {
                // DoTo: Alle Layer? sichtbare Layer?
                r.layers = [];
                $.each(this.layers, function (i, l) {
                    r.layers.push(l.id);
                });
                //r.layers = [0];
            }
            if (!r.scale)
                r.scale = this.map.scale();
            if (!r.width)
                r.width = 250;
            if (!r.height)
                r.height = 200;
            let urlParams = { width: r.width, height: r.height, bbox: r.bbox.join(','), layers: r.layers.join(','), crs: this.map.crs.id };
            if (r.scale)
                urlParams.scale = r.scale;
            webgis.hmac.appendHMACData(urlParams);
            url += "&" + $.param(urlParams);
        }
        return url;
    };
    this.getSampleTileUrl = function () {
        try {
            let extent = this.map.getProjectedExtent();
            let center = [(extent[0] + extent[2]) * 0.5, (extent[1] + extent[3]) * 0.5];
            //console.log(center);

            let mapResolution = this.map.resolution();
            let tileResolutions = this.serviceInfo.properties.resolutions;

            let diff, level = 0;

            for (let i = 0; i < tileResolutions.length; i++) {
                if (i === 0) {
                    diff = Math.abs(tileResolutions[i] - mapResolution);
                } else {
                    let d = Math.abs(tileResolutions[i] - mapResolution);
                    if (d < diff) {
                        diff = d;
                        level = i;
                    }
                }
            }
           
            //console.log(level, tileResolutions[level]);

            let dx = center[0] - this.serviceInfo.properties.origin[0];
            let dy = center[1] - this.serviceInfo.properties.origin[1];

            let tileSize = this.serviceInfo.properties.tilesize * tileResolutions[level];

            let x = parseInt(dx / tileSize), y = parseInt(-dy / tileSize);

            //console.log(x, y);

            // Hex Darstellung für AGS TileCaches
            let x_ = x.toString(16).lpad('0', 8);
            let y_ = y.toString(16).lpad('0', 8);
            let z_ = level.toString().lpad('0', 2);

            let tileUrl = this.serviceInfo.properties.tileurl;
            tileUrl = tileUrl.replace('{x}', x).replace('{y}', y).replace('{z}', level);
            tileUrl = tileUrl.replace('{x_}', x_).replace('{y_}', y_).replace('{z_}', z_);

            if (this.serviceInfo.properties.domains && this.serviceInfo.properties.domains.length > 0) {
                tileUrl = tileUrl.replace('{s}', this.serviceInfo.properties.domains[0]);
            }

            //console.log(tileUrl);

            return tileUrl;
        } catch (e) {
            return null;
        }
    };

    this.getLegendUrl = function (withoutPostData) {
        let url = webgis.baseUrl + "/rest/services/" + this.id + "/getlegend?";
        
        let urlParams = withoutPostData === true ? {} : this.getLegendPostData();
        webgis.hmac.appendHMACData(this.map.appendRequestParameters(urlParams));
        url += $.param(urlParams);
        return url;
    };
    this.getLegendPostData = function () {
        let bounds = this.map.getProjectedExtent(),
            crsId = this.map.crs ? this.map.crs.id : '4326',
            mapSize = this.map.getMapImageSize(),
            bbox = bounds.join(',');

        let vislayers = [];
        for (let i = 0; i < this.layers.length; i++) {
            let layer = this.layers[i];
            if (layer.visible === true && layer.legend === true) {
                vislayers.push(layer.id);
            }
        }
        return { width: mapSize[0], height: mapSize[0], bbox: bbox, crs: crsId, layers: vislayers.join(','), f: 'json' };
    };

    this.copy = function () {
        return new webgis.service(this.map, JSON.parse(JSON.stringify(this.serviceInfo)));
    };

    this.hasInitialError = function () {
        return (this.getInitialError() !== null);
    };

    this.isWatermark = function () {
        return (this.serviceInfo && this.serviceInfo.image_service_type === 'watermark') ? true : false;
    };

    this.getInitialError = function () {
        if (this.serviceInfo && this.serviceInfo.initial_exception) {
            return this.serviceInfo.initial_exception;
        }

        return null;
    };

    this.addCustomRequestParameters = function (params) {
        if (this.serviceInfo && this.serviceInfo.custom_request_parameters) {
            for (let c in this.serviceInfo.custom_request_parameters) {
                params['custom.' + this.id + '.' + c] = this.serviceInfo.custom_request_parameters[c];
            }
        }
    };

    /* Static Overlays ... */
    this.setAffinePoints = function (affinePoints) {
        //console.log('service.setAffinePoints', affinePoints);
        if (affinePoints &&
            affinePoints.length === 3 &&
            affinePoints[0] && affinePoints[1] && affinePoints[2] &&
            this.frameworkElement.reposition) {
            this.frameworkElement.reposition(
                { lng: affinePoints[0][0], lat: affinePoints[0][1] },
                { lng: affinePoints[1][0], lat: affinePoints[1][1] },
                { lng: affinePoints[2][0], lat: affinePoints[2][1] }
            );
        }
    };

    this.setPassPoints = function (passPoints) {
        let me = this;
        let set = function () {
            //console.log('service.setPassPoints', passPoints);
            let pp = [];

            if (passPoints && passPoints.length > 0) {
                for (let p in passPoints) {
                    let passPoint = passPoints[p];
                    pp.push({
                        vec: { x: passPoint.vec[0], y: passPoint.vec[1] },
                        latLng: { lng: passPoint.pos[0], lat: passPoint.pos[1] }
                    });
                }
            }

            me.frameworkElement.setPassPoints(pp);
        }

        if (this.frameworkElement.setPassPoints) {
            if (this.frameworkElement.onLoaded) {
                this.frameworkElement.onLoaded(set);
            } else {
                set();
            }
        }
    };

    this.showPassPoints = function () {
        if (this.frameworkElement.showPassPoints) {
            //console.log('service.showPassPoints');
            this.frameworkElement.showPassPoints();
            if (this.type === "static_overlay") {
                //console.log('max', this.map.maxBackgroundCoveringServiceOrder());
                //this.setOrder(this.map.maxBackgroundCoveringServiceOrder() + 2);
            }
        }
    };
    this.hidePassPoints = function () {
        if (this.frameworkElement.hidePassPoints) {
            //console.log('service.hidePassPoints');
            this.frameworkElement.hidePassPoints();
            if (this.type === "static_overlay") {
                //this.setOrder(this.map.maxBackgroundCoveringServiceOrder() + 1);
            }
        }
    };

    this._fireCustomOnAdd = function () {
        if (this.serviceInfo && typeof this.serviceInfo.onAdd === "function") {
            this.serviceInfo.onAdd(this.map, this.frameworkElement);
        }
    };
    this._fireCustomOnRemove = function () {
        if (this.serviceInfo && typeof this.serviceInfo.onRemove === "function") {
            this.serviceInfo.onRemove(this.map, this.frameworkElement);
        }
    }
};