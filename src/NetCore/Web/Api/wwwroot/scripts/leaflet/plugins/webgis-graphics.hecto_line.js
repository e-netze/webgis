L.HectoLine = L.LayerCollection.extend({
    options: {
        color: '#ff0000',
        weight: 2,
        fontColor: 'black',
        fontSize: 13,
        fontSytle: '',
        circleMarkerOptions: { same_as_line:true, radius: 4, color: '#000', weight: 2, fillcolor: '#eee' },
        attributes: {},
        unit: 'm',
        interval:100
    },

    _calcCrs: 0,
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
    setUnit: function (unit) {
        this.options.unit = unit;
        this.rebuild();
    },
    getUnit: function () {
        return this.options.unit;
    },
    setInterval: function (interval) {
        this.options.interval = interval;
        this.rebuild();
    },
    getInterval: function () {
        return this.options.interval;
    },
    getCalcCrs: function () { return this._calcCrs; },
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

            var path = this._projectLatLngs();
            var pathLength = webgis.calc.length(path, "X", "Y"), interval = this.options.interval;
            //console.log('path', path, pathLength);

            switch (this.options.unit) {
                case 'km':
                    interval *= 1000.0;
                    break;
            }

            if (interval >= 1.0 && interval > pathLength / 1000.0) {
                var isEndpoint = false;
                for (var stat = 0, to = pathLength + interval * 2; stat < to; stat += interval) {
                    if (stat > pathLength) {
                        stat = pathLength;
                        isEndpoint = true;
                    }

                    var pathPoint = webgis.calc.pathPoint(path, stat, "X", "Y");
                    if (pathPoint) {
                        var direction = pathPoint.direction;
                        pathPoint = webgis.calc.unproject(pathPoint.x, pathPoint.y);
                        var latLng = { lng: pathPoint.x, lat: pathPoint.y };
                        //console.log('latLng', latLng);
                        var circle = L.circleMarker(latLng, this.options.circleMarkerOptions);
                        this.addChildLayer(circle);

                        //console.log(direction);

                        var labelText = '';
                        switch (this.options.unit) {
                            case 'km':
                                var unitStat = stat / 1000.0;
                                labelText = (Math.round(unitStat * 1000.0) / 1000.0).toLocaleString() + 'km';
                                break;
                            default:
                                labelText = (Math.round(stat * 100.0) / 100.0).toLocaleString() + 'm';
                                break;
                        }

                        var label = L.svgLabel(latLng, { text: '  '+ labelText, rotation: 0/* -direction + 90.0*/, fontSize: this.options.fontSize, anchor: 'left', alignmentBaseline: 'ideographic' });
                        this.addChildLayer(label);
                    }
                    if (isEndpoint)
                        break;
                }
            }
        }
    },

    _projectLatLngs: function () {
        var result = [];

        if (this._latLngs && webgis.calc._canProject()) {
            for (var i = 0; i < this._latLngs.length; i++) {
                result.push({ x: this._latLngs[i].lng, y: this._latLngs[i].lat });
            }
            this._calcCrs = webgis.calc.getCalcCrsId(result);
            result = webgis.calc._projectToCalcCrs(result);
        }

        return result;
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

L.hectoLine = function (latLngs, options) {
    return new L.HectoLine(latLngs, options);
};
