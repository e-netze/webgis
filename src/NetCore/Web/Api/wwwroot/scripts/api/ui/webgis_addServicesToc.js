(function ($) {
    "use strict";
    $.fn.webgis_addServicesToc = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_addServicesToc');
        }
    };
    var defaults = {
        map: null,
        searchTag: '',
        allowCustomServices: false
    };
    var methods = {
        init: function (options) {
            var $this = $(this);
            options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };
    $.addServicesToc = {
        checkItems: function (e) {
            var $elem = $('.webgis-addservices_toc-holder');
            if ($elem.length === 0)
                return;
            var map = $elem.get(0)._map;
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

                    console.log('dataServiceIds', dataServiceIds, allContained, someContained);

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
            var $elem = $('.webgis-addservices_toc-holder');
            if ($elem.length === 0) {
                return;
            }

            var map = $elem.get(0)._map;
            if (map == null) {
                return;
            }
            var serviceIds = map.serviceIds();

            if ($.inArray(serviceId, serviceIds) < 0) {
                $(item).find('.webgis-addservices_toc-checkable-icon')
                    .attr('src', webgis.css.imgResource('loader1.gif', 'hourglass'));
                var serviceInfo = webgis.serviceInfo(serviceId);

                //console.log('serviceInfo', serviceInfo);

                if (serviceInfo && serviceInfo.services) {
                    for (var s in serviceInfo.services) {
                        var service = serviceInfo.services[s];
                        service.order = service.order || map.maxServiceInsertOrder(service);
                        console.log('service.order', service.order);
                    }

                    var addedServices = map.addServices(serviceInfo.services);
                    map.recalcServiceOrder();

                    for (var i = 0; i < addedServices.length; i++) {
                        addedServices[i].refresh();
                    }
                }
            }
            else if(addIfNotInMap !== true) {
                map.removeServices([serviceId]);
            }
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem), map = options.map, allowAddServices = map.ui.allowAddServices() === true;
        elem._map = map;
        options.map.events.on('onaddservice', $.addServicesToc.checkItems, elem);
        options.map.events.on('onremoveservice', $.addServicesToc.checkItems, elem);
        $elem.addClass('webgis-addservices_toc-holder');
        $elem.html("<img src='" + webgis.css.imgResource('loader1.gif', 'hourglass') + "' />...");

        $(elem).empty();

        var searchPlaceholder = 'In Diensten suchen...';

        var $searchHolder = $("<div></div>")
            .addClass('webgis-addservices_toc-search-holder')
            .appendTo($elem)
            .click(function () {
                var serviceIds = '', $this = $(this);

                if ($this.hasClass('initialized') || $this.hasClass('loading'))
                    return;

                $this.addClass('loading');

                $this
                    .closest('.webgis-addservices_toc-holder')
                    .find('.webgis-addservices_toc-item')
                    .each(function (i, item) {
                        var $item = $(item);
                        if (!$item.hasClass('initialized') && !$item.hasClass('loading')) {
                            $item.addClass('loading')

                            if (serviceIds !== '') serviceIds += ',';
                            serviceIds += $(item).attr('data-serviceid');
                        }
                    });

                loadServiceInfo($elem, serviceIds, function () {
                    $this.removeClass('loading').addClass('initialized');

                    $this.empty();
                    $this.webgis_contentsearch({
                        searchTag: options.searchTag,
                        placeholder: searchPlaceholder,
                        displayMachted: 'inline-block',
                        container_selectors: ["li", "ul", ".webgis-addservices_toc-content"],
                        onMatch: function (sender, $item, val) {
                            var $text = $item.children('.webgis-addservices_toc-item-text');

                            var title = $text.children('.title').html();
                            var description = $text.children('.description').html();
                            var elements = $text.children('.elements').html();

                            var $info = $item.children('.webgis-addservices_toc-item-info')
                                .empty();

                            var vals = val.split(' '), $elements;
                            if (title)
                                $("<h3>").addClass('highlightable').html(title).appendTo($info);
                            if (description)
                                $("<p>").addClass('highlightable').html(description).appendTo($info);
                            if (elements) {
                                $elements = $("<p>")
                                    .html(elements/*.highlightText(vals)*/)
                                    .appendTo($info)
                            }
                            highlightElements($info, vals);
                            if ($elements)
                                removeUnhighlightedItems($elements);
                        }
                    });
                    $this.find('input').addClass('webgis-input').focus();
                });
            });

        $("<input placeholder='" + searchPlaceholder + "' readonly='readonly' >")
            .addClass('webgis-input')
            .appendTo($searchHolder);

        var $ul = $("<ul id='webgis-addservices_toc-list'></ul>").appendTo($elem);

        var containerClickEvent = function (event) {
            event.stopPropagation();
            var $li = $(this);

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
        }

        var buildServicesUi = function (services, copyright) {    
            if (copyright && options.map)
                options.map.addCopyright(copyright);
            if (!services)
                return;

            for (var i = 0; i < services.length; i++) {
                var service = services[i];
                if (service.supportedCrs && options.map && $.inArray(options.map.crsId(), service.supportedCrs) < 0) {
                    //alert(service.supportedCrs + " " + options.map.crsId);
                    continue;
                }

                const isCollection = service.type === 'collection';

                var $li = null, $item_ul = null;
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
                    var $div = $("<div class='webgis-addservices_toc-content tiles' style='display:none' id='div_'></div>");
                    $div.appendTo($li);
                    $item_ul = $("<ul style='margin:8px 0px 0px 0px'></ul>");
                    $item_ul.appendTo($div);

                    $li.click(containerClickEvent);
                }
                else {
                    $item_ul = $li.find('.webgis-addservices_toc-content').children('ul');
                }
                var $item_li = $("<li class='webgis-addservices_toc-item'></li>")
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
                        //click(function (e) {
                        //    e.stopPropagation();
                        //    $(this).parent().children('.webgis-addservices_toc-item-image').trigger('click');
                        //});
                }

                $("<div>")
                    .addClass("webgis-addservices_toc-item-info")
                    .appendTo($item_li);

                function showInfo(sender) {
                    var $item = $(sender).closest('.webgis-addservices_toc-item');
                    var $search = $item.closest('.webgis-addservices_toc-holder').find('.webgis-addservices_toc-search-holder').find('.webgis-input');
                    if ($item.hasClass('initialized')) {
                        var $text = $item.find('.webgis-addservices_toc-item-text');

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
                };

                if (!isCollection) {
                    $("<div>")
                        .addClass('webgis-addservices_toc-info')
                        .appendTo($item_li)
                        .click(function (e) {
                            e.stopPropagation();
                            showInfo(this);
                        });
                }

                var $text = $("<div></div>")
                    .addClass('webgis-addservices_toc-item-text')
                    .appendTo($item_li);

                $("<div class='more'>")
                    .text('mehr...')
                    .appendTo($text)
                    .click(function (e) {
                        e.stopPropagation();
                        showInfo(this);
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
                    $item_li.click(function (e) {
                        e.stopPropagation();

                        const $this = $(this);

                        if ($this.hasClass('collection')) {
                            const isPartlySelected = $this.hasClass('partly-selected');
                            for (const serviceId of $this
                                .closest('.webgis-addservices_toc-item')
                                .attr('data-serviceids')
                                .split(',')) {
                                    $.addServicesToc.process(
                                        this,
                                        serviceId,
                                        isPartlySelected === true);  // if partly-selected: addIfNotExits, let existing in map
                                    }
                        } else {
                            $.addServicesToc.process(
                                this,
                                $this.closest('.webgis-addservices_toc-item')
                                     .attr('data-serviceid'));
                        }

                        if ($.fn.webgis_contentsearch) {
                            $('.webgis-content-search-holder').webgis_contentsearch('trigger', {});
                        }
                    });
                }

                $item_li.mouseenter(function () {
                    var $item = $(this);
                    if ($item.hasClass('initialized') || $item.hasClass('loading')) {
                        return;
                    }

                    $item.addClass('loading');
                    loadServiceInfo($elem, $item.attr('data-serviceid'));
                })
            }
            $.addServicesToc.checkItems();

            if (options.searchTag) {
                $searchHolder.trigger('click');
            };
        };

        if (allowAddServices === true) {
            webgis.serviceInfos(buildServicesUi, true);
        } else {
            var mapServices = [];

            var serviceIds = map.serviceIds();
            for (var s in serviceIds) {
                var service = map.getService(serviceIds[s]);
                if (service) {
                    mapServices.push({
                        id: service.id,
                        name: service.serviceInfo.name,
                        type: service.serviceInfo.type,
                        container: map.ui.getMapTitle()
                    });
                }
            }

            buildServicesUi(mapServices);
        }

        if (options.allowCustomServices === true) {
            var $li = $("<li class='webgis-addservices_toc-title webgis-addservices_toc-collapsable'></li>")
                .appendTo($ul)
                .click(containerClickEvent);
            $("<span style='position:absolute' class='webgis-addservices_toc-plus webgis-api-icon webgis-api-icon-triangle-1-s'></span>").appendTo($li);
            $("<div class='webgis-addservices_toc-title-text'>Benutzerdefinierte Dienste</div>").appendTo($li);
            var $div = $("<div class='webgis-addservices_toc-content' style='display:none' id='div_'></div>")
                .css('padding','10px')
                .appendTo($li)
                .click(function (event) { event.stopPropagation() });

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

            $div.webgis_general_input({
                name: 'webgis-customservice-displayname',
                label: 'Anzeigename (optional)',
                placeholder: 'Anzeigename des Dienstes...',
                description: webgis.l10n.get('description-customservice-displayname'),
            });

            $div.webgis_general_input({
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

            var $successMessage, $errorMessage;

            $("<br/><br/>").appendTo($div);
            $("<button>Dienst hinzuf&uuml;gen</button>")
                .addClass('webgis-button uibutton')
                .appendTo($div)
                .click(function(){
                    var $button = $(this), $div = $button.parent();
                    $button.prop('disabled', true);
                    $successMessage.css('display', 'none').empty();
                    $errorMessage.css('display', 'none').empty();

                    var data = {
                        url: $div.webgis_general_input('val', { name: 'webgis-customservice-url' }),
                        displayname: $div.webgis_general_input('val', { name: 'webgis-customservice-displayname' }),
                        user: $div.webgis_general_input('val', { name: 'webgis-customservice-user' }),
                        password: $div.webgis_general_input('val', { name: 'webgis-customservice-pwd' })
                    };

                    loadCustomServiceInfo(data,
                        function (result) {
                            $button.prop('disabled', false);

                            if (result.id && result.name) {
                                map.addServices([result]);
                                map.refresh();

                                $div.webgis_general_input('clear', { name: 'webgis-customservice-url' });
                                $div.webgis_general_input('clear', { name: 'webgis-customservice-displayname' });
                                $div.webgis_general_input('clear', { name: 'webgis-customservice-user' });
                                $div.webgis_general_input('clear', { name: 'webgis-customservice-pwd' });

                                console.log('Dienst ' + result.name + ' wurde erfolgreich der Karte hinzugefügt');
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
            $successMessage = $("<div>").addClass('webgis-info').css('display','none').appendTo($div);
            $errorMessage = $("<div>").addClass('webgis-info error').css('display', 'none').appendTo($div);
        }
    };

    var setServiceItemDescription = function ($item, service) {
        var $text = $item
            .removeClass('loading')
            .addClass('initialized')
            .find('.webgis-addservices_toc-item-text');

        $("<div>")
            .addClass('description')
            .text(service.servicedescription)
            .appendTo($text);

        var $serviceElements = $("<div>").addClass('elements').appendTo($text);
        var $holder = $item.closest('.webgis-addservices_toc-holder');
        var map = $holder && $holder.length === 1 ? $holder.get(0)._map : null;

        if (map) {
            // if service is already in map, use this to show the current visibility, etc...
            var originalService = map.getService(service.id);
            if (originalService && originalService.serviceInfo) {
                service = originalService.serviceInfo;
            }
        }

        if (service.presentations) {
            var $p = $("<div>").appendTo($serviceElements)
            $("<h3>Darstellungsvarianten</h3>").appendTo($p);
            var $ul = $("<ul>").appendTo($p);
            for (var p in service.presentations) {
                var presentation = service.presentations[p];
                if (presentation.layers && presentation.layers.length > 0) {
                    var presentationItem = $("<li>").appendTo($ul);
                    $("<div>").addClass('highlightable title').text(presentation.name).appendTo(presentationItem);
                    $("<div>").addClass('highlightable description').text(presentation.description).appendTo(presentationItem);
                }
            }
        }

        if (service.queries) {
            var $p = $("<div>").appendTo($serviceElements)
            $("<h3>Abfragen</h3>").appendTo($p);
            var $ul = $("<ul>").appendTo($p);
            for (var q in service.queries) {
                var query = service.queries[q];
                var $queryItem = $("<li>")
                    .addClass('webgis-addservices-servicequery' + (query.items && query.items.length > 0 ? ' queryable' : ''))
                    .attr('data-queryid', query.id)
                    .attr('onclick', '$._webgis_addServicesToc_handleQueryClick(this, event);return false;')
                    .appendTo($ul);
                $("<div>").addClass('highlightable title').text(query.name).appendTo($queryItem);
                $("<div>").addClass('highlightable description').text(query.description).appendTo($queryItem);
            }
        }

        if (service.layers) {
            var $p = $("<div>").appendTo($serviceElements)
            $("<h3>Layer</h3>").appendTo($p);
            var $ul = $("<ul>").appendTo($p);
            for (var l in service.layers) {
                var layer = service.layers[l];
                var $layeritem = $("<li>")
                    .addClass('webgis-addservices-servicelayer' + (layer.visible ? ' visible' : ''))
                    .attr('data-layerid', layer.id)
                    .attr('onclick', '$._webgis_addServicesToc_handleLayerClick(this, event);return false;')
                    .appendTo($ul);
                $("<div>").addClass('highlightable title').text(layer.name).appendTo($layeritem);
                $("<div>").addClass('highlightable description').text(layer.description).appendTo($layeritem);
            }
        }
    }

    var loadServiceInfo = function ($parent, serviceIds, callback) {
        webgis.ajax({
            type: 'post',
            data: webgis.hmac.appendHMACData({ ids: serviceIds, f: 'json' }),
            url: webgis.baseUrl + '/rest/serviceinfo',
            success: function (result) {
                for (var s in result.services) {
                    var service = result.services[s];

                    var $item = $parent.find(".webgis-addservices_toc-item[data-serviceid='" + service.id + "']");
                    setServiceItemDescription($item, service);
                }

                if (callback) {
                    callback(result);
                }
            }
        });
    };

    var loadCustomServiceInfo = function (data, callback, errorCallback) {
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
    }

    var removeUnhighlightedItems = function ($element) {
        if ($element.hasClass('webgis-highlight-text') || $element.prop('tagName') === 'H3')
            return;

        if ($element.find('.webgis-highlight-text').length === 0) {
            $element.remove();
        } else {
            $element.children().each(function (i, e) {
                removeUnhighlightedItems($(e));
            });
        }
    }

    var highlightElements = function ($parent, vals) {
        $parent.find('.highlightable').each(function (i, e) {
            $(e).html($(e).text().highlightText(vals));
        });
    }

    $._webgis_addServicesToc_handleLayerClick = function (sender, evt) {
        evt.stopPropagation();

        var $this = $(sender), $item = $this.closest('.webgis-addservices_toc-item'), $holder = $item.closest('.webgis-addservices_toc-holder');

        var map = $holder.get(0)._map;
        var serviceId = $item.attr('data-serviceid'),
            layerId = $this.attr('data-layerid');

        var service = map.getService(serviceId);
        if (service) {
            var visible = !$this.hasClass('visible');
            service.setLayerVisibility([layerId], visible);

            if (visible) {
                $this.addClass('visible');
            } else {
                $this.removeClass('visible');
            }

            map.events.fire('refresh-toc-checkboxes');
        }
    }

    $._webgis_addServicesToc_handleQueryClick = function (sender, evt) {
        evt.stopPropagation();

        var $this = $(sender), $item = $this.closest('.webgis-addservices_toc-item'), $holder = $item.closest('.webgis-addservices_toc-holder');

        var map = $holder.get(0)._map;
        var serviceId = $item.attr('data-serviceid'),
            queryId = $this.attr('data-queryid');

        var $queryCombo = $(map._webgisContainer).find('.webgis-detail-search-combo');

        var comboValue = serviceId + '/queries/' + queryId;
        if ($queryCombo.find("option[value='" + comboValue + "']").length > 0) {
            $queryCombo.val(comboValue);
            $queryCombo.trigger('change');

            if ($queryCombo.closest('.webgis-detail-search-holder').css('display') === 'none') {
                $(map._webgisContainer).find('#webgis-detail-search-button').trigger('click');
            }
        }

        var $modal = $this.closest('.webgis-modal');
        
        if ($modal.length === 1) {
            $modal.webgis_modal('close');
        }
    }

})(webgis.$ || jQuery);
