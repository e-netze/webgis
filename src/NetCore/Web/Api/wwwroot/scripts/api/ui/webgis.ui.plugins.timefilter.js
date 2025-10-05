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

  
        for (let service of map.getTimeInfoServices()) {
            $("<option>")
                .attr("value", service.id)
                .text(service.name)
                .appendTo($serviceCombo);

            let start = Number.MAX_VALUE;
            let end = Number.MIN_VALUE;
            let intervalUnits = [];

            for (let layer of service.getTimeInfoLayers()) {
                start = Math.min(start, layer.time_info.extent[0]);
                end = Math.max(end, layer.time_info.extent[1]);

                if (!intervalUnits[layer.time_info.interval_unit]) {
                    intervalUnits[layer.time_info.interval_unit] = true;
                }
            }

            o[service.id] = {
                start: start,
                end: end,
                intervalUnits: intervalUnits
            };
        }

        const $epochControlHolder = $("<div>").addClass("webgis-timefilter-epochcontrol-holder").appendTo($el);   
        this.createEpochControl($epochControlHolder, o, $serviceCombo.val());

        const me = this;
        $serviceCombo.on('change.webgis_time_filter_control', function(e) {
            const selectedServiceId = $(this).val();

            me.createEpochControl($epochControlHolder, o, selectedServiceId);
        });

        $("<button>")
            .addClass('webgis-button')
            .text(webgis.l10n.get('apply'))
            .appendTo($el)
            .on('click.webgis_time_filter_control', function(e) {
                const start = $epochControlHolder.data('start');
                const end = $epochControlHolder.data('end');

                o.map.getService($serviceCombo.val())
                     .setTimeEpoch(start ? start.getTime() : null, end ? end.getTime() : null);
            });
    },
    destroy: function () {
        //console.log('Destroy time filter'); 
        this.$el.off('.webgis_time_filter_control');
    },
    methods: {
        createEpochControl: function ($parent, options, serviceId) {
            let start = options[serviceId].start;
            let end = options[serviceId].end;

            //$parent.destroy();
            $("<div>").appendTo($parent.empty()).webgis_dateCombo({
                range: true,
                from: new Date(start),
                to: new Date(end),
                showYear: true,
                showMonth: true,
                showDay: true,
                onChange: function (start, end) {
                    //const unit = $unitCombo.val();
                    //map.setTimeFilter(start ? start.getTime() : null, end ? end.getTime() : null, unit);
                    //console.log('Time filter changed', start, end);

                    $parent.data('start', start);
                    $parent.data('end', end);
                }
            });
        }
    }
});

webgis.ui.builder['timefiltercontrol'] = (map, $newElement, element) => {
    $newElement.webgis_timeFilterControl({ map: map });
};

webgis.ui.definePlugin('webgis_timeFilterList', {
    defaults: {
        map: null
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
    },
    destroy: function () {
        //console.log('Destroy time filter list'); 
        this.$el.off('.webgis_time_filter_list');
    },
    methods: {
        refresh: function () {
            const $el = this.$el;
            const o = this.options;
            const map = o.map;

            $el.empty()
                .addClass('webgis-timefilter-list-holder')
                .data('options', this.options);

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
            }
        }
    }
});

webgis.ui.builder['timefilterlist'] = (map, $newElement, element) => {
    $newElement.webgis_timeFilterList({ map: map });
};