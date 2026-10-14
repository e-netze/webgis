// WebGIS UI Plugins (edittheme tree)
// webgis_editthemeTree
// Service combo + edit theme list of the edit tool; value = 'serviceId,layerId,themeId'
// Element builder: webgis.ui.builder.combos.js (edittheme-tree)

webgis.ui.definePlugin('webgis_editthemeTree', {
    defaults: {
        map: null,
        customitems: [],
        onchange: null,
        dbrights: null
    },
    init: function () {
        const $ = this.$, me = this, o = this.options, $parent = this.$el;

        $parent.addClass('webgis-edittheme-tree').data('options', o);
        if (o.onchange) {
            $parent.on('change.webgis_editthemeTree', o.onchange);
        }

        const $valueElement = $("<input>")
            .attr('type', 'hidden')
            .appendTo($parent);

        $parent.data('webgis-value-element', $valueElement);

        const $serviceCombo = $("<select>")
            .addClass('webgis-edittheme-tree-service-combo webgis-input')
            .attr("id", "webgis-edittheme-tree-service-combo")
            .appendTo($parent)
            .on('change.webgis_editthemeTree', function (e) {
                e.stopPropagation();

                const $this = $(this),
                    $themesList = $parent.children('.webgis-edittheme-tree-theme-list');

                const serviceId = $this.val();

                let tagsSet = new Set();
                $themesList.children('li').removeClass('webgis-tag-item').each(function (i, item) {
                    const $item = $(item);
                    const display = $item.attr('data-serviceId') === serviceId ? 'block' : 'none';

                    $item
                        .removeClass('selected')
                        .css('display', display);

                    if (display != 'none') {
                        const tagsAttr = $item.addClass('webgis-tag-item').attr('tags');
                        if (tagsAttr) {
                            tagsAttr.split(',').map(function (t) { return t.trim(); }).forEach(function (t) {
                                if (t) tagsSet.add(t);
                            });
                        }
                    }
                });

                me._setValue($this.val());
                $this.webgis_tagsCombo('set_tags', { tags: Array.from(tagsSet) });
                $this.webgis_tagsCombo('restore');
            })
            .webgis_tagsCombo({
                itemContainer: $parent,
                itemSelector: '.webgis-tag-item',
                id: o.id + '-tags',
                addClasses: 'webgis-tool-parameter'
            });

        const $themeList = $("<ul>")
            .addClass('webgis-edittheme-tree-theme-list')
            .appendTo($parent);

        if (o.map !== null) {
            for (const serviceId in o.map.services) {
                this.addService({}, o.map.services[serviceId]);
            }

            this._onAddService = this.addService.bind(this);
            this._onRemoveService = this.removeService.bind(this);
            o.map.events.on('onaddservice', this._onAddService);
            o.map.events.on('onremoveservice', this._onRemoveService);

            let initalized = false;
            if ($parent.hasClass('webgis-tool-parameter-persistent')) {
                const persistentValue = o.map.getPersistentToolParameter(o.map.getActiveTool(), $parent.attr('id'));

                if (persistentValue) {
                    const persistentValueParts = persistentValue.split(',');

                    $serviceCombo.val(persistentValueParts[0]).trigger('change');
                    $themeList.children("[data-edittheme='" + persistentValue + "']").trigger('click');
                    initalized = true;
                }
            }

            if (!initalized) {
                $serviceCombo.val($serviceCombo.children().first().attr('value')).trigger('change');
            }
        }
    },
    destroy: function () {
        const map = this.options.map;
        if (map && this._onAddService) {
            map.events.off('onaddservice', this._onAddService);
            map.events.off('onremoveservice', this._onRemoveService);
        }
        this.$el.off('.webgis_editthemeTree');
    },
    methods: {
        value: function () {
            const $valueElement = this.$el.data('webgis-value-element');
            if ($valueElement) {
                return $valueElement.val();
            }

            return null;
        },
        editThemeSelected: function () {
            const $valueElement = this.$el.data('webgis-value-element');
            if ($valueElement) {
                return $valueElement.val() && $valueElement.val().indexOf(',') > 0;
            }

            return false;
        },
        addService: function (e, service) {
            const $ = this.$, me = this, o = this.options, $parent = this.$el;

            if (service == null || service.serviceInfo == null || service.editthemes == null || service.editthemes.length === 0) {
                return;
            }

            const $serviceCombo = $parent.children('.webgis-edittheme-tree-service-combo');
            const $themeList = $parent.children('.webgis-edittheme-tree-theme-list');

            $("<option>")
                .attr('value', service.serviceInfo.id)
                .text(service.serviceInfo.name)
                .appendTo($serviceCombo);

            for (let i = 0, to = service.editthemes.length; i < to; i++) {
                const edittheme = service.editthemes[i];

                if (o.dbrights && edittheme.dbrights) {
                    if (edittheme.dbrights.indexOf(o.dbrights) < 0) {
                        continue;
                    }
                }

                const layerIds = [edittheme.layerid]; // ToDo: use associated layers!?
                const val = service.id + ',' + edittheme.layerid + ',' + edittheme.themeid;

                const $item = $("<li>")
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
                    .on('click.webgis_editthemeTree', function (e) {
                        e.stopPropagation();

                        const $this = $(this);
                        let val = '';

                        if ($this.hasClass('selected')) {
                            $this.removeClass('selected');

                            val = $this.attr('data-edittheme').split(',')[0];
                        } else {
                            $this.parent().children('.selected').removeClass('selected');
                            $this.addClass('selected');

                            val = $this.attr('data-edittheme');
                        }

                        me._setValue(val);
                    });

                if (edittheme.tags) {
                    $item.attr('title', "Tags: " + edittheme.tags.map(t => "#" + t).join(', '));
                }

                $("<div>")
                    .data('layerIds', layerIds)
                    .addClass('checkbox')
                    .addClass(service.checkLayerVisibility(layerIds) !== 0 ? 'checked' : '')
                    .appendTo($item)
                    .on('click.webgis_editthemeTree', function (e) {
                        e.stopPropagation();

                        const $this = $(this),
                            serviceId = $this.parent().attr('data-serviceId');

                        if (!o.map)
                            return;

                        const service = o.map.getService(serviceId);
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
        },
        removeService: function (e, service) {
            const $ = this.$, $parent = this.$el;

            if (service == null || service.serviceInfo == null || service.editthemes == null || service.editthemes.length === 0) {
                return;
            }

            const $serviceCombo = $parent.children('.webgis-edittheme-tree-service-combo');
            const $themeList = $parent.children('.webgis-edittheme-tree-theme-list');

            $serviceCombo.children("option[value='" + service.serviceInfo.id + "']").remove();

            $themeList.children().each(function (i, item) {
                const $item = $(item);

                if ($item.attr('data-serviceId') === service.serviceInfo.id) {
                    $item.remove();
                }
            });

            $serviceCombo.trigger('change');
        },
        _setValue: function (val) {
            const o = this.options, $parent = this.$el;

            $parent.data('webgis-value-element').val(val);

            if (o.map && $parent.hasClass('webgis-tool-parameter-persistent')) {
                o.map.setPersistentToolParameter(o.map.getActiveTool(), $parent.attr('id'), val);
            }

            if (o.map) {
                o.map.ui.refreshUIElements();
            }
        }
    }
});
