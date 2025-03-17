webgis.calc = new function () {
    this.length = function (coords, xParam, yParam, circum/*, spheric*/) {
        if (coords.length < 2)
            return 0.0;
        if (xParam == "X" && yParam == "Y") {
            coords = this._projectToCalcCrs(coords);
        }
        var l = 0, to = coords.length;
        if (circum) {
            if (to < 3)
                return 0;
            to++;
        }
        try {
            for (var i = 1; i < to; i++) {
                var j0 = i < coords.length ? i - 1 : 0;
                var j1 = i < coords.length ? i : coords.length - 1;
                var x1 = coords[j0][xParam], y1 = coords[j0][yParam];
                var x2 = coords[j1][xParam], y2 = coords[j1][yParam];
                l += this.DistanceFromXY(x1, y1, x2, y2);
            }
        }
        catch (e) { }
        return l;
    };
    this.area = function (coords, xParam, yParam) {
        var F = 0, max = coords.length;
        if (max < 3)
            return 0.0;
        if (xParam == "X" && yParam == "Y") {
            coords = this._projectToCalcCrs(coords);
        }
        try {
            for (var i = 0; i < max; i++) {
                var y1 = (i == max - 1) ? coords[0][yParam] : coords[i + 1][yParam];
                var y2 = (i == 0) ? coords[max - 1][yParam] : coords[i - 1][yParam];
                F += 0.5 * coords[i][xParam] * (y1 - y2);
            }
        }
        catch (e) { }
        return Math.abs(F);
    };
    this.round = function (r, acc) {
        var f = Math.pow(10, acc);
        return Math.round(r * f) / f;
    };
    this.angle_deg = function (coords) {
        return (this.angle_rad(coords) || 0.0) * 180.0 / Math.PI;
    }
    this.angle_rad = function (coords) {
        if (coords && coords.length == 2) {
            var coords = webgis.calc._projectToCalcCrs(coords);
            var dx = coords[1].X - coords[0].X;
            var dy = coords[1].Y - coords[0].Y;
            var len = Math.sqrt(dx * dx + dy * dy);
            dx /= len; dy /= len;
            var angle = Math.atan2(dy, dx);

            //console.log(angle, dx, dy);
            return angle;
        }
        return null;
    }
    this.azimut_rad = function (coords) {
        if (coords && coords.length == 2) {
            var coords = webgis.calc._projectToCalcCrs(coords);
            var dx = coords[1].X - coords[0].X;
            var dy = coords[1].Y - coords[0].Y;
            var len = Math.sqrt(dx * dx + dy * dy);
            dx /= len; dy /= len;
            var azimut = Math.atan2(dx, dy);

            return azimut;
        }
        return null;
    }

    this.pathPoint = function (coords, stat, xParam, yParam) {
        if (coords == null || coords.length < 2) {
            return null;
        }

        xParam = xParam || 'x';
        yParam = yParam || 'y';
        var station = 0.0, station0 = 0.0, direction = 0;
        var x1 = coords[0][xParam];
        var y1 = coords[0][yParam];
        for (var i = 1; i < coords.length; i++) {
            var x2 = coords[i][xParam];
            var y2 = coords[i][yParam];

            station0 = station;
            station += Math.sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            if (station >= stat) {
                var t = stat - station0;
                var l = Math.sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                var dx = (x2 - x1) / l, dy = (y2 - y1) / l;

                direction = Math.atan2(dy, dx);

                return {
                    x: x1 + dx * t,
                    y: y1 + dy * t,
                    direction: direction * 180.0 / Math.PI
                };
            }

            x1 = x2; y1 = y2;
        }
        return null;
    };

    /**** Spheric ****/
    var rad = function (d) { return d * Math.PI / 180.0; };
    var haversine = function (x) { return (1.0 - Math.cos(x)) / 2.0; };
    this.R = 6378137.0;
    this.SphericDistance = function (lon1, lat1, lon2, lat2) {
        var R = 6378137; // Radius of the earth in m
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos((lat1) * Math.PI / 180.0) * Math.cos((lat2) * Math.PI / 180.0) *
            Math.sin(dLon / 2) * Math.sin(dLon / 2);
        var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        var d = R * c; // Distance in m
        return d;
    };
    this.DistanceFromXY = function (x1, y1, x2, y2, spheric) {
        if (spheric) {
            return this.SphericDistance(p1[0], p1[1], p2[0], p2[1]);
        }
        else {
            var dx = x2 - x1, dy = y2 - y1;
            return Math.sqrt(dx * dx + dy * dy);
        }
    };

    this.linearEquation2 = function (l0, l1, a00, a01, a10, a11) {
        var _l0 = l0;
        var _l1 = l1;
        var _a00 = a00;
        var _a01 = a01;
        var _a10 = a10;
        var _a11 = a11;
        var t1, t2;
        this.solve = function () {
            var detA = this.calc22Det(_a00, _a01, _a10, _a11);
            if (detA == 0.0)
                return false;
            t1 = this.calc22Det(_l0, _a01, _l1, _a11) / detA;
            t2 = this.calc22Det(_a00, _l0, _a10, _l1) / detA;
            return true;
        };
        this.calc22Det = function (a00, a01, a10, a11) {
            return a00 * a11 - a01 * a10;
        };
        this.var1 = function () { return t1; };
        this.var2 = function () { return t2; };
    };
    this.linearEquation3 = function (l0, l1, l2,
        a00, a01, a02,
        a10, a11, a12,
        a20, a21, a22) {

        var _l0 = l0, _l1 = l1, _l2 = l2;
        var _a00 = a00, _a01 = a01, _a02 = a02;
        var _a10 = a10, _a11 = a11, _a12 = a12;
        var _a20 = a20, _a21 = a21, _a22 = a22;
        var t1, t2, t3;

        this.solve = function () {
            var detA = this.calc33Det(_a00, _a01, _a02, _a10, _a11, _a12, _a20, _a21, _a22);
            if (detA == 0.0)
                return false;

            t1 = this.calc33Det(_l0, _a01, _a02, _l1, _a11, _a12, _l2, _a21, _a22) / detA;
            t2 = this.calc33Det(_a00, _l0, _a02, _a10, _l1, _a12, _a20, _l2, _a22) / detA;
            t3 = this.calc33Det(_a00, _a01, _l0, _a10, _a11, _l1, _a20, _a21, _l2) / detA;

            return true;
        };

        this.calc33Det = function (
            a00, a01, a02,
            a10, a11, a12,
            a20, a21, a22) {
            return a00 * this._calc22Det(a11, a12, a21, a22)
                - a01 * this._calc22Det(a10, a12, a20, a22)
                + a02 * this._calc22Det(a10, a11, a20, a21);
        };
        this._calc22Det = function (a00, a01, a10, a11) {
            var det22 = a00 * a11 - a01 * a10;
            return det22;
        };

        this.var1 = function () { return t1; };
        this.var2 = function () { return t2; };
        this.var3 = function () { return t3; };
    };
    this.helmert2d = function (x, y, cx, cy, mue, r, rx, ry) {
        // xt = c + mue * R*x_  => 
        var x_ = x - rx, y_ = y - ry;
        var cos = Math.cos(r), sin = Math.sin(r);
        var xt = cx + mue * (x_ * cos - y_ * sin);
        var yt = cy + mue * (x_ * sin + y_ * cos);
        return [xt + rx, yt + ry];
    };

    /**** Spatial Reference / Project ****/
    this.setCalcCrs = function (crs) {
        //console.trace('webgis.calc.setCalcCrs', crs);
        this._crsId = crs;
    };
    this.getCalcCrsId = function (coords) {
        if (typeof this._crsId === 'function') {
            return this._crsId(coords);
        }
        return this._crsId;
    };
    this._crsId = 0;
    this._crs = {};
    this._canProject = function () {
        return this._crsId > 0 || typeof this._crsId === 'function';
    };
    this._projectToCalcCrs = function (coords) {
        var crsId = this.getCalcCrsId(coords);
        if (crsId <= 0)
            return coords;

        if (!this._crs[crsId]) {
            var crsDef = webgis.crsInfo(crsId);
            if (crsDef.id && crsDef.name && crsDef.p4) {
                this._crs[crsId] = webgis.mapFramework === 'leaflet' ? new L.Proj.CRS('EPSG:' + crsDef.id, crsDef.p4) : {};
            }
        }
        if (this._crs[crsId] && this._crs[crsId].projection) {
            var projCoords = [], proj = this._crs[crsId].projection;
            for (var i = 0, to = coords.length; i < to; i++) {
                var point = proj.project(L.latLng(coords[i].y, coords[i].x));
                projCoords.push({ x: coords[i].x, y: coords[i].y, X: point.x, Y: point.y, srs: crsId });
            }
            coords = projCoords;
        }
        return coords;
    };
    this._unprojectFromCalcCrs = function (coords) {
        var crsId = this.getCalcCrsId();
        if (crsId <= 0)
            return coords;

        if (!this._crs[crsId]) {
            var crsDef = webgis.crsInfo(crsId);
            if (crsDef.id && crsDef.name && crsDef.p4) {
                this._crs = webgis.mapFramework === 'leaflet' ? new L.Proj.CRS('EPSG:' + crsDef.id, crsDef.p4) : {};
            }
        }
        if (this._crs[crsId] && this._crs[crsId].projection) {
            var projCoords = [], proj = this._crs[crsId].projection;
            for (var i = 0, to = coords.length; i < to; i++) {
                var latlng = proj.unproject(L.point(coords[i].X, coords[i].Y));
                projCoords.push({ x: latlng.lng, y: latlng.lat, X: coords[i].X, Y: coords[i].Y, srs: crsId });
            }
            coords = projCoords;
        }
        return coords;
    };
    this.project = function (lng, lat) {
        var coords = this._projectToCalcCrs([{ x: lng, y: lat }]);
        return coords[0];
    };
    this.unproject = function (x, y) {
        var coords = this._unprojectFromCalcCrs([{ X: x, Y: y }]);
        return coords[0];
    };

    /*** Jordan ***/
    this.pointInRings = function (rings, point) {  /* Jordan Alg */
        //console.log('rings', rings);
        var inter = 0;
        for (var i = 0; i < rings.length; i++) {
            inter += this.calcIntersections(rings[i], point);
        }
        return ((inter % 2) == 0) ? false : true;
    };
    this.isRingsHole = function (rings, hole) {  /* Jordan Alg */
        for (var i = 0; i < hole.length; i++) {
            if (!this.pointInRings(rings, hole[i])) {
                return false;
            }
        }
        return true;
    };
    this.calcIntersections = function (ringVertices, candidateVertex) {
        //console.log('ringVertices', ringVertices);
        //console.log('candidateVertex', candidateVertex);
        var first = true;
        var x1 = 0.0, y1 = 0.0, x2 = 0, y2 = 0, x0 = 0, y0 = 0, k, d;
        var inter = 0;

        for (var i = 0; i < ringVertices.length; i++) {
            var vertex = ringVertices[i];
            //console.log('vertex',vertex);
            //console.log('candidateVertex',candidateVertex);
            x2 = vertex.x - candidateVertex.x;
            y2 = vertex.y - candidateVertex.y;

            //console.log(x2, y2);

            if (!first) {
                if (this.isPositive(x1) != this.isPositive(x2)) {
                    if (this.getLineKD(x1, y1, x2, y2).d > 0) {
                        inter++;
                    }
                }
            }
            x1 = x2;
            y1 = y2;
            if (first) {
                first = false;
                x0 = x1; y0 = y1;
            }
        }

        //Ring schliessen
        if (Math.abs(x0 - x2) > 1e-12 || Math.abs(y0 - y2) > 1e-12) {
            if (this.isPositive(x0) != this.isPositive(x2)) {
                if (this.getLineKD(x0, y0, x2, y2).d > 0) {
                    inter++;
                }
            }
        }
        return inter;
    };
    this.isPositive = function (z) { return Math.sign(z) < 0 ? false : true };
    this.getLineKD = function (x1, y1, x2, y2) {
        var dx = x2 - x1;
        var dy = y2 - y1;
        if (Math.abs(dx) < 1e-12) {
            d = k = 0.0;
            return { d: d, k: k };
        }
        var k = dy / dx;
        var d = y1 - k * x1;  // y=kx+d
        //console.log({ d: d, k: k });
        return { d: d, k: k };
    };
    /*** Self Intersection ***/
    this.isSelfIntersecting = function (rings, closeRings) {
        // Segmente sammeln
        var segments = [];
        for (var r = 0; r < rings.length; r++) {
            var ring = rings[r];
            for (var i = 0; i < ring.length - 1; i++) {
                var vertex1 = ring[i], vertex2 = ring[i + 1];
                segments.push({ v1: vertex1, v2: vertex2 });
            }
            if (closeRings === true && ring.length >= 3) {  // close ring
                segments.push({ v1: ring[ring.length - 1], v2: ring[0] });
            }
        }

        // Segmente auf Schnittpunkte überprüfen
        var segmentsLength = segments.length;
        for (var i = 0; i < segmentsLength; i++) {
            var segment = segments[i];
            for (var j = i + 1; j < segmentsLength; j++) {
                var candidateSegement = segments[j];
                var intersection = this.intersectSegments(segment.v1, segment.v2, candidateSegement.v1, candidateSegement.v2, true);
                if (intersection != null && intersection.touching == false) {
                    return true;
                }
            }
        }

        return false;
    };
    this.intersectSegments = function (v11, v12, v21, v22, mustBetween) {
        var lx = v21.x - v11.x;
        var ly = v21.y - v11.y;

        var r1x = v12.x - v11.x, r1y = v12.y - v11.y;
        var r2x = v22.x - v21.x, r2y = v22.y - v21.y;

        var lineq = new this.linearEquation2(
            lx, ly,
            r1x, -r2x,
            r1y, -r2y);

        if (lineq.solve()) {
            var t1 = lineq.var1();
            var t2 = lineq.var2();

            //console.log('solved', t1, t2);

            if (mustBetween == true &&
                (t1 < 0.0 || t1 > 1.0 ||
                    t2 < 0.0 || t2 > 1.0)) return null;

            var touching =
                Math.abs(t1) < 1e-12 ||
                Math.abs(t1 - 1.0) < 1e-12 ||
                Math.abs(t2) < 1e-12 ||
                Math.abs(t2 - 1.0) < 1e-12;

            return { x: v11.x + t1 * r1x, y: v11.y + t2 * r1y, touching: touching }
        };

        return null;
    };
    this.isParallel = function (p11, p12, p21, p22) {
        var tolerance = 1e-3;
        var P11 = this.project(p11.x, p11.y),
            P12 = this.project(p12.x, p12.y),
            P21 = this.project(p21.x, p21.y),
            P22 = this.project(p22.x, p22.y);

        var X1 = P12.X - P11.X, Y1 = P12.Y - P11.Y,
            X2 = P22.X - P21.X, Y2 = P22.Y - P21.Y;

        var len1 = Math.sqrt(X1 * X1 + Y1 * Y1);
        var len2 = Math.sqrt(X2 * X2 + Y2 * Y2);

        if (len1 == 0 || len2 == 0) {
            return 0;  // NaN ??
        }

        X1 /= len1;
        Y1 /= len1;
        X2 /= len2;
        Y2 /= len2;

        //console.log('parallel 1', Math.abs(X2 - X1), Math.abs(Y2 - Y1));
        //console.log('parallel -1', Math.abs(X2 + X1), Math.abs(Y2 + Y1));

        if (Math.abs(X2 - X1) < tolerance && Math.abs(Y2 - Y1) < tolerance)   // Parallel with same direction
        {
            return 1;
        }

        if (Math.abs(X1 + X2) < tolerance && Math.abs(Y1 + Y2) < tolerance)   // Paralell with opposite direction 
        {
            return -1;
        }

        return 0;
    }
    /*** Rotation ***/
    this.rotateBbox = function (bbox, rotation) {
        var rotation = rotation * Math.PI / 180.0;

        var center = [(bbox[0] + bbox[2]) * .5, (bbox[1] + bbox[3]) * .5];

        var p1 = this._rotPoint(center, [bbox[0], bbox[1]], rotation);
        var p2 = this._rotPoint(center, [bbox[0], bbox[3]], rotation);
        var p3 = this._rotPoint(center, [bbox[2], bbox[3]], rotation);
        var p4 = this._rotPoint(center, [bbox[2], bbox[1]], rotation);

        return [p1, p2, p3, p4];
    };
    this._rotPoint = function (center, point, rotation) {
        var vec = [point[0] - center[0], point[1] - center[1]];

        var sin_r = Math.sin(rotation), cos_r = Math.cos(rotation);

        var r_vec = [
            cos_r * vec[0] + sin_r * vec[1],
            -sin_r * vec[0] + cos_r * vec[1]
        ];

        return [center[0] + r_vec[0], center[1] + r_vec[1]];
    };
    /*** Bounds ***/
    this.resizeBounds = function (bounds, factor) {
        if (bounds && bounds.length === 4) {
            var w = Math.abs(bounds[0] - bounds[2]),
                h = Math.abs(bounds[1] - bounds[3]);

            var cx = (bounds[0] + bounds[2]) * .5,
                cy = (bounds[1] + bounds[3]) * .5;

            bounds = [
                cx - w / 2.0 * factor,
                cy - h / 2.0 * factor,
                cx + w / 2.0 * factor,
                cy + h / 2.0 * factor
            ];
        }

        return bounds;
    };
    /*** Advanced Coordinates Systems ***/
    this.crs_BestAustria_GK = function (coords) {
        if (coords && coords.length > 0) {
            var meanX = 0;
            for (var i = 0, to = coords.length; i < to; i++) {
                meanX += coords[i].x / to;
            }

            if (meanX < 11.8333333333333333)
                return 31254;
            if (meanX > 14.8333333333333333)
                return 31256;

            return 31255;
        }

        return 31255;
    };
    /*** Intersect ***/
    this.intersectionPoint = function (calcCrs, linePoints1, linePoints2, mode) {
        webgis.complementProjected(calcCrs, linePoints1);
        webgis.complementProjected(calcCrs, linePoints2);

        return this.intersectionPoint2(calcCrs, linePoints1, linePoints2, mode);
    }
    this.intersectionPoint2 = function (calcCrs, linePoints1, linePoints2, mode) {
        var r1_x = linePoints1[1].X - linePoints1[0].X,
            r1_y = linePoints1[1].Y - linePoints1[0].Y,
            r2_x = linePoints2[1].X - linePoints2[0].X,
            r2_y = linePoints2[1].Y - linePoints2[0].Y,
            l_x = linePoints2[0].X - linePoints1[0].X,
            l_y = linePoints2[0].Y - linePoints1[0].Y;

        if (mode === 'close_perpendicular') {
            r2_x = r1_x;
            r2_y = r1_y;

            r1_x = r1_y;
            r1_y = -r2_x;

            //console.log([r1_x, r1_y], [r2_x, r2_y]);
        }

        var D = r1_x * (-r2_y) - (-r2_x) * r1_y;
        if (D == 0.0) {
            return null;
        }

        Dx = l_x * (-r2_y) - (-r2_x) * l_y;
        Dy = r1_x * l_y - l_x * r1_y;

        var t1 = Dx / D; // t2 = Dy / D;

        var results = [{
            X: linePoints1[0].X + r1_x * t1,
            Y: linePoints1[0].Y + r1_y * t1,
            D: D
        }];

        webgis.complementWGS84(calcCrs, results);

        return results[0];
    }

    // Vertices
    this.nearestVertex = function (point, vertices, xParam, yParam) {
        if (!point || !vertices) return null;

        xParam = xParam || 'x';
        yParam = yParam || 'y';

        const pointX = point[xParam], pointY = point[yParam];

        let result = null, resultDist2 = 0.0;
        for (let v in vertices) {
            const vertexX = vertices[v][xParam], vertexY = vertices[v][yParam];
            const dx = pointX - vertexX, dy = pointY - vertexY;
            const dist2 = dx * dx + dy * dy;

            if (!result || dist2 < resultDist2) {
                resultDist2 = dist2;
                result = vertices[v];
            }
        }
        return result;
    };
    this.verticesBbox = function (vertices, xParam, yParam) {
        if (!vertices || vertices.length === 0) return null;

        xParam = xParam || 'x';
        yParam = yParam || 'y';

        let minX = vertices[0][xParam], minY = vertices[0][yParam], maxX = minX, maxY = minY;

        for (let i = 1; i < vertices.length; i++) {
            minX = Math.min(minX, vertices[i][xParam]);
            minY = Math.min(minY, vertices[i][yParam]);
            maxX = Math.max(maxX, vertices[i][xParam]);
            maxY = Math.max(maxY, vertices[i][yParam]);
        }

        return [minX, minY, maxX, maxY];
    };

    // bbox operations
    this.bboxInside = function (bbox, point) {
        if (!bbox || bbox.length !== 4) {
            return false;
        }
        if (!point || point.length !== 2) {
            return false;
        }

        var isInside =
            bbox[0] <= point[0] && bbox[2] >= point[0] &&
            bbox[1] <= point[1] && bbox[3] >= point[1]

        return isInside;
    };
    this.bboxContains = function (bbox, cand) {
        if (!cand || cand.length < 2 || cand.length % 2 !== 0) {
            return false;
        }

        for (let i = 0; i < cand.length; i += 2) {
            if (this.bboxInside(bbox, [cand[i], cand[i + 1]]) === false) {
                return false;
            }
        }

        return true;
    };
    this.bboxIntersects = function (bbox1, bbox2) {
        // Check if bbox1 and bbox2 are valid arrays
        if (!bbox1 || bbox1.length !== 4 || !bbox2 || bbox2.length !== 4) {
            return false;
        }

        // Step 1: Check if bbox1 is completely to the left or right of bbox2
        if (bbox1[2] < bbox2[0] || bbox1[0] > bbox2[2]) {
            return false;
        }

        // Step 2: Check if bbox1 is completely above or below bbox2
        if (bbox1[3] < bbox2[1] || bbox1[1] > bbox2[3]) {
            return false;
        }

        // Step 3: The bounding boxes intersect
        return true;
    };
    this.bboxSizeRatio = function(bbox1, bbox2) {
        // Calculate the area of bbox1
        const area1 = (bbox1[2] - bbox1[0]) * (bbox1[3] - bbox1[1]);

        // Calculate the area of bbox2
        const area2 = (bbox2[2] - bbox2[0]) * (bbox2[3] - bbox2[1]);

        // Calculate the ratio of the areas
        return area1 / area2;
    };
    this.bboxResizePerRatio = function(bbox, ratio) {
        // Calculate the current center of the bounding box
        const center_x = (bbox[0] + bbox[2]) / 2;
        const center_y = (bbox[1] + bbox[3]) / 2;

        // Calculate the current width and height of the bounding box
        const width = bbox[2] - bbox[0];
        const height = bbox[3] - bbox[1];

        // Calculate the new width and height of the bounding box based on the ratio
        const new_width = width * ratio;
        const new_height = height * ratio;

        // Calculate the new minimum and maximum coordinates of the bounding box
        const new_minx = center_x - new_width / 2;
        const new_miny = center_y - new_height / 2;
        const new_maxx = center_x + new_width / 2;
        const new_maxy = center_y + new_height / 2;

        // Return the new bounding box as an array
        return [new_minx, new_miny, new_maxx, new_maxy];
    }
};