webgis.chart = function (map) {
    "use strict";
    this.map = map;
    this.chartDef = null;
    this.sketch = null;
    this._c3chart = null;
    this._target = null;
    this.init = function (chartDef, callback) {
        this.chartDef = chartDef;
        if (!window.c3) {
            webgis.loadScript(webgis.baseUrl + '/scripts/d3/d3.js', '', function () {
                webgis.loadScript(webgis.baseUrl + '/scripts/c3/c3.js', webgis.baseUrl + '/scripts/c3/c3.css', callback);
            });
        }
        else {
            callback();
        }
    };
    this.create = function (options) {
        if (this._c3chart)
            this._c3chart.destroy();
        this._c3chart = c3.generate({
            bindto: this._target = $(options.target).get(0),
            data: this.chartDef.data,
            grid: this.chartDef.grid,
            axis: this.chartDef.axis,
            point: this.chartDef.point
        });
        this._c3chart.webgisChart = this;
    };
    this.destroy = function () {
        if (this._c3chart) {
            this._c3chart.destroy();
            this._c3chart = null;
        }
        if (this.sketch) {
            // ToDo: unbind event...
            this.sketch = null;
        }
    };
    this.bindSketch = function (sketch) {
        this.sketch = sketch;
        this.chartDef.data.onmouseover = function (d) {
            var map = this.webgisChart.map, sketch = this.webgisChart.sketch, stat = d.x, val = d.value;
            if (map && sketch) {
                var sketchPoint = sketch.statPoint(stat);
                if (sketchPoint)
                    map.showTempMarker(sketchPoint);
            }
            return null;
        };
        this.chartDef.data.onmouseout = function (d) {
            var map = this.webgisChart.map;
            if (map)
                map.removeTempMarker();
        };
        this.sketch.events.on('onchanged', function () {
            this.destroy();
            $(this._target).empty();
            $("<button>Aktualisieren</button>").appendTo($(this._target))
                .click(function () {
                webgis.tools.onButtonClick(map, { type: 'servertoolcommand', command: 'refreshchart' });
            });
        }, this);
    };
};
