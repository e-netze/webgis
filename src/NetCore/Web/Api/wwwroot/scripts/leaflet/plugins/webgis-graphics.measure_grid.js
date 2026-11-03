// MapMarkup measure tool 'measure_grid' (Messgitter):
//   alignToGrid false => vertex 0,1 => base edge (origin = vertex 0, direction of the columns), vertex 2 => depth (rows)
//   alignToGrid true  => vertex 0,1 => opposite corners, the grid is axis-parallel and its lines lie on multiples
//                        of the cell width/height (calc crs), the origin is the corner on the side of vertex 0
//   cell A1 is at the origin, columns A, B, ... Z, AA, ... run along the base edge, rows 1, 2, ... in the depth direction
//   labels: cells (each cell), border (column/row names at the border), coords (aligned only: coordinates of the grid lines), none
//   labels are hidden (cells) or thinned out (border, coords) if they don't fit
// Calculation in the calc crs (L.ViewAngle.toCalc), the server (print, export) uses the same geometry: MeasureGridElement.cs
L.MeasureGrid = L.LayerCollection.extend({
    options: {
        color: '#000000',
        weight: 2,
        dashArray: null,
        fontSize: 14,
        cellWidth: 10,       // meters
        cellHeight: 10,      // meters
        labels: 'cells',     // cells | border | coords | none
        alignToGrid: false,
        attributes: {}
    },

    _calcCrs: 0,
    _latLngs: null,
    _previewLatLng: null,
    _grid: null,
    _lineCount: 0,
    _labelKey: null,

    _setLatLngs: function (latLngs) {
        this.setLatLngs(latLngs);
    },

    _setStyle: function (style) {
        for (var i in style) {
            switch (i.toLocaleLowerCase()) {
                case 'font-size':
                case 'fontsize':
                case 'size':
                    this.options.fontSize = style[i];
                    break;
            }
        }
    },

    // the children (lines, labels) get their own styles => rebuild instead of passing the style to the children
    setStyle: function (style) {
        L.setOptions(this, style);
        if (this._setStyle) this._setStyle(style);
        this.rebuild();
        return this;
    },

    onAdd: function () {
        this._map.on('zoomend', this._onZoomEnd, this);
        this.rebuild();
    },
    onRemove: function () {
        this._map.off('zoomend', this._onZoomEnd, this);
        L.LayerCollection.prototype.onRemove.call(this);
    },
    setLatLngs: function (latLngs) {
        this._latLngs = latLngs;
        this.rebuild();
    },
    addLatLng: function (latLng) {
        if (this._latLngs)
            this._latLngs.push(latLng);
        else
            this._latLngs = [latLng];

        this.rebuild();
    },
    getLatLngs: function () { return this._latLngs; },
    getCalcCrs: function () { return this._calcCrs; },

    // sketch: the missing last vertex at the mouse position (lines only), null => no preview
    setPreviewLatLng: function (latLng) {
        if (!latLng && !this._previewLatLng)
            return;
        this._previewLatLng = latLng;
        this.rebuild();
    },

    setCellWidth: function (width) {
        width = parseFloat(width);
        if (!(width > 0))
            return;
        this.options.cellWidth = width;
        this.rebuild();
    },
    getCellWidth: function () { return this.options.cellWidth; },
    setCellHeight: function (height) {
        height = parseFloat(height);
        if (!(height > 0))
            return;
        this.options.cellHeight = height;
        this.rebuild();
    },
    getCellHeight: function () { return this.options.cellHeight; },
    setLabels: function (labels) {
        this.options.labels = labels;
        this.rebuild();
    },
    getLabels: function () { return this.options.labels; },
    setAlignToGrid: function (align) {
        this.options.alignToGrid = align === true;
        this.rebuild();
    },
    getAlignToGrid: function () { return this.options.alignToGrid; },

    // grid of the current vertices (without preview) => see L.MeasureGrid.calc
    getGrid: function () {
        return L.MeasureGrid.calc(this._latLngs, this.options);
    },

    redraw: function () {
        this.rebuild();
    },
    rebuild: function () {
        if (!this._map)
            return;

        this.trimChildren(0);
        this._grid = null;
        this._lineCount = 0;
        this._labelKey = null;

        var latLngs = this._latLngs ? this._latLngs.slice() : [];
        var required = L.MeasureGrid.requiredVertexCount(this.options.alignToGrid), preview = false;
        if (this._previewLatLng && latLngs.length === required - 1) {
            latLngs.push(this._previewLatLng);
            preview = true;
        }

        var grid = L.MeasureGrid.calc(latLngs, this.options);
        if (!grid)
            return;
        if (!preview)
            this._calcCrs = grid.crsId;

        var lineOptions = { color: this.options.color, weight: this.options.weight, opacity: 1.0, dashArray: this.options.dashArray, fill: false };

        if (grid.tooMany) {  // frame only
            this._addLine([grid.latLng(0, 0), grid.latLng(grid.cols, 0), grid.latLng(grid.cols, grid.rows), grid.latLng(0, grid.rows), grid.latLng(0, 0)], lineOptions);
            this._addLabel(grid.latLng(grid.cols / 2.0, grid.rows / 2.0), L.MeasureGrid.tooManyCellsText(grid.cells), 'middle');
            this._lineCount = this.getChildLayerCount();
            return;
        }

        for (var c = 0; c <= grid.cols; c++) {
            this._addLine([grid.latLng(c, 0), grid.latLng(c, grid.rows)], lineOptions);
        }
        for (var r = 0; r <= grid.rows; r++) {
            this._addLine([grid.latLng(0, r), grid.latLng(grid.cols, r)], lineOptions);
        }
        this._lineCount = this.getChildLayerCount();

        if (preview)
            return;

        this._grid = grid;
        this._addLabels(grid, this._labelState(grid));
    },
    _onZoomEnd: function () {
        if (!this._map || !this._grid)
            return;

        var state = this._labelState(this._grid);
        if (state && state.key === this._labelKey)  // same labels and positions
            return;

        this.trimChildren(this._lineCount);
        this._addLabels(this._grid, state);
    },

    // which labels fit at the current zoom => { key, mode, showCells, colStep, rowStep, colOffset, rowOffset } or null
    _labelState: function (grid) {
        var mode = this.options.labels || 'cells';
        if (mode === 'none')
            return null;
        if (mode === 'coords' && !grid.aligned)
            mode = 'border';

        var map = this._map,
            p0 = map.latLngToLayerPoint(grid.latLng(0, 0)),
            pu = map.latLngToLayerPoint(grid.latLng(1, 0)),
            pv = map.latLngToLayerPoint(grid.latLng(0, 1));
        var du = { x: pu.x - p0.x, y: pu.y - p0.y }, dv = { x: pv.x - p0.x, y: pv.y - p0.y };
        var lu = Math.sqrt(du.x * du.x + du.y * du.y), lv = Math.sqrt(dv.x * dv.x + dv.y * dv.y);
        if (lu === 0 || lv === 0)
            return null;

        var fontSize = parseFloat(this.options.fontSize) || 14, textHeight = fontSize * 1.2, charWidth = fontSize * 0.6;
        // extent of a text (chars) in the direction of an axis (dir, len => pixels of one cell)
        var extent = function (dir, len, chars) {
            return Math.abs(dir.x) / len * chars * charWidth + Math.abs(dir.y) / len * textHeight;
        };
        var step = function (dir, len, chars) {
            return Math.max(1, Math.ceil((extent(dir, len, chars) + 4) / len));
        };

        var state = { mode: mode, showCells: false, colStep: 1, rowStep: 1, colOffset: 0, rowOffset: 0 };

        if (mode === 'cells') {
            var chars = L.MeasureGrid.columnName(grid.cols - 1).length + String(grid.rows).length;
            state.showCells = extent(du, lu, chars) + 4 <= lu && extent(dv, lv, chars) + 4 <= lv;
            state.key = 'cells:' + state.showCells;
            return state;
        }

        var colChars, rowChars;
        if (mode === 'coords') {
            colChars = Math.max(L.MeasureGrid.formatCoordinate(grid.lineX(0)).length, L.MeasureGrid.formatCoordinate(grid.lineX(grid.cols)).length);
            rowChars = Math.max(L.MeasureGrid.formatCoordinate(grid.lineY(0)).length, L.MeasureGrid.formatCoordinate(grid.lineY(grid.rows)).length);
        } else {
            colChars = L.MeasureGrid.columnName(grid.cols - 1).length;
            rowChars = String(grid.rows).length;
        }
        state.colStep = step(du, lu, colChars);
        state.rowStep = step(dv, lv, rowChars);
        // outside the frame: column labels before row 0, row labels before column 0 (in cell units)
        state.colOffset = (extent(dv, lv, colChars) / 2.0 + 4) / lv;
        state.rowOffset = (extent(du, lu, rowChars) / 2.0 + 4) / lu;
        state.key = null;  // positions depend on the zoom => always redraw
        return state;
    },
    _addLabels: function (grid, state) {
        this._labelKey = state ? state.key : null;
        if (!state)
            return;

        var c, r;
        switch (state.mode) {
            case 'cells':
                if (!state.showCells)
                    return;
                for (c = 0; c < grid.cols; c++) {
                    for (r = 0; r < grid.rows; r++) {
                        this._addLabel(grid.latLng(c + 0.5, r + 0.5), L.MeasureGrid.cellName(c, r), 'middle');
                    }
                }
                break;
            case 'border':
                for (c = 0; c < grid.cols; c += state.colStep) {
                    this._addLabel(grid.latLng(c + 0.5, -state.colOffset), L.MeasureGrid.columnName(c), 'middle');
                }
                for (r = 0; r < grid.rows; r += state.rowStep) {
                    this._addLabel(grid.latLng(-state.rowOffset, r + 0.5), String(r + 1), 'middle');
                }
                break;
            case 'coords':
                for (c = 0; c <= grid.cols; c += state.colStep) {
                    this._addLabel(grid.latLng(c, -state.colOffset), L.MeasureGrid.formatCoordinate(grid.lineX(c)), 'middle');
                }
                for (r = 0; r <= grid.rows; r += state.rowStep) {
                    this._addLabel(grid.latLng(-state.rowOffset, r), L.MeasureGrid.formatCoordinate(grid.lineY(r)), 'middle');
                }
                break;
        }
    },
    _addLine: function (latLngs, options) {
        var line = L.polyline(latLngs, options);
        this.addChildLayer(line);
        line.on('click', function (e) {
            this.fireEvent('click', e);
        }, this);
    },
    _addLabel: function (latLng, text, anchor) {
        this.addChildLayer(L.svgLabel(latLng, {
            text: text,
            fontSize: this.options.fontSize,
            fontColor: this.options.color,  // text color = line color
            anchor: anchor || 'middle',
            alignmentBaseline: 'central'
        }));
    },
    toGeoJSON: function () {
        var geoJson = {
            geometry: {
                type: 'LineString',
                coordinates: []
            }
        };

        if (this._latLngs) {
            for (var l in this._latLngs) {
                geoJson.geometry.coordinates.push([this._latLngs[l].lng, this._latLngs[l].lat]);
            }
        }

        return geoJson;
    }
});

