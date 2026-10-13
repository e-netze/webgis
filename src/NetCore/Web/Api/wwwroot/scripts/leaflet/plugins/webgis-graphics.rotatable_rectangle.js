L.RotatableRectangle = L.LayerCollection.extend({
    options: {
        color: '#ff0000',
        weight: 2,
        fillColor: '#ffff00',
        fillOpacity: 0.2,
        width: 500,
        height: 300,
        rotation: 0,
        label: '',
        markerOptions: {
            draggable: true, 
            icon: L.icon({
                iconUrl: webgis.css.imgResource('rotate_26.png', 'markers'),
                iconSize: [26, 26],
                iconAnchor: [13, 13],
                popupAnchor: [-13, -13]
            })
        }
    },
    _center: null,
    _rotation: 0,
    _marker: null,

    _setLatLngs: function (latLngs) {
        if (latLngs && latLngs.length > 0) {
            this.setLatLng(latLngs[0]);
        }
    },

    setLatLng: function (latLng) {
        this._center = latLng;
        this.redraw();
    },

    setRotation: function (rotation) {
        this._rotation = rotation;
        this.redraw();
    },

    getLatLngs: function () {
        return [this._center];
    },
    getBounds: function() {
        var rectCoords = this._calcRectangleCoords(this._center, this.options.width, this.options.height, this._rotation);
        return L.latLngBounds(rectCoords);
    },
    onAdd: function () {
        this.redraw();
    },

    //onRemove: function () {
    //    this.removeAllChildLayers();
    //    if (this._marker) {
    //        this._map.removeLayer(this._marker);
    //        this._marker = null;
    //    }
    //},

    redraw: function () {
        this.removeAllChildLayers();
        if (!this._center || !this._map) return;

        // Calculate rectangle corners
        let rectCoords = this._calcRectangleCoords(this._center, this.options.width, this.options.height, this._rotation);

        // Draw rectangle
        let rectangle = L.polygon(rectCoords, {
            color: this.options.color,
            weight: this.options.weight,
            fillColor: this.options.fillColor,
            fillOpacity: this.options.fillOpacity
        });
        this.addChildLayer(rectangle);

        let northArrow = L.polyline(this._calcNorthArrowFromRectangle(rectCoords), {
            color: 'black',
            weight: 1
        }); 
        this.addChildLayer(northArrow);

        let hLine = L.polyline(this._calcHLineFromRectangle(rectCoords), {
            color: 'black',
            weight: 1,
            dashArray: '4,4'
        });
        this.addChildLayer(hLine);
        let vLine = L.polyline(this._calcVLineFromRectangle(rectCoords), {
            color: 'black',
            weight: 1,
            dashArray: '4,4'
        });
        this.addChildLayer(vLine);

        if (this.options.label) {
            let label = L.svgLabel(rectCoords[1], { // top left
                text: this.options.label,
                fontSize: 14,
                fontWeight: 'bold',
                anchor: 'start',
                alignmentBaseline: 'hanging',
                rotation: -this._rotation
            });
            this.addChildLayer(label);
        }

        // Calculate marker position (top right corner)
        let markerPos = rectCoords[2]; // [top right]

        // Add draggable marker
        if (!this._marker) {
            this._marker = L.marker(markerPos, this.options.markerOptions);
            this._marker.on('drag', this._onMarkerDrag, this);
            this._marker.on('dragend', this._onMarkerDragEnd, this);
            //this._marker.addTo(this._map);
            this.addChildLayer(this._marker);
        } else {
            this._marker.setLatLng(markerPos);
        }
    },

    _calcRectangleCoords: function (center, width, height, rotation) {
        // Project center to map coordinates
        let p = [{ x: center.lng, y: center.lat }];
        let xProp = "x", yProp = "y";
        if (webgis.calc._canProject()) {
            p = webgis.calc._projectToCalcCrs(p);
            xProp = "X"; yProp = "Y";
        }
        let cx = p[0][xProp], cy = p[0][yProp];

        // Rectangle corners before rotation
        let corners = [
            { X: cx - width / 2, Y: cy - height / 2 }, // bottom left
            { X: cx - width / 2, Y: cy + height / 2 }, // top left
            { X: cx + width / 2, Y: cy + height / 2 }, // top right
            { X: cx + width / 2, Y: cy - height / 2 }, // bottom right
        ];

        // Apply rotation
        if (rotation && rotation !== 0) {
            const sinA = Math.sin(rotation * Math.PI / 180.0);
            const cosA = Math.cos(rotation * Math.PI / 180.0);
            for (let i = 0; i < corners.length; i++) {
                let dx = corners[i][xProp] - cx;
                let dy = corners[i][yProp] - cy;
                corners[i][xProp] = cx + (dx * cosA - dy * sinA);
                corners[i][yProp] = cy + (dx * sinA + dy * cosA);
            }
        }

        // Unproject if needed
        if (webgis.calc._canProject()) {
            corners = webgis.calc._unprojectFromCalcCrs(corners);
        }

        // Convert to LatLng
        return corners.map(c => L.latLng(c.y, c.x));
    },
    _calcNorthArrowFromRectangle: function (rectCoords) {
        return [
            L.latLng(rectCoords[0].lat, rectCoords[0].lng), // bottom left
            L.latLng((rectCoords[1].lat + rectCoords[2].lat) * 0.5, (rectCoords[1].lng + rectCoords[2].lng) * 0.5), // top
            L.latLng(rectCoords[3].lat, rectCoords[3].lng)  // bottom right
        ];
    },
    _calcHLineFromRectangle: function (rectCoords) {
        return [
            L.latLng((rectCoords[0].lat + rectCoords[1].lat) * 0.5, (rectCoords[0].lng + rectCoords[1].lng) * 0.5), // left
            L.latLng((rectCoords[2].lat + rectCoords[3].lat) * 0.5, (rectCoords[2].lng + rectCoords[3].lng) * 0.5)  // right
        ]
    },
    _calcVLineFromRectangle: function (rectCoords) {
        return [
            L.latLng((rectCoords[0].lat + rectCoords[3].lat) * 0.5, (rectCoords[0].lng + rectCoords[3].lng) * 0.5), // bottom
            L.latLng((rectCoords[1].lat + rectCoords[2].lat) * 0.5, (rectCoords[1].lng + rectCoords[2].lng) * 0.5)  // top
        ]
    },
            
    _onMarkerDrag: function (e) {
        if (!this._center) return;
        let markerLatLng = this._marker.getLatLng();

        var p = [
            { x: this._center.lng, y: this._center.lat },
            { x: markerLatLng.lng, y: markerLatLng.lat }
        ];
        let xProp = "x", yProp = "y";
        if (webgis.calc._canProject()) {
            p = webgis.calc._projectToCalcCrs(p);
            xProp = "X"; yProp = "Y";
        }
        
        // Calculate angle between center and marker
        let dx = p[1][xProp] - p[0][xProp];
        let dy = p[1][yProp] - p[0][yProp];
        let angle = Math.atan2(dy, dx) * 180.0 / Math.PI;
        let angleR = Math.atan2(this.options.height, this.options.width) * 180.0 / Math.PI;
        this._rotation = angle - angleR;

        let rectCoords = this._calcRectangleCoords(this._center, this.options.width, this.options.height, this._rotation);
        this.getChildLayer(0).setLatLngs(rectCoords); // Update rectangle
        this.getChildLayer(1).setLatLngs(this._calcNorthArrowFromRectangle(rectCoords)); // Update north arrow
        this.getChildLayer(2).setLatLngs(this._calcHLineFromRectangle(rectCoords)); // Update H line
        this.getChildLayer(3).setLatLngs(this._calcVLineFromRectangle(rectCoords)); // Update V line
        this.getChildLayer(4).setLatLng(rectCoords[1]); // Update label position
        this.getChildLayer(4).setRotation(-this._rotation);
    },

    _onMarkerDragEnd: function (e) {
        //console.log("onMarkerDragEnd", this._rotation);
        if (!this._center || !this._marker) return;
        let rectCoords = this._calcRectangleCoords(this._center, this.options.width, this.options.height, this._rotation);
        this._marker.setLatLng(rectCoords[2]); // Ensure marker is at top right corner

        // Optionally fire event or update state
        this.fireEvent('rotated', { sender: this, rotation: this._rotation });
    },

    toGeoJSON: function () {
        let geoJson = {
            geometry: {
                type: 'Polygon',
                coordinates: []
            }
        };
        let rectCoords = this._calcRectangleCoords(this._center, this.options.width, this.options.height, this._rotation);
        geoJson.geometry.coordinates.push(rectCoords.map(ll => [ll.lng, ll.lat]));
        return geoJson;
    }
});

L.rotatableRectangle = function (latLngs, options) {
    return new L.RotatableRectangle(latLngs, options);
};
