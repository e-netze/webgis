// WebGIS Click Note Plugin
// A simple clickable note that can be dismissed or trigger a callback
// eg. webgis.ui.showLayerNotVisibleNotification(service, query, $target);
//     used in query result table, to notify the user that the corresponing layer is invisible   

webgis.ui.definePlugin('webgis_clickNote', {
    defaults: {
        text: 'Note...',
        callback: null,
        styles: { backgroundColor: '#faa', cursor: 'pointer', padding: '4px 8px' }
    },

    init: function () {
        let o = this.options;
        // Events always use name namespace -> simpler deploy
        this.$el
            .css(o.styles)
            .css('position', 'relative')
            .text(o.text)
            .on('click.webgis_click_note', (e) => {
                e.stopPropagation();
                if (o.callback) o.callback();

                this.destroy();
                this.$el.remove();
            });
        $('<div>')
            .text('\u2715')
            .css({ position: 'absolute', top: 0, right: 0, padding: '4px 4px', cursor: 'pointer' })
            .appendTo(this.$el)
            .on('click.webgis_click_note', (e) => {
                e.stopPropagation();
                webgis.ui.destroyPluginsDeep(this.$el);
                this.$el.remove();
            });
    },
    destroy: function () {
        //console.log('Destroy click note'); 
        this.$el.off('.webgis_click_note');
    }
});

webgis.ui.showLayerNotVisibleNotification = function (service, query, $target) {
    if (webgis.usability.showQueryLayerNotVisbleNotification === true && service && query) {
        let associatedLayers = query.associatedlayers || [{ id: query.layerid }];
        let foundVisible = false;

        for (var associatedLayer of associatedLayers) {
            let layerService = associatedLayer.serviceid ? service.map?.getService(associatedLayer.serviceid) : service;
            let layer = layerService?.getLayer(associatedLayer.id);
            if (layer?.visible === true) {
                foundVisible = true;
            }
        };

        if (foundVisible === false) {
            $("<div>")
                .appendTo($target)
                .webgis_clickNote({
                    text: webgis.l10n.get('query-layer-not-visible-notification'),
                    callback: function () {
                        // Try to find and show the first associated layer
                        for (var associatedLayer of associatedLayers) {
                            let layerService = associatedLayer.serviceid ? service.map?.getService(associatedLayers[0].serviceid) : service;
                            //console.log('Set layer visible', associatedLayer, layerService, service);
                            if (layerService) {
                                layerService.setLayerVisibility([associatedLayer.id], true);
                                break;
                            }
                        }
                    }
                });
        }
    }
};

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


