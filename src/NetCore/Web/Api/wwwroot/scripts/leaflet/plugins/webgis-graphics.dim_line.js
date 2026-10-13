L.DimLine = L.LayerCollection.extend({
    options: {
        color: '#ff0000',
        weight: 2,
        fontColor: 'black',
        fontSize: 13,
        fontSytle: '',
        circleMarkerOptions: { same_as_line: true, radius: 4, color: '#000', weight: 2, fillcolor: '#eee' },
        lengthUnit: 'm',
        labelTotalLength: false,
        attributes: {}
    },

    _calcCrs:0,
    _latLngs: null,
    _setLatLngs: function (latLngs) {
        this.setLatLngs(latLngs);
    },

    _setStyle: function (style) {
        for (var i in style) {
            switch (i.toLocaleLowerCase()) {
                case 'font-size':
                case 'fontsize':
                case 'size':
                    this.options.fontSize = style[i];
                    break;
            }
        }
    },

    onAdd: function () {
        this.rebuild();
    },
    setLatLngs: function (latLngs) {
        this._latLngs = latLngs;
        this.rebuild();
    },
    addLatLng: function (latLng) {
        if (this._latLngs)
            this._latLngs.push(latLng);
        else
            this._latLngs = [latLng];

        this.rebuild();
    },
    getLatLngs: function () { return this._latLngs; },
    getCalcCrs: function () { return this._calcCrs; },
    setLengthUnit: function (unit) {
        this.options.lengthUnit = unit;
        this.rebuild();
    },
    getLengthUnit: function () {
        return this.options.lengthUnit;
    },
    setLabelTotalLength: function (doLabel) {
        this.options.labelTotalLength = doLabel;
        this.rebuild();
    },
    getLabelTotalLength: function () {
        return this.options.labelTotalLength;
    },
    redraw: function () {
        this.rebuild();
    },
    rebuild: function () {
        if (this._latLngs && this._latLngs.length >= 2) {
            var count = this.getChildLayerCount();
            if (count === 0) {
                var line = L.polyline(this._latLngs, this.options);
                this.addChildLayer(line);
                line.on('click', function (e) {
                    this.fireEvent('click', e);
                }, this);
            } else {
                var line = this.getChildLayer(0);
                line.setLatLngs(this._latLngs);

                this.trimChildren(1);
            }

            if (this.options.circleMarkerOptions.same_as_line === true) {
                this.options.circleMarkerOptions.color = this.options.color;
                this.options.circleMarkerOptions.weight = this.options.weight;
            }

            let lengthFactor = 1.0;
            let lengthUnit = '';
            let totalLength = 0.0;

            switch (this.options.lengthUnit) {
                case 'km':
                    lengthFactor = 0.001;
                    lengthUnit = 'km';
                    break;
                default:
                    lengthFactor = 1.0;
                    lengthUnit = 'm';
                    break;
            }

            for (var i = 0; i < this._latLngs.length - 1; i++) {
                var p = [
                    { x: this._latLngs[i].lng,     y: this._latLngs[i].lat },
                    { x: this._latLngs[i + 1].lng, y: this._latLngs[i + 1].lat }
                ];
                var insertAt = { lng: (p[0].x + p[1].x) * .5, lat: (p[0].y + p[1].y) * .5 };

                var xProp = "x", yProp = "y";

                //console.log(p);

                if (webgis.calc._canProject()) {
                    this._calcCrs = webgis.calc.getCalcCrsId(p);
                    p = webgis.calc._projectToCalcCrs(p);
                    xProp = "X"; yProp = "Y";
                    //console.log(p);
                }

                var dx = p[1][xProp] - p[0][xProp];
                var dy = p[1][yProp] - p[0][yProp];
                var len = Math.sqrt(dx * dx + dy * dy);
                var angle = Math.atan2(dy, dx) * 180.0 / Math.PI;
                if (angle < 0)
                    angle += 360.0;

                //console.log(angle);

                if (angle > 90.0 && angle < 270)
                    angle += 180;

                var circle = L.circleMarker(this._latLngs[i], this.options.circleMarkerOptions);
                this.addChildLayer(circle);

                totalLength += len;

                var text = L.svgLabel(insertAt, {
                    text: (Math.round(len * lengthFactor * 100.0) / 100.0).toLocaleString() + lengthUnit,
                    rotation: -angle,
                    fontSize: this.options.fontSize,
                    alignmentBaseline: 'ideographic' //'central'
                });
                this.addChildLayer(text);
            }

            var circle = L.circleMarker(this._latLngs[this._latLngs.length - 1], this.options.circleMarkerOptions);
            this.addChildLayer(circle);

            if (this.options.labelTotalLength && this._latLngs.length > 2) {
                this.addChildLayer(L.svgText(this._latLngs[this._latLngs.length - 1], {
                    text: " ∑: " + (Math.round(totalLength * lengthFactor * 100.0) / 100.0).toLocaleString() + lengthUnit,
                    fontSize: this.options.fontSize * 1.2
                }));
            }
        }
    },
    toGeoJSON: function () {
        var geoJson = {
            geometry: {
                type: 'LineString',
                coordinates: []
            }
        };

           
        if (this._latLngs) {
            for (var l in this._latLngs) {
                geoJson.geometry.coordinates.push([this._latLngs[l].lng, this._latLngs[l].lat]);
            }
        }

        return geoJson;
    }
});

L.dimLine = function (latLngs, options) {
    return new L.DimLine(latLngs, options);
};
