(function ($) {
    "use strict";
    $.fn.webgis_servicesToc = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on webgis.$.webgis_servicesToc');
        }
    };
    var defaults = {
        map: null,
        gdi_button: false,
        themes: false,
        opacity_slider: true,
    };
    var methods = {
        init: function (options) {
            var $this = $(this), options = $.extend({}, defaults, options);
            return this.each(function () {
                new initUI(this, options);
            });
        }
    };
    var initUI = function (elem, options) {
        var $elem = $(elem);
        elem._map = options.map;
        options.map.events.on('onaddservice', addService, elem);
        options.map.events.on('onremoveservice', removeService, elem);
        elem.options = options;
        $elem.addClass('webgis-services_toc-holder');
        var $ul = $("<ul class='webgis-services_toc-list'></ul>").appendTo($elem);
        // gibt es eigentlich nicht mehr
        //if ($.fn.sortable) {
        //    $ul.sortable({
        //        update: function (event, ui) {
        //            var $ul = $(this);
        //            updateOrder(this.parentNode._map, $ul);
        //        },
        //        handle: '.webgis-services_toc-title-text'
        //    });
        //}
        if (options.map !== null) {
            for (var serviceId in options.map.services) {
                var service = options.map.services[serviceId];
                addService({}, service, elem);
            }
        }
        if (options.gdi_button == true) {
            $("<button>Dienste hinzufügen</button>")
                .addClass('webgis-button light')
                .appendTo($elem)
                .click(function () {
                var map = this.parentNode._map;
                $('body').webgis_modal({
                    title: 'Dienst hinzufügen',
                    onload: function ($content) {
                        $content.webgis_addServicesToc({ map: map });
                    }
                });
            });
        }
    };
    $.servicesToc = {
        checkItemVisibility: function (e, service, parent) {
            parent = parent || $('.webgis-services_toc-holder');
            var $elem = $(parent);
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
    };
    var addService = function (e, service, parent) {
        var parent = parent || this;
        if (service == null || (service.serviceInfo.type !== 'image' && service.serviceInfo.type !== 'collection'))
            return;
        var $ul = $(parent).find('.webgis-services_toc-list');
        var $li = $("<li class='webgis-services_toc-title webgis-services_toc-collapsable'></li>");
        $li.prependTo($ul);
        $("<span style='position:absolute' class='webgis-services_toc-plus webgis-api-icon webgis-api-icon-triangle-1-s'></span>").appendTo($li);
        $("<div class='webgis-services_toc-title-text'>" + service.name + "</div>").click(function (event) {
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
        var $div = $("<div class='webgis-services_toc-content' style='display:none' id='div_'></div>").appendTo($li);
        $li.get(0).service = service;
        var $content_ul = $("<ul>").appendTo($div);
        if (parent.options && parent.options.themes) {
            var $themes_li = $("<li class='webgis-services_toc-item-group webgis-services_toc-collapsable'>").appendTo($content_ul);
            $("<div><span style='position:absolute' class='webgis-api-icon webgis-api-icon-triangle-1-s'></span><span style='margin-left:14px'>&nbsp;Themen</span></div>").appendTo($themes_li)
                .click(function () {
                    var $this = $(this);
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
            var $toc_ul = $("<ul style='display:none'>").appendTo($themes_li);
            for (var l = 0, to = service.layers.length; l < to; l++) {
                var layer = service.layers[l];
                if (layer.locked)
                    continue;
                var name = layer.name.split('\\');
                name = name[name.length - 1];
                var $toc_li = $("<li class='webgis-services_toc-item webgis-services_toc-checkable'></li>").appendTo($toc_ul);
                $("<span><img class='webgis-service_toc-checkable-icon' />&nbsp;" + name + "</span>").appendTo($toc_li);
                $toc_li.get(0).layer = layer;
                $toc_li.click(function () {
                    var $li = $(this).closest('.webgis-services_toc-title');
                    if ($li.length == 1) {
                        var service = $li.get(0).service;
                        var layer = this.layer;
                        if (service && layer) {
                            service.setLayerVisibility([layer.id], !$(this).find('.webgis-service_toc-checkable-icon').hasClass('webgis-checked'));
                        }
                    }
                });
            }
        }
        updateOrder(service.map, $ul);
        $.servicesToc.checkItemVisibility(service, parent);
        service.events.on('onchangevisibility', $.servicesToc.checkItemVisibility, parent);
    };
    var removeService = function (e, service, parent) {
        var parent = parent || this;
        if (service == null)
            return;
        var $ul = $(parent).find('.webgis-services_toc-list');
        $ul.find('li').each(function (i, e) {
            if (e.service && e.service.id == service.id) {
                $(e).remove();
            }
        });
        updateOrder(service.map, $ul);
    };
    var updateOrder = function (map, $ul) {
        // Nix mehr machen: darf nur noch über ServiceOrder Dialog oder im MapBuilder erfolgen!!!
        //var serviceIds = [];
        //$ul.children('li').each(function (i, e) {
        //    serviceIds.push(e.service.id);
        //});
        //map.setServiceOrder(serviceIds);
    };
})(webgis.$ || jQuery);
