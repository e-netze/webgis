// WebGIS UI Plugin webgis_bbox_input
// Bounding box input (minx, miny, maxx, maxy), used by the input-bbox builder (webgis.ui.builder.inputs.js),
// its setters (webgis_uibuilder.js) and webgis.tools.js (downloadmapimage)

webgis.ui.definePlugin('webgis_bbox_input', {
    defaults: {
        readonly: false
    },
    init: function () {
        const $ = this.$;
        const $item = this.$el
            .addClass('webgis-bbox-input-holder');

        const $row1 = $("<div>").addClass('row').appendTo($item);
        const $row2 = $("<div>").addClass('row').appendTo($item);
        const $row3 = $("<div>").addClass('row').appendTo($item);

        $("<input type='number' class='webgis-bbox-input webgis-bbox-maxy'>")
            .appendTo($row1);
        $("<input type='number' class='webgis-bbox-input webgis-bbox-minx'>")
            .appendTo($row2);
        $("<input type='number' class='webgis-bbox-input webgis-bbox-maxx'>")
            .appendTo($row2);
        $("<input type='number' class='webgis-bbox-input webgis-bbox-miny'>")
            .appendTo($row3);

        const $valueElement = $("<input type='hidden' class='webgis-bbox-value' />")
            .appendTo($item);

        $item.data('webgis-value-element', $valueElement);

        $item.find('.webgis-bbox-input')
            .on('change.webgis_bbox_input', function () {
                const $holder = $(this).closest('.webgis-bbox-input-holder');
                const val = $holder.find('.webgis-bbox-minx').val() + "," +
                            $holder.find('.webgis-bbox-miny').val() + "," +
                            $holder.find('.webgis-bbox-maxx').val() + "," +
                            $holder.find('.webgis-bbox-maxy').val();

                $holder.find('.webgis-bbox-value').val(val);
            });

        if (this.options.readonly === true) {
            $item.find('.webgis-bbox-input').attr('readonly', 'readonly');
        }
    },
    destroy: function () {
        this.$el.find('.webgis-bbox-input').off('.webgis_bbox_input');
    },
    methods: {
        // val() => "minx,miny,maxx,maxy"; val("minx,miny,maxx,maxy") sets the inputs
        val: function (val) {
            if (typeof val === 'string') {
                const vals = val.split(',');
                if (vals.length == 4) {
                    this.$el.find('.webgis-bbox-value').val(val);
                    this.$el.find('.webgis-bbox-minx').val(parseFloat(vals[0]));
                    this.$el.find('.webgis-bbox-miny').val(parseFloat(vals[1]));
                    this.$el.find('.webgis-bbox-maxx').val(parseFloat(vals[2]));
                    this.$el.find('.webgis-bbox-maxy').val(parseFloat(vals[3]));
                }
            }

            return this.$el.find('.webgis-bbox-value').val();
        }
    }
});
