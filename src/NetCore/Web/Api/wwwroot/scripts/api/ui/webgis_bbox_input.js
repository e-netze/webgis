// WebGIS jQuery plugin webgis_bbox_input (bounding box input, used by the input-bbox builder, its setters and webgis.tools.js)

(function ($) {
    $.fn.webgis_bbox_input = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_bbox_input');
        }
    };
    var defaults = {
        readonly: false
    };
    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        },
        val: function (options) {
            if (typeof options === 'string') {
                var $this = $(this);

                var vals = options.split(',');
                if (vals.length == 4) {
                    $this.find('.webgis-bbox-value').val(options);
                    $this.find('.webgis-bbox-minx').val(parseFloat(vals[0]));
                    $this.find('.webgis-bbox-miny').val(parseFloat(vals[1]));
                    $this.find('.webgis-bbox-maxx').val(parseFloat(vals[2]));
                    $this.find('.webgis-bbox-maxy').val(parseFloat(vals[3]));
                }
            }

            return $(this).find('.webgis-bbox-value').val();
        }
    };
    var initUI = function (item, options) {
        var $item = $(item)
            .addClass('webgis-bbox-input-holder');
       

        var $row1 = $("<div>").addClass('row').appendTo($item);
        var $row2 = $("<div>").addClass('row').appendTo($item);
        var $row3 = $("<div>").addClass('row').appendTo($item);

        $("<input type='number' class='webgis-bbox-input webgis-bbox-maxy'>")
            .appendTo($row1);
        $("<input type='number' class='webgis-bbox-input webgis-bbox-minx'>")
            .appendTo($row2);
        $("<input type='number' class='webgis-bbox-input webgis-bbox-maxx'>")
            .appendTo($row2);
        $("<input type='number' class='webgis-bbox-input webgis-bbox-miny'>")
            .appendTo($row3);

        $valueElement = $("<input type='hidden' class='webgis-bbox-value' />")
            .appendTo($item);

        $item.data('webgis-value-element', $valueElement);

        $item.find('.webgis-bbox-input')
            .change(function () {
                var $holder = $(this).closest('.webgis-bbox-input-holder');
                var val = $holder.find('.webgis-bbox-minx').val() + "," +
                          $holder.find('.webgis-bbox-miny').val() + "," +
                          $holder.find('.webgis-bbox-maxx').val() + "," +
                          $holder.find('.webgis-bbox-maxy').val();

                $holder.find('.webgis-bbox-value').val(val);
            });

        if (options.readonly === true) {
            $item.find('.webgis-bbox-input').attr('readonly', 'readonly');
        }
    };
})(webgis.$ || jQuery);
