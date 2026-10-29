// Base geometry type for webgis.sketch (webgis.sketch.js) and the shared families
//   _base    => defaults for all hooks and flags + shared helpers (json, ...)
//               Unknown geometry types use _base directly
//   _line    => polyline, dimline, hectoline
//   _polygon => polygon, dimpolygon
//   _radius  => circle, distance_circle, compass_rose
//
// The types itself are defined in webgis.sketch.geometry.<type>.js
// The registry is defined in webgis.sketch.geometry.js
//
// Never cache state.vertices (or other state values) in a type instance.
// The sketch reassigns these arrays (eg. text, reverse, mergeParts, redrawMarkers)

webgis.sketch.geometryTypes.define('_base', null, function (ctx) {
    "use strict";

    var $ = webgis.$;
    var sketch = ctx.sketch, state = ctx.state;

    /********* Flags *********/
    this.isSimplePoint = false;
    this.isLine = false;
    this.isPolygon = false;
    this.isLineOrPolygon = false;
    this.isPlainPath = false;                       // polyline, polygon: special marker click and click tolerance handling
    this.hasClosingSegment = false;                 // polygon: last vertex connects to first vertex of the part
    this.frameworkElementIsVertexMarker = false;    // point: framework element is the draggable vertex marker
    this.redrawOnVertexMoved = false;
    this.supportsMergeParts = false;
    this.createsFrameworkElementPerPart = true;
    this.styleIndex = 0;
    this.addVertexUndoKey = null;                   // l10n key
    this.vertexMarkerIcon = null;                   // fix icon for vertex markers (otherwise sketch_vertex[_fixed|_selected])
    this.wktType = null;                            // POINT, LINESTRING, POLYGON
                                                    // null => toWKT/toWKT2 write no valid WKT (no prefix), the server (ShapeFromWKT) ignores it => intended

    /********* Hooks *********/
    this.createFrameworkElement = function (latLngs) {
        return undefined;
    };
    this.isValid = function () {
        return false;
    };
    this.calcProperties = function (xParam, yParam) {
        return {};
    };
    this.infoTextLines = function (infoText, properties) { };
    this.metaInfo = function (info) { };
    this.toJson = function (ret, crsId) { };     // only implemented for point, polyline and polygon => other types return { geometry: {}, valid: false } (intended)
    this.fromJson = function (json, crsId, readOnly) {
        this._fromJsonList(json, crsId);
    };
    this.fromJsonFast = function (json, crsId) {
        this._fromJsonFastList(json, crsId);
    };
    this.wktVertices = function (useWGS84) {
        return state.vertices;
    };
    this.cloneGraphicElements = function () {
        return null;  // null => sketch uses the default implementation
    };

    // addVertices
    this.beforeAddVertex = function (vertex) { };
    this.tryAddVertex = function (vertex, vertices, fireEvents, coords) {
        return false;  // true => vertex is handled by the type
    };
    this.pushVertex = function (coords) {
        state.vertices.push(coords);
        sketch.snapFixVertex(coords);
    };
    this.addsVertexMarker = function () {
        return false;
    };

    // map events
    this.onMapMouseUp = function () { };
    this.handleMapMouseMove = function (latlng) {
        return false;  // true => handled, sketch stops mouse move handling
    };
    this.handleFreeDrawMouseMove = function (latlng) {
        return false;  // true => handled, sketch stops mouse move handling
    };

    // vertex marker events
    this.onVertexMarkerDrag = function (marker, latlng, latLngs) {
        marker.sketch.showMoverLine(latLngs);
    };
    this.onVertexMarkerDragEnd = function (marker, latlng) { };

    // read type specific values from a framework element (and set jsonGeometry.type)
    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) { };

    /********* Helpers *********/
    this._jsonVertex = function (json, xy, XY) {
        var XYIndex = 0;

        var vertex = { x: xy[0], y: xy[1], X: XY[XYIndex++], Y: XY[XYIndex++], projEvent: 'original' };
        if (json.hasZ === true && XYIndex < XY.length) {
            vertex.z = XY[XYIndex++];
        }
        if (json.hasM === true && XYIndex < XY.length) {
            vertex.m = XY[XYIndex++];
        }

        return vertex;
    };

    // { coordinates: [x,y] }
    this._fromJsonSingle = function (json, crsId) {
        var coordinates = crsId ? webgis.unproject(crsId, json.coordinates) : json.coordinates;
        if (json.COORDINATES && json.COORDINATES.length > 1) {
            sketch.addVertex(this._jsonVertex(json, coordinates, json.COORDINATES));
        }
        else {
            sketch.addVertexCoords(coordinates[0], coordinates[1]);
        }
    };

    // { coordinates: [[[x,y],...],...] }
    this._fromJsonRings = function (json, crsId, readOnly) {
        for (var r = 0; r < json.coordinates.length; r++) {
            var coordinateArray = json.coordinates[r];
            var COORDINATEArray = json.COORDINATES ? json.COORDINATES[r] : null;
            var firstPoint = true;
            var vertices = [];
            for (var i = 0; i < coordinateArray.length; i++) {
                var xy = coordinateArray[i];
                xy = crsId ? webgis.unproject(crsId, xy) : xy;
                if (i > 0 && i == coordinateArray.length - 1) {
                    if (coordinateArray[0][0] == xy[0] && coordinateArray[0][1] == xy[1])
                        continue;
                }
                if (firstPoint && state.vertices.length > 0) {
                    sketch.appendPart();
                }
                firstPoint = false;
                if (COORDINATEArray) {
                    vertices.push(this._jsonVertex(json, xy, COORDINATEArray[i]));
                }
                else {
                    vertices.push({ x: xy[0], y: xy[1] });
                }
            }
            if (vertices.length > 0) {
                sketch.addVertices(vertices, false, readOnly);
            }
        }
    };

    // { coordinates: [[x,y],...], partindex: [...] }
    this._fromJsonList = function (json, crsId) {
        var partIndex = json.partindex;
        for (var i = 0; i < json.coordinates.length; i++) {
            if (partIndex && $.inArray(i, partIndex) >= 0)
                sketch.appendPart();
            var xy = json.coordinates[i];
            xy = crsId ? webgis.unproject(crsId, xy) : xy;
            if (json.COORDINATES && json.COORDINATES.length > i) {
                sketch.addVertices([this._jsonVertex(json, xy, json.COORDINATES[i])]);
            }
            else {
                sketch.addVertices([{ x: xy[0], y: xy[1] }]);
            }
        }
    };

    this._fromJsonFastSingle = function (json, crsId) {
        var coordinates = crsId ? webgis.unproject(crsId, json.coordinates) : json.coordinates;
        if (json.COORDINATES && json.COORDINATES.length > 1) {
            sketch.addVertex({ x: coordinates[0], y: coordinates[1], X: json.COORDINATES[0], Y: json.COORDINATES[1], projEvent: 'original' });
        }
        else {
            sketch.addVertexCoords(coordinates[0], coordinates[1]);
        }
    };

    this._fromJsonFastRings = function (json, crsId) {
        sketch.appendPart(json.coordinates, true);
        for (var r = 0; r < json.coordinates.length; r++) {
            var coorindateArray = json.coordinates[r];
            for (var i = 0; i < coorindateArray.length; i++) {
                var xy = coorindateArray[i];
                xy = crsId ? webgis.unproject(crsId, xy) : xy;
                if (i > 0 && i == coorindateArray.length - 1) {
                    if (coorindateArray[0][0] == xy[0] && coorindateArray[0][1] == xy[1])
                        continue;
                }
                state.vertices.push({ x: xy[0], y: xy[1] });
            }
        }
    };

    this._fromJsonFastList = function (json, crsId) {
        for (var i = 0; i < json.coordinates.length; i++) {
            var xy = json.coordinates[i];
            xy = crsId ? webgis.unproject(crsId, xy) : xy;

            if (json.COORDINATES && json.COORDINATES.length > i) {
                var XY = json.COORDINATES[i];
                sketch.addVertex({ x: xy[0], y: xy[1], X: XY[0], Y: XY[1], projEvent: 'original' });
            }
            else {
                sketch.addVertexCoords(xy[0], xy[1]);
            }
        }
    };

    // polyline, polygon
    this._toJsonPath = function (ret, crsId, jsonType, minVertices) {
        var vertices = state.vertices;

        if (vertices.length >= minVertices) {
            ret.geometry.type = jsonType;
            ret.valid = true;
        }
        if (ret.valid) {
            if (crsId)
                ret.geometry.SRID = crsId;
            ret.geometry.coordinates = [];
            ret.geometry.COORDINATES = [];
            for (var i = 0; i < vertices.length; i++) {
                ret.geometry.coordinates.push(crsId ? webgis.project(crsId, [vertices[i].x, vertices[i].y]) : [vertices[i].x, vertices[i].y]);
                ret.geometry.COORDINATES.push([vertices[i].X, vertices[i].Y]);
            }
        }
    };
});

