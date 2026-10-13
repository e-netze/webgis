// WebGIS UI Builder List Elements
// checkable-div, option-list, option-list-item, optionscontainer, click-toggle, drop-list

// WebGIS Click Toggle Plugin
// A UI plugin that toggles CSS styles and classes on an element when clicked.
// Supports resetting sibling toggles and custom style changes for interactive controls.
webgis.ui.definePlugin('webgis_clickToggle', {
    defaults: {
        toggleStyle: [],
        toggleStyleValue: [],
        resetSiblings: false
    },

    init: function () {
        const $ = this.$;

        this.$el
            .addClass('webgis-click-toggle')
            .data('options', this.options)
            .on('click.webgis_click_toggle', (e) => {
                e.stopPropagation();
                const options = this.$el.data('options');
                if (options.toggleStyle && options.toggleStyleValue !== null) {
                    if (this.$el.hasClass('toggled')) {
                        this.$el.removeClass('toggled');
                        for (let i = 0; i < options.toggleStyle.length; i++) {
                            this.$el.css(options.toggleStyle[i], '');
                        }
                    } else {
                        if (options.resetSiblings === true) {
                            let $toggledSiblings = this.$el.siblings('.webgis-click-toggle.toggled').removeClass('toggled');
                            for (let i = 0; i < options.toggleStyle.length; i++) {
                                $toggledSiblings.css(options.toggleStyle[i], '');
                            }
                        }
                        for (let i = 0; i < Math.min(options.toggleStyle.length, options.toggleStyleValue.length); i++) {
                            this.$el.css(options.toggleStyle[i], options.toggleStyleValue[i]);
                        }
                        this.$el.addClass('toggled');
                    }
                }
            });
    },
    destroy: function () {
        //console.log('Destroy click toggle'); 
        this.$el.off('.webgis_click_toggle');
    }
});

