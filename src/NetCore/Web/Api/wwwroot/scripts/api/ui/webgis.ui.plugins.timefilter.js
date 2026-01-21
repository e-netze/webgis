webgis.ui.definePlugin('webgis_timeFilterControl', {
    defaults: {
        map: null
    },
    init: function () {
        const $el = this.$el;
        let o = this.options;

        $el.addClass('webgis-timefilter-holder')
           .data('options', this.options);

        const map = this.options.map;
        if (!map) {
            console.error('Time Filter: map option is required');
            return;
        };

        const $serviceCombo = $("<select>")
            .addClass("webgis-input webgis-timefilter-services")
            .appendTo($el);


        let minStart = Number.MAX_VALUE;
        let maxEnd = Number.MIN_VALUE;
        $("<option>").attr('value', '*').text(webgis.l10n.get('all-time-dependent-services')).appendTo($serviceCombo);

        for (let service of map.getTimeInfoServices()) {
            $("<option>")
                .attr("value", service.id)
                .text(service.name)
                .appendTo($serviceCombo);

            let start = Number.MAX_VALUE;
            let end = Number.MIN_VALUE;
            let intervalUnits = [];
            let interval = 9999999;

            for (let layer of service.getTimeInfoLayers()) {
                start = Math.min(start, layer.time_info.extent[0]);
                end = Math.max(end, layer.time_info.extent[1]);

                minStart = Math.min(start, minStart);
                maxEnd = Math.max(end, maxEnd);

                if (!intervalUnits[layer.time_info.interval_unit]) {
                    intervalUnits[layer.time_info.interval_unit] = true;
                }

                interval = Math.min(interval, layer.time_info.interval);
            }

            o[service.id] = {
                start: start,
                end: end,
                intervalUnits: intervalUnits,
                interval: Math.max(1, interval)
            };
        }

        o["*"] = {
            start: minStart,
            end: maxEnd,
            intervalUnits: [],
            interval: 1
        };
        o["*"].intervalUnits["years"] = true;

        const $epochControlHolder = $("<div>").addClass("webgis-timefilter-epochcontrol-holder").appendTo($el);   
        this.createEpochControl($epochControlHolder, o, $serviceCombo.val());

        const me = this;
        $serviceCombo.on('change.webgis_time_filter_control', function(e) {
            const selectedServiceId = $(this).val();

            me.createEpochControl($epochControlHolder, o, selectedServiceId);
        });

        const $buttonGroup = webgis.ui.createButtonGroup($el);

        $("<button>")
            .addClass('webgis-button')
            .text(webgis.l10n.get('apply'))
            .appendTo($buttonGroup)
            .on('click.webgis_time_filter_control', function(e) {
                const start = $epochControlHolder.data('start');
                const end = $epochControlHolder.data('end');

                var affectedServices = $serviceCombo.val() === '*'
                    ? o.map.getTimeInfoServices()
                    : [o.map.getService($serviceCombo.val())];

                for (let affectedService of affectedServices) {
                    affectedService.setTimeEpoch(start ? start.getTime() : null, end ? end.getTime() : null);
                }
            });

        o.map.events.on('onaddservice', this.addService, this);
        o.map.events.on('onremoveservice', this.removeService, this);
    },
    destroy: function () {
        //console.log('Destroy time filter'); 
        this.$el.off('.webgis_time_filter_control');

        const o = this.options;
        o.map.events.off('onaddservice', this.addService);
        o.map.events.off('onremoveservice', this.removeService);

    },
    methods: {
        createEpochControl: function ($parent, options, serviceId) {
            const start = options[serviceId].start;
            const end = options[serviceId].end;

            console.log("createEpochControl", options);

            let currentStart, currentEnd;
            const service = options.map.getService(serviceId);
            if (service) {  // serviceId can be '*' => all services...
                const timeEpoch = options.map.getService(serviceId)?.getTimeEpoch();
                if (timeEpoch && timeEpoch.length === 2) {
                    currentStart = timeEpoch[0];
                    currentEnd = timeEpoch[1];
                }
            }

            console.log('years', options[serviceId].intervalUnits["years"] === true);
            console.log('months', options[serviceId].intervalUnits["months"] === true);
            console.log('days', options[serviceId].intervalUnits["days"] === true);

            //$parent.destroy();
            $("<div>").appendTo($parent.empty()).webgis_dateCombo({
                range: true,
                start: new Date(start),
                end: new Date(end),
                currentStart: currentStart,
                currentEnd: currentEnd,
                showYear: options[serviceId].intervalUnits["years"] === true || options[serviceId].intervalUnits["months"] === true || options[serviceId].intervalUnits["days"] === true,
                showMonth: options[serviceId].intervalUnits["months"] === true || options[serviceId].intervalUnits["days"] === true,
                showDay: options[serviceId].intervalUnits["days"] === true,
                interval: options[serviceId].interval,
                onChange: function (start, end) {
                    //const unit = $unitCombo.val();
                    //map.setTimeFilter(start ? start.getTime() : null, end ? end.getTime() : null, unit);
                    //console.log('Time filter changed', start, end);

                    $parent.data('start', start);
                    $parent.data('end', end);
                }
            });
        },
        setService: function (options) {
            //console.log('setService', options);
            const service = options.service;
            if (!service) return;

            const $serviceCombo = this.$el.find('.webgis-timefilter-services');
            $serviceCombo.val(service.id).trigger('change');
        },

        addService: function (e, service) {
            if (!service?.hasTimeInfoLayers()) return;

            const $serviceCombo = this.$el.find(".webgis-timefilter-services");
            $("<option>")
                .attr("value", service.id)
                .text(service.name)
                .appendTo($serviceCombo);
   
        },
        removeService: function (e, service) {
            const $serviceCombo = this.$el.find(".webgis-timefilter-services");
            $serviceCombo.children("option[value='" + service.id + "']").remove();
        }
    }
});

