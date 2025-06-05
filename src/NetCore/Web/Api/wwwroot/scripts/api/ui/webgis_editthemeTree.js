(function ($) {
    "use strict";
    $.fn.webgis_editthemeTree = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$webgis_editthemeTree');
        }
    };
    var defaults = {
        map: null,
        customitems: [],
        onchange: null,
        dbrights: null
    };
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        },
        value: function (options) {
            var $valueElement = $(this).data('webgis-value-element');
            if ($valueElement) {
                return $valueElement.val();
            }

            return null;
        },
        editThemeSelected: function (options) {
            var $valueElement = $(this).data('webgis-value-element');
            if ($valueElement) {
                return $valueElement.val() && $valueElement.val().indexOf(',') > 0;
            }

            return false;
        }
    };

    var initUI = function (parent, options) {
        var $parent = $(parent);

        $parent.addClass('webgis-edittheme-tree').data('options', options);
        if (options.onchange) {
            $parent.change(options.onchange);
        }

        var $valueElement = $("<input>")
            .attr('type', 'hidden')
            .appendTo($parent);

        $parent.data('webgis-value-element', $valueElement);

        var $serviceCombo = $("<select>")
            .addClass('webgis-edittheme-tree-service-combo webgis-input')
            .attr("id", "webgis-edittheme-tree-service-combo")
            .appendTo($parent)
            .change(function (e) {
                e.stopPropagation();

                var $this = $(this),
                    $themesList = $this.closest('.webgis-edittheme-tree').children('.webgis-edittheme-tree-theme-list'),
                    $parent = $this.closest('.webgis-edittheme-tree');

                var serviceId = $this.val();

                let tagsSet = new Set();
                $themesList.children('li').removeClass('webgis-tag-item').each(function (i, item) {
                    const $item = $(item);
                    const display = $item.attr('data-serviceId') === serviceId ? 'block' : 'none'

                    $item
                        .removeClass('selected')
                        .css('display', display);

                    if (display != 'none') {
                        var tagsAttr = $item.addClass('webgis-tag-item').attr('tags');
                        if (tagsAttr) {
                            tagsAttr.split(',').map(function (t) { return t.trim(); }).forEach(function (t) {
                                if (t) tagsSet.add(t);
                            });
                        }
                    }
                });

                setValue($parent, $this.val());
                $this.webgis_tagsCombo('set_tags', { tags: Array.from(tagsSet) });
                $this.webgis_tagsCombo('restore');
            })
            .webgis_tagsCombo({
                itemContainer: $parent,
                itemSelector: '.webgis-tag-item'
            });

        const $themeList = $("<ul>")
            .addClass('webgis-edittheme-tree-theme-list')
            .appendTo($parent);

        if (options.map !== null) {
            for (var serviceId in options.map.services) {
                var service = options.map.services[serviceId];
                addService({}, service, parent);
            }

            options.map.events.on('onaddservice', addService, parent);
            options.map.events.on('onremoveservice', removeService, parent);

            var initalized = false;
            if ($parent.hasClass('webgis-tool-parameter-persistent')) {
                var persistentValue = options.map.getPersistentToolParameter(options.map.getActiveTool(), $parent.attr('id'));

                if (persistentValue) {
                    var persistentValueParts = persistentValue.split(',');

                    $serviceCombo.val(persistentValueParts[0]).trigger('change');
                    $themeList.children("[data-edittheme='" + persistentValue + "']").trigger('click');
                    initalized = true;
                }
            }

            if (!initalized) {
                $serviceCombo.val($serviceCombo.children().first().attr('value')).trigger('change');
            }
        }
    };

    var addService = function (e, service, parent) {
        var $parent = $(parent || this),
            options = $parent
                .data('options');

        if (service == null || service.serviceInfo == null || service.editthemes == null || service.editthemes.length === 0) {
            return;
        }

        var $serviceCombo = $parent.children('.webgis-edittheme-tree-service-combo');
        var $themeList = $parent.children('.webgis-edittheme-tree-theme-list');

        //var isFirstElement = $serviceCombo.children().length === 0;

        $("<option>")
            .attr('value', service.serviceInfo.id)
            .text(service.serviceInfo.name)
            .appendTo($serviceCombo);

        for (var i = 0, to = service.editthemes.length; i < to; i++) {
            var edittheme = service.editthemes[i];

            if (options.dbrights && edittheme.dbrights) {
                if (edittheme.dbrights.indexOf(options.dbrights) < 0) {
                    continue;
                }
            }

            var layerIds = [edittheme.layerid]; // ToDo: use associated layers!?
            var val = service.id + ',' + edittheme.layerid + ',' + edittheme.themeid;

            var $item = $("<li>")
                .addClass('webgis-edittheme-tree-theme-list-item')
                .css({
                    display: 'none',
                    backgroundImage: 'url(' + webgis.css.legendImage(service.id, edittheme.layerid) + ')'
                })
                .attr('data-edittheme', val)
                .attr('data-serviceId', service.serviceInfo.id)
                .attr('tags', edittheme.tags)
                .text(edittheme.name)
                .appendTo($themeList)
                .click(function (e) {
                    e.stopPropagation();

                    var $this = $(this), val = '', $parent = $this.closest('.webgis-edittheme-tree');

                    if ($this.hasClass('selected')) {
                        $this.removeClass('selected');

                        val = $this.attr('data-edittheme').split(',')[0];
                    } else {
                        $this.parent().children('.selected').removeClass('selected');
                        $this.addClass('selected');

                        val = $this.attr('data-edittheme');
                    }

                    setValue($parent, val);
                });

            $("<div>")
                .data('layerIds', layerIds)
                .addClass('checkbox')
                .addClass(service.checkLayerVisibility(layerIds) !== 0 ? 'checked' : '')
                .appendTo($item)
                .click(function (e) {
                    e.stopPropagation();

                    var $this = $(this),
                        $parent = $this.closest('.webgis-edittheme-tree'),
                        options = $parent.data('options'),
                        serviceId = $this.parent().attr('data-serviceId');

                    if (!options || !options.map)
                        return;

                    var service = options.map.getService(serviceId);
                    if (!service)
                        return;

                    $this.toggleClass('checked');
                    service.setLayerVisibility($this.data('layerIds'), $this.hasClass('checked'), true);
                });

            if (webgis.initialParameters.editthemeid === edittheme.themeid && !webgis.initialParameters.editthemeid_initialized) {
                webgis.initialParameters.editthemeid_initialized = true;

                $serviceCombo.val(service.serviceInfo.id).trigger('changed');
                $item.trigger('click');

                const $toolbox = $(".webgis-ui-optionscontainer[data-tool-id='webgis.tools.editing.edit']");
                if (webgis.initialParameters.original && webgis.initialParameters.original.tooloption) {
                    $toolbox.children(`[data-value='${webgis.initialParameters.original.tooloption}']`).trigger('click');
                }
            }
        }

        //if (isFirstElement) {
        //    $serviceCombo.val(service.serviceInfo.id).trigger('change');
        //}
    };

    var removeService = function (e, service, parent) {
        var $parent = $(parent || this),
            options = $parent.data('options');

        if (service == null || service.serviceInfo == null || service.editthemes == null || service.editthemes.length === 0) {
            return;
        }

        var $serviceCombo = $parent.children('.webgis-edittheme-tree-service-combo');
        var $themeList = $parent.children('.webgis-edittheme-tree-theme-list');

        $serviceCombo.children("option[value='" + service.serviceInfo.id + "']").remove();

        $themeList.children().each(function (i, item) {
            var $item = $(item);

            if ($item.attr('data-serviceId') === service.serviceInfo.id) {
                $item.remove();
            }
        });

        $serviceCombo.trigger('change');
    };

    var setValue = function ($parent, val) {
        var options = $parent.data('options');

        $parent.data('webgis-value-element').val(val);

        if ($parent.hasClass('webgis-tool-parameter-persistent')) {
            var options = $parent.data('options');
            if (options && options.map) {
                options.map.setPersistentToolParameter(options.map.getActiveTool(), $parent.attr('id'), val);
            }
        }

        if (options && options.map) {
            options.map.ui.refreshUIElements();
        }
    };
})(webgis.$ || jQuery);
