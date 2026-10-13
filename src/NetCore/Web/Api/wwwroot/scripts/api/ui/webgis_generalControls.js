// WebGIS general controls (jQuery plugins webgis_collapsable_paragraph, webgis_general_input)
// Collapsable paragraph and generic input control, used by the presentation TOC and the add services TOC

(function ($) {
    $.fn.webgis_collapsable_paragraph = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_collapsable_paragraph');
        }
    };
    var defaults = {
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            if ($this.length === 0)
                return initUI(this, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };
    var initUI = function (item, options) {
        webgis.delayed(function () {
            var $item = $(item);
            var height = $item.height();
            $item.addClass('webgis-collapsable-paragraph collapsed');
            if ($item.height() < height) {
                console.log(height, $item.height())
                $("<div></div>")
                    .addClass('switcher')
                    .appendTo($item)
                    .click(function (e) {
                        e.stopPropagation();
                        $(this).closest('.webgis-collapsable-paragraph').toggleClass('collapsed');
                    });
            } else {
                $item.css('height', 'auto');
            }
        }, 100);
    };
})(webgis.$ || jQuery);

(function ($) {
    $.fn.webgis_general_input = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_general_input');
        }
    };
    var defaults = {
        type: 'text',
        name: null,
        inputElement: 'input',
        label: null,
        placeholder: null,
        onInit:null
    };
    var methods = {
        init: function (options) {
            return this.each(function () {
                new initUI(this, $.extend({}, defaults, options));
            });
        },
        val: function (options) {
            return $(this).children("input[name='" + options.name + "']").val();
        },
        clear: function (options) {
            $(this).children("input[name='" + options.name + "']").val('');
        }
    };
    var initUI = function (parent, options) {
        var $parent = $(parent);

        if ($parent.children('.webgis-input').length > 0) {
            $("<br/>").appendTo($parent);
        }

        if (options.label) {
            var $label = $("<div>")
                .addClass("webgis-label")
                .text(options.label + ':')
                .appendTo($parent);

            if (options.description) {
                $("<span>")
                    .css('display', 'inline-block')
                    .addClass('webgis-api-icon webgis-api-icon-help')
                    .appendTo($label);

                var description = options.description, isMarkdown=false;
                if (description.indexOf('md:') === 0) {
                    description = description.substr(3);
                    isMarkdown = true;
                }

                $("<div>")
                    .addClass('webgis-label-description')
                    .html(webgis.encodeUntrustedHtml(description, isMarkdown))
                    .appendTo($parent);

                $label.click(function () {
                    $(this).next('.webgis-label-description').toggleClass('show');
                });
            }
            $("<br/>").appendTo($parent);
        }

        if (options.inputElement) {
            var $input = $("<" + options.inputElement + " name='" + options.name + "' type='" + options.type + "'>")
                .addClass('webgis-input')
                .appendTo($parent);

            if (options.placeholder) {
                $input.attr('placeholder', options.placeholder);
            }
        }

        if (options.onInit) {
            options.onInit($parent, $input);
        }
    };
})(webgis.$ || jQuery);
