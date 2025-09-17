// WebGIS Click Note Plugin
// A simple clickable note that can be dismissed or trigger a callback
// eg. webgis.ui.showLayerNotVisibleNotification(service, query, $target);
//     used in query result table, to notify the user that the corresponing layer is invisible   

webgis.ui.definePlugin('webgis_clickNote', {
    defaults: {
        text: 'Note...',
        callback: null,
        styles: { backgroundColor: '#faa', cursor: 'pointer', padding: '4px 8px' }
    },

    init: function () {
        let o = this.options;
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

webgis.ui.definePlugin('webgis_clickToggle', {
    defaults: {
        toggleStyle: [],
        toggleStyleValue: [],
        resetSiblings: false
    },

    init: function () {
        this.$el
            .addClass('webgis-click-toggle')
            .data('options', this.options)
            .on('click.webgis_click_toggle', (e) => {
                e.stopPropagation();
                const options = this.$el.data('options');
                if (options.toggleStyle && options.toggleStyleValue !== null) {
                    if (this.$el.hasClass('toggled')) {
                        this.$el.removeClass('toggled');
                        for (let i = 0; i < options.toggleStyle.length; i++) {
                            this.$el.css(options.toggleStyle[i], '');
                        }
                    } else {
                        if (options.resetSiblings === true) {
                            let $toggledSiblings = this.$el.siblings('.webgis-click-toggle.toggled').removeClass('toggled');
                            for (let i = 0; i < options.toggleStyle.length; i++) {
                                $toggledSiblings.css(options.toggleStyle[i], '');
                            }
                        }
                        for (let i = 0; i < Math.min(options.toggleStyle.length, options.toggleStyleValue.length); i++) {
                            this.$el.css(options.toggleStyle[i], options.toggleStyleValue[i]);
                        }
                        this.$el.addClass('toggled');
                    }
                }
            });
    },
    destroy: function () {
        //console.log('Destroy click toggle'); 
        this.$el.off('.webgis_click_toggle');
    }
});
