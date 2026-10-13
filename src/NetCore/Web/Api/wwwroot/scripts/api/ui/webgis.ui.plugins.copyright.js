// WebGIS UI Plugins (copyright)
// webgis_copyright: lists the copyright infos of the services of a map (updated on add/remove service)

webgis.ui.definePlugin('webgis_copyright', {
    defaults: {
        map: null
    },
    init: function () {
        const o = this.options;

        this.$el.addClass('webgis-copyright-container');
        this.$el.data('map', o.map);

        this._onAddService = (e, service) => this.addService(service);
        this._onRemoveService = (e, service) => this.removeService(service);
        o.map.events.on('onaddservice', this._onAddService, this.$el[0]);
        o.map.events.on('onremoveservice', this._onRemoveService, this.$el[0]);

        for (let serviceId in o.map.services) {
            this.addService(o.map.services[serviceId]);
        }
    },
    destroy: function () {
        const map = this.options.map;
        if (map && map.events) {
            map.events.off('onaddservice', this._onAddService);
            map.events.off('onremoveservice', this._onRemoveService);
        }
    },
    methods: {
        addService: function (service) {
            const $ = this.$, $elem = this.$el;

            if (!service || !service.copyrightId)
                return;
            const map = this.options.map;
            if (map == null || map !== service.map)
                return;

            const copyrightIds = service.copyrightId.split(',');
            for (let c in copyrightIds) {
                const copyrightInfo = map.getCopyright(copyrightIds[c]);
                if (!copyrightInfo)
                    return;

                let $div = $elem.find("[data-copyrightid='" + copyrightIds[c] + "']");
                if ($div.length === 0) {
                    $div = $("<div></div>").attr("data-copyrightid", copyrightIds[c]).addClass('webgis-copyright-info').appendTo($elem);
                    $("<div>").addClass('webgis-copyright-services').appendTo($div);
                    if (copyrightInfo.logo) {
                        const $img = $("<img>").attr('src', copyrightInfo.logo);
                        if (copyrightInfo.logo_size && copyrightInfo.logo_size[0] > 0)
                            $img.css('width', copyrightInfo.logo_size[0]);
                        if (copyrightInfo.logo_size && copyrightInfo.logo_size[1] > 0)
                            $img.css('height', copyrightInfo.logo_size[1]);
                        $img.appendTo($div);
                    }
                    if (copyrightInfo.copyright)
                        $("<div>" + copyrightInfo.copyright + "</div>").appendTo($div);
                    if (copyrightInfo.link)
                        $("<a href='" + copyrightInfo.link + "' target='_blank'>" + (copyrightInfo.link_text ? copyrightInfo.link_text : copyrightInfo.link) + "</a>").appendTo($div);
                    if (copyrightInfo.advice)
                        $("<p>").html(copyrightInfo.advice).appendTo($div);
                }

                const $servicesDiv = $div.find('.webgis-copyright-services');
                $("<div>" + service.name + "</div>").attr("data-service", service.id).appendTo($servicesDiv);
            }
        },
        removeService: function (service) {
            const $elem = this.$el;

            if (!service || !service.copyrightId)
                return;
            const map = this.options.map;
            if (map == null || map !== service.map)
                return;

            const copyrightIds = service.copyrightId.split(',');
            for (let c in copyrightIds) {
                const $div = $elem.find("[data-copyrightid='" + copyrightIds[c] + "']");
                const $servicesDiv = $div.find('.webgis-copyright-services');
                $servicesDiv.find("[data-service='" + service.id + "']").remove();
                if ($servicesDiv.find('div').length == 0)
                    $div.remove();
            }
        }
    }
});
