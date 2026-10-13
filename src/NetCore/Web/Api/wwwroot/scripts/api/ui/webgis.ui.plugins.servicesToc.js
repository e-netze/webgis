// WebGIS UI Plugins (servicesToc)
// webgis_servicesToc: simple services TOC (settings tab) - collapsible service items, optional layer checkboxes, 'add services' button

webgis.ui.definePlugin('webgis_servicesToc', {
    defaults: {
        map: null,
        gdi_button: false,
        themes: false,
        opacity_slider: true
    },
    init: function () {
        const $ = this.$, o = this.options, elem = this.$el.get(0);
        const ctor = this.constructor;

        elem._map = o.map;
        elem.options = o;

        this._services = [];
        this._onAddService = this.addService.bind(this);
        this._onRemoveService = this.removeService.bind(this);
        // parent is not passed (legacy behavior): updates the items of all services TOCs
        this._onChangeVisibility = function (e, service) {
            ctor.checkItemVisibility(e, service);
        };

        if (o.map) {
            o.map.events.on('onaddservice', this._onAddService);
            o.map.events.on('onremoveservice', this._onRemoveService);
        }

        this.$el.addClass('webgis-services_toc-holder');
        $("<ul class='webgis-services_toc-list'></ul>").appendTo(this.$el);

        if (o.map !== null) {
            for (let serviceId in o.map.services) {
                this.addService({}, o.map.services[serviceId]);
            }
        }
        if (o.gdi_button == true) {
            $("<button>Dienste hinzufügen</button>")
                .addClass('webgis-button light')
                .appendTo(this.$el)
                .on('click.webgis_services_toc', function () {
                    const map = o.map;
                    $('body').webgis_modal({
                        title: 'Dienst hinzufügen',
                        onload: function ($content) {
                            $content.webgis_addServicesToc({ map: map });
                        }
                    });
                });
        }
    },
    destroy: function () {
        const map = this.options.map;
        if (map && map.events) {
            map.events.off('onaddservice', this._onAddService);
            map.events.off('onremoveservice', this._onRemoveService);
        }
        for (const service of this._services || []) {
            if (service.events) {
                service.events.off('onchangevisibility', this._onChangeVisibility);
            }
        }
        this._services = [];
        this.$el.find('*').addBack().off('.webgis_services_toc');
    },
    methods: {
        addService: function (e, service) {
            const $ = this.$, o = this.options;
            if (service == null || (service.serviceInfo.type !== 'image' && service.serviceInfo.type !== 'collection'))
                return;

            const $ul = this.$el.find('.webgis-services_toc-list');
            let $li = $("<li class='webgis-services_toc-title webgis-services_toc-collapsable'></li>");
            $li.prependTo($ul);
            $("<span style='position:absolute' class='webgis-services_toc-plus webgis-api-icon webgis-api-icon-triangle-1-s'></span>").appendTo($li);
            $("<div class='webgis-services_toc-title-text'>" + service.name + "</div>").on('click.webgis_services_toc', function (event) {
                event.stopPropagation();

                $li = $(this.parentNode);
                if ($(this.parentNode).hasClass('webgis-expanded')) {
                    $li.removeClass('webgis-expanded');
                    $li.children('.webgis-services_toc-content').slideUp();

                    $li.children('.webgis-services_toc-plus')
                        .removeClass('webgis-api-icon-triangle-1-e')
                        .addClass('webgis-api-icon-triangle-1-s');
                }
                else {
                    $li.closest('ul').find('.webgis-expanded')
                        .removeClass('webgis-expanded')
                        .find('.webgis-services_toc-content').slideUp();

                    $li.closest('ul')
                        .find('.webgis-services_toc-plus')
                        .removeClass('webgis-api-icon-triangle-1-e')
                        .addClass('webgis-api-icon-triangle-1-s');

                    $li.addClass('webgis-expanded');
                    $li.children('.webgis-services_toc-content').slideDown();
                    $li.children('.webgis-services_toc-plus')
                        .removeClass('webgis-api-icon-triangle-1-s')
                        .addClass('webgis-api-icon-triangle-1-e');
                }
            }).appendTo($li);
            const $div = $("<div class='webgis-services_toc-content' style='display:none' id='div_'></div>").appendTo($li);
            $li.get(0).service = service;
            const $content_ul = $("<ul>").appendTo($div);
            if (o.themes) {
                const $themes_li = $("<li class='webgis-services_toc-item-group webgis-services_toc-collapsable'>").appendTo($content_ul);
                $("<div><span style='position:absolute' class='webgis-api-icon webgis-api-icon-triangle-1-s'></span><span style='margin-left:14px'>&nbsp;" + webgis.l10n.get("layers") + "</span></div>").appendTo($themes_li)
                    .on('click.webgis_services_toc', function () {
                        const $this = $(this);
                        if ($this.children('.webgis-api-icon-triangle-1-e').length > 0) {
                            $this.children('.webgis-api-icon-triangle-1-e')
                                .removeClass('webgis-api-icon-triangle-1-e')
                                .addClass('webgis-api-icon-triangle-1-s');
                            $this.parent()
                                .children('ul').slideUp();
                        }
                        else {
                            $this.children('.webgis-api-icon-triangle-1-s')
                                .removeClass('webgis-api-icon-triangle-1-s')
                                .addClass('webgis-api-icon-triangle-1-e');
                            $this.parent().children('ul').slideDown();
                        }
                    });
                const $toc_ul = $("<ul style='display:none'>").appendTo($themes_li);
                for (let l = 0, to = service.layers.length; l < to; l++) {
                    const layer = service.layers[l];
                    if (layer.locked)
                        continue;
                    let name = layer.name.split('\\');
                    name = name[name.length - 1];
                    const $toc_li = $("<li class='webgis-services_toc-item webgis-services_toc-checkable'></li>").appendTo($toc_ul);
                    $("<span><img class='webgis-service_toc-checkable-icon' />&nbsp;" + name + "</span>").appendTo($toc_li);
                    $toc_li.get(0).layer = layer;
                    $toc_li.on('click.webgis_services_toc', function () {
                        const $li = $(this).closest('.webgis-services_toc-title');
                        if ($li.length == 1) {
                            const service = $li.get(0).service;
                            const layer = this.layer;
                            if (service && layer) {
                                service.setLayerVisibility([layer.id], !$(this).find('.webgis-service_toc-checkable-icon').hasClass('webgis-checked'));
                            }
                        }
                    });
                }
            }

            // parent is not passed (legacy behavior): updates the items of all services TOCs
            this.constructor.checkItemVisibility(service);

            if (this._services.indexOf(service) < 0) {
                this._services.push(service);
                service.events.on('onchangevisibility', this._onChangeVisibility);
            }
        },
        removeService: function (e, service) {
            const $ = this.$;
            if (service == null)
                return;

            this.$el.find('.webgis-services_toc-list').find('li').each(function (i, e) {
                if (e.service && e.service.id == service.id) {
                    $(e).remove();
                }
            });

            const index = this._services.indexOf(service);
            if (index >= 0) {
                this._services.splice(index, 1);
                if (service.events) {
                    service.events.off('onchangevisibility', this._onChangeVisibility);
                }
            }
        }
    },
    staticMethods: {
        checkItemVisibility: function (e, service, parent) {
            const $ = webgis.$ || jQuery;
            const $elem = $(parent || '.webgis-services_toc-holder');
            $elem.find('.webgis-services_toc-item').each(function (i, li) {
                if (!li.layer)
                    return;
                if (li.layer.visible) {
                    $(li).find('.webgis-service_toc-checkable-icon').attr('src', webgis.css.imgResource('check1.png', 'toc')).addClass('webgis-checked');
                }
                else {
                    $(li).find('.webgis-service_toc-checkable-icon').attr('src', webgis.css.imgResource('check0.png', 'toc')).removeClass('webgis-checked');
                }
            });
        }
    }
});
