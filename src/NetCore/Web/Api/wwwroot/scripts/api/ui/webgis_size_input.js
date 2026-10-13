// WebGIS jQuery plugin webgis_size_input (width/height input, used by the input-size builder, its setters and webgis.tools.js)

(function ($) {
    $.fn.webgis_size_input = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_size_input');
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
                if (vals.length == 2) {
                    $this.find('.webgis-size-value').val(options);
                    $this.find('.webgis-size-x').val(parseFloat(vals[0]));
                    $this.find('.webgis-size-y').val(parseFloat(vals[1]));
                }
            }

            return $(this).find('.webgis-size-value').val();
        }
    };
    var initUI = function (item, options) {
        var $item = $(item).addClass('webgis-size-input-holder');

        $("<input type='number' class='webgis-size-input webgis-size-x'>")
            .appendTo($item);
        $("<div>x</div>").appendTo($item);
        $("<input type='number' class='webgis-size-input webgis-size-y'>")
            .appendTo($item);

        $("<input type='hidden' class='webgis-size-value' />")
            .appendTo($item);

        $item.find('.webgis-size-input')
            .change(function () {
                var $holder = $(this).closest('.webgis-size-input-holder');
                var val = $holder.find('.webgis-size-x').val() + "," +
                          $holder.find('.webgis-size-y').val();

                $holder.find('.webgis-size-value').val(val);
            });

        if (options.readonly === true) {
            $item.find('.webgis-size-input').attr('readonly', 'readonly');
        }
    };
})(webgis.$ || jQuery);
