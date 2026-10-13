// WebGIS UI Plugin webgis_size_input
// Width/height input, used by the input-size builder (webgis.ui.builder.inputs.js),
// its setters (webgis_uibuilder.js) and webgis.tools.js (downloadmapimage)

webgis.ui.definePlugin('webgis_size_input', {
    defaults: {
        readonly: false
    },
    init: function () {
        const $ = this.$;
        const $item = this.$el.addClass('webgis-size-input-holder');

        $("<input type='number' class='webgis-size-input webgis-size-x'>")
            .appendTo($item);
        $("<div>x</div>").appendTo($item);
        $("<input type='number' class='webgis-size-input webgis-size-y'>")
            .appendTo($item);

        $("<input type='hidden' class='webgis-size-value' />")
            .appendTo($item);

        $item.find('.webgis-size-input')
            .on('change.webgis_size_input', function () {
                const $holder = $(this).closest('.webgis-size-input-holder');
                const val = $holder.find('.webgis-size-x').val() + "," +
                            $holder.find('.webgis-size-y').val();

                $holder.find('.webgis-size-value').val(val);
            });

        if (this.options.readonly === true) {
            $item.find('.webgis-size-input').attr('readonly', 'readonly');
        }
    },
    destroy: function () {
        this.$el.find('.webgis-size-input').off('.webgis_size_input');
    },
    methods: {
        // val() => "x,y"; val("x,y") sets the inputs
        val: function (val) {
            if (typeof val === 'string') {
                const vals = val.split(',');
                if (vals.length == 2) {
                    this.$el.find('.webgis-size-value').val(val);
                    this.$el.find('.webgis-size-x').val(parseFloat(vals[0]));
                    this.$el.find('.webgis-size-y').val(parseFloat(vals[1]));
                }
            }

            return this.$el.find('.webgis-size-value').val();
        }
    }
});
