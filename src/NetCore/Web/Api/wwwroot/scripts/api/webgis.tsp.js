webgis.tsp = new function () {
    var tspAlg = new function () {
        // based on: https://github.com/lovasoa/salesman.js/blob/master/salesman.js

        this.path = function (points) {
            this.points = points;
            this.order = new Array(points.length);

            for (var i = 0; i < points.length; i++) {
                this.order[i] = i;
            }

            this.distances = new Array(points.length * points.length);

            for (var i = 0; i < points.length; i++) {
                for (var j = 0; j < points.length; j++) {
                    this.distances[j + i * points.length] = distance(points[i], points[j]);
                }
            }

            this.change = function (temp) {
                var i = this.randomPos(), j = this.randomPos();
                var delta = this.delta_distance(i, j);
                if (delta < 0 || Math.random() < Math.exp(-delta / temp)) {
                    this.swap(i, j);
                }
            };

            this.size = function () {
                var s = 0;
                for (var i = 0; i < this.points.length; i++) {
                    s += this.distance(i, ((i + 1) % this.points.length));
                }
                return s;
            };

            this.swap = function (i, j) {
                var tmp = this.order[i];
                this.order[i] = this.order[j];
                this.order[j] = tmp;
            };

            this.delta_distance = function (i, j) {
                var jm1 = this.index(j - 1),
                    jp1 = this.index(j + 1),
                    im1 = this.index(i - 1),
                    ip1 = this.index(i + 1);

                var s =
                    this.distance(jm1, i)
                    + this.distance(i, jp1)
                    + this.distance(im1, j)
                    + this.distance(j, ip1)
                    - this.distance(im1, i)
                    - this.distance(i, ip1)
                    - this.distance(jm1, j)
                    - this.distance(j, jp1);

                if (jm1 === i || jp1 === i)
                    s += 2 * this.distance(i, j);

                return s;
            };

            this.index = function (i) {
                return (i + this.points.length) % this.points.length;
            };

            this.access = function (i) {
                return this.points[this.order[this.index(i)]];
            };

            this.distance = function (i, j) {
                return this.distances[this.order[i] * this.points.length + this.order[j]];
            };

            // Random index between 1 and the last position in the array of points
            this.randomPos = function () {
                return 1 + Math.floor(Math.random() * (this.points.length - 1));
            };
        };

        function distance(p, q) {
            var dx = p.x - q.x, dy = p.y - q.y;
            return Math.sqrt(dx * dx + dy * dy);
        }

        this.solve = function (points, temp_coeff, callback) {
            var path = new this.path(points);
            if (points.length < 2) {
                return path.order; // There is nothing to optimize
            }

            if (!temp_coeff) {
                temp_coeff = 1 - Math.exp(-10 - Math.min(points.length, 1e6) / 1e5);
            }

            var has_callback = typeof (callback) === "function";

            for (var temperature = 100 * distance(path.access(0), path.access(1)); temperature > 1e-6; temperature *= temp_coeff) {
                path.change(temperature);

                if (has_callback) {
                    callback(path.order);
                }
            }

            return path.order;
        };
    }

    this.point = function (id, x, y) {
        this.id = id;
        this.x = x;
        this.y = y;
    };

    var calcPathLength = function (points, order) {
        var len = 0;

        for (var i = 0, to = order.length - 1; i < to; i++) {
            var p1 = points[order[i]],
                p2 = points[order[i + 1]];

            len += Math.sqrt((p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y));
        }

        return len;
    }

    this.orderFeatures = function (crs, featureCollection, startPoint) {
        var points = [];

        if (startPoint) {
            var coords = webgis.fromWGS84(crs, startPoint.lng, startPoint.lat);
            points.push(new this.point('', coords.x, coords.y));
        }

        for (var f in featureCollection.features) {
            var feature = featureCollection.features[f];

            var coords = webgis.fromWGS84(crs, feature.geometry.coordinates[0], feature.geometry.coordinates[1]);
            var point = new this.point(feature.oid, coords.x, coords.y);

            points.push(point);
        }

        //console.log(points);

        var order = null, pathLength=0;

        for (var i = 0; i < 15; i++) {  // try some times to get the best result
            var tryOrder = tspAlg.solve(points);
            var length = calcPathLength(points, tryOrder);
            //console.log('order', order);

            if (order == null || length < pathLength) {
                order = tryOrder;
                pathLength = length;
            }
        }

        var orderedFeatures = [], indexOffset = startPoint ? 1 : 0;
        for (var o in order) {
            var index = order[o] - indexOffset;
            if (index < 0) {
                continue;
            }

            var feature = featureCollection.features[index];

            orderedFeatures.push({
                type: 'Feature',
                oid: feature.oid,
                bounds: feature.bounds,
                geometry: feature.geometry,
                properties: feature.properties
            });
        }

        return {
            type: "FeatureCollection",
            features: orderedFeatures,
            bounds: featureCollection.bounds,
            metadata: {
                connect: true,
                reorderAble: true,
                tool: 'tsp',
                startPoint: startPoint,
                dynamicContentDef: featureCollection.metadata ? featureCollection.metadata.dynamicContentDef : null
            }
        };
    }
    this.orderFeaturesWithUI = function (map) {
        if (map) {
            var features = map.queryResultFeatures.features();

            var cancelTracker = new webgis.cancelTracker();
            webgis.showProgress('Aktuelle Position wird abgefragt...', null, cancelTracker);

            webgis.currentPosition.get({
                highAccuracy: true,
                maxWatch: webgis.currentPosition.maxWatch,
                onSuccess: function (pos) {
                    webgis.hideProgress('Aktuelle Position wird abgefragt...');

                    var lng = pos.coords.longitude,
                        lat = pos.coords.latitude,
                        acc = pos.coords.accuracy / (webgis.calc.R) * 180.0 / Math.PI;

                    webgis.confirm({
                        title: "Rundreise",
                        iconUrl: webgis.css.imgResource('roundtrip-128.png', 'tools'),
                        message: 'Mit dieser Funktion werden die Marker in der Reihenfolge einer optimalen Rundreise sortiert',
                        okText: 'Marker sortieren',
                        okNoneBlocking: true,
                        onOk: function (sender) {
                            var orderedFeatures = webgis.tsp.orderFeatures(map.calcCrs(), features, { lng: lng, lat: lat });
                            //console.log('orderedFeatures', orderedFeatures);

                            // remove current dynamic content
                            if (map.hasCurrentDynamicContent()) {
                                map.unloadDynamicContent();
                            }

                            map.ui.showQueryResults(orderedFeatures, true, orderedFeatures.metadata ? orderedFeatures.metadata.dynamicContentDef : null);
                        }
                    });

                },
                onError: function (err) {
                    webgis.hideProgress('Aktuelle Position wird abgefragt...');
                    webgis.alert('Ortung nicht möglich: ' + (err ? err.message + " (Error Code " + err.code + ")" : ""), "Fehler");
                }
            });
        }
    }
};