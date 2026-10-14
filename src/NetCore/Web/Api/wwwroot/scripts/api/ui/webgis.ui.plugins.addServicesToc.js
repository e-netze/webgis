// WebGIS UI Plugins (add services toc)
// webgis_addServicesToc
// List of the (CMS) services that can be added to/removed from the map (incl. content search,
// service info and custom WMS/AGS services). Globals for legacy/inline callers:
// $.addServicesToc.checkItems/process, $._webgis_addServicesToc_handleLayerClick/handleQueryClick

webgis.ui.definePlugin('webgis_addServicesToc', {
    reinit: true,   // calling it again on the same element rebuilds the list (legacy behaviour)
    defaults: {
        map: null,
        searchTag: '',
        allowCustomServices: false
    },
    init: function () {
        const $ = this.$, me = this, o = this.options, Ctor = this.constructor;
        const elem = this.$el.get(0), $elem = this.$el, map = o.map;
        const allowAddServices = map.ui.allowAddServices() === true;

        elem._map = map;

        this._onServicesChanged = function () { Ctor.checkItems(); };
        map.events.on('onaddservice', this._onServicesChanged, elem);
        map.events.on('onremoveservice', this._onServicesChanged, elem);

        $elem.addClass('webgis-addservices_toc-holder');
        $elem.html("<img src='" + webgis.css.imgResource('loader1.gif', 'hourglass') + "' />...");

        $elem.empty();

        const searchPlaceholder = 'In Diensten suchen...';

        const $searchHolder = this._$searchHolder = $("<div></div>")
            .addClass('webgis-addservices_toc-search-holder')
            .appendTo($elem)
            .on('click.webgis_add_services_toc', function () {
                let serviceIds = '';
                const $this = $(this);

                if ($this.hasClass('initialized') || $this.hasClass('loading'))
                    return;

                $this.addClass('loading');

                $this
                    .closest('.webgis-addservices_toc-holder')
                    .find('.webgis-addservices_toc-item')
                    .each(function (i, item) {
                        const $item = $(item);
                        if (!$item.hasClass('initialized') && !$item.hasClass('loading')) {
                            $item.addClass('loading')

                            if (serviceIds !== '') serviceIds += ',';
                            serviceIds += $(item).attr('data-serviceid');
                        }
                    });

                Ctor.loadServiceInfo($elem, serviceIds, function () {
                    $this.removeClass('loading').addClass('initialized');

                    $this.empty();
                    $this.webgis_contentsearch({
                        searchTag: o.searchTag,
                        placeholder: searchPlaceholder,
                        displayMachted: 'inline-block',
                        container_selectors: ["li", "ul", ".webgis-addservices_toc-content"],
                        onMatch: function (sender, $item, val) {
                            const $text = $item.children('.webgis-addservices_toc-item-text');

                            const title = $text.children('.title').html();
                            const description = $text.children('.description').html();
                            const elements = $text.children('.elements').html();

                            const $info = $item.children('.webgis-addservices_toc-item-info')
                                .empty();

                            const vals = val.split(' ');
                            let $elements;
                            if (title)
                                $("<h3>").addClass('highlightable').html(title).appendTo($info);
                            if (description)
                                $("<p>").addClass('highlightable').html(description).appendTo($info);
                            if (elements) {
                                $elements = $("<p>")
                                    .html(elements/*.highlightText(vals)*/)
                                    .appendTo($info)
                            }
                            Ctor.highlightElements($info, vals);
                            if ($elements)
                                Ctor.removeUnhighlightedItems($elements);
                        }
                    });
                    $this.find('input').addClass('webgis-input').focus();
                });
            });

        $("<input placeholder='" + searchPlaceholder + "' readonly='readonly' >")
            .addClass('webgis-input')
            .appendTo($searchHolder);

        this._$ul = $("<ul id='webgis-addservices_toc-list'></ul>").appendTo($elem);

        if (allowAddServices === true) {
            webgis.serviceInfos(function (services, copyright) {
                if (me._destroyed)
                    return;
                me._buildServicesUi(services, copyright, allowAddServices);
            }, true);
        } else {
            const mapServices = [];

            const serviceIds = map.serviceIds();
            for (const s in serviceIds) {
                const service = map.getService(serviceIds[s]);
                if (service) {
                    mapServices.push({
                        id: service.id,
                        name: service.serviceInfo.name,
                        type: service.serviceInfo.type,
                        container: map.ui.getMapTitle()
                    });
                }
            }

            this._buildServicesUi(mapServices, undefined, allowAddServices);
        }

        if (o.allowCustomServices === true) {
            this._buildCustomServicesUi();
        }
    },
    destroy: function () {
        const map = this.options.map;
        if (map && map.events && this._onServicesChanged) {
            map.events.off('onaddservice', this._onServicesChanged);
            map.events.off('onremoveservice', this._onServicesChanged);
        }
        this._onServicesChanged = null;

        this.$el.find('*').addBack().off('.webgis_add_services_toc');
        this.$el.empty().removeClass('webgis-addservices_toc-holder');

        const elem = this.$el.get(0);
        if (elem) {
            elem._map = null;
        }
        this._$ul = this._$searchHolder = null;
    },
    methods: {
        _buildServicesUi: function (services, copyright, allowAddServices) {
            const $ = this.$, o = this.options, Ctor = this.constructor;
            const $ul = this._$ul;

            if (copyright && o.map)
                o.map.addCopyright(copyright);
            if (!services)
                return;

            for (let i = 0; i < services.length; i++) {
                const service = services[i];
                if (service.supportedCrs && o.map && $.inArray(o.map.crsId(), service.supportedCrs) < 0) {
                    continue;
                }

                const isCollection = service.type === 'collection';

                let $li = null, $item_ul = null;
                $ul.find('.webgis-addservices_toc-title-text').each(function (i, obj) {
                    if (obj.innerHTML === service.container) {
                        $li = $(obj.parentNode);
                    }
                });
                if ($li == null) {
                    $li = $("<li>")
                        .addClass('webgis-addservices_toc-title webgis-addservices_toc-collapsable')
                        .appendTo($ul);

                    $("<span style='position:absolute' class='webgis-addservices_toc-plus webgis-api-icon webgis-api-icon-triangle-1-s'></span>").appendTo($li);
                    $("<div class='webgis-addservices_toc-title-text'>" + service.container + "</div>").appendTo($li);
                    const $div = $("<div class='webgis-addservices_toc-content tiles' style='display:none' id='div_'></div>");
                    $div.appendTo($li);
                    $item_ul = $("<ul style='margin:8px 0px 0px 0px'></ul>");
                    $item_ul.appendTo($div);

                    $li.on('click.webgis_add_services_toc', Ctor.containerClickEvent);
                }
                else {
                    $item_ul = $li.find('.webgis-addservices_toc-content').children('ul');
                }
                const $item_li = $("<li class='webgis-addservices_toc-item'></li>")
                    .addClass(service.type)
                    .addClass('webgis-search-content active')
                    .appendTo($item_ul);

                $item_li.addClass('webgis-addservices_toc-checkable');

                $("<div>")
                    .addClass("webgis-addservices_toc-item-image")
                    .appendTo($item_li);

                if (allowAddServices) {
                    $("<img class='' src=" + webgis.css.imgResource("check0.png", "toc") + ">")
                        .addClass('webgis-addservices_toc-checkable-icon')
                        .appendTo($item_li);
                }

                $("<div>")
                    .addClass("webgis-addservices_toc-item-info")
                    .appendTo($item_li);

                if (!isCollection) {
                    $("<div>")
                        .addClass('webgis-addservices_toc-info')
                        .appendTo($item_li)
                        .on('click.webgis_add_services_toc', function (e) {
                            e.stopPropagation();
                            Ctor.showInfo(this);
                        });
                }

                const $text = $("<div></div>")
                    .addClass('webgis-addservices_toc-item-text')
                    .appendTo($item_li);

                $("<div class='more'>")
                    .text('mehr...')
                    .appendTo($text)
                    .on('click.webgis_add_services_toc', function (e) {
                        e.stopPropagation();
                        Ctor.showInfo(this);
                    });

                $("<div>")
                    .addClass('title')
                    .text(service.name)
                    .appendTo($text);

                if (isCollection) {
                    $item_li
                        .addClass('collection')
                        .attr('data-serviceids', service.childServices ? service.childServices.toString() : '');
                } else {
                    $item_li.attr('data-serviceid', service.id);
                }

                if (allowAddServices) {
                    $item_li.on('click.webgis_add_services_toc', function (e) {
                        e.stopPropagation();

                        const $this = $(this);

                        if ($this.hasClass('collection')) {
                            const isPartlySelected = $this.hasClass('partly-selected');
                            for (const serviceId of $this
                                .closest('.webgis-addservices_toc-item')
                                .attr('data-serviceids')
                                .split(',')) {
                                Ctor.process(
                                    this,
                                    serviceId,
                                    isPartlySelected === true);  // if partly-selected: addIfNotExits, let existing in map
                            }
                        } else {
                            Ctor.process(
                                this,
                                $this.closest('.webgis-addservices_toc-item')
                                    .attr('data-serviceid'));
                        }

                        if ($.fn.webgis_contentsearch) {
                            $('.webgis-content-search-holder').webgis_contentsearch('trigger', {});
                        }
                    });
                }

                const $elem = this.$el;
                $item_li.on('mouseenter.webgis_add_services_toc', function () {
                    const $item = $(this);
                    if ($item.hasClass('initialized') || $item.hasClass('loading')) {
                        return;
                    }

                    $item.addClass('loading');
                    Ctor.loadServiceInfo($elem, $item.attr('data-serviceid'));
                })
            }
            Ctor.checkItems();

            if (o.searchTag) {
                this._$searchHolder.trigger('click');
            };
        },
        _buildCustomServicesUi: function () {
            const $ = this.$, map = this.options.map, Ctor = this.constructor;

            const $li = $("<li class='webgis-addservices_toc-title webgis-addservices_toc-collapsable'></li>")
                .appendTo(this._$ul)
                .on('click.webgis_add_services_toc', Ctor.containerClickEvent);
            $("<span style='position:absolute' class='webgis-addservices_toc-plus webgis-api-icon webgis-api-icon-triangle-1-s'></span>").appendTo($li);
            $("<div class='webgis-addservices_toc-title-text'>Benutzerdefinierte Dienste</div>").appendTo($li);
            const $div = $("<div class='webgis-addservices_toc-content' style='display:none' id='div_'></div>")
                .css('padding', '10px')
                .appendTo($li)
                .on('click.webgis_add_services_toc', function (event) { event.stopPropagation() });

            $("<div>")
                .addClass('webgis-label-description show')
                .html(webgis.encodeUntrustedHtml(webgis.l10n.get('description-customservice'), true))
                .appendTo($div);

            $div.webgis_general_input({
                name: 'webgis-customservice-url',
                label: 'Dienst Url',
                placeholder: 'Url zum WMS/AGS Dienst...',
                description: webgis.l10n.get('description-customservice-url'),
                onInit: function ($parent, $input) {
                    $input.css({ width: '100%', boxSizing: 'border-box' });
                }
            });

            $div.webgis_general_input('add', {
                name: 'webgis-customservice-displayname',
                label: 'Anzeigename (optional)',
                placeholder: 'Anzeigename des Dienstes...',
                description: webgis.l10n.get('description-customservice-displayname'),
            });

            $div.webgis_general_input('add', {
                name: 'webgis-customservice-user',
                label: 'Security (optional)',
                placeholder: 'Username...',
                description: webgis.l10n.get('description-customservice-security'),
                onInit: function ($parent, $input) {
                    $("<input name='webgis-customservice-pwd' type='password'>")
                        .attr('placeholder', 'Passwort')
                        .addClass('webgis-input')
                        .appendTo($parent);
                }
            });

            let $successMessage, $errorMessage;

            $("<br/><br/>").appendTo($div);
            $("<button>Dienst hinzuf&uuml;gen</button>")
                .addClass('webgis-button uibutton')
                .appendTo($div)
                .on('click.webgis_add_services_toc', function () {
                    const $button = $(this), $div = $button.parent();
                    $button.prop('disabled', true);
                    $successMessage.css('display', 'none').empty();
                    $errorMessage.css('display', 'none').empty();

                    const data = {
                        url: $div.webgis_general_input('val', { name: 'webgis-customservice-url' }),
                        displayname: $div.webgis_general_input('val', { name: 'webgis-customservice-displayname' }),
                        user: $div.webgis_general_input('val', { name: 'webgis-customservice-user' }),
                        password: $div.webgis_general_input('val', { name: 'webgis-customservice-pwd' })
                    };

                    Ctor.loadCustomServiceInfo(data,
                        function (result) {
                            $button.prop('disabled', false);

                            if (result.id && result.name) {
                                map.addServices([result]);
                                map.refresh();

                                $div.webgis_general_input('clear', { name: 'webgis-customservice-url' });
                                $div.webgis_general_input('clear', { name: 'webgis-customservice-displayname' });
                                $div.webgis_general_input('clear', { name: 'webgis-customservice-user' });
                                $div.webgis_general_input('clear', { name: 'webgis-customservice-pwd' });

                                webgis.log('service added to map:', result.name);
                                $successMessage
                                    .html('Dienst ' + result.name + ' wurde erfolgreich der Karte hinzugefügt')
                                    .css('display', 'block');
                            } else {
                                if (result.exception || result.error) {
                                    $errorMessage.text("Error: " + (result.exception || result.error));
                                } else {
                                    $errorMessage.text("Ein unbekannter Fehler ist aufgetreten");
                                }
                                $errorMessage.css('display', 'block');
                            }
                        },
                        function (err) {
                            $button.prop('disabled', false);
                            webgis.alert('Unknown Error. Statuscode:' + err.status, "error");
                        }
                    );
                });

            $("<br/><br/>").appendTo($div);
            $successMessage = $("<div>").addClass('webgis-info').css('display', 'none').appendTo($div);
            $errorMessage = $("<div>").addClass('webgis-info error').css('display', 'none').appendTo($div);
        }
    },
    staticMethods: {
        // static methods don't rely on "this": checkItems/process/handle*Click are also called via
        // the legacy globals and as event handlers (this = context element)
        checkItems: function () {
            const $ = webgis.$ || jQuery;
            const $elem = $('.webgis-addservices_toc-holder');
            if ($elem.length === 0)
                return;
            const map = $elem.get(0)._map;
            if (map === null)
                return;

            const serviceIds = map.serviceIds();
            // Find all items and check visibility
            $elem.find('.webgis-addservices_toc-item').each(function (i, e) {
                const $e = $(e);

                if ($e.hasClass('collection')) {
                    const dataServiceIds = $e.attr('data-serviceids')
                        ? $e.attr('data-serviceids').split(',')
                        : [];

                    const allContained = dataServiceIds.length > 0 && dataServiceIds.every(id => serviceIds.includes(id));
                    const someContained = dataServiceIds.some(id => serviceIds.includes(id));

                    webgis.log('dataServiceIds', dataServiceIds, allContained, someContained);

                    if (allContained) {
                        $e.addClass('selected').removeClass('partly-selected')
                            .find('.webgis-addservices_toc-checkable-icon')
                            .attr('src', webgis.css.imgResource('check1.png', 'toc'));
                    } else if (someContained) {
                        $e.addClass('partly-selected').removeClass('selected')
                            .find('.webgis-addservices_toc-checkable-icon')
                            .attr('src', webgis.css.imgResource('check1.png', 'toc'));
                    } else {
                        $e.removeClass('selected partly-selected')
                            .find('.webgis-addservices_toc-checkable-icon')
                            .attr('src', webgis.css.imgResource('check0.png', 'toc'));
                    }
                }
                else {
                    if ($.inArray($e.attr('data-serviceid'), serviceIds) >= 0) {
                        $e.addClass('selected')
                            .find('.webgis-addservices_toc-checkable-icon')
                            .attr('src', webgis.css.imgResource('check1.png', 'toc'));
                    }
                    else {
                        $e.removeClass('selected')
                            .find('.webgis-addservices_toc-checkable-icon')
                            .attr('src', webgis.css.imgResource('check0.png', 'toc'));
                    }
                }
            });
        },
        process: function (item, serviceId, addIfNotInMap) {
            const $ = webgis.$ || jQuery;
            const $elem = $('.webgis-addservices_toc-holder');
            if ($elem.length === 0) {
                return;
            }

            const map = $elem.get(0)._map;
            if (map == null) {
                return;
            }
            const serviceIds = map.serviceIds();

            if ($.inArray(serviceId, serviceIds) < 0) {
                $(item).find('.webgis-addservices_toc-checkable-icon')
                    .attr('src', webgis.css.imgResource('loader1.gif', 'hourglass'));
                const serviceInfo = webgis.serviceInfo(serviceId);

                if (serviceInfo && serviceInfo.services) {
                    for (const s in serviceInfo.services) {
                        const service = serviceInfo.services[s];
                        service.order = service.order || map.maxServiceInsertOrder(service);
                        webgis.log('service.order', service.order);
                    }

                    const addedServices = map.addServices(serviceInfo.services);
                    map.recalcServiceOrder();

                    for (let i = 0; i < addedServices.length; i++) {
                        addedServices[i].refresh();
                    }
                }
            }
            else if (addIfNotInMap !== true) {
                map.removeServices([serviceId]);
            }
        },
        containerClickEvent: function (event) {
            const $ = webgis.$ || jQuery;
            event.stopPropagation();
            const $li = $(this);

            if ($li.hasClass('webgis-expanded')) {
                $li.removeClass('webgis-expanded');
                $li.children('.webgis-addservices_toc-content').slideUp();
                $li.children('.webgis-addservices_toc-plus')
                    .removeClass('webgis-api-icon-triangle-1-e')
                    .addClass('webgis-api-icon-triangle-1-s');
            }
            else {
                $li.closest('ul')
                    .find('.webgis-expanded')
                    .removeClass('webgis-expanded')
                    .find('.webgis-addservices_toc-content').slideUp();

                $li.closest('ul')
                    .find('.webgis-addservices_toc-plus')
                    .removeClass('webgis-api-icon-triangle-1-e')
                    .addClass('webgis-api-icon-triangle-1-s');

                $li.addClass('webgis-expanded');
                $li.children('.webgis-addservices_toc-content').slideDown();
                $li.children('.webgis-addservices_toc-plus')
                    .removeClass('webgis-api-icon-triangle-1-s')
                    .addClass('webgis-api-icon-triangle-1-e');
            }
        },
        showInfo: function (sender) {
            const $ = webgis.$ || jQuery;
            const $item = $(sender).closest('.webgis-addservices_toc-item');
            const $search = $item.closest('.webgis-addservices_toc-holder').find('.webgis-addservices_toc-search-holder').find('.webgis-input');
            if ($item.hasClass('initialized')) {
                const $text = $item.find('.webgis-addservices_toc-item-text');

                webgis.modalDialog(
                    $text.children('.title').text(),
                    function ($content) {
                        $content.addClass('webgis-service-info')
                        $("<div>")
                            .addClass('webgis-service-description')
                            .html($text.children('.description').html().highlightText(($search.val() || '').split(' ')))
                            .appendTo($content);
                        $("<div>")
                            .addClass('webgis-service-elements')
                            .html($text.children('.elements').html().highlightText(($search.val() || '').split(' ')))
                            .appendTo($content);
                    },
                    function () { }
                );
            }
        },
        setServiceItemDescription: function ($item, service) {
            const $ = webgis.$ || jQuery;
            const $text = $item
                .removeClass('loading')
                .addClass('initialized')
                .find('.webgis-addservices_toc-item-text');

            $("<div>")
                .addClass('description')
                .text(service.servicedescription)
                .appendTo($text);

            const $serviceElements = $("<div>").addClass('elements').appendTo($text);
            const $holder = $item.closest('.webgis-addservices_toc-holder');
            const map = $holder && $holder.length === 1 ? $holder.get(0)._map : null;

            if (map) {
                // if service is already in map, use this to show the current visibility, etc...
                const originalService = map.getService(service.id);
                if (originalService && originalService.serviceInfo) {
                    service = originalService.serviceInfo;
                }
            }

            if (service.presentations) {
                const $p = $("<div>").appendTo($serviceElements)
                $("<h3>Darstellungsvarianten</h3>").appendTo($p);
                const $ul = $("<ul>").appendTo($p);
                for (const p in service.presentations) {
                    const presentation = service.presentations[p];
                    if (presentation.layers && presentation.layers.length > 0) {
                        const presentationItem = $("<li>").appendTo($ul);
                        $("<div>").addClass('highlightable title').text(presentation.name).appendTo(presentationItem);
                        $("<div>").addClass('highlightable description').text(presentation.description).appendTo(presentationItem);
                    }
                }
            }

            if (service.queries) {
                const $p = $("<div>").appendTo($serviceElements)
                $("<h3>Abfragen</h3>").appendTo($p);
                const $ul = $("<ul>").appendTo($p);
                for (const q in service.queries) {
                    const query = service.queries[q];
                    const $queryItem = $("<li>")
                        .addClass('webgis-addservices-servicequery' + (query.items && query.items.length > 0 ? ' queryable' : ''))
                        .attr('data-queryid', query.id)
                        .attr('onclick', '$._webgis_addServicesToc_handleQueryClick(this, event);return false;')
                        .appendTo($ul);
                    $("<div>").addClass('highlightable title').text(query.name).appendTo($queryItem);
                    $("<div>").addClass('highlightable description').text(query.description).appendTo($queryItem);
                }
            }

            if (service.layers) {
                const $p = $("<div>").appendTo($serviceElements)
                $("<h3>Layer</h3>").appendTo($p);
                const $ul = $("<ul>").appendTo($p);
                for (const l in service.layers) {
                    const layer = service.layers[l];
                    const $layeritem = $("<li>")
                        .addClass('webgis-addservices-servicelayer' + (layer.visible ? ' visible' : ''))
                        .attr('data-layerid', layer.id)
                        .attr('onclick', '$._webgis_addServicesToc_handleLayerClick(this, event);return false;')
                        .appendTo($ul);
                    $("<div>").addClass('highlightable title').text(layer.name).appendTo($layeritem);
                    $("<div>").addClass('highlightable description').text(layer.description).appendTo($layeritem);
                }
            }
        },
        loadServiceInfo: function ($parent, serviceIds, callback) {
            const Ctor = (webgis.$ || jQuery).fn.webgis_addServicesToc.plugin;
            webgis.ajax({
                type: 'post',
                data: webgis.hmac.appendHMACData({ ids: serviceIds, f: 'json' }),
                url: webgis.baseUrl + '/rest/serviceinfo',
                success: function (result) {
                    for (const s in result.services) {
                        const service = result.services[s];

                        const $item = $parent.find(".webgis-addservices_toc-item[data-serviceid='" + service.id + "']");
                        Ctor.setServiceItemDescription($item, service);
                    }

                    if (callback) {
                        callback(result);
                    }
                }
            });
        },
        loadCustomServiceInfo: function (data, callback, errorCallback) {
            data.f = 'json';
            webgis.ajax({
                type: 'post',
                data: webgis.hmac.appendHMACData(data),
                url: webgis.baseUrl + '/rest/customserviceinfo',
                success: function (result) {
                    if (callback) {
                        callback(result);
                    }
                },
                error: errorCallback
            });
        },
        removeUnhighlightedItems: function ($element) {
            const $ = webgis.$ || jQuery;
            const Ctor = $.fn.webgis_addServicesToc.plugin;
            if ($element.hasClass('webgis-highlight-text') || $element.prop('tagName') === 'H3')
                return;

            if ($element.find('.webgis-highlight-text').length === 0) {
                $element.remove();
            } else {
                $element.children().each(function (i, e) {
                    Ctor.removeUnhighlightedItems($(e));
                });
            }
        },
        highlightElements: function ($parent, vals) {
            const $ = webgis.$ || jQuery;
            $parent.find('.highlightable').each(function (i, e) {
                $(e).html($(e).text().highlightText(vals));
            });
        },
        handleLayerClick: function (sender, evt) {
            const $ = webgis.$ || jQuery;
            evt.stopPropagation();

            const $this = $(sender), $item = $this.closest('.webgis-addservices_toc-item'), $holder = $item.closest('.webgis-addservices_toc-holder');

            const map = $holder.get(0)._map;
            const serviceId = $item.attr('data-serviceid'),
                layerId = $this.attr('data-layerid');

            const service = map.getService(serviceId);
            if (service) {
                const visible = !$this.hasClass('visible');
                service.setLayerVisibility([layerId], visible);

                if (visible) {
                    $this.addClass('visible');
                } else {
                    $this.removeClass('visible');
                }

                map.events.fire('refresh-toc-checkboxes');
            }
        },
        handleQueryClick: function (sender, evt) {
            const $ = webgis.$ || jQuery;
            evt.stopPropagation();

            const $this = $(sender), $item = $this.closest('.webgis-addservices_toc-item'), $holder = $item.closest('.webgis-addservices_toc-holder');

            const map = $holder.get(0)._map;
            const serviceId = $item.attr('data-serviceid'),
                queryId = $this.attr('data-queryid');

            const $queryCombo = $(map._webgisContainer).find('.webgis-detail-search-combo');

            const comboValue = serviceId + '/queries/' + queryId;
            if ($queryCombo.find("option[value='" + comboValue + "']").length > 0) {
                $queryCombo.val(comboValue);
                $queryCombo.trigger('change');

                if ($queryCombo.closest('.webgis-detail-search-holder').css('display') === 'none') {
                    $(map._webgisContainer).find('#webgis-detail-search-button').trigger('click');
                }
            }

            const $modal = $this.closest('.webgis-modal');

            if ($modal.length === 1) {
                $modal.webgis_modalDialog('close');
            }
        }
    }
});

// legacy globals: used from inline onclick attributes ($._webgis_addServicesToc_handle*Click)
// and possibly from custom code ($.addServicesToc)
(function ($) {
    "use strict";
    const plugin = $.fn.webgis_addServicesToc.plugin;

    $.addServicesToc = {
        checkItems: function (e) { return plugin.checkItems(e); },
        process: function (item, serviceId, addIfNotInMap) { return plugin.process(item, serviceId, addIfNotInMap); }
    };

    $._webgis_addServicesToc_handleLayerClick = function (sender, evt) { return plugin.handleLayerClick(sender, evt); };
    $._webgis_addServicesToc_handleQueryClick = function (sender, evt) { return plugin.handleQueryClick(sender, evt); };
})(webgis.$ || jQuery);
