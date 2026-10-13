// WebGIS UI Plugins (editthemeCombo)
// webgis_editthemeCombo: select with the edit themes of all map services (updated on add/remove service)
// Element builder: webgis.ui.builder.combos.js ('editthemecombo')

webgis.ui.definePlugin('webgis_editthemeCombo', {
    defaults: {
        map: null,
        customitems: [],
        onchange: null,
        dbrights: null
    },
    init: function () {
        const $ = this.$, o = this.options, $elem = this.$el;
        const elem = $elem[0];
        elem._map = o.map;
        elem._opt = o;
        if (o.onchange)
            $elem.on('change.webgis_editthemecombo', o.onchange);

        if (o.customitems) {
            for (let i in o.customitems) {
                const item = o.customitems[i];
                let $parent = $elem;
                if (item.category) {
                    $parent = $elem.find("optgroup[label='" + item.category + "']");
                    if ($parent.length == 0)
                        $parent = $("<optgroup label='" + item.category + "' />").addClass('webgis-custom-option').appendTo($elem);
                }
                $("<option value='" + item.value + "'>" + item.name + "</option>").addClass('webgis-custom-option').appendTo($parent);
            }
        }
        if (o.map !== null) {
            for (let serviceId in o.map.services) {
                this.addService(o.map.services[serviceId]);
            }

            this._onAddService = (e, service) => this.addService(service);
            this._onRemoveService = (e, service) => this.removeService(service);
            o.map.events.on('onaddservice', this._onAddService, elem);
            o.map.events.on('onremoveservice', this._onRemoveService, elem);
        }
    },
    destroy: function () {
        const map = this.options.map;
        if (map && map.events) {
            map.events.off('onaddservice', this._onAddService);
            map.events.off('onremoveservice', this._onRemoveService);
        }
        this.$el.off('.webgis_editthemecombo');
    },
    methods: {
        addService: function (service) {
            const $ = this.$;
            if (service == null || service.editthemes == null || service.editthemes.length === 0)
                return;

            const typePrefix = [];
            typePrefix["point"] = "☆ ";
            typePrefix["line"] = "— ";
            typePrefix["polygon"] = "▭ ";

            const $elem = this.$el;
            const count = $elem.find('option').length;
            const options = this.options;
            let isInitialized = $elem.hasClass('initialized');
            const $group = $("<optgroup label='" + service.name + "' />");
            const customOptions = $elem.children('.webgis-custom-option');
            if (customOptions.length === 0) {
                $group.prependTo($elem);
            } else {
                $group.insertAfter(customOptions[customOptions.length - 1]);
            }

            $group.get(0).service = service;
            for (let i = 0, to = service.editthemes.length; i < to; i++) {
                const edittheme = service.editthemes[i];

                if (options.dbrights && edittheme.dbrights) {
                    if (edittheme.dbrights.indexOf(options.dbrights) < 0) {
                        continue;
                    }
                }

                const layer = service.getLayer(edittheme.layerid);
                const prefix = layer && typePrefix[layer.type] ? typePrefix[layer.type] : '';

                const val = service.id + ',' + edittheme.layerid + ',' + edittheme.themeid;
                const $opt = $("<option value='" + val + "'>" + prefix + edittheme.name + "</option>")
                    .addClass('filter-' + ((layer && layer.type) ? layer.type : 'unknown'))
                    .appendTo($group);

                $opt.get(0).edittheme = edittheme;
                if (!isInitialized && (val === webgis.initialParameters.query || edittheme.themeid === webgis.initialParameters.editthemeid)) {
                    $elem.addClass('initialized').val(val).trigger('change');
                    isInitialized = true;
                }
            }
            if (count === 0 && !$elem.hasClass('initialized'))
                $elem.trigger('change');

            if ($.fn.webgis_catCombo)
                $elem.webgis_catCombo({
                    classFilters: {
                        "filter-point": { label: typePrefix['point'] + 'Punkte/Symbole' },
                        "filter-line": { label: typePrefix['line'] + 'Linien' },
                        "filter-polygon": { label: typePrefix['polygon'] + 'Flächen/Polygone' }
                    }
                });
        },
        removeService: function (service) {
            const $ = this.$, $elem = this.$el;
            $elem.find('optgroup').each(function (i, e) {
                if (e.service && e.service.id === service.id)
                    $(e).remove();
            });
            $elem.trigger('change');
        }
    }
});