webgis.ui.definePlugin('webgis_dateCombo', {
    defaults: {
        showYear: true,
        showMonth: true,
        showDay: true,
        from: new Date(),  // now?
        to: new Date(),
        range: false,
        onChange: null // function (startDate, endDate) { ... }
    },
    init: function () {
        const o = this.options;
        const $container = this.$el.empty().addClass('webgis-date-combo');
        const now = new Date();
        let fromDate = o.from ? new Date(o.from) : now;
        let toDate = o.to ? new Date(o.to) : fromDate;

        let cls = 'year';
        if (o.showMonth) cls += '-month';
        if (o.showDay) cls += '-day';
        $container.addClass(cls);

        // Helper to create select options
        function createOptions(start, end, selected) {
            let opts = '';
            for (let i = start; i <= end; i++) {
                opts += `<option value="${i}"${i === selected ? ' selected' : ''}>${i}</option>`;
            }
            return opts;
        }

        const $labels = $("<div>").appendTo($container);
        const $combos = $("<div>").appendTo($container);
        const $dateLabelContainer = $("<div>").addClass('webgis-datelabel-container').appendTo($labels);
        const $dateContainer = $("<div>").addClass('webgis-date-container').appendTo($combos);

        // Year
        let $year, $yearEnd;
        let $yearLabel, $yearEndLabel;

        const yearStart = fromDate.getFullYear();
        const yearEnd = toDate.getFullYear();

        if (o.showYear) {
            $yearLabel = $("<div>").addClass("date-year").text(webgis.l10n.get("year"));
            $year = $(`<select class="webgis-input date-year">${createOptions(yearStart, yearEnd, fromDate.getFullYear())}</select>`);
        }

        // Month
        let $month, $monthEnd;
        let $monthLabel, $monthEndLabel;

        if (o.showMonth) {
            $monthLabel = $("<div>").addClass("date-month").text(webgis.l10n.get("month"));
            $month = $(`<select class="webgis-input date-month">${createOptions(1, 12, fromDate.getMonth() + 1)}</select>`);
        }

        // Day
        let $day, $dayEnd;
        let $dayLabel, $dayEndLabel;

        function daysInMonth(year, month) {
            return new Date(year, month, 0).getDate();
        }
        if (o.showDay) {
            $dayLabel = $("<div>").addClass("date-day").text(webgis.l10n.get("day"));
            $day = $(`<select class="webgis-input date-day">${createOptions(1, daysInMonth(fromDate.getFullYear(), fromDate.getMonth() + 1), fromDate.getDate())}</select>`);
        }

        for (let $e of [$dayLabel, $monthLabel, $yearLabel]) {
            $e?.appendTo($dateLabelContainer);
        }
        for (let $e of [$day, $month, $year]) {
            $e?.appendTo($dateContainer);
        }

        if (o.range) {
            $labels.append(" - ");
            $combos.append(" - ");

            const $dateEndLabelContainer = $("<div>").addClass('webgis-datelabel-container').appendTo($labels);
            const $dateEndContainer = $("<div>").addClass('webgis-date-container').appendTo($combos);

            if (o.showYear) {
                $yearEndLabel = $("<div>").addClass("date-year").text(webgis.l10n.get("year"));
                $yearEnd = $(`<select class="webgis-input date-year-end">${createOptions(yearStart, yearEnd, toDate ? toDate.getFullYear() : fromDate.getFullYear())}</select>`);
            }
            if (o.showMonth) {
                $monthEndLabel = $("<div>").addClass("date-month").text(webgis.l10n.get("month"));
                $monthEnd = $(`<select class="webgis-input date-month-end">${createOptions(1, 12, toDate ? toDate.getMonth() + 1 : fromDate.getMonth() + 1)}</select>`);
            }
            if (o.showDay) {
                $dayEndLabel = $("<div>").addClass("date-day").text(webgis.l10n.get("day"));
                $dayEnd = $(`<select class="webgis-input date-day-end">${createOptions(1, daysInMonth(toDate ? toDate.getFullYear() : fromDate.getFullYear(), toDate ? toDate.getMonth() + 1 : fromDate.getMonth() + 1), toDate ? toDate.getDate() : fromDate.getDate())}</select>`);
            }

            for (let $e of [$dayEndLabel, $monthEndLabel, $yearEndLabel]) {
                $e?.appendTo($dateEndLabelContainer);
            }
            for (let $e of [$dayEnd, $monthEnd, $yearEnd]) {
                $e?.appendTo($dateEndContainer);
            }
        }

        // Update days when year/month changes
        function updateDays($yearSel, $monthSel, $daySel) {
            if (!$yearSel || !$monthSel || !$daySel) return;
            const y = parseInt($yearSel.val(), 10);
            const m = parseInt($monthSel.val(), 10);
            const d = parseInt($daySel.val(), 10);
            const days = daysInMonth(y, m);
            $daySel.empty().append(createOptions(1, days, Math.min(d, days)));
        }

        if (o.showDay && o.showMonth && o.showYear) {
            $year && $year.on('change.webgis_date_combo', () => updateDays($year, $month, $day));
            $month && $month.on('change.webgis_date_combo', () => updateDays($year, $month, $day));
            if (o.range) {
                $yearEnd && $yearEnd.on('change.webgis_date_combo', () => updateDays($yearEnd, $monthEnd, $dayEnd));
                $monthEnd && $monthEnd.on('change.webgis_date_combo', () => updateDays($yearEnd, $monthEnd, $dayEnd));
            }
        }

        // Store value(s) on change
        function getDateFromSelects($y, $m, $d, isEnd) {
            let result = null;
            if ($y && $m && $d) {
                result = new Date(
                    parseInt($y.val(), 10),
                    parseInt($m.val(), 10) - 1,
                    parseInt($d.val(), 10) + (isEnd ? 1 : 0));
            }
            else if ($y && $m) {
                result = new Date(
                    parseInt($y.val(), 10),
                    parseInt($m.val(), 10) - 1 + (isEnd ? 1 : 0),
                    1);
            }
            else if ($y) {
                result = new Date(
                    parseInt($y.val(), 10) + (isEnd ? 1 : 0),
                    0,
                    1);
            }

            if (result && isEnd) result.setSeconds(result.getSeconds() - 1);

            return result;
        }

        const updateValue = () => {
            this.value_start = getDateFromSelects($year, $month, $day);
            if (o.range) {
                this.value_end = getDateFromSelects($yearEnd, $monthEnd, $dayEnd, true);
            } else {
                this.value_end = new Date(this.value_start.getTime() + 10000 * 60 * 24 * 364);
            }

            if (o.onChange) {
                o.onChange(this.value_start, this.value_end);
            }
        };

        $container.find('select').on('change.webgis_date_combo', updateValue);
        updateValue();
    },
    destroy: function () {
        console.log('Destroy time filter'); 
        this.$el.off('.webgis_date_combo');
    }
});