// polyline, dimline, hectoline
webgis.sketch.geometryTypes.define('_line', '_base', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.isLine = true;
    this.isLineOrPolygon = true;
    this.redrawOnVertexMoved = true;
    this.addVertexUndoKey = 'sketch-add-vertex';

    this.isValid = function () {
        return state.vertices.length > 1;
    };
    this.calcProperties = function (xParam, yParam) {
        var len = 0;
        for (var i = 0; i < sketch.partCount(); i++) {
            len += webgis.calc.length(sketch.partVertices(i), xParam, yParam);
        }
        return {
            length: len,
            set_values: true
        };
    };
    this.addsVertexMarker = function () {
        return true;
    };
});

// polygon, dimpolygon
webgis.sketch.geometryTypes.define('_polygon', '_base', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.isPolygon = true;
    this.isLineOrPolygon = true;
    this.redrawOnVertexMoved = true;
    this.addVertexUndoKey = 'sketch-add-vertex';

    this.isValid = function () {
        return state.vertices.length > 2;
    };
    this.calcProperties = function (xParam, yParam) {
        var circum = 0, area = 0;
        if (sketch.isValid()) {
            var partsVertices = [];
            for (var i = 0; i < sketch.partCount(); i++) {
                var partVertices = sketch.partVertices(i);
                partsVertices.push(partVertices);
            }

            // Check for self intersecting
            var selfIntersecting = webgis.calc.isSelfIntersecting(partsVertices, true);

            for (var i = 0; i < partsVertices.length; i++) {
                if (selfIntersecting == true) {
                    area = NaN;
                } else {
                    var otherRings = [];
                    // Jeden Ring mit allen anderen testen
                    for (var j = 0; j < partsVertices.length; j++) {
                        if (j != i) otherRings.push(partsVertices[j]);
                    }

                    if (webgis.calc.isRingsHole(otherRings, partsVertices[i])) {
                        area -= webgis.calc.area(partsVertices[i], xParam, yParam);
                    } else {
                        area += webgis.calc.area(partsVertices[i], xParam, yParam);
                    }
                }
                circum += webgis.calc.length(partsVertices[i], xParam, yParam, true);
            }
        }
        return {
            circumference: circum,
            area: area,
            set_values: true,
        };
    };
    this.fromJson = function (json, crsId, readOnly) {
        this._fromJsonRings(json, crsId, readOnly);
    };
    this.addsVertexMarker = function () {
        return true;
    };
});

