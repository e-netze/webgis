(function ($) {
    "use strict";

    $.fn.webgis_contentsearch = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_contentsearch');
        }
    };
    var defaults = {
        placeholder: 'Inhalte filtern...',
        container_selectors: ["li", "ul"],
        onChanged: null,
        onMatch: null,
        onReset: null,
        displayMachted: 'block',
        searchTag: null,
        resetButton: true,
        addInputElementClass: ''
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        reset: function (options) {
            //console.log('reset search...');

            var $area = $(this)
                .children('input')
                .val('')
                .closest('.webgis-content-search-area')
                .removeClass('active');
            
            $area.find('[data-orig-display]')
                .each(function (i, e) {
                    var $container = $(e);
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

            $(this).find('.reset-button').css('display', '');

            if (options && options.onReset) {
                options.onReset($(this).closest('.webgis-content-search-area'));
            }
        },
        trigger: function (options) {
            $(this)
                .children('input')
                .trigger('keyup');
        },
        set: function (options) {
            var $area = $(this)
                .children('input')
                .val(options.value)
                .focus()
                .trigger('keyup');
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem).addClass('webgis-content-search-holder');

        $elem.parent().addClass('webgis-content-search-area');

        function tog(v) { return v ? 'addClass' : 'removeClass'; }
        var $input = $("<input type='text' placeholder='" + options.placeholder + "'>")
            .addClass('clearable x')
            .addClass(options.addInputElementClass)
            .appendTo($elem)
            .on('mousemove', function (e) {
                $(this)[tog(this.offsetWidth - 18 < e.clientX - this.getBoundingClientRect().left)]('onX');
            }).on('touchstart click', function (ev) {
                if ($(this).hasClass('onX')) {
                    ev.preventDefault();
                    $(this)
                        .removeClass('onX')
                        .closest('.webgis-content-search-holder')
                        .webgis_contentsearch('reset', options);
                }
            })
            .on('keydown', function (e) {
                if (!$(this).val()) {  // first letter
                    var keyCode = e.keyCode;
                    if (keyCode === 8 || keyCode === 46 || keyCode === 13)  // Backspace, Del, Enter -> do nothing
                        return;
                    //console.log('reset orig display');
                    $(this).closest('.webgis-content-search-area')
                        .find('[data-orig-display]')
                        .attr('data-orig-display', '');
                }
            })
            .on('keyup', function () {
                var $this = $(this), tag = $this.val().toLowerCase();
                if (!tag) {
                    $this.closest('.webgis-content-search-holder').webgis_contentsearch('reset', options);
                } else {
                    var tagWords = tag.split(' ');

                    var $area = $this
                        .closest('.webgis-content-search-area')
                        .addClass('active');

                    $area.find('.webgis-search-content.active')
                        .each(function (i, e) {
                            var $e = $(e);

                            var contains = true;
                            for (var i in tagWords) {
                                if (tagWords[i]) {
                                    contains &= $e   // all words must fit
                                        .text()
                                        .toLowerCase()
                                        .indexOf(tagWords[i]) >= 0;
                                }
                            }
                            //if (contains) {
                            //    console.log($e.text());
                            //}

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

                    $this
                        .closest('.webgis-content-search-holder')
                        .find('.reset-button')
                        .css('display', 'block');
                }

                if (options.onChanged) {
                    options.onChanged($(this).closest('.webgis-content-search-area'), $(this).val());
                }
            });

        var $resetButton = $("<button>")
            .addClass('webgis-button uibutton uibutton-cancel reset-button')
            .text('Filter entfernen')
            .appendTo($elem)
            .click(function () {
                $(this).closest('.webgis-content-search-holder').webgis_contentsearch('reset');
            });

        if (options.searchTag) {
            $input.val(options.searchTag).trigger('keyup');
        }
    };

    var setContainerVisibility = function ($e, container_selector, contains, tag, options) {
        var $container = $e.closest(container_selector);
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
            setContainerVisibility($container.parent(), container_selector, contains, tag);

            if (options && options.onMatch)
                options.onMatch(this, $container, tag);

            //$container.parent('display', 'block');
            //while ($container.parent().closest('li').length > 0) {
            //    $container = $container.parent().closest('li').css('display', 'block');
            //    $container.parent('display', 'block');
            //}
        }
    };
})(webgis.$ || jQuery);