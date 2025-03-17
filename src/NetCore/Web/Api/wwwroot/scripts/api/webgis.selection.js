webgis.selection = function (map, name) {
    "use strict";

    var $ = webgis.$;

    webgis.implementEventController(this);
    this.map = map;
    this.name = name;
    this.frameworkElement = null;
    this.service = null;
    this.layerid = '';
    this.queryid = '';
    this.customid = '';
    this.fids = '';
    this._isActive = false;

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
    };
    this.destroy = function () {
        console.log('destroy selection: ' + this.name);
    };
    this.setTargetLayer = function (selectionService, selectionLayerid, selectionFids) {
        this.service = selectionService;
        this.layerid = selectionLayerid;
        this.fids = selectionFids;
        if (this.frameworkElement && this.frameworkElement.setTargetLayer)
            this.frameworkElement.setTargetLayer(this.service, this.layerid, this.fids);
        this._isActive = true;
        this.refresh();
    };
    this.setTargetQuery = function (selectionService, selectionQueryid, selectionFids) {
        this.service = selectionService;
        this.queryid = selectionQueryid;
        this.fids = selectionFids;
        if (this.frameworkElement && this.frameworkElement.setTargetQuery)
            this.frameworkElement.setTargetQuery(this.service, this.queryid, this.fids);
        this._isActive = true;
        this.refresh();
    };
    this.setTargetCustomId = function (selectionService, customId) {
        this.service = selectionService;
        this.customid = customId;
        if (this.frameworkElement && this.frameworkElement.setTargetCustomId)
            this.frameworkElement.setTargetCustomId(this.service, this.customid);
        this._isActive = true;
        this.refresh();
    };
    this.isActive = function () { return this._isActive; };
    this.includesOid = function (featureOid) {
        if (!this.fids || !this.service || !this.queryid)
            return false;

        var fids = this.fids.split(',');
        for (var i in fids) {
            if (this.service.id + ':' + this.queryid + ':' + fids[i] === featureOid)
                return true;
        }

        return false;
    };
    this.count = function () {
        if (!this.fids)
            return 0;

        return this.fids.split(',').length;
    };
    this.refresh = function () {
        this.events.fire('refresh');
    };
    this.remove = function () {
        this._isActive = false;
        this.events.fire('remove');
        /*
        if (webgis.mapFramework == "leaflet") {
            this.map.frameworkElement.removeLayer(this.frameworkElement);
        }
        */
    };
    this.serialize = function () {
        return {
            type: this.name,
            serviceid: this.service.id,
            queryid: this.queryid,
            fids: this.fids,
            customid: this.customid
        };
    };
};
