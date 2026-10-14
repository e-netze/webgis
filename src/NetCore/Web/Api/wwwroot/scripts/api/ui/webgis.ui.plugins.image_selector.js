// WebGIS UI Plugin webgis_image_selector
// Selectable image tiles (single or multi select), used by the image-selector builder (webgis.ui.builder.misc.js)

webgis.ui.definePlugin('webgis_image_selector', {
    defaults: {
        image_urls: [],
        image_labels: null,
        image_width: null,
        image_height: null,
        selected: null
    },
    init: function () {
        const $ = this.$;
        const self = this;
        const options = this.options;
        const $parent = this.$el
            .addClass('webgis-image-selector');

        webgis.log(options);

        this._$valueElement = $("<input type='hidden'>").appendTo($parent);
        $parent.data('webgis-value-element', this._$valueElement);

        for (var i in options.image_urls) {
            var imageUrl = options.image_urls[i];

            var $imgContainer = $("<div>")
                .data('imageUrl', imageUrl)
                .addClass('webgis-image-selector-image-container')
                .on('click.webgis_image_selector', function (e) {
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

                    self.resetValue();
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

        this.resetValue();
    },
    destroy: function () {
        this.$el.children('.webgis-image-selector-image-container').off('.webgis_image_selector');
    },
    methods: {
        resetValue: function () {
            const $ = this.$;
            const $valueElement = this._$valueElement;

            $valueElement.val('');
            let val = '';
            this.$el.children('.webgis-image-selector-image-container.selected').each(function (i, e) {
                if (val) {
                    val += ',';
                }

                val += $(e).data('imageUrl');
            });

            $valueElement.val(val);
        }
    }
});