L.measureGrid = function (latLngs, options) {
    return new L.MeasureGrid(latLngs, options);
};

// more cells => only the frame is drawn (performance), same limit on the server (print, export)
L.MeasureGrid.maxCells = 2500;

L.MeasureGrid.requiredVertexCount = function (alignToGrid) {
    return alignToGrid === true ? 2 : 3;
};

// latLngs, options { cellWidth, cellHeight, alignToGrid } =>
//   { crsId, aligned, origin: {X,Y}, u: {x,y}, v: {x,y}, cw, ch, cols, rows, cells, tooMany, width, height, azimuth,
//     corner(c, r) => {X,Y}, latLng(c, r), lineX(c), lineY(r) } or null
//   c, r: cell units (may be fractional), corner(c, r) = origin + u * c * cw + v * r * ch
L.MeasureGrid.calc = function (latLngs, options) {
    var aligned = options.alignToGrid === true, required = L.MeasureGrid.requiredVertexCount(aligned);
    var cw = parseFloat(options.cellWidth), ch = parseFloat(options.cellHeight);
    if (!latLngs || latLngs.length < required || !(cw > 0) || !(ch > 0))
        return null;

    var calc = L.ViewAngle.toCalc(latLngs.slice(0, required));
    var a = calc.points[0], b = calc.points[1], eps = 1e-6;
    var origin, u, v, cols, rows;

    if (aligned) {
        var minX = Math.floor(Math.min(a.X, b.X) / cw + eps) * cw, maxX = Math.ceil(Math.max(a.X, b.X) / cw - eps) * cw,
            minY = Math.floor(Math.min(a.Y, b.Y) / ch + eps) * ch, maxY = Math.ceil(Math.max(a.Y, b.Y) / ch - eps) * ch;
        cols = Math.max(1, Math.round((maxX - minX) / cw));
        rows = Math.max(1, Math.round((maxY - minY) / ch));
        maxX = minX + cols * cw;
        maxY = minY + rows * ch;
        origin = { X: a.X <= b.X ? minX : maxX, Y: a.Y <= b.Y ? minY : maxY };
        u = { x: a.X <= b.X ? 1 : -1, y: 0 };
        v = { x: 0, y: a.Y <= b.Y ? 1 : -1 };
    } else {
        var dx = b.X - a.X, dy = b.Y - a.Y, len = Math.sqrt(dx * dx + dy * dy);
        if (len === 0)
            return null;

        var p = calc.points[2];
        u = { x: dx / len, y: dy / len };
        v = { x: -u.y, y: u.x };
        var depth = (p.X - a.X) * v.x + (p.Y - a.Y) * v.y;
        if (depth < 0) {
            v = { x: -v.x, y: -v.y };
            depth = -depth;
        }
        cols = Math.max(1, Math.ceil(len / cw - eps));
        rows = Math.max(1, Math.ceil(depth / ch - eps));
        origin = { X: a.X, Y: a.Y };
    }

    var azimuth = (90.0 - Math.atan2(u.y, u.x) * 180.0 / Math.PI) % 360.0;
    if (azimuth < 0) azimuth += 360.0;

    var grid = {
        crsId: calc.crsId,
        aligned: aligned,
        origin: origin, u: u, v: v,
        cw: cw, ch: ch,
        cols: cols, rows: rows,
        cells: cols * rows,
        width: cols * cw, height: rows * ch,
        azimuth: azimuth
    };
    grid.tooMany = grid.cells > L.MeasureGrid.maxCells;
    grid.corner = function (c, r) {
        return { X: origin.X + u.x * c * cw + v.x * r * ch, Y: origin.Y + u.y * c * cw + v.y * r * ch };
    };
    grid.latLng = function (c, r) {
        var corner = grid.corner(c, r);
        return calc.toLatLng(corner.X, corner.Y);
    };
    // aligned: coordinate of the grid line c (X) resp. r (Y)
    grid.lineX = function (c) { return origin.X + u.x * c * cw; };
    grid.lineY = function (r) { return origin.Y + v.y * r * ch; };

    return grid;
};

// 0 => A, 25 => Z, 26 => AA, ...
L.MeasureGrid.columnName = function (col) {
    var name = '', n = col + 1;
    while (n > 0) {
        var m = (n - 1) % 26;
        name = String.fromCharCode(65 + m) + name;
        n = Math.floor((n - 1) / 26);
    }
    return name;
};

// 0-based column/row => A1, B1, ...
L.MeasureGrid.cellName = function (col, row) {
    return L.MeasureGrid.columnName(col) + (row + 1);
};

L.MeasureGrid.formatCoordinate = function (value) {
    return String(Math.round(value * 100.0) / 100.0);
};

L.MeasureGrid.tooManyCellsText = function (cells) {
    return webgis.l10n.get('measure-grid-too-many-cells')
        .replace('{0}', cells)
        .replace('{1}', L.MeasureGrid.maxCells);
};
