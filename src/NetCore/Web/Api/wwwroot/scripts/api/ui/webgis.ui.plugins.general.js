// WebGIS Click Note Plugin
// A simple clickable note that can be dismissed or trigger a callback
// eg. webgis.ui.showLayerNotVisibleNotification(service, query, $target);
//     used in query result table, to notify the user that the corresponing layer is invisible   

webgis.ui.definePlugin('webgis_click_note', {
    defaults: {
        text: 'Note...',
        callback: null,
        styles: { backgroundColor: '#faa', cursor: 'pointer', padding: '4px 8px' }
    },

    init: function () {
        var o = this.options;
        // Events always use name namespace -> simpler deploy
        this.$el
            .css(o.styles)
            .css('position', 'relative')
            .text(o.text)
            .on('click.webgis_click_note', (e) => {
                e.stopPropagation();
                if (o.callback) o.callback();

                this.destroy();
                this.$el.remove();
            });
        $('<div>')
            .text('\u2715')
            .css({ position: 'absolute', top: 0, right: 0, padding: '4px 4px', cursor: 'pointer' })
            .appendTo(this.$el)
            .on('click.webgis_click_note', (e) => {
                e.stopPropagation();
                webgis.ui.destroyPluginsDeep(this.$el);
                this.$el.remove();
            });
    },
    destroy: function () {
        //console.log('Destroy click note'); 
        this.$el.off('.webgis_click_note');
    }
});

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
            $("<div>")
                .appendTo($target)
                .webgis_click_note({
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
}