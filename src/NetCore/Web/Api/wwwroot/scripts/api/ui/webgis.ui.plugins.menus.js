// WebGIS UI Builder Menu Elements
// menu, menuitem (callbacks: webgis.ui.appendCallback in webgis.ui.plugins.general.js)

(function ($) {
    "use strict";

    // menu historically ignores id, css, style, visible, ... (".properties" = false)
    webgis.ui.builder["menu.tagname"] = "ul";
    webgis.ui.builder["menu.properties"] = false;
    webgis.ui.builder["menu.attributes"] = "style='list-style:none;padding:0px;' class='webgis-toolbox-tool-item-group-details'";
    webgis.ui.builder["menu"] = (map, $newElement, element) => {
        if (element.header) {
            $("<li><h2>" + element.header + "</h2></li>").appendTo($newElement);
        }
        if (element.collapsable) {
            $newElement.addClass('collapsable');
        }
    };

    // menuitems historically get no data-tool-id attribute (".toolid" = false), even if the UI is built for a tool
    webgis.ui.builder["menuitem.tagname"] = "li";
    webgis.ui.builder["menuitem.toolid"] = false;
    webgis.ui.builder["menuitem"] = (map, $newElement, element, options) => {
        var imgSrc = element.icon_large || element.icon ? webgis.css.imgResource(element.icon_large || element.icon) : webgis.css.imgResource('enter-26.png', 'ui');

        $newElement
            .html("<div>" + element.text + "</div>")
            .addClass('webgis-toolbox-tool-item')
            .css({
                padding: element.icon_large ? '36px 4px 4px 105px' : '12px 4px 12px 32px'
            });

        if (element.text2) {
            $("<div>")
                .addClass('text2')
                .text(element.text2)
                .appendTo($newElement);
        }

        if (element.subtext) {
            $("<div>")
                .addClass('subtext')
                .text(element.subtext)
                .appendTo($newElement);
        }

        if (imgSrc.indexOf('data:') == 0) {
            $newElement.css({
                position: 'relative'
            });
            $("<img src='" + imgSrc + "'>")
                .css({
                    position: 'absolute',
                    left: 3, top: 8, maxHeight: 22
                })
                .appendTo($newElement);
        } else {
            $newElement.css({
                backgroundImage: 'url(' + imgSrc + ')',
                backgroundPosition: '2px 10px',
                backgroundSize: 'auto 26px',
                backgroundRepeat: 'no-repeat',
            });
        }

        if (element.icon_large) {
            $newElement.css({
                backgroundSize: '90px',
                height: '110px',
                whiteSpace: 'normal'
            });
        }

        if (element.removable === true) {
            $("<div>")
                .addClass('remove remove-element')
                .text('✖')
                .appendTo($newElement);
        }

        if (element.show_checkbox) {
            appendQueryLayersCheckbox(map, $newElement, element);
        }

        webgis.ui.appendCallback($newElement, element, options);

        if (element.highlight_feature) {
            if (typeof element.highlight_feature === 'string') {
                element.highlight_feature = webgis.$.parseJSON(element.highlight_feature);
            }

            $newElement
                .data('highlight-feature', element.highlight_feature)
                .mouseenter(function (e) {
                    map.graphics.addPreviewFromJson($(this).data('highlight-feature').features[0].geometry);
                })
                .mouseleave(function (e) {
                    map.graphics.removePreview();
                })
                .click(function (e) {
                    map.graphics.removePreview();
                });
        }
    };

    // value = "serviceId:queryId" => checkbox toggling the visibility of the query's associated layers
    var appendQueryLayersCheckbox = function (map, $newElement, element) {
        var p = element.value.split(':');
        if (p.length !== 2)
            return;

        var service = map.getService(p[0]);
        if (!service)
            return;

        var query = service.getQuery(p[1]);
        if (!query || !query.associatedlayers || query.associatedlayers.length === 0)
            return;

        var layers = $.map(query.associatedlayers, function (l) { return l.id });
        var hasVisibleLayers = service.checkLayerVisibility(layers) !== 0;

        $("<div>")
            .data('service', service)
            .data('layers', layers)
            .addClass('webgis-checkbox')
            .css('backgroundImage', 'url(' + webgis.css.imgResource(hasVisibleLayers ? 'check1.png' : 'check0.png', 'toc') + ')')
            .appendTo($newElement
                .addClass('checkable')
                .css({
                    paddingLeft: '70px',
                    backgroundPosition: '36px center'
                })
            )
            .click(function (e) {
                e.stopPropagation();

                var service = $(this).data('service');
                var layers = $(this).data('layers');
                var hasVisibleLayers = service.checkLayerVisibility(layers) !== 0;

                service.setLayerVisibilityDelayed(layers, hasVisibleLayers ? false : true);
                $(this).css('backgroundImage', 'url(' + webgis.css.imgResource(hasVisibleLayers ? 'check0.png' : 'check1.png', 'toc') + ')');
            });
    };
})(webgis.$ || jQuery);
