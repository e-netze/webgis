L.AngleGraphics = L.LayerCollection.extend({
    options: {
        color: '#aaaaaa',
        weight: 1
    },

    _latLngs: null,
    _setLatLngs: function (latLngs) {
        this.setLatLngs(latLngs);
    },

    onAdd: function () {
        this.rebuild();
    },
    setLatLngs: function (latLngs) {
        this._latLngs = latLngs;
        this.rebuild();
    },
    getLatLngs: function () { return this._latLngs; },
    rebuild: function () {
        if (this._latLngs && this._latLngs.length >= 2) {

            var count = this.getChildLayerCount();
            if (count === 0) {
                var line = L.polyline([this._latLngs[0], this._latLngs[1]], this.options);
                this.addChildLayer(line);

                var rectangle = L.polyline([], this.options);
                this.addChildLayer(rectangle);
                this._rebuild_rect(rectangle);

                var arrow = L.polyline([], this.options);
                this.addChildLayer(arrow);
                this._rebuild_arrow(arrow);
            } else {
                var line = this.getChildLayer(0);
                line.setLatLngs([this._latLngs[0], this._latLngs[1]]);

                this._rebuild_rect(this.getChildLayer(1));
                this._rebuild_arrow(this.getChildLayer(2));
            }
        }
    },
    _rebuild_rect: function (element) {
        var p = [
            { x: this._latLngs[0].lng, y: this._latLngs[0].lat },
            { x: this._latLngs[1].lng, y: this._latLngs[1].lat }
        ];

        var xProp = "x", yProp = "y";

        if (webgis.calc._canProject()) {
            p = webgis.calc._projectToCalcCrs(p);
            xProp = "X"; yProp = "Y";
        }

        var ox1 = p[0][xProp], oy1 = p[0][yProp];
        var ox2 = p[1][xProp], oy2 = p[1][yProp];

        var dx = ox2 - ox1;
        var dy = oy2 - oy1;

        rect = [{}, {}, {}, {}, {}];
        rect[0][yProp] = oy1 - dy / 2 + dx / 4; rect[0][xProp] = ox1 - dx / 2 - dy / 4;
        rect[1][yProp] = oy1 + dy / 2 + dx / 4; rect[1][xProp] = ox1 + dx / 2 - dy / 4;
        rect[2][yProp] = oy1 + dy / 2 - dx / 4; rect[2][xProp] = ox1 + dx / 2 + dy / 4;
        rect[3][yProp] = oy1 - dy / 2 - dx / 4; rect[3][xProp] = ox1 - dx / 2 + dy / 4;
        rect[4][yProp] = oy1 - dy / 2 + dx / 4; rect[4][xProp] = ox1 - dx / 2 - dy / 4;

        if (webgis.calc._canProject()) {
            rect = webgis.calc._unprojectFromCalcCrs(rect);
        }

        element.setLatLngs(this._toLatLngs(rect));
    },
    _rebuild_arrow: function (element) {
        var p = [
            { x: this._latLngs[0].lng, y: this._latLngs[0].lat },
            { x: this._latLngs[1].lng, y: this._latLngs[1].lat }
        ];

        var xProp = "x", yProp = "y";

        if (webgis.calc._canProject()) {
            p = webgis.calc._projectToCalcCrs(p);
            xProp = "X"; yProp = "Y";
            //console.log(p);
        }

        var ox1 = p[0][xProp], oy1 = p[0][yProp];
        var ox2 = p[1][xProp], oy2 = p[1][yProp];

        var dx = ox2 - ox1;
        var dy = oy2 - oy1;

        arrow = [{}, {}, {}];
        arrow[0][yProp] = oy1 - dy / 2 - dx / 4; arrow[0][xProp] = ox1 - dx / 2 + dy / 4;
        arrow[1][yProp] = oy1 + dx / 4;          arrow[1][xProp] = ox1 - dy / 4;
        arrow[2][yProp] = oy1 + dy / 2 - dx / 4; arrow[2][xProp] = ox1 + dx / 2 + dy / 4;

        if (webgis.calc._canProject()) {
            arrow = webgis.calc._unprojectFromCalcCrs(arrow);
            //console.log(arrow);
        }

        element.setLatLngs(this._toLatLngs(arrow));
    },
    _toLatLngs: function (coords) {
        var latLngs = [];

        for (var i in coords) {
            latLngs.push([coords[i].y, coords[i].x]);
        }

        return latLngs;
    }
});

L.angleGraphics = function (latLngs, options) {
    return new L.AngleGraphics(latLngs, options);
}