webgis.ui.builder['timefiltercontrol'] = (map, $newElement, element) => {
    $newElement.webgis_timeFilterControl({ map: map });
};

webgis.ui.definePlugin('webgis_timeFilterList', {
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

        map.events.on('service-timeepoch-changed',
            this.refresh,
            this
        );

        this.refresh();

        map.events.on('onremoveservice', this.removeService, this);
    },
    destroy: function () {
        //console.log('Destroy time filter list'); 
        this.$el.off('.webgis_time_filter_list');

        const map = this.options.map;
        map.events.off('onremoveservice', this.removeService);
    },
    methods: {
        refresh: function () {
            const $ = this.$;
            const $el = this.$el;
            const o = this.options;
            const map = o.map;

            $el.empty()
                .addClass('webgis-timefilter-list-holder')
                .data('options', o);

            $("<button>")
                .addClass("webgis-button uibutton uibutton-danger")
                .addClass("webgis-dependencies webgis-dependency-hastimefilters")
                .text(webgis.l10n.get("remove-all-filters"))
                .appendTo(webgis.ui.createButtonGroup($el))
                .on("click", function () {
                    for (let s of map.getTimeInfoServices()) {
                        s.setTimeEpoch(null);
                    }

                    if (o.onRemoveAllFilters) o.onRemoveAllFilters();
                });

            const $timeEpochList = $("<ul>")
                .addClass('webgis-timefilter-list')
                .appendTo($el);

            for (let service of map.getTimeInfoServices()) {
                let timeEpoch = service.getTimeEpoch();
                if (!timeEpoch || timeEpoch.length === 0) {
                    continue;
                }

                let $item = $("<li>")
                    .data('service', service)
                    .addClass('webgis-timefilter-list-item')
                    .appendTo($timeEpochList);

                $("<div>").addClass('title')
                    .text(service.name)
                    .appendTo($item);
                $("<div>").addClass('range')
                    .text((timeEpoch[0] ? new Date(timeEpoch[0]).toLocaleString() : 'null') + ' - ' + (timeEpoch[1] ? new Date(timeEpoch[1]).toLocaleString() : 'null'))
                    .appendTo($item);

                $("<div>").addClass('button delete')
                    .appendTo($item)
                    .on("click.webgis_time_filter_list", function(e)  {
                        e.stopPropagation();
                        $(this).closest('.webgis-timefilter-list-item')
                            .data('service')
                            .setTimeEpoch(null);
                    });

                $item.on('click.webgis_time_filter_list', function (e) {
                    e.stopPropagation();
                    const $i = $(this);
                    $(".webgis-timefilter-holder").webgis_timeFilterControl('setService', {
                        service: $i.data('service')
                    });
                });
            }
        },
        removeService: function (e, service) {
            const $timeEpochList = this.$el.find(".webgis-timefilter-list");
            $timeEpochList.children("li").each(function () {
                const $item = $(this);
                if ($item.data('service').id === service.id) {
                    $item.remove();
                }
            });
        }
    }
});

webgis.ui.builder['timefilterlist'] = (map, $newElement, element) => {
    $newElement.webgis_timeFilterList({ map: map });
};

webgis.ui.showRemoveTimeFiltersDialog = function (map) {
    webgis.$("body").webgis_modal({
        title: webgis.l10n.get("remove-filters"),
        width: "600px",
        onload: function ($content) {
            $("<div>").webgis_timeFilterList({
                map: map,
                onRemoveAllFilters: function () { webgis.$("body").webgis_modal("close"); }
            })
            .appendTo($content);
        }
    });
};