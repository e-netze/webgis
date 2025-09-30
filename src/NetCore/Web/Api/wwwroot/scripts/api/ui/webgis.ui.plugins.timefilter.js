webgis.ui.definePlugin('webgis_timeFilter', {
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

        const serviceList = $("<ul>").appendTo(this.$el);
        let start = Number.MAX_VALUE;
        let end = Number.MIN_VALUE;
        let intervalUnits = [];

        for (let service of map.getTimeInfoServices()) {
            $("<li>").text(service.name).appendTo(serviceList);
            for (let layer of service.getTimeInfoLayers()) {
                start = Math.min(layer.time_info.extent[0]);
                end = Math.max(layer.time_info.extent[1]);
                if (!intervalUnits[layer.time_info.interval_unit]) {
                    intervalUnits[layer.time_info.interval_unit] = true;
                }
            }
        }

        if (start === Number.MAX_VALUE || end === Number.MIN_VALUE) {
            $("<div>").text(webgis.l10n.get('time-filter-no-time-info')).appendTo($el);
            return;
        }

        const $unitCombo = $("<select>")
            .addClass('webgis-input')
            .appendTo($el);
        for (let unit in intervalUnits) {
            $("<option>").attr('value', unit).text(webgis.l10n.get(unit)).appendTo($unitCombo);
        }

        $("<div>").text(webgis.l10n.get('time-filter-range') + ': ' + new Date(start)
            .toISOString()
            .substring(0, 10) + ' - ' + new Date(end)
                .toISOString()
                .substring(0, 10))
            .appendTo(this.$el);

        $("<div>").appendTo(this.$el).webgis_dateCombo({
            range: false,
            from: new Date(start),
            to: new Date(end),
            showYear: true,
            showMonth: false,
            showDay: false,
            onChange: function (start, end) {
                //const unit = $unitCombo.val();
                //map.setTimeFilter(start ? start.getTime() : null, end ? end.getTime() : null, unit);
                //console.log('Time filter changed', start, end);

                $el.data('start', start);
                $el.data('end', end);
            }
        });

        $("<button>")
            .addClass('webgis-button')
            .text(webgis.l10n.get('apply'))
            .appendTo($el)
            .on('click.webgis_time-filter', (e) => {
                const start = $el.data('start');
                const end = $el.data('end');

                o.map.setTimeFilter(start ? start.getTime() : null, end ? end.getTime() : null)
            });
    },
    destroy: function () {
        //console.log('Destroy time filter'); 
        this.$el.off('.webgis_time_filter');
    }
});

webgis.ui.builder['timefilter'] = (map, $newElement, element) => {
    $newElement.webgis_timeFilter({ map: map });
};