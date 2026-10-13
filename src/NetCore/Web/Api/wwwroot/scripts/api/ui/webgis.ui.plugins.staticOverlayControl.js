// WebGIS UI Plugins (staticOverlayControl)
// webgis_staticOverlayControl: list of the static overlay services (select, sort, opacity, topmost, remove, command buttons)
// Element builders: webgis.ui.builder.misc.js

webgis.ui.definePlugin('webgis_staticOverlayControl', {
    defaults: {
        map: null,
        add_remove: true,
        command_buttons: []
    },
    init: function () {
        const $ = this.$, options = this.options, self = this;
        const $elem = this.$el.addClass('webgis-static-overlay-control'), map = options.map;

        const $valueElement = $("<input type='hidden' class='webgis-selected-id' />")
            .appendTo($elem);

        $elem.data('webgis-value-element', $valueElement);

        if (!map)
            return;

        const $list = $("<div>")
            .appendTo($elem);

        const serviceIds = map.serviceIds(), overlayServices = [];

        // collect overlay services
        $.each(serviceIds, function (i, serviceId) {
            const service = map.getService(serviceId);
            if (service.type === "static_overlay") {
                overlayServices.push(service);
            }
        });

        // order by zIndex
        overlayServices.sort(function (a, b) {
            if (a.getOrder() == b.getOrder())
                return 0;

            return (a.getOrder() < b.getOrder()) ? -1 : 1;
        }).reverse();

        // create service items
        $.each(overlayServices, function (i, service) {
            self._addService($list, service);
        });

        webgis.require('sortable', function () {
            if (self._destroyed)
                return;

            Sortable.create($list.get(0),
                {
                    animation: 150,
                    ghostClass: 'webgis-sorting',
                    onSort: function (e) {
                        const $items = $list.find('.webgis-static-overlay-control-item');
                        let zIndex = map.maxBackgroundCoveringServiceOrder() + $items.length + 1;
                        $items.each(function (i, item) {
                            $(item).data('service').setOrder(zIndex--);
                        });
                    }
                });
        });
    },
    destroy: function () {
        this.$el.find('*').off('.webgis_static_overlay_control');
    },
    methods: {
        val: function (value) {
            const $ = this.$, $elem = this.$el;

            if (typeof value === 'string') {
                $elem.data('webgis-value-element').val(value);

                let $serviceItem = null;
                $elem.find('.webgis-static-overlay-control-item').each(function (i, item) {
                    if ($(item).data('service').id === value) {
                        $serviceItem = $(item);
                    }
                });

                if ($serviceItem && !$serviceItem.hasClass('selected')) {
                    $serviceItem.trigger('click');
                }
            }

            return $elem.data('webgis-value-element').val();
        },
        _addService: function ($parent, service) {
            const $ = this.$, options = this.options;

            const $item = $("<div>")
                .addClass('webgis-static-overlay-control-item')
                .data('service', service)
                .appendTo($parent)
                .on('click.webgis_static_overlay_control', function (e) {
                    e.stopPropagation();

                    const $this = $(this);

                    const $valueControl = $this
                        .closest('.webgis-static-overlay-control')
                        .find('input.webgis-selected-id');

                    if ($this.hasClass('selected')) {
                        $this.removeClass('selected');
                        $this.data('service').hidePassPoints();
                        $valueControl.val('');
                    } else {
                        const selectedService = $this.parent().children('.selected').data('service');
                        $this.parent().children('.selected').removeClass('selected');
                        if (selectedService) {
                            selectedService.hidePassPoints();
                        }

                        $this.addClass('selected');

                        $valueControl.val($this.data('service').id);
                        $this.data('service').showPassPoints();
                    }
                });

            const $tools = $("<div>")
                .addClass('item-tools')
                .appendTo($item);

            $("<div>")
                .addClass('item-tool remove')
                .appendTo($tools)
                .on('click.webgis_static_overlay_control', function (e) {
                    e.stopPropagation();

                    const $item = $(this).closest('.webgis-static-overlay-control-item');
                    const service = $item.data('service');
                    service.map.removeServices([service.id]);
                    $item.remove();
                });

            $("<div>")
                .addClass('item-tool topmost' + (service.isTopMost() === true ? ' selected' : ''))
                .appendTo($tools)
                .on('click.webgis_static_overlay_control', function (e) {
                    e.stopPropagation();

                    const $this = $(this);
                    const $item = $this.closest('.webgis-static-overlay-control-item');
                    const service = $item.data('service');

                    if ($this.hasClass('selected')) {
                        service.resetOrder();
                        $this.removeClass('selected');
                    } else {
                        $item.closest('.webgis-static-overlay-control')
                            .find('.webgis-static-overlay-control-item > .item-tools > .item-tool.topmost')
                            .removeClass('selected');

                        service.bringToTop();
                        $this.addClass('selected');
                    }
                });

            $.each(options.command_buttons, function (i, button) {
                $("<div>")
                    .addClass('item-tool')
                    .data('button', button)
                    .css('background-image', 'url(' + webgis.css.imgResource(button.icon) + ')')
                    .appendTo($tools)
                    .on('click.webgis_static_overlay_control', function (e) {
                        e.stopPropagation();

                        const $item = $(this).closest('.webgis-static-overlay-control-item');
                        const service = $item.data('service');

                        const button = $(this).data('button');

                        const tool = service.map.getActiveTool();
                        const type = service.map.isServerButton(tool.id) ? 'servertoolcommand_ext' : 'servertoolcommand';
                        webgis.tools.onButtonClick(service.map, {
                            command: button.command, type: type, id: tool.id, map: service.map, argument: service.id
                        });
                    });
            });

            $("<div>")
                .addClass('item-title')
                .text(service.name)
                .appendTo($item);

            $("<div>")
                .addClass('service-opacity-control')
                .appendTo($item)
                .webgis_opacity_control({ service: service });
        }
    }
});
