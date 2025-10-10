
webgis.ui.definePlugin("webgis_visfilterCombo", {
    defaults: {
        map: null
    },
    init: function () {
        const $ = this.$;
        const o = this.options;

        $("<option value='#'></option>").text("--- " + webgis.l10n.get("select-filter") + " ---").appendTo(this.$el);
        if (o.map !== null) {
            for (var serviceId in o.map.services) {
                var service = o.map.services[serviceId];
                this.addService({}, service);
            }
        }
        o.map.events.on('onaddservice', this.addService);
        o.map.events.on('onremoveservice', this.removeService);
        if (o.val) {
            this.$el.val(o.val);
        }
    },
    destroy: function () {
        console.log("Destroy visfilter combo");

        const o = this.options;
        o.map.events.off('onaddservice', this.addService);
        o.map.events.off('onremoveservice', this.removeService);
    },
    methods: {
        addService: function (e, service) {
            const $ = this.$;
            //if (!$) return;

            if (!service || !service.filters || service.filters.length === 0)
                return;

            var $group = $("<optgroup label='" + service.name + "' />").appendTo(this.$el);

            $group.get(0).service = service;
            for (var i = 0, to = service.filters.length; i < to; i++) {
                var filter = service.filters[i];
                if (filter.visible === false)
                    continue;

                var val = service.id + "~" + filter.id;
                var $opt = $("<option value='" + val + "'>&nbsp;" + filter.name + "</option>").appendTo($group);
                if (service.map.hasFilter(val))
                    $opt.css({
                        backgroundImage: "url('" + webgis.css.img('check2-24.png') + "')",
                        backgroundPosition: "0px -2px",
                        backgroundRepeat: "no-repeat",
                        height: 24
                    });
            }
            if ($group.find('option').length === 0)
                $group.remove();
        },
        removeService: function (e, service) {
            const $ = this.$;
            //if (!$) return;

            this.$el.find('optgroup').each(function (i, e) {
                var groupService = $(e).data('service');
                if (groupService?.id === service.id)
                    $(e).remove();
            });
        }
    }
});

webgis.ui.builder["visfiltercombo.tagname"] = "select";
webgis.ui.builder["visfiltercombo"] = (map, $newElement, element) => {
    $newElement.addClass("webgis-select").webgis_visfilterCombo({ map: map, val: element.value });
};

