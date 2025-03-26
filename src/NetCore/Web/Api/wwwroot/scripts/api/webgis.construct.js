var webgis_construct = function (map) {
    "use strict";

    var $ = webgis.$;

    this._topology = null;
    this._map = map;
    this._const_point = 0, this._const_mulitpoint = 1, this._const_polyline = 2, this._const_polygon = 3;
    /***** Helper ******/
    this._checkEnvelope = function (s, lng, lat, dist) {
        if (this._topology.envelopes != null) {
            var ll = this._topology.vertices_wgs84[this._topology.envelopes[s].ll];
            var ur = this._topology.vertices_wgs84[this._topology.envelopes[s].ur];
            if (lng < ll[0] - dist || lat < ll[1] - dist ||
                lng > ur[0] + dist || lat > ur[1] + dist)
                return false;
        }
        return true;
    };
    this._getMeta = function (s) {
        if (this._topology.meta && this._topology.meta_index)
            return this._topology.meta[this._topology.meta_index[s]];
        return null;
    };
    this._getShapeEndpointIndices = function (shape) {
        var ret = [];
        if (shape[0] == this._const_polyline || shape[0] == this._const_polygon) {
            var partCount = shape[1], index = 2;
            for (var p = 0; p < partCount; p++) {
                var vertexCount = shape[index];
                if (!this.isClipVertex(shape[index + 1]))
                    ret.push(shape[index + 1]);
                if (!this.isClipVertex(shape[index + vertexCount]))
                    ret.push(shape[index + vertexCount]);
                index += vertexCount + 1;
            }
        }
        return ret;
    };
    this._getShapeVertexIndices = function (shape) {
        var ret = [];
        if (shape[0] == this._const_point) {
            ret.push(shape[1]);
        }
        else if (shape[0] == this._const_multipoint) {
            var vertexCount = shape[1];
            for (var i = 1; i <= vertexCount; i++)
                ret.push(shape[i]);
        }
        else if (shape[0] == this._const_polyline || shape[0] == this._const_polygon) {
            var partCount = shape[1], partIndex = 2;
            for (var p = 0; p < partCount; p++) {
                var vertexCount = shape[partIndex];
                for (var i = 1; i <= vertexCount; i++) {
                    if (!this.isClipVertex(shape[partIndex + i]))
                        ret.push(shape[partIndex + i]);
                }
                partIndex += vertexCount + 1;
            }
        }
        return ret;
    };
    this._shapePointDistance = function (shape, x, y) {
        var dist = 1e10;
        var result = null;
        var vertices = null;
        if (shape[0] == this._const_polyline || shape[0] == this._const_polygon) {
            var partCount = shape[1], partIndex = 2;
            for (var p = 0; p < partCount; p++) {
                var vertexCount = shape[partIndex];
                for (var i = 1; i < vertexCount; i++) {
                    if (this.isClipVertex(shape[partIndex + i]) && this.isClipVertex(shape[partIndex + i + 1])) {
                        continue;
                    }

                    var v1 = this._topology.vertices_wgs84[shape[partIndex + i]];
                    var v2 = this._topology.vertices_wgs84[shape[partIndex + i + 1]];
                    var V1 = this._topology.vertices[shape[partIndex + i]];
                    var V2 = this._topology.vertices[shape[partIndex + i + 1]];
                    var d = this._linePointDistance(v1, v2, x, y, V1, V2);
                    if (d && d.dist < dist) {
                        dist = d.dist;
                        result = d.result;
                        vertices = [shape[partIndex + i], shape[partIndex + i + 1]];
                    }
                }
                partIndex += vertexCount + 1;
            }
        }
        if (result) {
            return { dist: dist, result: result, vertices: vertices };
        }

        return null;
    };
    this._linePointDistance = function (v1, v2, x, y, V1, V2) {
        var l0 = x - v1[0], l1 = y - v1[1];
        var a00 = v2[0] - v1[0], a10 = v2[1] - v1[1];
        var len = Math.sqrt(a00 * a00 + a10 * a10);
        if (len == 0.0) {
            //console.log('len == 0.0');
            return null;
        }
        var linEq = new webgis.calc.linearEquation2(l0, l1, a00, a10, a10, -a00);
        if (!linEq.solve()) {
            //console.log('cant solve');
            return null;
        }
        var t1 = linEq.var1();
        var t2 = linEq.var2();
        if (t1 < 0.0 || t1 > 1.0) {
            //console.log('outside', t1, v1, v2, x, y);
            return null;
        }
        var d = Math.abs(len * t2);
        return {
            dist: d,
            result: {
                x: v1[0] + a00 * t1,
                y: v1[1] + a10 * t1,
                X: V1 && V2 ? V1[0] + (V2[0] - V1[0]) * t1 : null,
                Y: V1 && V2 ? V1[1] + (V2[1] - V1[1]) * t1 : null
            }
        };
    };
    /******* General ********/
    this.getTopoVertex = function (index) {
        //console.log('getTopoVertex', index);
        if (Array.isArray(index) && index.length === 2)  // index is already a vertex => e.g. source is sketch
            return index;

        if (index == null || !this._topology || index < 0 || index >= this._topology.vertices.length)
            return null;

        return [this._topology.vertices[index][0], this._topology.vertices[index][1]];
    };
    this.getTopVertexIndex = function (X, Y) {
        if (!this._topology)
            return -1;
        for (var i = 0, to = this._topology.vertices.length; i < to; i++) {
            var dx = Math.abs(X - this._topology.vertices[i][0]), dy = Math.abs(Y - this._topology.vertices[i][1]);
            if (dx < 1e-7 && dy < 1e-7)
                return i;
        }
        return -1;
    };
    this.getTopoVertexWGS84 = function (index) {
        if (Array.isArray(index) && index.length === 2)  // index is already a vertex => e.g. source is sketch
            return index;

        if (index == null || !this._topology || index < 0 || index >= this._topology.vertices_wgs84.length)
            return null;
        return [this._topology.vertices_wgs84[index][0], this._topology.vertices_wgs84[index][1]];
    };
    this.isClipVertex = function (index) {
        if (index == null || !this._topology || index < 0 || index >= this._topology.vertices.length)
            return null;
        var v = this._topology.vertices[index];
        return v.length > 2 && v[2] == 'clip';
    };
    this.toProjectedVertex = function (crs, latlng) {
        var p = webgis.fromWGS84(crs, latlng.lat, latlng.lng);
        return { x: latlng.lng, y: latlng.lat, X: p.x, Y: p.y };
    };
    this.vertextDistance = function (v1, v2) {
        var dx = v1.X - v2.X, dy = v1.Y - v2.Y;
        return Math.sqrt(dx * dx + dy * dy);
    };
    this.closestVertex = function (vertex, candiates) {
        if (!candiates || candiates.length == 0)
            return null;
        var ret = candiates[0], dmin = this.vertextDistance(vertex, ret);
        for (var i = 1; i < candiates.length; i++) {
            var d = this.vertextDistance(vertex, candiates[i]);
            if (d < dmin) {
                dmin = d;
                ret = candiates[i];
            }
        }
        return ret;
    };
    this.closestVertexIndex = function (vertex, candiates) {
        if (!candiates || candiates.length == 0)
            return null;
        var ret = 0, dmin = this.vertextDistance(vertex, candiates[0]);
        for (var i = 1; i < candiates.length; i++) {
            var d = this.vertextDistance(vertex, candiates[i]);
            if (d < dmin) {
                dmin = d;
                ret = i;
            }
        }
        return ret;
    };
    this.shapesFromNodeIndex = function (nodeIndex) {
        var shapes = [], node = this.getTopoVertexWGS84(nodeIndex);
        for (var s = 0, to = this._topology.shapes.length; s < to; s++) {
            var shape = this._topology.shapes[s];
            if (!this._checkEnvelope(s, node[0], node[1], 1e-5))
                continue;
            if (shape[0] == this._const_point && shape[1] == nodeIndex)
                shapes.push(shape);
            else if (shape[0] == this._const_mulitpoint) {
                var pointCount = shape[1];
                for (var i = 2, to_i = pointCount; i < to_i; i++) {
                    if (shape[i] == nodeIndex) {
                        shapes.push(shape);
                        break;
                    }
                }
            }
            else if (shape[0] == this._const_polyline || shape[0] == this._const_polygon) {
                var partCount = shape[1], found = false, index = 2;
                for (var p = 0; p < partCount; p++) {
                    var pointCount = shape[index];
                    for (var i = 0; i < pointCount; i++) {
                        if (shape[index + i + 1] == nodeIndex) {
                            shapes.push(shape);
                            found = true;
                            break;
                        }
                    }
                    index += pointCount;
                    if (found)
                        break;
                }
            }
        }
        return shapes;
    };
    this.connectedNodes = function (nodeIndex) {
        var shapes = this.shapesFromNodeIndex(nodeIndex);
        var connectedNodes = [];
        for (var s = 0, to = shapes.length; s < to; s++) {
            var shape = shapes[s];
            if (shape[0] == this._const_mulitpoint) {
                var pointCount = shape[1];
                for (var i = 2, to_i = pointCount; i < to_i; i++) {
                    if (shape[i] == nodeIndex) {
                        if (i > 2)
                            connectedNodes.push(shape[i - 1]);
                        if (i < pointCount - 1)
                            connectedNodes.push(shape[i + 1]);
                    }
                }
            }
            else if (shape[0] == this._const_polyline || shape[0] == this._const_polygon) {
                var partCount = shape[1], found = false, index = 2;
                for (var p = 0; p < partCount; p++) {
                    var pointCount = shape[index];
                    //var isClosedPath = shape[index + 1] == shape[index + pointCount];
                    //console.log(isClosedPath);
                    for (var i = 0; i < pointCount; i++) {
                        if (shape[index + i + 1] == nodeIndex) {
                            if (i > 0 && !this.isClipVertex(shape[index + i])) {   // wenn nicht erster Punkt im Path -> Vorgänger hinzufügen
                                connectedNodes.push(shape[index + i]);
                            }
                            if (i < pointCount - 1 && !this.isClipVertex(shape[index + i + 2])) {  // Wenn nicht letzter Punkt im Pfad -> Nachfolger hinzufügen
                                connectedNodes.push(shape[index + i + 2]);
                            }
                        }
                    }
                    index += pointCount + 1;
                }
            }
        }
        var v0 = this.getTopoVertex(nodeIndex);
        // Distinct & calc Distance
        var ret = [];
        for (var i = 0, to = connectedNodes.length; i < to; i++) {
            if (!ret[connectedNodes[i]]) {
                var v1 = this.getTopoVertex(connectedNodes[i]);
                if (!v1)
                    continue;
                var dx = v0[0] - v1[0], dy = v0[1] - v1[1];
                ret[connectedNodes[i]] = Math.sqrt(dx * dx + dy * dy);
            }
        }
        //console.log(ret);
        return ret;
    };
    this._traceNodes = function (toNodeIndex, djisktra) {
        // find closest 
        var dist = 0, currentNodeIndex = null;
        for (var r in djisktra) {
            var node = djisktra[r];
            if (node.done == true)
                continue;
            if (currentNodeIndex == null || node.dist < dist) {
                dist = node.dist;
                currentNodeIndex = r;
            }
        }
        if (currentNodeIndex == null || currentNodeIndex == toNodeIndex)
            return;
        var currentNode = djisktra[currentNodeIndex];
        var connectedNodes = this.connectedNodes(currentNodeIndex);
        for (var c in connectedNodes) {
            if (c == currentNodeIndex)
                continue;
            var dist = connectedNodes[c];
            if (djisktra[c]) {
                if (djisktra[c].dist > currentNode.dist + dist) {
                    djisktra[c].dist = currentNode.dist + dist;
                    djisktra[c].pre = currentNodeIndex;
                    djisktra[c].done = false;
                }
            }
            else {
                djisktra[c] = {
                    index: c,
                    dist: currentNode.dist + dist,
                    pre: currentNodeIndex,
                    done: false
                };
            }
        }
        currentNode.done = true;
        this._traceNodes(toNodeIndex, djisktra);
    };
    this.traceNodes = function (fromNodeIndex, toNodeIndex) {
        var djisktra = [];
        djisktra[fromNodeIndex] = {
            index: fromNodeIndex,
            dist: 0,
            pre: null,
            done: false
        };
        this._traceNodes(toNodeIndex, djisktra);
        var toNode = djisktra[toNodeIndex], node = toNode;
        if (!toNode)
            return null;
        var result = [];
        while (node.pre) {
            result.push(node.index);
            node = djisktra[node.pre];
        }
        result.push(fromNodeIndex);
        //console.log('reached:' + fromNodeIndex + " -> " + toNodeIndex);
        //console.log(djisktra);
        //console.log(result);
        return result.reverse();
    };
    this.dist = function (X1, Y1, X2, Y2) {
        var dx = X1 - X2, dy = Y1 - Y2;
        return Math.sqrt(dx * dx + dy * dy);
    };
    this.distFromWGS84 = function (lng1, lat1, lng2, lat2) {
        var delta = this.deltaXYFromWGS84(lng1, lat1, lng2, lat2);

        return Math.sqrt(delta[0] * delta[0] + delta[1] * delta[1]);
    };
    this.deltaXYFromWGS84 = function(lng1, lat1, lng2, lat2) {
        var p1 = webgis.fromWGS84(this._map.calcCrs(), lat1, lng1);
        var p2 = webgis.fromWGS84(this._map.calcCrs(), lat2, lng2);
        var dx = p2.x - p1.x,
            dy = p2.y - p1.y;

        return [dx, dy];
    };
    this.azimutFromWGS84 = function (lng1, lat1, lng2, lat2) {
        var p1 = webgis.fromWGS84(this._map.calcCrs(), lat1, lng1);
        var p2 = webgis.fromWGS84(this._map.calcCrs(), lat2, lng2);
        var dx = p2.x - p1.x, dy = p2.y - p1.y;
        return ((90 - Math.atan2(dy, dx) * 180.0 / Math.PI) + 360.0) % 360.0;
    };
    /****** Snapping ********/
    this.initSnapping = function (topology) {
        this._topology = topology;
        //console.log('topology', topology);

        if (this._topology) {
            // Debug
            //for(var i in topology.vertices_wgs84) 
            //{
            //    var v = topology.vertices_wgs84[i];

            //    var m = webgis.createMarker({
            //        lat: v[1], lng: v[0], draggable: false, icon:  i>0 ? "query_result" : "blue", index: (i-1)
            //    });
            //    m.addTo(map.frameworkElement);
            //}

            if (this._topology && this._topology.meta) {
                for (var m in this._topology.meta) {
                    var meta = this._topology.meta[m];
                    if (meta && meta.id) {
                        var serviceId = meta.id.split('~')[0];
                        var service = this._map.getService(serviceId);

                        meta.service = service;
                    }
                }
            }
        }
    };
    this.snapPixelTolerance = 15;
    this.performSnap = function (lng, lat, options, sketch) {
        if ((this._topology == null || this._topology.vertices_wgs84 == null || this._topology.shapes == null) && !sketch) {
            return null;
        }

        var found = false;
        var result_endpoints = null,
            result_meta_endpoints = null,
            result_nodes = null,
            result_meta_nodes = null,
            result_edges = null,
            result_meta_edges = null,
            result_edge_vertices = null;
        var snap_vertices = true,
            snap_endpoints = true,
            snap_edges = true;

        var dist = 0.0, envEpsilon = 1e-5;
        if (options && options.tolerance) {
            dist = options.tolerance;
        } else {
            var pixelTolerance = options ? options.pixelTolerance : null;
            dist = (pixelTolerance || this.snapPixelTolerance) * this._map.scale() / (96.0 / 0.0254) / (6371000 * Math.cos(lat / 180 * Math.PI)) * 180 / Math.PI;
            envEpsilon += dist;
        }
        var dist2 = dist * dist;
        
        if (sketch && sketch.map && sketch.isSketchMoving() === false && sketch.isSketchRotating() === false) {
            //Snap to Sketch
            var sketchSnappingTypes = sketch.map.getSnappingTypes(webgis.sketchSnappingSchemeId);

            if ($.inArray(sketch.getGeometryType(), ["polyline", "polygon", "dimline", "hectoline"]) >= 0 && sketchSnappingTypes && sketchSnappingTypes.length > 0) {

                var sketchParts = sketch.getParts();
                var sketchDistance = 1e8, snappedVertex = null;
                var checkSketchNodes = $.inArray("nodes", sketchSnappingTypes) >= 0,
                    checkSketchEndpoints = $.inArray("endpoints", sketchSnappingTypes) >= 0,
                    checkSketchEdges = $.inArray("edges", sketchSnappingTypes) >= 0;

                if (checkSketchNodes || checkSketchEndpoints) {

                    // Sketch Nodes/Endpoints
                    var numParts = sketchParts.length;

                    for (var p = 0; p < numParts; p++) {
                        var sketchPart = sketchParts[p], partLength = sketchPart.length, snapType = 'node';
                        for (var v = 0; v < partLength; v++) {
                            if (checkSketchNodes == false && (v == 0 || v == partLength - 1) == false)  // only endpoints
                                continue;
                            if (p == numParts - 1 && v == partLength - 1) // do not check last point
                                continue;

                            var sketchVertex = sketchPart[v];
                            var dx = lng - sketchVertex.x, dy = lat - sketchVertex.y, d = dx * dx + dy * dy;
                            if (d < sketchDistance) {
                                snappedVertex = sketchVertex;
                                sketchDistance = d;
                                snapType = (v == 0 || v == partLength - 1) ? "endpoint" : "node";
                            }
                        }
                    }
                }

                if (sketchDistance <= dist2 && snappedVertex) {
                    return {
                        result: snappedVertex,
                        meta: { name: webgis.l10n.get("current-sketch") },
                        type: snapType
                    };
                } else {
                    sketchDistance = 1e8, snappedVertex = null;

                    if (checkSketchEdges) {
                        // Sketch edges
                        for (var p in sketchParts) {
                            var sketchPart = sketchParts[p], partLength = sketchPart.length, to = partLength;
                            if (partLength === 0)
                                continue;

                            if (partLength >= 3 && sketch.getGeometryType() === "polygon")
                                to++;

                            for (var v = 1; v < to; v++) {
                                var v1 = sketchPart[v - 1];
                                var v2 = sketchPart[v % partLength];

                                var distResult = this._linePointDistance([v1.x, v1.y], [v2.x, v2.y], lng, lat, v1.X ? [v1.X, v1.Y] : null, v2.X ? [v2.X, v2.Y] : null);
                                if (distResult && distResult.dist < sketchDistance) {
                                    snappedVertex = distResult.result;
                                    sketchDistance = distResult.dist;
                                    result_edge_vertices = [[v1.X, v1.Y], [v2.X, v2.Y]];
                                }
                            }
                        }
                    }

                    if (sketchDistance <= dist && snappedVertex) {
                        return {
                            result: snappedVertex,
                            meta: { name: webgis.l10n.get("current-sketch") },
                            type: 'edge',
                            vertices: result_edge_vertices
                        };
                    }
                }
            }
        }

        if (this._topology == null || this._topology.vertices_wgs84 == null || this._topology.shapes == null) {
            return null;
        }

        for (var s = 0, s_to = this._topology.shapes.length; s < s_to; s++) {
            if (!this._checkEnvelope(s, lng, lat, envEpsilon)) {
                //console.log('snapping checkEnvelope ' + s + ' outside');
                continue;
            }

            var shape = this._topology.shapes[s];
            var meta = this._getMeta(s);
            if (meta == null) {
                continue;
            }

            var snappingTypes = this._map.getSnappingTypes(meta.id);

            if (options) {
                if (options.id !== meta.id) {
                    continue;
                }
                if (options.name && options.name !== "*" && options.name !== meta.name) {
                    continue;
                }
                if (options.types) {
                    snappingTypes = options.types;
                }
            } else {  // wenn options nicht explizit übergeben werden => nur auf sichtbare Layer snappen
                if (meta.service && meta.layerIds && meta.layerIds.length > 0) {
                    var layerVisible = false;
                    for (var l in meta.layerIds) {
                        if (meta.service.layerVisibleAndInScale(meta.layerIds[l])) {
                            layerVisible = true;
                            break;
                        }
                    }
                    if (layerVisible === false) {
                        //console.log('snapping - layer not visible:', meta.name);
                        continue;
                    }
                }
            }

            if (snappingTypes == null || snappingTypes.length == 0)
                continue;

            var found = false;
            if ($.inArray('endpoints', snappingTypes) >= 0) {
                var vertexIndices = this._getShapeEndpointIndices(shape);
                for (var i in vertexIndices) {
                    var v = this._topology.vertices_wgs84[vertexIndices[i]];
                    var dx = lng - v[0], dy = lat - v[1], d = dx * dx + dy * dy;
                    if (d < dist2) {
                        dist2 = d;
                        dist = Math.sqrt(dist2);
                        result_endpoints = { x: v[0], y: v[1], X: this._topology.vertices[vertexIndices[i]][0], Y: this._topology.vertices[vertexIndices[i]][1] };
                        result_meta_endpoints = meta;
                        found = true;
                    }
                }
            }
            if (found == false) {
                if ($.inArray('nodes', snappingTypes) >= 0) {
                    var vertexIndices = this._getShapeVertexIndices(shape);
                    for (var i in vertexIndices) {
                        var v = this._topology.vertices_wgs84[vertexIndices[i]];
                        var dx = lng - v[0], dy = lat - v[1], d = dx * dx + dy * dy;
                        if (d < dist2) {
                            dist2 = d;
                            dist = Math.sqrt(dist2);
                            result_nodes = { x: v[0], y: v[1], X: this._topology.vertices[vertexIndices[i]][0], Y: this._topology.vertices[vertexIndices[i]][1] };
                            result_meta_nodes = meta;
                            found = true;
                        }
                    }
                }
            }
            if (found == false) {
                if ($.inArray('edges', snappingTypes) >= 0) {
                    var d = this._shapePointDistance(shape, lng, lat);
                    if (d && d.dist < dist) {
                        dist = d.dist;
                        dist2 = dist * dist;
                        result_edges = d.result;
                        result_meta_edges = meta;
                        result_edge_vertices = d.vertices;
                        found = true;
                    }
                }
            }
        }
        if (result_endpoints) {
            return {
                result: result_endpoints,
                meta: result_meta_endpoints,
                type: 'endpoint'
            };
        }
        if (result_nodes) {
            return {
                result: result_nodes,
                meta: result_meta_nodes,
                type: 'node'
            };
        }
        if (result_edges) {
            return {
                result: result_edges,
                meta: result_meta_edges,
                type: 'edge',
                vertices: result_edge_vertices
            };
        }
        return null;
    };
    this.hasSnapping = function (sketch) {
        if (sketch && sketch.map) {
            var sketchSnappingTypes = sketch.map.getSnappingTypes(webgis.sketchSnappingSchemeId);
            if (sketchSnappingTypes && sketchSnappingTypes.length > 0) {
                return true;
            }
        }

        if (this._topology === null || this._topology.vertices_wgs84 === null || this._topology.shapes === null || this._topology.shapes === null) {
            return false;
        }
        return this._map.hasActiveSnappingInScale();
    };
    this.snappingSrsId = function () {
        if (this._topology && this._topology.srs_id)
            return this._topology.srs_id;

        return 0;
    }
    /****** Construct ******/
    this.projectOrthogonal = function (p, p1, r) {
        if (!r || r.X == null || r.Y == null)
            return p;
        var l0 = p.X - p1.X, l1 = p.Y - p1.Y;
        var linEq = new webgis.calc.linearEquation2(l0, l1, r.X, -r.Y, r.Y, r.X);
        if (linEq.solve()) {
            var t1 = linEq.var1(), t2 = linEq.var2();
            var X1 = p1.X + r.X * t1, Y1 = p1.Y + r.Y * t1;
            var p_wgs84 = webgis.toWGS84(this._map.calcCrs(), X1, Y1);
            return { x: p_wgs84.lng, y: p_wgs84.lat, X: X1, Y: Y1 };
        }
        return p;
    };
    this.projectOrthogonalSign = function (p, p1, r) {
        if (!r || r.X == null || r.Y == null)
            return p;
        var l0 = p.X - p1.X, l1 = p.Y - p1.Y;
        var linEq = new webgis.calc.linearEquation2(l0, l1, r.X, -r.Y, r.Y, r.X);
        if (linEq.solve()) {
            var t1 = linEq.var1(), t2 = linEq.var2();
            return t1 > 0 ? 1 : -1;
        }

        return 0;
    };
    this.projectLength = function (p, p1, length) {
        var rx = p.X - p1.X, ry = p.Y - p1.Y, len = Math.sqrt(rx * rx + ry * ry);
        rx /= len;
        ry /= len;
        var X1 = p1.X + rx * length, Y1 = p1.Y + ry * length;
        var p_wgs84 = webgis.toWGS84(this._map.calcCrs(), X1, Y1);
        return { x: p_wgs84.lng, y: p_wgs84.lat, X: X1, Y: Y1 };
    };
    this.intersectLines = function (p, r, p1, r1) {
        var l0 = p1.X - p.X, l1 = p1.Y - p.Y;
        var linEq = new webgis.calc.linearEquation2(l0, l1, r.X, -r1.X, r.Y, -r1.Y);
        if (linEq.solve()) {
            var t = linEq.var1(), t1 = linEq.var2();
            var X1 = p1.X + r1.X * t1, Y1 = p1.Y + r1.Y * t1;
            var p_wgs84 = webgis.toWGS84(this._map.calcCrs(), X1, Y1);
            return { x: p_wgs84.lng, y: p_wgs84.lat, X: X1, Y: Y1 };
        }
        return null;
    };
    this.intersectLines2 = function (p11, p12, p21, p22, between) {
        if (!p11 || !p12 || !p21 || !p22)
            return null;

        var lx = p21.X - p11.X;
        var ly = p21.Y - p11.Y;
        var r1x = p12.X - p11.X, r1y = p12.Y - p11.Y;
        var r2x = p22.X - p21.X, r2y = p22.Y - p21.Y;
        var linEq = new webgis.calc.linearEquation2(lx, ly, r1x, -r2x, r1y, -r2y);
        if (linEq.solve()) {
            var t1 = linEq.var1();
            var t2 = linEq.var2();
            //if (between)
            //    console.log(t1, t2);
            if (between &&
                (t1 < 0.0 || t1 > 1.0 ||
                    t2 < 0.0 || t2 > 1.0))
                return null;
            var X1 = p11.X + t1 * r1x, Y1 = p11.Y + t1 * r1y;

            var p_wgs84 = webgis.toWGS84(this._map.calcCrs(), X1, Y1);
            return { x: p_wgs84.lng, y: p_wgs84.lat, X: X1, Y: Y1 };
        }
        return null;
    };
    this.intersectCircleLine = function (c, radius, p1, r1) {
        // a*t² + b*t + c = 0
        var a = r1.X * r1.X + r1.Y * r1.Y;
        var b = 2 * (r1.X * (p1.X - c.X) + r1.Y * (p1.Y - c.Y));
        var c = (p1.X - c.X) * (p1.X - c.X) + (p1.Y - c.Y) * (p1.Y - c.Y) - radius * radius;
        // quadratic equation
        //       -b (+/-) sqrt(b*b-4*a*c)
        // t = -----------------------------
        //                 2*a
        var bb4ac = b * b - 4 * a * c;
        if (bb4ac < 0)
            return null;
        var t1 = (-b + Math.sqrt(bb4ac)) / (2 * a), t2 = (-b - Math.sqrt(bb4ac)) / (2 * a);
        var ret = [];
        var X1 = p1.X + r1.X * t1, Y1 = p1.Y + r1.Y * t1;
        var p_wgs84 = webgis.toWGS84(this._map.calcCrs(), X1, Y1);
        ret.push({ x: p_wgs84.lng, y: p_wgs84.lat, X: X1, Y: Y1 });
        X1 = p1.X + r1.X * t2;
        Y1 = p1.Y + r1.Y * t2;
        p_wgs84 = webgis.toWGS84(this._map.calcCrs(), X1, Y1);
        ret.push({ x: p_wgs84.lng, y: p_wgs84.lat, X: X1, Y: Y1 });
        return ret;
    };
    this.midPoint = function (p1, p2) {
        if (!p1 || !p2)
            return null;
        var X1 = (p1.X + p2.X) * .5, Y1 = (p1.Y + p2.Y) * .5;
        var p_wgs84 = webgis.toWGS84(this._map.calcCrs(), X1, Y1);
        return { x: p_wgs84.lng, y: p_wgs84.lat, X: X1, Y: Y1 };
    };
    this.distanceDirection = function (p1, azimut_deg, distance_m, z_angle_deg) {
        var w = (90 - azimut_deg) / 180.0 * Math.PI;
        if (z_angle_deg && z_angle_deg != 0) {
            var z = z_angle_deg / 180.0 * Math.PI;
            distance_m *= Math.cos(z);
        }
        var X1 = p1.X + distance_m * Math.cos(w), Y1 = p1.Y + distance_m * Math.sin(w);
        var p_wgs84 = webgis.toWGS84(this._map.calcCrs(), X1, Y1);
        return { x: p_wgs84.lng, y: p_wgs84.lat, X: X1, Y: Y1 };
    };
    this.simplify = function (vertices) {
        // doppelte Punkte suchen und löschen
        // Punkte auf einer Geraden löschen => sonst kann max Offset oft nicht berechnet werden
        var clone = [], epsilon = 1e-6;
        clone.push(vertices[0]);
        for (var i = 1, to = vertices.length; i < to; i++) {
            if (this.dist(vertices[i - 1].X, vertices[i - 1].Y, vertices[i].X, vertices[i].Y) < epsilon)
                continue;

            if (i < to - 1 &&
                vertices[i - 1].fixed !== true &&
                vertices[i].fixed !== true &&
                vertices[i + 1].fixed !== true) {
                var v1 = new this.vector2d(vertices[i].X - vertices[i - 1].X, vertices[i].Y - vertices[i - 1].Y);
                var v2 = new this.vector2d(vertices[i + 1].X - vertices[i].X, vertices[i + 1].Y - vertices[i].Y);
                v1.normalize();
                v2.normalize();
                var a_ = v1.vectorAngle(v2);
                if (Math.abs(a_) < 1e-10 || Math.abs(2.0 * Math.PI - a_) < 1e-10) {
                    console.log('ignore point on straight line', vertices[i]);
                    continue;
                }
            }
            clone.push(vertices[i]);
        }

        return clone;
    };
    this.offset = function (vertices, offset, crs) {
        var ofac = offset >= 0 ? -1.0 : 1.0;
        offset = Math.abs(offset);
        var o = offset;

        //console.log('offset', vertices);

        // fixed vertices
        var fixedVertices = [];
        for (var i = 0; i < vertices.length; i++) {
            if (vertices[i].fixed === true) {
                fixedVertices.push(vertices[i]);
            }
        }

        while (o > 0.0 && vertices.length > 1) {
            vertices = this.simplify(vertices);
            // Strecken berechnen
            var s = [];
            for (var i = 1, to = vertices.length; i < to; i++) {
                s[i - 1] = this.dist(vertices[i - 1].X, vertices[i - 1].Y, vertices[i].X, vertices[i].Y);
            }
            // Winkel zwischen den Strecken
            var a = [];
            for (var i = 1, to = vertices.length - 1; i < to; i++) {
                var v1 = new this.vector2d(vertices[i].X - vertices[i - 1].X, vertices[i].Y - vertices[i - 1].Y);
                var v2 = new this.vector2d(vertices[i + 1].X - vertices[i].X, vertices[i + 1].Y - vertices[i].Y);
                v1.normalize();
                v2.normalize();
                var a_ = v1.vectorAngle(v2);
                a[i - 1] = Math.PI + a_ * ofac;
            }
            // h (=maximal berechenbarer Offset) ermitteln
            var h = 1e10;
            for (var i = 1, to = s.length; i < to; i++) {
                if (a[i - 1] >= Math.PI ||
                    Math.abs(Math.abs(a[i - 1] / 2.0) - Math.PI / 2.0) <= 1e-8)
                    continue;
                h = Math.min(h, Math.abs(Math.max(s[i - 1], s[i]) * Math.tan(a[i - 1] / 2.0)));
            }
            if (h == 0.0)
                return null;
            o = Math.min(o, h);
            // Offset Punkte berechnen
            var offsetVertices = [];
            for (var i = 0, to = vertices.length - 1; i < to; i++) {
                var fixed0 = this._isFixedVertex(vertices[i], fixedVertices);
                var fixed1 = this._isFixedVertex(vertices[i + 1], fixedVertices);

                var v1 = new this.vector2d(vertices[i + 1].X - vertices[i].X, (vertices[i + 1].Y - vertices[i].Y));
                v1.normalize();
                v1.perpendicular();

                var offsetVertex1 = null;
                var offsetVertex2 = null;

                if (fixed0) {
                    offsetVertex1 = { X: vertices[i].X, Y: vertices[i].Y, fixed: true };
                } else {
                    offsetVertex1 = { X: vertices[i].X + v1.X() * o * ofac * -1.0, Y: vertices[i].Y + v1.Y() * o * ofac * -1.0 };
                }

                if (fixed1) {
                    offsetVertex2 = { X: vertices[i + 1].X, Y: vertices[i + 1].Y, fixed: true };
                } else {
                    offsetVertex2 = { X: vertices[i + 1].X + v1.X() * o * ofac * -1.0, Y: vertices[i + 1].Y + v1.Y() * o * ofac * -1.0 };
                }

                if (offsetVertex1 && vertices[i].srs) {
                    offsetVertex1.srs = vertices[i].srs;
                }
                if (offsetVertex2 && vertices[i + 1].srs) {
                    offsetVertex2.srs = vertices[i + 1].srs;
                }

                offsetVertices.push(offsetVertex1);
                offsetVertices.push(offsetVertex2);
            }
            //var projected = [];
            //for (var i = 0; i < offsetVertices.length; i++) {
            //    if (offsetVertices[i].x && offsetVertices[i].y) {
            //        projected.push(offsetVertices[i]);
            //    } else {
            //        var X = offsetVertices[i].X, Y = offsetVertices[i].Y;
            //        var p_wgs84 = webgis.toWGS84(this._map.calcCrs(), X, Y);
            //        projected.push({ x: p_wgs84.lng, y: p_wgs84.lat, X: X, Y: Y });
            //    }
            //}
            //return projected;
            // Knotenpunkte verschneiden

            var add = -1;
            var nVertices = [];
            nVertices.push(offsetVertices[0]);
            for (var i = 1, to = vertices.length - 1; i < to; i++) {
                var v = this.intersectLines2(offsetVertices[i + add], offsetVertices[i + add + 1], offsetVertices[i + add + 2], offsetVertices[i + add + 3], false);
                add++;
                if (v == null) {
                    return null;
                }

                if (vertices[i].srs) {
                    v.srs = vertices[i].srs;
                }
                nVertices.push(v);
            }
            nVertices.push(offsetVertices[offsetVertices.length - 1]);
            // Self Intersection
            vertices = [];
            for (var i = 0, to = nVertices.length - 1; i < to; i++) {
                vertices.push(nVertices[i]);
                var iVertex = null;
                var ni = -1;
                for (var j = i + 2; j < to; j++) {
                    var p = this.intersectLines2(nVertices[i], nVertices[i + 1], nVertices[j], nVertices[j + 1], true);
                    if (p != null) {
                        iVertex = p;
                        ni = j;
                    }
                }
                if (ni != -1) {
                    nVertices[ni] = iVertex;
                    i = ni - 1;
                }
            }
            vertices.push(nVertices[nVertices.length - 1]);
            offset = offset - o;
            o = offset;
        }

        // project
        let projected = [];
        for (let i = 0; i < vertices.length; i++) {
            if (vertices[i].x && vertices[i].y) {
                projected.push(vertices[i]);
            }
            else {
                let X = vertices[i].X, Y = vertices[i].Y;
                let p_wgs84 = webgis.toWGS84(crs || this._map.calcCrs(), X, Y);
                let vertex = { x: p_wgs84.lng, y: p_wgs84.lat, X: X, Y: Y, fixed: vertices[i].fixed };
                if (vertices[i].srs) {
                    vertex.srs = vertices[i].srs;
                }
                projected.push(vertex);
            }
        }

        for (var p in projected) {
            if (this._isFixedVertex(projected[p], fixedVertices)) {
                projected[p].fixed = true;
            }
        }

        return projected;
    };
    this._isFixedVertex = function (vertex, fixedVertices) {
        if (vertex.fixed === true) {
            return true;
        }

        if (fixedVertices != null) {
            for (var i in fixedVertices) {
                if (fixedVertices[i].X && fixedVertices[i].Y && vertex.X && vertex.X) {
                    if (Math.abs(fixedVertices[i].X - vertex.X) <= 1e-4 || Math.abs(fixedVertices[i].Y - vertex.Y) <= 1e-4) {
                        return true;
                    }
                }
                else if (fixedVertices[i].x && fixedVertices[i].y && vertex.x && vertex.y) {
                    if (Math.abs(fixedVertices[i].x - vertex.x) <= 1e-8 || Math.abs(fixedVertices[i].y - vertex.y) <= 1e-8) {
                        return true;
                    }
                }
            }
        }

        return false;
    };
    /******* Linalg *********/
    this.vector2d = function (x, y) {
        this._x = x;
        this._y = y;
        this.length = function () {
            return Math.sqrt(this._x * this._x + this._y * this._y);
        };
        this.normalize = function () {
            var len = this.length();
            this._x /= len;
            this._y /= len;
        };
        this.inv = function () {
            this._x = -this._x;
            this._y = -this._y;
        };
        this.angle = function () {
            var a = Math.atan2(this._y, this._x);
            return a >= 0 ? a : a + 2.0 * Math.PI;
        };
        this.vectorAngle = function (vec) {
            var cosa = this.innerProduct(vec);
            var sina = this.crossProduct(vec);
            var a = Math.atan2(-sina, cosa); // -sina, weil Hochwert nach oben positiv
            return a >= 0 ? a : a + 2.0 * Math.PI;
        };
        this.X = function () { return this._x; };
        this.Y = function () { return this._y; };
        this.innerProduct = function (vec) {
            return this.X() * vec.X() + this.Y() * vec.Y();
        };
        this.crossProduct = function (vec) {
            return this.X() * vec.Y() - this.Y() * vec.X();
        };
        this.perpendicular = function () {
            var x = this._x;
            this._x = this._y;
            this._y = -x;
        };
    };
    /******* Geometry *****/
    this.equalVertex = function (v1, v2, tolerance) {
        tolerance = tolerance || 1e-8;

        var dist = this.dist(v1.x, v1.y, v2.x, v2.y);
        return dist <= tolerance;
    };
    this._concatArrays = function (array1, array2, skip) {
        skip = skip || 0;

        for (var i = skip, to = array2.length; i < to; i++) {
            array1.push(array2[i]);
        }
    };
    this.mergePaths = function (paths, tolerance) {
        if (!paths || paths.length == 0) {
            return null;
        }

        // remove invalid Paths (no vertices)
        var validPaths = [];
        for (var i = 0, to = paths.length; i < to; i++) {
            var path = paths[i];
            if (path && path.length > 0) {
                validPaths.push(path);
            }
        }
        paths = validPaths;
        //console.log('validPaths', paths);

        var pathCount = paths.length;

        if (pathCount == 1) {
            return paths[0]
        }

        var appended = [0];
        var merged = paths[0];

        var iterations = 0;
        while (true) {
            var appendLength = appended.length;

            if (appendLength == paths.length) {  // already all merged
                return merged;
            }

            for (var i = 1, to = pathCount; i < to; i++) {
                if ($.inArray(i, appended) >= 0) {
                    continue;
                }
                var mergedFirst = merged[0];
                var mergedLast = merged[merged.length - 1];

                var path = paths[i];
                var pathFirst = path[0];
                var pathLast = path[path.length - 1];

                var connected = false;
                if (this.equalVertex(mergedLast, pathFirst, tolerance)) {
                    connected = true;
                }
                else if (this.equalVertex(mergedLast, pathLast, tolerance)) {
                    path = path.reverse();
                    connected = true;
                }
                else if (this.equalVertex(mergedFirst, pathFirst, tolerance)) {
                    merged = merged.reverse(); 
                    connected = true;
                }
                else if (this.equalVertex(mergedFirst, pathLast, tolerance)) {
                    merged = merged.reverse();
                    path = path.reverse();
                    connected = true;
                }

                if (connected === true) {
                    this._concatArrays(merged, path, 1);
                    appended.push(i);
                }
            }

            iterations++;
            if (appendLength == appended.length ||   // nothing added
                iterations > paths.length) {  
                return null;
            }
        }

        return null;
    }
};
