webgis.map._viewLense = function (map) {

    var $ = webgis.$;
    var _map = map;

    var _currentMapViewLense = null;
    var _currentMapViewLenseOptions = null;
    var _currentMapViewLenseTool = 'pan';
    var _currentMapViewLenseRotation = 0;
    var _currentMapViewLenseInitRotation = 0;
    var _currentMapViewLenseExtent = null;
    var _currentMapViewLenseScale = 0;
    var _currentMapViewLenseSize = [0, 0];

    this.show = function (lense, options) {
        if (lense && lense.width < 0 && lense.height < 0) // let current lense
            return;
        this.hide(true);
        if (!lense) {
            _currentMapViewLenseOptions = null;
            return;
        }
        //console.log(lense);

        options = options || _currentMapViewLenseOptions || {};

        _currentMapViewLense = lense;
        _currentMapViewLenseOptions = options;

        var R = 6378137.0; // WGS84
        var w = lense.width /** 180.0 / (R * Math.PI)*/, h = lense.height /** 180.0 / (R * Math.PI)*/;
        _currentMapViewLenseSize = [w, h];
        var extent = _map.getProjectedExtent();

        if (options.zoom === true) {
            _currentMapViewLenseOptions.zoom = false;

            var cX = (extent[2] + extent[0]) * .5,
                cY = (extent[3] + extent[1]) * .5;

            var rotation = _currentMapViewLenseRotation;

            var rotatedBbox = webgis.calc.rotateBbox([cX - w * .5, cY - h * .5, cX + w * .5, cY + h * .5], rotation);

            var minx = rotatedBbox[0][0], miny = rotatedBbox[0][1];
            var maxx = rotatedBbox[2][0], maxy = rotatedBbox[2][1];

            for (var i = 0; i < rotatedBbox.length; i++) {
                minx = Math.min(minx, rotatedBbox[i][0]);
                miny = Math.min(miny, rotatedBbox[i][1]);
                maxx = Math.max(maxx, rotatedBbox[i][0]);
                maxy = Math.max(maxy, rotatedBbox[i][1]);
            }

            _map.zoomTo(webgis.calc.resizeBounds([minx, miny, maxx, maxy], 1.08), true);

            if (options.lenseScale) {
                _currentMapViewLenseScale = options.lenseScale;
                webgis.delayed(function (viewLense) {
                    //console.log('scale', map.scale());
                    _map.setDpi(96.0 * (options.lenseScale / _map.scale()));
                }, 500, this);
            } else {
                _currentMapViewLenseScale = 0;
                _map.resetDpi();
            }

            //
            // Damit bei drehen alles sichtbar ist, nicht auf die BBOX zoomen, sondern
            // so, dass die maximale Ausdehungen in alles Richtungen ins bild passt
            //
            // _map.zoomTo([cX - w * .5, cY - h * .5, cX + w * .5, cY + h * .5], true);

            //var maxWH = Math.max(w, h);
            //_map.zoomTo([cX - maxWH * .5, cY - maxWH * .5, cX + maxWH * .5, cY + maxWH * .5], true);

            webgis.delayed(function (lense) {
                lense.updateOptions();
                lense.show(_currentMapViewLense);
            }, 500, this);

            return;
        }

        $(_map.elem).find('.webgis-mapviewlense').css("transform", "rotate(0deg)");
        var W = extent[2] - extent[0], H = extent[3] - extent[1];
        var elemW = $(_map.elem).width(), elemH = $(_map.elem).height();
        //console.log([elemW, elemH]);
        var iW = elemW * (w / W), iH = elemH * (h / H);
        //console.log([iW, iH]);
        var bH = (elemH - iH) / 2;

        var ll = _map.frameworkElement.project(L.latLng(_currentMapViewLenseExtent[1], _currentMapViewLenseExtent[0]), _map.frameworkElement.getZoom());
        var ur = _map.frameworkElement.project(L.latLng(_currentMapViewLenseExtent[3], _currentMapViewLenseExtent[2]), _map.frameworkElement.getZoom());
        var pixelOrigin = _map.frameworkElement.getPixelOrigin();
        ll.x -= pixelOrigin.x;
        ll.y -= pixelOrigin.y;
        ur.x -= pixelOrigin.x;
        ur.y -= pixelOrigin.y;

        var mainpane = $(_map.frameworkElement.getContainer()).clone();
        var mapPane = mainpane.find(".leaflet-map-pane")[0];
        var mapTransform = mapPane.style.transform.split(",");
        var translateX = parseFloat(mapTransform[0].split("(")[1].replace("px", ""));
        var translateY = parseFloat(mapTransform[1].replace("px", ""));

        //console.log('point-coordinates', ll, ur, L.latLng(_currentMapViewLenseExtent[1], _currentMapViewLenseExtent[0]), L.latLng(_currentMapViewLenseExtent[3], _currentMapViewLenseExtent[2]), _map.frameworkElement.getZoom());

        //var left = (elemW - iW) / 2, right = (elemW - iW) / 2;
        //var top = (elemH - iH) / 2, buttom = (elemH - iH) / 2;
        var left = ll.x + translateX, right = (elemW - ur.x) - translateX;
        var top = ur.y + translateY, bottom = (elemH - ll.y) - translateY;
        //console.log('translate', translateX, translateY);
        //console.log('lrtp', left, right, top, bottom);

        var $lense = $("<div class='webgis-mapviewlense'></div>").appendTo($(_map.elem));
        var $top = $("<div class='border-b top' style='left:" + left + "px;right:" + right + "px;top:0px;height:" + (top - 30) + "px'></div>").appendTo($lense);


        var $tools = $("<div class='tools' style='left:" + (left - 4) + "px;right:" + (right - 4) + "px;top:" + (top - 30) + "px;height:30px'></div>").appendTo($lense);
        $("<div class='text'>Druckbereich</div>").appendTo($tools);

        var me = this;
        $('<div>')
            .addClass('tool')
            .text('⮙')
            .appendTo($tools)
            .click(function () {
                _currentMapViewLenseRotation = _currentMapViewLenseInitRotation = 0;
                me.refresh(true);
            });
        $('<div>')
            .addClass('tool')
            .attr('data-tool', 'rotate')
            .text('↻')
            .appendTo($tools)
            .click(function () {
                $(this).parent().children('.selected').removeClass('selected');
                $(this).addClass('selected');
                _currentMapViewLenseTool = 'rotate';
                _map.frameworkElement.dragging.disable();
                _refreshInfo();
            });
        $('<div>')
            .addClass('tool')
            .attr('data-tool', 'pan')
            .text('👆')
            .appendTo($tools)

            .click(function () {
                $(this).parent().children('.selected').removeClass('selected');
                $(this).addClass('selected');
                _currentMapViewLenseTool = 'pan';
                _map.frameworkElement.dragging.enable();
                _refreshInfo();
            });

        $tools.children(".tool[data-tool='" + _currentMapViewLenseTool + "']").trigger('click');

        $('<div>')
            .addClass('tool')
            .text('❌')
            .css({ position: 'absolute', right: '0px' })
            .appendTo($tools)
            .click(function (e) {
                e.stopPropagation();
                _map.setActiveTool(null);
            });

        if (options.scalecontrolid) {

            $("<div>")
                .addClass('tool')
                .text('➕')   //➕
                .appendTo($tools)
                .click(function () {
                    var scaleCtrl = $('#' + _currentMapViewLenseOptions.scalecontrolid);
                    var val = parseInt(scaleCtrl.val()), len = scaleCtrl.children('option').length;
                    scaleCtrl.children('option').each(function (i, o) {
                        if (parseInt(o.value) === val && i < len - 1) {
                            scaleCtrl.val(scaleCtrl.children('option')[i + 1].value);
                            scaleCtrl.trigger('change');
                        }
                    });
                    //if (scaleCtrl.children('option:selected').next().length > 0) {
                    //    scaleCtrl.children('option:selected').removeAttr('selected').next().attr('selected', 'selected');
                    //    scaleCtrl.trigger('change');
                    //}
                });
            $("<div>")
                .addClass('tool')
                .text('➖')  //➖
                .appendTo($tools).click(function () {
                    var scaleCtrl = $('#' + _currentMapViewLenseOptions.scalecontrolid);
                    var val = parseInt(scaleCtrl.val()), len = scaleCtrl.children('option').length;
                    scaleCtrl.children('option').each(function (i, o) {
                        if (parseInt(o.value) === val && i > 0) {
                            scaleCtrl.val(scaleCtrl.children('option')[i - 1].value);
                            scaleCtrl.trigger('change');
                        }
                    });
                    //if (scaleCtrl.children('option:selected').prev().length > 0) {
                    //    scaleCtrl.children('option:selected').removeAttr('selected').prev().attr('selected', 'selected');
                    //    scaleCtrl.trigger('change');
                    //}
                });
        }

        $("<div class='border-t' style='left:" + left + "px;right:" + right + "px;bottom:0px;height:" + bottom + "px'></div>").appendTo($lense);
        $("<div class='border-r' style='left:0px;top:" + top + "px;bottom:" + bottom + "px;width:" + left + "px'></div>").appendTo($lense);
        $("<div class='border-l' style='right:0px;top:" + top + "px;bottom:" + bottom + "px;width:" + right + "px'></div>").appendTo($lense);
        $("<div style='left:0px;width:" + left + "px;top:0px;height:" + top + "px'></div>").appendTo($lense);
        $("<div style='width:" + right + "px;right:0px;top:0px;height:" + top + "px'></div>").appendTo($lense);
        $("<div style='left:0px;width:" + left + "px;bottom:0px;height:" + bottom + "px'></div>").appendTo($lense);
        $("<div style='width:" + right + "px;right:0px;bottom:0px;height:" + bottom + "px'></div>").appendTo($lense);

        $(_map.elem).find('.webgis-mapviewlense').css("transform", "rotate(" + _currentMapViewLenseRotation + "deg)");

        $('.webgis-map-overlay-ui-element').css('display', 'none');

        var $lenseInfo = $("<div>")
            .addClass('webgis-mapviewlense-info')
            .appendTo(_map.elem);
        $("<div>")
            .addClass('webgis-mapviewlense-info-tool')
            .appendTo($lenseInfo);
        $("<div>")
            .addClass('webgis-mapviewlense-info-tool-description')
            .appendTo($lenseInfo);

        _refreshInfo();
    };

    this.refresh = function (zoom) {
        if (_currentMapViewLense) {
            _currentMapViewLenseOptions = _currentMapViewLenseOptions || {};
            _currentMapViewLenseOptions.zoom = zoom;
            this.show(_currentMapViewLense, _currentMapViewLenseOptions);
        }
    };

    var _refreshInfo = function () {
        var rotation = Math.round(_currentMapViewLenseRotation * 10) / 10;

        $(_map.elem).find('.webgis-mapviewlense-info-tool')
            .text('Tool: ' + webgis.i18n.get(_currentMapViewLenseTool) + ' (Rotation: ' + (rotation < 0 ? rotation + 360 : rotation) + '°)');

        $(_map.elem).find('.webgis-mapviewlense-info-tool-description')
            .text(webgis.i18n.get(_currentMapViewLenseTool + '-tool-description'));
    };

    this.updateOptions = function () {
        var extent = _map.getProjectedExtent();

        var cX = (extent[2] + extent[0]) * .5,
            cY = (extent[3] + extent[1]) * .5;

        var rotation = _currentMapViewLenseRotation;
        var w = _currentMapViewLenseSize[0], h = _currentMapViewLenseSize[1];

        var sw = _map.crs.frameworkElement.projection.unproject(L.point(cX - w * .5, cY - h * .5));
        var ne = _map.crs.frameworkElement.projection.unproject(L.point(cX + w * .5, cY + h * .5));
        _currentMapViewLenseExtent = [sw.lng, sw.lat, ne.lng, ne.lat];
        //console.log(_currentMapViewLenseExtent, _currentMapViewLenseSize, rotation);
    };

    this.hide = function (suppressResetDpi) {
        _currentMapViewLense = null;
        _currentMapViewLenseTool = 'pan';
        $(_map.elem).find('.webgis-mapviewlense').remove();
        $(_map.elem).find('.webgis-mapviewlense-info').remove();

        $('.webgis-map-overlay-ui-element').css('display', '');

        if (suppressResetDpi !== true) {
            if (_map.resetDpi());
        }
    };

    this.isActive = function () { return _currentMapViewLense !== null; };

    this.currentTool = function () { return _currentMapViewLenseTool; };

    this.currentExtent = function () { return _currentMapViewLenseExtent; };

    this.currentRotation = function () { return _currentMapViewLenseRotation; };

    this.currentScale = function () { return _currentMapViewLenseScale; };

    this._performMapLenseToolInit = function (offset) {
        if (_currentMapViewLenseTool === 'rotate') {
            _currentMapViewLenseInitRotation = _currentMapViewLenseRotation;
        }
    };

    this._performMapLenseToolRelease = function () {
        if (_currentMapViewLenseTool === 'rotate') {
            this.refresh(true);
        }
    };

    this._performMapLenseTool = function (event, prevOffset, mouseDownOffset) {

        if (_currentMapViewLenseTool === 'rotate') {

            var elemOffset = $(_map.elem).offset();

            var dx0 = (mouseDownOffset.x - elemOffset.left) - $(_map.elem).width() / 2.0;
            var dy0 = (mouseDownOffset.y - elemOffset.top) - $(_map.elem).height() / 2.0;

            var dx = (event.x - elemOffset.left) - $(_map.elem).width() / 2.0;
            var dy = (event.y - elemOffset.top) - $(_map.elem).height() / 2.0;

            var alpha0 = Math.atan2(-dy0, dx0) * 180.0 / Math.PI;
            var alpha = Math.atan2(-dy, dx) * 180.0 / Math.PI;

            _currentMapViewLenseRotation = _currentMapViewLenseInitRotation + (alpha0 - alpha);

            if (event.shiftKey === true) {
                _currentMapViewLenseRotation = Math.round(_currentMapViewLenseRotation / 5) * 5;
            }
            else if (event.ctrlKey === true) {
                _currentMapViewLenseRotation = Math.round(_currentMapViewLenseRotation / 10) * 10;
            }

            $(_map.elem).find('.webgis-mapviewlense').css("transform", "rotate(" + _currentMapViewLenseRotation + "deg)");

            _refreshInfo();
        }
    };
};