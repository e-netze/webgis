// WebGIS UI Plugins (lists)
// webgis_clickToggle
// Element builders: webgis.ui.builder.lists.js

// WebGIS Click Toggle Plugin
// A UI plugin that toggles CSS styles and classes on an element when clicked.
// Supports resetting sibling toggles and custom style changes for interactive controls.
webgis.ui.definePlugin('webgis_clickToggle', {
    defaults: {
        toggleStyle: [],
        toggleStyleValue: [],
        resetSiblings: false
    },

    init: function () {
        const $ = this.$;

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
