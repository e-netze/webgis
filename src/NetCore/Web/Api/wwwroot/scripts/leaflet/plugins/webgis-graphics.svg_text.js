L.SVGText = L.Layer.extend({
    options: {
        fontColor: 'black',
        fontSize: 14,
        fontSytle: '',
        text: 'SVG Text',
        refResolution: 0,
        textAnchor: '',
        rotation: 0,   // degrees, clockwise, around the insertion point
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
        map.on('click', this._mapClick, this);
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

        this._removeRotationHandle();

        this._map.off('moveend', this.redraw, this);
        this._map.off('zoomend', this.redraw, this);
        this._map.off('click', this._mapClick, this);
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
        rotation = parseFloat(rotation);
        this.options.rotation = isNaN(rotation) ? 0 : L.SVGText.normalizeRotation(rotation);
        this.redraw();
    },
    getRotation: function () {
        return this.options.rotation || 0;
    },

    /* Rotation handle (marker right of the first line), only while the text is edited in the sketch */
    showRotationHandle: function (show) {
        this._rotationHandleEnabled = show === true;
        if (this._rotationHandleEnabled) {
            this._updateRotationHandle();
        } else {
            this._removeRotationHandle();
        }
    },

    _rotationHandleEnabled: false,
    _rotationHandle: null,
    _rotationHandleDragging: false,

    _rotationHandleOffset: function () {  // offset (layer points, unrotated) from the insertion point
        var width = 0;
        try {
            var firstLine = this._textNode && this._textNode.firstChild;
            width = firstLine && firstLine.getComputedTextLength ? firstLine.getComputedTextLength() : 0;
        } catch (ex) { }

        return L.point(width + 18, -this.options.fontSize * 0.35);
    },
    _rotatePoint: function (point, rotation) {
        var a = rotation * Math.PI / 180.0, cos = Math.cos(a), sin = Math.sin(a);
        return L.point(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
    },
    _updateRotationHandle: function () {
        if (!this._rotationHandleEnabled || !this._map || !this._pos || !this._textNode) {
            this._removeRotationHandle();
            return;
        }
        if (this._rotationHandleDragging) {
            return;
        }

        var anchor = this._map.latLngToLayerPoint(this._pos);
        var handleLatLng = this._map.layerPointToLatLng(anchor.add(this._rotatePoint(this._rotationHandleOffset(), this.getRotation())));

        if (!this._rotationHandle) {
            this._rotationHandle = L.marker(handleLatLng, {
                draggable: true,
                zIndexOffset: 1000,
                title: webgis.l10n.get('graphics-text-rotation-handle'),
                icon: L.icon({
                    iconUrl: webgis.css.imgResource('rotate_26.png', 'markers'),
                    iconSize: [26, 26],
                    iconAnchor: [13, 13]
                })
            });
            this._rotationHandle.on('dragstart', this._onRotationHandleDragStart, this);
            this._rotationHandle.on('drag', this._onRotationHandleDrag, this);
            this._rotationHandle.on('dragend', this._onRotationHandleDragEnd, this);
            this._rotationHandle.addTo(this._map);
        } else {
            this._rotationHandle.setLatLng(handleLatLng);
        }
    },
    _removeRotationHandle: function () {
        if (this._rotationHandle) {
            this._rotationHandle.off();
            if (this._rotationHandle._map) {
                this._rotationHandle._map.removeLayer(this._rotationHandle);
            }
            this._rotationHandle = null;
        }
        this._rotationHandleDragging = false;
    },
    _onRotationHandleDragStart: function () {
        this._rotationHandleDragging = true;
        this._rotationHandleOffsetAngle = (function (o) { return Math.atan2(o.y, o.x) * 180.0 / Math.PI; })(this._rotationHandleOffset());
    },
    _onRotationHandleDrag: function (e) {
        if (!this._map || !this._pos) return;

        var anchor = this._map.latLngToLayerPoint(this._pos);
        var p = this._map.latLngToLayerPoint(this._rotationHandle.getLatLng());
        if (p.distanceTo(anchor) < 2) return;

        var rotation = Math.atan2(p.y - anchor.y, p.x - anchor.x) * 180.0 / Math.PI - this._rotationHandleOffsetAngle;
        var originalEvent = e && e.originalEvent;

        this.setRotation(L.SVGText.snapRotation(rotation,
            originalEvent && originalEvent.shiftKey === true,
            originalEvent && (originalEvent.ctrlKey === true || originalEvent.metaKey === true)));
        this.fire('rotating', { rotation: this.getRotation() });
    },
    _onRotationHandleDragEnd: function () {
        this._rotationHandleDragging = false;
        this._updateRotationHandle();
        this.fire('rotated', { rotation: this.getRotation() });
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
                    this.redraw();
                    break;
                case 'rotation':
                case 'font-rotation':
                    this.setRotation(style[i]);
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

            var text = (this.options.text || '').replace(/ /g, '\u00A0').split('\n');  // Non breakable spaces
            this._textNode.innerHTML = this._textNode2.innerHTML = '';

            for (var t in text) {
                var tspan = L.SVG.create('tspan');
                var tspan2 = L.SVG.create('tspan');

                tspan.setAttribute('x', point.x);
                tspan.setAttribute('dy', t > 0 ? this.options.fontSize * 1.3 : 0);

                tspan2.setAttribute('x', point.x);
                tspan2.setAttribute('dy', t > 0 ? this.options.fontSize * 1.3 : 0);

                if (this.options.textAnchor) {
                    tspan.setAttribute('text-anchor', this.options.textAnchor);
                    tspan2.setAttribute('text-anchor', this.options.textAnchor);
                }

                tspan.appendChild(document.createTextNode(text[t]));
                tspan2.appendChild(document.createTextNode(text[t]));

                this._textNode.appendChild(tspan);
                this._textNode2.appendChild(tspan2);
            }

            this._textNode.setAttribute('x', point.x);
            this._textNode.setAttribute('y', point.y);

            this._textNode2.setAttribute('x', point.x);
            this._textNode2.setAttribute('y', point.y);

            var rotation = this.getRotation();
            if (rotation) {
                var transform = 'rotate(' + rotation + ' ' + point.x + ' ' + point.y + ')';
                this._textNode.setAttribute('transform', transform);
                this._textNode2.setAttribute('transform', transform);
            } else {
                this._textNode.removeAttribute('transform');
                this._textNode2.removeAttribute('transform');
            }

            this._setRefSize();
            this._updateRotationHandle();
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

            this.setStyle({'fill': this.options.fontColor });
            this.setStyle({'font-size': this.options.fontSize });
            this.setStyle({'font-style': this.options.fontStyle });

            svg.childNodes[0].appendChild(this._textNode2);
            svg.childNodes[0].appendChild(this._textNode);
        }
    },

    _mapClick: function (e) {
        if (this._pos && this._textNode) {
            var clickPoint = this._map.latLngToLayerPoint(e.latlng);
            var rotation = this.getRotation();
            if (rotation) {  // getBBox is unrotated => rotate the click point back around the insertion point
                var anchor = this._map.latLngToLayerPoint(this._pos);
                clickPoint = anchor.add(this._rotatePoint(clickPoint.subtract(anchor), -rotation));
            }
            var bbox = this._textNode.getBBox();
            
            if (clickPoint.x >= bbox.x && clickPoint.y >= bbox.y &&
                clickPoint.x <= bbox.x + bbox.width && clickPoint.y <= bbox.y + bbox.height) {

                e.originalEvent.preventDefault();
                this.fireEvent('click', e);
            }
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
        } catch(ex) { return null; }
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

        this._textNode.style['font-size'] = this._textNode2.style['font-size'] = this.options.fontSize + 'px';
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

// => [0, 360)
L.SVGText.normalizeRotation = function (rotation) {
    rotation = Math.round(rotation * 10) / 10 % 360;
    return rotation < 0 ? rotation + 360 : rotation;
};

// SHIFT => 5° steps, CTRL => 15° steps, always: snap to 0/90/180/270 (±2°)
L.SVGText.snapRotation = function (rotation, shift, ctrl) {
    var step = ctrl ? 15 : (shift ? 5 : 0);
    if (step) {
        rotation = Math.round(rotation / step) * step;
    } else {
        var right = Math.round(rotation / 90) * 90;
        rotation = Math.abs(rotation - right) <= 2 ? right : Math.round(rotation);
    }
    return L.SVGText.normalizeRotation(rotation);
};

L.svgText = function (latLng, options) {
    return new L.SVGText(latLng, options);
};
