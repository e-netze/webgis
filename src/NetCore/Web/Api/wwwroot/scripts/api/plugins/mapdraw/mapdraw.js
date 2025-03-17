(function (webgis) {
    "use strict"

    webgis.addPlugin(new function () {

        this.onInit = function () {

        };

        this.onMapCreated = function (map, container) {

        }
    });
})(webgis);


(function ($) {
    $.fn.webgis_mapdraw = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_mapdraw');
        }
    };

    var defaults = {
        map_options: null,
        on_save: null,
        on_init: null,
        quick_tools: 'webgis.tools.navigation.currentPos',
        map_search_categories: 'Adresse',
        geometrytype: null,
        showTabs: false,
        sketch_max_vertices: null
    };

    var ua = window.navigator.userAgent;

    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);

            return this.each(function () {
                var eventHandlers = {};
                webgis.implementEventController(eventHandlers);
                $(this).data('eventHandlers', eventHandlers);
                new initUI(this, options);
            });
        },
        getMap: function () {
            return getMap($(this).find("#map"))
        }
    };

    var initUI = function (elem, options) {
        var $elem = $(elem);
        $elem.addClass('webgis-plugin-mapdraw-container webgis-container-styles');

        var $content = $("<div class='content'></div>");
        $content.appendTo($elem);

        createMap(options, $content);
        $(window).resize(function () {
            if ($("#map").length > 0)
                getMap($("#map")).invalidateSize();  // Karte an aktuelle DIV Größe anpassen (für jeden Browser)
        });

        var map = getMap($content);

        // Map tools
        var tooltype = 'sketch0d';
        switch (options.geometrytype) {
            case 'line':
                tooltype = 'sketch1d';
                break;
            case 'polygon':
                tooltype = 'sketch2d';
                break;
        }

        var tool = {
            name: 'pos',
            type: 'clienttool',
            tooltype: tooltype,
            id: 'collector-tool-' + tooltype,
        };
        map.setActiveTool(tool);
        map.sketch.events.on('onchanged', function (channel, sender) {
            var vertexCount = map.sketch.vertexCount();
            if (options.sketch_max_vertices == null || (options.sketch_max_vertices != null && vertexCount <= options.sketch_max_vertices))
                $content.closest('.webgis-plugin-mapdraw-container').data('eventHandlers').events.fire('changeSketch', { success: true, result: sender });
            else
                map.sketch.removeVertex(vertexCount - 1);

            if (options.sketch_max_vertices != null && vertexCount == options.sketch_max_vertices)
                map.sketch.close();
        });

        // Wenn alles fertig ist:
        if (options.on_init) {
            options.on_init({
                map: map,
                webgisContainer: $content.closest('.webgis-plugin-mapdraw-container').find('.webgis-container')
            });
        }
    }

     var createMap = function (options, $target) {
        $target.addClass('webgis-container');
        var $map = $("<div id='map' style='position:absolute;left:0px;top:0px;right:0px;bottom:0px'></div>").appendTo($target);
        $("<div class='webgis-tool-button-bar shadow' data-tools='" + options.quick_tools + "' style='position:absolute;left:9px;top:109px'></div>").appendTo($target);

        var map = webgis.createMap($map, options.map_options);
        $target.closest('.webgis-plugin-mapdraw-container').data('map', map);


         // UI-Elemente erzeugen
        $webgisContainer = $target.closest('.webgis-plugin-mapdraw-container').find('.webgis-container')
        $("<div id='map-container-topbar' style='position:absolute;right:0px;top:0px;z-index:9999;text-align:right;'></div>" +
            "<div style='z-index:10;position:absolute;right:0px;width:320px;bottom:0px;height:24px;background:#aaa;z-index:9999'>" +
                "<div id='map-container-hourglass'></div>" +
            "</div>" +
          "<div class='webgis-tool-button-bar shadow' data-tools='webgis.tools.navigation.currentPos' style='position:absolute;left:9px;top:109px'></div>")
        .appendTo($webgisContainer);

        map.ui.createHourglass('#map-container-hourglass');
        map.ui.createTopbar('#map-container-topbar', {
            quick_search_service: options.map_search_service,
            quick_search_categorie: options.map_search_categorie,
            quick_search: true,
            detail_search: false,
            app_menu: false
        });

        if (options.showTabs) {
            map.ui.createTabs('.webgis-container', {
                left: null, right: 0, bottom: 24, top: null, width: 320,
                add_presentations: true,
                add_settings: false,
                add_tools: false,
                add_queryResults: true,
                options_presentations: {
                    gdi_button: false
                },
            });
        }
    }

    var getMap = function ($elem) {
        return $elem.closest('.webgis-plugin-mapdraw-container').data('map');
    }

})(webgis.$ || jQuery);