L.DimPolygon = L.LayerCollection.extend({
    options: {
        fillColor: '#ffff00',
        fillOpacity: 0.2,
        strokeColor: '#ff0000',
        strokeWeight: 2,
        fontColor: 'black',
        fontSize: 13,
        fontSytle: '',
        circleMarkerOptions: { same_as_line: true, radius: 4, color: '#000', weight: 2, fillcolor: '#eee' },
        areaUnit: 'm²',
        labelEdges: false,
        attributes: {}
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
    getCalcCrs: function () { return this._calcCrs; },
    setAreaUnit: function (unit) {
        this.options.areaUnit = unit;
        this.rebuild();
    },
    getAreaUnit: function () {
        return this.options.areaUnit;
    },
    setLabelEdges: function (doLabel) {
        this.options.labelEdges = doLabel;
        this.rebuild();
    },
    getLabelEdges: function () {
        return this.options.labelEdges;
    },
    redraw: function () {
        this.rebuild();
    },
    rebuild: function () {
        if (this._latLngs && this._latLngs.length >= 2) {
            let count = this.getChildLayerCount();
            if (count === 0) {
                let polygon = L.polygon(this._latLngs, this.options);
                this.addChildLayer(polygon);
                polygon.on('click', function (e) {
                    this.fireEvent('click', e);
                }, this);
            } else {
                var polygon = this.getChildLayer(0);
                polygon.setLatLngs(this._latLngs);

                this.trimChildren(1);
            }

            if (this.options.circleMarkerOptions.same_as_line === true) {
                this.options.circleMarkerOptions.color = this.options.color;
                this.options.circleMarkerOptions.weight = this.options.weight;
            }

            let xProp = "x", yProp = "y";
            if (webgis.calc._canProject()) {
                xProp = "X"; yProp = "Y";
            }
            let calcVertices = [];
            let insertAtArea = { lng: 0, lat: 0 };

            let lengthFactor = 1.0;
            let areaFactor = 1.0;
            let lengthUnit = ''
            let areaUnit = '';
            let digitsRound = 100.0;

            switch (this.options.areaUnit) {
                case 'km':
                case 'km²':
                case 'km2':
                    lengthFactor = 1.0/1000.0;
                    areaFactor = 1.0/1000000.0;
                    lengthUnit = 'km';
                    areaUnit = 'km²';
                    digitsRound = 1000.0;
                    break;
                case "ha":
                    lengthFactor = 1.0;  // use meters
                    areaFactor = 1.0 / 10000.0;
                    lengthUnit = 'm';
                    areaUnit = 'ha';
                    break;
                case "a":
                case "ar":
                    lengthFactor = 1.0;  // use meters
                    areaFactor = 1.0 / 100.0;
                    lengthUnit = 'm';
                    areaUnit = 'a';
                    break;
                default:  // m
                    lengthFactor = 1.0;
                    areaFactor = 1.0;
                    lengthUnit = 'm';
                    areaUnit = 'm²';
            }


            for (let i = 0; i < this._latLngs.length; i++) {
                let p = [
                    { x: this._latLngs[i].lng, y: this._latLngs[i].lat },
                    { x: this._latLngs[(i + 1) % this._latLngs.length].lng, y: this._latLngs[(i + 1) % this._latLngs.length].lat }
                ];
                let insertAt = { lng: (p[0].x + p[1].x) * .5, lat: (p[0].y + p[1].y) * .5 };

               
                //console.log(p);

                if (webgis.calc._canProject()) {
                    this._calcCrs = webgis.calc.getCalcCrsId(p);
                    p = webgis.calc._projectToCalcCrs(p);
                }

                calcVertices.push({ x: this._latLngs[i].lng, y: this._latLngs[i].lat });
                insertAtArea.lng += (this._latLngs[i].lng - insertAtArea.lng) / calcVertices.length;
                insertAtArea.lat += (this._latLngs[i].lat - insertAtArea.lat) / calcVertices.length;

                let dx = p[1][xProp] - p[0][xProp];
                let dy = p[1][yProp] - p[0][yProp];
                let len = Math.sqrt(dx * dx + dy * dy);
                let angle = Math.atan2(dy, dx) * 180.0 / Math.PI;
                if (angle < 0)
                    angle += 360.0;

                //console.log(angle);

                if (angle > 90.0 && angle < 270)
                    angle += 180;

                let circle = L.circleMarker(this._latLngs[i], this.options.circleMarkerOptions);
                this.addChildLayer(circle);

                if (this.options.labelEdges) {
                    let text = L.svgLabel(insertAt, {
                        text: (Math.round(len * lengthFactor * digitsRound) / digitsRound).toLocaleString() + lengthUnit,
                        rotation: -angle,
                        fontSize: this.options.fontSize,
                        alignmentBaseline: 'ideographic' //'central'
                    });
                    this.addChildLayer(text);
                }
            }

            let area = webgis.calc.area(calcVertices, xProp, yProp);
            let circumference = webgis.calc.length(calcVertices, xProp, yProp, true);
            //console.log('calcVertices', calcVertices, 'area', area, 'circumference', circumference);

            if (area > 0) {
                this.addChildLayer(L.svgText(insertAtArea, {
                    text:
                        webgis.l10n.get('area')[0] + ": " + (Math.round(area * areaFactor * digitsRound) / digitsRound).toLocaleString() + areaUnit + '\n' +
                        webgis.l10n.get('circumference')[0] + ": " + (Math.round(circumference * lengthFactor * digitsRound) / digitsRound).toLocaleString() + lengthUnit,
                    fontSize: this.options.fontSize * 1.2,
                    textAnchor: 'middle'
                }));
            }

            let circle = L.circleMarker(this._latLngs[this._latLngs.length - 1], this.options.circleMarkerOptions);
            this.addChildLayer(circle);
        }
    },
    toGeoJSON: function () {
        let geoJson = {
            geometry: {
                type: 'Polygon',
                coordinates: []
            }
        };

        let ring = [];
        if (this._latLngs) {
            for (let l in this._latLngs) {
                ring.push([this._latLngs[l].lng, this._latLngs[l].lat]);
            }
        }

        geoJson.geometry.coordinates.push(ring);
        //geoJson.geometry.coordinates = ring;

        return geoJson;
    }
})

L.dimPolygon = function (latLngs, options) {
    return new L.DimPolygon(latLngs, options);
};
