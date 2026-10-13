// WebGIS UI Plugin webgis_multi_select
// Turns a select into a multi value selection (used by the print label combos, webgis.ui.builder.combos.js):
// every selected option is added as removable item, the joined values are stored in a hidden input,
// which takes over the id and the tool-parameter classes of the select.

webgis.ui.definePlugin('webgis_multi_select', {
    defaults: {
        separator: ',',
        map: null
    },
    init: function () {
        const $ = this.$;
        const self = this;
        const options = this.options;
        const $select = this.$el;

        const $multiSelect = $("<div>")
            .insertBefore($select);
        const $input = $("<input type='hidden'>")
            .attr('id', $select.attr('id'))
            .appendTo($multiSelect);

        $select.appendTo($multiSelect);
        $select.removeAttr('id');

        $("<div>") // items container
            .addClass('webgis-multiselect-container')
            .appendTo($multiSelect);

        this._$multiSelect = $multiSelect;

        if ($select.hasClass('webgis-tool-parameter')) {
            $select.removeClass('webgis-tool-parameter');
            $input.addClass('webgis-tool-parameter');
        }

        if ($select.hasClass('webgis-tool-parameter-persistent')) {
            $select.removeClass('webgis-tool-parameter-persistent');
            $input.addClass('webgis-tool-parameter-persistent');

            if (options.map) {
                const val = options.map.getPersistentToolParameter(options.map.getActiveTool(), $input.attr('id'));
                if (val) {
                    const vals = val.split(options.separator);
                    for (let i = 0; i < vals.length; i++) {
                        this.addItem(vals[i]);
                    }
                }
                this.resetValue();
            }
        }

        $select.on('change.webgis_multi_select', function () {
            self.addItem($select.val());

            self.resetValue();
            $select.val('');
        });
    },
    destroy: function () {
        this.$el.off('.webgis_multi_select');
    },
    methods: {
        addItem: function (val) {
            if (!val) return;

            const $ = this.$;
            const self = this;
            const $itemsContainer = this._$multiSelect.children('.webgis-multiselect-container');
            let exists = false;
            $itemsContainer.children().each(function (i, e) {
                if ($(e).data('value') === val) {
                    exists = true;
                }
            });
            if (exists) return;

            const $item = $("<div>")
                .addClass('webgis-multiselect-continer-item')
                .data('value', val)
                .text(val)
                .appendTo($itemsContainer);
            $("<div>")
                .addClass('remove')
                .text('X')
                .appendTo($item)
                .on('click.webgis_multi_select', function () {
                    $(this).closest('.webgis-multiselect-continer-item').remove();
                    self.resetValue();
                });
        },
        resetValue: function () {
            const $ = this.$;
            const separator = this.options.separator;
            let val = '';

            this._$multiSelect.children('.webgis-multiselect-container').children().each(function (i, e) {
                if (val)
                    val += separator;
                val += $(e).data('value');
            });

            this._$multiSelect.children('input').val(val);
        }
    }
});
