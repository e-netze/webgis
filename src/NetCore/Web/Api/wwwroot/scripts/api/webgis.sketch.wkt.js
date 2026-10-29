// Sketch component (static): WKT export of the vertex model (webgis.sketch.vertices)
// Used by the sketch facade (toWKT, toWKT2). No state => all inputs are parameters.
// See docs/architecture/sketch-geometry-types.md

webgis.sketch.wkt = new function () {
    "use strict";

    // "x y" (WGS84) or "X Y [meta:..] [srs:..] [m:..] [z:..]"
    var _vertex = function (vertex, useWGS84, appendMeta, roundFactor) {
        if (useWGS84 === true) {
            return vertex.x + ' ' + vertex.y;
        }

        var X = vertex.X, Y = vertex.Y;
        if (roundFactor > 1.0) {
            X = Math.round(X * roundFactor) / roundFactor;
            Y = Math.round(Y * roundFactor) / roundFactor;
        }
        var wkt = X + ' ' + Y;
        if (appendMeta) {
            if (vertex.projEvent && vertex.projEvent !== 'original' && vertex.projEvent !== 'snapped') {
                wkt += ' meta:' + vertex.projEvent;
            }
            if (vertex.srs) {
                wkt += ' srs:' + vertex.srs;
            }
            if (typeof vertex.m === 'number') {  // can be null or undefined also
                wkt += ' m:' + vertex.m;
            }
            if (typeof vertex.z === 'number') {
                wkt += ' z:' + vertex.z;
            }
        }
        return wkt;
    };
    this.vertex = _vertex;

    // always MULTI<wktType>, vertices from the geometry type (wktVertices), in fan mode one part per ray
    this.toWKT = function (model, geometryTypeDef, useWGS84, fanMode) {
        if (model.vertices.length == 0) {
            return '';
        }

        let wkt = '';

        if (geometryTypeDef.wktType) {
            wkt += "MULTI" + geometryTypeDef.wktType + "(";
        }

        var vertices = geometryTypeDef.wktVertices(useWGS84);
        var addVertexFunc = function (wkt, vertex) {
            return wkt + _vertex(vertex, useWGS84, true, 0);
        };

        wkt += "(";
        for (var i = 0; i < vertices.length; i++) {
            if (i > 0) {
                if (fanMode && i > 1) {
                    wkt += '),(';
                    wkt = addVertexFunc(wkt, vertices[0]);
                    wkt += ',';
                }
                else if (i < vertices.length - 1 && model.isPartStart(i)) {
                    wkt += '),(';
                }
                else {
                    wkt += ',';
                }
            }
            wkt = addVertexFunc(wkt, vertices[i]);
        }
        wkt += ")";
        if (wkt !== '')
            wkt += ")";

        return wkt;
    };

    // MULTI only for real multiparts, polygons are closed, optional rounding (digits)
    this.toWKT2 = function (model, wktType, useWGS84, appendMeta, digits) {
        var vertices = model.vertices;
        if (vertices.length == 0)
            return '';

        let isMultipart = model.count() > 2 || (model.count() == 2 && vertices.length > model.start(1));  // its no an multipart if only the first is "closed";
        webgis.log('isMultipart', isMultipart, model.getPartIndex());

        let wkt = '', closeVertex = null;
        if (wktType === 'POLYGON') {
            wkt += isMultipart ? "MULTIPOLYGON((" : "POLYGON((";
            closeVertex = vertices.length > 0 ? vertices[0] : null;
        }
        else if (wktType) {
            wkt += (isMultipart ? "MULTI" + wktType : wktType) + "(";
        }
        let roundFactor = 0.0;

        if (digits && digits > 0) {
            roundFactor = Math.pow(10.0, digits);
        }

        var addVertexFunc = function (wkt, vertex) {
            return wkt + _vertex(vertex, useWGS84, appendMeta, roundFactor);
        };

        wkt += isMultipart ? "(" : "";
        for (var i = 0; i < vertices.length; i++) {
            if (i > 0) {
                if (i < vertices.length - 1 && model.isPartStart(i)) {
                    if (closeVertex) {
                        wkt = addVertexFunc(wkt + ',', closeVertex);
                        closeVertex = vertices[i];
                    }
                    wkt += '),(';
                } else {
                    wkt += ',';
                }
            }
            wkt = addVertexFunc(wkt, vertices[i]);
        }

        if (closeVertex) {
            wkt = addVertexFunc(wkt + ',', closeVertex);
        }

        wkt += isMultipart ? ")" : "";
        if (wkt !== '') {
            if (wktType === 'POLYGON') {
                wkt += ")";
            }
            wkt += ")";
        }
        return wkt;
    };
};