// WebGIS UI Plugin webgis_collapsable_paragraph
// Collapses a long text block and adds a switcher to expand it (used by the presentation TOC, service descriptions/copyright)

webgis.ui.definePlugin('webgis_collapsable_paragraph', {
    defaults: {
    },
    init: function () {
        const $ = this.$;
        const $item = this.$el;

        // delayed: the height is only known after the element is rendered
        webgis.delayed(function () {
            var height = $item.height();
            $item.addClass('webgis-collapsable-paragraph collapsed');
            if ($item.height() < height) {
                console.log(height, $item.height())
                $("<div></div>")
                    .addClass('switcher')
                    .appendTo($item)
                    .on('click.webgis_collapsable_paragraph', function (e) {
                        e.stopPropagation();
                        $(this).closest('.webgis-collapsable-paragraph').toggleClass('collapsed');
                    });
            } else {
                $item.css('height', 'auto');
            }
        }, 100);
    }
});
