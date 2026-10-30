// WebGIS UI Builder List Elements
// checkable-div, option-list, option-list-item, optionscontainer, click-toggle, drop-list
// (plugins: webgis.ui.plugins.lists.js)

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

    // option groups: image buttons with the same data-group (UIImageButton.group) are moved into a flyout
    // of a group button (webgis.usability.groupToolOptions). The group button shows the icon of the last
    // selected option of its group and is selected, if an option of its group is selected.
    webgis.ui.optionsContainerGroups = {
        // all options of a container (including the options in group flyouts), in document order
        items: function ($container) {
            return $container.children().not('.webgis-ui-option-group, .webgis-ui-option-group-flyout')
                .add($container.children('.webgis-ui-option-group-flyout').children());
        },
        build: function ($container) {
            if (webgis.usability.groupToolOptions === false) {
                return;
            }

            var groups = {};
            $container.children('[data-group]').each(function (i, item) {
                var group = $(item).attr('data-group');
                (groups[group] = groups[group] || []).push(item);
            });

            for (var group in groups) {
                var items = groups[group];
                if (items.length < 2) {
                    continue;
                }

                var $first = $(items[0]),
                    groupText = $first.data('group-text') || group;
                var $groupButton = $("<div>")
                    .addClass('webgis-ui-imagebutton webgis-ui-option-group')
                    .attr('data-option-group', group)
                    .attr('title', groupText)
                    .css('background-image', $first.css('background-image'))
                    .insertBefore($first);
                $("<div>").text(groupText).addClass('webgis-ui-imagebutton-text').appendTo($groupButton);

                var $flyout = $("<div>")
                    .addClass('webgis-ui-option-group-flyout')
                    .attr('data-option-group', group)
                    .css('display', 'none')
                    .appendTo($container);
                $(items).appendTo($flyout);

                $groupButton.click(function (e) {
                    e.stopPropagation();
                    var $button = $(this),
                        $groupFlyout = $button.parent().children(".webgis-ui-option-group-flyout[data-option-group='" + $button.attr('data-option-group') + "']"),
                        isOpen = $groupFlyout.is(':visible');

                    webgis.ui.optionsContainerGroups.closeFlyouts();
                    if (!isOpen) {
                        $groupFlyout
                            .css('top', ($button.position().top + $button.outerHeight(true)) + 'px')
                            .css('display', 'block');
                        $button.addClass('webgis-ui-option-group-open');
                    }
                });
            }

            if ($container.children('.webgis-ui-option-group').length > 0) {
                $container.addClass('webgis-ui-optionscontainer-grouped');
            }
        },
        // updates the selection state and icon of the group buttons (call after the selected option changed)
        sync: function ($container) {
            $container.children('.webgis-ui-option-group').each(function (i, groupButton) {
                var $groupButton = $(groupButton),
                    $selected = $container
                        .children(".webgis-ui-option-group-flyout[data-option-group='" + $groupButton.attr('data-option-group') + "']")
                        .children('.webgis-ui-option-selected');

                if ($selected.length > 0) {
                    $groupButton
                        .addClass('webgis-ui-option-selected')
                        .css('background-image', $selected.first().css('background-image'));
                } else {
                    $groupButton.removeClass('webgis-ui-option-selected');
                }
            });
        },
        closeFlyouts: function () {
            $('.webgis-ui-option-group-flyout').css('display', 'none');
            $('.webgis-ui-option-group-open').removeClass('webgis-ui-option-group-open');
        }
    };

    $(document)
        .on('mousedown.webgis-option-group touchstart.webgis-option-group', function (e) {
            if ($(e.target).closest('.webgis-ui-option-group, .webgis-ui-option-group-flyout').length === 0) {
                webgis.ui.optionsContainerGroups.closeFlyouts();
            }
        })
        .on('keydown.webgis-option-group', function (e) {
            if (e.key === 'Escape') {
                webgis.ui.optionsContainerGroups.closeFlyouts();
            }
        });

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
            webgis.ui.optionsContainerGroups.items($(this)).each(function (i, e) {
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
        webgis.ui.optionsContainerGroups.build($newElement);
        var $items = webgis.ui.optionsContainerGroups.items($newElement);

        if (!newElement.value && !$newElement.hasClass('allow-null-values')) {
            var defaultValue = null;
            for (var d in webgis.usability.optionContainerDefault) {
                var ocd = webgis.usability.optionContainerDefault[d];

                if (ocd.id === element.id) {
                    defaultValue = ocd.value;
                }
            }
            if (defaultValue && $items.filter("[data-value='" + defaultValue + "']").length === 1) {
                $items.filter("[data-value='" + defaultValue + "']").trigger('click');
            } else {
                $items.first().trigger('click');
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
