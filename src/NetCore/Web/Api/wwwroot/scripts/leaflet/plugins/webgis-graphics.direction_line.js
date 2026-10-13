L.DirectionLine = L.LayerCollection.extend({


    _setLatLngs: function (latLngs) {
        
    },

    getLatLngs: function () {
        var latLngs = [];

        if (this.options.vertices) {
            for (var v in this.options.vertices) {
                latLngs.push({ lng: this.options.vertices[v].x, lat: this.options.vertices[v].y });
            }
        }

        return latLngs;
    },

    setVertices: function (vertices) {
        this.options.vertices = vertices;
        this.rebuild();
    },
    getVertices: function () {
        return this.options.vertices;
    },

    onAdd: function () {
        this.setVertices(this.options.vertices); 
    },

    setDirectionVertex: function (vertex) {
        if (this.options.vertices && this.options.vertices.length === 2) {
            this.options.vertices[1] = vertex;

            this.rebuild();
        }
    },

    rebuild: function () {
        if (!this.options.sketch || !this.options.vertices || this.options.vertices.length !== 2) {
            return;
        }

        var polyline = this.getChildLayer(0);
        //var xPoint1 = this.getChildLayer(1);
        //var xPoint2 = this.getChildLayer(2);

        webgis.complementProjected(this.options.sketch.map.calcCrs(), this.options.vertices);

        var r = {
            x: this.options.vertices[1].X - this.options.vertices[0].X, y: this.options.vertices[1].Y - this.options.vertices[0].Y
        };
        var len = Math.sqrt(r.x * r.x + r.y * r.y);
        if (len < 1e-2) {
            return;
        }

        var extend = this.options.sketch.map.getProjectedExtent(), w = extend[2] - extend[0], h = extend[3] - extend[1];
        var diagLen = Math.sqrt(w * w + h * h), from = 0, to = 1;

        if (diagLen > len) {
            from = -Math.min(100, parseInt(diagLen / len) + 1);
            to = -from;
        }

        //console.log('from-to', from, to, diagLen, len);

        var vertices = [];
        for (var t = from; t <= to; t++) {
            vertices.push({ X: this.options.vertices[0].X + r.x * t, Y: this.options.vertices[0].Y + r.y * t });
        }

        webgis.complementWGS84(this.options.sketch.map.calcCrs(), vertices);

        var latLngs = [];
        for (var v in vertices) {
            latLngs.push({ lng: vertices[v].x, lat: vertices[v].y });
        }

        if (!polyline) {
            polyline = L.polyline(latLngs, this.options);
            this.addChildLayer(polyline);
        } else {
            polyline.setLatLngs(latLngs);
        }

        //if (!xPoint1) {
        //    xPoint1 = L.xPoint({ lng: this.options.vertices[0].x, lat: this.options.vertices[0].y }, { color: this.options.color, weight: this.options.weight });
        //    this.addChildLayer(xPoint1);
        //}
    }
});

L.directionLine = function (options) { 
    return new L.DirectionLine(null, options);
};
