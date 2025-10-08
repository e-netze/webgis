
webgis.ui.definePlugin("webgis_visfilterCombo", {
    defaults: {
        map: null
    },
    init: function () {
        var o = this.options;

        $("<option value='#'></option>").text(webgis.l10n.get("all-filters")).appendTo(this.$el);
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
    destroy: function () { },
    methods: {
        addService: function (e, service) {
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
        map: null
    },
    init: function () {
        this.refresh();
    },
    destroy: function () {
        this.$el.off('.webgis_visfilter_list');
    },
    methods: {
        refresh: function () {
            const $el = this.$el;
            const o = this.options;
            const map = o.map;

            $el.empty()
                .addClass('webgis-visfilter-list-holder')
                .data('options', o);

            const $visFilterList = $("<ul>")
                .addClass('webgis-visfilter-list')
                .appendTo($el);

            const visFilters = map._visfilters || [];

            for (let f in visFilters) {
                const visFilter = visFilters[f];

                let $item = $("<li>")
                    .addClass('webgis-visfilter-list-item')
                    .appendTo($visFilterList);

                $("<div>").addClass("title").text(visFilter.id).appendTo($item);

                $("<div>").addClass('button delete')
                    .appendTo($item)
                    .on("click.webgis_visfilter_list", function (e) {
                        e.stopPropagation();
                        //$(this).closest('.webgis-visfilter-list-item')
                        //    .data('service')
                        //    .setTimeEpoch(null);
                    });
            }

        }
    }
});

webgis.ui.builder["visfilterlist"] = (map, $newElement, element) => {
    $newElement.webgis_visfilterList({ map: map });
};