(function ($) {
    "use strict";

    // checkable-div: div with a checkbox and a hidden status input ("checked" | ""), which gets the same properties (id, ...) as the div
    webgis.ui.builder["checkable-div"] = (map, $newElement, element, options) => {
        $newElement
            .addClass('webgis-checkable-div')
            .click(function (e) {
                e.stopPropagation();

                var $this = $(this);
                $this.addClass('checked');
                $this
                    .children('.webgis-checkable-div-status')
                    .val('checked');
            });

        $("<div>")
            .addClass('checkbox')
            .appendTo($newElement)
            .click(function (e) {
                e.stopPropagation();

                var $parent = $(this).parent();
                $parent.toggleClass('checked');
                $parent
                    .children('.webgis-checkable-div-status')
                    .val($parent.hasClass('checked') ? 'checked' : '');
            });
        var $valueElement = $("<input class='webgis-checkable-div-status' type='hidden' " + webgis.ui.elementProperties(element, options) + "/>")
            .val($newElement.hasClass('checked') ? 'checked' : '')
            .appendTo($newElement);

        $newElement.data('webgis-value-element', $valueElement);
    };

    // option-list: the properties (id, ...) belong to the hidden value input, not to the holder (".properties" = false).
    // The returned <ul> is the element the option-list-items are inserted into.
    webgis.ui.builder["option-list.properties"] = false;
    webgis.ui.builder["option-list"] = (map, $newElement, element, options) => {
        $newElement.addClass('webgis-ui-option-list-holder');

        $("<input type='hidden'" + webgis.ui.elementProperties(element, options) + " />")
            .addClass('webgis-ui-option-list-value')
            .appendTo($newElement);

        return $("<ul>")
            .addClass('webgis-ui-option-list')
            .appendTo($newElement);
    };

    // option-list-item historically ignores id, css, style, visible, ... (".properties" = false)
    webgis.ui.builder["option-list-item.tagname"] = "li";
    webgis.ui.builder["option-list-item.properties"] = false;
    webgis.ui.builder["option-list-item"] = (map, $newElement, element) => {
        $newElement
            .addClass('webgis-ui-option-list-item')
            .data('value', element.value);

        $newElement.click(function (e) {
            e.stopPropagation();

            $(this)
                .closest('.webgis-ui-option-list-holder')
                .find('.webgis-ui-option-list-value')
                .val($(this).data('value'));

            $(this).parent().children('.selected').removeClass('selected');
            $(this).addClass('selected');
        });

        // the first item is selected automatically
        if ($newElement.parent().children('.webgis-ui-option-list-item').length === 1)
            $newElement.trigger('click');
    };

    // optionscontainer: the options (e.g. imagebuttons) set container.value and call container.onChange when clicked
    webgis.ui.builder["optionscontainer"] = (map, $newElement, element) => {
        $newElement.addClass('webgis-ui-optionscontainer');
        if (element.allow_null_values === true) {
            $newElement.addClass('allow-null-values');
        }

        var newElement = $newElement.get(0);
        newElement.value = element.value;

        if (!element.onchange && $newElement.hasClass('webgis-tool-parameter-persistent')) {
            newElement.map = map;
            newElement.onChange = function () {
                this.map.setPersistentToolParameter(this.map.getActiveTool(), this.id, this.value);
            };
            newElement.value = (map.getPersistentToolParameter(map.getActiveTool(), newElement.id)) || newElement.value;
        }

        // clicks the option matching the current container value (the argument is ignored)
        newElement.setValue = function (val) {
            $(this).children().each(function (i, e) {
                var $e = $(e);

                if ($e.data('_value') == newElement.value ||
                    ( // if(value is an Object compare the id of this object... (ie. Redining Symbol Selector...)
                    $e.data('_value') && $e.data('_value').id && newElement.value && newElement.value.id && $e.data('_value').id === newElement.value.id)) {
                    $e.trigger('click');
                    return false;
                }
            });
        };
    };
    // after the options are inserted: select the current value, a configured default (webgis.usability.optionContainerDefault) or the first option
    webgis.ui.builder["optionscontainer.afterinsert"] = (map, $newElement, element) => {
        var newElement = $newElement.get(0);
        if (!newElement.value && !$newElement.hasClass('allow-null-values')) {
            var defaultValue = null;
            for (var d in webgis.usability.optionContainerDefault) {
                var ocd = webgis.usability.optionContainerDefault[d];

                if (ocd.id === element.id) {
                    defaultValue = ocd.value;
                }
            }
            if (defaultValue && $newElement.children("[data-value='" + defaultValue + "']").length === 1) {
                $newElement.children("[data-value='" + defaultValue + "']").trigger('click');
            } else {
                $newElement.children(':first-child').trigger('click');
            }
        }
        else {
            newElement.setValue(newElement.value);
        }
    };

    webgis.ui.builder["click-toggle"] = (map, $newElement, element) => {
        $newElement.webgis_clickToggle({ toggleStyle: element.togglestyle.split(','), toggleStyleValue: element.togglestylevalue.split(','), resetSiblings: element.resetsiblings });
    };

    // drop-list: drop target (id = <id>_droplist) + hidden value input (id = <id>, comma separated values) as sibling
    webgis.ui.builder["drop-list"] = (map, $newElement, element, options) => {
        $newElement.attr('id', element.id + "_droplist").attr('data-id', element.id).data('map', options.map).addClass('webgis-ui-droplist');
        $("<input type='hidden' " + webgis.ui.elementProperties(element, options) + "/>").appendTo($newElement.parent()).val(element.value);
        $newElement
            .on('dragover', function (e) {
                e.preventDefault();
            })
            .on('drop', function (e) {
                e.preventDefault();
                var map = $(this).data('map');
                var values = e.originalEvent.dataTransfer.getData('text').split(';');
                for (var v in values) {
                    var value = values[v];
                    if ($(this).children("div[data-dropvalue='" + value + "']").length > 0)
                        continue;
                    var $item = $("<div><span>" + value + "</span></div>").addClass('webgis-ui-droplist-item').attr('data-dropvalue', value).appendTo($(this));
                    map.events.fire('ondrop-add', map, { id: $(this).attr('data-id'), value: value });
                    $("<div>X</div>").addClass('webgis-ui-droplist-item-x').appendTo($item)
                        .click(function () {
                            var $list = $(this).closest('.webgis-ui-droplist');
                            var map = $list.data('map');
                            map.events.fire('ondrop-remove', map, { id: $(this).closest('.webgis-ui-droplist').attr('data-id'), value: $(this).closest('.webgis-ui-droplist-item').attr('data-dropvalue') });
                            $(this).closest('.webgis-ui-droplist-item').remove();
                            $list.data('calcVal')($list);
                        });
                }
                $(this).data('calcVal')($(this));
            })
            .data('calcVal', function ($elem) {
                var $target = $elem.parent().children("input[id='" + $elem.attr('data-id') + "']");
                var val = '';
                $elem.children('.webgis-ui-droplist-item').each(function () {
                    if (val !== '')
                        val += ',';
                    val += $(this).attr('data-dropvalue');
                });
                $target.val(val);
            });
    };
})(webgis.$ || jQuery);
