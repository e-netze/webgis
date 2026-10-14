// WebGIS UI Plugins (contentsearch)
// webgis_contentsearch: filter input that shows/hides the '.webgis-search-content' items of the parent area

webgis.ui.definePlugin('webgis_contentsearch', {
    defaults: {
        placeholder: null,  // default: webgis.l10n.get('filter-contents')
        container_selectors: ["li", "ul"],
        onChanged: null,
        onMatch: null,
        onReset: null,
        displayMachted: 'block',
        searchTag: null,
        resetButton: true,
        addInputElementClass: ''
    },
    init: function () {
        const $ = this.$, options = this.options, self = this;
        const $elem = this.$el.addClass('webgis-content-search-holder');
        const setContainerVisibility = this.constructor.setContainerVisibility;

        $elem.parent().addClass('webgis-content-search-area');

        function tog(v) { return v ? 'addClass' : 'removeClass'; }
        const $input = $("<input type='text' placeholder='" + (options.placeholder || webgis.l10n.get('filter-contents')) + "'>")
            .addClass('clearable x')
            .addClass(options.addInputElementClass)
            .appendTo($elem)
            .on('mousemove.webgis_contentsearch', function (e) {
                $(this)[tog(this.offsetWidth - 18 < e.clientX - this.getBoundingClientRect().left)]('onX');
            }).on('touchstart.webgis_contentsearch click.webgis_contentsearch', function (ev) {
                if ($(this).hasClass('onX')) {
                    ev.preventDefault();
                    $(this).removeClass('onX');
                    self.reset(options);
                }
            })
            .on('keydown.webgis_contentsearch', function (e) {
                if (!$(this).val()) {  // first letter
                    const keyCode = e.keyCode;
                    if (keyCode === 8 || keyCode === 46 || keyCode === 13)  // Backspace, Del, Enter -> do nothing
                        return;
                    $(this).closest('.webgis-content-search-area')
                        .find('[data-orig-display]')
                        .attr('data-orig-display', '');
                }
            })
            .on('keyup.webgis_contentsearch', function () {
                const $this = $(this), tag = $this.val().toLowerCase();
                if (!tag) {
                    self.reset(options);
                } else {
                    const tagWords = tag.split(' ');

                    const $area = $this
                        .closest('.webgis-content-search-area')
                        .addClass('active');

                    $area.find('.webgis-search-content.active')
                        .each(function (i, e) {
                            const $e = $(e);

                            let contains = true;
                            for (let w in tagWords) {
                                if (tagWords[w]) {
                                    contains &= $e   // all words must fit
                                        .text()
                                        .toLowerCase()
                                        .indexOf(tagWords[w]) >= 0;
                                }
                            }

                            $.each(options.container_selectors, function (index, container_selector) {
                                setContainerVisibility($e, container_selector, contains, tag, options);
                            });
                        });

                    $area.find('.webgis-search-content-alt-text')
                        .each(function (i, e) {
                            $(e)
                                .attr('data-search-tag', tag)
                                .text($(e).attr('data-content-search-alt-text'));
                        });

                    $elem.find('.reset-button').css('display', 'block');
                }

                if (options.onChanged) {
                    options.onChanged($(this).closest('.webgis-content-search-area'), $(this).val());
                }
            });

        $("<button>")
            .addClass('webgis-button uibutton uibutton-cancel reset-button')
            .text(webgis.l10n.get('remove-filters'))
            .appendTo($elem)
            .on('click.webgis_contentsearch', function () {
                self.reset();
            });

        if (options.searchTag) {
            $input.val(options.searchTag).trigger('keyup');
        }
    },
    destroy: function () {
        this.$el.find('*').off('.webgis_contentsearch');
    },
    methods: {
        // options (optional): { onReset } - the internal handlers pass the plugin options
        reset: function (options) {
            const $ = this.$, $elem = this.$el;

            const $area = $elem
                .children('input')
                .val('')
                .closest('.webgis-content-search-area')
                .removeClass('active');

            $area.find('[data-orig-display]')
                .each(function (i, e) {
                    const $container = $(e);
                    if ($container.attr('data-orig-display')) {
                        $container
                            .removeClass('webgis-content-search-match')
                            .css('display', $container.attr('data-orig-display'))
                            .attr('data-search-tag', '')
                            .attr('data-orig-display', '');
                    }
                });
            $area.find('.webgis-search-content-alt-text')
                .each(function (i, e) {
                    $(e)
                        .attr('data-search-tag', '')
                        .text($(e).attr('data-content-search-text'));
                });

            $elem.find('.reset-button').css('display', '');

            if (options && options.onReset) {
                options.onReset($elem.closest('.webgis-content-search-area'));
            }
        },
        trigger: function () {
            this.$el
                .children('input')
                .trigger('keyup');
        },
        set: function (options) {
            this.$el
                .children('input')
                .val(options.value)
                .focus()
                .trigger('keyup');
        }
    },
    staticMethods: {
        setContainerVisibility: function setContainerVisibility($e, container_selector, contains, tag, options) {
            const $container = $e.closest(container_selector);
            if ($container.length === 0)
                return;

            if (!$container.attr('data-orig-display')) {
                $container.attr('data-orig-display', $container.css('display'));
            }

            if (!tag) {
                $container.css('display', $container.attr('data-orig-display')).attr('data-search-tag', '');
            }
            else if (!contains) {
                if ($container.attr('data-search-tag') !== tag) {
                    $container
                        .removeClass('webgis-content-search-match')
                        .css('display', 'none');
                }
            } else {
                $container
                    .addClass('webgis-content-search-match')
                    .css('display', $container.hasClass('webgis-search-content') ? options.displayMachted : 'block')
                    .attr('data-search-tag', tag);
                // recursion without options (legacy behavior): parents are displayed as 'block' and don't call onMatch
                setContainerVisibility($container.parent(), container_selector, contains, tag);

                if (options && options.onMatch)
                    options.onMatch(this, $container, tag);
            }
        }
    }
});
