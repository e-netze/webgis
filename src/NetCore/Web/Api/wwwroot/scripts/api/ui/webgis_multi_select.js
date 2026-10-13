// WebGIS Multi Select Plugin
// Turns a select into a multi value selection (used by the print label combos):
// every selected option is added as removable item, the joined values are stored in a hidden input,
// which takes over the id and the tool-parameter classes of the select.
(function ($) {
    $.fn.webgis_multi_select = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_multi_select');
        }
    };

    var defaults = { separator: ',', map: null };

    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        }
    };

    var initUI = function (select, options) {
        let $select = $(select);

        let $multiSelect = $("<div>")
            .insertBefore($select);
        let $input = $("<input type='hidden'>")
            .attr('id', $select.attr('id'))
            .appendTo($multiSelect);

        $select.appendTo($multiSelect);
        $select.removeAttr('id');

        $("<div>") // $itemsContainer
            .addClass('webgis-multiselect-container')
            .appendTo($multiSelect);
        
        if ($select.hasClass('webgis-tool-parameter')) {
            $select.removeClass('webgis-tool-parameter');
            $input.addClass('webgis-tool-parameter');
        }

        if ($select.hasClass('webgis-tool-parameter-persistent')) {
            $select.removeClass('webgis-tool-parameter-persistent');
            $input.addClass('webgis-tool-parameter-persistent');

            if (options.map) {
                var val = options.map.getPersistentToolParameter(options.map.getActiveTool(), $input.attr('id'));
                if (val) {
                    var vals = val.split(options.separator);
                    for (let i = 0; i < vals.length; i++) {
                        addItem(vals[i], $multiSelect, options)
                    }
                }
                resetValue($multiSelect, options);
            }
        }
            
        $select.change(function () {
            addItem($select.val(), $multiSelect, options)

            resetValue($multiSelect, options);
            $select.val('');
        });
    };

    var addItem = function (val, $parent, options) {
        if (!val) return;

        $itemsContainer = $parent.children('.webgis-multiselect-container');
        let exists = false;
        $itemsContainer.children().each(function (i, e) {
            if ($(e).data('value') === val) {
                exists = true;
            }
        });
        if (exists) return;

        let $item = $("<div>")
            .addClass('webgis-multiselect-continer-item')
            .data('value', val)
            .text(val)
            .appendTo($itemsContainer);
        $("<div>")
            .addClass('remove')
            .text('X')
            .appendTo($item)
            .click(function () {
                $(this).closest('.webgis-multiselect-continer-item').remove();
                resetValue($parent, options);
            });
    }

    var resetValue = function ($parent, options) {
        var val = '';

        $itemsContainer = $parent.children('.webgis-multiselect-container');
        $itemsContainer.children().each(function (i, e) {
            if (val)
                val += options.separator;
            val += $(e).data('value');
        });

        $parent.children('input').val(val);
    };
})(webgis.$ || jQuery);
