// Geometry type 'measure_grid' (Messgitter)
//   alignToGrid false => 3 vertices: base edge (vertex 0,1) + depth (vertex 2)
//   alignToGrid true  => 2 vertices: opposite corners, grid lines on multiples of the cell size (calc crs)
//   further clicks replace the last vertex, the sketch stays open (properties can still be changed)
//   while the last vertex is missing, the grid is previewed at the mouse position
// One framework element (L.measureGrid), see webgis-graphics.measure_grid.js
// See webgis.sketch.geometry.js (registry) and webgis.sketch.geometry.base.js (defaults, _line)

webgis.sketch.geometryTypes.define('measure_grid', '_line', function (ctx) {
    "use strict";

    var sketch = ctx.sketch, state = ctx.state;

    this.styleIndex = 1;
    this.cellWidth = 10;     // meters
    this.cellHeight = 10;    // meters
    this.labels = 'cells';   // cells | border | coords | none
    this.alignToGrid = false;
    this.elementOptions = {
        cellWidth: ['getCellWidth', 'setCellWidth'],
        cellHeight: ['getCellHeight', 'setCellHeight'],
        labels: ['getLabels', 'setLabels'],
        alignToGrid: ['getAlignToGrid', 'setAlignToGrid']
    };

    var _requiredVertexCount = function () {
        return L.MeasureGrid.requiredVertexCount(sketch.getMeasureGridAlignToGrid());
    };
    var _currentElement = function () {
        return state.frameworkElements[state.currentFrameworkIndex];
    };
    var _latLngs = function () {
        return state.vertices.map(function (v) { return L.latLng(v.y, v.x); });
    };
    var _format = function (value) {
        return L.ViewAngle.formatNumber(value);
    };

    // grid of the current vertices => see L.MeasureGrid.calc
    this.getGrid = function () {
        return L.MeasureGrid.calc(_latLngs(), {
            cellWidth: sketch.getMeasureGridCellWidth(),
            cellHeight: sketch.getMeasureGridCellHeight(),
            alignToGrid: sketch.getMeasureGridAlignToGrid()
        });
    };

    this.isValid = function () {
        return this.getGrid() !== null;
    };

    this.calcProperties = function (xParam, yParam) {
        var grid = this.getGrid();
        return {
            circumference: grid ? 2.0 * (grid.width + grid.height) : 0,
            area: grid ? grid.width * grid.height : 0,
            set_values: true
        };
    };

    this.infoTextLines = function (infoText, properties) {
        var grid = this.getGrid();
        if (!grid)
            return;

        infoText.push(webgis.l10n.get('measure-grid-cells') + ': ' + grid.cols + ' × ' + grid.rows + ' = ' + grid.cells);
        infoText.push(webgis.l10n.get('measure-grid-frame') + ': ' + _format(grid.width) + ' m × ' + _format(grid.height) + ' m');
        infoText.push(webgis.l10n.get('area') + ': ' + _format(grid.width * grid.height) + ' m²');
        infoText.push(webgis.l10n.get('measure-grid-orientation') + ': ' + _format(grid.azimuth) + '°');
        if (grid.crsId > 0) {
            infoText.push(webgis.l10n.get('measure-grid-origin') + ' (A1): ' + _format(grid.origin.X) + ' / ' + _format(grid.origin.Y) + ' (EPSG:' + grid.crsId + ')');
        }
        if (grid.tooMany) {
            infoText.push(L.MeasureGrid.tooManyCellsText(grid.cells));
        }
    };

    // fixed number of vertices: further clicks replace the last vertex
    this.tryAddVertex = function (vertex, vertices, fireEvents, coords) {
        if (state.vertices.length < _requiredVertexCount())
            return false;

        if (fireEvents)
            this._replaceLastVertex(vertex);
        return true;
    };

    // last vertex missing => preview of the grid at the mouse position (instead of the mover line)
    this.handleMapMouseMove = function (latlng) {
        if (sketch.isSketchMoving() || sketch.isSketchRotating())
            return false;

        var element = _currentElement(), required = _requiredVertexCount(), count = state.vertices.length;

        if (count >= 1 && count === required - 1) {
            sketch.hideMoverLine();
            sketch.hideTraceLine();
            if (element && element.setPreviewLatLng)
                element.setPreviewLatLng(latlng);
            sketch.fireCurrentState();
            return true;
        }

        if (element && element.setPreviewLatLng)
            element.setPreviewLatLng(null);

        if (count >= required) {
            sketch.hideMoverLine();
            sketch.hideTraceLine();
            sketch.fireCurrentState();
            return true;
        }

        return false;
    };

    // switched to aligned (2 vertices) => remove the depth vertex
    this.onAlignToGridChanged = function () {
        var required = _requiredVertexCount();
        for (var i = state.vertices.length - 1; i >= required; i--) {
            sketch.removeVertex(i, true);
        }
    };

    // moves the grid, so that the origin (corner of A1) is at X/Y (calc crs of the grid)
    // aligned => the shift is rounded to multiples of the cell size (the grid lines stay on the multiples)
    this.setOrigin = function (X, Y) {
        var grid = this.getGrid();
        X = parseFloat(X);
        Y = parseFloat(Y);
        if (!grid || grid.crsId === 0 || !isFinite(X) || !isFinite(Y))
            return false;

        var dX = X - grid.origin.X, dY = Y - grid.origin.Y;
        if (grid.aligned) {
            dX = Math.round(dX / grid.cw) * grid.cw;
            dY = Math.round(dY / grid.ch) * grid.ch;
        }
        if (dX === 0 && dY === 0)
            return true;

        var count = Math.min(state.vertices.length, _requiredVertexCount());
        var calc = L.ViewAngle.toCalc(_latLngs().slice(0, count));
        if (calc.crsId !== grid.crsId)
            return false;

        var map = sketch.map, crs = map ? (map.hasDynamicCalcCrs() ? map.crs : map.calcCrs()) : null;

        sketch.addUndo(webgis.l10n.get('measure-grid-undo-origin'));
        for (var i = 0; i < count; i++) {
            var vertex = state.vertices[i], latLng = calc.toLatLng(calc.points[i].X + dX, calc.points[i].Y + dY);
            vertex.x = latLng.lng;
            vertex.y = latLng.lat;
            if (crs) {
                var projected = webgis.fromWGS84(crs, latLng.lat, latLng.lng);
                if (projected) {
                    vertex.X = projected.x;
                    vertex.Y = projected.y;
                    vertex.projEvent = 'changed';
                }
            }
        }

        sketch.redraw(true);
        sketch.redrawMarkers();  // redraw(true) keeps the existing vertex markers
        sketch.events.fire('onchanged', sketch);
        return true;
    };

    this.createFrameworkElement = function (latLngs) {
        var measureGrid = L.measureGrid(latLngs || [], {
            color: sketch.getColor(),
            weight: sketch.getWeight(),
            dashArray: sketch._toDashArray(sketch.getLineStyle()),
            fontSize: sketch.getTextSize(),
            cellWidth: sketch.getMeasureGridCellWidth(),
            cellHeight: sketch.getMeasureGridCellHeight(),
            labels: sketch.getMeasureGridLabels(),
            alignToGrid: sketch.getMeasureGridAlignToGrid()
        });
        measureGrid._webgis = { geometryType: state.geometryType, lineStyle: sketch.getLineStyle() };
        return measureGrid;
    };

    this.fromFrameworkElement = function (frameworkElement, jsonGeometry) {
        jsonGeometry.type = 'measure_grid';
        this.cellWidth = frameworkElement.getCellWidth();
        this.cellHeight = frameworkElement.getCellHeight();
        this.labels = frameworkElement.getLabels();
        this.alignToGrid = frameworkElement.getAlignToGrid();
    };
});
