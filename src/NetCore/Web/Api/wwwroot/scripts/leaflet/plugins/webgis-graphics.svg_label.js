L.SVGLabel = L.Layer.extend({
    options: {
        fontColor: 'black',
        fontSize: 14,
        fontSytle: '',
        text: 'SVG Label',
        anchor: 'middle',
        alignmentBaseline:'ideographic',
        rotation: 0,
        refResolution: 0,
        attributes: {}
    },

    initialize: function (latlng, options) {
        L.setOptions(this, options);
        this.setLatLng(latlng);
    },

    beforeAdd: function (map) {
        // Renderer is set here because we need to call renderer.getEvents
        // before this.getEvents.
        this._renderer = map.getRenderer(this);

        map.on('moveend', this.redraw, this);
        map.on('zoomend', this.redraw, this);
    },

    onAdd: function () {
        this.redraw();
    },

    onRemove: function () {
        if (this._textNode) {
            this._textNode.parentNode.removeChild(this._textNode);
            this._textNode = null;
            this._textNode2.parentNode.removeChild(this._textNode2);
            this._textNode2 = null;
        }

        this._map.off('moveend', this.redraw, this);
        this._map.off('zoomend', this.redraw, this);
    },

    _pos: null,
    setLatLng: function (latlng) {
        this._pos = latlng;
        this.redraw();
    },

    setText: function (text) {
        this.options.text = text;
        this.redraw();
    },
    getText: function () {
        return this.options.text;
    },
    setRotation: function (rotation) {
        this.options.rotation = rotation;
        this.redraw();
    },
    getRotation: function () {  
        return this.options.rotation;
    },

    setStyle: function (style) {
        for (var i in style) {
            switch (i.toLocaleLowerCase()) {
                case 'fill':
                case 'font-color':
                    this._textNode.style['fill'] = this.options.fontColor = style[i];
                    this._textNode2.style['stroke'] = this._grayValue(this._textNode.style['fill']) < 150 ? '#fff' : '#000';
                    break;
                case 'font-size':
                case 'fontsize':
                case 'size':
                    //console.log('setsize', this.options.refResolution);
                    if (this.options.refResolution === 0) {
                        this.options.refResolution = this._currentMapResolution();
                    }
                    this.options.fontSize = style[i];
                    this._setRefSize();
                    break;
                case 'font-style':
                case 'fontstyle':
                case 'style':
                    this.options.fontStyle = style[i];
                    switch (style[i]) {
                        case 'regular':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'normal';
                            break;
                        case 'italic':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'italic';
                            break;
                        case 'bold':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'normal';
                            this._textNode.style['font-weight'] = this._textNode2.style['font-weight'] = 'bold';
                            break;
                        case 'bolditalic':
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'italic';
                            this._textNode.style['font-weight'] = this._textNode2.style['font-weight'] = 'bold';
                            break;
                        default:
                            this._textNode.style['font-style'] = this._textNode2.style['font-style'] = 'normal';
                            this._textNode.style['font-weight'] = this._textNode2.style['font-weight'] = 'normal';
                            break;
                    }
                    break;
                default:
                    this._textNode.style[i] = style[i];
                    break;
            }
        }

        //console.log(this.options);
        //L.setOptions(this._textNode, style);
        return this;
    },

    redraw: function () {
        //console.log('text redraw', this._map, this._pos, this._textNode);

        if (!this._textNode) {
            this.rebuild();
        }

        if (this._map && this._pos && this._textNode) {
            var point = this._map.latLngToLayerPoint(this._pos);

            var text = (this.options.text || '').replace(/ /g, '\u00A0');  // Non breakable spaces
            this._textNode.innerHTML = this._textNode2.innerHTML = '';

            this._textNode.appendChild(document.createTextNode(text));
            this._textNode2.appendChild(document.createTextNode(text));


            this._textNode.setAttribute('text-anchor', this.options.anchor);
            this._textNode.setAttribute('alignment-baseline', this.options.alignmentBaseline);
            this._textNode.setAttribute('transform', 'translate(' + point.x + ',' + point.y + ') rotate(' + this.options.rotation + ')');

            this._textNode2.setAttribute('text-anchor', this.options.anchor);
            this._textNode2.setAttribute('alignment-baseline', this.options.alignmentBaseline);
            this._textNode2.setAttribute('transform', 'translate(' + point.x + ',' + point.y + ') rotate(' + this.options.rotation + ')');

            this._setRefSize();
        }

        return this;
    },

    _textNode: null,
    _textNodeStroke: null,
    rebuild: function () {
        if (!L.Browser.svg || typeof this._map === 'undefined') {
            return this;
        }

        if (!this._textNode) {
            var id = 'svgtext-' + L.Util.stamp(this);
            var id2 = 'svgtext-' + L.Util.stamp(this);
            var svg = this._map._renderer._container;

            this._textNode = L.SVG.create('text');
            this._textNode2 = L.SVG.create('text');
            for (var attr in this.options.attributes) {
                this._textNode.setAttribute(attr, this.options.attributes[attr]);
                this._textNode.setAttributeNS("http://www.w3.org/1999/xlink", "xlink:href", '#' + id);

                this._textNode2.setAttribute(attr, this.options.attributes[attr]);
                this._textNode2.setAttributeNS("http://www.w3.org/1999/xlink", "xlink:href", '#' + id2);
            }

            this._textNode2.style["stroke-width"] = 2;

            this.setStyle({ 'fill': this.options.fontColor });
            this.setStyle({ 'font-size': this.options.fontSize });
            this.setStyle({ 'font-style': this.options.fontStyle });

            svg.childNodes[0].appendChild(this._textNode2);
            svg.childNodes[0].appendChild(this._textNode);
        }
    },

    _parseColor: function (input) {
        try {
            if (input.substr(0, 1) === "#") {
                var collen = (input.length - 1) / 3;
                var fact = [17, 1, 0.062272][collen - 1];
                return [
                    Math.round(parseInt(input.substr(1, collen), 16) * fact),
                    Math.round(parseInt(input.substr(1 + collen, collen), 16) * fact),
                    Math.round(parseInt(input.substr(1 + 2 * collen, collen), 16) * fact)
                ];
            }
            else return input.split("(")[1].split(")")[0].split(",").map(Math.round);
        } catch (ex) { return null; }
    },
    _grayValue: function (input) {
        var col = this._parseColor(input);
        if (col) {
            return (col[0] + col[1] + col[2]) / 3;
        }

        return 0;
    },

    _currentMapResolution: function () {
        try {
            if (this._map.options.crs.options.resolutions) {
                //console.log(this._map.options.crs);
                return this._map.options.crs.options.resolutions[this._map.getZoom()];
            }
        } catch (ex) { console.log(ex); }

        return 0;
    },

    _setRefSize: function () {
        // Reference Scale: später einmal ...

        //console.log('_setRefSize',this.options.refResolution);
        //if (this.options.refResolution > 0) {
        //    var currentResolution = this._currentMapResolution();
        //    console.log('_setRefSize2',this.options.currentResolution);
        //    if (currentResolution > 0) {
        //        var factor = this.options.refResolution / currentResolution;

        //        console.log('refResolution', this.options.refResolution);
        //        console.log('currentResolution', currentResolution);
        //        console.log('factor', factor);

        //        this._textNode.style['font-size'] = this._textNode2.style['font-size'] = this.options.fontSize * factor;
        //        return;
        //    }
        //}

        this._textNode.style['font-size'] = this._textNode2.style['font-size'] = this.options.fontSize;
    },

    toGeoJSON: function () {
        var geoJson = {
            geometry: {
                type: 'Point',
                coordinates: []
            }
        };

        if (this._pos) {
            geoJson.geometry.coordinates = [this._pos.lng, this._pos.lat];
        }

        return geoJson;
    }
});

L.svgLabel = function (latLng, options) {
    return new L.SVGLabel(latLng, options);
};
