(function (webgis) {
    "use strict"

    webgis.addPlugin(new function () {

        this.onInit = function () {
            //alert('init');
        };

        this.onMapCreated = function (map, container) {
            //alert('map created');

            var left = 2;
            for (var d in map.dynamicContent) {
                var dynamicContent = map.dynamicContent[d];

                var $button = $("<div class='webgis-button' style='z-index:9999;position:absolute;bottom:2px;left:" + left + "px'>" + dynamicContent.name + "</div>").appendTo($(container))
                    .click(function () {
                        map.loadDynamicContent($(this).data('content'));
                    }).data('content', dynamicContent);

                left += $button.width() + 18;
            }
        }
    });

})(webgis);