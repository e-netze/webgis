// Geometry type 'rectangle'
// Two vertices (corners), the second vertex is replaced while drawing.
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults)

webgis.sketch.geometryTypes.define('rectangle', '_base', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.createFrameworkElement = function (latLngs) {
        var rect = L.rectangle(latLngs || [[0, 0], [0, 0]], { interactive: true, color: sketch.getColor(), fillColor: sketch.getFillColor(), opacity: sketch.getOpacity(), fillOpacity: sketch.getFillOpacity(), weight: sketch.getWeight(), dashArray: sketch._toDashArray(sketch.getLineStyle()) });
        rect._webgis = { geometryType: state.geometryType, lineStyle: sketch.getLineStyle() };
        return rect;
    };
    this.isValid = function () {
        return state.vertices.length > 1;
    };
    this.fromJson = function (json, crsId, readOnly) {
        if (json.coordinates.length === 1) {
            var coordinateArray = json.coordinates[0], minx, miny, maxx, maxy;
            for (var i = 0; i < coordinateArray.length; i++) {
                if (i === 0) {
                    minx = maxx = coordinateArray[i][0];
                    miny = maxy = coordinateArray[i][1];
                } else {
                    minx = Math.min(coordinateArray[i][0], minx);
                    miny = Math.min(coordinateArray[i][1], miny);
                    maxx = Math.max(coordinateArray[i][0], maxx);
                    maxy = Math.max(coordinateArray[i][1], maxy);
                }
            }

            sketch.addVertices([{ x: minx, y: maxy }, { x: maxx, y: miny }]);
        }
    };
    this.cloneGraphicElements = function () {
        var elements = [];
        var frameworkElements = state.frameworkElements, currentFrameworkIndex = state.currentFrameworkIndex;

        var bounds = frameworkElements[currentFrameworkIndex].getBounds();
        if (bounds.getWest() !== bounds.getEast() && bounds.getSouth() !== bounds.getNorth()) {
            var element = sketch._createFrameworkElement();
            element.setBounds(frameworkElements[currentFrameworkIndex].getBounds());
            elements.push(element);
        }

        return elements;
    };

    this.tryAddVertex = function (vertex, vertices, fireEvents, coords) {
        if (fireEvents) {
            sketch.events.fire('beforeaddvertex', sketch, vertex);
        }

        var sketchVertices = state.vertices, vertexFrameworkElements = state.vertexFrameworkElements;
        var rectBounds = [[0, 0], [0, 0]];
        if (sketchVertices.length === 0) {
            sketchVertices.push(vertex);
            rectBounds = [[sketchVertices[0].y, sketchVertices[0].x], [sketchVertices[0].y, sketchVertices[0].x]];

            vertexFrameworkElements[sketchVertices.length - 1] = webgis.createMarker({
                lat: vertex.y, lng: vertex.x, draggable: true, icon: "sketch_vertex", vertex: sketchVertices.length
            });
            sketch._addToMap(vertexFrameworkElements[sketchVertices.length - 1]);
            vertexFrameworkElements[sketchVertices.length - 1].sketch_vertex_coords = vertex;
            vertexFrameworkElements[sketchVertices.length - 1].sketch = sketch;
            vertexFrameworkElements[sketchVertices.length - 1].vertex_index = sketchVertices.length - 1;
            sketch._appendMarkerEvents(vertexFrameworkElements[sketchVertices.length - 1]);
        }
        else if (sketchVertices.length === 1) {
            sketchVertices.push(vertex);
            rectBounds = [[sketchVertices[0].y, sketchVertices[0].x], [sketchVertices[1].y, sketchVertices[1].x]];
        }
        else if (sketchVertices.length > 1) {
            sketchVertices[1].y = vertex.y, sketchVertices[1].x = vertex.x;
            rectBounds = [[sketchVertices[0].y, sketchVertices[0].x], [sketchVertices[1].y, sketchVertices[1].x]];
        }
        state.frameworkElements[0].setBounds(rectBounds);

        return true;
    };
    this.addsVertexMarker = function () {
        return true;
    };

    this.handleMapMouseMove = function (latlng) {
        if (sketch.isClosed()) {
            return true;
        }

        var sketchVertices = state.vertices;
        if (sketchVertices.length > 0) {
            var rectBounds = [[latlng.lat, latlng.lng], [sketchVertices[0].y, sketchVertices[0].x]];

            if (sketch._moverLine === null || !sketch._moverLine.setBounds) {
                sketch.hideMoverLine();
                sketch._moverLine = L.rectangle(rectBounds, { color: 'green', weight: 1, fillColor: 'none' });
                sketch._addToMap(sketch._moverLine);
            }
            else {
                sketch._moverLine.setBounds(rectBounds);
            }
        }
        return true;
    };

    this.onVertexMarkerDragEnd = function (marker, latlng) {
        var frameworkElements = state.frameworkElements;
        if (frameworkElements.length === 1) {
            var rectBounds = frameworkElements[0].getBounds();
            state.vertices[0] = { x: marker.sketch_vertex_coords.x, y: marker.sketch_vertex_coords.y };
            frameworkElements[0].setBounds([[marker.sketch_vertex_coords.y, marker.sketch_vertex_coords.x], [rectBounds.getSouth(), rectBounds.getEast()]]);
        }
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'rectangle';
    };
});
