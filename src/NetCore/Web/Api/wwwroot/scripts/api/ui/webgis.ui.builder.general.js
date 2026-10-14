// WebGIS Simple UI Builder Elements
// like label, paragraph, etc
// (plugins: webgis.ui.plugins.general.js)

webgis.ui.builder["label"] = (map, $newElement, element) => {
    $newElement.addClass("webgis-label");

    var labels = element.label.split('|');

    const toTxtFunc = element.is_trusted === true
        ? webgis.asMarkdownOrRawHtml
        : webgis.asMarkdownOrText;

    if (labels.length == 1) {
        $newElement.html(toTxtFunc(element.label));
    } else {
        for (var label in labels) {
            $("<p>")
                .html(toTxtFunc(labels[label]))
                .appendTo($newElement);
        }
    }
}

webgis.ui.builder["paragraph"] = (map, $newElement, element) => {
    $newElement
        .addClass("webgis-paragraph")
        .html(webgis.asMarkdownOrText(element.text));
}

webgis.ui.builder["title.tagname"] = "h1";
webgis.ui.builder["title"] = (map, $newElement, element) => {
    $newElement.html(element.label);
}

webgis.ui.showLayerNotVisibleNotification = function (service, query, $target) {
    if (webgis.usability.showQueryLayerNotVisbleNotification === true && service && query) {
        let associatedLayers = query.associatedlayers || [{ id: query.layerid }];
        let foundVisible = false;

        for (var associatedLayer of associatedLayers) {
            let layerService = associatedLayer.serviceid ? service.map?.getService(associatedLayer.serviceid) : service;
            let layer = layerService?.getLayer(associatedLayer.id);
            if (layer?.visible === true) {
                foundVisible = true;
            }
        };

        if (foundVisible === false) {
            webgis.$("<div>")
                .appendTo($target)
                .webgis_clickNote({
                    text: webgis.l10n.get('query-layer-not-visible-notification'),
                    callback: function () {
                        // Try to find and show the first associated layer
                        for (var associatedLayer of associatedLayers) {
                            let layerService = associatedLayer.serviceid ? service.map?.getService(associatedLayers[0].serviceid) : service;
                            //console.log('Set layer visible', associatedLayer, layerService, service);
                            if (layerService) {
                                layerService.setLayerVisibility([associatedLayer.id], true);
                                break;
                            }
                        }
                    }
                });
        }
    }
};

webgis.ui.builder['card'] = (map, $newElement, element) => {
    $newElement.webgis_card({ title: element.title });
};

webgis.ui.createButtonGroup = function ($parent) {
    return webgis.$("<div>").appendTo($parent).webgis_buttonGroup();
};

// Binds element.callback (toolevent | servertoolcommand | servertoolcommand_ext) to the click event of a UI builder element.
// Used by menuitem; generic enough for other element types (type-specific behaviour is keyed on element.type).
webgis.ui.appendCallback = function ($newElem, element, options) {
    const $ = webgis.$ || jQuery;
    if (!element || !element.callback)
        return;
    if (options.map && element.callback.tool) {
        element.callback.tool = options.map.getTool(element.callback.tool.id) || element.callback.tool; // assign original tool, if possible
    }

    $newElem
        .data('_callback', element.callback)
        .data('_type', element.type)
        .data('_value', element.value)
        .data('_map', options.map)
        .data('_event', options.event)
        .click(function (e) {
            var $this = $(this);

            if ($this.data('_type') === 'menuitem') {
                e.stopPropagation();

                var $menu = $(this).closest('ul');
                if ($menu.hasClass('collapsable')) {
                    var collapsed = $menu.hasClass('collapsed');
                    $menu.toggleClass('collapsed');

                    if (collapsed === true) {
                        options.map.events.fire('ui-builder-collapsable-expanded', options.map, { $element: $menu });
                        return;
                    } else {
                        $menu.children().removeClass('selected-collapsed');
                        $(this).addClass('selected-collapsed');
                    }
                }
            }
            var cb = $this.data('_callback');
            var val = $this.data('_value');
            var custom = {};

            custom[$this.data('_type') + "-value"] = val;
            if ($(this).attr('data-item-command'))
                custom[$this.data('_type') + "-item-command"] = $(this).attr('data-item-command');

            if (cb.type === 'toolevent' && cb.tool) {
                webgis.tools.sendToolRequest($this.data('_map'), cb.tool, cb.type, $this.data('_event'), custom);
            }
            else if (cb.type === 'servertoolcommand') {
                webgis.tools.onButtonClick($this.data('_map'), { command: cb.command, type: 'servertoolcommand', id: cb.tool.id, map: $this.data('_map') }, this, null, custom);
            }
            else if (cb.type === 'servertoolcommand_ext') {
                webgis.tools.onButtonClick($this.data('_map'), { command: cb.command, type: 'servertoolcommand_ext', id: cb.tool.id, map: $this.data('_map') }, this, null, custom);
            }

            if ($this.data('_type') === 'menuitem') {
                var $menu = $(this).closest('ul');

                if ($menu.attr('element-target') === 'modaldialog') {
                    webgis.ui.closeModal();
                }
            }
        });
    $newElem.children('.remove-element')
        .click(function (e) {
            e.stopPropagation();

            var $parent = $(this).parent();
            $parent.attr('data-item-command', 'remove-element').trigger('click');
            $parent.remove();
        });
};
