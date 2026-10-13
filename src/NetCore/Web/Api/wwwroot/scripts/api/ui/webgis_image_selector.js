// WebGIS jQuery plugin webgis_image_selector (used by the image-selector builder)

(function ($) {
    $.fn.webgis_image_selector = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_image_selector');
        }
    };

    var defaults = {
        image_urls: [],
        image_labels: null,
        image_width: null,
        image_height: null,
        selected: null
    };

    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        }
    };

    var initUI = function (parent, options) {
        var $parent = $(parent)
            .addClass('webgis-image-selector');

        console.log(options);

        var $valueElement = $("<input type='hidden'>").appendTo($parent);
        $parent.data('webgis-value-element', $valueElement);

        for (var i in options.image_urls) {
            var imageUrl = options.image_urls[i];

            var $imgContainer = $("<div>")
                .data('imageUrl', imageUrl)
                .addClass('webgis-image-selector-image-container')
                .click(function (e) {
                    e.stopPropagation();

                    var $this = $(this);
                    if (options.multi_select) {
                        $this.toggleClass('selected');
                    } else {
                        var isSelected = $this.hasClass('selected');
                        $parent.children('.webgis-image-selector-image-container').removeClass('selected');
                        if (!isSelected) {
                            $this.addClass('selected');
                        }
                    }

                    _resetValue($parent, $valueElement);
                });

            var $img = $("<div>")
                .addClass('webgis-image-selector-image')
                .css({ backgroundImage: 'url("' + webgis.css.imgResource(imageUrl) + '")' })
                .appendTo($imgContainer);

            var $label = $("<div>")
                .addClass("webgis-image-selector-image-label");

            if (options.image_width) {
                $imgContainer.css('width', options.image_width);
                $label.css('width', options.image_width);
                $img.css('width', options.image_width);
            }

            if (options.image_height) {
                $imgContainer.css('height', options.image_labels ? options.image_height + 30 : options.image_height);
                $img.css('height', options.image_height);
            }

            if (options.image_labels && i < options.image_labels.length) {
                $label.text(options.image_labels[i])
                      .appendTo($imgContainer);
            }

            if (options.selected && options.selected === imageUrl) {
                $imgContainer.addClass('selected');
            }

            $imgContainer.appendTo($parent);
        }

        _resetValue($parent, $valueElement);
    };

    var _resetValue = function ($parent, $valueElement) {
        $valueElement.val('');
        var val = '';
        $parent.children('.webgis-image-selector-image-container.selected').each(function (i, e) {
            if (val) {
                val += ',';
            }

            val += $(e).data('imageUrl');
        });

        $valueElement.val(val);
    }
})(webgis.$ || jQuery);
