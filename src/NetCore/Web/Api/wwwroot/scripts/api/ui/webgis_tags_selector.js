(function ($) {
    "use strict";
    $.fn.webgis_tags_selector = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$webgis_tags_selector');
        }
    };
    var defaults = {
        selector: null, // The selector for the tags input element
    };
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
    };
    let initUI = function (parent, options) {
        let $parent = $(parent);
        $parent
            .addClass('webgis-tags-selector')
            .data('options', options);

        let $target = $(options.selector);


        // 2. Collect all tags from child elements
        let tagsSet = new Set();
        $target.find('[tags]').each(function (i, e) {
            console.log('Processing element:', e); // Debugging output
            var tagsAttr = $(e).attr('tags');
            if (tagsAttr) {
                tagsAttr.split(',').map(function (t) { return t.trim(); }).forEach(function (t) {
                    if (t) tagsSet.add(t);
                });
            }
        });
        let tags = Array.from(tagsSet);
        //console.log('Tags found:', tags); // Debugging output

        // 3. create main button
        let $mainBtn = $("<div>")
            .addClass("webgis-tags-main-btn")
            .appendTo($parent);

        // 4. tag selection container (initially hidden)
        let $tagsDiv = $("<div>")
            .addClass("webgis-tags-popup")
            .css({ "display": "none", "position": "absolute", zIndex: 1000 })
            .appendTo($parent)
            .click(function () { e.stopPropagation() });

        // 5. Store active tags
        let activeTags = new Set();

        $("<button>")
            .addClass("has-tags-button")
            .text("#")
            .appendTo($mainBtn)
            .click(function (e) {
                stopPropagation(e);
                if (activeTags.size === 0) {
                    $tagsDiv.toggle();
                } else {
                    activeTags.clear();
                    updateTarget($target, Array.from(activeTags));
                }
            });

        // 6. Click on main button toggles tag selection
        $("<button>")
            .addClass("add-tags-button")
            .text("+")
            .appendTo($mainBtn)
            .click(function (e) {
                e.stopPropagation();
                $tagsDiv.toggle();
            });


        tags.forEach(function (tag) {
            $("<button type='button'>")
                .addClass("webgis-tag-btn")
                .data('tag', tag)
                .text("#" + tag)
                .appendTo($tagsDiv)
                .click(function () {
                    var tag = $(this).data('tag');
                    if (activeTags.has(tag)) {
                        activeTags.delete(tag);
                        $(this).removeClass('active');
                    } else {
                        activeTags.add(tag);
                        $(this).addClass('active');
                    }

                    if (activeTags.size > 0) {
                        $mainBtn.addClass('has-tags');
                        $parent.addClass('has-tags');
                    } else {
                        $mainBtn.removeClass('has-tags');
                        $parent.removeClass('has-tags');
                    }

                    updateTarget($target, Array.from(activeTags));
                });
        });
    };

    let updateTarget = function ($target, tags) {
        $target.find('[tags]').each(function (i, e) {
            const $el = $(e);
            const currentTags = $el.attr('tags')
                ? $el.attr('tags').split(',').map(function (t) { return t.trim(); })
                : [];

            let found = tags.length === 0;  // if tags is empty, show all elements
            for (const tag of tags) {
                if (currentTags.includes(tag)) {
                    found = true;
                    break;
                }
            }

            $el.css('display', found ? '' : 'none');
        });
    };
})(webgis.$ || jQuery);

(function ($) {
    "use strict";
    $.fn.webgis_tags_selector2 = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$webgis_tags_selector');
        }
    };
    var defaults = {
        selector: null, // The selector for the tags input element
    };
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
    };
    let initUI = function (parent, options) {
        let $parent = $(parent);
        $parent
            .addClass('webgis-tags-selector')
            .data('options', options);

        let $target = $(options.selector);


        // 2. Collect all tags from child elements
        let tagsSet = new Set();
        $target.find('[tags]').each(function (i, e) {
            console.log('Processing element:', e); // Debugging output
            var tagsAttr = $(e).attr('tags');
            if (tagsAttr) {
                tagsAttr.split(',').map(function (t) { return t.trim(); }).forEach(function (t) {
                    if (t) tagsSet.add(t);
                });
            }
        });
        let tags = Array.from(tagsSet);
        //console.log('Tags found:', tags); // Debugging output

        // 3. create main button
        let $mainBtn = $("<div>")
            .addClass("webgis-tags-main-btn")
            .appendTo($parent);

        // 4. tag selection container (initially hidden)
        let $tagsDiv = $("<div>")
            .addClass("webgis-tags-popup")
            .css({ "display": "none", "position": "absolute", zIndex: 1000 })
            .appendTo($parent)
            .click(function () { e.stopPropagation() });

        // 5. Store active tags
        let activeTags = new Set();

        $("<button>")
            .addClass("has-tags-button")
            .text("#")
            .appendTo($mainBtn)
            .click(function (e) {
                e.stopPropagation();
                if (activeTags.size === 0) {
                    $tagsDiv.toggle();
                } else {
                    activeTags.clear();
                    updateTarget($target, Array.from(activeTags));
                }
            });

        // 6. Click on main button toggles tag selection
        $("<button>")
            .addClass("add-tags-button")
            .text("+")
            .appendTo($mainBtn)
            .click(function (e) {
                e.stopPropagation();
                $tagsDiv.toggle();
            });


        tags.forEach(function (tag) {
            $("<button type='button'>")
                .addClass("webgis-tag-btn")
                .data('tag', tag)
                .text("#" + tag)
                .appendTo($tagsDiv)
                .click(function () {
                    var tag = $(this).data('tag');
                    if (activeTags.has(tag)) {
                        activeTags.delete(tag);
                        $(this).removeClass('active');
                    } else {
                        activeTags.add(tag);
                        $(this).addClass('active');
                    }

                    if (activeTags.size > 0) {
                        $mainBtn.addClass('has-tags');
                        $parent.addClass('has-tags');
                    } else {
                        $mainBtn.removeClass('has-tags');
                        $parent.removeClass('has-tags');
                    }

                    updateTarget($target, Array.from(activeTags));
                });
        });
    };

    let updateTarget = function ($target, tags) {
        $target.find('[tags]').each(function (i, e) {
            const $el = $(e);
            const currentTags = $el.attr('tags')
                ? $el.attr('tags').split(',').map(function (t) { return t.trim(); })
                : [];

            let found = tags.length === 0;  // if tags is empty, show all elements
            for (const tag of tags) {
                if (currentTags.includes(tag)) {
                    found = true;
                    break;
                }
            }

            $el.css('display', found ? '' : 'none');
        });
    };
})(webgis.$ || jQuery);