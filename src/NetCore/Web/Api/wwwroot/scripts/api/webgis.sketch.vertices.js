// Vertex model of webgis.sketch (webgis.sketch.js)
//   vertices  => flat array of all vertices of all parts
//   partIndex => index of the first vertex of each part (starts with 0)
//                if the last part start equals vertices.length, the last part is closed
//                and the next vertex starts a new part
//
// Contains everything that only calculates with the vertices (parts, selection, fix, move, rotate, ...).
// Events, undo, markers and map elements stay in the sketch.
//
// The vertices array is a stable reference (sketch._getRawVertices() returns it):
// it is always changed in place, never reassigned.
// The part index is private, use the methods to read or change it.

webgis.sketch.vertices = function () {
    "use strict";

    var _vertices = [], _partIndex = [0];
    var _static = webgis.sketch.vertices;

    this.vertices = _vertices;

    /********* Parts (read) *********/
    this.count = function () { return _partIndex.length; };
    this.start = function (partNumber) { return _partIndex[partNumber]; };
    // exclusive
    this.end = function (partNumber) {
        return partNumber < _partIndex.length - 1 ? _partIndex[partNumber + 1] : _vertices.length;
    };
    this.currentPartStart = function () { return _partIndex[_partIndex.length - 1]; };
    this.currentPartLength = function () {
        if (_partIndex.length <= 1)
            return _vertices.length;

        return Math.max(0, _vertices.length - this.currentPartStart());
    };
    this.partOf = function (vertexIndex) { return _static.partOf(_partIndex, vertexIndex); };
    // differs from partOf, if the part index does not start with 0 (see addPrevVertex(0)) or for negative indices
    this.partNumber = function (vertexIndex) {
        for (var p = 0, to = _partIndex.length - 1; p < to; p++) {
            if (_partIndex[p] <= vertexIndex && _partIndex[p + 1] > vertexIndex) {
                return p;
            }
        }
        return _partIndex.length - 1;
    };
    this.isPartStart = function (index) { return _partIndex.indexOf(index) >= 0; };
    this.isFirstPartVertex = function (index) { return _static.isFirstPartVertex(_partIndex, index); };
    this.isClosed = function () {
        return _partIndex.length > 1 && this.currentPartStart() == _vertices.length;
    };
    this.isLastPartClosed = function () {
        return _partIndex.length > 0 && _vertices.length == this.currentPartStart();
    };
    this.partVertices = function (partNumber) {
        if (partNumber < 0 || partNumber >= _partIndex.length)
            return [];

        var vertices = [];
        for (var i = _partIndex[partNumber], to = this.end(partNumber); i < to; i++) {
            vertices.push(_vertices[i]);
        }
        return vertices;
    };
    // copy
    this.getPartIndex = function () { return _partIndex.slice(0); };
    // deep copy of the vertices (undo)
    this.snapshot = function () {
        var vertices = [];
        for (var i = 0, to = _vertices.length; i < to; i++) {
            vertices.push(_static.copyVertex(_vertices[i]));
        }
        return { vertices: vertices, partIndex: this.getPartIndex() };
    };

    /********* Vertices (read) *********/
    // fn(vertex) for all vertices of all parts (part order) => array of the results
    this.collect = function (fn) {
        var result = [];
        for (var p = 0, pTo = _partIndex.length; p < pTo; p++) {
            var partVertices = this.partVertices(p);
            for (var i = 0; i < partVertices.length; i++) {
                result.push(fn(partVertices[i]));
            }
        }
        return result;
    };
    this.allHaveProperty = function (propertyName) {
        for (var p = 0, pTo = _partIndex.length; p < pTo; p++) {
            var partVertices = this.partVertices(p);
            for (var i = 0; i < partVertices.length; i++) {
                if (!partVertices[i].hasOwnProperty(propertyName)) {
                    return false;
                }
            }
        }
        return true;
    };
    // vertex with its neighbors in the same part, closedRing => first and last vertex of a part are neighbors
    this.neighbors = function (index, includeVertex, closedRing) {
        var vertices = [];
        if (!(index < _vertices.length)) {  // also NaN
            return vertices;
        }

        var partNumber = this.partNumber(index);
        var min = partNumber == 0 ? 0 : _partIndex[partNumber];
        var max = this.end(partNumber) - 1;

        if (closedRing && index == min) {
            vertices.push({ x: _vertices[max].x, y: _vertices[max].y, index: max });
        }
        for (var i = Math.max(min, index - 1), to = Math.min(index + 1, max); i <= to; i++) {
            if (i !== index || includeVertex === true) {
                vertices.push({ x: _vertices[i].x, y: _vertices[i].y, index: i });
            }
        }
        if (closedRing && index == max) {
            vertices.push({ x: _vertices[min].x, y: _vertices[min].y, index: min });
        }
        return vertices;
    };
    // indices (strings, for..in) of the vertices inside the bounds (minX <= maxX, minY <= maxY)
    this.indicesInBounds = function (minX, minY, maxX, maxY) {
        var indices = [];
        for (var i in _vertices) {
            var vertex = _vertices[i];
            if (vertex.x >= minX && vertex.x <= maxX &&
                vertex.y >= minY && vertex.y <= maxY) {
                indices.push(i);
            }
        }
        return indices;
    };
    // one L.latLng array per part, closeRings => first latLng is appended to parts with more than 2 latLngs
    this.toLatLngs = function (closeRings) {
        var parts = [], latLngs = null, partNumber = 0;
        var close = function () {
            if (latLngs != null && closeRings === true && latLngs.length > 2) {
                latLngs.push(L.latLng(latLngs[0].lat, latLngs[0].lng));
            }
        };
        for (var i = 0; i < _vertices.length; i++) {
            if (i === 0 || _partIndex[partNumber] === i) {
                close();
                latLngs = [];
                parts.push(latLngs);
                partNumber++;
            }
            latLngs.push(L.latLng(_vertices[i].y, _vertices[i].x));
        }
        close();
        return parts;
    };

    /********* Selection / fix *********/
    this.hasSelected = function () {
        for (var i in _vertices) {
            if (_vertices[i].selected === true) {
                return true;
            }
        }
        return false;
    };
    // first selected index (string, for..in) or null
    this.firstSelectedIndex = function () {
        for (var i in _vertices) {
            if (_vertices[i].selected === true) {
                return i;
            }
        }
        return null;
    };
    // select: true, false or undefined => toggle
    this.select = function (index, select) {
        _setFlag(index, 'selected', select);
    };
    this.selectAll = function (select) {
        for (var i in _vertices) {
            _setFlag(i, 'selected', select);
        }
    };
    // fix: true, false or undefined => toggle; returns false for an invalid index
    this.fix = function (index, fix) {
        return _setFlag(index, 'fixed', fix);
    };
    var _setFlag = function (index, flag, value) {
        if (index < 0 || index >= _vertices.length) {
            return false;
        }
        if (value === true) {
            _vertices[index][flag] = true;
        } else if (value === false) {
            _vertices[index][flag] = false;
        } else {  // toogle
            _vertices[index][flag] = _vertices[index][flag] === true ? false : true;
        }
        return true;
    };
    // fixed vertices never move, if there are selected vertices only these move
    this.isMovable = function (vertex, onlySelected) {
        return !(vertex.fixed === true || (onlySelected && vertex.selected !== true));
    };

    /********* Move / rotate *********/
    this.translate = function (deltaX, deltaY, onlySelected) {
        for (var i = 0; i < _vertices.length; i++) {
            var vertex = _vertices[i];
            if (this.isMovable(vertex, onlySelected)) {
                _vertices[i] = { x: vertex.x + deltaX, y: vertex.y + deltaY, selected: vertex.selected };
            }
        }
    };
    // center: projected { X, Y }, angle in radians
    this.rotate = function (center, angle, crs, onlySelected) {
        var cosA = Math.cos(angle), sinA = Math.sin(angle);

        for (var i = 0; i < _vertices.length; i++) {
            var vertex = _vertices[i];
            if (this.isMovable(vertex, onlySelected)) {
                vertex = { x: _vertices[i].x, y: _vertices[i].y };
                webgis.complementProjected(crs, [vertex]);

                var dX = vertex.X - center.X;
                var dY = vertex.Y - center.Y;

                var rVertex = {
                    X: cosA * dX - sinA * dY + center.X,
                    Y: sinA * dX + cosA * dY + center.Y,
                    selected: _vertices[i].selected
                };
                webgis.complementWGS84(crs, [rVertex]);

                _vertices[i] = rVertex;
            }
        }
    };
    // preview: latLngParts from toLatLngs(true), changed in place.
    // The movable rule is applied with a running index over all latLngs (incl. closing latLngs)
    this.translateLatLngs = function (latLngParts, deltaLng, deltaLat, onlySelected) {
        _forEachMovableLatLng(this, latLngParts, onlySelected, function (latLng) {
            latLng.lng += deltaLng;
            latLng.lat += deltaLat;
        });
    };
    this.rotateLatLngs = function (latLngParts, center, angle, crs, onlySelected) {
        _forEachMovableLatLng(this, latLngParts, onlySelected, function (latLng) {
            var vertex = { x: latLng.lng, y: latLng.lat };
            webgis.complementProjected(crs, [vertex]);

            var dX = vertex.X - center.X;
            var dY = vertex.Y - center.Y;

            var rVertex = {
                X: (Math.cos(angle) * dX - Math.sin(angle) * dY) + center.X,
                Y: (Math.sin(angle) * dX + Math.cos(angle) * dY) + center.Y
            };
            webgis.complementWGS84(crs, [rVertex]);

            latLng.lng = rVertex.x;
            latLng.lat = rVertex.y;
        });
    };
    var _forEachMovableLatLng = function (model, latLngParts, onlySelected, fn) {
        var vIndex = 0;
        for (var p in latLngParts) {
            var latLngs = latLngParts[p];
            for (var l in latLngs) {
                if (model.isMovable(_vertices[vIndex % _vertices.length], onlySelected)) {
                    fn(latLngs[l]);
                }
                vIndex++;
            }
        }
    };

    /********* Measure *********/
    // point at station s (projected length along all vertices, parts are not considered) => { x, y, X, Y } or null
    this.statPoint = function (s) {
        var stat = 0.0, stat0 = 0.0;
        if (_vertices.length == 0)
            return null;
        var x1 = _vertices[0].x, y1 = _vertices[0].y;
        var X1 = _vertices[0].X, Y1 = _vertices[0].Y;
        for (var i = 1, to = _vertices.length; i < to; i++) {
            var x2 = _vertices[i].x, y2 = _vertices[i].y;
            var X2 = _vertices[i].X, Y2 = _vertices[i].Y;
            stat0 = stat;
            var l = Math.sqrt((X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1));
            if (l > 0) {
                stat += l;
                if (stat >= s) {
                    var t = s - stat0;
                    var dx = (x2 - x1) / l, dy = (y2 - y1) / l;
                    var dX = (X2 - X1) / l, dY = (Y2 - Y1) / l;
                    var x = x1 + dx * t, y = y1 + dy * t;
                    var X = X1 + dX * t, Y = Y1 + dY * t;
                    return { x: x, y: y, X: X, Y: Y };
                }
            }
            x1 = x2;
            y1 = y2;
            X1 = X2;
            Y1 = Y2;
        }
        return null;
    };
    // extent of the vertices (x, y) => [minX, minY, maxX, maxY] or null (no vertices)
    this.extent = function () {
        if (_vertices.length === 0) return null;

        var minX = _vertices[0].x, minY = _vertices[0].y, maxX = _vertices[0].x, maxY = _vertices[0].y;
        for (var i = 1; i < _vertices.length; i++) {
            var vertex = _vertices[i];
            minX = Math.min(minX, vertex.x);
            minY = Math.min(minY, vertex.y);
            maxX = Math.max(maxX, vertex.x);
            maxY = Math.max(maxY, vertex.y);
        }
        return [minX, minY, maxX, maxY];
    };
    // bounds of the vertices (+ an optional additional vertex) or null
    // projected => { minX, maxX, minY, maxY, width, height } (X, Y)
    // otherwise => { minLng, maxLng, minLat, maxLat, width, height } (x, y)
    // width/height are only set, if all min/max values are truthy (a value of 0 => no width/height, kept as before)
    this.bounds = function (projected, addVertex) {
        var xp = projected ? 'X' : 'x', yp = projected ? 'Y' : 'y';
        var minX = projected ? 'minX' : 'minLng', maxX = projected ? 'maxX' : 'maxLng',
            minY = projected ? 'minY' : 'minLat', maxY = projected ? 'maxY' : 'maxLat';
        var bounds = null;
        var add = function (vertex) {
            if (bounds === null) {
                bounds = {};
                bounds[minX] = bounds[maxX] = vertex[xp];
                bounds[minY] = bounds[maxY] = vertex[yp];
            } else {
                bounds[minX] = Math.min(vertex[xp], bounds[minX]);
                bounds[maxX] = Math.max(vertex[xp], bounds[maxX]);
                bounds[minY] = Math.min(vertex[yp], bounds[minY]);
                bounds[maxY] = Math.max(vertex[yp], bounds[maxY]);
            }
        };
        for (var i = 0; i < _vertices.length; i++) {
            add(_vertices[i]);
        }
        if (addVertex) {
            add(addVertex);
        }
        if (bounds && bounds[minX] && bounds[minY] && bounds[maxX] && bounds[maxY]) {
            bounds.width = Math.abs(bounds[maxX] - bounds[minX]);
            bounds.height = Math.abs(bounds[maxY] - bounds[minY]);
        }
        return bounds;
    };

    /********* Closest vertex/edge (screen points) *********/
    // toPoint(vertex) => { x, y } (e.g. map layer point), pos => { x, y }
    // => { index (string, for..in), vertex, dist } or null
    this.closestVertex = function (toPoint, pos) {
        var index = -1, dist = 0;
        for (var v in _vertices) {
            var point = toPoint(_vertices[v]);
            var d = (point.x - pos.x) * (point.x - pos.x) + (point.y - pos.y) * (point.y - pos.y);
            if (index < 0 || d < dist) {
                index = v;
                dist = d;
            }
        }
        if (index >= 0) {
            return {
                index: index,
                vertex: _vertices[index],
                dist: Math.sqrt(dist)
            };
        }
        return null;
    };
    // hasClosingSegment => the segment last => first vertex of each part is included
    // => { index (segment start vertex), t (0..1), dist } or null
    this.closestEdge = function (toPoint, pos, hasClosingSegment) {
        if (_vertices.length < 2) {
            return null;
        }
        var index = -1, dist = 0, t = .5, to = _vertices.length - 1, currentPartIndex = 0;
        if (hasClosingSegment)
            to++;
        for (var i = 0; i < to; i++) {
            var v1 = _vertices[i], v2 = null;
            var nextPartIndex = (i + 1) >= _vertices.length ? currentPartIndex + 1 : this.partOf(i + 1);
            if (nextPartIndex > currentPartIndex) {
                if (hasClosingSegment) {
                    v2 = _vertices[_partIndex[currentPartIndex]];
                }
                currentPartIndex = nextPartIndex;
            }
            else {
                v2 = _vertices[i + 1];
            }
            if (!v2)
                continue;
            var p1 = toPoint(v1), p2 = toPoint(v2);
            var rx = p2.x - p1.x, ry = p2.y - p1.y;
            var lx = pos.x - p1.x, ly = pos.y - p1.y;
            var linEq = new webgis.calc.linearEquation2(lx, ly, rx, -ry, ry, rx);
            if (linEq.solve()) {
                var t1 = linEq.var1();
                if (t1 >= 0 && t1 <= 1.0) {
                    var p = {
                        x: p1.x + t1 * rx,
                        y: p1.y + t1 * ry
                    };
                    var d = (pos.x - p.x) * (pos.x - p.x) + (pos.y - p.y) * (pos.y - p.y);
                    if (index < 0 || d < dist) {
                        dist = d;
                        index = i;
                        t = t1;
                    }
                }
            }
        }
        if (index >= 0) {
            return {
                index: index,
                t: t,
                dist: Math.sqrt(dist)
            };
        }
        return null;
    };

    /********* Change (in place) *********/
    this.clear = function () {
        _vertices.splice(0, _vertices.length);
        _partIndex.splice(0, _partIndex.length);
        _partIndex.push(0);
    };
    // replaces the vertices, the part index is kept
    this.setVertices = function (vertices) {
        _vertices.splice(0, _vertices.length);
        for (var i = 0; i < vertices.length; i++) {
            _vertices.push(vertices[i]);
        }
    };
    this.setPartIndex = function (partIndex) {
        _partIndex.splice(0, _partIndex.length);
        for (var i = 0; i < partIndex.length; i++) {
            _partIndex.push(partIndex[i]);
        }
    };
    this.removeVertex = function (index) {
        _vertices.splice(index, 1);

        for (var p = 0; p < _partIndex.length; p++) {
            if (_partIndex[p] > index)
                _partIndex[p] = _partIndex[p] - 1;
        }
    };
    // the next vertex starts a new part
    this.closePart = function () {
        if (!this.isPartStart(_vertices.length))
            _partIndex.push(_vertices.length);
    };
};

/********* Static helpers (work with any part index array) *********/
webgis.sketch.vertices.partOf = function (partIndex, vertexIndex) {
    if (partIndex == null || partIndex.length <= 1)
        return 0;
    for (var p = 1, to = partIndex.length; p < to; p++) {
        if (partIndex[p] > vertexIndex)
            return p - 1;
    }
    return partIndex.length - 1;
};
webgis.sketch.vertices.isFirstPartVertex = function (partIndex, index) {
    return index > 0 && partIndex.indexOf(index) >= 0;
};
// part index of the same parts in reversed vertex order
webgis.sketch.vertices.reversedPartIndex = function (partIndex, vertexCount) {
    var reversed = [0], last = vertexCount;
    for (var i = partIndex.length - 1; i > 0; i--) {
        reversed.push(reversed[reversed.length - 1] + (last - partIndex[i]));
        last = partIndex[i];
    }
    return reversed;
};
webgis.sketch.vertices.copyVertex = function (vertex) {
    return {
        x: vertex.x, y: vertex.y,
        X: vertex.X, Y: vertex.Y,
        projEvent: vertex.projEvent,
        fixed: vertex.fixed,
        selected: vertex.selected,
        m: vertex.m,
        z: vertex.z
    };
};