webgis.ui.definePlugin("webgis_visfilterList", {
    defaults: {
        map: null,
        onRemoveAllFilters: null
    },
    init: function () {
        const map = this.options.map;
        if (!map) {
            console.error('Time Filter: map option is required');
            return;
        };

        map.events.on('visfilters-changed',
            this.refresh,
            this
        );

        this.refresh();
    },
    destroy: function () {
        this.$el.off('.webgis_visfilter_list');
    },
    methods: {
        refresh: function () {
            const $ = this.$;
            const $el = this.$el;
            const o = this.options;
            const map = o.map;

            $el.empty()
                .addClass('webgis-visfilter-list-holder')
                .data('options', o);

            $("<button>")
                .addClass("webgis-button uibutton uibutton-danger")
                .addClass("webgis-dependencies webgis-dependency-hasfilters")
                .text(webgis.l10n.get("remove-all-filters"))
                .appendTo(webgis.ui.createButtonGroup($el))
                .on("click", function () {
                    map.unsetAllFilters();
                    map.refresh();
                    map.ui.refreshUIElements();

                    if (o.onRemoveAllFilters) o.onRemoveAllFilters();
                });

            const $visFilterList = $("<ul>")
                .addClass('webgis-visfilter-list')
                .appendTo($el);

            const visFilters = map._visfilters || [];
            const spans = [];

            for (let f in visFilters) {
                const visFilter = visFilters[f];
                let itemVisFilters = [visFilter];

                if (visFilter.sp_id) {
                    if (spans[visFilter.sp_id]) continue;
                    spans[visFilter.sp_id] = true;
                    itemVisFilters = visFilters.filter(f => f.sp_id === visFilter.sp_id);
                } 

                let $item = $("<li>")
                    .addClass('webgis-visfilter-list-item')
                    .data('spanId', visFilter.sp_id)
                    .data('filterId', visFilter.id)
                    .appendTo($visFilterList);

                for (var itemVisFilter of itemVisFilters) {
                    let id = itemVisFilter.id;
                    if (id.indexOf("#TOC#~") === 0) {
                        const idParts = id.split('~');
                        const filterService = map.getService(idParts[1]);
                        if (!filterService) continue;
                        const filterLayer = filterService.getLayer(idParts[2]);
                        id = filterLayer ? filterLayer.name : id;
                    } else {
                        id = o.map.getFilterName(id) || id;
                    }
                    $("<div>").addClass("title").text(id).appendTo($item);
                }

                for (var itemVisFilter of itemVisFilters) {
                    if ($item.children('.statement').length > 0) break;

                    const $statement = $("<div>").addClass("statement").appendTo($item);
                    for (let a in itemVisFilter.args || []) {
                        const args = itemVisFilter.args[a];
                        $("<div>").text(args.n + ": " + args.v).appendTo($statement);
                    }
                }

                $("<div>").addClass('button delete')
                    .appendTo($item)
                    .on("click.webgis_visfilter_list", function (e) {
                        e.stopPropagation();

                        const $item = $(this).closest('.webgis-visfilter-list-item');
                        const o = $item.closest('.webgis-visfilter-list-holder').data('options');

                        if ($item.data("spanId")) {   // TOC visfilter
                            o.map.unsetFilterSpan($item.data("spanId"));
                        } else {
                            o.map.unsetFilter($item.data("filterId"));
                            o.map.refresh();
                        }

                        o.map.ui.refreshUIElements();
                    });

                if (map.getFilterSpan(visFilter.sp_id).length > 1) {
                    $("<div>").addClass('button split')
                        .appendTo($item)
                        .on("click.webgis_visfilter_list", function (e) {
                            e.stopPropagation();

                            const $item = $(this).closest('.webgis-visfilter-list-item');
                            const o = $item.closest('.webgis-visfilter-list-holder').data('options');

                            o.map.splitFilterSpan($item.data("spanId"));
                        });
                }

                $item.on("click.webgis_visfilter_list", function (e) {
                    e.stopPropagation();

                    const $item = $(this);
                    const o = $item.closest('.webgis-visfilter-list-holder').data('options');

                    if ($item.data("spanId")) {
                        webgis.tools.onButtonClick(o.map,
                            {
                                command: "edit_toc_layer_filter",
                                type: "servertoolcommand_ext",
                                id: "webgis.tools.presentation.visfilter",
                                map: o.map,
                                argument: $item.data("spanId")
                            }, this, null, null);
                        return;
                    }
                });
            }

        }
    }
});

webgis.ui.builder["visfilterlist"] = (map, $newElement, element) => {
    $newElement.webgis_visfilterList({ map: map });
};

webgis.ui.showRemoveVisFiltersDialog = function (map) {
    webgis.$("body").webgis_modal({
        title: webgis.l10n.get("remove-filters"),
        width: "600px",
        onload: function ($content) {

            //$("<button>")
            //    .addClass("webgis-button uibutton uibutton-danger")
            //    .text(webgis.l10n.get("remove-all-filters"))
            //    .appendTo(webgis.ui.createButtonGroup($content))
            //    .on("click", function () {
            //        map.unsetAllFilters();
            //        map.refresh();
            //        map.ui.refreshUIElements();
            //        $("body").webgis_modal("close");
            //    });

            $("<div>").webgis_visfilterList({
                    map: map,
                    onRemoveAllFilters: function () { webgis.$("body").webgis_modal("close"); }
                })
                .appendTo($content);
        }
    });
};