// circle, distance_circle, compass_rose
// each type holds its own radius (this.radius)
webgis.sketch.geometryTypes.define('_radius', '_base', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.redrawOnVertexMoved = true;

    this.isValid = function () {
        return state.vertices.length > 0;
    };
    this.fromJson = function (json, crsId, readOnly) {
        this._fromJsonSingle(json, crsId);
    };

    // first vertex => center (default handling), following vertices => radius
    this.tryAddVertex = function (vertex, vertices, fireEvents, coords) {
        var sketchVertices = state.vertices, frameworkElements = state.frameworkElements;

        if (sketchVertices.length === 0)
            return false;

        if (frameworkElements.length === 1) {
            var vertex0 = sketchVertices[0], vertex1 = vertices[vertices.length - 1], radius = 0;
            if (vertex0.hasOwnProperty('X') && vertex0.hasOwnProperty('Y') && vertex1.hasOwnProperty('X') && vertex1.hasOwnProperty('Y')) {
                radius = Math.sqrt((vertex1.X - vertex0.X) * (vertex1.X - vertex0.X) + (vertex1.Y - vertex0.Y) * (vertex1.Y - vertex0.Y));
            } else {
                var latLng1 = L.latLng(vertices[vertices.length - 1].y, vertices[vertices.length - 1].x);
                var latLng2 = L.latLng(sketchVertices[0].y, sketchVertices[0].x);

                radius = latLng1.distanceTo(latLng2);
            }

            frameworkElements[0].setRadius(radius);

            // do not close sketch => sonst kann man nachträglich Radius und steps nicht mehr setzen
            this.radius = radius === 0 ? this.radius : radius;

            sketch.map.ui.refreshUIElements();
        }

        return true;
    };
    this.addsVertexMarker = function () {
        return state.vertices.length === 1;
    };

    this.handleMapMouseMove = function (latlng) {
        if (sketch.isClosed()) {
            return true;
        }

        var sketchVertices = state.vertices;
        if (sketchVertices.length > 0) {
            var latLng1 = latlng;
            var latLng2 = L.latLng(sketchVertices[0].y, sketchVertices[0].x);

            var r = latLng1.distanceTo(latLng2);
            var radius = latLng1.distanceTo(latLng2);

            if (sketch._moverLine === null || !sketch._moverLine.setRadius) {
                sketch.hideMoverLine();

                sketch._moverLine = L.circlePro([L.latLng(sketchVertices[0].y, sketchVertices[0].x)], { color: 'green', weight: 1, fillColor: 'none' });
                sketch._moverLine.setCirclePoint(latlng);

                sketch._addToMap(sketch._moverLine);
            }
            else {
                sketch._moverLine.setCirclePoint(latlng);
            }
        }
        return true;
    };
});
